using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;

namespace Pulsar4X.Engine.Api
{
    /// <summary>
    /// The single place a live <see cref="Game"/> is translated into the faction-scoped Pulsar4X.Api
    /// DTOs. <see cref="EngineGameServer"/> handles sessions/commands/events and delegates <em>all</em>
    /// projection here, so the mapping grows in one isolated, cohesive place rather than swelling the
    /// server. Adding an entity view is one entry in <see cref="ViewProjectors"/> (plus a small
    /// <c>To*View</c> helper if it needs logic); new snapshot kinds get their own <c>Project*</c> method.
    /// </summary>
    internal sealed class GameProjector
    {
        private readonly Game _game;

        public GameProjector(Game game) => _game = game;

        // ----- top-level projections -----

        public TimeState ProjectTime()
        {
            var tp = _game.TimePulse;
            return new TimeState(tp.GameGlobalDateTime, tp.IsRunning, tp.TimeMultiplier, tp.Ticklength, tp.TickFrequency);
        }

        public FactionSnapshot? ProjectFaction(int factionId)
        {
            if (!_game.Factions.TryGetValue(factionId, out var faction)) return null;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info)) return null;
            string name = faction.TryGetDataBlob<NameDB>(out var nameDB) ? nameDB.OwnersName : factionId.ToString();
            return new FactionSnapshot(name, info.Abbreviation, info.Money.GetCurrentFunds());
        }

        public IReadOnlyList<SystemSummary> ProjectKnownSystems(int factionId)
        {
            var result = new List<SystemSummary>();
            if (!_game.Factions.TryGetValue(factionId, out var faction)) return result;

            foreach (var systemId in faction.GetDataBlob<FactionInfoDB>().KnownSystems)
            {
                var system = FindSystem(systemId);
                if (system != null)
                    result.Add(new SystemSummary(system.ID, system.NameDB.GetName(factionId)));
            }
            return result;
        }

        /// <summary>Projects a system by id, or null if this game has no such system.</summary>
        public SystemSnapshot? ProjectSystem(string systemId, int factionId)
        {
            var system = FindSystem(systemId);
            return system == null ? null : ProjectSystem(system, factionId);
        }

        public SystemSnapshot ProjectSystem(StarSystem system, int factionId)
        {
            // Mirror the client's SystemState: make this faction aware of the system's default-visible
            // neutral bodies (stars, planets, …) before filtering.
            system.SetupDefaultNeutralEntitiesForFaction(factionId);

            const EntityFilter all = EntityFilter.Friendly | EntityFilter.Neutral | EntityFilter.Hostile;
            var visible = system.GetFilteredEntities(all, factionId);

            var entities = new List<EntitySnapshot>(visible.Count);
            foreach (var entity in visible)
                entities.Add(ProjectEntity(entity, factionId));

            return new SystemSnapshot
            {
                SystemId = system.ID,
                Name = system.NameDB.GetName(factionId),
                DateTime = system.StarSysDateTime,
                Entities = entities,
            };
        }

        public EntitySnapshot ProjectEntity(Entity entity, int factionId)
        {
            var views = new List<IComponentView>(ViewProjectors.Length);
            foreach (var project in ViewProjectors)
                if (project(entity, factionId) is { } view)
                    views.Add(view);

            return new EntitySnapshot
            {
                Id = entity.Id,
                FactionId = entity.FactionOwnerID,
                Relation = RelationOf(entity, factionId),
                Kind = ClassifyKind(entity),
                Views = views,
            };
        }

        public IReadOnlyList<FleetSnapshot> ProjectFleets(int factionId)
        {
            var result = new List<FleetSnapshot>();
            if (!_game.Factions.TryGetValue(factionId, out var faction)) return result;
            if (!faction.TryGetDataBlob<FleetDB>(out var factionFleet)) return result;

            var roots = factionFleet.RootDB?.Children;
            if (roots == null) return result;

            foreach (var fleet in roots)
            {
                if (fleet.HasDataBlob<ShipInfoDB>()) continue; // ships only appear nested under a fleet
                result.Add(ProjectFleet(fleet, factionId));
            }
            return result;
        }

        // ----- entity views: add a view by adding one entry here (+ a To*View helper if it needs logic) -----

        private static readonly Func<Entity, int, IComponentView?>[] ViewProjectors =
        {
            (e, f) => e.TryGetDataBlob<NameDB>(out var n) ? new NameView(n.GetName(f)) : null,
            (e, _) => e.TryGetDataBlob<Pulsar4X.Movement.PositionDB>(out var p) ? ToPositionView(p) : null,
            (e, _) => e.TryGetDataBlob<OrbitDB>(out var o) ? ToOrbitView(o) : null,
            (e, _) => e.TryGetDataBlob<MassVolumeDB>(out var m) ? new MassVolumeView(m.MassTotal, m.RadiusInM, m.DensityDry_gcm) : null,
            (e, _) => e.TryGetDataBlob<SystemBodyInfoDB>(out var b) ? ToBodyView(b) : null,
            (e, _) => e.TryGetDataBlob<StarInfoDB>(out var s) ? ToStarView(s) : null,
            (e, _) => e.TryGetDataBlob<ColonyInfoDB>(out var c) ? ToColonyView(c) : null,
            (e, _) => e.TryGetDataBlob<ShipInfoDB>(out var sh) ? new ShipView(sh.Design.Name) : null,
        };

        private static PositionView ToPositionView(Pulsar4X.Movement.PositionDB p)
            => new(new Vec3(p.AbsolutePosition.X, p.AbsolutePosition.Y, p.AbsolutePosition.Z),
                   new Vec3(p.RelativePosition.X, p.RelativePosition.Y, p.RelativePosition.Z),
                   p.Parent?.Id);

        private static OrbitView ToOrbitView(OrbitDB o)
            => new(o.SemiMajorAxis / 1000.0,   // engine stores SMA in metres
                   o.Eccentricity, o.OrbitalPeriod.TotalSeconds, o.Parent?.Id);

        private static BodyView ToBodyView(SystemBodyInfoDB b)
            => new(b.BodyType.ToDescription(), b.Gravity, b.BaseTemperature, b.LengthOfDay,
                   b.AxialTilt, b.Tectonics.ToDescription(), b.MagneticField, b.SupportsPopulations);

        private static StarView ToStarView(StarInfoDB s)
            => new(s.SpectralType.ToDescription(), s.SpectralSubDivision, s.Class, s.LuminosityClass.ToString(),
                   s.Temperature, s.Luminosity, s.Age, s.MinHabitableRadius_AU, s.MaxHabitableRadius_AU);

        private static ColonyView ToColonyView(ColonyInfoDB c)
        {
            long population = 0;
            foreach (var speciesPop in c.Population.Values)
                population += speciesPop;
            int? planetId = c.PlanetEntity.IsValid ? c.PlanetEntity.Id : null;
            return new ColonyView(population, planetId);
        }

        // ----- fleet hierarchy -----

        private static FleetSnapshot ProjectFleet(Entity fleet, int factionId)
        {
            fleet.TryGetDataBlob<FleetDB>(out var fleetDB);
            int flagshipId = fleetDB?.FlagShipID ?? -1;

            var subFleets = new List<FleetSnapshot>();
            var ships = new List<ShipSnapshot>();
            if (fleetDB != null)
            {
                foreach (var child in fleetDB.GetChildren())
                {
                    if (child.HasDataBlob<FleetDB>())
                        subFleets.Add(ProjectFleet(child, factionId));
                    else
                        ships.Add(new ShipSnapshot(child.Id, child.GetName(factionId), child.Manager?.ManagerID ?? ""));
                }
            }

            var orders = new List<string>();
            if (fleet.TryGetDataBlob<OrderableDB>(out var orderable))
                foreach (var action in orderable.ActionList)
                    orders.Add(action.Name);

            string? location = null;
            if (flagshipId >= 0 && fleet.Manager != null
                && fleet.Manager.TryGetEntityById(flagshipId, out var flagship)
                && flagship.TryGetDataBlob<Pulsar4X.Movement.PositionDB>(out var pos))
            {
                location = pos.Parent?.GetName(factionId);
            }

            return new FleetSnapshot
            {
                Id = fleet.Id,
                Name = fleet.GetName(factionId),
                FlagshipId = flagshipId >= 0 ? flagshipId : null,
                FlagshipLocationName = location,
                Orders = orders,
                SubFleets = subFleets,
                Ships = ships,
            };
        }

        // ----- classification -----

        // Mirrors the client's former Utils.EntityBodyType so the body classification is computed once,
        // server-side, and travels in the snapshot.
        private static BodyKind ClassifyKind(Entity entity)
        {
            if (entity.TryGetDataBlob<SystemBodyInfoDB>(out var body))
            {
                switch (body.BodyType)
                {
                    case BodyType.Asteroid: return BodyKind.Asteroid;
                    case BodyType.Comet: return BodyKind.Comet;
                    case BodyType.DwarfPlanet: return BodyKind.DwarfPlanet;
                    case BodyType.Moon: return BodyKind.Moon;
                    case BodyType.GasDwarf:
                    case BodyType.GasGiant:
                    case BodyType.IceGiant:
                    case BodyType.Terrestrial: return BodyKind.Planet;
                }
            }
            if (entity.HasDataBlob<StarInfoDB>()) return BodyKind.Star;
            if (entity.HasDataBlob<ColonyInfoDB>()) return BodyKind.Colony;
            if (entity.HasDataBlob<ShipInfoDB>()) return BodyKind.Ship;
            return BodyKind.Unknown;
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
}
