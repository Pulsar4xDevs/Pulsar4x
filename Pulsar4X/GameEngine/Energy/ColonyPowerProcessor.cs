using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Interfaces;
using Pulsar4X.Movement;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Energy
{
    /// <summary>
    /// Recomputes a colony's <see cref="ColonyPowerDB"/> from its installations, population
    /// and body, following the InfrastructureProcessor pattern. Runs when installations change
    /// (ReCalcProcessor) and once a day (<see cref="ColonyPowerHotloopProcessor"/>), so population
    /// and the sun's distance stay current. Advisory only.
    /// </summary>
    public static class ColonyPowerProcessor
    {
        /// <summary>Life support every colonist pays, kW per person, on any world.</summary>
        public const double LifeSupportBase_kWPerColonist = 0.1;

        /// <summary>
        /// Extra life support per colonist per point of the species' colony cost on this body
        /// (0 on a garden world, capped at 3), kW. Colony cost is what sets how much infrastructure
        /// a person needs (PopulationProcessor: max pop = support / colony cost).
        /// </summary>
        public const double LifeSupportHostile_kWPerColonistPerCost = 0.6;

        /// <summary>Solar farms see half a day of sun on average.</summary>
        public const double SolarDayFraction = 0.5;

        /// <summary>Oxygen partial pressure a combustion plant needs from the air, atm.</summary>
        public const double MinOxygen_atm = 0.1;

        public const string LifeSupportLineId = "life-support";

        public static void Recalc(Entity colonyEntity)
        {
            if (colonyEntity == null
                || colonyEntity.Manager == null
                || !colonyEntity.TryGetDataBlob<ColonyInfoDB>(out var colonyInfo)
                || !colonyEntity.TryGetDataBlob<ComponentInstancesDB>(out var instancesDB))
                return;

            if (!colonyEntity.TryGetDataBlob<ColonyPowerDB>(out var power))
            {
                power = new ColonyPowerDB();
                colonyEntity.SetDataBlob(power);
            }

            var consumers = new List<ColonyPowerConsumer>();
            var plants = new List<ColonyPowerPlant>();
            var demandByTier = new Dictionary<PowerTier, double>();
            var fuelAtFull = new Dictionary<string, double>();
            double generation = 0;
            double storeMax = 0;

            Entity body = colonyInfo.PlanetEntity;
            double atmosphereFactor = SolarAtmosphereFactor(body);
            bool hasOxygen = HasAtmosphericOxygen(body);

            foreach (var (design, _) in instancesDB.DesignsAndComponentCount.ToList())
            {
                var enabled = instancesDB.GetComponentsBySpecificDesign(design.UniqueID).Where(c => c.IsEnabled).ToList();
                int count = enabled.Count;
                if (count == 0)
                    continue;
                double health = enabled.Sum(c => (double)c.HealthPercent);

                if (design.TryGetAttribute<PowerConsumerAtb>(out var consumer) && consumer.Draw_kW > 0)
                {
                    double draw = consumer.Draw_kW * count;
                    consumers.Add(new ColonyPowerConsumer(design.UniqueID, design.Name, consumer.Tier, count, draw));
                    Add(demandByTier, consumer.Tier, draw);
                }

                if (design.TryGetAttribute<PowerPlantAtb>(out var plant))
                {
                    double nameplate = plant.Output_kW * count;
                    double output = plant.Output_kW * health;
                    string? note = null;
                    if (plant.NeedsAtmosphericOxygen && !hasOxygen)
                    {
                        output = 0;
                        note = "No oxygen in the atmosphere";
                    }
                    double fuelPerDay = output > 0 ? plant.FuelAtFull_kgPerS * 86400 * (output / plant.Output_kW) : 0;
                    if (fuelPerDay > 0)
                        Add(fuelAtFull, plant.FuelId, fuelPerDay);
                    generation += output;
                    plants.Add(new ColonyPowerPlant(design.UniqueID, design.Name, ColonyPowerPlantKind.Fuel, count,
                        output, nameplate, plant.FuelId, fuelPerDay, 0, note));
                }

                if (design.TryGetAttribute<EnergySolarGenerationAtb>(out var panel))
                {
                    double one = SolarPanelOutput(colonyEntity, body, panel) * atmosphereFactor;
                    double output = one * health;
                    generation += output;
                    plants.Add(new ColonyPowerPlant(design.UniqueID, design.Name, ColonyPowerPlantKind.Solar, count,
                        output, one * count, null, 0, 0, null));
                }

                if (design.TryGetAttribute<EnergyStoreAtb>(out var battery) && battery.EnergyTypeID == power.EnergyTypeID)
                {
                    double store = battery.MaxStore * health;
                    storeMax += store;
                    plants.Add(new ColonyPowerPlant(design.UniqueID, design.Name, ColonyPowerPlantKind.Battery, count,
                        0, 0, null, 0, store, null));
                }
            }

            var (lifeSupport, colonists) = LifeSupport(colonyEntity, colonyInfo);
            if (lifeSupport > 0)
            {
                consumers.Insert(0, new ColonyPowerConsumer(LifeSupportLineId, "Life support", PowerTier.LifeSupport, 1, lifeSupport));
                Add(demandByTier, PowerTier.LifeSupport, lifeSupport);
            }

            power.Consumers = consumers
                .OrderBy(c => (int)c.Tier)
                .ThenByDescending(c => c.Draw_kW)
                .ThenBy(c => c.Name, StringComparer.Ordinal)
                .ToList();
            power.Plants = plants;
            power.DemandByTier = demandByTier;
            power.FuelAtFull_kgPerDay = fuelAtFull;
            power.Generation_kW = generation;
            power.StoreMax_kJ = storeMax;
            power.LifeSupport_kW = lifeSupport;
            power.LifeSupportColonists = colonists;
            power.Demand_kW = demandByTier.Values.Sum();
            power.TierSatisfaction = ShareByTier(power.Delivered_kW, demandByTier);
            power.LastRecalc = colonyEntity.StarSysDateTime;
        }

        /// <summary>
        /// Life support kW = Σ over species: people × (base + hostile × colony cost).
        /// Colony cost is capped at MAX_COST (3); an unsurvivable body counts as MAX_COST.
        /// </summary>
        internal static (double kW, long colonists) LifeSupport(Entity colonyEntity, ColonyInfoDB colonyInfo)
        {
            double kw = 0;
            long colonists = 0;
            foreach (var (speciesId, people) in colonyInfo.Population)
            {
                if (people <= 0)
                    continue;
                double cost = 0;
                if (colonyEntity.Manager.TryGetGlobalEntityById(speciesId, out var speciesEntity)
                    && speciesEntity.TryGetDataBlob<SpeciesDB>(out var species)
                    && colonyInfo.PlanetEntity.HasDataBlob<SystemBodyInfoDB>())
                {
                    cost = species.ColonyCost(colonyInfo.PlanetEntity);
                    // ColonyCost can exceed MAX_COST (Luna is ~36 for humans); the hostility
                    // term is capped there, as is an unsurvivable body (-1).
                    if (cost < 0 || double.IsNaN(cost) || cost > SpeciesDBExtensions.MAX_COST)
                        cost = SpeciesDBExtensions.MAX_COST;
                }
                kw += people * (LifeSupportBase_kWPerColonist + LifeSupportHostile_kWPerColonistPerCost * cost);
                colonists += people;
            }
            return (kw, colonists);
        }

        /// <summary>
        /// Shares delivered power out by tier, highest priority first. Each tier gets
        /// min(1, left / demand). Tiers with no demand are 1.
        /// </summary>
        internal static Dictionary<PowerTier, double> ShareByTier(double delivered, Dictionary<PowerTier, double> demandByTier)
        {
            var result = new Dictionary<PowerTier, double>();
            double left = Math.Max(0, delivered);
            foreach (PowerTier tier in Enum.GetValues(typeof(PowerTier)))
            {
                double demand = demandByTier.TryGetValue(tier, out var d) ? d : 0;
                if (demand <= 0)
                {
                    result[tier] = 1;
                    continue;
                }
                double got = Math.Min(left, demand);
                result[tier] = got / demand;
                left -= got;
            }
            return result;
        }

        /// <summary>
        /// Day-average output of one panel at the colony, kW, before the atmosphere.
        /// Reuses the ship solar model (star spectrum × panel efficiency × area).
        /// </summary>
        static double SolarPanelOutput(Entity colonyEntity, Entity body, EnergySolarGenerationAtb panel)
        {
            Pulsar4X.Movement.PositionDB? position = null;
            if (!colonyEntity.TryGetDataBlob<PositionDB>(out position))
                body.TryGetDataBlob<PositionDB>(out position);
            if (position == null)
                return 0;
            return EnergyGenHotloopProcessor.SolarAt(colonyEntity.Manager, position.AbsolutePosition, new[] { panel })
                   * SolarDayFraction;
        }

        /// <summary>
        /// Share of sunlight that reaches the ground: 1 / (1 + 1.5 × pressure in atm).
        /// Airless 1; Earth 0.4 (with the day fraction that is about a 20 % capacity factor).
        /// </summary>
        internal static double SolarAtmosphereFactor(Entity body)
        {
            if (body == null || !body.TryGetDataBlob<AtmosphereDB>(out var atmosphere))
                return 1;
            double p = Math.Max(0, atmosphere.Pressure);
            return 1 / (1 + 1.5 * p);
        }

        internal static bool HasAtmosphericOxygen(Entity body)
        {
            if (body == null || !body.TryGetDataBlob<AtmosphereDB>(out var atmosphere) || atmosphere.Composition == null)
                return false;
            return atmosphere.Composition.TryGetValue("oxygen", out var o2) && o2 >= MinOxygen_atm;
        }

        static void Add<TKey>(Dictionary<TKey, double> dict, TKey key, double value) where TKey : notnull
        {
            dict[key] = (dict.TryGetValue(key, out var v) ? v : 0) + value;
        }
    }

    /// <summary>
    /// Daily colony grid recalc, before research (0.5 h) and mining (1 h).
    /// </summary>
    public class ColonyPowerHotloopProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromDays(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.25);

        public Type GetParameterType => typeof(ColonyPowerDB);

        public void Init(Game game) { }

        public void ProcessEntity(Entity entity, int deltaSeconds) => ColonyPowerProcessor.Recalc(entity);

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var entities = manager.GetAllEntitiesWithDataBlob<ColonyPowerDB>();
            foreach (var entity in entities)
                ProcessEntity(entity, deltaSeconds);
            return entities.Count;
        }
    }
}
