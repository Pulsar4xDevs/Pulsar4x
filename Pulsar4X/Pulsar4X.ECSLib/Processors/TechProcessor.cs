using System;
using NCalc;

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
            foreach (var kvpTech in techdb.UnavailableTechs)
            {
                bool requrementsMet = false;

                foreach (var kvpRequrement in kvpTech.Key.Requirements)
                {                       
                    if (techdb.ResearchedTechs.ContainsKey(kvpRequrement.Key) 
                        && techdb.ResearchedTechs[kvpRequrement.Key] >= kvpRequrement.Value)
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
                    ApplyTech(techdb, kvpTech.Key);
                }
            }
        }


        /// <summary>
        /// Applies the researched tech to the faction. Can be used when tech is gifted, stolen, researched...
        /// Increases the specific TechSD by one level for the given faction.
        /// </summary>
        /// <param name="factionAbilities"></param>
        /// <param name="factionTechs"></param>
        /// <param name="research"></param>
        public static void ApplyTech(TechDB factionTechs, TechSD research)
        {


            factionTechs.ResearchedTechs[research.ID] += 1;

           
            if (factionTechs.LevelforTech(research) >= research.MaxLevel)
            {
                factionTechs.ResearchableTechs.Remove(research);
            }
            else if (!factionTechs.ResearchableTechs.ContainsKey(research))
                factionTechs.ResearchableTechs.Add(research, 0); 

            if (factionTechs.UnavailableTechs[research] >= research.MaxLevel)
                factionTechs.UnavailableTechs.Remove(research); //if we've reached the max value for this tech remove it from the unavailbile list
            else                                             //else if we've not reached max value, increase the level.
                factionTechs.UnavailableTechs[research] += 1;
            
            //check if it's opened up other reasearch.
            MakeResearchable(factionTechs);
        }

        public static double ExpresionDataEval(TechDB factionTechs, TechSD tech)
        {
            string stringExpression = tech.DataFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", factionTechs.LevelforTech(tech));
            double result = (double)expression.Evaluate();
            return result;
        }

    }
}
