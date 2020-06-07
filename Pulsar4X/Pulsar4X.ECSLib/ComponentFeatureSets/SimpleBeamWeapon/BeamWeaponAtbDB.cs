using Newtonsoft.Json;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;

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

    public class BeamWeaponAtbDB : BaseDataBlob, IComponentDesignAttribute, IFireWeaponInstr
    {
        /// <summary>
        /// Max range of this weapon. Measured in KM.
        /// </summary>
        [JsonProperty]
        public int MaxRange { get; internal set; }

        /// <summary>
        /// Damage of this weapon at point blank range - drops off over longer distances
        /// </summary>
        [JsonProperty]
        public int BaseDamage { get; internal set; }

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

        public BeamWeaponAtbDB(int maxRange, int baseDamage, float accuracyMultiplier, float powerRequired, float powerRechargeRate, int shotsPerVolley, BeamWeaponType weaponType)
        {
            MaxRange = maxRange;
            BaseDamage = baseDamage;
            AccuracyMultiplier = accuracyMultiplier;
            PowerRequired = powerRequired;
            PowerRechargeRate = powerRechargeRate;
            ShotsPerVolley = shotsPerVolley;
            WeaponType = weaponType;
        }

        public override object Clone()
        {
            return new BeamWeaponAtbDB(MaxRange, BaseDamage, AccuracyMultiplier, PowerRequired, PowerRechargeRate, ShotsPerVolley, WeaponType);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!componentInstance.HasAblity<WeaponState>())
                componentInstance.SetAbilityState<WeaponState>(new WeaponState(componentInstance, this));
        }

        public bool CanLoadOrdnance(OrdnanceDesign ordnanceDesign)
        {
            throw new System.NotImplementedException();
        }

        public bool AssignOrdnance(OrdnanceDesign ordnanceDesign)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetOrdnance(out OrdnanceDesign ordnanceDesign)
        {
            throw new System.NotImplementedException();
        }

        public void FireWeapon(Entity launchingEntity, Entity tgtEntity)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GenericWeaponAtb : IComponentDesignAttribute
    {
        /*
        public enum WpnTypes
        {
            Missile,
            Beam,
            Railgun
        }
*/
        //public WpnTypes WpnType;
        public int InternalMagSize;
        public int ReloadAmountPerSec;
        public int AmountPerShot;
        public int MinShotsPerfire;
        
        private GenericWeaponAtb()
        {
        }

        /// <summary>
        /// constructor for json, all values get cast to ints (not rounded)
        /// </summary>
        /// <param name="magSize"></param>
        /// <param name="reloadPerSec"></param>
        /// <param name="amountPerShot"></param>
        /// <param name="minShotsPerfire"></param>
        public GenericWeaponAtb(double magSize, double reloadPerSec, double amountPerShot, double minShotsPerfire)
        {
            InternalMagSize = (int)magSize;
            ReloadAmountPerSec = (int)reloadPerSec;
            AmountPerShot = (int)amountPerShot;
            MinShotsPerfire = (int)minShotsPerfire;
            //WpnType = type;
        }
        
        public GenericWeaponAtb(int magSize, int reloadPerSec, int amountPerShot, int minShotsPerfire)
        {
            InternalMagSize = magSize;
            ReloadAmountPerSec = reloadPerSec;
            AmountPerShot = amountPerShot;
            MinShotsPerfire = minShotsPerfire;
            //WpnType = type;
        }

        
        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            
        }
    }
}