namespace Pulsar4X.ECSLib
{
    internal static class RuinsProcessor
    {
        /// <summary>
        /// store some sort of TotalProgress variable, in Faction? in XenoTeam?
        /// Increment TotalProgress with RNG range 0,TotalDifficalty
        /// Xeno skill comes into this somewhere.
        /// if TotalProgress >= TotalDifficalty,
        /// the ruins are fully researched and unlocked
        /// for that faction, ruins can then be dug up.
        /// </summary>
        public static void ResearchRuins()
        {
        }

        /// <summary>
        /// Again, some sort of TotalProgress, faction or ConstructionBrigade.
        /// Three outcomes, Not Successfull, Successful, Vult(hostile robots)
        /// on success, return what's descovered. see http://aurorawiki.pentarch.org/index.php?title=Ruins
        /// for a table, can probibly be turned into a weighted list.
        /// </summary>
        public static void DigUpRuins()
        {
        }
    }
}