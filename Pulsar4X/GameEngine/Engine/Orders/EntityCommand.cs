using System;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine.Orders
{
    public abstract class EntityCommand
    {
        [Flags]
        public enum ActionLaneTypes
        {
            InstantOrder = 0,
            Movement = 1,
            InteractWithExternalEntity = 2,
            InteractWithEntitySameFleet = 4,
            IneteractWithSelf = 8,
        }

        [JsonProperty]
        public string CmdID { get; internal set; } = Guid.NewGuid().ToString();
        public bool UseActionLanes = true;
        public abstract ActionLaneTypes ActionLanes { get; }
        public abstract bool IsBlocking { get; }
        public abstract string Name { get; }
        public abstract string Details { get; }

        public virtual void UpdateDetailString()
        {}

        [JsonProperty]
        /// <summary>
        /// This is the faction that has requested the command.
        /// </summary>
        /// <value>The requesting faction GUID.</value>
        internal int RequestingFactionGuid { get; set; }
        [JsonProperty]
        /// <summary>
        /// The Entity this command is targeted at
        /// </summary>
        /// <value>The entity GUID.</value>
        internal int EntityCommandingGuid { get; set; }

        [JsonProperty]
        /// <summary>
        /// Gets or sets the datetime this command was created by the player/client.
        /// </summary>
        /// <value>The created date.</value>
        public DateTime CreatedDate{ get; set; }

        /// <summary>
        /// This sets the datetime that the order should be actioned on (ie delayed from creation)
        ///
        /// </summary>
        [JsonProperty]
        public DateTime ActionOnDate { get; set; }

        [JsonProperty]
        /// <summary>
        /// Gets or sets the datetime this command was actioned/processed by the server.
        /// this may be needed by the client to ensure it stays in synch with the server.
        /// </summary>
        /// <value>The actioned on date.</value>
        public DateTime ActionedOnDate { get; set; }


        internal abstract Entity EntityCommanding { get; }

        /// <summary>
        /// checks that the entities exsist and that the entity is owned by the faction.
        /// may eventualy need to return a responce instead of just bool.
        /// </summary>
        internal abstract bool IsValidCommand(Game game);
        /// <summary>
        /// Actions the command.
        /// </summary>
        /// <param name="game">Game.</param>
        internal abstract void Execute(DateTime atDateTime);

        public bool PauseOnAction = false;

        public bool IsRunning { get; protected set; } = false;
        public abstract bool IsFinished();

        public abstract EntityCommand Clone();
    }

    public static class CommandHelpers
    {
        public static bool IsCommandValid(EntityManager globalManager, int factionId, int targetEntityId, out Entity factionEntity, out Entity targetEntity)
        {
            targetEntity = globalManager.GetGlobalEntityById(targetEntityId);
            if(targetEntity != Entity.InvalidEntity)
            {
                if(globalManager.Game.Factions.ContainsKey(factionId))
                {
                    factionEntity = globalManager.Game.Factions[factionId];
                    if(targetEntity.FactionOwnerID == factionEntity.Id)
                        return true;
                }
            }
            factionEntity = Entity.InvalidEntity;
            return false;
        }
    }

    public class CommandReferences
    {
        internal int FactionId;
        internal int EntityId;
        public IOrderHandler Handler;
        private ManagerSubPulse _subPulse;
        internal DateTime GetSystemDatetime { get { return _subPulse.StarSysDateTime; } }

        internal CommandReferences(int faction, int entity, IOrderHandler handler, ManagerSubPulse subPulse)
        {
            FactionId = faction;
            EntityId = entity;
            Handler = handler;
            _subPulse = subPulse;
        }

        public static CommandReferences CreateForEntity(Game game, Entity entity)
        {
            return new CommandReferences(entity.FactionOwnerID, entity.Id, game.OrderHandler, entity.Manager.ManagerSubpulses);
        }

        public static CommandReferences CreateForEntity(Game game, int entityId)
        {
            Entity entity;
            if (game.GlobalManager.TryGetEntityById(entityId, out entity))
                return new CommandReferences(entity.FactionOwnerID, entityId, game.OrderHandler, entity.Manager.ManagerSubpulses);
            else
                throw new Exception("Entity Not Found");
        }
    }
}
