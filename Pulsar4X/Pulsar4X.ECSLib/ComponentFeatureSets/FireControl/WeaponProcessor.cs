using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
                        var fc = (FireControlAbilityState)wpnState.ParentState;
                        if (fc != null)
                        {
                            if (fc.IsEngaging)
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
            FireControlAbilityState fireControl = (FireControlAbilityState)stateInfo.ParentState;
            if(!fireControl.Target.IsValid)
            {
                fireControl.SetTarget(null);
                fireControl.IsEngaging = false;
                return;
            }
            
            //var myPos = beamWeapon.GetDataBlob<ComponentInstanceData>().ParentEntity.GetDataBlob<PositionDB>();
            var targetPos = fireControl.Target.GetDataBlob<PositionDB>();


            //TODO chance to hit
            //int damageAmount = 10;//TODO damageAmount calc
            var designAtb = beamWeapon.Design.GetAttribute<GenericBeamWeaponAtbDB>();
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
    
    
/// <summary>
/// Currently this has some problems, it needs to be able to remove itself from an entity after the weapons are no longer firing and all weapons have been reloaded,
/// or can't be reloaded due to lack of ordnance. currently it doesn't do this. 
/// </summary>
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
            
            //when firing weapons we need to have the parent in the right place.
            //orbits don't update every subtick, so we update just this entity for this tick, if it's an orbiting entity.
            foreach (var blob in blobs) 
            {
                var entity = blob.OwningEntity; //
                if (entity.HasDataBlob<OrbitDB>())
                {
                    var fastBlob = new OrbitUpdateOftenDB(entity.GetDataBlob<OrbitDB>());
                    entity.SetDataBlob(fastBlob);
                    entity.RemoveDataBlob<OrbitDB>();
                    
                }
                List<Entity> targets = new List<Entity>();
                for (int i = 0; i < blob.FireControlStates.Length; i++)
                {
                    var tgt = blob.FireControlStates[i].Target;
                    if(!targets.Contains(tgt))
                        targets.Add(tgt);
                }
                foreach (var tgt in targets)
                {
                    if (tgt.HasDataBlob<OrbitDB>())
                    {
                        var fastBlob = new OrbitUpdateOftenDB(tgt.GetDataBlob<OrbitDB>());
                        tgt.SetDataBlob(fastBlob);
                        tgt.RemoveDataBlob<OrbitDB>();
                        
                    }
                }
            }

            foreach (GenericFiringWeaponsDB blob in blobs)
            {
                ProcessReloadWeapon(blob);
            }
            foreach (GenericFiringWeaponsDB blob in blobs)
            {
                ProcessWeaponFire(blob);
            }

            foreach (var blob in blobs)
            {
                UpdateReloadState(blob);
            }
        }

        public static void ProcessReloadWeapon(GenericFiringWeaponsDB reloadingWeapons)
        {
            for (int i = 0; i < reloadingWeapons.WpnIDs.Length; i++)
            {
                if(reloadingWeapons.WpnIDs[i] == Guid.Empty)
                    continue;//just incase a weapon gets removed from the array and leaves an empty spot. 
                if (reloadingWeapons.InternalMagQty[i] < reloadingWeapons.InternalMagSizes[i])
                {
                    reloadingWeapons.InternalMagQty[i] += Math.Min(reloadingWeapons.ReloadAmountsPerSec[i], reloadingWeapons.InternalMagSizes[i]);
                }
                //if it's reloaded enough to fire at least one shot.
                if (reloadingWeapons.InternalMagQty[i] >= reloadingWeapons.AmountPerShot[i] * reloadingWeapons.MinShotsPerfire[i])
                {

                    
                    //if this is not attached to a fire control, 
                    if (reloadingWeapons.FireControlStates[i] == null)
                    {   //and is fully reloaded.
                        if(reloadingWeapons.InternalMagQty[i] >= reloadingWeapons.InternalMagSizes[i]) 
                            reloadingWeapons.RemoveWeapons(reloadingWeapons.WpnIDs[i]);//remove it from being processed every second.
                    }
                    //if it *is* attached to a firecontrol, and is firing. 
                    else if(reloadingWeapons.FireControlStates[i].IsEngaging)
                    { //then fire 
                        int numshots = reloadingWeapons.InternalMagQty[i] / reloadingWeapons.AmountPerShot[i];
                        reloadingWeapons.ShotsFiredThisTick[i] = numshots;
                        int depleteinternalMag = numshots * reloadingWeapons.AmountPerShot[i];
                        reloadingWeapons.InternalMagQty[i] -= depleteinternalMag;
                    }
                    // if it's attached to firecontrol, but not firing and is fully reloaded
                    else if(reloadingWeapons.InternalMagQty[i] >= reloadingWeapons.InternalMagSizes[i]) 
                    {   //remove it from being processed every second.
                        reloadingWeapons.RemoveWeapons(reloadingWeapons.WpnIDs[i]);
                    }
                }
            }
        }
        
        public static void ProcessWeaponFire(GenericFiringWeaponsDB firingWeapons)
        {
            for (int i = 0; i < firingWeapons.WpnIDs.Length; i++)
            {
                int shotsFired = firingWeapons.ShotsFiredThisTick[i];
                firingWeapons.ShotsFiredThisTick[i] = 0;
                if(shotsFired > 0)
                {
                    var lunchEnt = firingWeapons.OwningEntity;
                    var tgtEnt = firingWeapons.FireControlStates[i].Target;
                    var force = firingWeapons.LaunchForces[i];

                    firingWeapons.FireInstructions[i].FireWeapon(lunchEnt, tgtEnt, shotsFired);
                }
            }
        }

        /// <summary>
        /// updates the non hot process data (which is what the ui reads) for reload state.
        /// this feels a bit janky, would it be better to just include the weapon states in the hot datablob? (GenericFiringWeaponsDB)
        /// </summary>
        /// <param name="reloadingWeapons"></param>
        public static void UpdateReloadState(GenericFiringWeaponsDB reloadingWeapons)
        {
            var entity = reloadingWeapons.OwningEntity;

            if (entity.GetDataBlob<ComponentInstancesDB>().TryGetStates<WeaponState>(out var wpnStates))
            {
                for (int i = 0; i < reloadingWeapons.WpnIDs.Length; i++)
                {
                    foreach (WeaponState wpnState in wpnStates)
                    {
                        if(wpnState.ID == reloadingWeapons.WpnIDs[i])
                            wpnState.InternalMagCurAmount = reloadingWeapons.InternalMagQty[i];
                    }
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
        public IFireWeaponInstr[] FireInstructions = new IFireWeaponInstr[0];
        public double[] LaunchForces = new double[0];
        public FireControlAbilityState[] FireControlStates = new FireControlAbilityState[0];


        internal GenericFiringWeaponsDB(ComponentInstance[] wpns)
        {
            SetWeapons(wpns);
        }

        /// <summary>
        /// Adds to exsisting weapons.
        /// not thread safe.
        /// </summary>
        /// <param name="wpns"></param>
        internal void AddWeapons(ComponentInstance[] wpns)
        {



            //first check that the weapons to add don't already exsist in the blob.
            List<ComponentInstance> weaponsToAdd = new List<ComponentInstance>();
            foreach (var wpn in wpns)
            {
                bool add = true;
                foreach (var wpnID in WpnIDs)
                {
                    if (wpn.ID == wpnID)
                        add = false;
                }
                if(add)
                    weaponsToAdd.Add(wpn);
            }
            if (weaponsToAdd.Count == 0)
                return;
            
            int count = WpnIDs.Length + weaponsToAdd.Count;
            int currentCount = WpnIDs.Length;
            int addCount = weaponsToAdd.Count;
            
            
            Guid[] wpnIDs = new Guid[count];
            int[] internalMagSizes = new int[count];
            int[] internalMagQty = new int[count];
            int[] reloadAmountsPerSec = new int[count];
            int[] amountPerShot = new int[count];
            int[] minShotsPerfire = new int[count];  
            int[] shotsfireThisTick = new int[count];
            IFireWeaponInstr[] fireInstr = new IFireWeaponInstr[count];
            double[] launchForce =  new double[count];
            
            FireControlAbilityState[] fcStates = new FireControlAbilityState[count];
            
            
            if(WpnIDs.Length > 0)
            {
                Array.Copy(WpnIDs, wpnIDs, currentCount); //we can't blockcopy a non primitive. 
                Array.Copy(FireControlStates, fcStates, currentCount);
                Array.Copy(FireInstructions, fireInstr, currentCount);
                Buffer.BlockCopy(InternalMagSizes, 0, internalMagSizes, 0, currentCount);
                Buffer.BlockCopy(InternalMagQty, 0, internalMagQty, 0, currentCount);
                Buffer.BlockCopy(ReloadAmountsPerSec, 0, reloadAmountsPerSec, 0, currentCount);
                Buffer.BlockCopy(AmountPerShot, 0, amountPerShot, 0, currentCount);
                Buffer.BlockCopy(MinShotsPerfire, 0, minShotsPerfire, 0, currentCount);
                Buffer.BlockCopy(ShotsFiredThisTick, 0, shotsfireThisTick, 0, currentCount );
                Buffer.BlockCopy(LaunchForces, 0, launchForce, 0, currentCount);
            }
            int offset = currentCount;
            for (int i = 0; i < addCount; i++)
            {
                GenericWeaponAtb wpnAtb = wpns[i].Design.GetAttribute<GenericWeaponAtb>();
                var wpnState = wpns[i].GetAbilityState<WeaponState>();
                
                wpnIDs[i + offset] = wpns[i].ID;
                internalMagSizes[i + offset] = wpnAtb.InternalMagSize;
                internalMagQty[i + offset] = wpnState.InternalMagCurAmount; 
                reloadAmountsPerSec[i + offset] = wpnAtb.ReloadAmountPerSec;
                amountPerShot[i + offset] = wpnAtb.AmountPerShot;
                minShotsPerfire[i + offset] = wpnAtb.MinShotsPerfire;
                fcStates[i + offset] = (FireControlAbilityState)wpnState.ParentState;
                fireInstr[i + offset] = wpnState.FireWeaponInstructions;
                shotsfireThisTick[i] = 0;
                if (wpns[i].Design.HasAttribute<MissileLauncherAtb>())
                    launchForce[i] = wpns[i].Design.GetAttribute<MissileLauncherAtb>().LaunchForce;
                else
                {
                    launchForce[i] = 1;
                }
                
            }

            WpnIDs = wpnIDs;
            InternalMagSizes = internalMagSizes;
            InternalMagQty = internalMagQty;
            ReloadAmountsPerSec = reloadAmountsPerSec;
            AmountPerShot = amountPerShot;
            MinShotsPerfire = minShotsPerfire;
            FireControlStates = fcStates;
            FireInstructions = fireInstr;
            ShotsFiredThisTick = shotsfireThisTick;
            LaunchForces = launchForce;
        }
        
        internal void RemoveWeapons(Guid wpnId)
        {
            ComponentInstance[] wpnInstances = new ComponentInstance[1];
            wpnInstances[0]= OwningEntity.GetDataBlob<ComponentInstancesDB>().AllComponents[wpnId];
            RemoveWeapons(wpnInstances);
        }
        
        internal void RemoveWeapons(Guid[] wpnIds)
        {
            ComponentInstance[] wpnInstances = new ComponentInstance[wpnIds.Length];
            for (int i = 0; i < wpnIds.Length; i++)
            {
                wpnInstances[i]= OwningEntity.GetDataBlob<ComponentInstancesDB>().AllComponents[wpnIds[i]];
            }
            RemoveWeapons(wpnInstances);
        }
        
        
        /// <summary>
        /// removes weapons from the index. 
        /// </summary>
        /// <param name="wpns"></param>
        internal void RemoveWeapons(ComponentInstance[] wpns)
        {
            //Guid[] wpnToRemoveIDs = new Guid[wpns.Length];
            //bool[] keepOrRemove = new bool[WpnIDs.Length];
            List<(Guid id, int index)> wpnsToKeep = new List<(Guid, int)>();
            //List<int> removeIndexs;
            
            
            
            for (int i = 0; i < WpnIDs.Length; i++)
            {
                bool keep = true;
                int j = 0;
                foreach (var wpn in wpns)
                {
                    if (WpnIDs[i] == wpn.ID)
                    {
                        keep = false;
                        var wpnState = wpn.GetAbilityState<WeaponState>();
                        wpnState.InternalMagCurAmount = InternalMagQty[i];
                        break;
                    }
                }
                if(keep)
                    wpnsToKeep.Add((WpnIDs[i], i));
            }
            
            int count = wpnsToKeep.Count;
            
            Guid[] wpnIDs = new Guid[count];
            int[] internalMagSizes = new int[count];
            int[] internalMagQty = new int[count];
            int[] reloadAmountsPerSec = new int[count];
            int[] amountPerShot = new int[count];
            int[] minShotsPerfire = new int[count];            
            //GenericWeaponAtb.WpnTypes[] wpnTypes = new GenericWeaponAtb.WpnTypes[count];
            IFireWeaponInstr[] fireInstr = new IFireWeaponInstr[count];
            double[] launchForce =  new double[count];
            FireControlAbilityState[] fcStates = new FireControlAbilityState[count];

            int newIndex = 0;
            foreach (var wpn in wpnsToKeep)
            {
                int oldIndex = wpn.index;
                wpnIDs[newIndex] = WpnIDs[oldIndex];
                internalMagSizes[newIndex] = InternalMagSizes[oldIndex];
                internalMagQty[newIndex] = InternalMagQty[oldIndex];
                reloadAmountsPerSec[newIndex] = ReloadAmountsPerSec[oldIndex];
                amountPerShot[newIndex] = AmountPerShot[oldIndex];
                minShotsPerfire[newIndex] = MinShotsPerfire[oldIndex];
                fireInstr[newIndex] = FireInstructions[oldIndex];
                launchForce[newIndex] = LaunchForces[oldIndex];
                fcStates[newIndex] = FireControlStates[oldIndex];
                
                newIndex++;
            }
            
            WpnIDs = wpnIDs;
            InternalMagSizes = internalMagSizes;
            InternalMagQty = internalMagQty;
            ReloadAmountsPerSec = reloadAmountsPerSec;
            AmountPerShot = amountPerShot;
            MinShotsPerfire = minShotsPerfire;
            //WpnTypes = wpnTypes;
            FireControlStates = fcStates;
            FireInstructions = fireInstr;
            LaunchForces = launchForce;
        }

        /// <summary>
        /// Sets weapons, this will remove exsisting. 
        /// </summary>
        /// <param name="wpns"></param>
        internal void SetWeapons(ComponentInstance[] wpns)
        {
            int count = wpns.Length;
            
            Guid[] wpnIDs = new Guid[count];
            int[] internalMagSizes = new int[count];
            int[] internalMagQty = new int[count];
            int[] reloadAmountsPerSec = new int[count];
            int[] amountPerShot = new int[count];
            int[] minShotsPerfire = new int[count];  
            //GenericWeaponAtb.WpnTypes[] wpnTypes = new GenericWeaponAtb.WpnTypes[count];
            FireControlAbilityState[] fcStates = new FireControlAbilityState[count];
            IFireWeaponInstr[] fireInstr = new IFireWeaponInstr[count];
            double[] launchForce = new double[count];
            ShotsFiredThisTick = new int[count];
            for (int i = 0; i < count; i++)
            {
                GenericWeaponAtb wpnAtb = wpns[i].Design.GetAttribute<GenericWeaponAtb>();
                var wpnState = wpns[i].GetAbilityState<WeaponState>();
                
                wpnIDs[i] = wpns[i].ID;
                internalMagSizes[i] = wpnAtb.InternalMagSize;
                internalMagQty[i] = wpnState.InternalMagCurAmount; 
                reloadAmountsPerSec[i] = wpnAtb.ReloadAmountPerSec;
                amountPerShot[i] = wpnAtb.AmountPerShot;
                minShotsPerfire[i] = wpnAtb.MinShotsPerfire;
                //wpnTypes[i] = wpnAtb.WpnType;
                fcStates[i] = (FireControlAbilityState)wpnState.ParentState;
                fireInstr[i] = wpnState.FireWeaponInstructions;
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
            FireInstructions = fireInstr;
            LaunchForces = launchForce;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
    
}
