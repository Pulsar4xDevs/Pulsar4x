using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;

namespace Pulsar4X.ECSLib
{
    public class WeaponProcessor : IInstanceProcessor
    {
        internal override void ProcessEntity(Entity entity, DateTime atDate)
        {
            var instances = entity.GetDataBlob<ComponentInstancesDB>();
            var fireControl = entity.GetDataBlob<FireControlAbilityDB>();
            
            
            
            if(instances.TryGetComponentsByAttribute<SimpleBeamWeaponAtbDB>(out var wpnList))
            {
                foreach (var wpn in wpnList)
                {
                    var wpnState = wpn.GetAbilityState<WeaponState>();
                    if (wpn.IsEnabled && wpnState.CoolDown <= atDate)
                    {
                        var fc = wpnState.FireControl;
                        if (wpnState.FireControl != null)
                        {
                            var fcstate = fc.GetAbilityState<FireControlAbilityState>();
                            if (fcstate.IsEngaging)
                            {
                                wpnState.ReadyToFire = true;
                                FireBeamWeapons(wpn, atDate);
                            }
                        }
                    }
                }
            }
        }

   

        public static void FireBeamWeapons(ComponentInstance beamWeapon, DateTime atDate)
        {
            //TODO: all this needs to get re-written. 
            WeaponState stateInfo = beamWeapon.GetAbilityState<WeaponState>();
            FireControlAbilityState fireControl = stateInfo.FireControl.GetAbilityState<FireControlAbilityState>();
            if(!fireControl.Target.IsValid)
            {
                fireControl.Target = null;
                fireControl.IsEngaging = false;
                return;
            }

            //var myPos = beamWeapon.GetDataBlob<ComponentInstanceData>().ParentEntity.GetDataBlob<PositionDB>();
            var targetPos = fireControl.Target.GetDataBlob<PositionDB>();


            //TODO chance to hit
            //int damageAmount = 10;//TODO damageAmount calc
            var designAtb = beamWeapon.Design.GetAttribute<SimpleBeamWeaponAtbDB>();
            int damageAmount = designAtb.DamageAmount; // TODO: Better damage calculation

            double range = 1000;// myPos.GetDistanceTo_AU(targetPos);

            // only fire if target is in range TODO: fire anyway, but miss. TODO: this will be wrong if we do movement last, this needs to be done after movement. 
            if (range <= designAtb.MaxRange)//TODO: firecontrol shoudl have max range too?: Math.Min(designAtb.MaxRange, stateInfo.FireControl.GetDataBlob<BeamFireControlAtbDB>().Range))
            {
                /*
                DamageFragment damage = new DamageFragment()
                {
                    Density = 
                };
                
                DamageTools.DealDamage(fireControl.Target, new DamageFragment())
                //DamageProcessor.OnTakingDamage(, damageAmount, atDate);
                */
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
                //var design = beamWeapon.GetDataBlob<ComponentInstanceData>().Design;


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
        public static void SetWeaponToFC(ComponentInstance fireControlInstance, ComponentInstance weaponInstance)
        {
            if (fireControlInstance.HasAblity<FireControlAbilityState>() && weaponInstance.TryGetAbilityState<WeaponState>(out var wpnState))
                wpnState.FireControl = fireControlInstance;
            else
                throw new Exception("needs FireContInstanceAbilityDB on fireControlInstance, and WeaponStateDB on weaponInstance");
        }

        public static void RemoveWeaponFromFC(ComponentInstance weaponInstance)
        {
            if (weaponInstance.TryGetAbilityState<WeaponState>(out var wpnState))
                wpnState.FireControl = null;
            else
                throw new Exception("needs WeaponStateDB on weaponInstance");
        }

        public static void SetTarget(ComponentInstance fireControlInstance, Entity target)
        {
            if (fireControlInstance.TryGetAbilityState<FireControlAbilityState>(out var fcState))
                fcState.Target = target;
            else
                throw new Exception("No FireContInstanceAbilityDB on entity");
        }
    }
}
