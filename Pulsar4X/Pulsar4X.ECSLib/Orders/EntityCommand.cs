using System;
namespace Pulsar4X.ECSLib
{
    public interface EntityCommand
    {
        /// <summary>
        /// This is the faction that has requested the command. 
        /// </summary>
        /// <value>The requesting faction GUID.</value>
        Guid RequestingFactionGuid { get; set; }

        /// <summary>
        /// The Entity this command is targeted at
        /// </summary>
        /// <value>The entity GUID.</value>
        Guid EntityCommandingGuid { get; set; }

        Entity EntityCommanding{ get; }

        /// <summary>
        /// Gets or sets the datetime this command was created by the player/client. 
        /// </summary>
        /// <value>The created date.</value>
        DateTime CreatedDate{ get; set; }

        /// <summary>
        /// Gets or sets the datetime this command was actioned/processed by the server. 
        /// this may be needed by the client to ensure it stays in synch with the server. 
        /// </summary>
        /// <value>The actioned on date.</value>
        DateTime ActionedOnDate{ get; set; }

        /// <summary>
        /// checks that the entities exsist and that the entity is owned by the faction.
        /// may eventualy need to return a responce instead of just bool. 
        /// </summary>
        bool IsValidCommand(Game game);
        /// <summary>
        /// Actions the command.
        /// </summary>
        /// <param name="game">Game.</param>
        void ActionCommand(Game game);

        int ActionLanes { get;}

        bool IsBlocking { get;}

        bool IsRunning{ get;}
        bool IsFinished(); 
    }

    public static class CommandHelpers
    {
        public static bool IsCommandValid(EntityManager globalManager, Guid factionGuid, Guid targetEntityGuid, out Entity factionEntity, out Entity targetEntity)
        {
            if(globalManager.FindEntityByGuid(targetEntityGuid, out targetEntity)) {
                if(globalManager.FindEntityByGuid(factionGuid, out factionEntity)) {
                    if(targetEntity.GetDataBlob<OwnedDB>().OwnedByFaction == factionEntity) {
                        return true;
                    }
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
        internal OrderHandler Handler;
        private ManagerSubPulse _subPulse;
        internal DateTime GetSystemDatetime { get { return _subPulse.SystemLocalDateTime; } }

        internal CommandReferences(Guid faction, Guid entity, OrderHandler handler, ManagerSubPulse subPulse)
        {
            FactionGuid = faction;
            EntityGuid = entity;
            Handler = handler;
            _subPulse = subPulse;
        }
    }
}
