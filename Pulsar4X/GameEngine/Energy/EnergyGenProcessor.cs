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

            // A ship with loads (sensors) or batteries but no generator has nothing to run yet.
            if (_energyGenDB.EnergyType == null
                || !_energyGenDB.EnergyStored.ContainsKey(_energyGenDB.EnergyType.UniqueID)
                || !_energyGenDB.EnergyStoreMax.ContainsKey(_energyGenDB.EnergyType.UniqueID))
            {
                _energyGenDB.dateTimeLastProcess = atDateTime;
                return;
            }

            _energyGenDB.LocalFuel = EnergyGenAbilityDB.SaneFuel(_energyGenDB.LocalFuel);

            // If the reactor (or the fuel cells' propellant) runs dry inside this step, run up
            // to the empty instant with it on, then the rest of the step without it. The empty
            // instant accounts for the store filling or emptying in the same step. Both can run
            // out in one step, so up to two passes.
            for (int pass = 0; pass < 2 && dt > 0; pass++)
            {
                double left = (atDateTime - _energyGenDB.dateTimeLastProcess).TotalSeconds;
                if (!(left > 0))
                    break;
                double tReactor = _energyGenDB.ReactorOutOfFuel ? double.PositiveInfinity : FuelOutTime(_energyGenDB, left);
                double tCells = _energyGenDB.CellsOutputMax > 0 ? CellFuelOutTime(_energyGenDB, left) : double.PositiveInfinity;
                double tEmpty = Math.Min(tReactor, tCells);
                if (!(tEmpty < left))
                    break;
                DateTime emptyAt = _energyGenDB.dateTimeLastProcess + TimeSpan.FromSeconds(tEmpty);
                if (emptyAt > _energyGenDB.dateTimeLastProcess && emptyAt < atDateTime)
                    Integrate(entity, _energyGenDB, emptyAt);
                if (tReactor <= tCells)
                    _energyGenDB.LocalFuel = 0;
                else
                    EmptyCellFuel(entity, _energyGenDB);
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
        /// A step has at most two phases. Phase A runs until the store fills (charging) or
        /// empties (draining), at full output. Phase B follows the demand with a full store,
        /// or stays at full output with an empty one. tA is phase A's length in seconds (0 if
        /// none); pA and pB are the kW produced in each phase.
        /// </summary>
        internal static (double tA, double pA, double pB) Phases(EnergyGenAbilityDB db)
        {
            string energyType = db.EnergyType.UniqueID;
            double stored = db.EnergyStored[energyType];
            double storeMax = db.EnergyStoreMax[energyType];
            double freestore = Math.Max(0, storeMax - stored);
            double maxOutput = db.TotalOutputMax;
            double rate = maxOutput - db.Demand;

            // A full store also serves the ship's other loads from the spare output.
            double pB = rate >= 0 ? Math.Max(0, db.Demand) + Math.Min(Math.Max(0, db.AuxDemand), rate) : maxOutput;
            if (rate > 0 && freestore > 0)
                return (freestore / rate, maxOutput, pB);
            if (rate < 0 && stored > 0)
                return (stored / -rate, maxOutput, pB);
            return (0, pB, pB);
        }

        /// <summary>
        /// Reactor fuel burn in kg/s while the plant produces this many kW.
        /// Solar carries the load first, the reactor the rest.
        /// </summary>
        internal static double BurnRate(EnergyGenAbilityDB db, double producedKw)
            => BurnRates(db, producedKw).reactor;

        /// <summary>
        /// Fuel burn in kg/s while the plant produces this many kW. Solar and isotopes carry the
        /// load first (they cost nothing to run), then the reactor, then the fuel cells.
        /// reactor: built-in fuel; cells: propellant from the hold.
        /// </summary>
        internal static (double reactor, double cells) BurnRates(EnergyGenAbilityDB db, double producedKw)
        {
            double rest = Math.Max(0, producedKw - db.MaxOutputFromSolar - db.DecayOutputMax);
            double reactorMax = db.ReactorOutputMax;
            double reactor = 0;
            if (reactorMax > 0)
            {
                double fromReactor = Math.Min(rest, reactorMax);
                reactor = db.TotalFuelUseAtMax.maxUse * (fromReactor / reactorMax);
                rest -= fromReactor;
            }
            double cells = 0;
            double cellsMax = db.CellsOutputMax;
            if (cellsMax > 0 && rest > 0)
                cells = db.CellFuelUseAtMax.maxUse * Math.Min(1, rest / cellsMax);
            return (reactor, cells);
        }

        /// <summary>Seconds into a dt step at which the cells' propellant in the hold runs out, or +infinity.</summary>
        internal static double CellFuelOutTime(EnergyGenAbilityDB db, double dt)
        {
            double fuel = db.CellFuelAvailable_kg;
            var (tA, pA, pB) = Phases(db);
            double a = Math.Min(tA, dt);
            double rA = BurnRates(db, pA).cells;
            double rB = BurnRates(db, pB).cells;
            if (rA * a >= fuel)
                return rA > 0 ? fuel / rA : double.PositiveInfinity;
            return rB > 0 ? a + (fuel - rA * a) / rB : double.PositiveInfinity;
        }

        /// <summary>Takes burnt cell propellant out of the hold, whole units at a time; the rest stays as a debt.</summary>
        internal static void TakeCellFuel(Entity entity, EnergyGenAbilityDB db, double kg)
        {
            if (!(kg > 0) || !double.IsFinite(kg))
                return;
            db.CellFuelDebt_kg = Math.Max(0, db.CellFuelDebt_kg) + kg;
            var fuel = db.CellFuel;
            if (fuel == null || !(fuel.MassPerUnit > 0) || !entity.TryGetDataBlob<Storage.CargoStorageDB>(out var cargo))
                return;
            long units = (long)Math.Floor(db.CellFuelDebt_kg / fuel.MassPerUnit);
            if (units <= 0)
                return;
            long removed = Storage.CargoMath.RemoveCargoByUnit(cargo, fuel, units);
            db.CellFuelDebt_kg = removed < units ? 0 : db.CellFuelDebt_kg - removed * fuel.MassPerUnit;
            if (removed > 0)
                Storage.CargoTransferProcessor.UpdateMassFuelAndDeltaV(entity);
        }

        /// <summary>The cells ran dry: whatever is left of the propellant (under a unit) is spent.</summary>
        static void EmptyCellFuel(Entity entity, EnergyGenAbilityDB db)
        {
            var fuel = db.CellFuel;
            if (fuel != null && entity.TryGetDataBlob<Storage.CargoStorageDB>(out var cargo))
            {
                long units = Storage.CargoMath.GetUnitsStored(cargo, fuel, false);
                if (units > 0 && Storage.CargoMath.RemoveCargoByUnit(cargo, fuel, units) > 0)
                    Storage.CargoTransferProcessor.UpdateMassFuelAndDeltaV(entity);
            }
            db.CellFuelDebt_kg = 0;
        }

        /// <summary>
        /// Seconds into a dt step at which LocalFuel runs out, or +infinity if it lasts.
        /// </summary>
        internal static double FuelOutTime(EnergyGenAbilityDB db, double dt)
        {
            double fuel = db.LocalFuel;
            var (tA, pA, pB) = Phases(db);
            double a = Math.Min(tA, dt);
            double rA = BurnRate(db, pA);
            double rB = BurnRate(db, pB);
            if (rA * a >= fuel)
                return rA > 0 ? fuel / rA : double.PositiveInfinity;
            return rB > 0 ? a + (fuel - rA * a) / rB : double.PositiveInfinity;
        }

        /// <summary>
        /// One step of dt seconds at the current demand and plant output.
        /// delta: kJ into (+) or out of (-) the store. load: (demand + charge rate) / max output,
        /// clamped 0..1, averaged over the step. burn: reactor fuel in kg.
        /// </summary>
        internal static (double delta, double load, double burn) Step(EnergyGenAbilityDB db, double dt)
        {
            var (delta, load, burn, _) = StepWithCells(db, dt);
            return (delta, load, burn);
        }

        /// <summary><see cref="Step"/> plus the fuel cells' propellant burn in kg.</summary>
        internal static (double delta, double load, double burn, double cellBurn) StepWithCells(EnergyGenAbilityDB db, double dt)
        {
            string energyType = db.EnergyType.UniqueID;
            double stored = db.EnergyStored[energyType];
            double storeMax = db.EnergyStoreMax[energyType];
            double freestore = Math.Max(0, storeMax - stored);

            double maxOutput = db.TotalOutputMax;
            double rate = maxOutput - db.Demand;
            double delta = GeneralMath.Clamp(rate * dt, -stored, freestore);

            var (tA, pA, pB) = Phases(db);
            double produced;
            double burn = 0;
            double cellBurn = 0;
            if (dt > 0)
            {
                double a = Math.Min(tA, dt);
                produced = (pA * a + pB * (dt - a)) / dt;
                var rA = BurnRates(db, pA);
                var rB = BurnRates(db, pB);
                burn = rA.reactor * a + rB.reactor * (dt - a);
                cellBurn = rA.cells * a + rB.cells * (dt - a);
            }
            else
            {
                produced = tA > 0 ? pA : pB;
            }

            double load = 0;
            if (maxOutput > 0)
                load = GeneralMath.Clamp(produced / maxOutput, 0, 1);
            return (delta, load, burn, cellBurn);
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
            var (delta, load, burn, cellBurn) = StepWithCells(_energyGenDB, dt);
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
            TakeCellFuel(entity, _energyGenDB, cellBurn);

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