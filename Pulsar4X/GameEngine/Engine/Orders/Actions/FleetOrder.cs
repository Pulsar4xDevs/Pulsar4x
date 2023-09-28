using System;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine.Orders
{
    public enum FleetOrderType
    {
        Create,
        Disband,
        ChangeParent,
        AssignShip,
        UnassignShip,
        SetFlagShip,
        ToggleInheritOrders,
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

        private FleetOrder(string factionGuid, Entity entity)
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

        public static FleetOrder DisbandFleet(string requestingFaction, Entity fleet)
        {
            var order = new FleetOrder(requestingFaction, fleet)
            {
                OrderType = FleetOrderType.Disband
            };

            return order;
        }

        public static FleetOrder ChangeParent(string requestingFaction, Entity sourceFleet, Entity target)
        {
            var order = new FleetOrder(requestingFaction, sourceFleet)
            {
                OrderType = FleetOrderType.ChangeParent,
                _targetEntity = target
            };

            return order;
        }

        public static FleetOrder AssignShip(string requestinFaction, Entity fleet, Entity ship)
        {
            var order = new FleetOrder(requestinFaction, fleet)
            {
                OrderType = FleetOrderType.AssignShip,
                _targetEntity = ship
            };

            return order;
        }

        public static FleetOrder UnassignShip(string requestinFaction, Entity fleet, Entity ship)
        {
            var order = new FleetOrder(requestinFaction, fleet)
            {
                OrderType = FleetOrderType.UnassignShip,
                _targetEntity = ship
            };

            return order;
        }

        public static FleetOrder SetFlagShip(string requestingFaction, Entity fleet, Entity ship)
        {
            var order = new FleetOrder(requestingFaction, fleet)
            {
                OrderType = FleetOrderType.SetFlagShip,
                _targetEntity = ship
            };

            return order;
        }

        public static FleetOrder ToggleInheritOrders(string requestingFaction, Entity fleet)
        {
            var order = new FleetOrder(requestingFaction, fleet)
            {
                OrderType = FleetOrderType.ToggleInheritOrders
            };

            return order;
        }

        internal override void Execute(DateTime atDateTime)
        {
            var factionRoot = _factionEntity.GetDataBlob<FleetDB>();
            switch(OrderType)
            {
                case FleetOrderType.Create:
                    var fleet = FleetFactory.Create(_manager, RequestingFactionGuid, _requestedName);
                    fleet.GetDataBlob<FleetDB>().SetParent(_factionEntity);
                    break;
                case FleetOrderType.Disband:
                    var navyDB = _entityCommanding.GetDataBlob<FleetDB>();

                    // Handle the children of the disbanding fleet
                    // Sub-fleets:
                    //  - Should assign to the parent of the disbanding fleet
                    // Ships:
                    //  - Should assign un-attached to the root
                    if(navyDB.Children.Count > 0)
                    {
                        foreach(var child in navyDB.GetChildren())
                        {
                            // Fleet
                            if(child.HasDataBlob<FleetDB>())
                            {
                                var childDB = child.GetDataBlob<FleetDB>();
                                childDB.SetParent(navyDB.Parent);
                            }
                            // Ship
                            else
                            {
                                factionRoot.AddChild(child);
                            }
                        }
                    }

                    if(factionRoot.Children.Contains(_entityCommanding))
                    {
                        factionRoot.RemoveChild(_entityCommanding);
                    }
                    else
                    {
                        navyDB.Children.Clear();
                        navyDB.ParentDB.RemoveChild(_entityCommanding);
                    }

                    _entityCommanding.Manager.RemoveEntity(_entityCommanding);
                    break;
                case FleetOrderType.ChangeParent:
                    // Remove the entity from the parent tree
                    var sourceFleetInfo = _entityCommanding.GetDataBlob<FleetDB>();

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
                    navyDB = _entityCommanding.GetDataBlob<FleetDB>();

                    // If no children or no flagship set the ship as the flagship
                    if(navyDB.Children.Count == 0 || navyDB.FlagShipID == String.Empty)
                    {
                        navyDB.FlagShipID = _targetEntity.Guid;
                        // update to the ships manager
                        _entityCommanding.Transfer(_targetEntity.Manager);
                    }

                    navyDB.AddChild(_targetEntity);
                    break;
                case FleetOrderType.UnassignShip:
                    navyDB = _entityCommanding.GetDataBlob<FleetDB>();
                    navyDB.RemoveChild(_targetEntity);

                    if(_targetEntity.Guid == navyDB.FlagShipID)
                    {
                        navyDB.FlagShipID = String.Empty;
                        // if we have no flagship, move to the global entity manager
                        _entityCommanding.Transfer(_manager);
                    }
                    break;
                case FleetOrderType.SetFlagShip:
                    if(_entityCommanding.Manager != _targetEntity.Manager)
                    {
                        _entityCommanding.Transfer(_targetEntity.Manager);
                    }
                    _entityCommanding.GetDataBlob<FleetDB>().FlagShipID = _targetEntity.Guid;
                    break;
                case FleetOrderType.ToggleInheritOrders:
                    navyDB = _entityCommanding.GetDataBlob<FleetDB>();
                    navyDB.InheritOrders = !navyDB.InheritOrders;
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

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }
}