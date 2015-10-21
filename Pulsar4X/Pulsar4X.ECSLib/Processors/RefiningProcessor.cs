using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public static class RefiningProcessor
    {


        /// <summary>
        /// TODO: refineing rates should also limit the amount that can be refined for a specific mat each tick. 
        /// </summary>
        internal static void RefineMaterials(Entity colony, Game game, int econTicks)
        {

            JDictionary<Guid, int> mineralStockpile = colony.GetDataBlob<ColonyInfoDB>().MineralStockpile;
            JDictionary<Guid, int> materialsStockpile = colony.GetDataBlob<ColonyInfoDB>().RefinedStockpile;

            ColonyRefiningDB refiningDB = colony.GetDataBlob<ColonyRefiningDB>();
            int refinaryPoints = refiningDB.RefinaryPoints * econTicks;

            for (int jobIndex = 0; jobIndex < refiningDB.JobBatchList.Count; jobIndex++)
            {
                if (refinaryPoints > 0)
                {
                    var job = refiningDB.JobBatchList[jobIndex];
                    RefinedMaterialSD material = game.StaticData.RefinedMaterials[job.ItemGuid];
                    Dictionary<Guid, int> mineralCosts = material.RawMineralCosts;
                    Dictionary<Guid, int> materialCosts = material.RefinedMateraialsCosts;

                    while (job.NumberCompleted < job.NumberOrdered && job.PointsLeft > 0)
                    {
                        if (job.PointsLeft == material.RefinaryPointCost)
                        {
                            //consume all ingredients for this job on the first point use. 
                            if (Misc.HasReqiredItems(mineralStockpile, mineralCosts) && Misc.HasReqiredItems(materialsStockpile, materialCosts))
                            {
                                Misc.UseFromStockpile(mineralStockpile, mineralCosts);
                                Misc.UseFromStockpile(materialsStockpile, materialCosts);
                            }
                            else
                            {
                                break;
                            }
                        }
                   
                        //use refinary points
                        ushort pointsUsed = (ushort)Math.Min(job.PointsLeft, material.RefinaryPointCost);
                        job.PointsLeft -= pointsUsed;
                        refinaryPoints -= pointsUsed;

                        //if job is complete
                        if (job.PointsLeft == 0)
                        {
                            job.NumberCompleted++; //complete job,                          
                            materialsStockpile.SafeValueAdd(material.ID, material.OutputAmount); //and add the product to the stockpile
                            job.PointsLeft = material.RefinaryPointCost; //and reset the points left for the next job in the batch.
                        }
                        
                    }
                    //if the whole batch is completed
                    if (job.NumberCompleted == job.NumberOrdered)
                    {
                        //remove it from the list
                        refiningDB.JobBatchList.RemoveAt(jobIndex);
                        if (job.Auto) //but if it's set to auto, re-add it. 
                        {
                            job.PointsLeft = material.RefinaryPointCost;
                            job.NumberCompleted = 0;
                            refiningDB.JobBatchList.Add(job);
                        }
                    }
                }
            }
        }

        



        /// <summary>
        /// called by ReCalcProcessor
        /// </summary>
        /// <param name="colonyEntity"></param>
        public static void ReCalcRefiningRate(Entity colonyEntity)
        {
            List<Entity> installations = colonyEntity.GetDataBlob<ColonyInfoDB>().Installations.Keys.ToList();
            List<Entity> refinarys = new List<Entity>();
            foreach (var inst in installations)
            {
                if (inst.HasDataBlob<RefineResourcesDB>())
                    refinarys.Add(inst);
            }
            int pointsRate = 0;
            JDictionary<Guid, int> matRate = new JDictionary<Guid, int>();
            foreach (var refinary in refinarys)
            {
                int points = refinary.GetDataBlob<RefineResourcesDB>().RefinaryPoints;
                
                foreach (var mat in refinary.GetDataBlob<RefineResourcesDB>().RefinableMatsList)
                {
                   matRate.SafeValueAdd(mat, points); 
                }
                pointsRate += points;
            }
            colonyEntity.GetDataBlob<ColonyRefiningDB>().RefinaryPoints = pointsRate;
        }


        /// <summary>
        /// Adds a job to a colonys ColonyRefiningDB.JobBatchList
        /// </summary>
        /// <param name="staticData"></param>
        /// <param name="colonyEntity"></param>
        /// <param name="job"></param>
        [PublicAPI]
        public static void AddJob(StaticDataStore staticData, Entity colonyEntity, RefineingJob job)
        {
            ColonyRefiningDB refiningDB = colonyEntity.GetDataBlob<ColonyRefiningDB>();
            lock (refiningDB.JobBatchList) //prevent threaded race conditions
            {
                //check if the job materialguid is valid, then add it if so.
                if (staticData.RefinedMaterials.ContainsKey(job.ItemGuid))
                    refiningDB.JobBatchList.Add(job);
            }
        }

        /// <summary>
        /// Moves a job up or down the ColonyRefiningDB.JobBatchList. 
        /// </summary>
        /// <param name="colonyEntity">the colony that's being interacted with</param>
        /// <param name="job">the job that needs re-prioritising</param>
        /// <param name="delta">How much to move it ie: 
        /// -1 moves it down the list and it will be done later
        /// +1 moves it up the list andit will be done sooner
        /// this will safely handle numbers larger than the list size, 
        /// placing the item either at the top or bottom of the list.
        /// </param>
        [PublicAPI]
        public static void MoveJob(Entity colonyEntity, RefineingJob job, int delta)
        {
            ColonyRefiningDB refiningDB = colonyEntity.GetDataBlob<ColonyRefiningDB>();
            lock (refiningDB.JobBatchList) //prevent threaded race conditions
            {
                //first check that the job does still exsist in the list.
                if (refiningDB.JobBatchList.Contains(job))
                {
                    int currentIndex = refiningDB.JobBatchList.IndexOf(job);
                    int newIndex = currentIndex + delta;
                    if (newIndex <= 0)
                    {
                        refiningDB.JobBatchList.RemoveAt(currentIndex);
                        refiningDB.JobBatchList.Insert(0, job);
                    }
                    else if (newIndex >= refiningDB.JobBatchList.Count - 1)
                    {
                        refiningDB.JobBatchList.RemoveAt(currentIndex);
                        refiningDB.JobBatchList.Add(job);
                    }
                    else
                    {
                        refiningDB.JobBatchList.RemoveAt(currentIndex);
                        refiningDB.JobBatchList.Insert(newIndex,job);
                    }
                }
            }
        }
    }
}