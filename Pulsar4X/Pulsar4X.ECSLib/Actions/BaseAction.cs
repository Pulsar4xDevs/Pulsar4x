using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{

    public abstract class BaseAction
    {
        /// <summary>
        /// BaseAction constructor
        /// </summary>
        /// <param name="lanes">bitmask</param>
        /// <param name="isBlocking">if true, will block on the lanes it's running on till complete</param>
        /// <param name="entityGuid"></param>
        /// <param name="factionID"></param>
        protected BaseAction(int lanes, bool isBlocking, Guid entityGuid, Guid factionID)
        {
            Lanes = lanes;
            IsBlocking = isBlocking;
            IsFinished = false;
            
            EntityGuid = entityGuid;
            FactionGuiD = factionID;
            IsTargetEntityDependant = false;
        }

        protected BaseAction(int lanes, bool isBlocking, Guid entityGuid, Guid factionID, Guid targetGuid) : 
            this (lanes, isBlocking, entityGuid, factionID)
        {
            TargetEntityGuid = targetGuid;
            IsTargetEntityDependant = true;
        }
        
        /// <summary>
        /// bitmask
        /// </summary>
        internal int Lanes { get; set; } 

        internal bool IsBlocking { get; set; }
        internal bool IsFinished { get; set; }
        internal DateTime LastRunTime { get; set; }
        internal bool HasRunOnceBefore { get; set; } = false;

        internal IOrderableProcessor OrderableProcessor { get; set; }
    
        public Guid EntityGuid { get; set; }
        public Guid FactionGuiD { get; set; }
        public Guid TargetEntityGuid { get; internal set; }
        internal Entity ThisEntity { get; private set; }
        internal Entity FactionEntity { get; private set; }
        internal Entity TargetEntity { get; private set; }
        public DateTime EstTimeComplete { get; internal set; }


        public bool IsTargetEntityDependant { get; internal set; }


        /// <summary>
        /// looks up entity guids, and checks validity.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        internal bool PreProcessing(Game game)
        {
            Entity entity;
            if (!game.GlobalManager.TryGetEntityByGuid(EntityGuid, out entity))
                return false;
            ThisEntity = entity;
            if (!ThisEntity.HasDataBlob<OrderableDB>())
                return false;

            Entity factionEntity;
            if (!game.GlobalManager.FindEntityByGuid(FactionGuiD, out factionEntity))
                return false;
            FactionEntity = factionEntity;
            if (IsTargetEntityDependant)
            {
                Entity targetEntity;
                if (!game.GlobalManager.FindEntityByGuid(TargetEntityGuid, out targetEntity))
                    return false;
                TargetEntity = targetEntity;
            }
            if (entity.GetDataBlob<OwnedDB>().EntityOwner != FactionEntity)
                return false;

            return true;
        }
    }

   
   
    /// <summary>
    /// This will enable a component/facility. ie turn on a mine, active sensor etc, 
    /// it is non blocking and shouldn't normaly be blocked by other actions. 
    /// keeping enable/disable seperate instead of just a single toggle should help with laggy network race conditions.
    /// </summary>
    public class EnableComponent : BaseAction
    {
        public EnableComponent(Guid entityGuid, Guid factionID) : base(3, false, entityGuid, factionID)
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
        public DisableComponent(Guid entityGuid, Guid factionID) : base(3, false, entityGuid, factionID)
        {
        }
    }
}

