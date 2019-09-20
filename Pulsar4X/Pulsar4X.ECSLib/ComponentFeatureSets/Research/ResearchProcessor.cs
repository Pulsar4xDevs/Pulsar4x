using NCalc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// See also the Installation Processors for DoResearch
    /// </summary>
    public class ResearchProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromDays(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.5);

        public Type GetParameterType => typeof(EntityResearchDB);

        //StaticDataStore _staticData;

        Dictionary<Guid, TechSD> Techs = new Dictionary<Guid, TechSD>();

        public void Init(Game game)
        {
            Techs = game.StaticData.Techs;
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            DoResearch(entity);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<Entity> entitysWithReserch = manager.GetAllEntitiesWithDataBlob<EntityResearchDB>();
            foreach(var entity in entitysWithReserch)
            {
                ProcessEntity(entity, deltaSeconds);
            }
        }




        /// <summary>
        /// adds research points to a scientists project.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="factionAbilities"></param>
        /// <param name="factionTechs"></param>
        internal void DoResearch(Entity entity)
        {
            
            Entity faction;
            entity.Manager.FindEntityByGuid(entity.FactionOwner, out faction);
            FactionAbilitiesDB factionAbilities = faction.GetDataBlob<FactionAbilitiesDB>();
            FactionTechDB factionTechs = faction.GetDataBlob<FactionTechDB>();
            EntityResearchDB entityResearch = entity.GetDataBlob<EntityResearchDB>();
            Dictionary<ComponentInstance, int> labs = entityResearch.Labs;
            
            foreach (Scientist scientist in entity.GetDataBlob<TeamsHousedDB>().TeamsByType[TeamTypes.Science])
            {
    
                if (scientist.ProjectQueue.Count == 0)
                {
                    continue;
                }

                //(TechSD)scientist.GetDataBlob<TeamsDB>().TeamTask;
                Guid projectGuid = scientist.ProjectQueue[0];
                TechSD project = factionTechs.GetResarchableTech(projectGuid).tech;//_staticData.Techs[projectGuid];
                int numProjectLabs = scientist.TeamSize;
                float bonus = scientist.Bonuses[project.Category];
                //bonus *= BonusesForType(factionEntity, colonyEntity, InstallationAbilityType.Research);

                int researchmax = CostFormula(factionTechs, project);

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
                if (factionTechs.ResearchableTechs.ContainsKey(project))
                {
                    factionTechs.ResearchableTechs[project] += researchPoints;
                    if (factionTechs.ResearchableTechs[project] >= researchmax)
                    {
                        ApplyTech(factionTechs, project); //apply effects from tech, and add it to researched techs
                        scientist.TeamTask = null; //team task is now nothing. 
                    }
                }
            }
        }

        /// <summary>
        /// assigns more labs to a given scientist
        /// will not assign more than scientists MaxLabs
        /// </summary>
        /// <param name="scientist"></param>
        /// <param name="labs"></param>
        public static void AssignLabs(Scientist scientist, byte labs)
        {
            //TODO: ensure that the labs are availible to assign.
            scientist.AssignedLabs = Math.Min(scientist.MaxLabs, labs);
        }
        
        public static void AddLabs(Scientist scientist, int labs)
        {
            //TODO: ensure that the labs are availible to assign.
            byte numlabs = (byte)(scientist.AssignedLabs + labs);
            AssignLabs(scientist, numlabs);
        }
        


        /// <summary>
        /// adds a tech to a scientists research queue.
        /// </summary>
        /// <param name="scientist"></param>
        /// <param name="techID"></param>
        public static void AssignProject(Scientist scientist, Guid techID)
        {
            //TODO: check valid research, scientist etc for the empire.
            //TechSD project = _game.StaticData.Techs[techID];
            scientist.ProjectQueue.Add(techID);
        }

        /// <summary>
        /// maybe techsd should link up as well as down. it would make this more efficent, but harder on the modder. 
        /// </summary>
        /// <param name="techdb"></param>
        internal static void MakeResearchable(FactionTechDB techdb)
        {
            List<TechSD> requrementsMetTechs = new List<TechSD>();
            foreach (var kvpTech in techdb.UnavailableTechs.ToArray())
            {
                bool requrementsMet = false;

                if (kvpTech.Key.Requirements.Count == 0) //if requirements is an empty dict
                {
                    requrementsMet = true;
                }
                else {
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
                }
                if (requrementsMet)
                {
                    requrementsMetTechs.Add(kvpTech.Key);             
                }
            }
            foreach (var item in requrementsMetTechs)
            {
                ApplyTech(techdb, item);
            }
            if (requrementsMetTechs.Count > 0)
                MakeResearchable(techdb);//run again.
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

            if (factionTechs.ResearchedTechs.ContainsKey(research.ID))
                factionTechs.ResearchedTechs[research.ID] += 1;
            else
                factionTechs.ResearchedTechs.Add(research.ID, 0);

           
            if (factionTechs.LevelforTech(research) >= research.MaxLevel)
            {
                factionTechs.ResearchableTechs.Remove(research);
            }
            else if (!factionTechs.ResearchableTechs.ContainsKey(research))
            {
                factionTechs.MakeResearchable(research);
            } 

            if (factionTechs.UnavailableTechs[research] >= research.MaxLevel)
                factionTechs.UnavailableTechs.Remove(research); //if we've reached the max value for this tech remove it from the unavailbile list
            else                                             //else if we've not reached max value, increase the level.
                factionTechs.UnavailableTechs[research] += 1;
            
            //check if it's opened up other reasearch.
            //MakeResearchable(factionTechs);
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
