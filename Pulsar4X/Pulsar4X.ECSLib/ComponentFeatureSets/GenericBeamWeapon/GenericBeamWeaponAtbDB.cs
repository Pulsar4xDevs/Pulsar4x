using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.ComponentFeatureSets.GenericBeamWeapon;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;

namespace Pulsar4X.ECSLib
{
    public class GenericBeamWeaponAtbDB : BaseDataBlob, IComponentDesignAttribute, IFireWeaponInstr
    {
        [JsonProperty]
        public double MaxRange { get; internal set; }
        [JsonProperty]
        public int DamageAmount { get; internal set; }
        [JsonProperty]
        public int ReloadRate { get; internal set; }

        [JsonProperty] public int Frequncy { get; internal set; } = 700;
        
        public double LenPerPulseInSeconds = 1;
        
        public double BeamSpeed { get; internal set; } = 299792458; //299792458 is speed of light.
        public float BaseHitChance { get; internal set; } = 0.95f;
        
        public GenericBeamWeaponAtbDB() { }

        public GenericBeamWeaponAtbDB(double maxRange, double damageAmount, double reloadRate)
        {
            MaxRange = maxRange;
            DamageAmount = (int)damageAmount;
            ReloadRate = (int)reloadRate;
        }

        public GenericBeamWeaponAtbDB(GenericBeamWeaponAtbDB db)
        {
            MaxRange = db.MaxRange;
            DamageAmount = db.DamageAmount;
            ReloadRate = db.ReloadRate;
        }

        public override object Clone()
        {
            return new GenericBeamWeaponAtbDB(this);
        }

        public bool CanLoadOrdnance(OrdnanceDesign ordnanceDesign)
        {
            return false;
        }

        public bool AssignOrdnance(OrdnanceDesign ordnanceDesign)
        {
            return false;
        }

        public bool TryGetOrdnance(out OrdnanceDesign ordnanceDesign)
        {
            ordnanceDesign = null;
            return false;
        }

        public void FireWeapon(Entity launchingEntity, Entity tgtEntity, int count)
        {
            
            var beamLen = Math.Min(1, count * LenPerPulseInSeconds); //our beam can't be longer than the time period.
            var tohit = ToHitChance(launchingEntity, tgtEntity);
            var hitsTarget = (launchingEntity.Manager as StarSystem).RNGNexBool(tohit);
            
            
            //TODO: DELETE! (for testing purposes turning this on so always hitting)
            hitsTarget = true;
            
            BeamWeapnProcessor.FireBeamWeapon(launchingEntity, tgtEntity, hitsTarget, Frequncy,BeamSpeed, beamLen);
        }

        public float ToHitChance(Entity launchingEntity, Entity tgtEntity)
        {
            double range = Math.Abs((launchingEntity.GetAbsolutePosition() - tgtEntity.GetAbsolutePosition()).Length());
            
            //var ttt = BeamWeapnProcessor.TimeToTarget(range, launchingEntity.)) 
            //tempory timetotarget
            double ttt = range / BeamSpeed; //this should be the closing speed (ie the velocity of the two, the beam speed and the range)
            double missChance = ttt * ( 1 - BaseHitChance);
            return (float)(1 - missChance);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            var instancesDB = parentEntity.GetDataBlob<ComponentInstancesDB>();
            if (!parentEntity.HasDataBlob<FireControlAbilityDB>())
            {
                var fcdb = new FireControlAbilityDB();
                parentEntity.SetDataBlob(fcdb);
            }
           
            if (!componentInstance.HasAblity<WeaponState>())
            {
                var wpnState = new WeaponState(componentInstance, this);
                wpnState.WeaponType = "Beam";
                wpnState.WeaponStats = new (string name, double value, ValueTypeStruct valueType)[3];
                wpnState.WeaponStats[0] = ("Max Range:", MaxRange, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Distance, ValueTypeStruct.ValueSizes.BaseUnit));
                wpnState.WeaponStats[1] = ("Damage:", DamageAmount, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Power, ValueTypeStruct.ValueSizes.BaseUnit));
                wpnState.WeaponStats[2] = ("Rate Of Fire:", ReloadRate, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Number, ValueTypeStruct.ValueSizes.BaseUnit));
                componentInstance.SetAbilityState<WeaponState>(wpnState);
            }
            

        }

    }
}
