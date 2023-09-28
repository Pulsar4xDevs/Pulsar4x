using Pulsar4X.Interfaces;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Engine.Damage;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Atb
{
    public class MissileLauncherAtb : IComponentDesignAttribute, IFireWeaponInstr
    {
        public double LauncherSize;
        public double ReloadRate;
        public double LaunchForce;
        public OrdnanceDesign AssignedOrdnance { get; private set; }

        public MissileLauncherAtb()
        {
        }
        public MissileLauncherAtb(double maxMissileWeight, double reloadRate, double launchForce)
        {
            LauncherSize = maxMissileWeight;
            ReloadRate = reloadRate;
            LaunchForce = launchForce;
        }

        public bool CanLoadOrdnance(OrdnanceDesign ordnanceDesign)
        {
            //need to check ordnance type, size etc.
            return true;
        }

        public bool AssignOrdnance(OrdnanceDesign ordnanceDesign)
        {
            if (CanLoadOrdnance(ordnanceDesign))
            {
                AssignedOrdnance = ordnanceDesign;
                return true;
            }
            else return false;
        }

        public bool TryGetOrdnance(out OrdnanceDesign ordnanceDesign)
        {
            ordnanceDesign = AssignedOrdnance;
            return AssignedOrdnance != null;
        }

        public void FireWeapon(Entity launchingEntity, Entity tgtEntity, int count)
        {
            MissileProcessor.LaunchMissile(launchingEntity, tgtEntity, LaunchForce, AssignedOrdnance, count);
        }

        public float ToHitChance(Entity launchingEntity, Entity tgtEntity)
        {
            //should change this to 1 or 0 depending if the missile is "in range"
            //ie can actualy reach the target with the given amount of fuel on board.
            return 1;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!componentInstance.HasAblity<WeaponState>())
            {
                var wpnState = new WeaponState(componentInstance, this);
                wpnState.WeaponType = "Missile Launcher";
                wpnState.WeaponStats = new (string name, double value, ValueTypeStruct valueType)[3];
                wpnState.WeaponStats[0] = ("Max Size:", LauncherSize, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Distance, ValueTypeStruct.ValueSizes.Milli));
                wpnState.WeaponStats[1] = ("Launch Force:", LaunchForce, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Force, ValueTypeStruct.ValueSizes.BaseUnit));
                wpnState.WeaponStats[2] = ("Rate Of Fire:", ReloadRate, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Number, ValueTypeStruct.ValueSizes.BaseUnit));
                componentInstance.SetAbilityState<WeaponState>(wpnState);
            }
            /*
            if(!parentEntity.HasDataBlob<MissileLaunchersAbilityDB>())
            {
                var mla = new MissileLaunchersAbilityDB();
                parentEntity.SetDataBlob(mla);
            }*/
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {

        }

        public string AtbName()
        {
            return "Missle Launcher";
        }

        public string AtbDescription()
        {

            return " ";
        }
    }
}