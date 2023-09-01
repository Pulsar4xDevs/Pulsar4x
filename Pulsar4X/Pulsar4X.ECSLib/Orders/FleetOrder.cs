using System;

namespace Pulsar4X.ECSLib
{
    public enum FleetOrderType
    {
        Create,
        Disband,
        ChangeParent,
        AssignShip,
        UnassignShip,
    }
    public class FleetOrder : EntityCommand
    {
        public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

        public override bool IsBlocking => false;

        public override string Name => "Fleet Order (" + OrderType.ToString() + ")";

        public FleetOrderType OrderType { get; private set; }

        public override string Details => Name;

        private Entity _factionEntity;
        private Entity _entityCommanding;
        private Entity _targetEntity;
        private string _requestedName;
        private EntityManager _manager;
        internal override Entity EntityCommanding
        {
            get { return _entityCommanding; }
        }

        private bool isFinished = false;

        public override bool IsFinished()
        {
            return isFinished;
        }

        private FleetOrder(Guid factionGuid, Entity entity)
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = entity.Guid;
            CreatedDate = entity.StarSysDateTime;
            UseActionLanes = true;
        }

        public static FleetOrder CreateFleetOrder(string name, Entity faction)
        {
            var order = new FleetOrder(faction.Guid, faction)
            {
                OrderType = FleetOrderType.Create,
                _requestedName = name,
            };

            return order;
        }

        public static FleetOrder DisbandFleet(Guid requestingFaction, Entity fleet)
        {
            var order = new FleetOrder(requestingFaction, fleet)
            {
                OrderType = FleetOrderType.Disband
            };

            return order;
        }

        public static FleetOrder ChangeParent(Guid requestingFaction, Entity sourceFleet, Entity target)
        {
            var order = new FleetOrder(requestingFaction, sourceFleet)
            {
                OrderType = FleetOrderType.ChangeParent,
                _targetEntity = target
            };

            return order;
        }

        public static FleetOrder AssignShip(Guid requestinFaction, Entity fleet, Entity ship)
        {
            var order = new FleetOrder(requestinFaction, fleet)
            {
                OrderType = FleetOrderType.AssignShip,
                _targetEntity = ship
            };

            return order;
        }

        public static FleetOrder UnassignShip(Guid requestinFaction, Entity fleet, Entity ship)
        {
            var order = new FleetOrder(requestinFaction, fleet)
            {
                OrderType = FleetOrderType.UnassignShip,
                _targetEntity = ship
            };

            return order;
        }

        internal override void ActionCommand(DateTime atDateTime)
        {
            var factionRoot = _factionEntity.GetDataBlob<NavyDB>();
            switch(OrderType)
            {
                case FleetOrderType.Create:
                    var fleet = FleetFactory.Create(_manager, RequestingFactionGuid, _requestedName);
                    _factionEntity.GetDataBlob<NavyDB>().AddChild(fleet);
                    break;
                case FleetOrderType.Disband:
                    if(factionRoot.Children.Contains(_entityCommanding))
                    {
                        factionRoot.RemoveChild(_entityCommanding);
                    }
                    else
                    {
                        var fleetInfo = _entityCommanding.GetDataBlob<NavyDB>();
                        fleetInfo.Children.Clear();
                        fleetInfo.ParentDB.RemoveChild(_entityCommanding);
                    }
                    break;
                case FleetOrderType.ChangeParent:
                    // Remove the entity from the parent tree
                    var sourceFleetInfo = _entityCommanding.GetDataBlob<NavyDB>();

                    // Check if nested
                    if(sourceFleetInfo.Root != _entityCommanding)
                    {
                        sourceFleetInfo.ParentDB.RemoveChild(_entityCommanding);
                        sourceFleetInfo.ClearParent();
                    }

                    if(factionRoot.Children.Contains(_entityCommanding))
                    {
                        factionRoot.RemoveChild(_entityCommanding);
                    }

                    // Drop the dragEntity
                    sourceFleetInfo.SetParent(_targetEntity);
                    break;
                case FleetOrderType.AssignShip:
                    _entityCommanding.GetDataBlob<NavyDB>().AddChild(_targetEntity);
                    break;
                case FleetOrderType.UnassignShip:
                    _entityCommanding.GetDataBlob<NavyDB>().RemoveChild(_targetEntity);
                    break;
            }

            isFinished = true;
        }

        internal override bool IsValidCommand(Game game)
        {
            _manager = game.GlobalManager;

            switch(OrderType)
            {
                case FleetOrderType.Create:
                    if(_manager.FindEntityByGuid(RequestingFactionGuid, out _factionEntity)
                        && _manager.FindEntityByGuid(EntityCommandingGuid, out _entityCommanding))
                    {
                        return true;
                    }
                    break;
                default:
                    if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }
    }
}