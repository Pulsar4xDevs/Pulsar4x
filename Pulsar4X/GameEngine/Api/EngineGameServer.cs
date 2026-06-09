using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Messaging;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;

namespace Pulsar4X.Engine.Api
{
    /// <summary>
    /// The engine-side implementation of <see cref="IGameServer"/>: wraps a live <see cref="Game"/>
    /// and projects it into the faction-scoped DTO contract defined in Pulsar4X.Api. It has no UI
    /// dependency, so the same class backs both the in-process adapter and a headless dedicated server.
    /// </summary>
    public sealed class EngineGameServer : IGameServer, IDisposable
    {
        private readonly Game _game;

        /// <summary>
        /// Per-command-type translators: map an authorized API <see cref="GameCommand"/> to an engine
        /// order and dispatch it. Adding a new command is one DTO (in Pulsar4X.Api) + one entry here.
        /// Each translator runs only after <see cref="SubmitCommand"/> has resolved the commanded
        /// entity and confirmed the faction owns it.
        /// </summary>
        private readonly Dictionary<Type, Func<Entity, Entity, GameCommand, CommandResult>> _translators;

        // Active subscriber sinks. Time is global, so clock changes are broadcast to all of them;
        // entity events are faction-filtered inside each ServerSubscription.
        private readonly object _sinkLock = new();
        private readonly List<Action<GameEventEnvelope>> _sinks = new();
        private readonly DateChangedEventHandler _onDateChanged;

        public EngineGameServer(Game game)
        {
            _game = game;
            _translators = new Dictionary<Type, Func<Entity, Entity, GameCommand, CommandResult>>
            {
                [typeof(Pulsar4X.Api.RenameCommand)] = TranslateRename,
            };

            // The clock advances on the engine thread with no per-tick request from clients; push a
            // TimeChanged delta whenever it does, so clients never have to poll the time.
            _onDateChanged = _ => BroadcastTimeChanged();
            _game.TimePulse.GameGlobalDateChangedEvent += _onDateChanged;
        }

        public void Dispose()
        {
            _game.TimePulse.GameGlobalDateChangedEvent -= _onDateChanged;
            lock (_sinkLock) _sinks.Clear();
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

        public TimeState GetTimeState(PlayerSession session) => ToTimeState(_game.TimePulse);

        private static TimeState ToTimeState(MasterTimePulse tp)
            => new TimeState(tp.GameGlobalDateTime, tp.IsRunning, tp.TimeMultiplier, tp.Ticklength, tp.TickFrequency);

        private void BroadcastTimeChanged()
        {
            var evt = new GameEventEnvelope(GameEventType.TimeChanged, Time: ToTimeState(_game.TimePulse));
            Action<GameEventEnvelope>[] sinks;
            lock (_sinkLock) sinks = _sinks.ToArray();
            foreach (var sink in sinks)
                sink(evt);
        }

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

            if (!_translators.TryGetValue(command.GetType(), out var translate))
                return CommandResult.Reject($"Unsupported command: {command.GetType().Name}");

            return translate(faction, commanded, command);
        }

        // Fully qualified engine order type: it shares its name with the API DTO.
        private CommandResult TranslateRename(Entity faction, Entity commanded, GameCommand command)
        {
            var rename = (Pulsar4X.Api.RenameCommand)command;
            bool accepted = Pulsar4X.Names.RenameCommand.CreateRenameCommand(_game, faction, commanded, rename.NewName);
            return accepted
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");
        }

        // ----- queries -----

        public IReadOnlyList<SystemSummary> GetKnownSystems(PlayerSession session)
        {
            var result = new List<SystemSummary>();
            if (!_game.Factions.TryGetValue(session.FactionId, out var faction))
                return result;

            foreach (var systemId in faction.GetDataBlob<FactionInfoDB>().KnownSystems)
            {
                var system = FindSystem(systemId);
                if (system != null)
                    result.Add(new SystemSummary(system.ID, system.NameDB.GetName(session.FactionId)));
            }
            return result;
        }

        public SystemSnapshot GetSystemSnapshot(PlayerSession session, string systemId)
        {
            var system = FindSystem(systemId)
                ?? throw new ArgumentException($"Unknown system '{systemId}'.", nameof(systemId));
            return BuildSystemSnapshot(system, session.FactionId);
        }

        // Faction-scoped snapshot of a whole system. Shared by the bulk query and the SystemRevealed
        // push so a reveal ships the system and its visible entities in one self-contained delta.
        private SystemSnapshot BuildSystemSnapshot(StarSystem system, int factionId)
        {
            // Mirror the client's SystemState: make this faction aware of the system's
            // default-visible neutral bodies (stars, planets, …) before filtering.
            system.SetupDefaultNeutralEntitiesForFaction(factionId);

            const EntityFilter all = EntityFilter.Friendly | EntityFilter.Neutral | EntityFilter.Hostile;
            var visible = system.GetFilteredEntities(all, factionId);

            var entities = new List<EntitySnapshot>(visible.Count);
            foreach (var entity in visible)
                entities.Add(Project(entity, factionId));

            return new SystemSnapshot
            {
                SystemId = system.ID,
                Name = system.NameDB.GetName(factionId),
                DateTime = system.StarSysDateTime,
                Entities = entities,
            };
        }

