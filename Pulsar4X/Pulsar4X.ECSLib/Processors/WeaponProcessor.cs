using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public static class WeaponProcessor
    {


        public static void FireBeamWeapons(StarSystem starSys, Entity beamWeapon)
        {

            WeaponStateDB stateInfo = beamWeapon.GetDataBlob<WeaponStateDB>();
            FireControlInstanceAbilityDB fireControl = stateInfo.FireControl.GetDataBlob<FireControlInstanceAbilityDB>();

            // only fire if the beam weapon is finished with its cooldown
            if (stateInfo.CoolDown <= TimeSpan.FromSeconds(0) && stateInfo.FireControl != null && fireControl.IsEngaging)
            {
                //TODO chance to hit
                //int damageAmount = 10;//TODO damageAmount calc
                int damageAmount = beamWeapon.GetDataBlob<BeamWeaponAtbDB>().BaseDamage; // TODO: Better damage calculation

                double range = fireControl.Target.GetDataBlob<PositionDB>().GetDistanceTo(beamWeapon.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>());

                // only fire if target is in range
                if (range <= Math.Min(beamWeapon.GetDataBlob<BeamWeaponAtbDB>().MaxRange, stateInfo.FireControl.GetDataBlob<BeamFireControlAtbDB>().Range))
                {
                    DamageProcessor.OnTakingDamage(stateInfo.FireControl, damageAmount);
                    stateInfo.CoolDown = TimeSpan.FromSeconds(beamWeapon.GetDataBlob<BeamWeaponAtbDB>().PowerRechargeRate);
                    starSys.SystemManager.ManagerSubpulses.AddEntityInterupt(starSys.SystemManager.ManagerSubpulses.SystemLocalDateTime + stateInfo.CoolDown, PulseActionEnum.SomeOtherProcessor, beamWeapon);
                }

                
            }            
        }

        public static void RecalcBeamWeapons(Entity ship)
        {
            List<KeyValuePair<Entity, List<Entity>>> beamWeaponEntities = ship.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<BeamWeaponAtbDB>()).ToList();
            List<Entity>fireControlEntities = new List<Entity>();

            BeamWeaponsDB bwDB;

            int numFireControls = 0 ;
            int numBeamWeapons = 0;
            int totalDamage = 0;
            int maxDamage = 0;
            int maxRange = 0;
            int maxTrackingSpeed = 0;

            foreach (KeyValuePair<Entity, List<Entity>> beamWeaponTemplate in beamWeaponEntities)
            {
                foreach(Entity beamWeapon in beamWeaponTemplate.Value)
                {
                    WeaponStateDB state = beamWeapon.GetDataBlob<WeaponStateDB>();
                    BeamWeaponAtbDB bwAtb = beamWeapon.GetDataBlob<BeamWeaponAtbDB>();
                    BeamFireControlAtbDB fcAtb = state.FireControl.GetDataBlob<BeamFireControlAtbDB>();

                    if (!fireControlEntities.Contains(state.FireControl)) ; //This semi-colon is probably bad
                        fireControlEntities.Add(state.FireControl);

                    numBeamWeapons++;
                    totalDamage += bwAtb.BaseDamage; // How is damage at any range calculated?
                    if (bwAtb.BaseDamage > maxDamage)
                        maxDamage = bwAtb.BaseDamage;
                    if (bwAtb.MaxRange > maxRange)
                        if (fcAtb.Range > bwAtb.MaxRange)
                            maxRange = bwAtb.MaxRange;
                        else if(fcAtb.Range > maxRange)
                            maxRange = fcAtb.Range;

                    if (fcAtb.TrackingSpeed > maxTrackingSpeed)
                        maxTrackingSpeed = fcAtb.TrackingSpeed;
                }
            }
            numFireControls = fireControlEntities.Count;

            bwDB = ship.GetDataBlob<BeamWeaponsDB>();

            bwDB.NumFireControls = numFireControls;
            bwDB.NumBeamWeapons = numBeamWeapons;
            bwDB.TotalDamage = totalDamage;
            bwDB.MaxDamage = maxDamage;
            bwDB.MaxRange = maxRange;
            bwDB.MaxTrackingSpeed = maxTrackingSpeed;
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
