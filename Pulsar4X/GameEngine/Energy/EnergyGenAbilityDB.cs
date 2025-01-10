using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.Storage;

namespace Pulsar4X.Energy
{
    public class EnergyGenAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public DateTime dateTimeLastProcess;
        [JsonProperty]
        public ICargoable EnergyType;
        [JsonProperty]
        public double TotalOutputMax = 0;
        [JsonProperty]
        public (string type, double maxUse) TotalFuelUseAtMax;
        [JsonProperty]
        public double Demand { get; private set; }


        /// <summary>
        /// as a percentage of max output.
        /// </summary>
        /// [JsonProperty]
        public double Load { get; internal set; }

        /// <summary>
        /// In Kw
        /// </summary>
        /// [JsonProperty]
        public double Output { get; internal set; }
        public void AddDemand(double demand, DateTime atDateTime)
        {
            if(OwningEntity != null)
                EnergyGenProcessor.EnergyGen(OwningEntity, atDateTime);
            Demand += demand;
        }

        /// <summary>
        /// In Kjoules
        /// </summary>
        [JsonProperty]
        public Dictionary<string, double > EnergyStored = new ();
        /// <summary>
        /// In Kjoules
        /// </summary>
        [JsonProperty]
        public Dictionary<string, double > EnergyStoreMax = new ();
        [JsonProperty]
        public double LocalFuel;

        private int _histogramSize = 60;
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
        [JsonProperty]
        public DateTime HistogramStartDate;
        [JsonProperty]
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
            TotalOutputMax = db.TotalOutputMax;
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