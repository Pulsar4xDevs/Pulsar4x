using NCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

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
            //Dictionary<ComponentInstance, int> labs = entityResearch.Labs;
            List<(ComponentInstance lab, int pnts)> allLabs = new List<(ComponentInstance lab, int pnts)>();
            if (entity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<ResearchPointsAtbDB>(out var labs))
            {
                foreach (var labInstance in labs)
                {
                    var points = labInstance.Design.GetAttribute<ResearchPointsAtbDB>().PointsPerEconTick;
                    allLabs.Add((labInstance, points));
                }
            }
            
            int labIndex = 0;
            int maxLabs = allLabs.Count;
            
            foreach (Scientist scientist in entity.GetDataBlob<TeamsHousedDB>().TeamsByType[TeamTypes.Science])
            {
    
                if (scientist.ProjectQueue.Count == 0)
                {
                    continue;
                }
                Guid projectGuid = scientist.ProjectQueue[0].techID;
                bool cycleProject = scientist.ProjectQueue[0].cycle;
                
                if(!factionTechs.IsResearchable(projectGuid))
                {
                    scientist.ProjectQueue.RemoveAt(0);
                    continue;
                }

                int assignedLabs = scientist.AssignedLabs;
                //(TechSD)scientist.GetDataBlob<TeamsDB>().TeamTask;
                
                TechSD project = factionTechs.GetResarchableTech(projectGuid).tech;//_staticData.Techs[projectGuid];
                //int numProjectLabs = scientist.TeamSize;
                float bonus = 1;
                if (scientist.Bonuses.ContainsKey(project.Category))
                    bonus += scientist.Bonuses[project.Category];
                //bonus *= BonusesForType(factionEntity, colonyEntity, InstallationAbilityType.Research);

                int researchPoints = 0;

                var maxIndex = Math.Max(labIndex + assignedLabs, maxLabs); //shouldn't happen unless assigned labs is more than the labs availible.
                for (int i = labIndex; i < maxIndex; i++)
                {
                    researchPoints += allLabs[i].pnts;
                }
                
                researchPoints = (int)(researchPoints * bonus);
                
                if (factionTechs.IsResearchable(project.ID))
                {
                    int currentLvl = factionTechs.GetLevelforTech(project);
                    factionTechs.AddPoints(project.ID, researchPoints);
                    if (factionTechs.GetLevelforTech(project) > currentLvl)
                    {
                        scientist.ProjectQueue.RemoveAt(0);
                        if(cycleProject)
                            scientist.ProjectQueue.Add((project.ID, true));
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
            scientist.ProjectQueue.Add((techID, false));
        }

        /// <summary>
        /// maybe techsd should link up as well as down. it would make this more efficent, but harder on the modder. 
        /// </summary>
        /// <param name="techdb"></param>
        internal static void CheckRequrements(FactionTechDB techdb)
        {
            List<TechSD> requrementsMetTechs = new List<TechSD>();
            foreach (var kvpTech in techdb.UnavailableTechs.ToArray())
            {
                bool requrementsMet = false;

                if (kvpTech.Requirements.Count == 0) //if requirements is an empty dict
                {
                    requrementsMet = true;
                }
                else {
                    foreach (var kvpRequrement in kvpTech.Requirements)
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
                    requrementsMetTechs.Add(kvpTech);             
                }
            }
            foreach (var item in requrementsMetTechs)
            {
                techdb.MakeResearchable(item);
            }
            if (requrementsMetTechs.Count > 0)
                CheckRequrements(techdb);//run again, we may have met a requirment by makign something else researchable.
        }






        public static double DataFormula(FactionTechDB factionTechs, TechSD tech)
        {
            string stringExpression = tech.DataFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", (double)factionTechs.GetLevelforTech(tech));
            object result = expression.Evaluate();
            if (result is int)
                return (double)(int)result;
            return (double)result;
        }

        public static Expression DataExpression(FactionTechDB factionTechs, TechSD tech)
        {
            string stringExpression = tech.DataFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", (double)factionTechs.GetLevelforTech(tech));
            return expression;
        }

        public static int CostFormula(FactionTechDB factionTechs, TechSD tech)
        {
            string stringExpression = tech.CostFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", factionTechs.GetLevelforTech(tech));
            int result = (int)expression.Evaluate();
            return result;
        }


    }
}
