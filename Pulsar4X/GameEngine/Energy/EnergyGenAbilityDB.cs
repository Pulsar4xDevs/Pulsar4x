using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Storage;

namespace Pulsar4X.Energy
{
    public class EnergyGenAbilityDB : BaseDataBlob
    {
        public DateTime dateTimeLastProcess;
        public ICargoable EnergyType;
        
        [JsonIgnore]
        public double TotalOutputMax 
        {
            get =>  ReactorOutputMax + MaxOutputFromSolar;
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
            Demand = db.Demand;
            Load = db.Load;
        }

        public override object Clone()
        {
            return new EnergyGenAbilityDB(this);
        }
    }
}