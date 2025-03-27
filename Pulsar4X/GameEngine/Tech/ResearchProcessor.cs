using System;
using Pulsar4X.Interfaces;
using Pulsar4X.Factions;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using System.IO;

namespace Pulsar4X.Technology
{
    /// <summary>
    /// See also the Installation Processors for DoResearch
    /// </summary>
    public class ResearchProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromDays(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.5);

        public Type GetParameterType => typeof(ResearcherDB);

        private Game _game;

        public void Init(Game game)
        {
            _game = game;
            EventManager.Instance.Subscribe(EventType.TechnologyQueued, OnTechnologyChanged);
            EventManager.Instance.Subscribe(EventType.TechnologyRemovedFromQueue, OnTechnologyChanged);
            EventManager.Instance.Subscribe(EventType.TechnologyMovedInQueue, OnTechnologyChanged);
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            DoResearch(entity);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var entitysWithResearch = manager.GetAllEntitiesWithDataBlob<ResearcherDB>();
            foreach(var entity in entitysWithResearch)
            {
                ProcessEntity(entity, deltaSeconds);
            }

            return entitysWithResearch.Count;
        }

        /// <summary>
        /// adds research points to a scientists project.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="factionAbilities"></param>
        /// <param name="factionTechs"></param>
        internal void DoResearch(Entity entity)
        {
            Entity faction = entity.Manager.Game.Factions[entity.FactionOwnerID];
            FactionAbilitiesDB factionAbilities = faction.GetDataBlob<FactionAbilitiesDB>();
            FactionTechDB factionTechs = faction.GetDataBlob<FactionTechDB>();
            FactionInfoDB factionInfoDB = faction.GetDataBlob<FactionInfoDB>();
            FactionDataStore factionDataStore = factionInfoDB.Data;

            // If unable to get the db return
            if(!entity.TryGetDatablob<ResearcherDB>(out var researcherDB))
                return;

            // Check if queue is empty
            if(!researcherDB.TechQueue.TryPeek(out var techId))
                return;

            // Get the tech that is being researched
            var tech = factionDataStore.Techs[techId];

            // Make sure that the tech is researchable
            if(!factionDataStore.IsResearchable(tech.UniqueID))
            {
                // If it isn't, dequeue the tech and return
                researcherDB.TechQueue.TryDequeue(out var result);
                return;
            }

            // Calculate the cost to run
            CalculateCost(researcherDB);

            // Calculate the research output
            CalculateResearchPoints(researcherDB, tech);

            // Make sure the calculated total is > 0
            if(researcherDB.CalculatedResearchPoints <= 0)
                return;

            // Check to make sure the cost can be paid
            if(factionInfoDB.Money.GetCurrentFunds() < researcherDB.CalculatedCostPerDay)
                return;

            // Pay the costs
            factionInfoDB.Money.AddExpense(
                entity.Manager.StarSysDateTime,
                TransactionCategory.Research,
                $"Payment to run research lab on {entity.Manager.StarSysDateTime.ToShortDateString()}",
                researcherDB.CalculatedCostPerDay);

            // Apply the research points
            int currentLvl = tech.Level;
            factionDataStore.AddTechPoints(tech, researcherDB.CalculatedResearchPoints);

            // If the tech level increased the tech research completed
            if (tech.Level > currentLvl)
            {
                // Remove the current tech from the queue
                if(!researcherDB.TechQueue.TryDequeue(out var result))
                    throw new Exception("Unable to dequeue from tech queue");

                if (tech.Faction != null && tech.Design != null && tech.Faction.TryGetDatablob<FactionInfoDB>(out var factionInfo))
                {
                    factionInfo.IndustryDesigns[tech.UniqueID] = tech.Design;
                }

                // if (cycleProject)
                //     scientist.ProjectQueue.Add((project.UniqueID, true));

                // Publish an event for research completion
                EventManager.Instance.Publish(
                    Event.Create(
                        EventType.ResearchCompleted,
                        entity.StarSysDateTime,
                        $"{tech.Name} research completed!",
                        entity.FactionOwnerID,
                        entity.Manager.ManagerID,
                        entity.Id));
            }
        }

        private void OnTechnologyChanged(Event e)
        {
            // Recalculate the stats of the researchDB
            var system = _game.Systems.Find(s => s.ManagerID.Equals(e.SystemId));

            if(system == null)
                return;

            if(e.EntityId == null || e.FactionId == null)
                return;

            if(!system.TryGetEntityById((int)e.EntityId, out var labEntity))
                return;

            if(!labEntity.TryGetDatablob<ResearcherDB>(out var researcherDB))
                return;

            // Try to find the tech at the front of the queue
            Tech? tech = null;
            if(researcherDB.TechQueue.TryPeek(out var techId))
            {
                if(_game.Factions[(int)e.FactionId].TryGetDatablob<FactionInfoDB>(out var factionInfoDB))
                {
                    tech = factionInfoDB.Data.Techs[techId];
                }
            }

            CalculateCost(researcherDB);
            CalculateResearchPoints(researcherDB, tech);
        }

        public static void CalculateCost(ResearcherDB researcherDB)
        {
            // TODO: Add bonuses for corporation administration

            // See the comments on ResearchDB.FundingLevel for an explanation
            researcherDB.CalculatedCostPerDay = researcherDB.FundingLevel switch
            {
                0 => 0,
                1 => researcherDB.BaseCostPerDay * 1,
                2 => researcherDB.BaseCostPerDay * 3,
                3 => researcherDB.BaseCostPerDay * 7,
                4 => researcherDB.BaseCostPerDay * 13,
                5 => researcherDB.BaseCostPerDay * 22,
                _ => throw new InvalidDataException("Unable to determine funding level")
            };
        }

        public static void CalculateResearchPoints(ResearcherDB researcherDB, Tech? currentTech)
        {
            int output = researcherDB.BaseResearchPoints;

            // See the comments on ResearchDB.FundingLevel for an explanation
            int fundingMultiplier = researcherDB.FundingLevel switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                3 => 3,
                4 => 4,
                5 => 5,
                _ => throw new InvalidDataException("Unable to determine funding level")
            };

            // Apply funding bonus
            output *= fundingMultiplier;

            if(currentTech != null)
            {
                // Apply any category bonuses
                foreach(var (category, bonus) in researcherDB.BonusCategories)
                {
                    // Make sure the categories match
                    if(!currentTech.Category.Equals(category))
                        continue;

                    output += (int)(output * bonus);
                }
            }

            //TODO: apply scientist bonus

            // Set the actual RP
            researcherDB.CalculatedResearchPoints = output;
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

        public static void AssignTech(ResearcherDB researcherDB, string techId)
        {
            researcherDB.TechQueue.Enqueue(techId);
        }
    }
}
