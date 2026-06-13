using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Messaging;

namespace Pulsar4X.Engine.Api
{
    /// <summary>
    /// The engine-side implementation of <see cref="IGameServer"/>: wraps a live <see cref="Game"/> and
    /// orchestrates sessions, commands, and the event stream. All translation of engine state into the
    /// Pulsar4X.Api DTOs is delegated to <see cref="GameProjector"/>. It has no UI dependency, so the same
    /// class backs both the in-process adapter and a headless dedicated server.
    /// </summary>
    public sealed class EngineGameServer : IGameServer, IDisposable
    {
        private readonly Game _game;
        private readonly GameProjector _projector;
        private readonly CommandTranslator _commands;

        // Active subscriptions. Time is global, but faction snapshots (funds) differ per subscriber, so
        // we track the subscriptions (which know their faction), not bare sinks.
        private readonly object _sinkLock = new();
        private readonly List<ServerSubscription> _subscriptions = new();
        private readonly DateChangedEventHandler _onDateChanged;

        public EngineGameServer(Game game)
        {
            _game = game;
            _projector = new GameProjector(game);
            _commands = new CommandTranslator(game);

            // The clock advances on the engine thread with no per-tick request from clients; push a
            // TimeChanged delta (and a refreshed faction snapshot — funds track the economy) whenever it
            // does, so clients never have to poll.
            _onDateChanged = _ => OnGlobalDateChanged();
            _game.TimePulse.GameGlobalDateChangedEvent += _onDateChanged;
        }

        public void Dispose()
        {
            _game.TimePulse.GameGlobalDateChangedEvent -= _onDateChanged;
            lock (_sinkLock) _subscriptions.Clear();
        }

        private ServerSubscription[] SnapshotSubscriptions()
        {
            lock (_sinkLock) return _subscriptions.ToArray();
        }

        // ----- connection -----

        public ConnectResult Connect(ConnectRequest request)
        {
            if (_game.Factions.Count == 0)
                return ConnectResult.Fail("Game has no factions to bind to.");

            // Bind to the requested faction when given (the in-process host knows the player's
            // faction); otherwise fall back to the first player faction — never the GameMaster,
            // who sees everything. Credential-gated auth lands with networking.
            int factionId = request.FactionId is { } requested && _game.Factions.ContainsKey(requested)
                ? requested
                : _game.Factions.Keys.FirstOrDefault(id => id != _game.GameMasterFaction.Id, _game.Factions.Keys.First());
            var session = new PlayerSession(Guid.NewGuid(), factionId);
            return ConnectResult.Ok(session, new GameInfo(_game.Name ?? "Pulsar4X", _game.LastSaveGitHash ?? "")
            {
                StrictNewtonian = _game.Settings.StrictNewtonion,
                UseRelativeVelocity = _game.Settings.UseRelativeVelocity,
            });
        }

        public void Disconnect(PlayerSession session)
        {
            SetSystemFocus(session, null);
        }

        // The focused system gets foreground-observer scheduling priority in the engine. One focus
        // per server is enough for the in-process case; per-session focus lands with networking.
        private string? _focusedSystemId;

        public void SetSystemFocus(PlayerSession session, string? systemId)
        {
            if (systemId == _focusedSystemId) return;

            if (_focusedSystemId != null)
                _game.Systems.FirstOrDefault(s => s.ID == _focusedSystemId)?.DecrementExternalObserver(true);
            if (systemId != null)
                _game.Systems.FirstOrDefault(s => s.ID == systemId)?.IncrementExternalObserver(true);

            _focusedSystemId = systemId;
        }

        // ----- time -----

        public void SetTimeControl(PlayerSession session, TimeControlRequest request)
        {
            var tp = _game.TimePulse;
            switch (request.Action)
            {
                case TimeControlAction.Pause:
                    tp.PauseTime();
                    break;
                case TimeControlAction.Start:
                    tp.StartTime();
                    break;
                case TimeControlAction.SetSpeed:
                    if (request.Multiplier is { } multiplier) tp.TimeMultiplier = multiplier;
                    break;
                case TimeControlAction.SetTickLength:
                    if (request.TickLength is { } tickLength) tp.Ticklength = tickLength;
                    break;
                case TimeControlAction.SetTickFrequency:
                    if (request.TickFrequency is { } tickFrequency) tp.TickFrequency = tickFrequency;
                    break;
                case TimeControlAction.StepOnce:
                    if (request.StepLength is { } step) tp.TimeStep(tp.GameGlobalDateTime + step);
                    else tp.TimeStep();
                    break;
            }

            // Control changes (pause/start/speed/tick settings) don't advance the date, so push a
            // TimeChanged delta here too. A step that does advance the date also fires the date event,
            // which is fine — clients just apply the latest snapshot.
            BroadcastTimeChanged();
        }

