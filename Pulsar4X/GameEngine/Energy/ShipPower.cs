using System;
using Pulsar4X.Engine;

namespace Pulsar4X.Energy
{
    /// <summary>
    /// Energy for a ship's work loads (energy phase 6), such as the asteroid miner.
    /// </summary>
    public static class ShipPower
    {
        /// <summary>
        /// Draws up to <paramref name="kJ"/> spread over the last <paramref name="seconds"/>, ending at
        /// <paramref name="at"/>, and returns the kJ delivered.
        /// The plant's spare output over that span goes first (warp and the ship's other loads keep
        /// theirs) and burns the matching reactor or fuel-cell fuel; the batteries cover the rest (up to
        /// their discharge rate over the span) and recharge afterwards. No plant or store: nothing.
        /// The span is treated as steady (store full, today's demand), which is what a parked miner is.
        /// </summary>
        public static double DrawEnergy(Entity ship, double kJ, double seconds, DateTime at)
        {
            if (!(kJ > 0) || !double.IsFinite(kJ))
                return 0;
            if (!ship.TryGetDataBlob<EnergyGenAbilityDB>(out var db) || db.EnergyType == null)
                return 0;
            string id = db.EnergyType.UniqueID;
            if (!db.EnergyStored.ContainsKey(id) || !db.EnergyStoreMax.ContainsKey(id))
                return 0;
            if (ship.Manager != null && at > db.dateTimeLastProcess)
                EnergyGenProcessor.EnergyGen(ship, at);

            double delivered = 0;
            if (seconds > 0 && double.IsFinite(seconds))
            {
                double baseKw = Math.Max(0, db.Demand) + db.AuxServed;
                double spareKw = Math.Max(0, db.TotalOutputMax - baseKw);
                double fromPlant = Math.Min(kJ, spareKw * seconds);
                if (fromPlant > 0)
                {
                    var before = EnergyGenProcessor.BurnRates(db, baseKw);
                    var after = EnergyGenProcessor.BurnRates(db, baseKw + fromPlant / seconds);
                    double reactorKg = Math.Max(0, after.reactor - before.reactor) * seconds;
                    double cellKg = Math.Max(0, after.cells - before.cells) * seconds;
                    // Not enough fuel for all of it: deliver the share the fuel covers.
                    double share = 1;
                    if (reactorKg > db.LocalFuel)
                        share = Math.Min(share, db.LocalFuel / reactorKg);
                    double cellFuel = db.CellFuelAvailable_kg;
                    if (cellKg > cellFuel)
                        share = Math.Min(share, cellFuel / cellKg);
                    share = Math.Max(0, share);
                    db.LocalFuel = Math.Max(0, db.LocalFuel - reactorKg * share);
                    EnergyGenProcessor.TakeCellFuel(ship, db, cellKg * share);
                    delivered += fromPlant * share;
                }
            }

            double rest = kJ - delivered;
            double stored = db.EnergyStored[id];
            if (rest > 0 && stored > 0)
            {
                // The batteries give at most their discharge rate over the span.
                double take = Math.Min(rest, stored);
                if (seconds > 0 && double.IsFinite(seconds))
                    take = Math.Min(take, db.DischargeLimit_kW * seconds);
                db.EnergyStored[id] = stored - take;
                delivered += take;
                // Book the recharge.
                if (ship.Manager != null)
                    EnergyGenProcessor.EnergyGen(ship, db.dateTimeLastProcess);
            }
            return delivered;
        }
    }
}
