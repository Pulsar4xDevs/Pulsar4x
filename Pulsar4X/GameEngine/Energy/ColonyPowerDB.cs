using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Energy
{
    /// <summary>
    /// A colony's power grid: the daily-rate side of the energy model (ships run the same
    /// kW/kJ model per second in <see cref="EnergyGenAbilityDB"/>). Energy type and units are
    /// shared: kW for power, kJ for storage, 'electricity' as the energy type.
    /// The daily pass burns plant fuel from the colony stockpile and sets per-tier satisfaction,
    /// which scales industry, mining and research. Grid batteries charge from the day's spare
    /// and cover a shortfall first.
    /// </summary>
    public class ColonyPowerDB : BaseDataBlob
    {
        /// <summary>Grid transmission efficiency (§4 "Transmit"). Delivered = generated × this.</summary>
        public const double DefaultGridEfficiency = 0.92;

        [JsonProperty]
        public string EnergyTypeID { get; internal set; } = "electricity";

        [JsonProperty]
        public double GridEfficiency { get; internal set; } = DefaultGridEfficiency;

        /// <summary>Total nameplate demand, kW, life support included.</summary>
        [JsonProperty]
        public double Demand_kW { get; internal set; }

        /// <summary>Electric generation available now (before grid loss), kW.</summary>
        [JsonProperty]
        public double Generation_kW { get; internal set; }

        /// <summary>Generation after grid loss, kW.</summary>
        [JsonIgnore]
        public double Delivered_kW => Generation_kW * GridEfficiency;

        /// <summary>Delivered minus demand, kW. Negative is a shortfall.</summary>
        [JsonIgnore]
        public double Balance_kW => Delivered_kW - Demand_kW;

        /// <summary>Grid battery capacity, kJ (installed batteries x health).</summary>
        [JsonProperty]
        public double StoreMax_kJ { get; internal set; }

        /// <summary>Energy in the grid batteries, kJ, 0..<see cref="StoreMax_kJ"/>. Charged and drawn by the daily pass.</summary>
        [JsonProperty]
        public double Stored_kJ { get; internal set; }

        /// <summary>Highest power the batteries take in, kW (grid side, before the charge loss). Set by Recalc.</summary>
        [JsonProperty]
        public double MaxCharge_kW { get; internal set; }

        /// <summary>Highest power the batteries give out, kW. Set by Recalc.</summary>
        [JsonProperty]
        public double MaxDischarge_kW { get; internal set; }

        /// <summary>
        /// Share of the charging power the batteries keep, 0..1 (capacity-weighted). The whole
        /// round-trip loss is taken while charging; discharge is lossless. Set by Recalc.
        /// </summary>
        [JsonProperty]
        public double ChargeEfficiency { get; internal set; } = 1;

        /// <summary>
        /// The batteries' average rate over the last daily pass, kJ/s: + charging (energy stored,
        /// after loss), - discharging.
        /// </summary>
        [JsonProperty]
        public double Battery_kW { get; internal set; }

        /// <summary>Average power the batteries took from the grid in the last daily pass, kW (before loss).</summary>
        [JsonProperty]
        public double BatteryCharge_kW { get; internal set; }

        /// <summary>Average power the batteries gave the grid in the last daily pass, kW.</summary>
        [JsonProperty]
        public double BatteryDischarge_kW { get; internal set; }

        [JsonProperty]
        public double LifeSupport_kW { get; internal set; }

        [JsonProperty]
        public long LifeSupportColonists { get; internal set; }

        [JsonProperty]
        public List<ColonyPowerConsumer> Consumers { get; internal set; } = new();

        [JsonProperty]
        public List<ColonyPowerPlant> Plants { get; internal set; } = new();

        /// <summary>Demand per tier, kW.</summary>
        [JsonProperty]
        public Dictionary<PowerTier, double> DemandByTier { get; internal set; } = new();

        /// <summary>
        /// What each tier gets, 0..1, with delivered power shared out by priority.
        /// Industry, mining and research output is multiplied by their tier's value.
        /// </summary>
        [JsonProperty]
        public Dictionary<PowerTier, double> TierSatisfaction { get; internal set; } = new();

        /// <summary>Fuel each fuel's plants would burn per day at full output, kg/day, by fuel id.</summary>
        [JsonProperty]
        public Dictionary<string, double> FuelAtFull_kgPerDay { get; internal set; } = new();

        [JsonProperty]
        public DateTime LastRecalc { get; internal set; }

        /// <summary>
        /// Idle/working state of the colony's mines, labs and construction offices at the last recalc
        /// (<see cref="ColonyPowerProcessor.IdleStateKey"/>), so a daily pass only refreshes demand
        /// when one flipped. Not saved: an empty key after a load just costs one recalc.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        internal string IdleStateKey { get; set; } = "";

        /// <summary>Average generation over the last daily pass, kW (before grid loss).</summary>
        [JsonProperty]
        public double Dispatched_kW { get; internal set; }

        /// <summary>Fuel burnt in the last daily pass, kg, by fuel id.</summary>
        [JsonProperty]
        public Dictionary<string, double> FuelBurnt_kgLastDay { get; internal set; } = new();

        /// <summary>Part-units of fuel already burnt but not yet taken from the whole-unit stockpile, kg.</summary>
        [JsonProperty]
        public Dictionary<string, double> FuelBurnDebt_kg { get; internal set; } = new();

        [JsonProperty]
        public DateTime LastDispatch { get; internal set; }

        /// <summary>True while some tier is short; a shortage event is raised when this turns on.</summary>
        [JsonProperty]
        public bool Short { get; internal set; }

        /// <summary>Fuels that have run out and already raised their event.</summary>
        [JsonProperty]
        public HashSet<string> FuelOut { get; internal set; } = new();

        /// <summary>Days of history kept. The oldest sample drops when a new day is added.</summary>
        public const int HistoryDays = 90;

        /// <summary>
        /// One sample per daily pass, oldest first, at most <see cref="HistoryDays"/>.
        /// Saved with the blob. A save from before this field loads with an empty history.
        /// </summary>
        [JsonProperty]
        public List<ColonyPowerSample> History { get; internal set; } = new();

        /// <summary>
        /// Adds a day's sample. A second pass on the same day replaces that day's sample,
        /// so the history stays one sample per day. When the new sample is more than a day after
        /// the last one (a background system runs its daily pass less often), each missing day is
        /// filled with a sample linearly interpolated between the two. This is for display only.
        /// Only the fills that the <see cref="HistoryDays"/> cap keeps are computed.
        /// </summary>
        internal void RecordDay(ColonyPowerSample sample)
        {
            // A save that wrote "History": null loads as null. Treat it as empty.
            History ??= new List<ColonyPowerSample>();
            if (History.Count > 0)
            {
                var last = History[History.Count - 1];
                int gapDays = (sample.Date.Date - last.Date.Date).Days;
                if (gapDays == 0)
                {
                    History[History.Count - 1] = sample;
                    return;
                }
                if (gapDays > 1)
                {
                    // Fill days 1..gapDays-1 after the last sample. The new sample takes one slot, so
                    // at most HistoryDays - 1 fills survive the cap: skip the older ones, and if any
                    // are skipped, every existing sample would be dropped too.
                    int firstFill = Math.Max(1, gapDays - (HistoryDays - 1));
                    if (firstFill > 1)
                        History.Clear();
                    for (int k = firstFill; k < gapDays; k++)
                    {
                        double t = (double)k / gapDays;
                        History.Add(new ColonyPowerSample(
                            last.Date.AddDays(k),
                            Lerp(last.Demand_kW, sample.Demand_kW, t),
                            Lerp(last.Generation_kW, sample.Generation_kW, t),
                            Lerp(last.Delivered_kW, sample.Delivered_kW, t),
                            Lerp(last.Stored_kJ, sample.Stored_kJ, t),
                            Lerp(last.Charge_kW, sample.Charge_kW, t),
                            Lerp(last.Discharge_kW, sample.Discharge_kW, t)));
                    }
                }
            }
            History.Add(sample);
            if (History.Count > HistoryDays)
                History.RemoveRange(0, History.Count - HistoryDays);
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        public ColonyPowerDB() { }

        public ColonyPowerDB(ColonyPowerDB db)
        {
            EnergyTypeID = db.EnergyTypeID;
            GridEfficiency = db.GridEfficiency;
            Demand_kW = db.Demand_kW;
            Generation_kW = db.Generation_kW;
            StoreMax_kJ = db.StoreMax_kJ;
            Stored_kJ = db.Stored_kJ;
            MaxCharge_kW = db.MaxCharge_kW;
            MaxDischarge_kW = db.MaxDischarge_kW;
            ChargeEfficiency = db.ChargeEfficiency;
            Battery_kW = db.Battery_kW;
            BatteryCharge_kW = db.BatteryCharge_kW;
            BatteryDischarge_kW = db.BatteryDischarge_kW;
            LifeSupport_kW = db.LifeSupport_kW;
            LifeSupportColonists = db.LifeSupportColonists;
            Consumers = db.Consumers.Select(c => c with { }).ToList();
            Plants = db.Plants.Select(p => p with { }).ToList();
            DemandByTier = new Dictionary<PowerTier, double>(db.DemandByTier);
            TierSatisfaction = new Dictionary<PowerTier, double>(db.TierSatisfaction);
            FuelAtFull_kgPerDay = new Dictionary<string, double>(db.FuelAtFull_kgPerDay);
            LastRecalc = db.LastRecalc;
            IdleStateKey = db.IdleStateKey;
            Dispatched_kW = db.Dispatched_kW;
            FuelBurnt_kgLastDay = new Dictionary<string, double>(db.FuelBurnt_kgLastDay);
            FuelBurnDebt_kg = new Dictionary<string, double>(db.FuelBurnDebt_kg);
            LastDispatch = db.LastDispatch;
            Short = db.Short;
            FuelOut = new HashSet<string>(db.FuelOut);
            History = db.History == null ? new List<ColonyPowerSample>() : new List<ColonyPowerSample>(db.History);
        }

        public override object Clone() => new ColonyPowerDB(this);
    }

    /// <summary>
    /// One day of the grid: nameplate demand, generation available before grid loss, power actually
    /// delivered after grid loss, and energy held in grid batteries at the end of the day, with
    /// the day's average battery charge (taken from the grid, before loss) and discharge (given
    /// to the grid). Power in kW, storage in kJ. Samples from before battery charging read 0 for both.
    /// </summary>
    public sealed record ColonyPowerSample(DateTime Date, double Demand_kW, double Generation_kW, double Delivered_kW, double Stored_kJ,
        double Charge_kW = 0, double Discharge_kW = 0);

    /// <summary>
    /// One consumer line: a design (or life support) and its total draw. A production facility draws
    /// what its queued jobs' build energy needs, capped at <see cref="Nameplate_kW"/>; every other line
    /// draws its nameplate. Nameplate_kW 0 means "same as Draw_kW".
    /// </summary>
    public sealed record ColonyPowerConsumer(string DesignId, string Name, PowerTier Tier, int Count, double Draw_kW,
        double Nameplate_kW = 0);

    public enum ColonyPowerPlantKind
    {
        Fuel,
        Solar,
        Battery,
    }

    /// <summary>One generation line: a design and what it gives now.</summary>
    public sealed record ColonyPowerPlant(
        string DesignId,
        string Name,
        ColonyPowerPlantKind Kind,
        int Count,
        double Output_kW,
        double Nameplate_kW,
        string? FuelId,
        double FuelAtFull_kgPerDay,
        double Store_kJ,
        string? Note,
        double FuelKgPerKJ = 0);
}