        // Control changes (pause/start/speed/tick) don't move the economy, so push only the clock.
        private void BroadcastTimeChanged()
        {
            var evt = new GameEventEnvelope(GameEventType.TimeChanged, Time: _projector.ProjectTime());
            foreach (var sub in SnapshotSubscriptions())
                sub.Send(evt);
        }

        // A clock advance refreshes the time plus each subscriber's per-faction snapshots: the
        // (funds-bearing) faction, the fleet hierarchy (order progress, locations, and ship
        // membership all evolve as the simulation runs), and research (progress accrues per tick).
        private void OnGlobalDateChanged()
        {
            var time = new GameEventEnvelope(GameEventType.TimeChanged, Time: _projector.ProjectTime());
            foreach (var sub in SnapshotSubscriptions())
            {
                sub.Send(time);
                var faction = _projector.ProjectFaction(sub.FactionId);
                if (faction != null)
                    sub.Send(new GameEventEnvelope(GameEventType.FactionChanged, Faction: faction));
                sub.Send(FleetsEnvelope(sub.FactionId));
                var research = _projector.ProjectResearch(sub.FactionId);
                if (research != null)
                    sub.Send(new GameEventEnvelope(GameEventType.ResearchChanged, Research: research));
                var commanders = _projector.ProjectCommanders(sub.FactionId);
                if (commanders != null)
                    sub.Send(new GameEventEnvelope(GameEventType.CommandersChanged, Commanders: commanders));
                RefreshColonies(sub);
                RefreshMovers(sub);
            }
        }

        // Colony economics (population, stockpiles, mining, infrastructure) and the planet beneath
        // (mineral depletion) mutate every econ tick without engine messages. Colonies are few, so
        // re-push them (and their planets) whole each clock advance until generic change-tracking exists.
        private void RefreshColonies(ServerSubscription sub)
        {
            if (!_game.Factions.TryGetValue(sub.FactionId, out var faction)) return;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info)) return;

