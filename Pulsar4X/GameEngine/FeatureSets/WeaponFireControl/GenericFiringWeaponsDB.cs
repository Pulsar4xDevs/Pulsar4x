using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine.Damage;
using Pulsar4X.Components;
using Pulsar4X.Atb;

namespace Pulsar4X.Datablobs
{
    public class GenericFiringWeaponsDB : BaseDataBlob
    {

        
        public string[] WpnIDs = new string[0];
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
                    if (wpn.UniqueID == wpnID)
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
            
            
            string[] wpnIDs = new string[count];
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
                
                wpnIDs[i + offset] = wpns[i].UniqueID;
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
        
        internal void RemoveWeapons(string wpnId)
        {
            ComponentInstance[] wpnInstances = new ComponentInstance[1];
            wpnInstances[0]= OwningEntity.GetDataBlob<ComponentInstancesDB>().AllComponents[wpnId];
            RemoveWeapons(wpnInstances);
        }
        
        internal void RemoveWeapons(string[] wpnIds)
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
            List<(string id, int index)> wpnsToKeep = new List<(string, int)>();
            //List<int> removeIndexs;
            
            
            
            for (int i = 0; i < WpnIDs.Length; i++)
            {
                bool keep = true;
                foreach (var wpn in wpns)
                {
                    if (WpnIDs[i] == wpn.UniqueID)
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
            
            string[] wpnIDs = new string[count];
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
            
            string[] wpnIDs = new string[count];
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
                
                wpnIDs[i] = wpns[i].UniqueID;
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