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
using Pulsar4X.GeoSurveys;
using Pulsar4X.JumpPoints;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

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

        /// <summary>The faction's whole command hierarchy: its root fleets (each with nested sub-fleets
        /// and member ships) plus the ships sitting at the root outside any fleet.</summary>
        public (IReadOnlyList<FleetSnapshot> Fleets, IReadOnlyList<ShipSnapshot> UnattachedShips) ProjectFleetHierarchy(int factionId)
        {
            var fleets = new List<FleetSnapshot>();
            var unattached = new List<ShipSnapshot>();
            if (!_game.Factions.TryGetValue(factionId, out var faction)) return (fleets, unattached);
            if (!faction.TryGetDataBlob<FleetDB>(out var factionFleet)) return (fleets, unattached);

            var roots = factionFleet.RootDB?.Children;
            if (roots == null) return (fleets, unattached);

            // The faction-visible entity set per system, computed at most once per hierarchy projection
            // (used to resolve each fleet's "orbiting" body to an ancestor the faction can actually see).
            var visibleCache = new Dictionary<string, HashSet<int>>();

            foreach (var child in roots)
            {
                if (child.HasDataBlob<FleetDB>())
                    fleets.Add(ProjectFleet(child, factionId, visibleCache));
                else
                    unattached.Add(ProjectShip(child, factionId));
            }
            return (fleets, unattached);
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
            (e, f) => e.TryGetDataBlob<GeoSurveyableDB>(out var g) ? new GeoSurveyView(g.IsSurveyComplete(f)) : null,
            (e, f) => e.TryGetDataBlob<JPSurveyableDB>(out var j) ? new GravSurveyView(j.IsSurveyComplete(f)) : null,
            // A jump point is only part of a faction's world once that faction has discovered it.
            (e, f) => e.TryGetDataBlob<JumpPointDB>(out var jp) && jp.IsDiscovered.Contains(f) ? new JumpPointView() : null,
            (e, _) => e.HasDataBlob<CargoStorageDB>() ? new CargoStorageView() : null,
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

        private FleetSnapshot ProjectFleet(Entity fleet, int factionId, Dictionary<string, HashSet<int>> visibleCache)
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
                        subFleets.Add(ProjectFleet(child, factionId, visibleCache));
                    else
                        ships.Add(ProjectShip(child, factionId));
                }
            }

            Entity? flagship = null;
            if (flagshipId >= 0 && fleet.Manager != null)
                fleet.Manager.TryGetEntityById(flagshipId, out flagship);

            // The fleet entity lives in its flagship's manager, so this is the fleet's current system.
            var system = fleet.Manager as StarSystem;

            // Resolve what the flagship is orbiting to the nearest ancestor this faction can see
            // (skipping hidden entities such as un-surveyed anomalies).
            (int Id, string Name)? orbiting = null;
            if (flagship != null && system != null
                && flagship.TryGetDataBlob<Pulsar4X.Movement.PositionDB>(out var pos))
            {
                var visible = VisibleIds(system, factionId, visibleCache);
                var parent = pos.Parent;
                while (parent != null)
                {
                    if (visible.Contains(parent.Id))
                    {
                        orbiting = (parent.Id, parent.GetName(factionId));
                        break;
                    }
                    parent = parent.TryGetDataBlob<Pulsar4X.Movement.PositionDB>(out var parentPos)
                        ? parentPos.Parent
                        : null;
                }
            }

            string? commander = null;
            if (flagship != null && flagship.TryGetDataBlob<ShipInfoDB>(out var flagInfo)
                && flagInfo.CommanderID != -1 && flagship.Manager != null
                && flagship.Manager.TryGetEntityById(flagInfo.CommanderID, out var commanderEntity))
            {
                commander = commanderEntity.GetName(factionId);
            }

            return new FleetSnapshot
            {
                Id = fleet.Id,
                Name = fleet.GetName(factionId),
                FlagshipId = flagshipId >= 0 ? flagshipId : null,
                FlagshipName = flagship?.GetName(factionId),
                CommanderName = commander,
                SystemId = system?.ID,
                SystemName = system?.NameDB.GetName(factionId),
                OrbitingEntityId = orbiting?.Id,
                OrbitingName = orbiting?.Name,
                InheritOrders = fleetDB?.InheritOrders ?? false,
                CanGeoSurvey = fleet.HasGeoSurveyAbility(),
                CanGravSurvey = fleet.HasJPSurveyAbililty(),
                Orders = ProjectOrders(fleet),
                SubFleets = subFleets,
                Ships = ships,
            };
        }

        private static ShipSnapshot ProjectShip(Entity ship, int factionId)
        {
            ship.TryGetDataBlob<ShipInfoDB>(out var shipInfo);

            string? commander = null;
            if (shipInfo != null && shipInfo.CommanderID != -1 && ship.Manager != null
                && ship.Manager.TryGetEntityById(shipInfo.CommanderID, out var commanderEntity))
            {
                commander = commanderEntity.GetName(factionId);
            }

            return new ShipSnapshot(ship.Id, ship.GetName(factionId), ship.Manager?.ManagerID ?? "",
                                    shipInfo?.Design.Name ?? "", commander)
            {
                Orders = ProjectOrders(ship),
            };
        }

        private static IReadOnlyList<OrderSnapshot> ProjectOrders(Entity entity)
        {
            if (!entity.TryGetDataBlob<OrderableDB>(out var orderable) || orderable.ActionList.Count == 0)
                return Array.Empty<OrderSnapshot>();

            var orders = new List<OrderSnapshot>(orderable.ActionList.Count);
            foreach (var action in orderable.ActionList)
                orders.Add(new OrderSnapshot(action.Name, action.IsRunning, action.GetIsFinished));
            return orders;
        }

        private static HashSet<int> VisibleIds(StarSystem system, int factionId, Dictionary<string, HashSet<int>> cache)
        {
            if (!cache.TryGetValue(system.ID, out var visible))
            {
                const EntityFilter all = EntityFilter.Friendly | EntityFilter.Neutral | EntityFilter.Hostile;
                visible = system.GetFilteredEntities(all, factionId).Select(e => e.Id).ToHashSet();
                cache[system.ID] = visible;
            }
            return visible;
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
