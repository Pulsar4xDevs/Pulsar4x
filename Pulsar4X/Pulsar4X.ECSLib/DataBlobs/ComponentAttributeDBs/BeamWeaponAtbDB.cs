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

    public class BeamWeaponAtbDB : BaseDataBlob
    {
        /// <summary>
        /// Max range of this weapon. Measured in KM.
        /// </summary>
        [JsonProperty]
        public int MaxRange { get; internal set; }

        /// <summary>
        /// Damage of this weapon at MaxRange
        /// </summary>
        [JsonProperty]
        public int DamageAtMaxRange { get; internal set; }

        /// <summary>
        /// AccuracyPenalty. ChanceToHit = (Speed/Range/other penalties) * AccuracyMultiplier
        /// </summary>
        [JsonProperty]
        public float AccuracyMultiplier { get; internal set; }
        
        /// <summary>
        /// Power required for this weapon to fire.
        /// </summary>
        [JsonProperty]
        public float PowerRequired { get; internal set; }

        /// <summary>
        /// Power this weapon can charge per second.
        /// </summary>
        [JsonProperty]
        public float PowerRechargeRate { get; internal set; }

        /// <summary>
        /// Number of shots fired in a single volley.
        /// </summary>
        [JsonProperty]
        public int ShotsPerVolley { get; internal set; }

        /// <summary>
        /// Type of weapon this beam weapon is.
        /// </summary>
        [JsonProperty]
        public BeamWeaponType WeaponType { get; internal set; }
        
        [JsonConstructor]
        internal BeamWeaponAtbDB() { }

        public BeamWeaponAtbDB(double maxRange, double damageAtMaxRange, double accuracyMultiplier, double powerRequired, double powerRechargeRate, double shotsPerVolley, BeamWeaponType weaponType) 
            : this((int) maxRange, (int) damageAtMaxRange, (float) accuracyMultiplier, (float) powerRequired, (float) powerRechargeRate, (int) shotsPerVolley, weaponType) { }

        public BeamWeaponAtbDB(int maxRange, int damageAtMaxRange, float accuracyMultiplier, float powerRequired, float powerRechargeRate, int shotsPerVolley, BeamWeaponType weaponType)
        {
            MaxRange = maxRange;
            DamageAtMaxRange = damageAtMaxRange;
            AccuracyMultiplier = accuracyMultiplier;
            PowerRequired = powerRequired;
            PowerRechargeRate = powerRechargeRate;
            ShotsPerVolley = shotsPerVolley;
            WeaponType = weaponType;
        }

        public override object Clone()
        {
            return new BeamWeaponAtbDB(MaxRange, DamageAtMaxRange, AccuracyMultiplier, PowerRequired, PowerRechargeRate, ShotsPerVolley, WeaponType);
        }
    }
}