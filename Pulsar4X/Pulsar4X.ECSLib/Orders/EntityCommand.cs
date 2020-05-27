using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public abstract class EntityCommand
    {
        [JsonProperty]
        public Guid CmdID { get; internal set; } = Guid.NewGuid();
        public bool UseActionLanes = true;
        public abstract int ActionLanes { get;  }
        public abstract bool IsBlocking { get; }

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
        internal abstract void ActionCommand();

        public bool IsRunning { get; protected set; } = false;
        public abstract bool IsFinished(); 
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
        public IOrderHandler Handler;
        private ManagerSubPulse _subPulse;
        internal DateTime GetSystemDatetime { get { return _subPulse.StarSysDateTime; } }

        internal CommandReferences(Guid faction, Guid entity, IOrderHandler handler, ManagerSubPulse subPulse)
        {
            FactionGuid = faction;
            EntityGuid = entity;
            Handler = handler;
            _subPulse = subPulse;
        }
        public static CommandReferences CreateForEntity(Game game, Entity entity)
        {
            return new CommandReferences(entity.FactionOwner, entity.Guid, game.OrderHandler, entity.Manager.ManagerSubpulses);
        }
        public static CommandReferences CreateForEntity(Game game, Guid entityGuid)
        {
            Entity entity;
            if (game.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                return new CommandReferences(entity.FactionOwner, entityGuid, game.OrderHandler, entity.Manager.ManagerSubpulses);
            else
                throw new Exception("Entity Not Found");
        }
    }

    public class RenameCommand : EntityCommand
    {
        
        public override int ActionLanes => 0;

        public override bool IsBlocking => false;
        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }
        bool _isFinished = false;
        string NewName;


        public static void CreateRenameCommand(Game game, Entity faction, Entity orderEntity, string newName)
        {
            var cmd = new RenameCommand()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                NewName = newName,
                UseActionLanes = false
            };

            game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand()
        {
            var namedb = _entityCommanding.GetDataBlob<NameDB>();
            namedb.SetName(_factionEntity.Guid, NewName);
            _isFinished = true;
        }

        public override bool IsFinished()
        {
            return _isFinished;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                return true;
            }
            return false;
        }
    }
}
