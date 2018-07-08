using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public abstract class EntityCommand
    {
        [JsonProperty]
        internal Guid CmdID { get; set; } = Guid.NewGuid();

        internal abstract int ActionLanes { get;  }
        internal abstract bool IsBlocking { get; }

        [JsonProperty]
        /// <summary>
        /// This is the faction that has requested the command. 
        /// </summary>
        /// <value>The requesting faction GUID.</value>
        internal Guid RequestingFactionGuid { get; set; }
        [JsonProperty]
        /// <summary>
        /// The Entity this command is targeted at
        /// </summary>
        /// <value>The entity GUID.</value>
        internal Guid EntityCommandingGuid { get; set; }

        [JsonProperty]
        /// <summary>
        /// Gets or sets the datetime this command was created by the player/client. 
        /// </summary>
        /// <value>The created date.</value>
        internal DateTime CreatedDate{ get; set; }

        [JsonProperty]
        /// <summary>
        /// Gets or sets the datetime this command was actioned/processed by the server. 
        /// this may be needed by the client to ensure it stays in synch with the server. 
        /// </summary>
        /// <value>The actioned on date.</value>
        internal DateTime ActionedOnDate{ get; set; }

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
        internal abstract void ActionCommand(Game game);

        public bool IsRunning { get; protected set; } = false;
        internal abstract bool IsFinished(); 
    }

    public static class CommandHelpers
    {
        public static bool IsCommandValid(EntityManager globalManager, Guid factionGuid, Guid targetEntityGuid, out Entity factionEntity, out Entity targetEntity)
        {
            if(globalManager.FindEntityByGuid(targetEntityGuid, out targetEntity)) {
                if(globalManager.FindEntityByGuid(factionGuid, out factionEntity)) {
                    if(targetEntity.FactionOwner == factionEntity.Guid) 
                        return true;
                }
            }
            factionEntity = Entity.InvalidEntity;
            return false;
        }
    }

    public class CommandReferences
    {
        internal Guid FactionGuid;
        internal Guid EntityGuid;
        internal IOrderHandler Handler;
        private ManagerSubPulse _subPulse;
        internal DateTime GetSystemDatetime { get { return _subPulse.SystemLocalDateTime; } }

        internal CommandReferences(Guid faction, Guid entity, IOrderHandler handler, ManagerSubPulse subPulse)
        {
            FactionGuid = faction;
            EntityGuid = entity;
            Handler = handler;
            _subPulse = subPulse;
        }
    }
}
