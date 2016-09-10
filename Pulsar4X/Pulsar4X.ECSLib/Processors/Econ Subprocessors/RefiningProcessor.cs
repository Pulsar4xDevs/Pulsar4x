using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public static class RefiningProcessor
    {


        /// <summary>
        /// TODO: refineing rates should also limit the amount that can be refined for a specific mat each tick. 
        /// </summary>
        internal static void RefineMaterials(Entity colony, Game game)
        {
            CargoStorageDB stockpiles = colony.GetDataBlob<CargoStorageDB>();
            ColonyRefiningDB refiningDB = colony.GetDataBlob<ColonyRefiningDB>();
            int RefineryPoints = refiningDB.PointsPerTick;

            for (int jobIndex = 0; jobIndex < refiningDB.JobBatchList.Count; jobIndex++)
            {
                if (RefineryPoints > 0)
                {
                    var job = refiningDB.JobBatchList[jobIndex];
                    ProcessedMaterialSD material = game.StaticData.ProcessedMaterials[job.ItemGuid];
                    Dictionary<Guid, int> costs = new Dictionary<Guid, int>(material.RawMineralCosts);
                    if(material.RefinedMateraialsCosts != null)
                        costs.Concat(new Dictionary<Guid, int>(material.RefinedMateraialsCosts));
                    while (job.NumberCompleted < job.NumberOrdered && job.PointsLeft > 0)
                    {
                        if (job.PointsLeft == material.RefineryPointCost)
                        {
                            //consume all ingredients for this job on the first point use. 
                            if (StorageSpaceProcessor.HasReqiredItems(stockpiles, costs))
                            {
                                StorageSpaceProcessor.RemoveResources(stockpiles, costs);
                            }
                            else
                            {
                                break;
                            }
                        }
                   
                        //use Refinery points
                        ushort pointsUsed = (ushort)Math.Min(job.PointsLeft, material.RefineryPointCost);
                        job.PointsLeft -= pointsUsed;
                        RefineryPoints -= pointsUsed;

                        //if job is complete
                        if (job.PointsLeft == 0)
                        {
                            job.NumberCompleted++; //complete job, 
                            StorageSpaceProcessor.AddItemToCargo(stockpiles, material.ID, material.OutputAmount); //and add the product to the stockpile                        
                            job.PointsLeft = material.RefineryPointCost; //and reset the points left for the next job in the batch.
                        }
                        
                    }
                    //if the whole batch is completed
                    if (job.NumberCompleted == job.NumberOrdered)
                    {
                        //remove it from the list
                        refiningDB.JobBatchList.RemoveAt(jobIndex);
                        if (job.Auto) //but if it's set to auto, re-add it. 
                        {
                            job.PointsLeft = material.RefineryPointCost;
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
        //    Dictionary<Entity, int> installations = colonyEntity.GetDataBlob<ColonyInfoDB>().Installations;
        //    Dictionary<Entity, int> Refinerys = installations.Where(kvp => kvp.Key.HasDataBlob<RefineResourcesDB>()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        //    int pointsRate = 0;
        //    Dictionary<Guid, int> matRate = new Dictionary<Guid, int>();
        //    foreach (var RefineryKvp in Refinerys)
        //    {
        //        int points = RefineryKvp.Key.GetDataBlob<RefineResourcesDB>().RefineryPoints;
                
        //        foreach (var mat in RefineryKvp.Key.GetDataBlob<RefineResourcesDB>().RefinableMatsList)
        //        {
        //           matRate.SafeValueAdd(mat, points * RefineryKvp.Value); 
        //        }
        //        pointsRate += points;
        //    }

            Dictionary<Guid, int> rates = new Dictionary<Guid, int>();

            List<KeyValuePair<Entity, List<Entity>>> refineingEntities = colonyEntity.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<RefineResourcesAtbDB>()).ToList();
            foreach (var refiningComponentDesignList in refineingEntities)
            {
                RefineResourcesAtbDB refineblob = refiningComponentDesignList.Key.GetDataBlob<RefineResourcesAtbDB>();
                foreach (var mineInstance in refiningComponentDesignList.Value)
                {
                    //todo check if it's damaged, check if it's enabled, check if there's enough workers here to.
                    foreach (var item in refineblob.RefinableMatsList)
                    {
                        rates.SafeValueAdd(item, refineblob.RefineryPoints);
                    }
                }
            }
            int maxPoints = 0;
            foreach (int p in rates.Values)
            {
                if (p > maxPoints)
                    maxPoints = p;
            }

            var refining = colonyEntity.GetDataBlob<ColonyRefiningDB>();
            refining.RefiningRates = rates;
            refining.PointsPerTick = maxPoints;
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
                if (staticData.ProcessedMaterials.ContainsKey(job.ItemGuid))
                {
                    refiningDB.JobBatchList.Add(job);
                }
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
        //[PublicAPI]
        //public static void ChangeJobPriority(Entity colonyEntity, JobBase job, int delta)
        //{
            
        //    ColonyRefiningDB refiningDB = colonyEntity.GetDataBlob<ColonyRefiningDB>();

        //}
    }

    public static class ListPriority<T> 
    {
        public static void ChangeJobPriority(MVMCollectionSyncher<T> jobBatchList, T job, int delta)
        {
            lock (jobBatchList) //prevent threaded race conditions
            {
                //first check that the job does still exsist in the list.
                if (jobBatchList.Contains(job))
                {    
                    int currentIndex = jobBatchList.IndexOf(job);
                    int newIndex = currentIndex + delta;
                    if (newIndex <= 0)
                    {
                        jobBatchList.RemoveAt(currentIndex);
                        jobBatchList.Insert(0, job);
                    }
                    else if (newIndex >= jobBatchList.Count - 1)
                    {
                        jobBatchList.RemoveAt(currentIndex);
                        jobBatchList.Add(job);
                    }
                    else
                    {
                        jobBatchList.RemoveAt(currentIndex);
                        jobBatchList.Insert(newIndex, job);
                    }
                }
            }
        }
    }

    public class MVMCollectionSyncher<T> : IList<T>,IDisposable, INotifyCollectionChanged
    {
        #region fields
        private IList<T> _wrappedCollection;
        #endregion

        public MVMCollectionSyncher(IList<T> wrappedCollection)
        {
            if (wrappedCollection == null)
                throw new ArgumentNullException(
                 "wrappedCollection",
                 "wrappedCollection must not be null.");
            _wrappedCollection = wrappedCollection;
        }

        #region ICollection<T> Members
        public void Add(T item)
        {
            _wrappedCollection.Add(item);
            FireCollectionChanged(
             new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }



        public void Insert(int index, T item)
        {
            _wrappedCollection.Insert(index, item);
            FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, index, index + 1));
        }

        public void RemoveAt(int index)
        {
            _wrappedCollection.RemoveAt(index);
            FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, index, index));
        }

        public T this[int index]
        {
            get { return _wrappedCollection[index];}
            set
            {
                T newItem = value;
                T oldItem = _wrappedCollection[index];
                _wrappedCollection[index] = newItem; FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem));
            }
        }

        public void Clear()
        {
            FireCollectionChanged(
             new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            _wrappedCollection.Clear();
        }

        public bool Contains(T item)
        {
            return _wrappedCollection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _wrappedCollection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _wrappedCollection.Count; }
        }

        public bool IsReadOnly
        {
            get { return _wrappedCollection.IsReadOnly; }
        }

        public bool Remove(T item)
        {
            if (_wrappedCollection.Remove(item))
            {
                FireCollectionChanged(
                  new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                return true;
            }
            return false;
        }

        #endregion

        #region IEnumerable<T> Members
        public IEnumerator<T> GetEnumerator()
        {
            return _wrappedCollection.GetEnumerator();
        }
        #endregion

        #region IEnumerable Members
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _wrappedCollection.GetEnumerator();
        }
        #endregion

        #region INotifyCollectionChanged Members
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private void FireCollectionChanged(NotifyCollectionChangedEventArgs eventArg)
        {
            NotifyCollectionChangedEventHandler handler = CollectionChanged;
            if (handler != null) handler.Invoke(this, eventArg);
        }
        #endregion

        #region IDisposable Members
        public void Dispose() { _wrappedCollection = null; }
        #endregion

        public int IndexOf(T item)
        {
            return _wrappedCollection.IndexOf(item);
        }

        int IList<T>.IndexOf(T item)
        {
            return _wrappedCollection.IndexOf(item);
        }
    }


}