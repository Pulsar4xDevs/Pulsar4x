using System;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;

namespace Pulsar4X.Energy
{
    public class EnergyGenProcessor : IInstanceProcessor
    {

        public static void EnergyGen(Entity entity, DateTime atDateTime)
        {
            EnergyGenAbilityDB _energyGenDB = entity.GetDataBlob<EnergyGenAbilityDB>();

            // Kilowatts are kilojoules per second. Warp books the bubble by rewinding
            // one second and integrating forward with the creation demand set, so a
            // backwards call only moves the mark. A zero step does not add energy;
            // AddDemand processes the current instant before it changes the demand.
            double dt = (atDateTime - _energyGenDB.dateTimeLastProcess).TotalSeconds;
            if (dt < 0)
            {
                _energyGenDB.dateTimeLastProcess = atDateTime;
                return;
            }

            string energyType = _energyGenDB.EnergyType.UniqueID;
            var stored = _energyGenDB.EnergyStored[energyType];
            var storeMax = _energyGenDB.EnergyStoreMax[energyType];
            double freestore = Math.Max(0, storeMax - stored);

            double totaldemand = _energyGenDB.Demand + freestore;
            double rate = _energyGenDB.TotalOutputMax - _energyGenDB.Demand;
            double delta = GeneralMath.Clamp(rate * dt, -stored, freestore);
            _energyGenDB.EnergyStored[energyType] = stored + delta;

            double nowStored = stored + delta;
            double nowFree = Math.Max(0, storeMax - nowStored);
            if (rate > 0 && nowFree > 0)
            {
                double timeToFill = Math.Max(1, Math.Ceiling(nowFree / rate));
                DateTime interuptTime = atDateTime + TimeSpan.FromSeconds(timeToFill);
                entity.Manager.ManagerSubpulses.AddEntityInterupt(interuptTime, nameof(EnergyGenProcessor), entity);
            }
            else if (rate < 0 && nowStored > 0)
            {
                double timeToEmpty = Math.Max(1, Math.Ceiling(nowStored / Math.Abs(rate)));
                DateTime interuptTime = atDateTime + TimeSpan.FromSeconds(timeToEmpty);
                entity.Manager.ManagerSubpulses.AddEntityInterupt(interuptTime, nameof(EnergyGenProcessor), entity);
            }

            double load = 0;
            if (delta > 0)
                load = _energyGenDB.TotalOutputMax / rate;
            else if (delta < 0)
                load = 1;
            _energyGenDB.Load = load;
            _energyGenDB.Output = dt > 0 ? delta / dt : 0;
            double fueluse = _energyGenDB.TotalFuelUseAtMax.maxUse * load;
            _energyGenDB.LocalFuel -= fueluse * dt;

            _energyGenDB.dateTimeLastProcess = atDateTime;

            var histogram = _energyGenDB.Histogram;
            int hgFirstIdx = _energyGenDB.HistogramIndex;
            int hgLastIdx;
            if (hgFirstIdx == 0)
                hgLastIdx = histogram.Count - 1;
            else
                hgLastIdx = hgFirstIdx - 1;

            var hgFirstObj = histogram[hgFirstIdx];
            var hgLastObj = histogram[hgLastIdx];
            int optime = hgLastObj.seconds;

            int newoptime = (int)(optime + dt);

            var nexval = (foo: _energyGenDB.Output, demand: totaldemand, store: stored, newoptime);

            if(histogram.Count < _energyGenDB.HistogramSize)
                histogram.Add(nexval);
            else
            {
                histogram[hgFirstIdx] = nexval;
                if (hgFirstIdx == histogram.Count - 1)
                    _energyGenDB.HistogramIndex = 0;
                else
                {
                    _energyGenDB.HistogramIndex++;
                }
            }
        }


        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            EnergyGen(entity, atDateTime);
        }
    }
}