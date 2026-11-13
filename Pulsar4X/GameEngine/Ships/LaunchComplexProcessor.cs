using System;
using System.Linq;
using System.Collections.Generic;
using Pulsar4X.Colonies;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.Storage;

namespace Pulsar4X.Ships
{
    public class LaunchComplexProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromDays(1);
        public TimeSpan FirstRunOffset => TimeSpan.FromHours(4);
        public Type GetParameterType => typeof(LaunchComplexDB);

        public void Init(Game game) { }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            if (!entity.TryGetDataBlob<LaunchComplexDB>(out var launchDB))
                return;

            AssignQueueToPads(launchDB, entity);
            ProcessPadLaunches(launchDB, entity);
            LaunchYardHulls(launchDB, entity);
        }

        public const string NoFreePad = "No free pad";
        /// <summary>The hold reason's prefix when the pad's launch fuel is short.</summary>
        public const string NotEnoughFuel = "Not enough";

        /// <summary>"Not enough RP-1": the hold reason naming the pad's launch fuel.</summary>
        public static string NotEnoughOf(string fuelName) => $"{NotEnoughFuel} {fuelName}";
        public const string OverTonnage = "Tonnage over the pad";

        /// <summary>
        /// Why a hull is still on the yard. Empty when a free pad can lift it and the colony has the fuel.
        /// </summary>
        public static string HoldReason(Entity colony, long mass)
        {
            if (!colony.TryGetDataBlob<LaunchComplexDB>(out var launch) || launch.Pads.Count == 0)
                return NoFreePad;

            bool anyFits = false;
            LaunchPad? firstFree = null;
            foreach (var pad in launch.Pads.Values)
            {
                if (mass > pad.MaxTonnage)
                    continue;
                anyFits = true;
                if (pad.ShipDesignId != null)
                    continue;
                if (HasFuel(colony, mass, pad))
                    return "";
                firstFree ??= pad;
            }

            if (!anyFits)
                return OverTonnage;
            if (firstFree == null)
                return NoFreePad;
            return NotEnoughOf(FuelName(colony, firstFree.LaunchFuel));
        }

        /// <summary>
        /// Why a pad holding a ship has not launched it: empty when it can go, else the pad's
        /// launch fuel is short.
        /// </summary>
        public static string PadHoldReason(Entity colony, LaunchPad pad)
        {
            if (pad.ShipDesignId == null)
                return "";
            return HasFuel(colony, pad.ShipMass, pad, pad.TargetOrbitRadius) ? "" : NotEnoughOf(FuelName(colony, pad.LaunchFuel));
        }

        /// <summary>The cargo's display name, or its id when the faction does not know it.</summary>
        public static string FuelName(Entity colony, string fuelId)
        {
            if (!colony.GetFactionOwner.TryGetDataBlob<FactionInfoDB>(out var info))
                return fuelId;
            return (info.Data.CargoGoods.GetAny(fuelId) ?? info.Data.LockedCargoGoods.GetAny(fuelId))?.Name ?? fuelId;
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var entities = manager.GetAllEntitiesWithDataBlob<LaunchComplexDB>();
            foreach (var entity in entities)
            {
                ProcessEntity(entity, deltaSeconds);
            }
            return entities.Count;
        }

        private static void AssignQueueToPads(LaunchComplexDB launchDB, Entity colonyEntity)
        {
            foreach (var kvp in launchDB.Pads)
            {
                var pad = kvp.Value;
                if (pad.ShipDesignId != null)
                    continue;

                for (int i = 0; i < launchDB.LaunchQueue.Count; i++)
                {
                    var entry = launchDB.LaunchQueue[i];

                    var factionInfo = colonyEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>();
                    if (!factionInfo.IndustryDesigns.TryGetValue(entry.DesignId, out var designInfo))
                        continue;

                    var shipDesign = (ShipDesign)designInfo;
                    if (shipDesign.MassPerUnit > pad.MaxTonnage)
                        continue;

                    pad.ShipDesignId = entry.DesignId;
                    pad.ShipName = entry.ShipName;
                    pad.ShipMass = shipDesign.MassPerUnit;
                    pad.TargetOrbitRadius = 0;
                    pad.ReadyToLaunch = true;
                    launchDB.LaunchQueue.RemoveAt(i);
                    break;
                }
            }
        }

        private static void ProcessPadLaunches(LaunchComplexDB launchDB, Entity colonyEntity)
        {
            foreach (var kvp in launchDB.Pads.ToArray())
            {
                var pad = kvp.Value;
                if (pad.ShipDesignId == null || !pad.ReadyToLaunch)
                    continue;

                TryLaunchShip(colonyEntity, kvp.Key);
            }
        }

        public static bool TryLaunchShip(Entity colonyEntity, string padId)
        {
            if (!colonyEntity.TryGetDataBlob<LaunchComplexDB>(out var launchDB))
                return false;

            if (!launchDB.Pads.TryGetValue(padId, out var pad))
                return false;

            if (pad.ShipDesignId == null)
                return false;

            if (!colonyEntity.TryGetDataBlob<ColonyInfoDB>(out var colonyInfo))
                return false;

            var planet = colonyInfo.PlanetEntity;
            var faction = colonyEntity.GetFactionOwner;
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();

            if (!factionInfo.IndustryDesigns.TryGetValue(pad.ShipDesignId, out var designInfo))
                return false;

            var shipDesign = (ShipDesign)designInfo;

            var targetRadius = pad.TargetOrbitRadius > 0
                ? pad.TargetOrbitRadius
                : OrbitMath.LowOrbitRadius(planet);

            double fuelCost = OrbitMath.FuelCostToOrbit(planet, shipDesign.MassPerUnit, targetRadius);

            if (!TryDeductFuel(colonyEntity, pad.LaunchFuel, fuelCost))
                return false;

            var position = new Vector3(targetRadius, 0, 0);
            var ship = ShipFactory.CreateShip(shipDesign, faction, position, planet, pad.ShipName);
            // Crew plan phase 3: crew come from the colony's pool (an NPC's population); short launches under-crewed.
            ShipCrew.BoardAtLaunch(ship, colonyEntity, colonyEntity.StarSysDateTime);
            // Ships that exist at the new game are started by PostNewGameInitialization.
            // A later launch is not, so the battery update never gets its first appointment.
            if (ship.HasDataBlob<EnergyGenAbilityDB>())
                EnergyGenProcessor.EnergyGen(ship, ship.Manager.ManagerSubpulses.NextSafeInterruptTime);

            if (faction.TryGetDataBlob<FleetDB>(out var fleetDB))
            {
                fleetDB.AddChild(ship);
            }

            pad.ShipDesignId = null;
            pad.ShipName = null;
            pad.ShipMass = 0;
            pad.TargetOrbitRadius = 0;
            pad.ReadyToLaunch = false;

            return true;
        }

        /// <summary>
        /// Takes the pad's launch fuel (picked in its launch complex design) and no other. Nothing is
        /// taken unless the whole amount is there. If a design picks a power plant fuel, a launch
        /// only gets what sits above the plants' runway reserve
        /// (<see cref="ColonyPowerProcessor.FuelReserveUnits"/>, the hold the market and the
        /// refinery planner also keep).
        /// </summary>
        private static bool TryDeductFuel(Entity colonyEntity, string fuelId, double fuelMassNeeded)
        {
            if (!colonyEntity.TryGetDataBlob<CargoStorageDB>(out var storage))
                return false;
            var (fuel, available) = LaunchFuelAvailable(colonyEntity, storage, fuelId);
            if (fuel == null || available + 1e-9 < fuelMassNeeded)
                return false;
            if (fuelMassNeeded > 0)
                CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, fuel, -fuelMassNeeded);
            return true;
        }

        /// <summary>
        /// The launch fuel in the colony's fuel store and the mass a launch may burn: all of it for a
        /// plain propellant, only what is above the runway reserve for a power plant fuel.
        /// </summary>
        static (ICargoable? Fuel, double Mass) LaunchFuelAvailable(Entity colony, CargoStorageDB storage, string fuelId)
        {
            if (!storage.TypeStores.TryGetValue("fuel-storage", out var fuelStore))
                return (null, 0);
            var fuel = fuelStore.Cargoables.Values.FirstOrDefault(c => c.UniqueID == fuelId);
            if (fuel == null)
                return (null, 0);
            double stored = storage.GetMassStored(fuel, false);
            long reserveUnits = ColonyPowerProcessor.FuelReserveUnits(colony, fuelId);
            return (fuel, Math.Max(0, stored - reserveUnits * fuel.MassPerUnit));
        }

        /// <summary>
        /// Each free pad launches at most one yard hull this tick. A hull that fits but cannot launch
        /// stays on the yard and blocks that pad. A hull over the pad's tonnage is skipped.
        /// </summary>
        static void LaunchYardHulls(LaunchComplexDB launchDB, Entity colony)
        {
            if (!colony.TryGetDataBlob<IndustryAbilityDB>(out var industry))
                return;

            var waiting = new List<(IndustryAbilityDB.ProductionLine Line, IndustryAbilityDB.YardHull Hull)>();
            foreach (var line in industry.ProductionLines.Values)
            {
                if (!line.IndustryTypeRates.ContainsKey("ship-assembly"))
                    continue;
                foreach (var hull in line.Hulls)
                    waiting.Add((line, hull));
            }

            var launched = new HashSet<IndustryAbilityDB.YardHull>();
            foreach (var pad in launchDB.Pads.Values)
            {
                if (pad.ShipDesignId != null)
                    continue;

                foreach (var (line, hull) in waiting)
                {
                    if (launched.Contains(hull))
                        continue;
                    if (hull.Mass > pad.MaxTonnage)
                        continue;

                    if (TryLaunchHull(colony, hull, pad))
                    {
                        line.Hulls.Remove(hull);
                        launched.Add(hull);
                    }
                    break;
                }
            }
        }

        static bool TryLaunchHull(Entity colony, IndustryAbilityDB.YardHull hull, LaunchPad pad)
        {
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var colonyInfo) || colonyInfo.PlanetEntity == null)
                return false;
            var planet = colonyInfo.PlanetEntity;
            if (!planet.HasDataBlob<MassVolumeDB>() || !planet.HasDataBlob<PositionDB>())
                return false;

            var faction = colony.GetFactionOwner;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo))
                return false;
            if (!factionInfo.IndustryDesigns.TryGetValue(hull.DesignId, out var designInfo) || designInfo is not ShipDesign shipDesign)
                return false;
            if (!TryFuelCost(colony, hull.Mass, 0, out double fuelCost))
                return false;
            if (!TryDeductFuel(colony, pad.LaunchFuel, fuelCost))
                return false;

            var position = new Vector3(OrbitMath.LowOrbitRadius(planet), 0, 0);
            var ship = ShipFactory.CreateShip(shipDesign, faction, position, planet, hull.Name);
            ShipCrew.BoardAtLaunch(ship, colony, colony.StarSysDateTime);
            if (ship.HasDataBlob<EnergyGenAbilityDB>())
                EnergyGenProcessor.EnergyGen(ship, ship.Manager.ManagerSubpulses.NextSafeInterruptTime);
            if (faction.TryGetDataBlob<FleetDB>(out var fleetDB))
                fleetDB.AddChild(ship);
            return true;
        }

        static bool HasFuel(Entity colony, long mass, LaunchPad pad, double targetRadius = 0)
        {
            if (!TryFuelCost(colony, mass, targetRadius, out double cost))
                return false;
            if (!colony.TryGetDataBlob<CargoStorageDB>(out var storage))
                return false;
            var (fuel, available) = LaunchFuelAvailable(colony, storage, pad.LaunchFuel);
            return fuel != null && available + 1e-9 >= cost;
        }

        /// <summary>Fuel to lift this mass to the target radius, low orbit when 0.</summary>
        static bool TryFuelCost(Entity colony, long mass, double targetRadius, out double cost)
        {
            cost = 0;
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info) || info.PlanetEntity == null)
                return false;
            if (!info.PlanetEntity.TryGetDataBlob<MassVolumeDB>(out _))
                return false;
            double radius = targetRadius > 0 ? targetRadius : OrbitMath.LowOrbitRadius(info.PlanetEntity);
            cost = OrbitMath.FuelCostToOrbit(info.PlanetEntity, mass, radius);
            return true;
        }
    }
}
