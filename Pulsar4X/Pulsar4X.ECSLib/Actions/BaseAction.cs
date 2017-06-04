using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseOrder
    {
        [JsonProperty]
        public Guid EntityGuid { get; set; }
        [JsonProperty]
        public Guid FactionGuiD { get; set; }
        [JsonProperty]
        public Guid TargetEntityGuid { get; internal set; }
        [JsonProperty]
        internal bool HasTargetEntity { get; } = false;

        protected BaseOrder() { }

        protected BaseOrder(Guid orderEntity, Guid faction)
        {
            FactionGuiD = faction;
            EntityGuid = orderEntity;
        }


        protected BaseOrder(Guid orderEntity, Guid faction, Guid targetEntity) : this(orderEntity, faction)
        {
            TargetEntityGuid = targetEntity;
            HasTargetEntity = true;
        }

        /// <summary>
        /// Creates an Action 
        /// </summary>
        /// <param name="game"></param>
        /// <param name="order"></param>
        internal abstract BaseAction CreateAction(Game game, BaseOrder order);

        /// <summary>
        /// returns true if the required entites are found and the orderedEntity is owned by the factionEntity
        /// I realy dislike this style of function however, and I may rewrite it. (bool with out)
        /// </summary>
        /// <param name="game"></param>
        /// <param name="order"></param>
        /// <param name="orderEntities"></param>
        /// <returns></returns>
        internal static bool GetOrderEntities(Game game, BaseOrder order, out OrderEntities orderEntities)
        {
            orderEntities = new OrderEntities();
            if (!game.GlobalManager.FindEntityByGuid(order.EntityGuid, out orderEntities.ThisEntity))
                return false;
            if (!orderEntities.ThisEntity.HasDataBlob<OrderableDB>())
                return false;
            if (!game.GlobalManager.FindEntityByGuid(order.FactionGuiD, out orderEntities.FactionEntity))
                return false;
            if (order.HasTargetEntity)
            {
                if (!game.GlobalManager.FindEntityByGuid(order.TargetEntityGuid, out orderEntities.TargetEntity))
                    return false;
            }
            if (orderEntities.ThisEntity.GetDataBlob<OwnedDB>().EntityOwner != orderEntities.FactionEntity)
                return false;
            
            return true;
        }
    }


    public interface IActionableProcessor
    {
        void ProcessAction(DateTime toDate, BaseAction action);
    }

    internal struct OrderEntities
    {
        internal Entity ThisEntity;
        internal Entity FactionEntity;
        internal Entity TargetEntity;
    }
    
    
    public abstract class BaseAction
    {
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

