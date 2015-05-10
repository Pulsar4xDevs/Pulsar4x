namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// See also the Installation Processors for DoResearch
    /// </summary>
    internal static class TechProcessor
    {
        /// <summary>
        /// maybe techsd should link up as well as down. it would make this more efficent, but harder on the modder. 
        /// </summary>
        /// <param name="techdb"></param>
        internal static void MakeResearchable(TechDB techdb)
        {
            foreach (var tech in techdb.UnavailableTechs)
            {
                bool requrementsMet = false;
                foreach (var requrement in tech.Requirements)
                {                       
                    if (techdb.ResearchedTechs.Contains(requrement))
                    {
                        requrementsMet = true;
                    }
                    else
                    {
                        requrementsMet = false;
                        break;
                    }
                }
                if (requrementsMet)
                {
                    techdb.ResearchableTechs.Add(tech, 0);
                    techdb.UnavailableTechs.Remove(tech);
                }
            }
        }


        /// <summary>
        /// Applies the researched tech to the faction. Can be used when tech is gifted, stolen, researched...
        /// Does not check if anyone is researching it.
        /// </summary>
        /// <param name="factionAbilities"></param>
        /// <param name="factionTechs"></param>
        /// <param name="research"></param>
        public static void ApplyTech(FactionAbilitiesDB factionAbilities, TechDB factionTechs, TechSD research)
        {
            factionTechs.ResearchedTechs.Add(research.ID); //add the tech to researched list
            factionTechs.ResearchableTechs.Remove(research); //remove the tech from researchable dict
            MakeResearchable(factionTechs);//check for new researchable techs

            //todo read the tech you researched and apply it to the faction
        }
    }
}
