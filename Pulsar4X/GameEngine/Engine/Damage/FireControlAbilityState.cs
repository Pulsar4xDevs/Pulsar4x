using Pulsar4X.Components;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine.Damage
{
    /// <summary>
    /// Fire control instance ability state.
    /// This goes on each fire control component instance
    /// </summary>
    public class FireControlAbilityState : ComponentTreeHeirarchyAbilityState
    {
        public Entity Target { get; private set; }
        private NameDB? _TargetNameDB;

        internal void SetTarget(Entity target)
        {
            Target = target;
            if (target == null)
                _TargetNameDB = null;
            else
                _TargetNameDB = target.GetDataBlob<NameDB>();
        }

        private int _factionOwner;
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
            _factionOwner = componentInstance.ParentInstances.OwningEntity.FactionOwnerID;
        }

        public FireControlAbilityState(FireControlAbilityState db) : base(db.ComponentInstance)
        {
            Target = db.Target;
            IsEngaging = db.IsEngaging;
        }
    }
}
