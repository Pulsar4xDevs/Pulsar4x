using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    internal static class RefiningProcessor
    {
        private const int _timeBetweenRuns = 68400; //one terran day.
        public static void Initialize()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        /// <param name="systems"></param>
        /// <param name="deltaSeconds"></param>
        public static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            //TODO: TO MUCH NESTING!
            foreach (var starsys in systems) //TODO should be threadable
            {
                starsys.EconLastTickRun += deltaSeconds;
                if (starsys.EconLastTickRun >= _timeBetweenRuns)
                {

                    starsys.EconLastTickRun -= _timeBetweenRuns;
                    List<Entity> colonys = starsys.SystemManager.GetAllEntitiesWithDataBlob<ColonyRefiningDB>();

                    foreach (var colony in colonys)
                    {

                        JDictionary<Guid, int> mineralStockpile = colony.GetDataBlob<ColonyInfoDB>().MineralStockpile;
                        JDictionary<Guid, int> materialsStockpile = colony.GetDataBlob<ColonyInfoDB>().RefinedStockpile;

                        ColonyRefiningDB refiningDB = colony.GetDataBlob<ColonyRefiningDB>();


                        for (int jobIndex = 0; jobIndex < refiningDB.JobBatchList.Count; jobIndex++)
                        {
                            var job = refiningDB.JobBatchList[jobIndex];


                            RefinedMaterialSD material = game.StaticData.RefinedMaterials[job.jobGuid];
                            Dictionary<Guid, int> mineralCosts = material.RawMineralCosts;
                            Dictionary<Guid, int> materialCosts = material.RefinedMateraialsCosts;

                            while (refiningDB.RemainingJobs > 0 && refiningDB.RemainingPoints > material.RefinaryPointCost)
                            {
                                if (HasReqiredItems(mineralStockpile, mineralCosts) && HasReqiredItems(materialsStockpile, materialCosts))
                                {
                                    UseFromStockpile(mineralStockpile, mineralCosts);
                                    UseFromStockpile(materialsStockpile, materialCosts);
                                    refiningDB.RefinaryPoints -= material.RefinaryPointCost;
                                    refiningDB.RemainingJobs -= 1;
                                    materialsStockpile.SafeValueAdd(material.ID, material.OutputAmount);
                                }
                            }
                            if (refiningDB.RemainingJobs == 0)
                            {
                                refiningDB.JobBatchList.RemoveAt(jobIndex);
                                if (job.auto)
                                    refiningDB.JobBatchList.Add(job);
                            }
                        }
                    }
                }
            }            
        }

        public static bool HasReqiredItems(JDictionary<Guid, int> stockpile, Dictionary<Guid, int> costs )
        {
            return costs.All(kvp => stockpile.ContainsKey(kvp.Key) || !(stockpile[kvp.Key] > kvp.Value));
        }

        public static void UseFromStockpile(JDictionary<Guid, int> stockpile, Dictionary<Guid, int> costs)
        {
            foreach (var kvp in costs)
            {
                stockpile[kvp.Key] -= kvp.Value;
            }
        }


        /// <summary>
        /// called by ReCalcProcessor
        /// </summary>
        /// <param name="colonyEntity"></param>
        public static void ReCalcRefiningRate(Entity colonyEntity)
        {
            List<Entity> installations = colonyEntity.GetDataBlob<ColonyInfoDB>().Installations;
            List<Entity> refinarys = new List<Entity>();
            foreach (var inst in installations)
            {
                if (inst.HasDataBlob<RefineResourcesDB>())
                    refinarys.Add(inst);
            }
            int rate = 0;
            foreach (var refinary in refinarys)
            {
                rate += refinary.GetDataBlob<RefineResourcesDB>().RefinaryPoints;
            }
            colonyEntity.GetDataBlob<ColonyRefiningDB>().RefinaryPoints = rate;
        }

    }
}