        public EntitySnapshot? GetEntitySnapshot(PlayerSession session, int entityId)
            => _game.GlobalManager.TryGetGlobalEntityById(entityId, out var entity)
                ? Project(entity, session.FactionId)
                : null;

        // ----- events -----

        public IDisposable Subscribe(PlayerSession session, Action<GameEventEnvelope> handler)
            => new ServerSubscription(this, session, handler);

        // ----- projection helpers -----

        private static EntitySnapshot Project(Entity entity, int factionId)
        {
            var views = new List<IComponentView>(6);

            if (entity.TryGetDataBlob<NameDB>(out var name))
                views.Add(new NameView(name.GetName(factionId)));

            if (entity.TryGetDataBlob<Pulsar4X.Movement.PositionDB>(out var pos))
                views.Add(new PositionView(
                    new Vec3(pos.AbsolutePosition.X, pos.AbsolutePosition.Y, pos.AbsolutePosition.Z),
                    new Vec3(pos.RelativePosition.X, pos.RelativePosition.Y, pos.RelativePosition.Z),
                    pos.Parent?.Id));

            if (entity.TryGetDataBlob<OrbitDB>(out var orbit))
                views.Add(new OrbitView(
                    orbit.SemiMajorAxis / 1000.0,       // engine stores SMA in metres
                    orbit.Eccentricity,
                    orbit.OrbitalPeriod.TotalSeconds,
                    orbit.Parent?.Id));

            if (entity.TryGetDataBlob<MassVolumeDB>(out var mass))
                views.Add(new MassVolumeView(mass.MassTotal, mass.RadiusInM, mass.DensityDry_gcm));

            if (entity.TryGetDataBlob<SystemBodyInfoDB>(out var body))
                views.Add(new BodyView(
                    body.BodyType.ToDescription(),
                    body.Gravity,
                    body.BaseTemperature,
                    body.LengthOfDay,
                    body.AxialTilt,
                    body.Tectonics.ToDescription(),
                    body.MagneticField,
                    body.SupportsPopulations));

            if (entity.TryGetDataBlob<StarInfoDB>(out var star))
                views.Add(new StarView(
                    star.SpectralType.ToDescription(),
                    star.SpectralSubDivision,
                    star.Class,
                    star.LuminosityClass.ToString(),
                    star.Temperature,
                    star.Luminosity,
                    star.Age,
                    star.MinHabitableRadius_AU,
                    star.MaxHabitableRadius_AU));

            if (entity.TryGetDataBlob<ColonyInfoDB>(out var colony))
            {
                long population = 0;
                foreach (var speciesPop in colony.Population.Values)
                    population += speciesPop;
                int? planetId = colony.PlanetEntity.IsValid ? colony.PlanetEntity.Id : null;
                views.Add(new ColonyView(population, planetId));
            }

            if (entity.TryGetDataBlob<ShipInfoDB>(out var ship))
                views.Add(new ShipView(ship.Design.Name));

            return new EntitySnapshot
            {
                Id = entity.Id,
                FactionId = entity.FactionOwnerID,
                Relation = RelationOf(entity, factionId),
                Views = views,
            };
        }

        private static OwnerRelation RelationOf(Entity entity, int factionId)
        {
            if (entity.FactionOwnerID == factionId) return OwnerRelation.Owned;
            if (entity.FactionOwnerID == Game.NeutralFactionId) return OwnerRelation.Neutral;
            return OwnerRelation.Hostile;
        }

        private StarSystem? FindSystem(string systemId)
            => _game.Systems.FirstOrDefault(s => s.ID == systemId);

        /// <summary>
        /// One subscriber's live feed. Bridges the engine's <see cref="MessagePublisher"/> (faction-
        /// filtered) into self-contained <see cref="GameEventEnvelope"/>s — projecting the affected
        /// entity so the client needs no follow-up request — and registers the sink for the global time
        /// broadcast. Disposing unsubscribes and deregisters.
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

                lock (server._sinkLock) server._sinks.Add(sink);
            }

            // Broadcast messages (no faction) and messages for this faction pass through.
            private bool PassesFactionFilter(Message m) => m.FactionId is null || m.FactionId == _session.FactionId;

            private Task Forward(GameEventType type, Message m)
            {
                // Deltas carry their payload so the client never makes a follow-up request.
                EntitySnapshot? entity = null;
                SystemSnapshot? system = null;

                if ((type is GameEventType.EntityAdded or GameEventType.EntityRevealed
                          or GameEventType.EntityChanged or GameEventType.EntityRenamed)
                    && m.EntityId is { } id
                    && _server._game.GlobalManager.TryGetGlobalEntityById(id, out var e))
                {
                    entity = Project(e, _session.FactionId);
                }
                else if (type == GameEventType.SystemRevealed
                    && m.SystemId is { } sysId
                    && _server.FindSystem(sysId) is { } revealed)
                {
                    // Ship the whole revealed system — including every entity now visible to this
                    // faction — so the client adds it without pulling anything back.
                    system = _server.BuildSystemSnapshot(revealed, _session.FactionId);
                }

                _sink(new GameEventEnvelope(type, m.SystemId, m.EntityId, m.FactionId, Entity: entity, System: system));
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                foreach (var (type, handler) in _handlers)
                    MessagePublisher.Instance.Unsubscribe(type, handler);
                _handlers.Clear();
                lock (_server._sinkLock) _server._sinks.Remove(_sink);
            }
        }
    }
}