            foreach (var colony in info.Colonies)
            {
                if (!colony.IsValid) continue;
                PushEntityRefresh(colony, sub.FactionId);

                var planet = colony.GetDataBlob<Pulsar4X.Colonies.ColonyInfoDB>()?.PlanetEntity;
                if (planet != null && planet.IsValid)
                    PushEntityRefresh(planet, sub.FactionId);
            }
        }

        // Keplerian movement is propagated client-side from OrbitView elements, but warp and
        // newtonian thrust aren't predictable from a snapshot — re-push those movers each clock
        // advance so their PositionView is at most a tick old.
        private void RefreshMovers(ServerSubscription sub)
        {
            if (!_game.Factions.TryGetValue(sub.FactionId, out var faction)) return;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info)) return;

            foreach (var systemId in info.KnownSystems)
            {
                var system = _game.Systems.FirstOrDefault(s => s.ID == systemId);
                if (system == null) continue;

                foreach (var mover in system.GetAllEntitiesWithDataBlob<Pulsar4X.Movement.WarpMovingDB>())
                    if (mover.FactionOwnerID == sub.FactionId)
                        PushEntityRefresh(mover, sub.FactionId);
                foreach (var mover in system.GetAllEntitiesWithDataBlob<Pulsar4X.Movement.NewtonMoveDB>())
                    if (mover.FactionOwnerID == sub.FactionId)
                        PushEntityRefresh(mover, sub.FactionId);
                foreach (var mover in system.GetAllEntitiesWithDataBlob<Pulsar4X.Movement.NewtonSimpleMoveDB>())
                    if (mover.FactionOwnerID == sub.FactionId)
                        PushEntityRefresh(mover, sub.FactionId);
                foreach (var beam in system.GetAllEntitiesWithDataBlob<Pulsar4X.Weapons.BeamInfoDB>())
                    if (beam.FactionOwnerID == sub.FactionId)
                        PushEntityRefresh(beam, sub.FactionId);
                // Energy generation/storage evolves through engine-scheduled interrupts with no
                // message; re-push so the power display's plot stays current.
                foreach (var generator in system.GetAllEntitiesWithDataBlob<Pulsar4X.Energy.EnergyGenAbilityDB>())
                    if (generator.FactionOwnerID == sub.FactionId)
                        PushEntityRefresh(generator, sub.FactionId);
            }
        }

        // The fleet hierarchy is always pushed whole (root fleets + unattached ships) — it's small,
        // and a self-contained replacement keeps the client's apply logic trivial.
        private GameEventEnvelope FleetsEnvelope(int factionId)
        {
            var (fleets, unattached) = _projector.ProjectFleetHierarchy(factionId);
            return new GameEventEnvelope(GameEventType.FleetsChanged, Fleets: fleets, UnattachedShips: unattached);
        }

        // ----- commands -----

        public CommandResult SubmitCommand(PlayerSession session, GameCommand command)
        {
            if (command is null)
                return CommandResult.Reject("Null command.");

            if (!_game.Factions.TryGetValue(session.FactionId, out var faction))
                return CommandResult.Reject("Unknown faction for session.");

            if (!_game.GlobalManager.TryGetGlobalEntityById(command.TargetEntityId, out var commanded))
                return CommandResult.Reject($"Entity {command.TargetEntityId} not found.");

            // Uniform authorization: a faction may only command entities it owns — or itself (e.g.
            // CreateFleet targets the faction). (Commands with a secondary target — e.g. a move
            // destination — carry that as a separate DTO field, which the translator resolves; only
            // the commanded entity is ownership-checked here.)
            if (commanded.Id != session.FactionId && commanded.FactionOwnerID != session.FactionId)
                return CommandResult.Reject("Faction does not control the commanded entity.");

            var result = _commands.Translate(faction, commanded, command);

            // Many instant orders (assign scientist, change funding, queue ops, …) mutate DataBlobs
            // without raising an engine message, so after any accepted command re-project the
            // commanded entity and push it — the client sees the effect without waiting for a tick.
            if (result.Accepted)
            {
                PushEntityRefresh(commanded, session.FactionId);

                // Assignment commands re-post commanders with no engine message; the roster is tiny,
                // so re-push it after every accepted command rather than special-casing them.
                PushCommanders(session.FactionId);

                // A new component design also registers a research project for itself.
                if (command is CreateComponentDesignCommand)
                    PushComponentDesigns(session.FactionId);

                // Standing orders live on the fleet-tree snapshot, and replacing them raises no
                // engine message (no entity is added/removed/reshaped).
                if (command is SetStandingOrdersCommand)
                {
                    var fleets = FleetsEnvelope(session.FactionId);
                    foreach (var sub in SnapshotSubscriptions())
                        if (sub.FactionId == session.FactionId)
                            sub.Send(fleets);
                }
            }

            return result;
        }

        /// <summary>
        /// In-process bridge for the client-side interactive component designer: the faction's
        /// design-time state (unlocked data store, tech levels, missile designs). Deliberately NOT on
        /// <see cref="IGameServer"/> — the contract stays engine-type-free; a network adapter instead
        /// syncs this state to the client on connect (both DataBlobs are save-serializable).
        /// </summary>
        public (FactionInfoDB Info, FactionTechDB Techs)? GetFactionDesignData(PlayerSession session)
        {
            if (!_game.Factions.TryGetValue(session.FactionId, out var faction)) return null;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info)) return null;
            if (!faction.TryGetDataBlob<FactionTechDB>(out var techs)) return null;
            return (info, techs);
        }

        private void PushComponentDesigns(int factionId)
        {
            var designs = _projector.ProjectComponentDesigns(factionId);
            var research = _projector.ProjectResearch(factionId);
            foreach (var sub in SnapshotSubscriptions())
            {
                if (sub.FactionId != factionId) continue;
                if (designs != null)
                    sub.Send(new GameEventEnvelope(GameEventType.ComponentDesignsChanged, ComponentDesigns: designs));
                if (research != null)
                    sub.Send(new GameEventEnvelope(GameEventType.ResearchChanged, Research: research));
            }
        }

        private void PushCommanders(int factionId)
        {
            var commanders = _projector.ProjectCommanders(factionId);
            if (commanders == null) return;
            var evt = new GameEventEnvelope(GameEventType.CommandersChanged, Commanders: commanders);
            foreach (var sub in SnapshotSubscriptions())
                if (sub.FactionId == factionId)
                    sub.Send(evt);
        }

        private void PushEntityRefresh(Entity commanded, int factionId)
        {
            if (commanded.Manager is not StarSystem system) return;

            var evt = new GameEventEnvelope(GameEventType.EntityChanged, system.ID, commanded.Id, factionId,
                Entity: _projector.ProjectEntity(commanded, factionId));
            foreach (var sub in SnapshotSubscriptions())
                if (sub.FactionId == factionId)
                    sub.Send(evt);
        }

        // ----- events -----

        public IDisposable Subscribe(PlayerSession session, Action<GameEventEnvelope> handler)
            => new ServerSubscription(this, session, handler);

        // Pushed to a subscriber immediately on Subscribe so the client gets its starting state with no
        // fetch: time + faction, then every system this faction already knows (with its visible
        // entities), then the fleet hierarchy. Thereafter deltas keep it current.
        private void PushInitialState(PlayerSession session, Action<GameEventEnvelope> sink)
        {
            sink(new GameEventEnvelope(GameEventType.TimeChanged, Time: _projector.ProjectTime()));

            var faction = _projector.ProjectFaction(session.FactionId);
            if (faction != null)
                sink(new GameEventEnvelope(GameEventType.FactionChanged, Faction: faction));

            if (_game.Factions.TryGetValue(session.FactionId, out var factionEntity))
            {
                foreach (var systemId in factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems)
                {
                    var system = _projector.ProjectSystem(systemId, session.FactionId);
                    if (system != null)
                        sink(new GameEventEnvelope(GameEventType.SystemRevealed, systemId, System: system));
                }
            }

            sink(FleetsEnvelope(session.FactionId));

            var research = _projector.ProjectResearch(session.FactionId);
            if (research != null)
                sink(new GameEventEnvelope(GameEventType.ResearchChanged, Research: research));

            var designs = _projector.ProjectComponentDesigns(session.FactionId);
            if (designs != null)
                sink(new GameEventEnvelope(GameEventType.ComponentDesignsChanged, ComponentDesigns: designs));

            var commanders = _projector.ProjectCommanders(session.FactionId);
            if (commanders != null)
                sink(new GameEventEnvelope(GameEventType.CommandersChanged, Commanders: commanders));

            // The faction's persisted event log is the starting backlog; live events follow as
            // they happen (the subscription's EventManager bridge starts after this, so nothing
            // is double-delivered).
            if (_game.Factions.TryGetValue(session.FactionId, out var loggedFaction)
                && loggedFaction.TryGetDataBlob<FactionInfoDB>(out var loggedInfo)
                && loggedInfo.EventLog != null)
            {
                var backlog = loggedInfo.EventLog.GetEvents()
                    .Select(e => _projector.ProjectLogEvent(e, session.FactionId))
                    .ToList();
                if (backlog.Count > 0)
                    sink(new GameEventEnvelope(GameEventType.LogEvent, Log: backlog));
            }
        }

        /// <summary>
        /// One subscriber's live feed. Bridges the engine's <see cref="MessagePublisher"/> (faction-
        /// filtered) into self-contained <see cref="GameEventEnvelope"/>s — delegating to the server's
        /// <see cref="GameProjector"/> for payloads so the client needs no follow-up request — and
        /// registers for the global time/faction broadcast. Disposing unsubscribes and deregisters.
        /// </summary>
        private sealed class ServerSubscription : IDisposable
        {
            private static readonly (MessageTypes Msg, GameEventType Evt)[] Map =
            {
                (MessageTypes.EntityAdded, GameEventType.EntityAdded),
                (MessageTypes.EntityRemoved, GameEventType.EntityRemoved),
                (MessageTypes.EntityHidden, GameEventType.EntityHidden),
                (MessageTypes.EntityRevealed, GameEventType.EntityRevealed),
                (MessageTypes.StarSystemRevealed, GameEventType.SystemRevealed),
                (MessageTypes.EntityRenamed, GameEventType.EntityRenamed),
                (MessageTypes.DBAdded, GameEventType.EntityChanged),
                (MessageTypes.DBRemoved, GameEventType.EntityChanged),
                (MessageTypes.EntityChanged, GameEventType.EntityChanged),
                (MessageTypes.FleetReorganized, GameEventType.FleetsChanged),
            };

            private readonly EngineGameServer _server;
            private readonly PlayerSession _session;
            private readonly Action<GameEventEnvelope> _sink;
            private readonly List<(MessageTypes Type, MessagePublisher.MessageHandler Handler)> _handlers = new();
            private readonly Action<Pulsar4X.Events.Event> _onLogEvent;

            public ServerSubscription(EngineGameServer server, PlayerSession session, Action<GameEventEnvelope> sink)
            {
                _server = server;
                _session = session;
                _sink = sink;

                foreach (var (msg, evt) in Map)
                {
                    GameEventType eventType = evt;
                    MessagePublisher.MessageHandler handler = m => Forward(eventType, m);
                    MessagePublisher.Instance.Subscribe(msg, handler, PassesFactionFilter);
                    _handlers.Add((msg, handler));
                }

                lock (server._sinkLock) server._subscriptions.Add(this);

                // Prime the new subscriber with its starting state (time + faction + known systems +
                // fleets + the game-log backlog).
                server.PushInitialState(session, sink);

                // Bridge the game-log stream (EventManager) after the backlog push, so an event
                // can't land in both. The sync-state bridges above are upserts, so their
                // overlap with the initial state is harmless; log entries append.
                _onLogEvent = OnLogEvent;
                Pulsar4X.Events.EventManager.Instance.Subscribe(
                    Pulsar4X.Events.EventTypeHelper.GetAllEventTypes(), _onLogEvent);
            }

            // Mirrors FactionEventLog's filter: an event reaches a faction when addressed to it or
            // when listed as concerned.
            private void OnLogEvent(Pulsar4X.Events.Event e)
            {
                if (e.FactionId != _session.FactionId && !e.ConcernedFactions.Contains(_session.FactionId))
                    return;

                _sink(new GameEventEnvelope(GameEventType.LogEvent, e.SystemId, e.EntityId, e.FactionId,
                    Log: new[] { _server._projector.ProjectLogEvent(e, _session.FactionId) }));
            }

            internal int FactionId => _session.FactionId;
            internal void Send(GameEventEnvelope evt) => _sink(evt);

            // Broadcast messages (no faction) and messages for this faction pass through.
            private bool PassesFactionFilter(Message m) => m.FactionId is null || m.FactionId == _session.FactionId;

            private Task Forward(GameEventType type, Message m)
            {
                var projector = _server._projector;

                // A fleet reorganisation just re-pushes the faction's whole fleet tree.
                if (type == GameEventType.FleetsChanged)
                {
                    _sink(_server.FleetsEnvelope(_session.FactionId));
                    return Task.CompletedTask;
                }

                // Deltas carry their payload so the client never makes a follow-up request.
                EntitySnapshot? entity = null;
                SystemSnapshot? system = null;

                if (type is GameEventType.EntityAdded or GameEventType.EntityRevealed
                         or GameEventType.EntityChanged or GameEventType.EntityRenamed)
                {
                    // The engine's add/change messages aren't visibility-scoped; drop pushes for
                    // entities this faction can't see — or can't be resolved (mid-construction
                    // DBAdded messages) — rather than leak them into its galaxy.
                    if (m.EntityId is not { } id
                        || !_server._game.GlobalManager.TryGetGlobalEntityById(id, out var e)
                        || e.Manager == null
                        || !e.Manager.IsEntityVisibleToFaction(e, _session.FactionId))
                        return Task.CompletedTask;

                    entity = projector.ProjectEntity(e, _session.FactionId);
                }
                else if (type == GameEventType.SystemRevealed && m.SystemId is { } sysId)
                {
                    // Ship the whole revealed system — including every entity now visible to this
                    // faction — so the client adds it without pulling anything back.
                    system = projector.ProjectSystem(sysId, _session.FactionId);
                }

                _sink(new GameEventEnvelope(type, m.SystemId, m.EntityId, m.FactionId, Entity: entity, System: system));

                // Entity creation/destruction/rename can reshape the fleet list (membership/names).
                // Explicit fleet ops push via FleetReorganized; this backstops entity-level changes.
                if (type is GameEventType.EntityAdded or GameEventType.EntityRemoved or GameEventType.EntityRenamed)
                    _sink(_server.FleetsEnvelope(_session.FactionId));

                return Task.CompletedTask;
            }

            public void Dispose()
            {
                foreach (var (type, handler) in _handlers)
                    MessagePublisher.Instance.Unsubscribe(type, handler);
                _handlers.Clear();
                Pulsar4X.Events.EventManager.Instance.Unsubscribe(
                    Pulsar4X.Events.EventTypeHelper.GetAllEventTypes(), _onLogEvent);
                lock (_server._sinkLock) _server._subscriptions.Remove(this);
            }
        }
    }
}
