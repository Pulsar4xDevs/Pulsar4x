using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine
{
    public class EnergyGenProcessor : IInstanceProcessor
    {

        public static void EnergyGen(Entity entity, DateTime atDateTime)
        {
            EnergyGenAbilityDB _energyGenDB = entity.GetDataBlob<EnergyGenAbilityDB>();

            TimeSpan t = atDateTime - _energyGenDB.dateTimeLastProcess;

            string energyType = _energyGenDB.EnergyType.UniqueID;
            var stored = _energyGenDB.EnergyStored[energyType];
            var storeMax = _energyGenDB.EnergyStoreMax[energyType];
            double freestore = Math.Max(0, storeMax - stored);

            double totaldemand = _energyGenDB.Demand + freestore;

            var output = _energyGenDB.TotalOutputMax - _energyGenDB.Demand;

            output = GeneralMath.Clamp(output, -stored, freestore);
            _energyGenDB.EnergyStored[energyType] += output;

            if (output > 0)
            {
                double timeToFill = Math.Ceiling( freestore / output);
                DateTime interuptTime = atDateTime + TimeSpan.FromSeconds(timeToFill);
                entity.Manager.ManagerSubpulses.AddEntityInterupt(interuptTime, nameof(EnergyGenProcessor), entity);
            }
            else if (output < 0)
            {
                double timeToEmpty = Math.Ceiling( Math.Abs(stored / output));
                DateTime interuptTime = atDateTime + TimeSpan.FromSeconds(timeToEmpty);
                entity.Manager.ManagerSubpulses.AddEntityInterupt(interuptTime, nameof(EnergyGenProcessor), entity);
            }


            double load = 0;
            if (output > 0)
            {
                load = _energyGenDB.TotalOutputMax / output;
            }
            else if (output < 0)
            {
                load = 1;
            }
            _energyGenDB.Load = load;
            _energyGenDB.Output = output;
            double fueluse = _energyGenDB.TotalFuelUseAtMax.maxUse * load;
            _energyGenDB.LocalFuel -= fueluse * t.TotalSeconds;

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

            int newoptime = (int)(optime + t.TotalSeconds);

            var nexval = (foo: output, demand: totaldemand, store: stored, newoptime);

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