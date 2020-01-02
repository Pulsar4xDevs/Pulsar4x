using System;

namespace Pulsar4X.ECSLib.Industry
{
    public class LaunchShipCmd : EntityCommand
    {
        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }
        ShipYardJob yardJob;
        private Guid _launchSlot;
        public double FuelCost;

        private Vector3 targetPosition;
        private Entity orbitalParent;
        private bool _hasLaunched = false;
        public static void CreateCommand(Guid faction, Entity orderEntity, Guid lauchSlot)
        {
            var cmd = new LaunchShipCmd()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,

            };

            var parent = Entity.GetSOIParentEntity(orderEntity);


            cmd._launchSlot = lauchSlot;

            
            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                var portDB = _entityCommanding.GetDataBlob<ShipYardAbilityDB>();
                
                foreach (ShipYardJob job in portDB.JobBatchList)
                {
                    if (job.SlipID == _launchSlot)
                    {
                        yardJob = job;
                        if(_entityCommanding.HasDataBlob<ColonyInfoDB>())
                        {
                            var planet = _entityCommanding.GetDataBlob<ColonyInfoDB>().PlanetEntity;
                            FuelCost = OrbitMath.FuelCostToLowOrbit(planet, yardJob.ShipDesign.Mass);
                            targetPosition = new Vector3(0, OrbitMath.LowOrbitRadius(planet), 0);
                            IsRunning = true;
                        }
                        else
                        {
                            FuelCost = OrbitMath.TsiolkovskyFuelCost(yardJob.ShipDesign.Mass, 275, 1);
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
                ShipFactory.CreateShip(yardJob.ShipDesign, _factionEntity, targetPosition, orbitalParent, (StarSystem)orbitalParent.Manager);
                _hasLaunched = true;
            }
        }

        internal override bool IsFinished()
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