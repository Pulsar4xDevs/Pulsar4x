using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.ECSLib.Processors
{
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
        /// adds research points to a scientists project for a given change in time. 
        /// </summary>
        /// <param name="faction"></param>
        /// <param name="scientist"></param>
        /// <param name="factionTechs"></param>
        /// <param name="deltaTime">the time since last this was run may need rethinking</param>
        internal static void DoResearch(FactionAbilitiesDB factionAbilities, Entity scientist, TechDB factionTechs, int deltaTime)
        {
            TechSD research = (TechSD)scientist.GetDataBlob<TeamsDB>().TeamTask;
            int numLabs = scientist.GetDataBlob<TeamsDB>().Teamsize;
            float bonus = scientist.GetDataBlob<ScientistBonusDB>().Bonuses[research.Category];           
            int researchmax = research.Cost;

            int amountthisdelta = (int)(factionAbilities.BaseResearchBonus * numLabs * bonus * deltaTime);
            if (factionTechs.ResearchableTechs.ContainsKey(research))
            {
                factionTechs.ResearchableTechs[research] += amountthisdelta;
                if (factionTechs.ResearchableTechs[research] >= researchmax)
                {
                    ApplyTech(factionAbilities,factionTechs,research); //apply effects from tech, and add it to researched techs
                    scientist.GetDataBlob<TeamsDB>().TeamTask = null; //team task is now nothing. 
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
        private static void ApplyTech(FactionAbilitiesDB factionAbilities, TechDB factionTechs, TechSD research)
        {
            factionTechs.ResearchedTechs.Add(research.Id); //add the tech to researched list
            factionTechs.ResearchableTechs.Remove(research); //remove the tech from researchable dict
            MakeResearchable(factionTechs);//check for new researchable techs

            //todo read the tech you researched and apply it to the faction
        }
    }
}
