using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.JumpPoints;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Technology;

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

        /// <summary>A display-ready game-log entry: the event-type name plus entity/faction names
        /// resolved with the subscriber's faction scope.</summary>
        public LogEvent ProjectLogEvent(Pulsar4X.Events.Event e, int factionId)
        {
            string? entityName = null;
            if (e.EntityId is { } entityId && _game.GlobalManager.TryGetGlobalEntityById(entityId, out var entity))
                entityName = entity.GetName(factionId);

            string? factionName = null;
            if (e.FactionId is { } eventFactionId && _game.Factions.TryGetValue(eventFactionId, out var faction))
                factionName = faction.GetName(factionId);

            return new LogEvent(e.StarDate, e.EventType.ToString(), e.Message,
                e.SystemId, e.EntityId, entityName, factionName);
        }

        // ----- entity views: add a view by adding one entry here (+ a To*View helper if it needs logic) -----

        private static readonly Func<Entity, int, IComponentView?>[] ViewProjectors =
        {
            (e, f) => e.TryGetDataBlob<NameDB>(out var n) ? new NameView(n.GetName(f)) : null,
            (e, _) => e.TryGetDataBlob<Pulsar4X.Movement.PositionDB>(out var p) ? ToPositionView(p) : null,
            (e, _) => e.TryGetDataBlob<OrbitDB>(out var o) ? ToOrbitView(o) : null,
            (e, _) => e.TryGetDataBlob<MassVolumeDB>(out var m)
                ? new MassVolumeView(m.MassTotal, m.RadiusInM, m.DensityDry_gcm) { DryMassKg = m.MassDry } : null,
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<OrderableDB>(out var ord) && ord.ActionList.Count > 0
                ? new OrdersView(ProjectOrders(e)) : null,
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<Pulsar4X.Movement.NewtonThrustAbilityDB>(out var th)
                ? ToThrustView(th, e, f) : null,
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<Pulsar4X.Movement.WarpAbilityDB>(out var wa)
                ? new WarpAbilityView(wa.MaxSpeed) : null,
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<Pulsar4X.Movement.WarpMovingDB>(out var wm)
                ? new WarpMovingView(wm.CurrentNonNewtonionVectorMS.Length())
                {
                    EntryPointAbsolute = ToVec3(wm.EntryPointAbsolute),
                    ExitPointAbsolute = ToVec3(wm.ExitPointAbsolute),
                    ExitPointRelative = ToVec3(wm.ExitPointrelative),
                    TargetEntityId = wm.TargetEntity?.Id,
                }
                : null,
            (e, _) => e.TryGetDataBlob<SystemBodyInfoDB>(out var b) ? ToBodyView(b) : null,
            (e, _) => e.TryGetDataBlob<StarInfoDB>(out var s) ? ToStarView(s) : null,
            (e, f) => e.TryGetDataBlob<ColonyInfoDB>(out var c) ? ToColonyView(c, e, f) : null,
            (e, _) => e.TryGetDataBlob<AtmosphereDB>(out var a) ? ToAtmosphereView(a, a.OwningEntity?.Manager?.Game) : null,
            // Non-owners see a ship's class but not its internals (health, armor, crew).
            (e, f) => e.TryGetDataBlob<ShipInfoDB>(out var sh)
                ? (e.FactionOwnerID == f ? ToShipView(sh, e, f) : new ShipView(sh.Design.Name))
                : null,
            (e, f) => e.TryGetDataBlob<GeoSurveyableDB>(out var g) ? ToGeoSurveyView(g, f) : null,
            (e, _) => e.HasDataBlob<ColonizeableDB>() ? new ColonizableView() : null,
            (e, f) => e.TryGetDataBlob<MineralsDB>(out var md) ? ToMineralDepositsView(md, e, f) : null,
            (e, f) => e.TryGetDataBlob<JPSurveyableDB>(out var j) ? ToGravSurveyView(j, f) : null,
            // A jump point is only part of a faction's world once that faction has discovered it.
            (e, f) => e.TryGetDataBlob<JumpPointDB>(out var jp) && jp.IsDiscovered.Contains(f) ? new JumpPointView() : null,
            // The views below expose an entity's internals (cargo, installations, mining economics),
            // so they are only projected for the owning faction.
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<CargoStorageDB>(out var cs) ? ToCargoStorageView(cs, e) : null,
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<InfrastructureDB>(out var inf)
                ? new InfrastructureView(inf.CapacityProvided, inf.CapacityRequired, inf.CapacityAvailable, inf.Efficiency,
                    HasInstalledInfrastructure(e))
                : null,
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<ComponentInstancesDB>(out var ci) ? ToInstallationsView(ci, e) : null,
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<ColonyInfoDB>(out var col) ? ToColonyMiningView(col, e, f) : null,
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<NavalAcademyDB>(out var na) ? ToNavalAcademyView(na) : null,
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<IndustryAbilityDB>(out var ind) ? ToIndustryView(ind, e, f) : null,
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<LocalConstructionDB>(out var lc) ? ToConstructionView(lc, e, f) : null,
            // A lab's queue/economics are internal to its owner; other factions just see the entity.
            (e, f) => e.FactionOwnerID == f && e.TryGetDataBlob<ResearcherDB>(out var r) ? ToResearcherView(r, e, f) : null,
            (e, f) => e.FactionOwnerID == f && e.HasDataBlob<Pulsar4X.Weapons.FireControlAbilityDB>() ? ToFireControlView(e) : null,
            (e, _) => e.TryGetDataBlob<Pulsar4X.Movement.NewtonMoveDB>(out var nm) ? ToNewtonMoveView(nm, e) : null,
            (e, _) => e.TryGetDataBlob<Pulsar4X.Movement.NewtonSimpleMoveDB>(out var ns) ? ToNewtonSimpleMoveView(ns) : null,
            (e, _) => e.HasDataBlob<Pulsar4X.Weapons.ProjectileInfoDB>() ? new ProjectileView() : null,
            (e, _) => e.TryGetDataBlob<Pulsar4X.Weapons.BeamInfoDB>(out var beam)
                ? new BeamView(ToVec3(beam.Positions.Item1), ToVec3(beam.Positions.Item2)) : null,
        };

        private static Vec3 ToVec3(Pulsar4X.Orbital.Vector3 v) => new(v.X, v.Y, v.Z);

        private static PositionView ToPositionView(Pulsar4X.Movement.PositionDB p)
            => new(new Vec3(p.AbsolutePosition.X, p.AbsolutePosition.Y, p.AbsolutePosition.Z),
                   new Vec3(p.RelativePosition.X, p.RelativePosition.Y, p.RelativePosition.Z),
                   p.Parent?.Id);

        private static OrbitView ToOrbitView(OrbitDB o)
            => new(o.SemiMajorAxis / 1000.0,   // engine stores SMA in metres
                   o.Eccentricity, o.OrbitalPeriod.TotalSeconds, o.Parent?.Id)
            {
                SemiMajorAxisM = o.SemiMajorAxis,
                InclinationRad = o.Inclination,
                LongitudeOfAscendingNodeRad = o.LongitudeOfAscendingNode,
                ArgumentOfPeriapsisRad = o.ArgumentOfPeriapsis,
                MeanAnomalyAtEpochRad = o.MeanAnomalyAtEpoch,
                MeanMotionRadPerSec = o.MeanMotion,
                Epoch = o.Epoch,
                StandardGravParameter = o.GravitationalParameter_m3S2,
                ParentSoiRadiusM = o.ParentDB is OrbitDB parentOrbit ? OrbitMath.GetSOIRadius(parentOrbit) : 0,
                SoiRadiusM = o.Parent != null ? OrbitMath.GetSOIRadius(o) : 0,
            };

        private static OrbitView ToOrbitView(Pulsar4X.Orbital.KeplerElements ke, int? parentId)
            => new(ke.SemiMajorAxis / 1000.0, ke.Eccentricity, ke.Period, parentId)
            {
                SemiMajorAxisM = ke.SemiMajorAxis,
                InclinationRad = ke.Inclination,
                LongitudeOfAscendingNodeRad = ke.LoAN,
                ArgumentOfPeriapsisRad = ke.AoP,
                MeanAnomalyAtEpochRad = ke.MeanAnomalyAtEpoch,
                MeanMotionRadPerSec = ke.MeanMotion,
                Epoch = ke.Epoch,
                StandardGravParameter = ke.StandardGravParameter,
            };

        private static NewtonMoveView ToNewtonMoveView(Pulsar4X.Movement.NewtonMoveDB n, Entity entity)
        {
            double thrust = entity.TryGetDataBlob<Pulsar4X.Movement.NewtonThrustAbilityDB>(out var thrustAbility)
                ? thrustAbility.ThrustInNewtons
                : 0;
            return new NewtonMoveView(
                n.SOIParent?.Id,
                n.SOIParent?.GetSOI_m() ?? 0,
                ToVec3(n.CurrentVector_ms),
                ToVec3(n.ManuverDeltaV),
                thrust,
                ToOrbitView(n.GetElements(), n.SOIParent?.Id));
        }

        private static NewtonSimpleMoveView ToNewtonSimpleMoveView(Pulsar4X.Movement.NewtonSimpleMoveDB n)
            => new(n.SOIParent?.Id,
                   n.SOIParent?.GetSOI_m() ?? 0,
                   ToOrbitView(n.CurrentTrajectory, n.SOIParent?.Id));

        private static BodyView ToBodyView(SystemBodyInfoDB b)
            => new(b.BodyType.ToDescription(), b.Gravity, b.BaseTemperature, b.LengthOfDay,
                   b.AxialTilt, b.Tectonics.ToDescription(), b.MagneticField, b.SupportsPopulations,
                   b.RadiationLevel, b.AtmosphericDust);

        private static AtmosphereView ToAtmosphereView(AtmosphereDB a, Game? game)
        {
            var composition = new List<GasAmount>(a.CompositionByPercent.Count);
            foreach (var (gasId, percent) in a.CompositionByPercent)
            {
                string name = game != null && game.AtmosphericGases.TryGetValue(gasId, out var gas) ? gas.Name : gasId;
                a.Composition.TryGetValue(gasId, out var partialPressure);
                composition.Add(new GasAmount(name, percent, gasId, partialPressure));
            }

            return new AtmosphereView(a.SurfaceTemperature, a.Pressure, a.Hydrosphere, (double)a.HydrosphereExtent)
            {
                Composition = composition,
            };
        }

        private static GeoSurveyView ToGeoSurveyView(GeoSurveyableDB g, int factionId)
        {
            bool started = g.HasSurveyStarted(factionId);
            double percent = 0;
            long completed = 0;
            if (started && g.PointsRequired > 0)
            {
                percent = (1.0 - (double)g.GeoSurveyStatus[factionId] / g.PointsRequired) * 100;
                completed = g.PointsRequired - g.GeoSurveyStatus[factionId];
            }

            return new GeoSurveyView(g.IsSurveyComplete(factionId), started, percent, g.PointsRequired, completed);
        }

        private static GravSurveyView ToGravSurveyView(JPSurveyableDB j, int factionId)
        {
            bool started = j.HasSurveyStarted(factionId);
            double percent = 0;
            if (started && j.PointsRequired > 0)
                percent = (1.0 - (double)j.SurveyPointsRemaining[factionId] / j.PointsRequired) * 100;

            return new GravSurveyView(j.IsSurveyComplete(factionId), started, percent);
        }

        private static MineralDepositsView? ToMineralDepositsView(MineralsDB minerals, Entity body, int factionId)
        {
            var game = body.Manager?.Game;
            if (game == null || !game.Factions.TryGetValue(factionId, out var faction)) return null;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo)) return null;

            int factionMask = factionInfo.FactionMask;
            var mineralsById = factionInfo.Data.CargoGoods.GetMineralsList().ToDictionary(m => m.ID);

            var rows = new List<MineralDepositRow>(minerals.Minerals.Count);
            foreach (var (mineralId, deposit) in minerals.Minerals)
            {
                if (!mineralsById.TryGetValue(mineralId, out var mineral)) continue;

                var amount = deposit.Amount.Resolve(factionMask, ObscureWithError);
                rows.Add(new MineralDepositRow(
                    mineralId,
                    mineral.Name,
                    amount.Access switch
                    {
                        AccessLevel.Full => DepositAccess.Full,
                        AccessLevel.Partial => DepositAccess.Partial,
                        _ => DepositAccess.None,
                    },
                    amount.Value,
                    deposit.Accessibility));
            }

            return new MineralDepositsView(rows);
        }

        /// <summary>Obscures a value by a deterministic +/- 20% error margin (stable for the same input).</summary>
        private static long ObscureWithError(long value)
        {
            var hash = value.GetHashCode();
            var factor = (hash % 41 - 20) / 100.0;
            return (long)(value * (1 + factor));
        }

        private static bool HasInstalledInfrastructure(Entity colony)
        {
            return colony.TryGetDataBlob<ComponentInstancesDB>(out var instances)
                && instances.TryGetComponentsByAttribute<InfrastructureCapacityAtb>(out var components)
                && components.Count > 0;
        }

        private static StarView ToStarView(StarInfoDB s)
            => new(s.SpectralType.ToDescription(), s.SpectralSubDivision, s.Class, s.LuminosityClass.ToString(),
                   s.Temperature, s.Luminosity, s.Age, s.MinHabitableRadius_AU, s.MaxHabitableRadius_AU,
                   s.LuminosityClass.ToDescription())
            {
                SpectralTypeIndex = (int)s.SpectralType,
            };

        private static ShipView ToShipView(ShipInfoDB shipInfo, Entity ship, int factionId)
        {
            string? commander = null;
            if (shipInfo.CommanderID >= 0 && ship.Manager != null
                && ship.Manager.TryGetEntityById(shipInfo.CommanderID, out var commanderEntity))
            {
                commander = commanderEntity.GetName(factionId);
            }

            double totalHealth = 0;
            int totalCount = 0, operationalCount = 0;
            if (ship.TryGetDataBlob<ComponentInstancesDB>(out var components))
            {
                foreach (var instances in components.ComponentsByDesign.Values)
                {
                    foreach (var instance in instances)
                    {
                        totalHealth += instance.HealthPercent;
                        totalCount++;
                        if (instance.HealthPercent > instance.StopWorkingAtPercent && instance.IsEnabled)
                            operationalCount++;
                    }
                }
            }

            double armorThickness = 0;
            if (ship.TryGetDataBlob<Pulsar4X.Damage.EntityDamageProfileDB>(out var damage)
                && damage.Armor.thickness > 0)
            {
                armorThickness = damage.Armor.thickness;
            }

            return new ShipView(
                shipInfo.Design.Name,
                shipInfo.Design.CrewReq,
                commander,
                totalCount > 0 ? totalHealth / totalCount : 1,
                operationalCount,
                totalCount,
                armorThickness);
        }

        private static ThrustView? ToThrustView(Pulsar4X.Movement.NewtonThrustAbilityDB thrust, Entity ship, int factionId)
        {
            // ΔV at full tanks: the dry mass pushed by however much fuel the tanks could hold.
            double maxDeltaV = 0;
            string fuelName = "";
            if (ship.Manager?.Game is { } game
                && game.Factions.TryGetValue(factionId, out var faction)
                && faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo)
                && thrust.FuelType != null && factionInfo.Data.CargoGoods.Contains(thrust.FuelType)
                && factionInfo.Data.CargoGoods.GetAny(thrust.FuelType) is { } fuel)
            {
                fuelName = fuel.Name;
                if (thrust.ExhaustVelocity > 0 && fuel.VolumePerUnit > 0
                    && ship.TryGetDataBlob<MassVolumeDB>(out var massVolume)
                    && ship.TryGetDataBlob<CargoStorageDB>(out var storage)
                    && storage.TypeStores.TryGetValue(fuel.CargoTypeID, out var fuelStore))
                {
                    double maxFuelKg = fuelStore.MaxVolume / fuel.VolumePerUnit * fuel.MassPerUnit;
                    double dryMass = massVolume.MassTotal - thrust.TotalFuel_kg;
                    if (dryMass > 0 && maxFuelKg > 0)
                        maxDeltaV = thrust.ExhaustVelocity * Math.Log((dryMass + maxFuelKg) / dryMass);
                }
            }

            return new ThrustView(thrust.ThrustInNewtons, thrust.FuelBurnRate, thrust.ExhaustVelocity,
                thrust.DeltaV, maxDeltaV)
            {
                TotalFuelKg = thrust.TotalFuel_kg,
                FuelName = fuelName,
            };
        }

        private static ColonyView ToColonyView(ColonyInfoDB c, Entity colony, int factionId)
        {
            long population = 0;
            var species = new List<SpeciesPopulation>(c.Population.Count);
            foreach (var (speciesId, speciesPop) in c.Population)
            {
                population += speciesPop;
                string name = colony.Manager != null && colony.Manager.TryGetGlobalEntityById(speciesId, out var speciesEntity)
                    ? speciesEntity.GetName(factionId)
                    : "Unknown";
                species.Add(new SpeciesPopulation(name, speciesPop));
            }

            int? planetId = c.PlanetEntity.IsValid ? c.PlanetEntity.Id : null;
            return new ColonyView(population, planetId) { SpeciesPopulations = species };
        }

        private static InstallationsView ToInstallationsView(ComponentInstancesDB ci, Entity entity)
        {
            entity.TryGetDataBlob<CargoStorageDB>(out var storage);

            var groups = new List<InstallationGroup>();
            foreach (var (designId, instances) in ci.ComponentsByDesign)
            {
                if (instances.Count == 0) continue;

                var first = instances[0];
                bool canStore = first.Design.ComponentMountType.HasFlag(ComponentMountType.ShipCargo)
                    && storage != null
                    && storage.TypeStores.ContainsKey(first.CargoTypeID);

                groups.Add(new InstallationGroup(
                    designId,
                    first.Name,
                    first.Design.TemplateName,
                    first.Design.Description,
                    instances.Count,
                    instances.Count(i => i.IsEnabled),
                    canStore));
            }

            return new InstallationsView(groups.OrderBy(g => g.Name).ToList());
        }

        private static FireControlView? ToFireControlView(Entity entity)
        {
            if (!entity.TryGetDataBlob<ComponentInstancesDB>(out var instances)) return null;
            if (!instances.TryGetStates<Pulsar4X.Weapons.FireControlAbilityState>(out List<Pulsar4X.Weapons.FireControlAbilityState> fcStates))
                return null;

            entity.TryGetDataBlob<CargoStorageDB>(out var cargo);
            long StoredUnits(Weapons.OrdnanceDesign design)
                => cargo != null
                   && cargo.TypeStores.TryGetValue(design.CargoTypeID, out var store)
                   && store.CurrentStoreInUnits.TryGetValue(design.ID, out var units)
                    ? units : 0;

            var fireControls = new List<FireControlSnapshot>(fcStates.Count);
            foreach (var fc in fcStates)
            {
                fireControls.Add(new FireControlSnapshot(
                    fc.ComponentInstance.UniqueID,
                    fc.Name,
                    fc.Target != null && fc.Target.IsValid ? fc.Target.Id : null,
                    fc.Target != null && fc.Target.IsValid ? fc.TargetName : null,
                    fc.IsEngaging)
                {
                    AssignedWeaponIds = fc.ChildrenStates.Select(w => w.ID).ToList(),
                });
            }

            var weapons = new List<WeaponSnapshot>();
            if (instances.TryGetStates<Pulsar4X.Weapons.WeaponState>(out List<Pulsar4X.Weapons.WeaponState> weaponStates))
            {
                foreach (var weapon in weaponStates)
                {
                    Weapons.OrdnanceDesign? ordnance = null;
                    weapon.FireWeaponInstructions?.TryGetOrdnance(out ordnance);

                    weapons.Add(new WeaponSnapshot(
                        weapon.ID,
                        weapon.Name,
                        weapon.ParentState?.ID,
                        weapon.InternalMagCurAmount,
                        weapon.ComponentInstance.Design.GetAttribute<Pulsar4X.Weapons.GenericWeaponAtb>().InternalMagSize,
                        ordnance?.UniqueID,
                        ordnance?.Name,
                        ordnance != null ? StoredUnits(ordnance) : 0));
                }
            }

            // Loadable ordnance: the faction's missile designs this entity actually has in cargo.
            var ordnanceItems = new List<OrdnanceStoreItem>();
            if (entity.Manager?.Game.Factions.TryGetValue(entity.FactionOwnerID, out var faction) == true
                && faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo))
            {
                foreach (var design in factionInfo.MissileDesigns.Values)
                {
                    long stored = StoredUnits(design);
                    if (stored > 0)
                        ordnanceItems.Add(new OrdnanceStoreItem(design.UniqueID, design.Name, stored));
                }
            }

            return new FireControlView(fireControls)
            {
                Weapons = weapons,
                Ordnance = ordnanceItems.OrderBy(o => o.Name).ToList(),
            };
        }

        private static CargoStorageView ToCargoStorageView(CargoStorageDB storage, Entity holder)
        {
            bool holderIsColony = holder.HasDataBlob<ColonyInfoDB>();
            bool holderIsShip = holder.HasDataBlob<ShipInfoDB>();
            var factionData = holder.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data;

            var stores = new List<CargoTypeStoreView>(storage.TypeStores.Count);
            foreach (var (typeId, typeStore) in storage.TypeStores)
            {
                var cargoables = typeStore.GetCargoables();
                var items = new List<CargoItemView>(typeStore.CurrentStoreInUnits.Count);
                foreach (var (itemId, units) in typeStore.CurrentStoreInUnits)
                {
                    if (!cargoables.TryGetValue(itemId, out var cargoable)) continue;

                    string kind = "", description = "";
                    bool canInstall = false;
                    switch (cargoable)
                    {
                        case Mineral mineral:
                            kind = "Mineral";
                            description = mineral.Description;
                            break;
                        case ProcessedMaterial material:
                            kind = "Processed Material";
                            description = material.Description;
                            break;
                        case ComponentInstance instance:
                            kind = instance.Design.ComponentType;
                            description = instance.Design.Description;
                            canInstall = (holderIsColony && instance.Design.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation))
                                      || (holderIsShip && instance.Design.ComponentMountType.HasFlag(ComponentMountType.ShipComponent));
                            break;
                        case Pulsar4X.Components.ComponentDesign design:
                            kind = design.ComponentType;
                            description = design.Description;
                            break;
                    }

                    items.Add(new CargoItemView(
                        cargoable.ID,
                        cargoable.Name,
                        kind,
                        description,
                        units,
                        CargoMath.GetUnitCountInEscro(storage, cargoable),
                        storage.GetMassStored(cargoable, true),
                        cargoable.MassPerUnit,
                        storage.GetVolumeStored(cargoable, true),
                        cargoable.VolumePerUnit,
                        storage.GetFreeUnitSpace(cargoable),
                        canInstall));
                }

                string typeName = factionData.CargoTypes.TryGetValue(typeId, out var cargoType) ? cargoType.Name : typeId;
                stores.Add(new CargoTypeStoreView(typeId, typeName, typeStore.MaxVolume, storage.GetFreeVolume(typeId))
                {
                    Items = items.OrderBy(i => i.Name).ToList(),
                });
            }

            return new CargoStorageView(storage.TotalStoredMass, storage.TransferRate, storage.TransferRangeDv_mps)
            {
                Stores = stores,
            };
        }

        private static ColonyMiningView? ToColonyMiningView(ColonyInfoDB colonyInfo, Entity colony, int factionId)
        {
            colony.TryGetDataBlob<MiningDB>(out var mining);

            if (!colonyInfo.PlanetEntity.IsValid
                || !colonyInfo.PlanetEntity.TryGetDataBlob<MineralsDB>(out var deposits))
            {
                return mining == null ? null : new ColonyMiningView(mining.NumberOfMines);
            }

            var game = colony.Manager?.Game;
            if (game == null || !game.Factions.TryGetValue(factionId, out var faction)) return null;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo)) return null;

            int factionMask = factionInfo.FactionMask;
            var mineralsById = factionInfo.Data.CargoGoods.GetMineralsList().ToDictionary(m => m.ID);
            colony.TryGetDataBlob<CargoStorageDB>(out var storage);

            var rows = new List<MineralMiningRow>(deposits.Minerals.Count);
            foreach (var (mineralId, deposit) in deposits.Minerals)
            {
                if (!mineralsById.TryGetValue(mineralId, out var mineral)) continue;

                long? stockpile = null;
                if (storage != null)
                {
                    var store = storage.TypeStores.Values.FirstOrDefault(s => s.CurrentStoreInUnits.ContainsKey(mineralId));
                    stockpile = store?.CurrentStoreInUnits[mineralId] ?? 0;
                }

                bool canMine = mining != null && mining.ActualMiningRate.ContainsKey(mineralId);
                long annualProduction = canMine ? 365 * mining!.ActualMiningRate[mineralId] : 0;

                rows.Add(new MineralMiningRow(
                    mineralId,
                    mineral.Name,
                    mineral.Description,
                    stockpile,
                    deposit.Amount.For(factionMask),
                    deposit.Accessibility,
                    annualProduction,
                    canMine));
            }

            return new ColonyMiningView(mining?.NumberOfMines ?? 0)
            {
                Minerals = rows.OrderBy(r => r.Name).ToList(),
            };
        }

        private static NavalAcademyView ToNavalAcademyView(NavalAcademyDB na)
        {
            var academies = new List<NavalAcademyClassView>(na.Academies.Count);
            foreach (var academy in na.Academies)
                academies.Add(new NavalAcademyClassView(academy.ClassSize, academy.TrainingPeriodInMonths, academy.GraduationDate));
            return new NavalAcademyView(academies);
        }

        // ----- industry / local construction -----

        private static IndustryView? ToIndustryView(IndustryAbilityDB industry, Entity entity, int factionId)
        {
            var game = entity.Manager?.Game;
            if (game == null || !game.Factions.TryGetValue(factionId, out var faction)) return null;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo)) return null;

            entity.TryGetDataBlob<CargoStorageDB>(out var storage);

            var sortedDesigns = factionInfo.IndustryDesigns.Values
                .Where(d => d.IsValid)
                .OrderBy(d => d.Name)
                .ToList();

            var lines = new List<ProductionLineView>(industry.ProductionLines.Count);
            foreach (var (lineId, line) in industry.ProductionLines)
            {
                var jobs = new List<IndustryJobView>(line.Jobs.Count);
                foreach (var job in line.Jobs)
                {
                    var requirements = new List<ResourceRequirement>(job.ResourcesRequiredRemaining.Count);
                    foreach (var (resourceId, amount) in job.ResourcesRequiredRemaining)
                        requirements.Add(new ResourceRequirement(ResolveItemName(factionInfo, resourceId), amount));

                    double percent = (1 - (double)job.ProductionPointsLeft / job.ProductionPointsCost) * 100;
                    jobs.Add(new IndustryJobView(
                        job.JobID, job.Name, job.NumberCompleted, job.NumberOrdered, job.Auto,
                        job.Status.ToString(), job.Status == IndustryJobStatus.MissingResources,
                        percent, job.ProductionPointsLeft)
                    {
                        RemainingRequirements = requirements,
                    });
                }

                // The line spends its output on the head job's industry type.
                double currentRate = 0;
                if (line.Jobs.Count > 0
                    && factionInfo.IndustryDesigns.TryGetValue(line.Jobs[0].ItemGuid, out var headDesign)
                    && line.IndustryTypeRates.TryGetValue(headDesign.IndustryTypeID, out var rate))
                {
                    currentRate = rate;
                }

                var constructibles = new List<ConstructibleItemView>();
                foreach (var design in sortedDesigns)
                {
                    if (!line.IndustryTypeRates.ContainsKey(design.IndustryTypeID)) continue;

                    var costs = new List<IndustryCostItem>(design.ResourceCosts.Count);
                    foreach (var (resourceId, perUnit) in design.ResourceCosts)
                    {
                        long available = 0;
                        if (storage != null && TryResolveCargoable(factionInfo, resourceId, out var cargoable))
                            available = storage.GetUnitsStored(cargoable, false);

                        bool canProduce = factionInfo.IndustryDesigns.ContainsKey(resourceId)
                            || factionInfo.Data.CargoGoods.IsMineral(resourceId);

                        costs.Add(new IndustryCostItem(ResolveItemName(factionInfo, resourceId), perUnit, available, canProduce));
                    }

                    constructibles.Add(new ConstructibleItemView(
                        design.UniqueID, design.Name, design.IndustryPointCosts, design.OutputAmount,
                        design.GuiHints == ConstructableGuiHints.CanBeInstalled)
                    {
                        Costs = costs,
                    });
                }

                lines.Add(new ProductionLineView(lineId, line.Name, currentRate)
                {
                    Jobs = jobs,
                    Constructibles = constructibles,
                });
            }

            return new IndustryView(lines);
        }

        private static ConstructionView? ToConstructionView(LocalConstructionDB construction, Entity entity, int factionId)
        {
            var game = entity.Manager?.Game;
            if (game == null || !game.Factions.TryGetValue(factionId, out var faction)) return null;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo)) return null;

            var queue = new List<ConstructionJobView>(construction.BuildQueue.Count);
            foreach (var job in construction.BuildQueue)
            {
                queue.Add(new ConstructionJobView(
                    job.Design.Name, job.Design.ComponentType, job.Design.IndustryPointCosts,
                    job.PointsAccumulated, job.CurrentItemProgress));
            }

            var designs = factionInfo.ComponentDesigns.Values
                .Where(d => d.IsValid && d.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation))
                .OrderBy(d => d.Name)
                .Select(d => new ConstructibleDesignView(d.UniqueID, d.Name, d.ComponentType, d.IndustryPointCosts))
                .ToList();

            return new ConstructionView(construction.PointsPerDay)
            {
                BuildQueue = queue,
                AvailableDesigns = designs,
            };
        }

        /// <summary>Resolves an industry resource id (cargo good or component design) to a display name.</summary>
        private static string ResolveItemName(FactionInfoDB factionInfo, string resourceId)
        {
            if (factionInfo.ComponentDesigns.TryGetValue(resourceId, out var design)) return design.Name;
            string name = factionInfo.Data.GetName(resourceId);
            return name.Length > 0 ? name : resourceId;
        }

        private static bool TryResolveCargoable(FactionInfoDB factionInfo, string resourceId, out ICargoable cargoable)
        {
            if (factionInfo.Data.CargoGoods.Contains(resourceId))
            {
                cargoable = factionInfo.Data.CargoGoods.GetAny(resourceId)!;
                return true;
            }
            if (factionInfo.ComponentDesigns.TryGetValue(resourceId, out var design))
            {
                cargoable = design;
                return true;
            }
            cargoable = null!;
            return false;
        }

        private static ResearcherView ToResearcherView(ResearcherDB r, Entity lab, int factionId)
        {
            string templateName = "", description = "";
            if (r.Design is Pulsar4X.Components.ComponentDesign design)
            {
                templateName = design.TemplateName;
                description = design.Description;
            }

            string locationName = "";
            if (lab.Manager != null && lab.Manager.TryGetEntityById(r.LocationId, out var location))
                locationName = location.GetName(factionId);

            string? scientistName = null;
            if (r.ScientistId >= 0 && lab.Manager != null
                && lab.Manager.TryGetGlobalEntityById(r.ScientistId, out var scientist))
            {
                scientistName = scientist.GetName(factionId);
            }

            return new ResearcherView(
                r.Design.Name,
                templateName,
                description,
                r.LocationId,
                locationName,
                r.ScientistId >= 0 ? r.ScientistId : null,
                scientistName,
                ToModifiedValue(r.CostPerDay),
                ToModifiedValue(r.PointsPerDay),
                r.FundingLevel,
                new List<string>(r.TechQueue));
        }

        private static ModifiedValue ToModifiedValue<T>(ModifiableValue<T> value) where T : IConvertible
        {
            var modifiers = new List<ValueModifier>();
            foreach (var modifier in value.GetModifiers())
                modifiers.Add(new ValueModifier(modifier.Name,
                    Convert.ToDouble(modifier.After) - Convert.ToDouble(modifier.Before)));

            return new ModifiedValue(Convert.ToDouble(value.GetValue()), Convert.ToDouble(value.BaseValue), modifiers);
        }

        // ----- research -----

        /// <summary>The faction's research state: tech categories, every unlocked tech with progress
        /// and researchability, and the faction's scientists (for assignment UIs).</summary>
        public ResearchSnapshot? ProjectResearch(int factionId)
        {
            if (!_game.Factions.TryGetValue(factionId, out var faction)) return null;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info)) return null;
            var data = info.Data;

            var categories = _game.TechCategories.Values
                .Select(c => new TechCategorySnapshot(c.UniqueID, c.Name))
                .OrderBy(c => c.Name)
                .ToList();

            var techs = new List<TechSnapshot>(data.Techs.Count);
            foreach (var tech in data.Techs.Values)
            {
                var unlocks = new List<string>();
                if (tech.Unlocks.TryGetValue(tech.Level + 1, out var unlockIds))
                    foreach (var unlockId in unlockIds)
                        unlocks.Add(data.GetName(unlockId));

                string categoryName = _game.TechCategories.TryGetValue(tech.Category, out var category)
                    ? category.Name
                    : "";

                techs.Add(new TechSnapshot(
                    tech.UniqueID, tech.Name, tech.DisplayName(), tech.MaxLevelName(), tech.Description,
                    tech.Category, categoryName, tech.Level, tech.MaxLevel,
                    tech.ResearchCost, tech.ResearchProgress, data.IsResearchable(tech.UniqueID))
                {
                    NextLevelUnlocks = unlocks,
                });
            }

            var scientists = new List<CommanderSnapshot>();
            foreach (var commander in info.Commanders)
            {
                if (!commander.TryGetDataBlob<CommanderDB>(out var commanderDB)
                    || commanderDB.Type != DataStructures.CommanderTypes.Scientist)
                    continue;
                scientists.Add(ProjectCommander(commander, commanderDB, data, factionId));
            }

            return new ResearchSnapshot { Categories = categories, Techs = techs, Scientists = scientists };
        }

        // ----- commanders -----

        /// <summary>Everyone in the faction's service, for the personnel roster. Faction-scoped:
        /// only the faction's own commanders are ever projected.</summary>
        public IReadOnlyList<CommanderSnapshot>? ProjectCommanders(int factionId)
        {
            if (!_game.Factions.TryGetValue(factionId, out var faction)) return null;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info)) return null;

            var shipCommands = MapShipCommands(faction, factionId);
            var commanders = new List<CommanderSnapshot>(info.Commanders.Count);
            foreach (var commander in info.Commanders)
            {
                if (!commander.IsValid || !commander.TryGetDataBlob<CommanderDB>(out var commanderDB))
                    continue;
                commanders.Add(ProjectCommander(commander, commanderDB, info.Data, factionId, shipCommands));
            }
            return commanders;
        }

        // Ship command isn't recorded on the commander (only lab/post assignments set AssignedTo), so
        // reverse-map it from the faction's fleet tree: commander id → commanded ship's name.
        private static Dictionary<int, string> MapShipCommands(Entity faction, int factionId)
        {
            var map = new Dictionary<int, string>();
            if (faction.TryGetDataBlob<FleetDB>(out var factionFleet) && factionFleet.RootDB?.Children is { } roots)
                foreach (var child in roots)
                    AddShipCommands(child, factionId, map);
            return map;
        }

        private static void AddShipCommands(Entity node, int factionId, Dictionary<int, string> map)
        {
            if (node.TryGetDataBlob<ShipInfoDB>(out var shipInfo) && shipInfo.CommanderID >= 0)
                map[shipInfo.CommanderID] = node.GetName(factionId);
            if (node.TryGetDataBlob<FleetDB>(out var fleetDB))
                foreach (var child in fleetDB.GetChildren())
                    AddShipCommands(child, factionId, map);
        }

        private CommanderSnapshot ProjectCommander(Entity commander, CommanderDB commanderDB,
            FactionDataStore data, int factionId, IReadOnlyDictionary<int, string>? shipCommands = null)
        {
            var bonuses = new List<CommanderBonusSnapshot>();
            if (commander.TryGetDataBlob<BonusesDB>(out var bonusesDB))
            {
                foreach (var bonus in bonusesDB.Bonuses)
                {
                    // Resolve the bonus's filter target to a display name: faction data first, then
                    // tech categories, then the raw id as a last resort.
                    string? filterName = null;
                    if (!string.IsNullOrEmpty(bonus.FilterId))
                    {
                        filterName = data.GetName(bonus.FilterId);
                        if (string.IsNullOrEmpty(filterName) && _game.TechCategories.TryGetValue(bonus.FilterId, out var category))
                            filterName = category.Name;
                        if (string.IsNullOrEmpty(filterName))
                            filterName = bonus.FilterId;
                    }

                    bonuses.Add(new CommanderBonusSnapshot(
                        bonus.Name, bonus.Value, bonus.Type == BonusType.Perentage, filterName));
                }
            }

            // A lab/post assignment is recorded on the commander; a ship command comes from the
            // reverse map (when the caller supplied one). Either way the name is resolved here so
            // the client never has to look the posting up.
            string? assignment = null;
            if (commanderDB.AssignedTo >= 0 && commander.Manager != null
                && commander.Manager.TryGetGlobalEntityById(commanderDB.AssignedTo, out var post))
            {
                assignment = post.GetName(factionId);
            }
            else if (shipCommands != null && shipCommands.TryGetValue(commander.Id, out var shipName))
            {
                assignment = shipName;
            }

            // Only the navy track has theme rank titles today.
            string? rankName = null;
            if (commanderDB.Type == DataStructures.CommanderTypes.Navy
                && _game.Themes.TryGetValue(_game.Settings.CurrentTheme, out var theme)
                && theme.NavyRanks != null
                && theme.NavyRanks.TryGetValue(commanderDB.Rank, out var title))
            {
                rankName = title;
            }

            return new CommanderSnapshot(
                commander.Id,
                commander.GetName(factionId),
                ToCommanderKind(commanderDB.Type),
                commanderDB.AssignedTo >= 0 || assignment != null,
                commanderDB.Experience,
                commanderDB.ExperienceCap,
                commanderDB.CommissionedOn)
            {
                Bonuses = bonuses,
                Rank = commanderDB.Rank,
                RankName = rankName,
                RankedOn = commanderDB.RankedOn,
                AssignmentName = assignment,
            };
        }

        private static CommanderKind ToCommanderKind(DataStructures.CommanderTypes type) => type switch
        {
            DataStructures.CommanderTypes.Navy => CommanderKind.Navy,
            DataStructures.CommanderTypes.Ground => CommanderKind.Ground,
            DataStructures.CommanderTypes.Scientist => CommanderKind.Scientist,
            _ => CommanderKind.Civilian,
        };

        // ----- component design -----

        /// <summary>The faction's component-design surface: its unlocked templates and the designs it
        /// has created (each carrying the inputs it was built with, so the client can reload it).</summary>
        public ComponentDesignsSnapshot? ProjectComponentDesigns(int factionId)
        {
            if (!_game.Factions.TryGetValue(factionId, out var faction)) return null;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info)) return null;

            var templates = info.Data.ComponentTemplates.Values
                .Select(t => new ComponentTemplateSummary(t.UniqueID, t.Name, t.ComponentType ?? "", TemplateDescription(t)))
                .OrderBy(t => t.Name)
                .ToList();

            var designs = new List<ComponentDesignSummary>(info.ComponentDesigns.Count);
            foreach (var design in info.ComponentDesigns.Values)
            {
                var values = new List<DesignerInput>(design.TemplatePropertyValues.Count);
                foreach (var (propName, _, propValue) in design.TemplatePropertyValues)
                {
                    values.Add(propValue switch
                    {
                        int i => new DesignerInput(propName, NumericValue: i),
                        float f => new DesignerInput(propName, NumericValue: f),
                        double d => new DesignerInput(propName, NumericValue: d),
                        _ => new DesignerInput(propName, StringValue: propValue?.ToString() ?? ""),
                    });
                }

                designs.Add(new ComponentDesignSummary(design.UniqueID, design.Name, design.TemplateID, design.TemplateName)
                {
                    PropertyValues = values,
                });
            }

            return new ComponentDesignsSnapshot { Templates = templates, Designs = designs };
        }

        // The "Description" formula is usually a quoted string literal; unwrap it rather than
        // evaluating (a full evaluation needs a designer instance per template).
        private static string TemplateDescription(ComponentTemplateBlueprint template)
        {
            if (!template.Formulas.TryGetValue("Description", out var formula) || string.IsNullOrEmpty(formula))
                return "";

            return formula.Length > 1 && formula[0] == '\'' && formula[^1] == '\''
                ? formula[1..^1]
                : formula;
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
                StandingOrders = ProjectStandingOrders(fleetDB),
                SubFleets = subFleets,
                Ships = ships,
            };
        }

        // The condition/action registries are engine code, so the type ids are part of the API
        // contract (StandingOrderTypes); only registry types can exist, the UI being their sole creator.
        private static IReadOnlyList<Pulsar4X.Api.StandingOrder> ProjectStandingOrders(FleetDB? fleetDB)
        {
            if (fleetDB == null || fleetDB.StandingOrders.Count == 0)
                return Array.Empty<Pulsar4X.Api.StandingOrder>();

            var orders = new List<Pulsar4X.Api.StandingOrder>(fleetDB.StandingOrders.Count);
            foreach (var order in fleetDB.StandingOrders)
            {
                var conditions = new List<StandingOrderCondition>();
                var items = order.Condition?.ConditionItems;
                for (int i = 0; items != null && i < items.Count; i++)
                {
                    if (items[i].Condition is not Engine.Orders.ComparisonCondition comparison)
                        continue;

                    string? conditionType = comparison switch
                    {
                        Engine.Orders.FuelCondition => StandingOrderTypes.FuelCondition,
                        _ => null,
                    };
                    if (conditionType == null)
                        continue;

                    conditions.Add(new StandingOrderCondition(
                        conditionType,
                        ToStandingOrderComparison(comparison.ComparisionType),
                        comparison.Threshold,
                        items[i].LogicalOperation switch
                        {
                            DataStructures.LogicalOperation.And => StandingOrderLogic.And,
                            DataStructures.LogicalOperation.Or => StandingOrderLogic.Or,
                            _ => null,
                        }));
                }

                var actions = new List<string>();
                foreach (var action in order.Actions)
                {
                    string? actionType = action switch
                    {
                        Pulsar4X.Movement.MoveToNearestColonyAction => StandingOrderTypes.MoveToNearestColony,
                        Pulsar4X.Movement.MoveToNearestGeoSurveyAction => StandingOrderTypes.MoveToNearestGeoSurvey,
                        Pulsar4X.Movement.MoveToNearestAnomalyAction => StandingOrderTypes.MoveToNearestAnomaly,
                        Pulsar4X.Fleets.RefuelAction => StandingOrderTypes.Refuel,
                        Pulsar4X.Fleets.ResupplyAction => StandingOrderTypes.Resupply,
                        _ => null,
                    };
                    if (actionType != null)
                        actions.Add(actionType);
                }

                orders.Add(new Pulsar4X.Api.StandingOrder(order.Name ?? "", conditions, actions));
            }
            return orders;
        }

        private static StandingOrderComparison ToStandingOrderComparison(DataStructures.ComparisonType comparison)
            => comparison switch
            {
                DataStructures.ComparisonType.LessThan => StandingOrderComparison.LessThan,
                DataStructures.ComparisonType.LessThanOrEqual => StandingOrderComparison.LessThanOrEqual,
                DataStructures.ComparisonType.EqualTo => StandingOrderComparison.EqualTo,
                DataStructures.ComparisonType.GreaterThan => StandingOrderComparison.GreaterThan,
                _ => StandingOrderComparison.GreaterThanOrEqual,
            };

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
            {
                var maneuver = !action.IsRunning ? action as Pulsar4X.Movement.NewtonThrustCommand : null;
                orders.Add(new OrderSnapshot(action.Name, action.IsRunning, action.GetIsFinished, action.Details,
                    maneuver != null)
                {
                    OrderId = action.CmdID,
                    IsBlocking = action.IsBlocking,
                    UsesMovementLane = action.ActionLanes.HasFlag(EntityCommand.ActionLaneTypes.Movement),
                    UsesExternalLane = action.ActionLanes.HasFlag(EntityCommand.ActionLaneTypes.InteractWithExternalEntity),
                    UsesSelfLane = action.ActionLanes.HasFlag(EntityCommand.ActionLaneTypes.IneteractWithSelf),
                    PauseOnAction = action.PauseOnAction,
                    ManeuverNodeTime = maneuver?.NodeDateTime,
                    ManeuverDeltaVMps = maneuver != null ? ToVec3(maneuver.OrbitrelativeDeltaV) : null,
                });
            }
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
