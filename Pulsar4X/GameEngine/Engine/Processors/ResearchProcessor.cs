using NCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Pulsar4X.DataStructures;
using Pulsar4X.Components;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine
{
    /// <summary>
    /// See also the Installation Processors for DoResearch
    /// </summary>
    public class ResearchProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromDays(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.5);

        public Type GetParameterType => typeof(EntityResearchDB);

        public void Init(Game game)
        {

        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            DoResearch(entity);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<Entity> entitysWithReserch = manager.GetAllEntitiesWithDataBlob<EntityResearchDB>();
            foreach(var entity in entitysWithReserch)
            {
                ProcessEntity(entity, deltaSeconds);
            }

            return entitysWithReserch.Count;
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
            entity.Manager.FindEntityByGuid(entity.FactionOwnerID, out faction);
            FactionAbilitiesDB factionAbilities = faction.GetDataBlob<FactionAbilitiesDB>();
            FactionTechDB factionTechs = faction.GetDataBlob<FactionTechDB>();
            EntityResearchDB entityResearch = entity.GetDataBlob<EntityResearchDB>();
            FactionDataStore factionDataStore = faction.GetDataBlob<FactionInfoDB>().Data;
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
                string projectGuid = scientist.ProjectQueue[0].techID;
                bool cycleProject = scientist.ProjectQueue[0].cycle;

                if(!factionTechs.IsResearchable(projectGuid))
                {
                    scientist.ProjectQueue.RemoveAt(0);
                    continue;
                }

                int assignedLabs = scientist.AssignedLabs;
                //(TechSD)scientist.GetDataBlob<TeamsDB>().TeamTask;

                Tech project = factionDataStore.Techs[projectGuid];//_staticData.Techs[projectGuid];
                //int numProjectLabs = scientist.TeamSize;
                float bonus = 1;
                if (scientist.Bonuses.ContainsKey(project.Category))
                    bonus += scientist.Bonuses[project.Category];
                //bonus *= BonusesForType(factionEntity, colonyEntity, InstallationAbilityType.Research);

                int researchPoints = 0;

                var maxIndex = Math.Min(labIndex + assignedLabs, maxLabs); //shouldn't happen unless assigned labs is more than the labs availible.
                for (int i = labIndex; i < maxIndex; i++)
                {
                    researchPoints += allLabs[i].pnts;
                }

                researchPoints = (int)(researchPoints * bonus);

                if (factionTechs.IsResearchable(project.UniqueID))
                {
                    int currentLvl = factionTechs.GetLevelforTech(project);
                    factionTechs.AddPoints(project.UniqueID, researchPoints);
                    if (factionTechs.GetLevelforTech(project) > currentLvl)
                    {
                        scientist.ProjectQueue.RemoveAt(0);

                        if(project.Faction != null && project.Faction.TryGetDatablob<FactionInfoDB>(out var factionInfo) && project.Design != null)
                        {
                            factionInfo.IndustryDesigns[project.UniqueID] = project.Design;
                        }

                        if(cycleProject)
                            scientist.ProjectQueue.Add((project.UniqueID, true));
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
        public static void AssignProject(Scientist scientist, string techID)
        {
            //TODO: check valid research, scientist etc for the empire.
            //TechSD project = _game.StaticData.Techs[techID];
            scientist.ProjectQueue.Add((techID, false));
        }
    }
}
