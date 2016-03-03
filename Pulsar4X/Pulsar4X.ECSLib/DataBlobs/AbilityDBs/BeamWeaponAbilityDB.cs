using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public enum BeamWeaponType
    {
        Invalid = 0,
        Gauss,
        HighPoweredMicrowave,
        Laser,
        Meson,
        ParticleBeam,
        PlasmaCarronade,
        Railgun,
    }

    public class BeamWeaponStateInfo
    {
        public float CurrentCharge { get; internal set; }
    }

    public class BeamWeaponAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int MaxRange { get; internal set; }
        [JsonProperty]
        public int DamageAtMaxRange { get; internal set; }

        [JsonProperty]
        public float AccuracyMultiplier { get; internal set; }
        
        [JsonProperty]
        public int PowerRequired { get; internal set; }

        /// <summary>
        /// Power recharged per second
        /// </summary>
        [JsonProperty]
        public int PowerRechargeRate { get; internal set; }

        [JsonProperty]
        public int ShotsPerVolley { get; internal set; }

        [JsonProperty]
        public BeamWeaponType WeaponType { get; internal set; }
        
        public override object Clone()
        {
            return new BeamWeaponAbilityDB
            {
                MaxRange = MaxRange,
                DamageAtMaxRange = DamageAtMaxRange,
                AccuracyMultiplier = AccuracyMultiplier,
                PowerRequired = PowerRequired,
                PowerRechargeRate = PowerRechargeRate,
                WeaponType = WeaponType,
                OwningEntity = OwningEntity,
            };
        }
    }
}