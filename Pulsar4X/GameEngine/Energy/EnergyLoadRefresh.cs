using Pulsar4X.Engine;

namespace Pulsar4X.Energy
{
    /// <summary>
    /// Brings saved power state up to the current rules after a load: every colony grid recalcs
    /// its demand, plants and battery limits (so an old save shows today's demand at once, not
    /// after its next daily pass), and every ship rebuilds its battery limits from its installed
    /// components (a save from before battery rates has none).
    /// Stored energy, history and the last daily pass's results are kept.
    /// </summary>
    internal static class EnergyLoadRefresh
    {
        internal static void AfterLoad(Game game)
        {
            foreach (var manager in game.GlobalManagerDictionary.Values)
            {
                foreach (var ship in manager.GetAllEntitiesWithDataBlob<EnergyGenAbilityDB>())
                    EnergyGenAbilityDB.RecomputeStoreRates(ship);
                foreach (var colony in manager.GetAllEntitiesWithDataBlob<ColonyPowerDB>())
                {
                    // Recalc replaces the between-pass satisfaction with a capacity estimate; a
                    // colony that has run a daily pass keeps the satisfaction that pass delivered.
                    var power = colony.GetDataBlob<ColonyPowerDB>();
                    var delivered = power.LastDispatch != default
                        ? new System.Collections.Generic.Dictionary<PowerTier, double>(power.TierSatisfaction)
                        : null;
                    ColonyPowerProcessor.Recalc(colony);
                    if (delivered != null)
                        power.TierSatisfaction = delivered;
                }
            }
        }
    }
}
