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
    /// which scales industry, mining and research. Batteries are not charged yet.
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

        /// <summary>Grid battery capacity, kJ. Batteries are not charged in phase 1.</summary>
        [JsonProperty]
        public double StoreMax_kJ { get; internal set; }

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

        public ColonyPowerDB() { }

        public ColonyPowerDB(ColonyPowerDB db)
        {
            EnergyTypeID = db.EnergyTypeID;
            GridEfficiency = db.GridEfficiency;
            Demand_kW = db.Demand_kW;
            Generation_kW = db.Generation_kW;
            StoreMax_kJ = db.StoreMax_kJ;
            LifeSupport_kW = db.LifeSupport_kW;
            LifeSupportColonists = db.LifeSupportColonists;
            Consumers = db.Consumers.Select(c => c with { }).ToList();
            Plants = db.Plants.Select(p => p with { }).ToList();
            DemandByTier = new Dictionary<PowerTier, double>(db.DemandByTier);
            TierSatisfaction = new Dictionary<PowerTier, double>(db.TierSatisfaction);
            FuelAtFull_kgPerDay = new Dictionary<string, double>(db.FuelAtFull_kgPerDay);
            LastRecalc = db.LastRecalc;
            Dispatched_kW = db.Dispatched_kW;
            FuelBurnt_kgLastDay = new Dictionary<string, double>(db.FuelBurnt_kgLastDay);
            FuelBurnDebt_kg = new Dictionary<string, double>(db.FuelBurnDebt_kg);
            LastDispatch = db.LastDispatch;
            Short = db.Short;
            FuelOut = new HashSet<string>(db.FuelOut);
        }

        public override object Clone() => new ColonyPowerDB(this);
    }

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
