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
        /// maybe techsd should link up as well as down. it would make this more efficent.
        /// </summary>
        /// <param name="techdb"></param>
        internal static void MakeResearchable(TechDB techdb)
        {
            foreach (var tech in techdb.UnavilableTechs)
            {
                bool requrementsMet = false;
                foreach (var requrement in tech.Reqirements)
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
                    techdb.UnavilableTechs.Remove(tech);
                }
            }
        }

        internal static void DoResearch(Entity scientist, TechDB factionTechs, DateTime deltaTime)
        {
            TechSD research = (TechSD)scientist.GetDataBlob<TeamsDB>().TeamTask;
            int teamsize = scientist.GetDataBlob<TeamsDB>().Teamsize;
            int bonus = scientist.GetDataBlob<ScientistBonusDB>().Bonuses[research.Category];           
            int researchmax = research.Cost;

            int amountthisdelta = 10; //somethingsomething teamsize bonus deltatime
            if (factionTechs.ResearchableTechs.ContainsKey(research))
            {
                factionTechs.ResearchableTechs[research] += amountthisdelta;
                if (factionTechs.ResearchableTechs[research] >= researchmax)
                {
                    MakeResearchable(factionTechs);
                    scientist.GetDataBlob<TeamsDB>().TeamTask = null;
                }
            }            
        }     
    }
}
