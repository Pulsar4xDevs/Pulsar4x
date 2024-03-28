using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Components;
using Pulsar4X.Engine.Damage;
using Pulsar4X.Engine.Designs;

namespace Pulsar4X.Engine.Orders
{
    public class SetWeaponsFireControlOrder : EntityCommand
    {
        public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

        public override bool IsBlocking => false;
        public override string Name { get; } = "Fire Control Set Weapons";
        public override string Details { get; } = "";

        [JsonIgnore]
        Entity _factionEntity;

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }



        [JsonProperty]
        public string FireControlGuid;
        [JsonIgnore]
        private ComponentInstance _fireControlComponent;

        public List<string> WeaponsAssigned = new List<string>();
        private List<WeaponState> _weaponsAssigned = new List<WeaponState>();


        public static void CreateCommand(Game game, DateTime starSysDate, int factionGuid, int orderEntity, string fireControlGuid, List<string> weaponsAssigned)
        {
            var cmd = new SetWeaponsFireControlOrder()
            {
                RequestingFactionGuid = factionGuid,
                EntityCommandingGuid = orderEntity,
                CreatedDate = starSysDate,
                FireControlGuid = fireControlGuid,
                WeaponsAssigned = weaponsAssigned,
                UseActionLanes = false
            };
            game.OrderHandler.HandleOrder(cmd);
        }



        internal override void Execute(DateTime atDateTime)
        {
            if (!IsRunning)
            {
                var fcState = _fireControlComponent.GetAbilityState<FireControlAbilityState>();

                fcState.SetChildren(_weaponsAssigned.ToArray());
                IsRunning = true;
            }
        }

