using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class SetWeaponsFireControlOrder : EntityCommand
    {
        internal override int ActionLanes => 1;

        internal override bool IsBlocking => false;

        [JsonIgnore]
        Entity _factionEntity;

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }



        [JsonProperty]
        public Guid FireControlGuid;
        [JsonIgnore]
        private ComponentInstance _fireControlComponent;

        public List<Guid> WeaponsAssigned = new List<Guid>();
        private List<ComponentInstance> _weaponsAssigned = new List<ComponentInstance>();


        public static void CreateCommand(Game game, DateTime starSysDate, Guid factionGuid, Guid orderEntity, Guid fireControlGuid, List<Guid> weaponsAssigned)
        {
            var cmd = new SetWeaponsFireControlOrder()
            {
                RequestingFactionGuid = factionGuid,
                EntityCommandingGuid = orderEntity,
                CreatedDate = starSysDate,
                FireControlGuid = fireControlGuid,
                WeaponsAssigned = weaponsAssigned
            };
            game.OrderHandler.HandleOrder(cmd);
        }



        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                var fcinstance = _fireControlComponent.GetAbilityState<FireControlAbilityState>();
                fcinstance.AssignedWeapons = _weaponsAssigned;
                foreach (var wpn in _weaponsAssigned)
                {
                    wpn.GetAbilityState<WeaponState>().FireControl = _fireControlComponent;
                }
                IsRunning = true;
            }
        }

        internal override bool IsFinished()
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
                        
                        foreach (var wpnGuid in WeaponsAssigned)
                        {
                            if (instancesdb.AllComponents.TryGetValue(wpnGuid, out var wpn))
                            {
                                if (wpn.HasAblity<WeaponState>())
                                {
                                    _weaponsAssigned.Add(wpn);
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class SetTargetFireControlOrder : EntityCommand
    {
        internal override int ActionLanes => 1;

        internal override bool IsBlocking => false;

        [JsonIgnore]
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        [JsonIgnore]
        Entity _factionEntity;

        [JsonProperty]
        public Guid TargetSensorEntityGuid { get; set; }
        private Entity _targetSensorEntity;
        private Entity _targetActualEntity;

        [JsonProperty]
        public Guid FireControlGuid;
        [JsonIgnore]
        private ComponentInstance _fireControlComponent;


        public static void CreateCommand(Game game, DateTime starSysDate, Guid factionGuid, Guid orderEntity, Guid fireControlGuid, Guid targetGuid)
        {
            var cmd = new SetTargetFireControlOrder()
            {
                RequestingFactionGuid = factionGuid,
                EntityCommandingGuid = orderEntity,
                CreatedDate = starSysDate,
                FireControlGuid = fireControlGuid,
                TargetSensorEntityGuid = targetGuid,
            };
            game.OrderHandler.HandleOrder(cmd);
        }



        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                _fireControlComponent.GetAbilityState<FireControlAbilityState>().Target = _targetActualEntity;
            }

        }

        internal override bool IsFinished()
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
                if (game.GlobalManager.FindEntityByGuid(TargetSensorEntityGuid, out _targetSensorEntity))
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
    }

    public class SetOpenFireControlOrder : EntityCommand
    {
        public enum FireModes:byte //this could be made more complex, rapid/overdrive, staggered, alphastrike, 
        {
            OpenFire,
            CeaseFire
        }
        internal override int ActionLanes => 1;

        internal override bool IsBlocking => false;

        [JsonIgnore]
        Entity _factionEntity;

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }



        [JsonProperty]
        public Guid FireControlGuid;
        [JsonIgnore]
        private ComponentInstance _fireControlComponent;

        public FireModes IsFiring;

        public static void CreateCmd(Game game, Entity faction, Entity shipEntity, Guid fireControlGuid, FireModes isFiring)
        {
            var cmd = new SetOpenFireControlOrder()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = shipEntity.Guid,
                CreatedDate = shipEntity.Manager.ManagerSubpulses.StarSysDateTime,
                FireControlGuid = fireControlGuid,
                IsFiring = isFiring
            };
            game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                var fcinstance = _fireControlComponent.GetAbilityState<FireControlAbilityState>();
                if (IsFiring == FireModes.OpenFire)
                {
                    fcinstance.IsEngaging = true;
                    DateTime dateTimeNow = _entityCommanding.Manager.ManagerSubpulses.StarSysDateTime;
                    foreach (var wpn in fcinstance.AssignedWeapons)
                        StaticRefLib.ProcessorManager.RunInstanceProcessOnEntity(nameof(WeaponProcessor),_entityCommanding,  dateTimeNow);
                }
                else if (IsFiring == FireModes.CeaseFire)
                {
                    fcinstance.IsEngaging = false;
                }
                IsRunning = true;
            }
        }

        internal override bool IsFinished()
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
    }
}
