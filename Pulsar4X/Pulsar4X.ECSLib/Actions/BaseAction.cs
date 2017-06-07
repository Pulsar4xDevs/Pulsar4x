using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public abstract class BaseAction
    {
        public string Name { get; set; }
        public string Status { get; set; }
        /// <summary>
        /// bitmask
        /// </summary>
        internal int Lanes { get; set; } 

        internal bool IsBlocking { get; set; }
        internal bool IsFinished { get; set; }
        internal DateTime LastRunTime { get; set; }

        internal IActionableProcessor OrderableProcessor { get; set; }
    
        internal Entity ThisEntity { get; private set; }
        internal Entity FactionEntity { get; private set; }
        internal Entity TargetEntity { get; private set; }
        public DateTime EstTimeComplete { get; internal set; }
        
        public BaseOrder Order { get; private set; }
        public bool HasTargetEntity { get; internal set; }

        /// <summary>
        /// BaseAction constructor
        /// </summary>
        /// <param name="lanes">bitmask</param>
        /// <param name="isBlocking">if true, will block on the lanes it's running on till complete</param>
        /// <param name="order"></param>
        /// <param name="entity"></param>
        /// <param name="faction"></param>
        protected BaseAction(int lanes, bool isBlocking, BaseOrder order, Entity entity, Entity faction)
        {
            Lanes = lanes;
            IsBlocking = isBlocking;
            IsFinished = false;
            Order = order;
            ThisEntity = entity;
            FactionEntity = faction;
            HasTargetEntity = false;
        }

        protected BaseAction(int lanes, bool isBlocking, BaseOrder order, Entity entity, Entity faction, Entity target) : 
            this (lanes, isBlocking, order, entity, faction)
        {
            TargetEntity = target;
            HasTargetEntity = true;
        }        
    }

    public interface IActionableProcessor
    {
        void ProcessAction(DateTime toDate, BaseAction action);
    }

   
    /// <summary>
    /// This will enable a component/facility. ie turn on a mine, active sensor etc, 
    /// it is non blocking and shouldn't normaly be blocked by other actions. 
    /// keeping enable/disable seperate instead of just a single toggle should help with laggy network race conditions.
    /// </summary>
    public class EnableComponent : BaseAction
    {
        public EnableComponent(BaseOrder order, Entity orderEntity, Entity factionEntity) : base(3, false, order, orderEntity, factionEntity)
        {
        }
    }
    
    /// <summary>
    /// This will disable a component/facility. ie turn off a mine, disable an active sensor etc, 
    /// it is non blocking and shouldn't normaly be blocked by other actions. 
    /// keeping enable/disable seperate instead of just a single toggle should help with laggy network race conditions.
    /// </summary>
    public class DisableComponent : BaseAction
    {
        public DisableComponent(BaseOrder order, Entity orderEntity, Entity factionEntity) : base(3, false, order, orderEntity, factionEntity)
        {
        }
    }
}

