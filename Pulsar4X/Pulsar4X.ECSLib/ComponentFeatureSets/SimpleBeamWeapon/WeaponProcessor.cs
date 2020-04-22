using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;

namespace Pulsar4X.ECSLib
{
    
    public class WeaponProcessor : IInstanceProcessor
    {
        internal override void ProcessEntity(Entity entity, DateTime atDate)
        {
            var instances = entity.GetDataBlob<ComponentInstancesDB>();
            var fireControl = entity.GetDataBlob<FireControlAbilityDB>();
            
            
            
            if(instances.TryGetComponentsWithStates<WeaponState>(out var wpnList))
            {
                foreach (ComponentInstance wpn in wpnList)
                {
                    var wpnState = wpn.GetAbilityState<WeaponState>();
                    if (wpn.IsEnabled && wpnState.CoolDown <= atDate)
                    {
                        var fc = wpnState.Master;
                        if (wpnState.Master != null)
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
            FireControlAbilityState fireControl = stateInfo.Master.GetAbilityState<FireControlAbilityState>();
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
    
    

    public class HotWpnProcessor : IHotloopProcessor
    {
        public void Init(Game game)
        {
            
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            throw new NotImplementedException();
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var blobs = manager.GetAllDataBlobsOfType<GenericFiringWeaponsDB>();
            foreach (GenericFiringWeaponsDB blob in blobs)
            {
                ProcessReloadWeapon(blob);
            }
            foreach (GenericFiringWeaponsDB blob in blobs)
            {
                ProcessWeaponFire(blob);
            }
        }

        public static void ProcessReloadWeapon(GenericFiringWeaponsDB firingWeapons)
        {
            for (int i = 0; i < firingWeapons.WpnIDs.Length; i++)
            {
                if(firingWeapons.WpnIDs[i] == Guid.Empty)
                    continue;//just incase a weapon gets removed from the array and leaves an empty spot. 
                if (firingWeapons.InternalMagQty[i] < firingWeapons.InternalMagSizes[i])
                {
                    firingWeapons.InternalMagQty[i] += Math.Min(firingWeapons.ReloadAmountsPerSec[i], firingWeapons.InternalMagSizes[i]);
                }

                if (firingWeapons.InternalMagQty[i] >= firingWeapons.AmountPerShot[i] * firingWeapons.MinShotsPerfire[i])
                {
                    int numshots = firingWeapons.InternalMagQty[i] / firingWeapons.AmountPerShot[i];
                    int depleteinternalMag = numshots * firingWeapons.AmountPerShot[i];
                    firingWeapons.InternalMagQty[i] -= depleteinternalMag;
                    firingWeapons.ShotsFiredThisTick[i] = numshots;
                }
            }
        }

        public static void ProcessWeaponFire(GenericFiringWeaponsDB firingWeapons)
        {
            for (int i = 0; i < firingWeapons.WpnIDs.Length; i++)
            {
                int shotsFired = firingWeapons.ShotsFiredThisTick[i];
                firingWeapons.ShotsFiredThisTick[i] = 0;
                var lunchEnt = firingWeapons.OwningEntity;
                var tgtEnt = firingWeapons.FireControlStates[i].Target;
                var force = firingWeapons.LaunchForces[i];
                for (int j = 0; j < shotsFired; j++)
                {
                    if(firingWeapons.OrdnanceDesigns[i]!= null)
                        firingWeapons.OrdnanceDesigns[i].CreateOrdnance(lunchEnt, tgtEnt, force);
                }
                
                

            }
        }

        public TimeSpan RunFrequency { get; } = TimeSpan.FromSeconds(1);
        public TimeSpan FirstRunOffset { get; } = TimeSpan.Zero;
        public Type GetParameterType { get; } = typeof(GenericFiringWeaponsDB);
    }

    public class GenericFiringWeaponsDB : BaseDataBlob
    {

        
        public Guid[] WpnIDs = new Guid[0];
        public int[] InternalMagSizes = new int[0];
        public int[] InternalMagQty = new int[0];
        public int[] ReloadAmountsPerSec = new int[0];
        public int[] AmountPerShot = new int[0];
        public int[] MinShotsPerfire = new int[0];

        public int[] ShotsFiredThisTick = new int[0];

        //public GenericWeaponAtb.WpnTypes[] WpnTypes = new GenericWeaponAtb.WpnTypes[0];
        public OrdnanceDesign[] OrdnanceDesigns = new OrdnanceDesign[0];
        public double[] LaunchForces = new double[0];
        public FireControlAbilityState[] FireControlStates = new FireControlAbilityState[0];
        
        /// <summary>
        /// Adds to exsisting weapons.
        /// not thread safe.
        /// </summary>
        /// <param name="wpns"></param>
        internal void AddWeapons(List<ComponentInstance> wpns)
        {
            int count = WpnIDs.Length + wpns.Count;
            

            Guid[] wpnIDs = new Guid[count];
            int[] internalMagSizes = new int[count];
            int[] internalMagQty = new int[count];
            int[] reloadAmountsPerSec = new int[count];
            int[] amountPerShot = new int[count];
            int[] minShotsPerfire = new int[count];            
            //GenericWeaponAtb.WpnTypes[] wpnTypes = new GenericWeaponAtb.WpnTypes[count];
            OrdnanceDesign[] ordDes = new OrdnanceDesign[count];
            double[] launchForce =  new double[count];
            FireControlAbilityState[] fcStates = new FireControlAbilityState[count];
            ShotsFiredThisTick = new int[count];
            
            if(WpnIDs.Length > 0)
            {
                Array.Copy(WpnIDs, wpnIDs, count); //we can't blockcopy a non primitive. 
                Buffer.BlockCopy(InternalMagSizes, 0, internalMagSizes, 0, count);
                Buffer.BlockCopy(InternalMagQty, 0, internalMagQty, 0, count);
                Buffer.BlockCopy(ReloadAmountsPerSec, 0, reloadAmountsPerSec, 0, count);
                Buffer.BlockCopy(AmountPerShot, 0, amountPerShot, 0, count);
                Buffer.BlockCopy(MinShotsPerfire, 0, minShotsPerfire, 0, count);
            }
            int offset = WpnIDs.Length;
            for (int i = 0; i < count; i++)
            {
                GenericWeaponAtb wpnAtb = wpns[i].Design.GetAttribute<GenericWeaponAtb>();
                var wpnState = wpns[i].GetAbilityState<WeaponState>();
                
                wpnIDs[i + offset] = wpns[i].ID;
                internalMagSizes[i + offset] = wpnAtb.InternalMagSize;
                internalMagQty[i + offset] = wpnState.InernalMagCurAmount; 
                reloadAmountsPerSec[i + offset] = wpnAtb.ReloadAmountPerSec;
                amountPerShot[i + offset] = wpnAtb.AmountPerShot;
                minShotsPerfire[i + offset] = wpnAtb.MinShotsPerfire;

                //wpnTypes[i + offset] = wpnAtb.WpnType;
                fcStates[i + offset] = wpnState.Master.GetAbilityState<FireControlAbilityState>();
                ordDes[i + offset] = wpnState.AssignedOrdnanceDesign;
                if (wpns[i].Design.HasAttribute<MissileLauncherAtb>())
                    launchForce[i] = wpns[i].Design.GetAttribute<MissileLauncherAtb>().LaunchForce;
                else
                {
                    launchForce[i] = 1;
                }
                ShotsFiredThisTick[i] = 0;
            }

            WpnIDs = wpnIDs;
            InternalMagSizes = internalMagSizes;
            InternalMagQty = internalMagQty;
            ReloadAmountsPerSec = reloadAmountsPerSec;
            AmountPerShot = amountPerShot;
            MinShotsPerfire = minShotsPerfire;
            //WpnTypes = wpnTypes;
            FireControlStates = fcStates;
            OrdnanceDesigns = ordDes;
            LaunchForces = launchForce;
        }

        internal void RemoveWeapons(List<ComponentInstance> wpns)
        {
            Guid[] wpnToRemoveIDs = new Guid[wpns.Count];
            bool[] keepOrRemove = new bool[WpnIDs.Length];
            //List<int> removeIndexs;
            for (int i = 0; i < wpns.Count; i++)
            {
                wpnToRemoveIDs[i] = wpns[i].ID;
            }

            for (int i = 0; i < WpnIDs.Length; i++)
            {
                keepOrRemove[i] = wpnToRemoveIDs.Contains(WpnIDs[i]);
            }

            int count = 0;
            for (int i = 0; i < keepOrRemove.Length; i++)
            {
                if (keepOrRemove[i]) ;
                count++;
            }
            
            Guid[] wpnIDs = new Guid[count];
            int[] internalMagSizes = new int[count];
            int[] internalMagQty = new int[count];
            int[] reloadAmountsPerSec = new int[count];
            int[] amountPerShot = new int[count];
            int[] minShotsPerfire = new int[count];            
            //GenericWeaponAtb.WpnTypes[] wpnTypes = new GenericWeaponAtb.WpnTypes[count];
            OrdnanceDesign[] ordDes = new OrdnanceDesign[count];
            double[] launchForce =  new double[count];
            FireControlAbilityState[] fcStates = new FireControlAbilityState[count];

            if (count > 0)
            {
                int i = 0;
                for (int j = 0; j < WpnIDs.Length; j++)
                {
                    if (keepOrRemove[j])
                    {
                        wpnIDs[i] = WpnIDs[j];
                        internalMagSizes[i] = InternalMagSizes[j];
                        internalMagQty[i] = InternalMagQty[j];
                        reloadAmountsPerSec[i] = ReloadAmountsPerSec[j];
                        amountPerShot[i] = AmountPerShot[j];
                        minShotsPerfire[i] = MinShotsPerfire[j];
                        ordDes[i] = OrdnanceDesigns[j];
                        launchForce[i] = LaunchForces[j];
                        fcStates[i] = FireControlStates[j];
                    }
                }
            }
            
            WpnIDs = wpnIDs;
            InternalMagSizes = internalMagSizes;
            InternalMagQty = internalMagQty;
            ReloadAmountsPerSec = reloadAmountsPerSec;
            AmountPerShot = amountPerShot;
            MinShotsPerfire = minShotsPerfire;
            //WpnTypes = wpnTypes;
            FireControlStates = fcStates;
            OrdnanceDesigns = ordDes;
            LaunchForces = launchForce;

        }

        /// <summary>
        /// Sets weapons, this will remove exsisting. 
        /// </summary>
        /// <param name="wpns"></param>
        internal void SetWeapons(List<ComponentInstance> wpns)
        {
            int count = wpns.Count;
            
            Guid[] wpnIDs = new Guid[count];
            int[] internalMagSizes = new int[count];
            int[] internalMagQty = new int[count];
            int[] reloadAmountsPerSec = new int[count];
            int[] amountPerShot = new int[count];
            int[] minShotsPerfire = new int[count];  
            //GenericWeaponAtb.WpnTypes[] wpnTypes = new GenericWeaponAtb.WpnTypes[count];
            FireControlAbilityState[] fcStates = new FireControlAbilityState[count];
            OrdnanceDesign[] ordDes = new OrdnanceDesign[count];
            double[] launchForce = new double[count];
            ShotsFiredThisTick = new int[count];
            for (int i = 0; i < count; i++)
            {
                GenericWeaponAtb wpnAtb = wpns[i].Design.GetAttribute<GenericWeaponAtb>();
                var wpnState = wpns[i].GetAbilityState<WeaponState>();
                
                wpnIDs[i] = wpns[i].ID;
                internalMagSizes[i] = wpnAtb.InternalMagSize;
                internalMagQty[i] = wpnState.InernalMagCurAmount; 
                reloadAmountsPerSec[i] = wpnAtb.ReloadAmountPerSec;
                amountPerShot[i] = wpnAtb.AmountPerShot;
                minShotsPerfire[i] = wpnAtb.MinShotsPerfire;
                //wpnTypes[i] = wpnAtb.WpnType;
                fcStates[i] = wpnState.Master.GetAbilityState<FireControlAbilityState>();
                ordDes[i] = wpnState.AssignedOrdnanceDesign;
                if (wpns[i].Design.HasAttribute<MissileLauncherAtb>())
                    launchForce[i] = wpns[i].Design.GetAttribute<MissileLauncherAtb>().LaunchForce;
                else
                {
                    launchForce[i] = 1;
                }

                ShotsFiredThisTick[i] = 0;
            }

            WpnIDs = wpnIDs;
            InternalMagSizes = internalMagSizes;
            InternalMagQty = internalMagQty;
            ReloadAmountsPerSec = reloadAmountsPerSec;
            AmountPerShot = amountPerShot;
            MinShotsPerfire = minShotsPerfire;
            //WpnTypes = wpnTypes;
            FireControlStates = fcStates;
            OrdnanceDesigns = ordDes;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
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
                wpnState.Master = fireControlInstance;
            else
                throw new Exception("needs FireContInstanceAbilityDB on fireControlInstance, and WeaponStateDB on weaponInstance");
        }

        public static void RemoveWeaponFromFC(ComponentInstance weaponInstance)
        {
            if (weaponInstance.TryGetAbilityState<WeaponState>(out var wpnState))
                wpnState.Master = null;
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
