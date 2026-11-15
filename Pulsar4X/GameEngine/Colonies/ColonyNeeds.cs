using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbits;
using Pulsar4X.Interfaces;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// Food, water and satisfaction for one colony (multi-faction phase 7a, plan §7.4). Created at colony creation
    /// and at load; a colony loaded with empty stock starts its shortage clock on the first day it runs short,
    /// never backdated.
    /// </summary>
    public class ColonyNeedsDB : BaseDataBlob
    {
        /// <summary>First day of the current shortage (null when supplied).</summary>
        [JsonProperty] public DateTime? ShortSince { get; internal set; }
        /// <summary>Consecutive fully supplied days since the last short day (10 resets the clock).</summary>
        [JsonProperty] public int SuppliedDays { get; internal set; }
        /// <summary>Last shortage day number processed (−1 when none), so stage events fire once.</summary>
        [JsonProperty] public int LastShortDay { get; internal set; } = -1;
        /// <summary>Last day consumption ran.</summary>
        [JsonProperty] public DateTime? LastDay { get; internal set; }
        /// <summary>Needed and supplied kg in the current 10-day shortage window.</summary>
        [JsonProperty] public double WindowFoodNeeded { get; internal set; }
        [JsonProperty] public double WindowFoodSupplied { get; internal set; }
        [JsonProperty] public double WindowWaterNeeded { get; internal set; }
        [JsonProperty] public double WindowWaterSupplied { get; internal set; }
        /// <summary>Food or water ran short at least once in the current shortage window.</summary>
        [JsonProperty] public bool FoodShort { get; internal set; }
        [JsonProperty] public bool WaterShort { get; internal set; }
        /// <summary>The under-30-days warning was sent and stock has not recovered since.</summary>
        [JsonProperty] public bool LowStockWarned { get; internal set; }
        /// <summary>Fractional people owed to the shortage decline.</summary>
        [JsonProperty] public double DeclineOwed { get; internal set; }
        [JsonProperty] public long DeathsTotal { get; internal set; }
        [JsonProperty] public long LeftTotal { get; internal set; }
        /// <summary>0..100; new games start at 60.</summary>
        [JsonProperty] public double Satisfaction { get; internal set; } = ColonyNeeds.StartSatisfaction;
        [JsonProperty] public double SatisfactionTarget { get; internal set; } = ColonyNeeds.StartSatisfaction;
        [JsonProperty] public DateTime? LastSatisfactionTick { get; internal set; }

        public ColonyNeedsDB() { }
        public ColonyNeedsDB(ColonyNeedsDB db)
        {
            ShortSince = db.ShortSince; SuppliedDays = db.SuppliedDays; LastShortDay = db.LastShortDay; LastDay = db.LastDay;
            WindowFoodNeeded = db.WindowFoodNeeded; WindowFoodSupplied = db.WindowFoodSupplied;
            WindowWaterNeeded = db.WindowWaterNeeded; WindowWaterSupplied = db.WindowWaterSupplied;
            FoodShort = db.FoodShort; WaterShort = db.WaterShort; LowStockWarned = db.LowStockWarned;
            DeclineOwed = db.DeclineOwed; DeathsTotal = db.DeathsTotal; LeftTotal = db.LeftTotal;
            Satisfaction = db.Satisfaction; SatisfactionTarget = db.SatisfactionTarget;
            LastSatisfactionTick = db.LastSatisfactionTick;
        }
        public override object Clone() => new ColonyNeedsDB(this);
    }

    public static class ColonyNeeds
    {
        public const string FoodId = "food";
        public const string WaterId = "water";
        public const string AgricultureId = "agriculture";
        public const double FoodPerPersonPerDay = 0.5;
        public const double WaterPerPersonPerDay = 1.0;
        public const double StartSatisfaction = 60;
        public const int WarnDays = 30;
        public const int DeclineDay = 30;
        public const int WarnEveryFromDay = 60;
        public const int DeathDay = 90;
        public const int StepDays = 10;
        public const int ResetDays = 10;
        public const double DeclinePerMonth = 0.01;
        public const double DeathRate = 0.01;
        /// <summary>Runway an off-world colony holds back from sale (and bids for, with a demand row).</summary>
        public const double HoldDays = 180;
        /// <summary>Share of a starving or unhappy colony that wants to leave (posted as relocation listings).</summary>
        public const double LeavingShare = 0.05;
        /// <summary>Hook for phase 7b medical goods: 1 now; supplied medical halves deaths.</summary>
        public static double MedicalFactor(Entity colony) => 1.0;

        public static void Ensure(Entity colony)
        {
            if (colony == null || !colony.HasDataBlob<ColonyInfoDB>() || colony.HasDataBlob<ColonyNeedsDB>())
                return;
            colony.SetDataBlob(new ColonyNeedsDB());
        }

        public static long People(Entity colony)
            => colony.TryGetDataBlob<ColonyInfoDB>(out var info) ? NavalAcademyCourse.PopulationOf(info) : 0;

        /// <summary>
        /// True when every species living here has colony cost 0: the world feeds and waters its own people
        /// (Earth, the blocs and the company's Earth colony eat nothing from stock).
        /// </summary>
        public static bool SelfSupplied(Entity colony)
        {
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info) || info.PlanetEntity == null)
                return true;
            var planet = info.PlanetEntity;
            if (!planet.HasDataBlob<SystemBodyInfoDB>() || !planet.HasDataBlob<OrbitDB>())
                return true;
            foreach (var (id, count) in info.Population)
            {
                if (count <= 0 || !colony.Manager.TryGetGlobalEntityById(id, out var species)
                    || !species.TryGetDataBlob<SpeciesDB>(out var sdb))
                    continue;
                if (sdb.ColonyCost(planet) > 0)
                    return false;
            }
            return true;
        }

        public static double FoodPerDay(Entity colony) => SelfSupplied(colony) ? 0 : People(colony) * FoodPerPersonPerDay;
        public static double WaterPerDay(Entity colony) => SelfSupplied(colony) ? 0 : People(colony) * WaterPerPersonPerDay;

        static ICargoable? Cargo(Entity colony, string id)
        {
            var game = colony.Manager?.Game;
            if (game == null || !game.Factions.TryGetValue(colony.FactionOwnerID, out var faction)
                || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
                return null;
            return info.Data.CargoGoods.Contains(id) ? info.Data.CargoGoods.GetAny(id) : null;
        }

        public static long Stock(Entity colony, string id)
        {
            var cargo = Cargo(colony, id);
            return cargo != null && colony.TryGetDataBlob<CargoStorageDB>(out var storage) ? storage.GetUnitsStored(cargo, false) : 0;
        }

        /// <summary>Days of stock at today's use; null when the colony draws none.</summary>
        public static double? DaysOfStock(Entity colony, string id)
        {
            double use = id == FoodId ? FoodPerDay(colony) : WaterPerDay(colony);
            return use > 0 ? Stock(colony, id) / use : null;
        }

        /// <summary>Food or water an off-world colony holds back (<see cref="HoldDays"/> of use). 0 for anything else, or when a demand row with a Min governs.</summary>
        public static long HoldUnits(Entity colony, string cargoId)
        {
            // A scenario demand row (Luna's gap rows) sets its own hold; the runway applies only without one.
            if (colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy) && policy.Rows.TryGetValue(cargoId, out var row) && row.Min > 0)
                return 0;
            if (cargoId == FoodId)
                return (long)Math.Ceiling(FoodPerDay(colony) * HoldDays);
            if (cargoId == WaterId)
                return (long)Math.Ceiling(WaterPerDay(colony) * HoldDays);
            return 0;
        }

        /// <summary>Whole days into the current shortage, or null.</summary>
        public static int? ShortageDay(Entity colony, DateTime now)
            => colony.TryGetDataBlob<ColonyNeedsDB>(out var db) && db.ShortSince is DateTime since
                ? Math.Max(0, (int)Math.Floor((now - since).TotalDays)) : null;

        public static double Satisfaction(Entity colony)
            => colony.TryGetDataBlob<ColonyNeedsDB>(out var db) ? db.Satisfaction : StartSatisfaction;

        /// <summary>Population growth multiplier: 0.5 + S/100. 1 without a needs record.</summary>
        public static double GrowthFactor(Entity colony)
            => colony.TryGetDataBlob<ColonyNeedsDB>(out var db) ? 0.5 + db.Satisfaction / 100.0 : 1.0;

        /// <summary>Growth stops on day 0 of a shortage.</summary>
        public static bool GrowthStopped(Entity colony) => colony.TryGetDataBlob<ColonyNeedsDB>(out var db) && db.ShortSince != null;

        /// <summary>Industry, mining and research: × 0.9 below satisfaction 20, × 1.05 above 80.</summary>
        public static double OutputFactor(Entity colony)
        {
            if (!colony.TryGetDataBlob<ColonyNeedsDB>(out var db))
                return 1.0;
            if (db.Satisfaction < 20) return 0.9;
            if (db.Satisfaction > 80) return 1.05;
            return 1.0;
        }

        /// <summary>People who want to leave: 5 % while starving (day 30 on) or below satisfaction 30.</summary>
        public static long Leaving(Entity colony)
        {
            if (!colony.TryGetDataBlob<ColonyNeedsDB>(out var db))
                return 0;
            bool starving = db.ShortSince != null && db.LastShortDay >= DeclineDay;
            if (!starving && db.Satisfaction >= 30)
                return 0;
            return (long)Math.Floor(People(colony) * LeavingShare);
        }

        /// <summary>One day of consumption, the shortage clock and (every 30 days) satisfaction.</summary>
        public static void RunDay(Entity colony, DateTime now)
        {
            Ensure(colony);
            if (!colony.TryGetDataBlob<ColonyNeedsDB>(out var db) || !colony.TryGetDataBlob<ColonyInfoDB>(out var info))
                return;
            // The hotloop may run less often than daily when time jumps: consume for the days elapsed (1 on the
            // first run, so a loaded save is never charged for time before it loaded; capped at 30).
            double elapsed = db.LastDay is DateTime last ? (now - last).TotalDays : 1;
            if (elapsed < 23.0 / 24)
                return;
            int days = (int)Math.Min(30, Math.Round(elapsed));
            db.LastDay = now;
            FarmDays(colony, days);

            long people = NavalAcademyCourse.PopulationOf(info);
            double needFood = SelfSupplied(colony) ? 0 : people * FoodPerPersonPerDay * days;
            double needWater = SelfSupplied(colony) ? 0 : people * WaterPerPersonPerDay * days;
            double gotFood = Draw(colony, FoodId, needFood);
            double gotWater = Draw(colony, WaterId, needWater);
            bool foodShort = gotFood + 0.5 < needFood;
            bool waterShort = gotWater + 0.5 < needWater;

            // Under 30 days of either: one warning until stock recovers.
            double? stockDays = MinDays(colony, needFood / days, needWater / days);
            if (stockDays is double dd && dd < WarnDays && db.ShortSince == null && !foodShort && !waterShort)
            {
                if (!db.LowStockWarned)
                {
                    db.LowStockWarned = true;
                    Publish(colony, EventType.SupplyWarning, now, $"{Name(colony)} has {Math.Floor(dd)} days of food and water left.");
                }
            }
            else if (stockDays is not double d2 || d2 >= WarnDays)
                db.LowStockWarned = false;

            if (foodShort || waterShort)
            {
                db.SuppliedDays = 0;
                if (db.ShortSince == null)
                {
                    db.ShortSince = now;
                    db.LastShortDay = -1;
                    ResetWindow(db);
                    Publish(colony, EventType.SupplyShortage, now,
                        $"{Name(colony)} is short of {(foodShort && waterShort ? "food and water" : foodShort ? "food" : "water")}. Growth has stopped.");
                }
            }
            else if (db.ShortSince != null)
            {
                db.SuppliedDays += days;
                if (db.SuppliedDays >= ResetDays)
                {
                    db.ShortSince = null;
                    db.SuppliedDays = 0;
                    db.LastShortDay = -1;
                    db.DeclineOwed = 0;
                    ResetWindow(db);
                }
            }

            if (db.ShortSince is DateTime since)
            {
                db.WindowFoodNeeded += needFood; db.WindowFoodSupplied += gotFood;
                db.WindowWaterNeeded += needWater; db.WindowWaterSupplied += gotWater;
                db.FoodShort |= foodShort; db.WaterShort |= waterShort;
                int day = Math.Max(0, (int)Math.Floor((now - since).TotalDays));
                for (int d = db.LastShortDay + 1; d <= day; d++)
                    Stage(colony, db, info, d, now);
                db.LastShortDay = day;
                if (day >= DeclineDay)
                    Decline(colony, db, info, now, Math.Min(days, day - DeclineDay + 1));
            }

            if (db.LastSatisfactionTick is not DateTime tick)
                db.LastSatisfactionTick = now;
            else if ((now - tick).TotalDays >= PopulationProcessor.TickDays - 0.01)
            {
                db.LastSatisfactionTick = now;
                db.SatisfactionTarget = Target(colony, db);
                db.Satisfaction = Math.Clamp(db.Satisfaction + 0.2 * (db.SatisfactionTarget - db.Satisfaction), 0, 100);
            }
        }

        /// <summary>Satisfaction target: 50, food short −20, water short −30, wages +5 / −15, overcrowded −15.</summary>
        public static double Target(Entity colony, ColonyNeedsDB db)
        {
            double t = 50;
            if (db.ShortSince != null && db.FoodShort) t -= 20;
            if (db.ShortSince != null && db.WaterShort) t -= 30;
            var game = colony.Manager?.Game;
            if (game != null && Pulsar4X.Factions.FactionSpending.PaysWages(game, colony.FactionOwnerID)
                && colony.TryGetDataBlob<ColonyWorkforceDB>(out var wf) && wf.Employed > 0
                && game.Factions.TryGetValue(colony.FactionOwnerID, out var faction)
                && faction.TryGetDataBlob<FactionInfoDB>(out var fi))
                t += fi.Money.GetCurrentFunds() < 0 ? -15 : 5;
            if (!SelfSupplied(colony) && colony.TryGetDataBlob<ColonyInfoDB>(out var info)
                && info.Population.Keys.Any(id => MigrationPlanner.Crowded(colony, id, includeLeaving: false) > 0))
                t -= 15;
            return Math.Clamp(t, 0, 100);
        }

        static void Stage(Entity colony, ColonyNeedsDB db, ColonyInfoDB info, int day, DateTime now)
        {
            if (day > 0 && day % StepDays == 0)
            {
                if (day >= DeathDay)
                    Deaths(colony, db, info, now, day);
                else if (day >= WarnEveryFromDay)
                    Publish(colony, EventType.SupplyWarning, now, $"{Name(colony)} has been short for {day} days. Deaths begin on day {DeathDay}.");
                else if (day == DeclineDay)
                    Publish(colony, EventType.SupplyWarning, now, $"{Name(colony)} has been short for {day} days. People are leaving.");
                ResetWindow(db);
            }
        }

        /// <summary>Unfed share over the last window, water counted double (a full water outage is 2 %/10 days).</summary>
        public static double UnfedShare(ColonyNeedsDB db)
        {
            double food = db.WindowFoodNeeded > 0 ? Math.Max(0, 1 - db.WindowFoodSupplied / db.WindowFoodNeeded) : 0;
            double water = db.WindowWaterNeeded > 0 ? Math.Max(0, 1 - db.WindowWaterSupplied / db.WindowWaterNeeded) : 0;
            return Math.Max(food, 2 * water);
        }

        static void Deaths(Entity colony, ColonyNeedsDB db, ColonyInfoDB info, DateTime now, int day)
        {
            long people = NavalAcademyCourse.PopulationOf(info);
            long dead = (long)Math.Floor(DeathRate * UnfedShare(db) * MedicalFactor(colony) * people);
            if (dead <= 0)
                return;
            Remove(info, dead);
            db.DeathsTotal += dead;
            Publish(colony, EventType.PopulationStarved, now, $"{dead} people died at {Name(colony)} after {day} days of shortage.");
        }

        static void Decline(Entity colony, ColonyNeedsDB db, ColonyInfoDB info, DateTime now, int days)
        {
            long people = NavalAcademyCourse.PopulationOf(info);
            db.DeclineOwed += people * (1 - Math.Pow(1 - DeclinePerMonth, days / PopulationProcessor.TickDays));
            long gone = (long)Math.Floor(db.DeclineOwed);
            if (gone <= 0)
                return;
            db.DeclineOwed -= gone;
            Remove(info, gone);
            db.LeftTotal += gone;
            int? day = ShortageDay(colony, now);
            if (day is int d && d % StepDays == 0)
                Publish(colony, EventType.PopulationLeft, now, $"People are leaving {Name(colony)}: {db.LeftTotal} gone so far.");
        }

        static void Remove(ColonyInfoDB info, long count)
        {
            while (count > 0)
            {
                int take = (int)Math.Min(int.MaxValue, count);
                NavalAcademyCourse.TakePeople(info, take);
                count -= take;
            }
            if (info.OwningEntity != null)
                Workforce.Recalc(info.OwningEntity);
        }

        static double Draw(Entity colony, string id, double need)
        {
            if (need <= 0)
                return 0;
            var cargo = Cargo(colony, id);
            if (cargo == null || !colony.TryGetDataBlob<CargoStorageDB>(out var storage))
                return 0;
            long want = (long)Math.Ceiling(need);
            long have = storage.GetUnitsStored(cargo, false);
            return storage.RemoveCargoByUnit(cargo, Math.Min(want, have));
        }

        static double? MinDays(Entity colony, double needFood, double needWater)
        {
            double? best = null;
            if (needFood > 0) best = Stock(colony, FoodId) / needFood;
            if (needWater > 0) { double w = Stock(colony, WaterId) / needWater; best = best is double b ? Math.Min(b, w) : w; }
            return best;
        }

        static void ResetWindow(ColonyNeedsDB db)
        {
            db.WindowFoodNeeded = db.WindowFoodSupplied = db.WindowWaterNeeded = db.WindowWaterSupplied = 0;
            db.FoodShort = db.WaterShort = false;
        }

        /// <summary>
        /// Farms grow food on their own: every agriculture line turns 1 kg water (and the 1 kWh
        /// its nameplate draws) into 1 kg food per point per day, scaled by infrastructure, life-support power, essential
        /// staffing and the satisfaction output factor, until the food row's Max (else 180 days of use, at least 30 t).
        /// Food is never queued as an industry job (the market pass skips it). Runs inside the needs pass so a background system (whose hotloops run
        /// less often) still grows the right amount for the days elapsed. Returns kg grown.
        /// </summary>
        public static long FarmDays(Entity colony, int days)
        {
            if (days <= 0 || !colony.TryGetDataBlob<Pulsar4X.Industry.IndustryAbilityDB>(out var industry))
                return 0;
            double points = 0;
            foreach (var line in industry.ProductionLines.Values)
            {
                if (line.IndustryTypeRates == null || !line.IndustryTypeRates.TryGetValue(AgricultureId, out var rate))
                    continue;
                // Food is the only agriculture recipe and the farms grow it here; a stray food job would double it.
                line.Jobs.RemoveAll(j => j.ItemGuid == FoodId);
                points += rate;
            }
            if (points <= 0)
                return 0;
            var food = Cargo(colony, FoodId);
            var water = Cargo(colony, WaterId);
            if (food == null || water == null || !colony.TryGetDataBlob<CargoStorageDB>(out var storage))
                return 0;
            double share = InfrastructureProcessor.GetEfficiency(colony)
                * Pulsar4X.Energy.ColonyPowerProcessor.Satisfaction(colony, Pulsar4X.Energy.PowerTier.LifeSupport)
                * Workforce.Share(colony, StaffTier.Essential)
                * OutputFactor(colony);
            long cap = FoodCap(colony);
            long stock = storage.GetUnitsStored(food, false);
            long grow = (long)Math.Floor(points * share * days);
            grow = Math.Min(grow, Math.Max(0, cap - stock));
            grow = Math.Min(grow, storage.GetUnitsStored(water, false));
            if (grow <= 0)
                return 0;
            long used = storage.RemoveCargoByUnit(water, grow);
            return storage.AddCargoByUnit(food, used);
        }

        /// <summary>Food stock farms grow up to: the food row's Max when set, else 180 days of use (at least 30 t).</summary>
        public static long FoodCap(Entity colony)
        {
            if (colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy) && policy.Rows.TryGetValue(FoodId, out var row) && row.Max > 0)
                return row.Max;
            return Math.Max(30_000, (long)Math.Ceiling(FoodPerDay(colony) * HoldDays));
        }

        static string Name(Entity colony) => colony.GetName(colony.FactionOwnerID);

        static void Publish(Entity colony, EventType type, DateTime now, string message)
            => EventManager.Instance.Publish(Event.Create(type, now, message, colony.FactionOwnerID, colony.Manager.ManagerID, colony.Id));
    }

    /// <summary>Daily food and water (multi-faction phase 7a). Keyed on the needs record.</summary>
    public class ColonyNeedsProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromDays(1);
        public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.25);
        public Type GetParameterType => typeof(ColonyNeedsDB);
        public void Init(Game game) { }

        public void ProcessEntity(Entity entity, int deltaSeconds) => ColonyNeeds.RunDay(entity, entity.StarSysDateTime);

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var colonies = manager.GetAllEntitiesWithDataBlob<ColonyNeedsDB>();
            foreach (var colony in colonies)
                ColonyNeeds.RunDay(colony, manager.StarSysDateTime);
            return colonies.Count;
        }
    }
}
