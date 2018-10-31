using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class WeaponProcessor : IInstanceProcessor
    {
        internal override void ProcessEntity(Entity beamWeapon, DateTime atDate)
        {
            WeaponInstanceStateDB stateInfo = beamWeapon.GetDataBlob<WeaponInstanceStateDB>();
            if (stateInfo.FireControl != null) 
            {
                FireControlInstanceStateDB fireControl = stateInfo.FireControl.GetDataBlob<FireControlInstanceStateDB>();
                if (fireControl.IsEngaging)
                {
                    if (!stateInfo.ReadyToFire)
                    {
                        if (stateInfo.CoolDown <= atDate)
                        {
                            stateInfo.ReadyToFire = true;
                            FireBeamWeapons(beamWeapon, atDate);
                        }
                    }
                    else
                        FireBeamWeapons(beamWeapon, atDate);
                    beamWeapon.Manager.ManagerSubpulses.AddEntityInterupt(stateInfo.CoolDown, nameof(WeaponProcessor), beamWeapon);
                }
            }

            


            
            
            
        }

        public static void FireBeamWeapons(Entity beamWeapon, DateTime atDate)
        {

            WeaponInstanceStateDB stateInfo = beamWeapon.GetDataBlob<WeaponInstanceStateDB>();
            FireControlInstanceStateDB fireControl = stateInfo.FireControl.GetDataBlob<FireControlInstanceStateDB>();
            var myPos = beamWeapon.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>();
            var targetPos = fireControl.Target.GetDataBlob<PositionDB>();


            //TODO chance to hit
            //int damageAmount = 10;//TODO damageAmount calc
            var designAtb = beamWeapon.GetDataBlob<DesignInfoDB>().DesignEntity.GetDataBlob<SimpleBeamWeaponAtbDB>();
            int damageAmount = designAtb.DamageAmount; // TODO: Better damage calculation

            double range = myPos.GetDistanceTo(myPos);

            // only fire if target is in range TODO: fire anyway, but miss. TODO: this will be wrong if we do movement last, this needs to be done after movement. 
            if (range <= Math.Min(beamWeapon.GetDataBlob<BeamWeaponAtbDB>().MaxRange, stateInfo.FireControl.GetDataBlob<BeamFireControlAtbDB>().Range))
            {
                DamageProcessor.OnTakingDamage(stateInfo.FireControl, damageAmount);
                int reloadRate = designAtb.ReloadRate;
                stateInfo.CoolDown = atDate + TimeSpan.FromSeconds(reloadRate);
                stateInfo.ReadyToFire = false;    
            }



        }
    }

    public static class WeaponHelpers
    {




        public static void RecalcBeamWeapons(Entity ship)
        {
            var instancesDB = ship.GetDataBlob<ComponentInstancesDB>();

            var beamWeaponEntites = instancesDB.GetDesignsByType(typeof(BeamWeaponAtbDB));
            //List<KeyValuePair<Entity, PrIwObsList<Entity>>> beamWeaponEntities = instancesDB.SpecificInstances.GetInternalDictionary().Where(item => item.Key.HasDataBlob<BeamWeaponAtbDB>()).ToList();
            List<Entity>fireControlEntities = new List<Entity>();

            BeamWeaponsDB bwDB;

            int numFireControls = 0 ;
            int numBeamWeapons = 0;
            int totalDamage = 0;
            int maxDamage = 0;
            int maxRange = 0;
            int maxTrackingSpeed = 0;


            foreach (var beamWeapon in beamWeaponEntites)
            {
                var design = beamWeapon.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity;


            }

            /*
            foreach (KeyValuePair<Entity, PrIwObsList<Entity>> beamWeaponTemplate in beamWeaponEntities)
            {
                foreach(Entity beamWeapon in beamWeaponTemplate.Value)
                {
                    WeaponStateDB state = beamWeapon.GetDataBlob<WeaponStateDB>();
                    BeamWeaponAtbDB bwAtb = beamWeapon.GetDataBlob<BeamWeaponAtbDB>();
                    BeamFireControlAtbDB fcAtb = state.FireControl.GetDataBlob<BeamFireControlAtbDB>();

                    if (!fireControlEntities.Contains(state.FireControl)) 
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
            }*/
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
            if (fireControlInstance.HasDataBlob<FireControlInstanceStateDB>() && weaponInstance.HasDataBlob<WeaponInstanceStateDB>())
                weaponInstance.GetDataBlob<WeaponInstanceStateDB>().FireControl = fireControlInstance;
            else
                throw new Exception("needs FireContInstanceAbilityDB on fireControlInstance, and WeaponStateDB on weaponInstance");
        }

        public static void RemoveWeaponFromFC(Entity weaponInstance)
        {
            if (weaponInstance.HasDataBlob<WeaponInstanceStateDB>())
                weaponInstance.GetDataBlob<WeaponInstanceStateDB>().FireControl = null;
            else
                throw new Exception("needs WeaponStateDB on weaponInstance");
        }

        public static void SetTarget(Entity fireControlInstance, Entity target)
        {
            if (fireControlInstance.HasDataBlob<FireControlInstanceStateDB>())
                fireControlInstance.GetDataBlob<FireControlInstanceStateDB>().Target = target;
            else
                throw new Exception("No FireContInstanceAbilityDB on entity");
        }

    }




}
