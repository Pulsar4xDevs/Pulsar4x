using System;

namespace Pulsar4X.ECSLib.Industry
{
    public class LaunchShipCmd : EntityCommand
    {
        public override int ActionLanes => 1;
        public override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }
        IndustryJob _yardJob;
        private Guid _launchSlot;
        private Guid _jobID;
        public double FuelCost;

        private Vector3 targetPosition;
        private Entity orbitalParent;
        private bool _hasLaunched = false;
        
        public static void CreateCommand(Guid faction, Entity orderEntity, Guid lauchSlot, Guid jobID)
        {
            var cmd = new LaunchShipCmd()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                _launchSlot = lauchSlot,
                _jobID = jobID

            };

            var parent = Entity.GetSOIParentEntity(orderEntity);
            
            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(DateTime atDateTime)
        {
            if (!IsRunning)
            {
                var portDB = _entityCommanding.GetDataBlob<IndustryAbilityDB>();
                
                foreach (var job in portDB.ProductionLines[_launchSlot].Jobs)
                {
                    if (job.ItemGuid == _jobID)
                    {
                        _yardJob = job;
                        ShipDesign design = (ShipDesign)_factionEntity.GetDataBlob<FactionInfoDB>().IndustryDesigns[job.ItemGuid];
                        if(_entityCommanding.HasDataBlob<ColonyInfoDB>())
                        {
                            var planet = _entityCommanding.GetDataBlob<ColonyInfoDB>().PlanetEntity;
                            
                            FuelCost = OrbitMath.FuelCostToLowOrbit(planet, design.MassPerUnit);
                            targetPosition = new Vector3(0, OrbitMath.LowOrbitRadius(planet), 0);
                            IsRunning = true;
                        }
                        else
                        {
                            FuelCost = OrbitMath.TsiolkovskyFuelCost(design.MassPerUnit, 275, 1);
                            //targetOrbit = (OrbitDB)_entityCommanding.GetDataBlob<OrbitDB>().Clone();
                            targetPosition = _entityCommanding.GetDataBlob<PositionDB>().RelativePosition_m;
                            IsRunning = true;
                        }
                        
                    }
                }
            }
            else //IsRunning
            {
                //_entityCommanding.GetDataBlob<CargoStorageDB>().StoredCargoTypes
                ShipDesign design = (ShipDesign)_factionEntity.GetDataBlob<FactionInfoDB>().IndustryDesigns[_yardJob.ItemGuid];
                ShipFactory.CreateShip(design, _factionEntity, targetPosition, orbitalParent, (StarSystem)orbitalParent.Manager);
                _hasLaunched = true;
            }
        }

        public override bool IsFinished()
        {
            if (_hasLaunched)
                return true;
            else
                return false;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
                return true;
            else
                return false;
        }
    }
}