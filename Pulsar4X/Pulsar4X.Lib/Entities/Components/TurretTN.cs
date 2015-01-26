using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    public class TurretDefTN : ComponentDefTN
    {
        /// <summary>
        /// The beam that forms the basis of this turret. each barrel will be one of these.
        /// </summary>
        private BeamDefTN BaseBeamWeapon;
        public BeamDefTN baseBeamWeapon
        {
            get { return BaseBeamWeapon; }
        }
        /// <summary>
        /// Multiplier influences shot count, size, and cost of this turret. essentially it is this many beams.
        /// </summary>
        private int Multiplier;
        public int multiplier
        {
            get { return Multiplier; }
        }
        /// <summary>
        /// Tracking dictates the speed this turret can track targets at, and also the size of the required tracking gears.
        /// </summary>
        private int Tracking;
        public int tracking
        {
            get { return Tracking; }
        }

        /// <summary>
        /// Armour is how difficult this turret is to kill. This is expressed in HTK and is not counted as part of the overall ship belt.
        /// </summary>
        private int Armour;
        public int armour
        {
            get { return Armour; }
        }

        /// <summary>
        /// Overall shot count of all barrels.
        /// </summary>
        private int TotalShotCount;
        public int totalShotCount
        {
            get { return TotalShotCount; }
        }

        /// <summary>
        /// Overall power drain of the turret.
        /// </summary>
        private int PowerRequirement;
        public int powerRequirement
        {
            get { return PowerRequirement; }
        }

        /// <summary>
        /// Gear size as percentage of total beam size
        /// </summary>
        private float GearPercent;
        public float gearPercent
        {
            get { return GearPercent; }
        }

        /// <summary>
        /// size of only the armour.
        /// </summary>
        private float ArmourSize;
        public float armourSize
        {
            get { return ArmourSize; }
        }

        /// <summary>
        /// Cost of only the armour.
        /// </summary>
        private decimal ArmourCost;
        public decimal armourCost
        {
            get { return ArmourCost; }
        }

        /// <summary>
        /// Constructor for turret definitions
        /// </summary>
        /// <param name="Title">Name of the turret</param>
        /// <param name="BaseWeapon">The beam weapon that forms the barrels of this turret</param>
        /// <param name="Mult">How many barrels there will be.</param>
        /// <param name="Track">The tracking speed of this turret</param>
        /// <param name="BaseTrackingTech">The faction tracking tech.</param>
        /// <param name="Armor">Desired armour coverage of this turret.</param>
        /// <param name="ArmourTech">Armour Tech for this turret.</param>
        public TurretDefTN(String Title, BeamDefTN BaseWeapon, int Mult, int Track, int BaseTrackingTech, int Armor, int ArmourTech)
        {
            Name = Title;
            Id = Guid.NewGuid();

            componentType = ComponentTypeTN.Turret;

            BaseBeamWeapon = BaseWeapon;

            if (Mult < 1)
                Mult = 1;
            if (Mult > 4)
                Mult = 4;

            Multiplier = Mult;
            Tracking = Track;
            Armour = Armor;

            float GearCount = (float)Tracking / (float)Constants.BFCTN.BeamFireControlTracking[BaseTrackingTech];
            float GearSize = BaseBeamWeapon.size;

            size = BaseBeamWeapon.size * Multiplier;
            htk = (byte)(BaseBeamWeapon.htk * Multiplier);
            crew = (byte)(BaseBeamWeapon.crew * Multiplier);
            cost = (decimal)Math.Round((float)BaseBeamWeapon.cost * Multiplier * 1.5f * GearCount);

            TotalShotCount = BaseBeamWeapon.shotCount * Multiplier;
            PowerRequirement = BaseBeamWeapon.powerRequirement * Multiplier;

            GearSize = GearSize * Constants.BeamWeaponTN.TurretGearFactor[(Multiplier - 1)];
            GearPercent = GearCount * Constants.BeamWeaponTN.TurretGearFactor[(Multiplier - 1)];

            crew = (byte)Math.Round(crew * (Constants.BeamWeaponTN.TurretGearFactor[(Multiplier - 1)] * 10.0f));
            cost = (decimal)Math.Round((float)cost * (Constants.BeamWeaponTN.TurretGearFactor[(Multiplier - 1)] * 10.0f));

            size = size + (GearSize * GearCount);

            minerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                minerialsCost[mineralIterator] = 0;
            }

            switch (BaseBeamWeapon.componentType)
            {
                case ComponentTypeTN.AdvLaser:
                case ComponentTypeTN.Laser:
                case ComponentTypeTN.Meson:
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = BaseBeamWeapon.minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] * Multiplier;
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = BaseBeamWeapon.minerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] * Multiplier;
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = BaseBeamWeapon.minerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] * Multiplier;
                    break;
                case ComponentTypeTN.Gauss:
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Vendarite] = BaseBeamWeapon.minerialsCost[(int)Constants.Minerals.MinerialNames.Vendarite] * Multiplier;
                    break;
            }

            minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] + (1.5m * (decimal)GearCount);

            if (Armour != 0)
            {
                double volume = size;
                double pi = 3.14159654;
                double temp1 = 1.0 / 3.0;

                double radius3 = (3.0 * volume) / (4.0 * pi);
                double radius = Math.Pow(radius3, temp1);

                double radius2 = Math.Pow(radius, 2.0);
                double area = 4.0 * pi * radius2;

                /// <summary>
                /// This is about 89% as big as armour in aurora proper. do turretArmour = turretArmour * 1.12f; to "fix" this
                /// </summary>
                float turretArmour = (float)((area / (double)Constants.MagazineTN.MagArmor[ArmourTech]) * Armour);
                size = size + turretArmour;
                htk = (byte)(htk + (Armour * Multiplier));
                cost = cost + (decimal)(area * Armour);

                ArmourCost = (decimal)area;
                ArmourSize = turretArmour;

                minerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = ArmourCost;
            }
            else
            {
                ArmourCost = 0.0m;
                ArmourSize = 0.0f;
            }

            cost = Math.Round(cost);

            isMilitary = true;
            isObsolete = false;
            isDivisible = false;
            isSalvaged = false;
            isElectronic = false;
        }
    }

    public class TurretTN : ComponentTN
    {
        /// <summary>
        /// Definition for this turret component.
        /// </summary>
        private TurretDefTN TurretDef;
        public TurretDefTN turretDef
        {
            get { return TurretDef; }
        }

        /// <summary>
        /// Which fire control component is this Turret linked to?
        /// </summary>
        private BeamFireControlTN FireController;
        public BeamFireControlTN fireController
        {
            get { return FireController; }
            set { FireController = value; }
        }

        /// <summary>
        /// What is the state of this beam weapon's capacitor?
        /// </summary>
        private float CurrentCapacitor;
        public float currentCapacitor
        {
            get { return CurrentCapacitor; }
            set { CurrentCapacitor = value; }
        }

        /// <summary>
        /// Has this Turret fired in defense, and if so how many of its shots were used.
        /// </summary>
        private int ShotsExpended;
        public int shotsExpended
        {
            get { return ShotsExpended; }
            set { ShotsExpended = value; }
        }

        /// <summary>
        /// Constructor for the turret component itself.
        /// </summary>
        /// <param name="definition">definition for this turret.</param>
        public TurretTN(TurretDefTN definition)
        {
            TurretDef = definition;

            FireController = null;

            CurrentCapacitor = TurretDef.powerRequirement;

            isDestroyed = false;
        }

        /// <summary>
        /// ReadyToFire determines if the beam weapon has the ability to fire. Gauss may always fire, everything else needs to have capacitor charge equal to their power requirement.
        /// </summary>
        /// <returns>Whether the weapon can fire or not(true or false)</returns>
        public bool readyToFire()
        {
            if (TurretDef.baseBeamWeapon.componentType == ComponentTypeTN.Gauss && isDestroyed == false)
            {
                return true;
            }
            else if (CurrentCapacitor == TurretDef.powerRequirement && isDestroyed == false)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// When this beamweapon is fired the capacitor is discharged completely.
        /// </summary>
        public bool Fire()
        {
            bool ready = readyToFire();

            if (ready == true)
            {
                CurrentCapacitor = 0;
            }

            return ready;
        }

        /// <summary>
        /// timeToFire calculates how many seconds until this turret is charged.
        /// </summary>
        /// <returns>number of seconds(not ticks) until this gun is recharged. the smallest unit of time is the 5 second increment for recharging, so round up.</returns>
        public int timeToFire()
        {
            int SecondsToCharge = (int)Math.Ceiling((((float)TurretDef.powerRequirement - CurrentCapacitor) / (TurretDef.baseBeamWeapon.weaponCapacitor * TurretDef.multiplier)));
            SecondsToCharge = SecondsToCharge * (int)Constants.TimeInSeconds.FiveSeconds;
            return SecondsToCharge;

        }
    }
}
