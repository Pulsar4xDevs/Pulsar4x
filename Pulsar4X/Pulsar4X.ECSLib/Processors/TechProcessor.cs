using System;
using System.Collections.Generic;
using System.Linq;
using NCalc;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// See also the Installation Processors for DoResearch
    /// </summary>
    public static class TechProcessor
    {
        private static Game _game;
        private const int _timeBetweenRuns = 68400; //one terran day.


        internal static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            foreach (var system in systems)
            {
                system.EconLastTickRun += deltaSeconds;
                if (system.EconLastTickRun >= _timeBetweenRuns)
                {
                    foreach (Entity colonyEntity in system.SystemManager.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
                    {
                        DoResearch(colonyEntity);
                    }
                    system.EconLastTickRun -= _timeBetweenRuns;
                }
            }
        }

        /// <summary>
        /// adds research points to a scientists project.
        /// </summary>
        /// <param name="colonyEntity"></param>
        /// <param name="factionAbilities"></param>
        /// <param name="factionTechs"></param>
        internal static void DoResearch(Entity colonyEntity)
        {
            FactionAbilitiesDB factionAbilities = colonyEntity.GetDataBlob<ColonyInfoDB>().FactionEntity.GetDataBlob<FactionAbilitiesDB>();
            FactionTechDB factionTechs = colonyEntity.GetDataBlob<ColonyInfoDB>().FactionEntity.GetDataBlob<FactionTechDB>();
            Dictionary<Entity, int> labs = new Dictionary<Entity, int>();
            foreach (var lab in colonyEntity.GetDataBlob<ColonyInfoDB>().Installations.Keys.Where(inst => inst.HasDataBlob<ResearchPointsAbilityDB>()))
            {               
                int points = lab.GetDataBlob<ResearchPointsAbilityDB>().PointsPerEconTick;
                labs.Add(lab, points);
            }
            
            int labsused = 0;

            foreach (var scientist in colonyEntity.GetDataBlob<ColonyInfoDB>().Scientists)
            {
                TechSD research = (TechSD)scientist.GetDataBlob<TeamsDB>().TeamTask;
                int numProjectLabs = scientist.GetDataBlob<TeamsDB>().TeamSize;
                float bonus = scientist.GetDataBlob<ScientistDB>().Bonuses[research.Category];
                //bonus *= BonusesForType(factionEntity, colonyEntity, InstallationAbilityType.Research);

                int researchmax = CostFormula(factionTechs, research);

                int researchPoints = 0;
                foreach (var kvp in labs)
                {
                    while (numProjectLabs > 0)
                    {
                        researchPoints += kvp.Value;
                        numProjectLabs --;
                    }
                }
                researchPoints = (int)(researchPoints * bonus);
                if (factionTechs.ResearchableTechs.ContainsKey(research))
                {
                    factionTechs.ResearchableTechs[research] += researchPoints;
                    if (factionTechs.ResearchableTechs[research] >= researchmax)
                    {
                        ApplyTech(factionTechs, research); //apply effects from tech, and add it to researched techs
                        scientist.GetDataBlob<TeamsDB>().TeamTask = null; //team task is now nothing. 
                    }
                }
            }
        }

        public static void AssignLabs(Entity scientist, byte labs)
        {

        }







        /// <summary>
        /// maybe techsd should link up as well as down. it would make this more efficent, but harder on the modder. 
        /// </summary>
        /// <param name="techdb"></param>
        internal static void MakeResearchable(FactionTechDB techdb)
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
        public static void ApplyTech(FactionTechDB factionTechs, TechSD research)
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



        public static double DataFormula(FactionTechDB factionTechs, TechSD tech)
        {
            string stringExpression = tech.DataFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", (double)factionTechs.LevelforTech(tech));
            object result = expression.Evaluate();
            if (result is int)
                return (double)(int)result;
            return (double)result;
        }

        public static Expression DataExpression(FactionTechDB factionTechs, TechSD tech)
        {
            string stringExpression = tech.DataFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", (double)factionTechs.LevelforTech(tech));
            return expression;
        }

        public static int CostFormula(FactionTechDB factionTechs, TechSD tech)
        {
            string stringExpression = tech.CostFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", factionTechs.LevelforTech(tech));
            int result = (int)expression.Evaluate();
            return result;
        }

    }
}
