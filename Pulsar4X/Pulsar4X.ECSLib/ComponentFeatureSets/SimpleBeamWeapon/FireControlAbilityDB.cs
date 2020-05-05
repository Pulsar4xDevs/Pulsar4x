using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Other than giving an entity a datablob that says it can do fire control,
    /// it just links component instsances for convenience.  
    /// </summary>
    public class FireControlAbilityDB : BaseDataBlob
    {
        [JsonIgnore]
        public List<ComponentInstance> FireControlInstances = new List<ComponentInstance>();
        [JsonIgnore]
        public List<ComponentInstance> WeaponInstances = new List<ComponentInstance>();


        public FireControlAbilityDB()
        {
        }

        FireControlAbilityDB(FireControlAbilityDB db)
        {
            
        }

        public override object Clone()
        {
            return new FireControlAbilityDB(this);
        }

        // JSON deserialization callback.
        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            var instancesDB = OwningEntity.GetDataBlob<ComponentInstancesDB>();
            if (instancesDB.TryGetComponentsByAttribute<BeamFireControlAtbDB>(out var fireControlInstances))
            {
                foreach (var fc in fireControlInstances)
                {
                    FireControlInstances.Add(fc);
                }
            }
            if (instancesDB.TryGetComponentsByAttribute<SimpleBeamWeaponAtbDB>(out var weaponInstances))
            {
                foreach (var gun in weaponInstances)
                {
                    WeaponInstances.Add(gun);
                }
            }
        }
    }

    /// <summary>
    /// Fire control instance ability state.
    /// This goes on each fire control component instance
    /// </summary>
    public class FireControlAbilityState : ComponentTreeHeirarchyAbilityState
    {
        public Entity Target { get; private set; }
        private NameDB _TargetNameDB;

        internal void SetTarget(Entity target)
        {
            Target = target;
            if (target == null)
                _TargetNameDB = null;
            else
                _TargetNameDB = target.GetDataBlob<NameDB>();
            
        }

        private Guid _factionOwner;
        public string TargetName
        {
            get
            {
                if (_TargetNameDB == null)
                    return "No Target";
                else
                {
                    return _TargetNameDB.GetName(_factionOwner);
                }
            }
        }

        //public ComponentInstance[] AssignedWeapons {get{return GetChildrenOfType<WeaponState>()}}

        public bool IsEngaging { get; internal set; } = false;
        

        public FireControlAbilityState(ComponentInstance componentInstance) : base(componentInstance)
        {
            _factionOwner = componentInstance.ParentInstances.OwningEntity.FactionOwner;
        }

        public FireControlAbilityState(FireControlAbilityState db) : base(db.ComponentInstance)
        {
            Target = db.Target;
            IsEngaging = db.IsEngaging;
        }
    }
}
