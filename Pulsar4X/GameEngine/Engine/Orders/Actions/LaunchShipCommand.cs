using System;
using Pulsar4X.Orbital;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Extensions;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Designs;

namespace Pulsar4X.Engine.Orders
{
    public class LaunchShipCommand : EntityCommand
    {
        public override ActionLaneTypes ActionLanes => ActionLaneTypes.IneteractWithSelf;
        public override bool IsBlocking => true;
        public override string Name { get; } = "Launch Ship From Storage";
        public override string Details { get; } = "";

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }
        IndustryJob _yardJob;
        private string _launchSlot;
        private string _jobID;
        public double FuelCost;

        private Vector3 targetPosition;
        private Entity orbitalParent = null;
        private bool _hasLaunched = false;

        public static void CreateCommand(string faction, Entity orderEntity, string lauchSlot, string jobID)
        {
            var cmd = new LaunchShipCommand()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                _launchSlot = lauchSlot,
                _jobID = jobID

            };

            var parent = orderEntity.GetSOIParentEntity();
            orderEntity.Manager.Game.OrderHandler.HandleOrder(cmd);
        }

        internal override void Execute(DateTime atDateTime)
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
                            targetPosition = _entityCommanding.GetDataBlob<PositionDB>().RelativePosition;
                            IsRunning = true;
                        }

                    }
                }
            }
            else //IsRunning
            {
                //_entityCommanding.GetDataBlob<CargoStorageDB>().StoredCargoTypes
                ShipDesign design = (ShipDesign)_factionEntity.GetDataBlob<FactionInfoDB>().IndustryDesigns[_yardJob.ItemGuid];
                ShipFactory.CreateShip(design, _factionEntity, targetPosition, orbitalParent);
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

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }
}