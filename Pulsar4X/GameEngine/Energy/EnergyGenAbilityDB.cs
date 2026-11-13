using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Storage;
using Pulsar4X.Extensions;

namespace Pulsar4X.Energy
{
    public class EnergyGenAbilityDB : BaseDataBlob
    {
        public DateTime dateTimeLastProcess;
        public ICargoable EnergyType;
        
        /// <summary>
        /// What the plant can give now, in kW: solar, decaying isotopes, the reactor while it has
        /// fuel and fuel cells while the hold has their propellant.
        /// </summary>
        [JsonIgnore]
        public double TotalOutputMax
        {
            get => ReactorOutputMax + MaxOutputFromSolar + DecayOutputMax + CellsOutputMax;
        }

        /// <summary>
        /// Reactor output the plant can give now, in kW. A plant that tracks fuel
        /// (TotalFuelUseAtMax.maxUse > 0) gives 0 once LocalFuel is spent.
        /// MaxOutputFromReactor keeps the installed nameplate.
        /// </summary>
        [JsonIgnore]
        public double ReactorOutputMax
        {
            get => ReactorOutOfFuel ? 0 : MaxOutputFromReactor;
        }

        [JsonIgnore]
        public bool ReactorOutOfFuel
        {
            get => TotalFuelUseAtMax.maxUse > 0 && !(LocalFuel > 0);
        }
        public double MaxOutputFromReactor = 0;
        public double MaxOutputFromSolar = 0;

        /// <summary>
        /// Fuel cells' nameplate output, kW. They burn <see cref="CellFuelUseAtMax"/> from the
        /// ship's hold, not built-in fuel, and give nothing once the hold has none left.
        /// </summary>
        public double MaxOutputFromCells = 0;

        /// <summary>The cells' propellant id and its burn at full cell output, kg/s.</summary>
        public (string type, double maxUse) CellFuelUseAtMax = ("", 0.0);

        /// <summary>Cell propellant burnt but not yet taken out of the hold (under one unit), kg.</summary>
        public double CellFuelDebt_kg;

        /// <summary>
        /// Isotope generators (RTGs): output when new, half-life and the date they started.
        /// Their output falls by half each half-life whatever the load.
        /// </summary>
        public List<DecaySource> DecaySources = new();

        /// <summary>
        /// Ship loads other than the warp drive (sensors, a construction bay), kW. They run on spare
        /// output only, once the store is full, so they never slow a warp or its charging.
        /// </summary>
        public double AuxDemand;

        /// <summary>Isotope output now (at the last process time), kW.</summary>
        [JsonIgnore]
        public double DecayOutputMax
        {
            get
            {
                double sum = 0;
                if (DecaySources != null)
                    foreach (var source in DecaySources)
                        sum += source.OutputAt(dateTimeLastProcess);
                return sum;
            }
        }

        /// <summary>Cell output the plant can give now, kW: 0 with no propellant in the hold.</summary>
        [JsonIgnore]
        public double CellsOutputMax
        {
            get => MaxOutputFromCells > 0 && CellFuelAvailable_kg > 0 ? MaxOutputFromCells : 0;
        }

        /// <summary>Cell propellant in the hold less what is already burnt, kg.</summary>
        [JsonIgnore]
        public double CellFuelAvailable_kg
        {
            get
            {
                var fuel = CellFuel;
                if (fuel == null || OwningEntity == null || !OwningEntity.TryGetDataBlob<CargoStorageDB>(out var cargo))
                    return 0;
                return Math.Max(0, cargo.GetUnitsStored(fuel, false) * fuel.MassPerUnit - Math.Max(0, CellFuelDebt_kg));
            }
        }

        [JsonIgnore]
        internal ICargoable? CellFuel
        {
            get
            {
                if (string.IsNullOrEmpty(CellFuelUseAtMax.type) || OwningEntity == null || OwningEntity.Manager == null)
                    return null;
                var lib = OwningEntity.GetFactionCargoDefinitions();
                return lib?.GetAny(CellFuelUseAtMax.type);
            }
        }

        /// <summary>
        /// The share of <see cref="AuxDemand"/> the plant serves now, kW: spare output with a full
        /// store, else nothing.
        /// </summary>
        [JsonIgnore]
        public double AuxServed
        {
            get
            {
                if (!(AuxDemand > 0) || EnergyType == null)
                    return 0;
                string id = EnergyType.UniqueID;
                if (!EnergyStored.TryGetValue(id, out double stored) || !EnergyStoreMax.TryGetValue(id, out double max))
                    return 0;
                double spare = TotalOutputMax - Demand;
                if (!(spare > 0) || max - stored > 1e-9)
                    return 0;
                return Math.Min(AuxDemand, spare);
            }
        }
        
        public (string type, double maxUse) TotalFuelUseAtMax = ("", 0.0);
        public double Demand { get; private set; }

        public List<EnergySolarGenerationAtb> SolarPanels { get; internal set; } = new();

        /// <summary>
        /// as a percentage of max output.
        /// </summary>
        public double Load { get; internal set; }

        /// <summary>
        /// In Kw
        /// </summary>
        public double Output { get; internal set; }
        internal void AddDemand(double demand, DateTime atDateTime)
        {
            if(OwningEntity != null)
                EnergyGenProcessor.EnergyGen(OwningEntity, atDateTime);
            Demand += demand;
        }

        /// <summary>
        /// In Kjoules
        /// </summary>
        public Dictionary<string, double > EnergyStored = new ();
        /// <summary>
        /// In Kjoules
        /// </summary>
        public Dictionary<string, double > EnergyStoreMax = new ();
        /// <summary>
        /// Reactor fuel left, in kg. The sum over installed reactors of FuelUsedAtMax * Lifetime.
        /// </summary>
        public double LocalFuel;

