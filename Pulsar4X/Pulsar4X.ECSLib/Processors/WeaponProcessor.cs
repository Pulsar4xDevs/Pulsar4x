using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    static class WeaponProcessor
    {


        static void FireBeamWeapons(StarSystem starSys, Entity beamWeapon)
        {

            WeaponStateDB stateInfo = beamWeapon.GetDataBlob<WeaponStateDB>();
            FireControlInstanceAbilityDB fireControl = stateInfo.FireControl.GetDataBlob<FireControlInstanceAbilityDB>();
            if (stateInfo.CoolDown <= TimeSpan.FromSeconds(0) && stateInfo.FireControl != null && fireControl.IsEngaging)
            {
                //TODO chance to hit
                int damageAmount = 10;//TODO damageAmount calc
                double range = fireControl.Target.GetDataBlob<PositionDB>().GetDistanceTo(beamWeapon.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>());
                ShipDamageProcessor.OnTakingDamage(stateInfo.FireControl, damageAmount);
                stateInfo.CoolDown = TimeSpan.FromSeconds(beamWeapon.GetDataBlob<BeamWeaponAtbDB>().PowerRechargeRate);
                starSys.SystemSubpulses.AddEntityInterupt(starSys.SystemSubpulses.SystemLocalDateTime + stateInfo.CoolDown, PulseActionEnum.SomeOtherProcessor, beamWeapon);
                
            }            
        }
    }



    public static class FireControlProcessor
    {

        public static void SetWeaponToFC(Entity fireControlInstance, Entity weaponInstance)
        {
            if (fireControlInstance.HasDataBlob<FireControlInstanceAbilityDB>() && weaponInstance.HasDataBlob<WeaponStateDB>())
                weaponInstance.GetDataBlob<WeaponStateDB>().FireControl = fireControlInstance;
            else
                throw new Exception("needs FireContInstanceAbilityDB on fireControlInstance, and WeaponStateDB on weaponInstance");
        }

        public static void RemoveWeaponFromFC(Entity weaponInstance)
        {
            if (weaponInstance.HasDataBlob<WeaponStateDB>())
                weaponInstance.GetDataBlob<WeaponStateDB>().FireControl = null;
            else
                throw new Exception("needs WeaponStateDB on weaponInstance");
        }

        public static void SetTarget(Entity fireControlInstance, Entity target)
        {
            if (fireControlInstance.HasDataBlob<FireControlInstanceAbilityDB>())
                fireControlInstance.GetDataBlob<FireControlInstanceAbilityDB>().Target = target;
            else
                throw new Exception("No FireContInstanceAbilityDB on entity");
        }

    }




}
