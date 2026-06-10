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
            // faction); otherwise fall back to the first. Credential-gated auth lands with networking.
            int factionId = request.FactionId is { } requested && _game.Factions.ContainsKey(requested)
                ? requested
                : _game.Factions.Keys.First();
            var session = new PlayerSession(Guid.NewGuid(), factionId);
            return ConnectResult.Ok(session, new GameInfo(_game.Name ?? "Pulsar4X", _game.LastSaveGitHash ?? ""));
        }

        public void Disconnect(PlayerSession session) { /* no per-session server state yet */ }

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

        // A clock advance refreshes both the time and each subscriber's (funds-bearing) faction snapshot.
        private void OnGlobalDateChanged()
        {
            var time = new GameEventEnvelope(GameEventType.TimeChanged, Time: _projector.ProjectTime());
            foreach (var sub in SnapshotSubscriptions())
            {
                sub.Send(time);
                var faction = _projector.ProjectFaction(sub.FactionId);
                if (faction != null)
                    sub.Send(new GameEventEnvelope(GameEventType.FactionChanged, Faction: faction));
            }
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

            // Uniform authorization: a faction may only command entities it owns. (Commands with a
            // secondary target — e.g. a move destination — carry that as a separate DTO field, which
            // the translator resolves; only the commanded entity is ownership-checked here.)
            if (commanded.FactionOwnerID != session.FactionId)
                return CommandResult.Reject("Faction does not control the commanded entity.");

            return _commands.Translate(faction, commanded, command);
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

            sink(new GameEventEnvelope(GameEventType.FleetsChanged, Fleets: _projector.ProjectFleets(session.FactionId)));
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
                (MessageTypes.FleetReorganized, GameEventType.FleetsChanged),
            };

            private readonly EngineGameServer _server;
            private readonly PlayerSession _session;
            private readonly Action<GameEventEnvelope> _sink;
            private readonly List<(MessageTypes Type, MessagePublisher.MessageHandler Handler)> _handlers = new();

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

                // Prime the new subscriber with its starting state (time + faction + known systems + fleets).
                server.PushInitialState(session, sink);
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
                    _sink(new GameEventEnvelope(GameEventType.FleetsChanged, Fleets: projector.ProjectFleets(_session.FactionId)));
                    return Task.CompletedTask;
                }

                // Deltas carry their payload so the client never makes a follow-up request.
                EntitySnapshot? entity = null;
                SystemSnapshot? system = null;

                if ((type is GameEventType.EntityAdded or GameEventType.EntityRevealed
                          or GameEventType.EntityChanged or GameEventType.EntityRenamed)
                    && m.EntityId is { } id
                    && _server._game.GlobalManager.TryGetGlobalEntityById(id, out var e))
                {
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
                    _sink(new GameEventEnvelope(GameEventType.FleetsChanged, Fleets: projector.ProjectFleets(_session.FactionId)));

                return Task.CompletedTask;
            }

            public void Dispose()
            {
                foreach (var (type, handler) in _handlers)
                    MessagePublisher.Instance.Unsubscribe(type, handler);
                _handlers.Clear();
                lock (_server._sinkLock) _server._subscriptions.Remove(this);
            }
        }
    }
}
