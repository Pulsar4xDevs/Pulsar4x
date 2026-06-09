using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Messaging;
using Pulsar4X.Names;
using Pulsar4X.Orbits;

namespace Pulsar4X.Engine.Api
{
    /// <summary>
    /// The engine-side implementation of <see cref="IGameServer"/>: wraps a live <see cref="Game"/>
    /// and projects it into the faction-scoped DTO contract defined in Pulsar4X.Api. It has no UI
    /// dependency, so the same class backs both the in-process adapter and a headless dedicated server.
    /// </summary>
    public sealed class EngineGameServer : IGameServer
    {
        private readonly Game _game;

        public EngineGameServer(Game game) => _game = game;

        // ----- connection -----

        public ConnectResult Connect(ConnectRequest request)
        {
            if (_game.Factions.Count == 0)
                return ConnectResult.Fail("Game has no factions to bind to.");

            // Slice behaviour: bind to the first player faction. Real faction selection / auth
            // (via ConnectRequest.Credential) lands with the networking phase.
            int factionId = _game.Factions.Keys.First();
            var session = new PlayerSession(Guid.NewGuid(), factionId);
            return ConnectResult.Ok(session, new GameInfo(_game.Name ?? "Pulsar4X", _game.LastSaveGitHash ?? ""));
        }

        public void Disconnect(PlayerSession session) { /* no per-session server state yet */ }

        // ----- time -----

        public TimeState GetTimeState(PlayerSession session)
        {
            var tp = _game.TimePulse;
            return new TimeState(tp.GameGlobalDateTime, tp.IsRunning, tp.TimeMultiplier, tp.Ticklength);
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
                case TimeControlAction.StepOnce:
                    if (request.StepLength is { } step) tp.TimeStep(tp.GameGlobalDateTime + step);
                    else tp.TimeStep();
                    break;
            }
        }

        // ----- commands -----

        public CommandResult SubmitCommand(PlayerSession session, GameCommand command)
        {
            if (!_game.Factions.TryGetValue(session.FactionId, out var faction))
                return CommandResult.Reject("Unknown faction for session.");

            switch (command)
            {
                case Pulsar4X.Api.RenameCommand rename:
                    if (!_game.GlobalManager.TryGetGlobalEntityById(rename.TargetEntityId, out var target))
                        return CommandResult.Reject($"Entity {rename.TargetEntityId} not found.");
                    // Fully qualified: the engine's order type shares the name with the API DTO.
                    Pulsar4X.Names.RenameCommand.CreateRenameCommand(_game, faction, target, rename.NewName);
                    return CommandResult.Ok(Guid.NewGuid().ToString("N"));

                default:
                    return CommandResult.Reject($"Unsupported command: {command.GetType().Name}");
            }
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

            // Mirror the client's SystemState: make this faction aware of the system's
            // default-visible neutral bodies (stars, planets, …) before filtering.
            system.SetupDefaultNeutralEntitiesForFaction(session.FactionId);

            const EntityFilter all = EntityFilter.Friendly | EntityFilter.Neutral | EntityFilter.Hostile;
            var visible = system.GetFilteredEntities(all, session.FactionId);

            var entities = new List<EntitySnapshot>(visible.Count);
            foreach (var entity in visible)
                entities.Add(Project(entity, session.FactionId));

            return new SystemSnapshot
            {
                SystemId = system.ID,
                Name = system.NameDB.GetName(session.FactionId),
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
            => new EngineEventBridge(session.FactionId, handler);

        // ----- projection helpers -----

        private static EntitySnapshot Project(Entity entity, int factionId)
        {
            var views = new List<IComponentView>(3);

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
    }

    /// <summary>
    /// Bridges the engine's <see cref="MessagePublisher"/> to a single faction-scoped
    /// <see cref="GameEventEnvelope"/> sink. Disposing unsubscribes from the publisher.
    /// </summary>
    internal sealed class EngineEventBridge : IDisposable
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

        private readonly int _factionId;
        private readonly Action<GameEventEnvelope> _sink;
        private readonly List<(MessageTypes Type, MessagePublisher.MessageHandler Handler)> _handlers = new();

        public EngineEventBridge(int factionId, Action<GameEventEnvelope> sink)
        {
            _factionId = factionId;
            _sink = sink;

            foreach (var (msg, evt) in Map)
            {
                GameEventType eventType = evt;
                MessagePublisher.MessageHandler handler = m => Forward(eventType, m);
                MessagePublisher.Instance.Subscribe(msg, handler, PassesFactionFilter);
                _handlers.Add((msg, handler));
            }
        }

        // Broadcast messages (no faction) and messages for this faction pass through.
        private bool PassesFactionFilter(Message m) => m.FactionId is null || m.FactionId == _factionId;

        private Task Forward(GameEventType type, Message m)
        {
            _sink(new GameEventEnvelope(type, m.SystemId, m.EntityId, m.FactionId));
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var (type, handler) in _handlers)
                MessagePublisher.Instance.Unsubscribe(type, handler);
            _handlers.Clear();
        }
    }
}
