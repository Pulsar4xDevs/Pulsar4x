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

            _energyGenDB.LocalFuel = EnergyGenAbilityDB.SaneFuel(_energyGenDB.LocalFuel);

            // If the reactor runs dry inside this step, run up to the empty instant
            // with the reactor on, then the rest of the step on solar and the battery.
            if (dt > 0 && !_energyGenDB.ReactorOutOfFuel)
            {
                var (_, _, burn) = Step(_energyGenDB, dt);
                if (burn > _energyGenDB.LocalFuel)
                {
                    double tEmpty = dt * _energyGenDB.LocalFuel / burn;
                    DateTime emptyAt = _energyGenDB.dateTimeLastProcess + TimeSpan.FromSeconds(tEmpty);
                    if (emptyAt > _energyGenDB.dateTimeLastProcess && emptyAt < atDateTime)
                        Integrate(entity, _energyGenDB, emptyAt);
                    _energyGenDB.LocalFuel = 0;
                }
            }
            Integrate(entity, _energyGenDB, atDateTime);
        }

        /// <summary>
        /// Brings the blob up to the entity's current time before an install change,
        /// when it can be processed (an energy type and a store are set up).
        /// </summary>
        internal static void CatchUp(Entity entity, EnergyGenAbilityDB db)
        {
            if (entity.Manager == null || db.EnergyType == null)
                return;
            string id = db.EnergyType.UniqueID;
            if (!db.EnergyStored.ContainsKey(id) || !db.EnergyStoreMax.ContainsKey(id))
                return;
            EnergyGen(entity, entity.StarSysDateTime);
        }

        /// <summary>
        /// One step of dt seconds at the current demand and plant output.
        /// delta: kJ into (+) or out of (-) the store. load: (demand + charge rate) / max output,
        /// clamped 0..1. burn: reactor fuel in kg. Solar carries the load first, the reactor the rest.
        /// </summary>
        internal static (double delta, double load, double burn) Step(EnergyGenAbilityDB db, double dt)
        {
            string energyType = db.EnergyType.UniqueID;
            double stored = db.EnergyStored[energyType];
            double storeMax = db.EnergyStoreMax[energyType];
            double freestore = Math.Max(0, storeMax - stored);

            double maxOutput = db.TotalOutputMax;
            double rate = maxOutput - db.Demand;
            double delta = GeneralMath.Clamp(rate * dt, -stored, freestore);

            double chargeRate;
            if (dt > 0)
                chargeRate = Math.Max(0, delta) / dt;
            else
                chargeRate = rate > 0 && freestore > 0 ? rate : 0;

            double load = 0;
            if (maxOutput > 0)
                load = GeneralMath.Clamp((Math.Max(0, db.Demand) + chargeRate) / maxOutput, 0, 1);

            double burn = 0;
            double reactorMax = db.ReactorOutputMax;
            if (reactorMax > 0 && dt > 0)
            {
                double produced = load * maxOutput;
                double fromReactor = Math.Max(0, produced - db.MaxOutputFromSolar);
                double reactorLoad = Math.Min(1, fromReactor / reactorMax);
                burn = db.TotalFuelUseAtMax.maxUse * reactorLoad * dt;
            }
            return (delta, load, burn);
        }

        static void Integrate(Entity entity, EnergyGenAbilityDB _energyGenDB, DateTime atDateTime)
        {
            double dt = (atDateTime - _energyGenDB.dateTimeLastProcess).TotalSeconds;
            string energyType = _energyGenDB.EnergyType.UniqueID;
            var stored = _energyGenDB.EnergyStored[energyType];
            var storeMax = _energyGenDB.EnergyStoreMax[energyType];
            double freestore = Math.Max(0, storeMax - stored);

            double totaldemand = _energyGenDB.Demand + freestore;
            double rate = _energyGenDB.TotalOutputMax - _energyGenDB.Demand;
            var (delta, load, burn) = Step(_energyGenDB, dt);
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

            _energyGenDB.Load = load;
            _energyGenDB.Output = dt > 0 ? delta / dt : 0;
            _energyGenDB.LocalFuel = Math.Max(0, _energyGenDB.LocalFuel - burn);

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