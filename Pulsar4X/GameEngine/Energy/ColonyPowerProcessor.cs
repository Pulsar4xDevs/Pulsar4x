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
    /// and the sun's distance stay current. The daily pass also burns fuel, charges and draws
    /// the grid batteries, and enforces tiers.
    /// </summary>
    public static class ColonyPowerProcessor
    {
        /// <summary>
        /// Life support every colonist pays, kW per person, on any world: the survival half of a
        /// household's electricity (refrigeration, water, minimum heating, cooling and lighting).
        /// World-average household use is about 0.1 kW per person; the discretionary half is not modelled.
        /// </summary>
        public const double LifeSupportBase_kWPerColonist = 0.05;

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

        public const double SecondsPerDay = 86400;

        public const string NoOxygenNote = "No oxygen in the atmosphere";

        public const string OutOfFuelNote = "Out of fuel";

        /// <summary>
        /// Days of full-load fuel a colony keeps for its plants: the market holds it back from
        /// sale and bids for the shortfall. Scenario colonies are seeded with this runway.
        /// </summary>
        public const double FuelReserveDays = 730;

        /// <summary>
        /// Share of its nameplate a production facility draws with nothing to build: lights, HVAC,
        /// controls (plan §4.5, "industry idle = 10 % hotel draw"). Working, it adds its jobs' build energy.
        /// </summary>
        public const double IndustryIdleShare = 0.1;

        /// <summary>
        /// Share of its nameplate a research lab draws with no project (empty tech queue or no
        /// funding). Labs keep much more running than an idle factory: fume-hood and once-through
        /// ventilation, cold storage, instruments and servers stay on, so the top of the 10-20 % band.
        /// </summary>
        public const double ResearchIdleShare = 0.2;

        /// <summary>
        /// Share of its nameplate a mine draws with nothing it can mine and store: no deposit of any
        /// mineral it extracts left on the body, or no room in the colony's stores for any of them.
        /// Dewatering, ventilation and site lighting stay on.
        /// </summary>
        public const double MiningIdleShare = 0.1;

        /// <summary>Share of its nameplate a construction office draws with an empty queue.</summary>
        public const double ConstructionIdleShare = 0.1;

        public const string ScrappingLineId = "scrapping";

        /// <summary>
        /// Refreshes demand when a facility's idle/working state (mines, labs, construction offices)
        /// differs from what the last <see cref="Recalc"/> used, e.g. after the stores filled up or
        /// a deposit ran out. Cheap when nothing flipped: no recalc.
        /// </summary>
        public static void RefreshIfIdleStateChanged(Entity? colonyEntity)
        {
            if (colonyEntity == null || !colonyEntity.IsValid || colonyEntity.Manager == null
                || !colonyEntity.TryGetDataBlob<ColonyPowerDB>(out var power))
                return;
            if (IdleStateKey(colonyEntity) != power.IdleStateKey)
                Recalc(colonyEntity);
        }

        /// <summary>
        /// Instant refresh for a lab's project or funding change: the lab's colony
        /// (<see cref="Pulsar4X.Technology.ResearcherDB.LocationId"/>) is recalculated.
        /// </summary>
        public static void RefreshLab(Entity? labEntity, bool onlyIfIdleStateChanged = false)
        {
            if (labEntity?.Manager == null
                || !labEntity.TryGetDataBlob<Pulsar4X.Technology.ResearcherDB>(out var researcher)
                || !labEntity.Manager.TryGetEntityById(researcher.LocationId, out var colony))
                return;
            if (onlyIfIdleStateChanged)
                RefreshIfIdleStateChanged(colony);
            else
                RefreshDemand(colony);
        }

        /// <summary>
        /// Idle share of one facility, or null when it is working (or has no idle state).
        /// Production lines are handled by <see cref="ProductionDraw_kW"/>.
        /// </summary>
        internal static double? IdleShare(Entity colonyEntity, ComponentDesign design, ComponentInstance facility)
        {
            if (design.TryGetAttribute<Pulsar4X.Technology.ResearchPointsAtbDB>(out _))
                return LabWorking(colonyEntity, facility) ? null : ResearchIdleShare;
            if (design.TryGetAttribute<Pulsar4X.Industry.MineResourcesAtbDB>(out var mine))
                return MineWorking(colonyEntity, mine) ? null : MiningIdleShare;
            if (design.TryGetAttribute<Pulsar4X.Industry.ConstructionAtb>(out var construction))
                return ConstructionWorking(colonyEntity, construction) ? null : ConstructionIdleShare;
            return null;
        }

        /// <summary>A lab works while its tech queue has a project and it is funded.</summary>
        internal static bool LabWorking(Entity colonyEntity, ComponentInstance lab)
        {
            if (colonyEntity.Manager == null
                || !colonyEntity.Manager.TryGetEntityById(lab.SpawnedEntityId, out var labEntity)
                || !labEntity.TryGetDataBlob<Pulsar4X.Technology.ResearcherDB>(out var researcher))
                return false;
            return researcher.TechQueue.Count > 0 && researcher.FundingLevel > 0;
        }

        /// <summary>
        /// A mine works while at least one mineral it extracts is still in the body's deposits and
        /// the colony has room to store it.
        /// </summary>
        internal static bool MineWorking(Entity colonyEntity, Pulsar4X.Industry.MineResourcesAtbDB mine)
        {
            if (mine.ResourcesPerEconTick == null
                || !colonyEntity.TryGetDataBlob<ColonyInfoDB>(out var info)
                || info.PlanetEntity == null
                || !info.PlanetEntity.TryGetDataBlob<Pulsar4X.Industry.MineralsDB>(out var minerals)
                || !colonyEntity.TryGetDataBlob<CargoStorageDB>(out var storage)
                || colonyEntity.Manager?.Game == null
                || !colonyEntity.Manager.Game.Factions.TryGetValue(colonyEntity.FactionOwnerID, out var faction)
                || !faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo))
                return false;
            foreach (var (mineralId, rate) in mine.ResourcesPerEconTick)
            {
                if (rate <= 0 || factionInfo.Data.CargoGoods.GetAny(mineralId) is not Pulsar4X.Industry.Mineral mineral)
                    continue;
                if (!minerals.Minerals.TryGetValue(mineral.ID, out var deposit)
                    || deposit.Amount.Actual <= 0 || deposit.Accessibility <= 0)
                    continue;
                if (!storage.TypeStores.ContainsKey(mineral.CargoTypeID) || storage.GetFreeUnitSpace(mineral) <= 0)
                    continue;
                return true;
            }
            return false;
        }

        /// <summary>A construction office works while the queue its points feed has a job.</summary>
        internal static bool ConstructionWorking(Entity colonyEntity, Pulsar4X.Industry.ConstructionAtb construction)
        {
            if (!colonyEntity.TryGetDataBlob<Pulsar4X.Industry.LocalConstructionDB>(out var queue))
                return false;
            bool installs = construction.ConstructionPoints.TryGetValue("installation", out int i) && i != 0;
            bool refits = construction.ConstructionPoints.TryGetValue("refit", out int r) && r != 0;
            return (installs && queue.BuildQueue.Count > 0) || (refits && queue.RefitQueue.Count > 0);
        }

        /// <summary>Every idle-capable facility's working (1) or idle (0) state, per enabled instance.</summary>
        internal static string IdleStateKey(Entity colonyEntity)
        {
            if (!colonyEntity.TryGetDataBlob<ComponentInstancesDB>(out var instancesDB))
                return "";
            var sb = new System.Text.StringBuilder();
            foreach (var (design, _) in instancesDB.DesignsAndComponentCount)
            {
                if (!design.TryGetAttribute<PowerConsumerAtb>(out var consumer) || !(consumer.Draw_kW > 0))
                    continue;
                bool any = false;
                foreach (var c in instancesDB.GetComponentsBySpecificDesign(design.UniqueID))
                {
                    if (!c.IsEnabled)
                        continue;
                    var share = IdleShare(colonyEntity, design, c);
                    if (!any)
                    {
                        if (share == null && !IdleCapable(design))
                            break;
                        sb.Append(design.UniqueID).Append(':');
                        any = true;
                    }
                    sb.Append(share == null ? '1' : '0');
                }
                if (any)
                    sb.Append(';');
            }
            return sb.ToString();
        }

        static bool IdleCapable(ComponentDesign design)
            => design.TryGetAttribute<Pulsar4X.Technology.ResearchPointsAtbDB>(out _)
            || design.TryGetAttribute<Pulsar4X.Industry.MineResourcesAtbDB>(out _)
            || design.TryGetAttribute<Pulsar4X.Industry.ConstructionAtb>(out _);

        /// <summary>
        /// Instant demand refresh, for a change to what the colony is building (an industry job
        /// queued, cancelled, edited, reordered or finished; an installation job queued, removed,
        /// reordered or finished). Runs <see cref="Recalc"/>, which only rebuilds the demand lines
        /// and the capacity-based satisfaction it already sets between daily passes: no fuel is
        /// burnt, no history is recorded and no event is raised, so the daily <see cref="RunDay"/>
        /// is unchanged. Does nothing for an entity with no grid (ships, colonies without a
        /// <see cref="ColonyPowerDB"/>), so it never creates one.
        /// </summary>
        public static void RefreshDemand(Entity? colonyEntity)
        {
            if (colonyEntity == null || !colonyEntity.IsValid || colonyEntity.Manager == null || !colonyEntity.HasDataBlob<ColonyPowerDB>())
                return;
            Recalc(colonyEntity);
        }

        /// <summary>
        /// What <see cref="ProductionDraw_kW"/> reads from the production lines: each line's head job
        /// of each industry type, with its build energy. Two equal keys give the same draw, so a daily
        /// industry pass only refreshes demand when the key changed.
        /// </summary>
        internal static string ProductionDrawKey(Pulsar4X.Industry.IndustryAbilityDB industry)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (lineId, line) in industry.ProductionLines)
            {
                sb.Append(lineId).Append(':');
                foreach (var (type, _) in line.IndustryTypeRates)
                {
                    var job = HeadJob(line, type);
                    sb.Append(type).Append('=').Append(job == null ? "-" : job.JobID + "@" + job.EnergyPerPoint.ToString("R")).Append(',');
                }
                sb.Append(';');
            }
            return sb.ToString();
        }

        static Pulsar4X.Industry.IndustryJob? HeadJob(Pulsar4X.Industry.IndustryAbilityDB.ProductionLine line, string type)
            => line.Jobs.FirstOrDefault(j => j.TypeID == type
                && j.NumberCompleted < j.NumberOrdered
                && j.Status != Pulsar4X.DataStructures.IndustryJobStatus.MissingResources);

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
            double maxCharge = 0, maxDischarge = 0, chargeLoss = 0;

            Entity body = colonyInfo.PlanetEntity;
            double atmosphereFactor = SolarAtmosphereFactor(body);
            bool hasOxygen = HasAtmosphericOxygen(body);

            colonyEntity.TryGetDataBlob<Pulsar4X.Industry.IndustryAbilityDB>(out var industry);
            double infraEfficiency = Pulsar4X.Industry.InfrastructureProcessor.GetEfficiency(colonyEntity);
            double energyFactor = EnergyTech.IndustrialEnergyFactor(colonyEntity);
            power.GridEfficiency = EnergyTech.GridEfficiency(colonyEntity);

            foreach (var (design, _) in instancesDB.DesignsAndComponentCount.ToList())
            {
                var enabled = instancesDB.GetComponentsBySpecificDesign(design.UniqueID).Where(c => c.IsEnabled).ToList();
                int count = enabled.Count;
                if (count == 0)
                    continue;
                // Sum of health (1 per undamaged instance). Damaged facilities draw and
                // generate in proportion to their health.
                double health = enabled.Sum(c => Math.Clamp((double)c.HealthPercent, 0, 1));

                if (design.TryGetAttribute<PowerConsumerAtb>(out var consumer) && consumer.Draw_kW > 0)
                {
                    double nameplate = consumer.Draw_kW * health;
                    double draw = enabled.Sum(c => FacilityDraw_kW(colonyEntity, industry, design, c, consumer.Draw_kW, infraEfficiency, energyFactor));
                    consumers.Add(new ColonyPowerConsumer(design.UniqueID, design.Name, consumer.Tier, count, draw, nameplate));
                    Add(demandByTier, consumer.Tier, draw);
                }

                if (design.TryGetAttribute<PowerPlantAtb>(out var plant))
                {
                    double nameplate = plant.Output_kW * count;
                    double output = plant.Output_kW * health;
                    double kgPerKJ = plant.Efficiency > 0 && plant.LHV_MJperKg > 0
                        ? 1 / (plant.Efficiency * plant.LHV_MJperKg * 1000)
                        : 0;
                    string? note = null;
                    if (plant.NeedsAtmosphericOxygen && !hasOxygen)
                    {
                        output = 0;
                        note = NoOxygenNote;
                    }
                    else if (kgPerKJ > 0 && FuelAvailable_kg(colonyEntity, power, plant.FuelId) <= 0)
                    {
                        output = 0;
                        note = OutOfFuelNote;
                    }
                    double fuelPerDay = output * kgPerKJ * SecondsPerDay;
                    if (fuelPerDay > 0)
                        Add(fuelAtFull, plant.FuelId, fuelPerDay);
                    generation += output;
                    plants.Add(new ColonyPowerPlant(design.UniqueID, design.Name, ColonyPowerPlantKind.Fuel, count,
                        output, nameplate, plant.FuelId, fuelPerDay, 0, note, kgPerKJ));
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
                    // An unrated battery (a design from before battery rates) can fill or empty
                    // within a second: no practical limit and no loss.
                    maxCharge += battery.IsRated ? battery.MaxCharge_kW * health : store;
                    maxDischarge += battery.IsRated ? battery.MaxDischarge_kW * health : store;
                    chargeLoss += store * (1 - battery.ChargeEfficiency);
                    plants.Add(new ColonyPowerPlant(design.UniqueID, design.Name, ColonyPowerPlantKind.Battery, count,
                        0, 0, null, 0, store, null));
                }
            }

            double scrapping = Scrapping_kW(colonyEntity);
            if (scrapping > 0)
            {
                consumers.Add(new ColonyPowerConsumer(ScrappingLineId, "Scrapping", PowerTier.Industry, 1, scrapping, scrapping));
                Add(demandByTier, PowerTier.Industry, scrapping);
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
            power.MaxCharge_kW = maxCharge;
            power.MaxDischarge_kW = maxDischarge;
            power.ChargeEfficiency = storeMax > 0 ? Math.Clamp(1 - chargeLoss / storeMax, 0.01, 1) : 1;
            // Batteries taken out or damaged lose the charge they can no longer hold.
            power.Stored_kJ = double.IsFinite(power.Stored_kJ) ? Math.Clamp(power.Stored_kJ, 0, storeMax) : 0;
            power.LifeSupport_kW = lifeSupport;
            power.LifeSupportColonists = colonists;
            power.Demand_kW = demandByTier.Values.Sum();
            // Between daily passes, satisfaction follows what the plants can give now, plus what the
            // batteries can give on average over a day.
            power.TierSatisfaction = ShareByTier(power.Delivered_kW + BatterySupply_kW(power, SecondsPerDay), demandByTier);
            power.IdleStateKey = IdleStateKey(colonyEntity);
            power.LastRecalc = colonyEntity.StarSysDateTime;
        }

        /// <summary>
        /// One facility's draw, kW: a production line's from <see cref="ProductionDraw_kW"/>; a lab,
        /// mine or construction office its nameplate working and <see cref="IdleShare"/> of it idle;
        /// anything else its nameplate. Damaged facilities draw in proportion to their health.
        /// </summary>
        internal static double FacilityDraw_kW(Entity colonyEntity, Pulsar4X.Industry.IndustryAbilityDB? industry,
            ComponentDesign design, ComponentInstance facility, double nameplatePerUnit_kW, double infraEfficiency, double energyFactor)
        {
            if (industry != null && industry.ProductionLines.ContainsKey(facility.UniqueID))
                return ProductionDraw_kW(industry, facility, nameplatePerUnit_kW, infraEfficiency, energyFactor);
            double nameplate = nameplatePerUnit_kW * Math.Clamp((double)facility.HealthPercent, 0, 1);
            return IdleShare(colonyEntity, design, facility) is double share ? nameplate * share : nameplate;
        }

        /// <summary>
        /// One facility's draw, kW. Not a production line: its nameplate x health. A production line
        /// draws <see cref="IndustryIdleShare"/> of its nameplate plus what its head job of each industry
        /// type needs at the line's full rate (rate x infrastructure x kWh per point / 24 h), capped at
        /// the nameplate. The cap is also what the line can spend in a day (IndustryTools).
        /// <paramref name="energyFactor"/> is Industrial Energy Efficiency's multiplier on kWh per point.
        /// </summary>
        internal static double ProductionDraw_kW(Pulsar4X.Industry.IndustryAbilityDB industry, ComponentInstance facility,
            double nameplatePerUnit_kW, double infraEfficiency, double energyFactor = 1)
        {
            double nameplate = nameplatePerUnit_kW * Math.Clamp((double)facility.HealthPercent, 0, 1);
            if (!industry.ProductionLines.TryGetValue(facility.UniqueID, out var line))
                return nameplate;
            double working_kWhPerDay = 0;
            foreach (var (type, rate) in line.IndustryTypeRates)
            {
                var job = HeadJob(line, type);
                if (job == null || job.EnergyPerPoint <= 0)
                    continue;
                working_kWhPerDay += rate * infraEfficiency * job.EnergyPerPoint * energyFactor;
            }
            return Math.Min(nameplate, nameplate * IndustryIdleShare + working_kWhPerDay / 24);
        }

        /// <summary>
        /// Draw of the facility upgrades that scrap a site-built facility, kW: each queued job's
        /// <see cref="Pulsar4X.Industry.LocalConstructionJob.ScrapEnergy"/> spread over its points,
        /// for the points the colony will spend on it today.
        /// </summary>
        internal static double Scrapping_kW(Entity colonyEntity)
        {
            if (!colonyEntity.TryGetDataBlob<Pulsar4X.Industry.LocalConstructionDB>(out var construction))
                return 0;
            long pointsLeft = construction.PointsPerDay;
            double kWh = 0;
            foreach (var job in construction.BuildQueue)
            {
                if (pointsLeft <= 0)
                    break;
                long needed = Math.Max(0, job.PointCost - job.PointsAccumulated);
                long spend = Math.Min(pointsLeft, needed);
                pointsLeft -= spend;
                if (job.ScrapEnergy > 0 && job.PointCost > 0)
                    kWh += job.ScrapEnergy * spend / job.PointCost;
            }
            return kWh / 24;
        }

        /// <summary>
        /// The daily pass: recalc, then run the plants for one day against the demand.
        /// Solar is used first, then fuel plants share the rest at one load factor.
        /// Each fuel plant burns E / (η × LHV) from the colony stockpile; a fuel that runs
        /// short gives only what its stock covers and its plants stop. Spare delivered power
        /// charges the grid batteries (up to their rate, less the charge loss) and a shortfall
        /// draws them down first. Tier satisfaction is set from what was actually delivered plus
        /// the batteries' share, and events are raised for a new shortage and for a fuel that ran out.
        /// </summary>
        public static void RunDay(Entity colonyEntity, double seconds = SecondsPerDay)
        {
            Recalc(colonyEntity);
            if (!colonyEntity.TryGetDataBlob<ColonyPowerDB>(out var power))
                return;

            double demandGen = power.GridEfficiency > 0 ? power.Demand_kW / power.GridEfficiency : 0;
            // The batteries ask for what they can take today: their charge rate, or less when the
            // room left fills sooner. Charging comes after the demand: plants run for it only with
            // capacity to spare, and it is not part of Demand_kW.
            double room = Math.Max(0, power.StoreMax_kJ - power.Stored_kJ);
            double eta = power.ChargeEfficiency > 0 ? power.ChargeEfficiency : 1;
            double chargeWant = seconds > 0 ? Math.Min(Math.Max(0, power.MaxCharge_kW), room / (eta * seconds)) : 0;
            double wantGen = demandGen + (power.GridEfficiency > 0 ? chargeWant / power.GridEfficiency : 0);
            double solar = power.Plants.Where(p => p.Kind == ColonyPowerPlantKind.Solar).Sum(p => p.Output_kW);
            double remaining = Math.Max(0, wantGen - solar);

            // Fuel plants grouped by fuel. A group whose stock cannot cover its share is
            // capped at what the stock gives, and the others pick up the slack.
            var groups = power.Plants
                .Where(p => p.Kind == ColonyPowerPlantKind.Fuel && p.Output_kW > 0)
                .GroupBy(p => p.FuelId ?? "")
                .Select(g => new FuelGroup(g.Key, g.ToList(), FuelAvailable_kg(colonyEntity, power, g.Key)))
                .ToList();
            // A fuel that is back in stock can warn again next time it runs out.
            power.FuelOut.RemoveWhere(id => FuelAvailable_kg(colonyEntity, power, id) > 0);
            var free = new List<FuelGroup>(groups);
            double fixedOutput = 0;
            double load = 0;
            while (true)
            {
                double freeCap = free.Sum(g => g.Capacity_kW);
                load = freeCap > 0 ? Math.Min(1, Math.Max(0, remaining - fixedOutput) / freeCap) : 0;
                var shortGroups = free.Where(g => g.KgPerKJ > 0 && g.Capacity_kW * load * g.KgPerKJ * seconds > g.Stock_kg).ToList();
                if (shortGroups.Count == 0)
                    break;
                foreach (var g in shortGroups)
                {
                    g.Short = true;
                    g.Output_kW = g.Stock_kg / (g.KgPerKJ * seconds);
                    fixedOutput += g.Output_kW;
                    free.Remove(g);
                }
            }
            foreach (var g in free)
                g.Output_kW = g.Capacity_kW * load;

            var burnt = new Dictionary<string, double>();
            double fuelOutput = 0;
            foreach (var g in groups)
            {
                fuelOutput += g.Output_kW;
                double kg = g.Output_kW * g.KgPerKJ * seconds;
                if (g.Short)
                    kg = g.Stock_kg;
                if (kg > 0)
                {
                    BurnFuel(colonyEntity, power, g.FuelId, kg);
                    burnt[g.FuelId] = kg;
                }
            }

            double generated = Math.Min(solar, wantGen) + fuelOutput;
            power.Dispatched_kW = generated;
            power.FuelBurnt_kgLastDay = burnt;
            double delivered = generated * power.GridEfficiency;

            // The batteries sit on the delivered side of the grid: they charge from what is left
            // after the demand and give back (without further grid loss) what the demand lacks,
            // before any tier goes short. Average kW over the pass, like the plants.
            // Known simplification (TessaBot P3, deferred): a plant that runs dry mid-pass is averaged
            // over the whole day, so the discharge cap applies to the day-average shortfall, not to
            // the deeper post-runout gap. Splitting the pass into before- and after-runout phases
            // would need each fuel group's own wanted load and a ramp for the other groups after
            // the runout, and would change what TierSatisfaction means for such a day.
            double chargeIn = 0, dischargeOut = 0;
            if (seconds > 0)
            {
                double surplus = delivered - power.Demand_kW;
                if (surplus > 0)
                    chargeIn = Math.Min(surplus, chargeWant);
                else if (surplus < 0)
                    dischargeOut = Math.Min(-surplus, Math.Min(Math.Max(0, power.MaxDischarge_kW), power.Stored_kJ / seconds));
                power.Stored_kJ = Math.Clamp(power.Stored_kJ + (chargeIn * eta - dischargeOut) * seconds, 0, power.StoreMax_kJ);
            }
            power.BatteryCharge_kW = chargeIn;
            power.BatteryDischarge_kW = dischargeOut;
            power.Battery_kW = chargeIn * eta - dischargeOut;
            double supply = delivered - chargeIn + dischargeOut;
            power.TierSatisfaction = ShareByTier(supply, power.DemandByTier);
            power.LastDispatch = colonyEntity.StarSysDateTime;

            RaiseEvents(colonyEntity, power, groups);

            // Plants whose fuel ran out today show it from now on.
            if (groups.Any(g => g.Short))
            {
                Recalc(colonyEntity);
                power.TierSatisfaction = ShareByTier(supply, power.DemandByTier);
            }

            power.RecordDay(new ColonyPowerSample(colonyEntity.StarSysDateTime, power.Demand_kW, power.Generation_kW, delivered,
                power.Stored_kJ, chargeIn, dischargeOut));
        }

        /// <summary>Fills the colony's grid batteries to their capacity (a scenario start). No grid or no batteries: nothing.</summary>
        public static void FillBatteries(Entity colonyEntity)
        {
            if (colonyEntity.TryGetDataBlob<ColonyPowerDB>(out var power))
                power.Stored_kJ = Math.Max(0, power.StoreMax_kJ);
        }

        /// <summary>
        /// What the batteries can give on average over <paramref name="seconds"/>, kW: their
        /// discharge rate, or less when the charge runs out sooner.
        /// </summary>
        internal static double BatterySupply_kW(ColonyPowerDB power, double seconds)
        {
            if (!(seconds > 0) || !(power.Stored_kJ > 0))
                return 0;
            return Math.Min(Math.Max(0, power.MaxDischarge_kW), power.Stored_kJ / seconds);
        }

        /// <summary>
        /// The share of its demand a tier gets, 0..1. 1 when the colony has no grid or the
        /// tier draws nothing, so colonies without a grid (old saves) are unaffected.
        /// </summary>
        public static double Satisfaction(Entity? colonyEntity, PowerTier tier)
        {
            if (colonyEntity == null || !colonyEntity.TryGetDataBlob<ColonyPowerDB>(out var power))
                return 1;
            if (!power.DemandByTier.TryGetValue(tier, out var demand) || demand <= 0)
                return 1;
            return power.TierSatisfaction.TryGetValue(tier, out var s) ? Math.Clamp(s, 0, 1) : 1;
        }

        static void RaiseEvents(Entity colonyEntity, ColonyPowerDB power, List<FuelGroup> groups)
        {
            string name = colonyEntity.TryGetDataBlob<Pulsar4X.Names.NameDB>(out var nameDB)
                ? nameDB.GetName(colonyEntity.FactionOwnerID)
                : "Colony";

            var shortTiers = power.TierSatisfaction
                .Where(kv => kv.Value < 1 - 1e-9)
                .OrderBy(kv => (int)kv.Key)
                .Select(kv => kv.Key)
                .ToList();
            bool isShort = shortTiers.Count > 0;
            if (isShort && !power.Short)
            {
                string tiers = string.Join(", ", shortTiers.Select(t =>
                    $"{TierName(t)} {power.TierSatisfaction[t] * 100:0}%"));
                Publish(colonyEntity, Pulsar4X.Events.EventType.PowerShortage,
                    $"{name}: power shortage. {tiers}.");
            }
            power.Short = isShort;

            foreach (var g in groups.Where(g => g.Short))
            {
                if (power.FuelOut.Contains(g.FuelId))
                    continue;
                power.FuelOut.Add(g.FuelId);
                string fuelName = FuelName(colonyEntity, g.FuelId);
                string plantsText = string.Join(", ", g.Plants.Select(p => p.Name).Distinct());
                Publish(colonyEntity, Pulsar4X.Events.EventType.PlantOutOfFuel,
                    $"{name}: {plantsText} ran out of {fuelName}.");
            }
        }

        static void Publish(Entity colonyEntity, Pulsar4X.Events.EventType type, string message)
        {
            Pulsar4X.Events.EventManager.Instance.Publish(Pulsar4X.Events.Event.Create(
                type,
                colonyEntity.StarSysDateTime,
                message,
                colonyEntity.FactionOwnerID,
                colonyEntity.Manager.ManagerID,
                colonyEntity.Id));
        }

        internal static string TierName(PowerTier tier) => tier == PowerTier.LifeSupport ? "Life support" : tier.ToString();

        /// <summary>
        /// Units of this cargo the colony keeps for its plants: FuelReserveDays at full
        /// nameplate load (a plant that is out of fuel still counts). 0 for anything else.
        /// </summary>
        public static long FuelReserveUnits(Entity colonyEntity, string cargoId)
        {
            if (colonyEntity == null || !colonyEntity.TryGetDataBlob<ColonyPowerDB>(out var power))
                return 0;
            double kgPerDay = power.Plants
                .Where(p => p.Kind == ColonyPowerPlantKind.Fuel && p.FuelId == cargoId && p.FuelKgPerKJ > 0)
                .Sum(p => p.Nameplate_kW * p.FuelKgPerKJ * SecondsPerDay);
            if (kgPerDay <= 0)
                return 0;
            var cargo = FuelCargo(colonyEntity, cargoId);
            double massPerUnit = cargo != null && cargo.MassPerUnit > 0 ? cargo.MassPerUnit : 1;
            return (long)Math.Ceiling(kgPerDay * FuelReserveDays / massPerUnit - 1e-9);
        }

        static ICargoable? FuelCargo(Entity colonyEntity, string fuelId)
        {
            if (string.IsNullOrEmpty(fuelId)
                || !colonyEntity.Manager.Game.Factions.TryGetValue(colonyEntity.FactionOwnerID, out var faction)
                || !faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo))
                return null;
            return factionInfo.Data.CargoGoods.GetAny(fuelId);
        }

        static string FuelName(Entity colonyEntity, string fuelId) => FuelCargo(colonyEntity, fuelId)?.Name ?? fuelId;

        /// <summary>Fuel the colony can still burn, kg: whole units in the stockpile less the part-unit already burnt.</summary>
        internal static double FuelAvailable_kg(Entity colonyEntity, ColonyPowerDB power, string fuelId)
        {
            var cargo = FuelCargo(colonyEntity, fuelId);
            if (cargo == null || cargo.MassPerUnit <= 0 || !colonyEntity.TryGetDataBlob<CargoStorageDB>(out var storage))
                return 0;
            double stock = storage.GetUnitsStored(cargo, includeEscro: false) * (double)cargo.MassPerUnit;
            double owed = power.FuelBurnDebt_kg.TryGetValue(fuelId, out var d) ? d : 0;
            return Math.Max(0, stock - owed);
        }

        /// <summary>
        /// Takes kg of fuel from the stockpile. Cargo is stored in whole units, so the part
        /// unit is carried as a debt and taken with the next whole unit.
        /// </summary>
        static void BurnFuel(Entity colonyEntity, ColonyPowerDB power, string fuelId, double kg)
        {
            var cargo = FuelCargo(colonyEntity, fuelId);
            if (cargo == null || cargo.MassPerUnit <= 0 || !colonyEntity.TryGetDataBlob<CargoStorageDB>(out var storage))
                return;
            double owed = (power.FuelBurnDebt_kg.TryGetValue(fuelId, out var d) ? d : 0) + kg;
            long units = (long)Math.Floor(owed / cargo.MassPerUnit + 1e-9);
            long stored = storage.GetUnitsStored(cargo, includeEscro: false);
            if (units > stored)
                units = stored;
            long removed = units > 0 ? storage.RemoveCargoByUnit(cargo, units) : 0;
            owed -= removed * (double)cargo.MassPerUnit;
            // Never owe more than what is left, so an emptied store reads as empty.
            long left = storage.GetUnitsStored(cargo, includeEscro: false);
            owed = Math.Clamp(owed, 0, left * (double)cargo.MassPerUnit);
            if (left == 0)
                owed = 0;
            power.FuelBurnDebt_kg[fuelId] = owed;
        }

        sealed class FuelGroup
        {
            public FuelGroup(string fuelId, List<ColonyPowerPlant> plants, double stock)
            {
                FuelId = fuelId;
                Plants = plants;
                Stock_kg = stock;
                Capacity_kW = plants.Sum(p => p.Output_kW);
                // Plants on one fuel can differ in efficiency: use the output-weighted mix.
                KgPerKJ = Capacity_kW > 0 ? plants.Sum(p => p.Output_kW * p.FuelKgPerKJ) / Capacity_kW : 0;
            }

            public string FuelId { get; }
            public List<ColonyPowerPlant> Plants { get; }
            public double Stock_kg { get; }
            public double Capacity_kW { get; }
            public double KgPerKJ { get; }
            public double Output_kW { get; set; }
            public bool Short { get; set; }
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
                    && colonyInfo.PlanetEntity != null
                    && colonyInfo.PlanetEntity.HasDataBlob<SystemBodyInfoDB>()
                    // ColonyCost reads the orbit for temperature; bare test bodies have none.
                    && colonyInfo.PlanetEntity.HasDataBlob<Pulsar4X.Orbits.OrbitDB>())
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
                // Rounding in the dispatch must not leave the last tier at 0.999999...
                result[tier] = got >= demand * (1 - 1e-9) ? 1 : got / demand;
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
                body?.TryGetDataBlob<PositionDB>(out position);
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
    /// Daily colony grid pass: burns fuel and sets tier satisfaction, before research (0.5 h),
    /// mining (1 h) and industry (3 h) read it.
    /// </summary>
    public class ColonyPowerHotloopProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromDays(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.25);

        public Type GetParameterType => typeof(ColonyPowerDB);

        public void Init(Game game) { }

        public void ProcessEntity(Entity entity, int deltaSeconds) => ColonyPowerProcessor.RunDay(entity);

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var entities = manager.GetAllEntitiesWithDataBlob<ColonyPowerDB>();
            foreach (var entity in entities)
                ProcessEntity(entity, deltaSeconds);
            return entities.Count;
        }
    }
}
