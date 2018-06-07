using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OrbitBodyCommand : EntityCommand
    {

        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        public Guid TargetEntityGuid { get; set; }
        public Vector4 TargetPosition { get; set; }

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        private Entity _targetEntity;

        public double ApihelionInKM { get; set; }
        public double PerhelionInKM { get; set; }
        internal List<EntityCommand> NestedCommands { get; } = new List<EntityCommand>();

        [JsonIgnore] Entity _factionEntity;



        public static void CreateOrbitBodyCommand(Game game, DateTime starSysDate, Guid factionGuid, Guid orderEntity, Guid targetEntity, double ApihelionKm, double PerhelionKm)
        {
            var cmd = new OrbitBodyCommand()
            {
                RequestingFactionGuid = factionGuid,
                EntityCommandingGuid = orderEntity,
                CreatedDate = starSysDate,
                TargetEntityGuid = targetEntity,
                ApihelionInKM = ApihelionKm,
                PerhelionInKM = PerhelionKm,
            };
            game.OrderHandler.HandleOrder(cmd);
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

        internal override void ActionCommand(Game game)
        {
            OrderableProcessor.ProcessOrderList(game, NestedCommands);
            if (NestedCommands.Count == 0)
            {
                if (!IsRunning)
                {
                    var entPos = _entityCommanding.GetDataBlob<PositionDB>().PositionInKm;
                    var tarPos = _targetEntity.GetDataBlob<PositionDB>().PositionInKm;
                    double distanceAU = PositionDB.GetDistanceBetween(_entityCommanding.GetDataBlob<PositionDB>(), _targetEntity.GetDataBlob<PositionDB>());
                    var rangeAU = ApihelionInKM / GameConstants.Units.KmPerAu;
                    if (Math.Abs(rangeAU - distanceAU) <= 500 / GameConstants.Units.MetersPerAu) //distance within 500m 
                    {
                        DateTime datenow = _entityCommanding.Manager.ManagerSubpulses.SystemLocalDateTime;
                        var newOrbit = ShipMovementProcessor.CreateOrbitHereWithPerihelion(_entityCommanding, _targetEntity, PerhelionInKM, datenow);
                        _entityCommanding.SetDataBlob(newOrbit);
                        IsRunning = true;
                    }
                    else //insert new translate move
                    {
                        var cmd = new TranslateMoveCommand() { RequestingFactionGuid = this.RequestingFactionGuid, EntityCommandingGuid = this.EntityCommandingGuid, CreatedDate = this.CreatedDate, TargetEntityGuid = this.TargetEntityGuid, RangeInKM = this.ApihelionInKM };
                        NestedCommands.Insert(0, cmd);
                        cmd.IsValidCommand(game);
                        cmd.ActionCommand(game);
                    }
                }
            }
        }

        internal override bool IsFinished()
        {
            if (_entityCommanding.HasDataBlob<OrbitDB>())
                return true;
            return false;
        }
    }
}