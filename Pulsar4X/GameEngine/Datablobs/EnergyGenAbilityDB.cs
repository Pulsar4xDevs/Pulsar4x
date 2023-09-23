using System.Collections.Generic;
using Pulsar4X.Interfaces;
using System;
using Newtonsoft.Json;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
    public class EnergyGenAbilityDB : BaseDataBlob
    {
        public DateTime dateTimeLastProcess;
        public ICargoable EnergyType;
        public double TotalOutputMax = 0;

        public (string type, double maxUse) TotalFuelUseAtMax;

        public double Demand { get; private set; }


        /// <summary>
        /// as a percentage of max output.
        /// </summary>
        public double Load { get; internal set; }

        /// <summary>
        /// In Kw
        /// </summary>
        public double Output { get; internal set; }
        public void AddDemand(double demand, DateTime atDateTime)
        {
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

            Random rng = new Random();
            for (int i = 0; i < _histogramSize; i++)
            {
                /*
                double o = rng.Next(0, 50);
                double d = rng.Next(0, 50);
                double s = rng.Next(0, 50);
                int lastt = 0;
                if(i > 0)
                    lastt = Histogram[i - 1].seconds;
                int t = rng.Next(lastt, lastt + 60);
                Histogram.Add((o,d,s,t));
                */
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