        public override bool IsFinished()
        {
            if (IsRunning)
                return true;
            return false;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                var instancesdb = _entityCommanding.GetDataBlob<ComponentInstancesDB>();

                if (instancesdb.AllComponents.TryGetValue(FireControlGuid, out var fc))
                {
                    if (fc.HasAblity<FireControlAbilityState>())
                    {
                        _fireControlComponent = fc;

                        if (instancesdb.TryGetComponentStates<WeaponState>(out var wpns))
                        {

                            foreach (var wpnGuid in WeaponsAssigned)
                            {
                                foreach (var wpnState in wpns)
                                {
                                    if (wpnState.ComponentInstance.UniqueID == wpnGuid)
                                        _weaponsAssigned.Add(wpnState);
                                }
                            }


                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class SetTargetFireControlOrder : EntityCommand
    {
        public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

        public override bool IsBlocking => false;

        public override string Name { get; } = "Fire Control Set Target";
        public override string Details { get; } = "";


        [JsonIgnore]
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        [JsonIgnore]
        Entity _factionEntity;

        [JsonProperty]
        public int TargetSensorEntityGuid { get; set; }
        private Entity _targetSensorEntity;
        private Entity _targetActualEntity;

        [JsonProperty]
        public string FireControlGuid;
        [JsonIgnore]
        private ComponentInstance _fireControlComponent;


        public static void CreateCommand(Game game, DateTime starSysDate, int factionGuid, int orderEntity, string fireControlGuid, int targetGuid)
        {
            var cmd = new SetTargetFireControlOrder()
            {
                RequestingFactionGuid = factionGuid,
                EntityCommandingGuid = orderEntity,
                CreatedDate = starSysDate,
                FireControlGuid = fireControlGuid,
                TargetSensorEntityGuid = targetGuid,
                UseActionLanes = false,
            };
            game.OrderHandler.HandleOrder(cmd);
        }



        internal override void Execute(DateTime atDateTime)
        {
            if (!IsRunning)
            {
                _fireControlComponent.GetAbilityState<FireControlAbilityState>().SetTarget(_targetActualEntity);
            }

        }

        public override bool IsFinished()
        {
            if (IsRunning)
                return true;
            return false;
            //if target is dead? or not seen for x amount of time?
        }

        internal override bool IsValidCommand(Game game)
        {
            //see if we can successfully turn the guids into entites.
            //the reason we use guids is to make it easier to serialise for network play.
            //getting the entites makes it a bit easier to ActionCommand though
            //it may also be a good idea to double check that the entites we're looking for have specific DBs to prevent a crash...
            //IsCommandValid also checks that the entity we're commanding is owned by our faction.
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.TryGetGlobalEntityById(TargetSensorEntityGuid, out _targetSensorEntity))
                {
                    if (_targetSensorEntity.HasDataBlob<SensorInfoDB>()) //we want to damage the actual entity, not the sensor clone.
                        _targetActualEntity = _targetSensorEntity.GetDataBlob<SensorInfoDB>().DetectedEntity;
                    else
                        _targetActualEntity = _targetSensorEntity;
                    var instancesdb = _entityCommanding.GetDataBlob<ComponentInstancesDB>();

                    if (instancesdb.AllComponents.TryGetValue(FireControlGuid, out var fc))
                    {
                        if (fc.HasAblity<FireControlAbilityState>())
                        {
                            _fireControlComponent = fc;

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class SetOpenFireControlOrder : EntityCommand
    {
        public enum FireModes:byte //this could be made more complex, rapid/overdrive, staggered, alphastrike,
        {
            OpenFire,
            CeaseFire
        }


        public override string Name { get; } = "Fire Control Set Firemode";
        public override string Details {
            get
            {
                return IsFiring.ToString();
            }
        }

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

        public override bool IsBlocking => false;

        [JsonIgnore]
        Entity _factionEntity;

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }



        [JsonProperty]
        public string FireControlGuid;
        [JsonIgnore]
        private ComponentInstance _fireControlComponent;

        public FireModes IsFiring;
        private Game _game;
        
        public static void CreateCmd(Game game, Entity faction, Entity shipEntity, string fireControlGuid, FireModes isFiring)
        {
            var cmd = new SetOpenFireControlOrder()
            {
                RequestingFactionGuid = faction.Id,
                EntityCommandingGuid = shipEntity.Id,
                CreatedDate = shipEntity.Manager.ManagerSubpulses.StarSysDateTime,
                FireControlGuid = fireControlGuid,
                IsFiring = isFiring,
                _game = game
            };
            game.OrderHandler.HandleOrder(cmd);
            
        }

        internal override void Execute(DateTime atDateTime)
        {
            if (!IsRunning)
            {
                var fcState = _fireControlComponent.GetAbilityState<FireControlAbilityState>();
                if (IsFiring == FireModes.OpenFire)
                {
                    fcState.IsEngaging = true;
                    DateTime dateTimeNow = _entityCommanding.Manager.ManagerSubpulses.StarSysDateTime;
                    if(!_entityCommanding.TryGetDatablob(out GenericFiringWeaponsDB blob))
                    {
                        blob = new GenericFiringWeaponsDB(fcState.GetChildrenInstances());
                        _entityCommanding.SetDataBlob(blob);
                    }
                    else
                    {
                        blob.AddWeapons(fcState.GetChildrenInstances());
                    }
                    
                    //_game.ProcessorManager.RunInstanceProcessOnEntity(nameof(WeaponProcessor),_entityCommanding,  dateTimeNow);
                }
                else if (IsFiring == FireModes.CeaseFire)
                {
                    fcState.IsEngaging = false;
                    GenericFiringWeaponsDB blob = _entityCommanding.GetDataBlob<GenericFiringWeaponsDB>();
                }
                IsRunning = true;
            }
        }

        public override bool IsFinished()
        {
            if (IsRunning)
                return true;
            return false;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                var instancesdb = _entityCommanding.GetDataBlob<ComponentInstancesDB>();
                if (instancesdb.AllComponents.TryGetValue(FireControlGuid, out var fc))
                {
                    if (fc.HasAblity<FireControlAbilityState>())
                    {
                        _fireControlComponent = fc;

                        return true;
                    }
                }
            }
            return false;
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class SetOrdinanceToWpnOrder : EntityCommand
    {

        public override string Name { get; } = "Fire Control Set Ordnance";
        public override string Details { get; } = "";

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

        public override bool IsBlocking => false;

        [JsonIgnore]
        Entity _factionEntity;

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }



        [JsonProperty]
        public string WeaponGuid;
        [JsonIgnore]
        private ComponentInstance _weaponInstance;

        public string OrdnanceAssigned;
        private OrdnanceDesign _ordnanceAssigned;


        public static void CreateCommand(DateTime starSysDate, Entity faction, Entity orderEntity, WeaponState weapon, string ordnanceAssigned)
        {
            var cmd = new SetOrdinanceToWpnOrder()
            {
                RequestingFactionGuid = faction.Id,
                EntityCommandingGuid = orderEntity.Id,
                _entityCommanding = orderEntity,
                CreatedDate = starSysDate,
                WeaponGuid = weapon.ID,
                OrdnanceAssigned = ordnanceAssigned
            };
            cmd.EntityCommanding.Manager.Game.OrderHandler.HandleOrder(cmd);
        }



        internal override void Execute(DateTime atDateTime)
        {
            if (!IsRunning)
            {   
                var wpnState = _weaponInstance.GetAbilityState<WeaponState>();
                wpnState.FireWeaponInstructions.AssignOrdnance(_ordnanceAssigned);
                IsRunning = true;
            }
        }

        public override bool IsFinished()
        {
            if (IsRunning)
                return true;
            return false;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                var instancesdb = _entityCommanding.GetDataBlob<ComponentInstancesDB>();

                if (instancesdb.AllComponents.TryGetValue(WeaponGuid, out ComponentInstance? wpn))
                {
                    _ordnanceAssigned = _factionEntity.GetDataBlob<FactionInfoDB>().MissileDesigns[OrdnanceAssigned];
                    if(wpn.TryGetAbilityState(out WeaponState? wpnState))
                    {
                        _weaponInstance = wpn;
                        return wpnState.FireWeaponInstructions.CanLoadOrdnance(_ordnanceAssigned);
                    }
                }
            }
            return false;
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }
}