        /// <summary>
        /// Old saves can hold a negative, NaN or infinite LocalFuel. Negative and NaN become 0
        /// (spent) and +infinity becomes double.MaxValue.
        /// </summary>
        internal static double SaneFuel(double fuel)
        {
            if (double.IsNaN(fuel) || fuel < 0)
                return 0;
            if (double.IsPositiveInfinity(fuel))
                return double.MaxValue;
            return fuel;
        }

        /// <summary>
        /// The faction's energy cargo type. Electricity is not a starting unlock (it is not
        /// buildable), so a locked type is used too.
        /// </summary>
        internal static ICargoable? FindEnergyType(Entity parentEntity, string energyTypeID)
        {
            var data = parentEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data;
            return data.CargoGoods.GetAny(energyTypeID) ?? data.LockedCargoGoods.GetAny(energyTypeID);
        }

        /// <summary>
        /// Takes kJ of capacity off the store for energyTypeID (an uninstall). Stored energy
        /// above the new cap is lost.
        /// </summary>
        internal static void RemoveStore(EnergyGenAbilityDB genDB, string energyTypeID, double capacity)
        {
            if (!genDB.EnergyStoreMax.TryGetValue(energyTypeID, out double max))
                return;
            double newMax = Math.Max(0, max - capacity);
            genDB.EnergyStoreMax[energyTypeID] = newMax;
            if (genDB.EnergyStored.TryGetValue(energyTypeID, out double stored))
                genDB.EnergyStored[energyTypeID] = Math.Min(stored, newMax);
        }

        [OnDeserialized]
        private void ClampSavedFuel(StreamingContext context)
        {
            LocalFuel = SaneFuel(LocalFuel);
            if (double.IsNaN(TotalFuelUseAtMax.maxUse) || TotalFuelUseAtMax.maxUse < 0)
                TotalFuelUseAtMax = (TotalFuelUseAtMax.type, 0);
            // Saves from before energy phase 6 have none of these.
            DecaySources ??= new();
            if (!double.IsFinite(AuxDemand) || AuxDemand < 0)
                AuxDemand = 0;
            if (!double.IsFinite(CellFuelDebt_kg) || CellFuelDebt_kg < 0)
                CellFuelDebt_kg = 0;
        }

        // The setter resizes Histogram. Save the field so load does not run that.
        [JsonProperty]
        private int _histogramSize = 60;
        [JsonIgnore]
        public int HistogramSize
        {
            get { return _histogramSize;}
            set
            {
                if (_histogramSize > value)
                {
                    Histogram.RemoveRange(value, _histogramSize - value);
                    //Histogram.TrimExcess();
                    Histogram.Capacity = value;
                }
                else
                {
                    Histogram.Capacity = value;
                }
                _histogramSize = value;
            }
        }

        public int HistogramIndex = 0;
        public DateTime HistogramStartDate;
        public List<(double outputval, double demandval, double storval, int seconds)> Histogram = new List<(double, double, double, int)>(60);

        [JsonConstructor]
        private EnergyGenAbilityDB()
        {
        }

        public EnergyGenAbilityDB(DateTime gameTime)
        {
            HistogramStartDate = gameTime - TimeSpan.FromSeconds(_histogramSize);
            dateTimeLastProcess = gameTime;// - TimeSpan.FromSeconds(_histogramSize);
            
            for (int i = 0; i < _histogramSize; i++)
            {
                Histogram.Add((0,0,0,i));
            }
        }

        public EnergyGenAbilityDB(EnergyGenAbilityDB db)
        {
            Histogram = new List<(double outputval, double demandval, double storval, int seconds)>(db.Histogram);
            _histogramSize = db._histogramSize;
            HistogramStartDate = db.HistogramStartDate;
            dateTimeLastProcess = db.dateTimeLastProcess;
            EnergyType = db.EnergyType;
            EnergyStored = new Dictionary<string, double>(db.EnergyStored);
            EnergyStoreMax = new Dictionary<string, double>(db.EnergyStoreMax);
            MaxOutputFromReactor = db.MaxOutputFromReactor;
            MaxOutputFromSolar = db.MaxOutputFromSolar;
            TotalFuelUseAtMax = db.TotalFuelUseAtMax;
            LocalFuel = db.LocalFuel;
            MaxOutputFromCells = db.MaxOutputFromCells;
            CellFuelUseAtMax = db.CellFuelUseAtMax;
            CellFuelDebt_kg = db.CellFuelDebt_kg;
            DecaySources = new List<DecaySource>(db.DecaySources ?? new());
            AuxDemand = db.AuxDemand;
            Demand = db.Demand;
            Load = db.Load;
        }

        public override object Clone()
        {
            return new EnergyGenAbilityDB(this);
        }
    }

    /// <summary>One isotope generator: output when new (kW), half-life (s) and when it started.</summary>
    public class DecaySource
    {
        public double Output_kW;
        public double HalfLife_s;
        public DateTime Since;

        [JsonConstructor]
        public DecaySource() { }

        public DecaySource(double output_kW, double halfLife_s, DateTime since)
        {
            Output_kW = output_kW;
            HalfLife_s = halfLife_s;
            Since = since;
        }

        /// <summary>Output at <paramref name="at"/>: Output_kW x 0.5^(age / half-life). Full output before it started.</summary>
        public double OutputAt(DateTime at)
        {
            if (!(Output_kW > 0) || !double.IsFinite(Output_kW))
                return 0;
            if (!(HalfLife_s > 0))
                return Output_kW;
            double age = Math.Max(0, (at - Since).TotalSeconds);
            return Output_kW * Math.Pow(0.5, age / HalfLife_s);
        }
    }
}
