using System;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    public class WarpMoveCommand : EntityCommand
    {

        public override string Name { get; } = "Nav: Warp Move";

        public override string Details
        {
            get
            {
                string targetName = _targetEntity.GetDataBlob<NameDB>().GetName(_factionEntity);
                return "Warp to + " + Stringify.Distance(TargetOffsetPosition_m.Length()) + " from " + targetName;
            }
        }
        
        public override int ActionLanes => 1;
        public override bool IsBlocking => true;

        [JsonProperty]
        public Guid TargetEntityGuid { get; set; }

        private Entity _targetEntity;

        
        [JsonIgnore]
        Entity _factionEntity;
        WarpMovingDB _db;


        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }
        
        public Vector3 TargetOffsetPosition_m { get; set; }
        public DateTime TransitStartDateTime;
        public Vector3 ExpendDeltaV;
        
        /// <summary>
        /// Creates the transit cmd.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="faction">Faction.</param>
        /// <param name="orderEntity">Order entity.</param>
        /// <param name="targetEntity">Target entity.</param>
        /// <param name="targetOffsetPos_m">Target offset position in au.</param>
        /// <param name="transitStartDatetime">Transit start datetime.</param>
        /// <param name="expendDeltaV_AU">Amount of DV to expend to change the orbit in AU/s</param>
        public static void CreateCommand(Guid faction, Entity orderEntity, Entity targetEntity, Vector3 targetOffsetPos_m, DateTime transitStartDatetime, Vector3 expendDeltaV)
        {
            var cmd = new WarpMoveCommand()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                TargetEntityGuid = targetEntity.Guid,
                TargetOffsetPosition_m = targetOffsetPos_m,
                TransitStartDateTime = transitStartDatetime,
                ExpendDeltaV = expendDeltaV,
            };
            StaticRefLib.OrderHandler.HandleOrder(cmd);
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.FindEntityByGuid(TargetEntityGuid, out _targetEntity))
                {
                    return true; 
                }
            }
            return false;
        }

        internal override void ActionCommand(DateTime atDateTime)
        {
            if (!IsRunning)
            {
                var warpDB = _entityCommanding.GetDataBlob<WarpAbilityDB>();
                var powerDB = _entityCommanding.GetDataBlob<EnergyGenAbilityDB>();
                Guid eType = warpDB.EnergyType;
                double estored = powerDB.EnergyStored[eType];
                double creationCost = warpDB.BubbleCreationCost;
                if (creationCost <= estored)
                {
                    
                    _db = new WarpMovingDB(_entityCommanding, _targetEntity, TargetOffsetPosition_m);
                    _db.ExpendDeltaV = ExpendDeltaV;
                    

                    if (EntityCommanding.HasDataBlob<OrbitDB>())
                        EntityCommanding.RemoveDataBlob<OrbitDB>();
                    if(EntityCommanding.HasDataBlob<NewtonMoveDB>())
                        EntityCommanding.RemoveDataBlob<NewtonMoveDB>();
                    EntityCommanding.SetDataBlob(_db);
                    
                    WarpMoveProcessor.StartNonNewtTranslation(EntityCommanding);
                    IsRunning = true;
                    
                    
                    //debug code:
                    double distance = (_db.EntryPointAbsolute - _db.ExitPointAbsolute).Length();
                    double time = distance / _entityCommanding.GetDataBlob<WarpAbilityDB>().MaxSpeed;
                    //Assert.AreEqual((_db.PredictedExitTime - _db.EntryDateTime).TotalSeconds, time, 1.0e-10);

                }
            }
        }

        public override bool IsFinished()
        {
            if(_db != null)
                return _db.IsAtTarget;
            return false;
        }
    }
}
