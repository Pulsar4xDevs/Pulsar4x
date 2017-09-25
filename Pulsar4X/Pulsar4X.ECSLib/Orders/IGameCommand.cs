using System;
namespace Pulsar4X.ECSLib
{
    public interface IEntityCommand
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
        Guid TargetEntityGuid { get; set; }

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
        /// Validates and actions the command. 
        /// may eventualy need to return a responce instead of just bool. 
        /// </summary>
        bool ActionCommand(Game game);


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
}
