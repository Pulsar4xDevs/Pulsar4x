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
        public const string NotEnoughFuel = "Not enough fuel";
        public const string OverTonnage = "Tonnage over the pad";

        /// <summary>
        /// Why a hull is still on the yard. Empty when a free pad can lift it and the colony has the fuel.
        /// </summary>
        public static string HoldReason(Entity colony, long mass)
        {
            if (!colony.TryGetDataBlob<LaunchComplexDB>(out var launch) || launch.Pads.Count == 0)
                return NoFreePad;

            bool anyFits = false;
            bool anyFreeFits = false;
            foreach (var pad in launch.Pads.Values)
            {
                if (mass > pad.MaxTonnage)
                    continue;
                anyFits = true;
                if (pad.ShipDesignId == null)
                    anyFreeFits = true;
            }

            if (!anyFits)
                return OverTonnage;
            if (!anyFreeFits)
                return NoFreePad;
            if (!HasFuel(colony, mass))
                return NotEnoughFuel;
            return "";
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

            if (!TryDeductFuel(colonyEntity, fuelCost))
                return false;

            var position = new Vector3(targetRadius, 0, 0);
            var ship = ShipFactory.CreateShip(shipDesign, faction, position, planet, pad.ShipName);
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

        private static bool TryDeductFuel(Entity colonyEntity, double fuelMassNeeded)
        {
            if (!colonyEntity.TryGetDataBlob<CargoStorageDB>(out var storage))
                return false;

            if (!storage.TypeStores.ContainsKey("fuel-storage"))
                return false;

            var fuelStore = storage.TypeStores["fuel-storage"];
            var cargoLib = colonyEntity.GetFactionCargoDefinitions();
            if (cargoLib == null)
                return false;

            double remaining = fuelMassNeeded;

            foreach (var kvp in fuelStore.Cargoables.ToArray())
            {
                if (remaining <= 0)
                    break;

                var fuelItem = kvp.Value;
                double storedMass = storage.GetMassStored(fuelItem, false);
                if (storedMass <= 0)
                    continue;

                double toDeduct = Math.Min(remaining, storedMass);
                CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, fuelItem, -toDeduct);
                remaining -= toDeduct;
            }

            return remaining <= 0;
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

                    if (TryLaunchHull(colony, hull))
                    {
                        line.Hulls.Remove(hull);
                        launched.Add(hull);
                    }
                    break;
                }
            }
        }

        static bool TryLaunchHull(Entity colony, IndustryAbilityDB.YardHull hull)
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
            if (!HasFuel(colony, hull.Mass) || !TryFuelCost(colony, hull.Mass, out double fuelCost))
                return false;
            if (!TryDeductFuel(colony, fuelCost))
                return false;

            var position = new Vector3(OrbitMath.LowOrbitRadius(planet), 0, 0);
            var ship = ShipFactory.CreateShip(shipDesign, faction, position, planet, hull.Name);
            if (ship.HasDataBlob<EnergyGenAbilityDB>())
                EnergyGenProcessor.EnergyGen(ship, ship.Manager.ManagerSubpulses.NextSafeInterruptTime);
            if (faction.TryGetDataBlob<FleetDB>(out var fleetDB))
                fleetDB.AddChild(ship);
            return true;
        }

        static bool HasFuel(Entity colony, long mass)
        {
            if (!TryFuelCost(colony, mass, out double cost))
                return false;
            return FuelOnHand(colony) + 1e-9 >= cost;
        }

        static bool TryFuelCost(Entity colony, long mass, out double cost)
        {
            cost = 0;
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info) || info.PlanetEntity == null)
                return false;
            if (!info.PlanetEntity.TryGetDataBlob<MassVolumeDB>(out _))
                return false;
            cost = OrbitMath.FuelCostToOrbit(info.PlanetEntity, mass, OrbitMath.LowOrbitRadius(info.PlanetEntity));
            return true;
        }

        static double FuelOnHand(Entity colony)
        {
            if (!colony.TryGetDataBlob<CargoStorageDB>(out var storage))
                return 0;
            if (!storage.TypeStores.ContainsKey("fuel-storage"))
                return 0;

            double mass = 0;
            foreach (var item in storage.TypeStores["fuel-storage"].GetCargoables().Values)
                mass += storage.GetMassStored(item, false);
            return mass;
        }
    }
}
