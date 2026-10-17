using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
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
            get =>  MaxOutputFromReactor + MaxOutputFromSolar;
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
        public double LocalFuel;

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
            Demand = db.Demand;
            Load = db.Load;
        }

        public override object Clone()
        {
            return new EnergyGenAbilityDB(this);
        }
    }
}