using Newtonsoft.Json;
using System;
using Pulsar4X.Engine;
using Pulsar4X.Components;
using Pulsar4X.Interfaces;
using Pulsar4X.DataStructures;
using System.Diagnostics.CodeAnalysis;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Movement;

namespace Pulsar4X.Weapons
{
    public class GenericBeamWeaponAtb : IComponentDesignAttribute, IFireWeaponInstr
    {
        [JsonProperty]
        public double MaxRange { get; internal set; }

        [JsonProperty]
        public double WaveLength { get; internal set; } = 700;

        [JsonProperty]
        public int Energy { get; internal set; }

        public double LenPerPulseInSeconds = 1;

        public double BeamSpeed { get; internal set; } = UniversalConstants.Units.SpeedOfLightInMetresPerSecond; //299792458 is speed of light.
        public float BaseHitChance { get; internal set; } = 0.95f;

        public GenericBeamWeaponAtb() { }

        public GenericBeamWeaponAtb(double maxRange, double waveLen, double jules)
        {
            MaxRange = maxRange;
            WaveLength = waveLen;
            Energy = (int)jules;
        }

        public GenericBeamWeaponAtb(GenericBeamWeaponAtb db)
        {
            MaxRange = db.MaxRange;
            WaveLength = db.WaveLength;
            Energy = db.Energy;
        }

        /*
        public override object Clone()
        {
            return new GenericBeamWeaponAtb(this);
        }*/

        public string WeaponType => "Beam";

        public void SetWeaponState(WeaponState state)
        {
            state.WeaponType = WeaponType;
            state.WeaponStats = new (string name, double value, ValueTypeStruct valueType)[3];
            state.WeaponStats[0] = ("Max Range:", MaxRange, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Distance, ValueTypeStruct.ValueSizes.BaseUnit));
            state.WeaponStats[1] = ("Wavelength:", WaveLength, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Distance, ValueTypeStruct.ValueSizes.BaseUnit));
            state.WeaponStats[2] = ("Power:", Energy, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Power, ValueTypeStruct.ValueSizes.BaseUnit));
        }

        public bool CanLoadOrdnance(OrdnanceDesign ordnanceDesign)
        {
            return false;
        }

        public bool AssignOrdnance(OrdnanceDesign ordnanceDesign)
        {
            return false;
        }

        public bool TryGetOrdnance([NotNullWhen(true)] out OrdnanceDesign? ordnanceDesign)
        {
            ordnanceDesign = null;
            return false;
        }

        public void FireWeapon(Entity launchingEntity, Entity tgtEntity, int count)
        {

            var beamLen = Math.Min(1, count * LenPerPulseInSeconds); //our beam can't be longer than the time period.

            BeamWeaponProcessor.FireBeamWeapon(launchingEntity, tgtEntity, true, Energy, WaveLength ,BeamSpeed, beamLen);
        }

        public float ToHitChance(Entity launchingEntity, Entity tgtEntity)
        {
            var launchPos = MoveMath.GetAbsolutePosition(launchingEntity);
            var tgtPos = MoveMath.GetAbsolutePosition(tgtEntity);
            double range = Math.Abs((launchPos - tgtPos).Length());

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
                SetWeaponState(wpnState);
                componentInstance.SetAbilityState<WeaponState>(wpnState);
            }
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {

        }

        public string AtbName()
        {
            return "Generic Beam Weapon";
        }

        public string AtbDescription()
        {
            return "";
        }

    }
}
