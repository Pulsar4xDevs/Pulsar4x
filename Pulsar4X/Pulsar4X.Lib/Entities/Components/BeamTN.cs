using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using log4net;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public class BeamDefTN : ComponentDefTN
    {
        /// <summary>
        /// All beam weapons will have either a Calibre size technology, or a rate of fire technology associated with them. not both though so here that is.
        /// </summary>
        private byte WeaponSizeTech;
        public byte weaponSizeTech
        {
            get { return WeaponSizeTech; }
            set { WeaponSizeTech = value; }
        }

        /// <summary>
        /// Every beam weapon except the venerable plasma carronade will have a range modifying tech associated with it.
        /// </summary>
        private byte WeaponRangeTech;
        public byte weaponRangeTech
        {
            get { return WeaponRangeTech; }
            set { WeaponRangeTech = value; }
        }

        /// <summary>
        /// Every beam weapon excepting gauss cannons will have a capacitor. this is the tech value not strength value.
        /// </summary>
        private byte WeaponCapacitorTech;
        public byte weaponcapacitorTech
        {
            get { return WeaponCapacitorTech; }
            set { WeaponCapacitorTech = value; }
        }

        /// <summary>
        /// Every beam weapon excepting gauss cannons will have a capacitor. this is the strength value, not the tech value
        /// </summary>
        private byte WeaponCapacitor;
        public byte weaponCapacitor
        {
            get { return WeaponCapacitor; }
            set { WeaponCapacitor = value; }
        }

        /// <summary>
        /// Damage over range. every beam weapon except particle beams and gauss will suffer reduced damage at higher ranges. This table stores what damage is done at what range.
        /// </summary>
        private BindingList<ushort> Damage;
        public BindingList<ushort> damage
        {
            get { return Damage; }
            set { Damage = value; }
        }

        /// <summary>
        /// How much power is required to fire this weapon.
        /// </summary>
        private ushort PowerRequirement;
        public ushort powerRequirement
        {
            get { return PowerRequirement; }
            set { PowerRequirement = value; }
        }

        /// <summary>
        /// Overall max range of the beam weapon. The lesser of this or BFC range determines the maximum range a weapon can fire at.
        /// </summary>
        private float Range;
        public float range
        {
            get { return Range; }
            set { Range = value; }
        }

        /// <summary>
        /// How often this weapon shoots in seconds. 5s minimum, modified by power requirement and capacitor technology.
        /// </summary>
        private ushort ROF;
        public ushort rof
        {
            get { return ROF; }
            set { ROF = value; }
        }

        /// <summary>
        /// How many shots does this weapon inflict on a target?
        /// </summary>
        private byte ShotCount;
        public byte shotCount
        {
            get { return ShotCount; }
            set { ShotCount = value; }
        }

        /// <summary>
        /// Base firing accuracy for this weapons, will be 1.0 in most cases, though gauss makes heavy use of this.
        /// </summary>
        private float BaseAccuracy;
        public float baseAccuracy
        {
            get { return BaseAccuracy; }
            set { BaseAccuracy = value; }
        }

        /// <summary>
        /// Type of damage this beam weapon does.
        /// </summary>
        private DamageTypeTN DamageType;
        public DamageTypeTN damageType
        {
            get { return DamageType; }
        }


        /// <summary>
        /// Constructor for all beam weapon types.
        /// </summary>
        /// <param name="Title">Name of the beam weapon, user entered string, or default string.</param>
        /// <param name="Type">What type of beam weapon this is, note that componentTypeTN encompasses much more than beam weapon types, don't give those values to this function.</param>
        /// <param name="SizeTech">Every beam weapon either has a calibre size, except for particle beams, which use warhead strength, and gauss which give a rate of fire. All use this variable.</param>
        /// <param name="RangeTech">Every beam weapon has a range tech except for plasma carronades which do not have any way to increase range or decrease damage falloff.</param>
        /// <param name="CapacitorTech">Every beam weapon has a capacitor tech associated with it except gauss. Shotcount tech for gauss.</param>
        /// <param name="Reduction">Lasers and Gauss both have size reduction capabilities, though with drawbacks for both types: recharge rate and accuracy respectively.</param>
        public BeamDefTN(String Title, ComponentTypeTN Type, byte SizeTech, byte RangeTech, byte CapacitorTech, float Reduction)
        {
            if (Type < ComponentTypeTN.Rail || Type > ComponentTypeTN.AdvParticle)
            {
                /// <summary>
                /// Error, bad ID passed to BeamDefTN.
                /// </summary>
                return;
            }
            Id = Guid.NewGuid();
            componentType = Type;

            Name = Title;
            WeaponSizeTech = SizeTech;
            WeaponRangeTech = RangeTech;
            WeaponCapacitorTech = CapacitorTech;
            WeaponCapacitor = Constants.BeamWeaponTN.Capacitor[CapacitorTech];

            ShotCount = 1;
            BaseAccuracy = 1.0f;

            Damage = new BindingList<ushort>();

            int RangeIncrement;

            switch (componentType)
            {
                /// <summary>
                /// Laser is the the most basic jack of all trade beam weapon.
                /// </summary>
                case ComponentTypeTN.Laser :

                    /// <summary>
                    /// I Suspect that size is 3.2cm per HS but am just using a table for now.
                    /// </summary>
                    size = (float)Constants.BeamWeaponTN.LaserSize[WeaponSizeTech] * Reduction;

                    /// <summary>
                    /// Lasers have the longest range of all beam weapons due to their high damage, normal 10,000km factor and weapon range tech.
                    /// </summary>
                    Range = (float)Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech] * 10000.0f * (float)(WeaponRangeTech+1);

                    /// <summary>
                    /// The first entry in the damage table is max damage at point blank(0-10k range) damage.
                    /// </summary>
                    Damage.Add((ushort)Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech]);

                    /// <summary>
                    /// Lasers require 1 unit of power for every unit of damage that they do.
                    /// </summary>
                    PowerRequirement = Damage[0];

                    /// <summary>
                    /// Subsequent entries up to Wavelength * 10k do full damage, after that the value is:
                    /// FullDamage * ( Wavelength / RangeIncrement Tick) with a minimum of 1 over range.
                    /// </summary>
                    /// 
                    RangeIncrement = (WeaponRangeTech+1) * Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech];
                    CalcDamageTable(RangeIncrement);

                    DamageType = DamageTypeTN.Beam;
                break;

                case ComponentTypeTN.AdvLaser :
                    size = (float)Constants.BeamWeaponTN.LaserSize[WeaponSizeTech] * Reduction;
                    Range = (float)Constants.BeamWeaponTN.AdvancedLaserDamage[WeaponSizeTech] * 10000.0f * (float)(WeaponRangeTech+1);
                    Damage.Add((ushort)Constants.BeamWeaponTN.AdvancedLaserDamage[WeaponSizeTech]);

                    /// <summary>
                    /// Advanced lasers do more damage per unit of power than regular lasers.
                    /// </summary>
                    PowerRequirement = (ushort)Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech];

                    RangeIncrement = (WeaponRangeTech+1) * Constants.BeamWeaponTN.AdvancedLaserDamage[WeaponSizeTech];
                    CalcDamageTable(RangeIncrement);

                    DamageType = DamageTypeTN.Beam;
                break;

                /// <summary>
                /// Plasmas are essentially cheaper infared lasers.
                /// </summary>
                case ComponentTypeTN.Plasma :
                    WeaponRangeTech = 0;

                    /// <summary>
                    /// I Suspect that size is 3.2cm per HS but am just using a table for now. No reductions for plasma.
                    /// </summary>
                    size = (float)Constants.BeamWeaponTN.LaserSize[WeaponSizeTech];

                    /// <summary>
                    /// Plasma carronades have the same range as an infared laser of equal size.
                    /// </summary>
                    Range = (float)Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech] * 10000.0f * (float)(WeaponRangeTech+1);

                    /// <summary>
                    /// The first entry in the damage table is max damage at point blank(0-10k range) damage.
                    /// </summary>
                    Damage.Add((ushort)Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech]);

                    /// <summary>
                    /// Plasmas require 1 unit of power for every unit of damage that they do.
                    /// </summary>
                    PowerRequirement = Damage[0];

                    /// <summary>
                    /// Subsequent entries up to Wavelength * 10k do full damage, after that the value is:
                    /// FullDamage * ( Wavelength / RangeIncrement Tick) with a minimum of 1 over range.
                    /// </summary>
                    /// 
                    RangeIncrement = (WeaponRangeTech+1) * Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech];
                    CalcDamageTable(RangeIncrement);

                    DamageType = DamageTypeTN.Plasma;
                break;

                case ComponentTypeTN.AdvPlasma :
                    WeaponRangeTech = 0;
                    size = (float)Constants.BeamWeaponTN.LaserSize[WeaponSizeTech];
                    Range = (float)Constants.BeamWeaponTN.AdvancedLaserDamage[WeaponSizeTech] * 10000.0f * (float)(WeaponRangeTech+1);
                    Damage.Add((ushort)Constants.BeamWeaponTN.AdvancedLaserDamage[WeaponSizeTech]);
                    PowerRequirement = (ushort)Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech];
                    RangeIncrement = (WeaponRangeTech+1) * Constants.BeamWeaponTN.AdvancedLaserDamage[WeaponSizeTech];
                    CalcDamageTable(RangeIncrement);

                    DamageType = DamageTypeTN.Plasma;
                break;
                
                /// <summary>
                /// Railguns have a higher damage than lasers of equal size, though it is spread out over many hits.
                /// Likewise railguns are not suitable for turrets, lastly railguns don't have the full tech progression that lasers have.
                /// Railguns and especially advanced railguns are also power efficient as far as the damage that they do.
                /// </summary>
                case ComponentTypeTN.Rail:
                    ShotCount = 4;

                    size = (float)Constants.BeamWeaponTN.RailGunSize[WeaponSizeTech];
                    Range = (float)Constants.BeamWeaponTN.RailGunDamage[WeaponSizeTech] * 10000.0f * (WeaponRangeTech+1);
                    Damage.Add((ushort)Constants.BeamWeaponTN.RailGunDamage[WeaponSizeTech]);
                    PowerRequirement = (ushort)(Constants.BeamWeaponTN.RailGunDamage[WeaponSizeTech] * 3);
                    RangeIncrement = (WeaponRangeTech+1) * Constants.BeamWeaponTN.RailGunDamage[WeaponSizeTech];
                    CalcDamageTable(RangeIncrement);

                    DamageType = DamageTypeTN.Kinetic;
                break;

                /// <summary>
                /// Only difference here is 1 more shot.
                /// </summary>
                case ComponentTypeTN.AdvRail:
                    ShotCount = 5;

                    size = (float)Constants.BeamWeaponTN.RailGunSize[WeaponSizeTech];
                    Range = (float)Constants.BeamWeaponTN.RailGunDamage[WeaponSizeTech] * 10000.0f * (WeaponRangeTech + 1);
                    Damage.Add((ushort)Constants.BeamWeaponTN.RailGunDamage[WeaponSizeTech]);
                    PowerRequirement = (ushort)(Constants.BeamWeaponTN.RailGunDamage[WeaponSizeTech] * 3);
                    RangeIncrement = (WeaponRangeTech + 1) * Constants.BeamWeaponTN.RailGunDamage[WeaponSizeTech];
                    CalcDamageTable(RangeIncrement);

                    DamageType = DamageTypeTN.Kinetic;
                break;

                /// <summary>
                /// Mesons have half the range that lasers have, and only do 1 damage, but always pass through armor and shields.
                /// </summary>
                case ComponentTypeTN.Meson:

                    size = (float)Constants.BeamWeaponTN.LaserSize[WeaponSizeTech];
                    Range = (float)Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech] * 5000.0f * (float)(WeaponRangeTech + 1);

                    Damage.Add(1);
                    PowerRequirement = (ushort)Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech];
                    RangeIncrement = (((WeaponRangeTech + 1) * Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech]) / 2);
                    for (int loop = 1; loop < RangeIncrement; loop++)
                    {
                        Damage.Add(1);
                    }
                    Damage.Add(0);

                    DamageType = DamageTypeTN.Meson;
                break;

                /// <summary>
                /// Microwaves do electronic only damage, though they do triple against shields. this isn't very useful though as they don't do triple normal laser damage vs shields, just 3.
                /// They share 1/2 range with mesons, but can't be turreted.
                /// </summary>
                case ComponentTypeTN.Microwave:

                    size = (float)Constants.BeamWeaponTN.LaserSize[WeaponSizeTech];
                    Range = (float)Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech] * 5000.0f * (float)(WeaponRangeTech + 1);

                    Damage.Add(1);
                    PowerRequirement = (ushort)Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech];

                    RangeIncrement = (((WeaponRangeTech + 1) * Constants.BeamWeaponTN.LaserDamage[WeaponSizeTech]) / 2);
                    for (int loop = 1; loop < RangeIncrement; loop++)
                    {
                        Damage.Add(1);
                    }
                    Damage.Add(0);

                    DamageType = DamageTypeTN.Microwave;
                break;

                /// <summary>
                /// Particle Beams suffer no range dissipation so will out damage lasers at their maximum range, but are shorter ranged than lasers.
                /// WeaponSizeTech for particle beams is their warhead strength, not any focal lense size as with lasers.
                /// </summary>
                case ComponentTypeTN.Particle:

                    size = (float)Constants.BeamWeaponTN.ParticleSize[WeaponSizeTech];
                    Range = (float)Constants.BeamWeaponTN.ParticleRange[WeaponRangeTech] * 10000.0f;
                    PowerRequirement = (ushort)Constants.BeamWeaponTN.ParticlePower[WeaponSizeTech];

                    Damage.Add(Constants.BeamWeaponTN.ParticleDamage[WeaponSizeTech]);
                    RangeIncrement = Constants.BeamWeaponTN.ParticleRange[WeaponRangeTech] / 10;
                    for (int loop = 1; loop < RangeIncrement; loop++)
                    {
                        Damage.Add(Constants.BeamWeaponTN.ParticleDamage[WeaponSizeTech]);
                    }
                    Damage.Add(0);

                    DamageType = DamageTypeTN.Kinetic;
                break;

                /// <summary>
                /// More damage is the only change for advanced particle beams.
                /// </summary>
                case ComponentTypeTN.AdvParticle:

                    size = (float)Constants.BeamWeaponTN.ParticleSize[WeaponSizeTech];
                    Range = (float)Constants.BeamWeaponTN.ParticleRange[WeaponRangeTech] * 10000.0f;
                    PowerRequirement = (ushort)Constants.BeamWeaponTN.ParticlePower[WeaponSizeTech];

                    Damage.Add(Constants.BeamWeaponTN.AdvancedParticleDamage[WeaponSizeTech]);
                    RangeIncrement = Constants.BeamWeaponTN.ParticleRange[WeaponRangeTech] / 10;
                    for (int loop = 1; loop < RangeIncrement; loop++)
                    {
                        Damage.Add(Constants.BeamWeaponTN.AdvancedParticleDamage[WeaponSizeTech]);
                    }
                    Damage.Add(0);

                    DamageType = DamageTypeTN.Kinetic;
                break;

                /// <summary>
                /// Gauss Cannons differ substantially from other beam weapons. Size is determined directly from a size vs accuracy choice.
                /// Likewise capacitor refers to shotcount rather than any capacitor technology.
                /// </summary>
                case ComponentTypeTN.Gauss:
                    size = Constants.BeamWeaponTN.GaussSize[WeaponSizeTech];
                    Range = (WeaponRangeTech + 1) * 10000.0f;
                    ShotCount = Constants.BeamWeaponTN.GaussShots[CapacitorTech];
                    BaseAccuracy = Constants.BeamWeaponTN.GaussAccuracy[WeaponSizeTech];
                    PowerRequirement = 0;
                    Damage.Add(1);
                    RangeIncrement = (WeaponRangeTech + 1);
                    for (int loop = 1; loop < RangeIncrement; loop++)
                    {
                        Damage.Add(1);
                    }
                    Damage.Add(0);

                    DamageType = DamageTypeTN.Kinetic;
                break;
            }

            /// <summary>
            /// Gauss cannons just have to be different.
            /// </summary>
            if (componentType != ComponentTypeTN.Gauss)
            {
                htk = (byte)(size / 2.0f);
                crew = (byte)(size * 5.0f);

                cost = htk * (WeaponRangeTech + 1) * (WeaponCapacitor+1);
            }
            else
            {
                if (size == 6.0 || size == 5.0 || size == 4.0)
                    htk = 2;
                else if (size >= 1.0)
                    htk = 1;
                else
                    htk = 0;
            
                crew = (byte)(size * 2.0f);
                cost = (byte)(size * 2.0f * (float)(WeaponRangeTech + 1) * (float)(WeaponCapacitor + 1));
            }

            isMilitary = true;
            isObsolete = false;
            isDivisible = false;
            isSalvaged = false;
            isElectronic = false;
        }

        /// <summary>
        /// Damage for certain beam weapons falls off over distance, this formula determines how much damage is done per 10k increment.
        /// </summary>
        /// <param name="RangeIncrement">number of 10k range ticks to calculate damage for.</param>
        public void CalcDamageTable(int RangeIncrement)
        {
            for (int loop = 1; loop < RangeIncrement; loop++)
            {
                ushort IncrementDamage = 0;
                if (loop < (WeaponRangeTech+1))
                {
                    IncrementDamage = Damage[0];
                }
                else
                {
                    IncrementDamage = (ushort)Math.Floor((float)((float)(WeaponRangeTech+1) / (float)(loop + 1)));

                    if (IncrementDamage == 0)
                        IncrementDamage = 1;
                }
                Damage.Add(IncrementDamage);
            }
            Damage.Add(0);
        }


    }

    /// <summary>
    /// BeamTN will likely be the weapon link storage.
    /// </summary>
    public class BeamTN : ComponentTN
    {
        /// <summary>
        /// Definition for the beam weapon.
        /// </summary>
        private BeamDefTN BeamDef;
        public BeamDefTN beamDef
        {
            get { return BeamDef; }
        }

        /// <summary>
        /// Which fire control component is this beamweapon linked to?
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
        private ushort CurrentCapacitor;
        public ushort currentCapacitor
        {
            get { return CurrentCapacitor; }
            set { CurrentCapacitor = value; }
        }   

        /// <summary>
        /// Constructor for beam weapons. FireController is null for the time being as no BFC is assigned by default, and CurrentCapacitor is set to filled and ready to fire.
        /// </summary>
        /// <param name="definition">Beam definition to use for this weapon.</param>
        public BeamTN(BeamDefTN definition)
        {
            BeamDef = definition;

            FireController = null;

            CurrentCapacitor = BeamDef.powerRequirement;

            isDestroyed = false;
        }

        /// <summary>
        /// ReadyToFire determines if the beam weapon has the ability to fire. Gauss may always fire, everything else needs to have capacitor charge equal to their power requirement.
        /// </summary>
        /// <returns>Whether the weapon can fire or not(true or false)</returns>
        public bool readyToFire()
        {
            if (BeamDef.componentType == ComponentTypeTN.Gauss && isDestroyed == false)
            {
                return true;
            }
            else if (CurrentCapacitor == BeamDef.powerRequirement && isDestroyed == false)
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
    }
}
