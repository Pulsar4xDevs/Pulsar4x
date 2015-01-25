using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public class BeamDefTN : ComponentDefTN
    {
        /// <summary>
        /// All beam weapons will have either a Calibre size technology, or a rate of fire technology associated with them. not both though so here that is.
        /// </summary>
        private byte m_oWeaponSizeTech;
        public byte weaponSizeTech
        {
            get { return m_oWeaponSizeTech; }
            set { m_oWeaponSizeTech = value; }
        }

        /// <summary>
        /// Every beam weapon except the venerable plasma carronade will have a range modifying tech associated with it.
        /// </summary>
        private byte m_oWeaponRangeTech;
        public byte weaponRangeTech
        {
            get { return m_oWeaponRangeTech; }
            set { m_oWeaponRangeTech = value; }
        }

        /// <summary>
        /// Every beam weapon excepting gauss cannons will have a capacitor. this is the tech value not strength value.
        /// </summary>
        private byte m_oWeaponCapacitorTech;
        public byte weaponcapacitorTech
        {
            get { return m_oWeaponCapacitorTech; }
            set { m_oWeaponCapacitorTech = value; }
        }

        /// <summary>
        /// Every beam weapon excepting gauss cannons will have a capacitor. this is the strength value, not the tech value
        /// </summary>
        private float m_oWeaponCapacitor;
        public float weaponCapacitor
        {
            get { return m_oWeaponCapacitor; }
            set { m_oWeaponCapacitor = value; }
        }

        /// <summary>
        /// Damage over range. every beam weapon except particle beams and gauss will suffer reduced damage at higher ranges. This table stores what damage is done at what range.
        /// </summary>
        private BindingList<ushort> m_lDamage;
        public BindingList<ushort> damage
        {
            get { return m_lDamage; }
            set { m_lDamage = value; }
        }

        /// <summary>
        /// How much power is required to fire this weapon.
        /// </summary>
        private ushort m_oPowerRequirement;
        public ushort powerRequirement
        {
            get { return m_oPowerRequirement; }
            set { m_oPowerRequirement = value; }
        }

        /// <summary>
        /// Overall max range of the beam weapon. The lesser of this or BFC range determines the maximum range a weapon can fire at.
        /// </summary>
        private float m_oRange;
        public float range
        {
            get { return m_oRange; }
            set { m_oRange = value; }
        }

        /// <summary>
        /// How often this weapon shoots in seconds. 5s minimum, modified by power requirement and capacitor technology.
        /// </summary>
        private ushort m_oROF;
        public ushort rof
        {
            get { return m_oROF; }
            set { m_oROF = value; }
        }

        /// <summary>
        /// How many shots does this weapon inflict on a target?
        /// </summary>
        private byte m_oShotCount;
        public byte shotCount
        {
            get { return m_oShotCount; }
            set { m_oShotCount = value; }
        }

        /// <summary>
        /// Base firing accuracy for this weapons, will be 1.0 in most cases, though gauss makes heavy use of this.
        /// </summary>
        private float m_oBaseAccuracy;
        public float baseAccuracy
        {
            get { return m_oBaseAccuracy; }
            set { m_oBaseAccuracy = value; }
        }

        /// <summary>
        /// Type of damage this beam weapon does.
        /// </summary>
        private DamageTypeTN m_oDamageType;
        public DamageTypeTN damageType
        {
            get { return m_oDamageType; }
        }

        public enum MountType
        {
            Standard,
            Spinal,
            AdvancedSpinal,
            Count
        }

        /// <summary>
        /// What type of mount is this weapon.
        /// </summary>
        private MountType m_oWMType;
        public MountType wMType
        {
            get { return m_oWMType; }
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
        public BeamDefTN(String Title, ComponentTypeTN Type, byte SizeTech, byte RangeTech, byte CapacitorTech, float Reduction, MountType MType = MountType.Standard)
        {
#warning function has beam weapon range related magic numbers and others
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
            m_oWeaponSizeTech = SizeTech;
            m_oWeaponRangeTech = RangeTech;
            m_oWeaponCapacitorTech = CapacitorTech;
            m_oWeaponCapacitor = Constants.BeamWeaponTN.Capacitor[CapacitorTech];
            m_oWMType = MType;

            m_oShotCount = 1;
            m_oBaseAccuracy = 1.0f;

            m_lDamage = new BindingList<ushort>();

            int RangeIncrement;

            switch (componentType)
            {
                /// <summary>
                /// Laser is the the most basic jack of all trade beam weapon.
                /// </summary>
                case ComponentTypeTN.Laser:

                    /// <summary>
                    /// I Suspect that size is 3.2cm per HS but am just using a table for now.
                    /// </summary>
                    size = (float)Math.Round(Constants.BeamWeaponTN.LaserSize[m_oWeaponSizeTech] * Reduction);

                    /// <summary>
                    /// The first entry in the damage table is max damage at point blank(0-10k range) damage.
                    /// </summary>
                    m_lDamage.Add((ushort)Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech]);

                    /// <summary>
                    /// Have to modify capacitor by size reduction values.
                    /// </summary>
                    if (Reduction == 0.75f)
                        m_oWeaponCapacitor = m_oWeaponCapacitor / 4.0f;
                    else if (Reduction == 0.5f)
                        m_oWeaponCapacitor = m_oWeaponCapacitor / 20.0f;

                    /// <summary>
                    /// Damage, Size, and Range are all modified by spinal mounting:
                    /// </summary>
                    switch (m_oWMType)
                    {
                        case MountType.Spinal:
                            size = (float)Math.Round(size * 1.25f);
                            m_lDamage[0] = (ushort)Math.Round((float)m_lDamage[0] * 1.5f);
                            break;
                        case MountType.AdvancedSpinal:
                            size = (float)Math.Round(size * 1.5f);
                            m_lDamage[0] = (ushort)Math.Round((float)m_lDamage[0] * 2.0f);
                            break;
                    }

                    /// <summary>
                    /// Lasers have the longest range of all beam weapons due to their high damage, normal 10,000km factor and weapon range tech.
                    /// </summary>
                    m_oRange = (float)m_lDamage[0] * 10000.0f * (float)(m_oWeaponRangeTech + 1);

                    /// <summary>
                    /// Lasers require 1 unit of power for every unit of damage that they do.
                    /// </summary>
                    m_oPowerRequirement = m_lDamage[0];

                    /// <summary>
                    /// Subsequent entries up to Wavelength * 10k do full damage, after that the value is:
                    /// FullDamage * ( Wavelength / RangeIncrement Tick) with a minimum of 1 over range.
                    /// </summary>
                    /// 
                    RangeIncrement = (m_oWeaponRangeTech + 1) * m_lDamage[0];
                    CalcDamageTable(RangeIncrement);

                    m_oDamageType = DamageTypeTN.Beam;
                    break;

                case ComponentTypeTN.AdvLaser:
                    size = (float)Math.Round(Constants.BeamWeaponTN.LaserSize[m_oWeaponSizeTech] * Reduction);
                    m_lDamage.Add((ushort)Constants.BeamWeaponTN.AdvancedLaserDamage[m_oWeaponSizeTech]);

                    /// <summary>
                    /// Advanced lasers do more damage per unit of power than regular lasers.
                    /// </summary>
                    m_oPowerRequirement = Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech];

                    /// <summary>
                    /// Have to modify capacitor by size reduction values.
                    /// </summary>
                    if (Reduction == 0.75f)
                        m_oWeaponCapacitor = m_oWeaponCapacitor / 4.0f;
                    else if (Reduction == 0.5f)
                        m_oWeaponCapacitor = m_oWeaponCapacitor / 20.0f;

                    /// <summary>
                    /// Damage, Size, and Range are all modified by spinal mounting:
                    /// </summary>
                    switch (MType)
                    {
                        case MountType.Spinal:
                            size = (float)Math.Round(size * 1.25f);
                            m_lDamage[0] = (ushort)Math.Round((float)m_lDamage[0] * 1.5f);
                            m_oPowerRequirement = (ushort)Math.Round((float)m_oPowerRequirement * 1.5f);
                            break;
                        case MountType.AdvancedSpinal:
                            size = (float)Math.Round(size * 1.5f);
                            m_lDamage[0] = (ushort)Math.Round((float)m_lDamage[0] * 2.0f);
                            m_oPowerRequirement = (ushort)Math.Round((float)m_oPowerRequirement * 2.0f);
                            break;
                    }

                    m_oRange = (float)m_lDamage[0] * 10000.0f * (float)(m_oWeaponRangeTech + 1);

                    RangeIncrement = (m_oWeaponRangeTech + 1) * m_lDamage[0];
                    CalcDamageTable(RangeIncrement);

                    m_oDamageType = DamageTypeTN.Beam;
                    break;

                /// <summary>
                /// Plasmas are essentially cheaper infared lasers.
                /// </summary>
                case ComponentTypeTN.Plasma:
                    m_oWeaponRangeTech = 0;

                    /// <summary>
                    /// I Suspect that size is 3.2cm per HS but am just using a table for now. No reductions for plasma.
                    /// </summary>
                    size = (float)Constants.BeamWeaponTN.LaserSize[m_oWeaponSizeTech];

                    /// <summary>
                    /// Plasma carronades have the same range as an infared laser of equal size.
                    /// </summary>
                    m_oRange = (float)Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech] * 10000.0f * (float)(m_oWeaponRangeTech + 1);

                    /// <summary>
                    /// The first entry in the damage table is max damage at point blank(0-10k range) damage.
                    /// </summary>
                    m_lDamage.Add((ushort)Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech]);

                    /// <summary>
                    /// Plasmas require 1 unit of power for every unit of damage that they do.
                    /// </summary>
                    m_oPowerRequirement = m_lDamage[0];

                    /// <summary>
                    /// Subsequent entries up to Wavelength * 10k do full damage, after that the value is:
                    /// FullDamage * ( Wavelength / RangeIncrement Tick) with a minimum of 1 over range.
                    /// </summary>
                    /// 
                    RangeIncrement = (m_oWeaponRangeTech + 1) * Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech];
                    CalcDamageTable(RangeIncrement);

                    m_oDamageType = DamageTypeTN.Plasma;
                    break;

                case ComponentTypeTN.AdvPlasma:
                    m_oWeaponRangeTech = 0;
                    size = (float)Constants.BeamWeaponTN.LaserSize[m_oWeaponSizeTech];
                    m_oRange = (float)Constants.BeamWeaponTN.AdvancedLaserDamage[m_oWeaponSizeTech] * 10000.0f * (float)(m_oWeaponRangeTech + 1);
                    m_lDamage.Add((ushort)Constants.BeamWeaponTN.AdvancedLaserDamage[m_oWeaponSizeTech]);
                    m_oPowerRequirement = (ushort)Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech];
                    RangeIncrement = (m_oWeaponRangeTech + 1) * Constants.BeamWeaponTN.AdvancedLaserDamage[m_oWeaponSizeTech];
                    CalcDamageTable(RangeIncrement);

                    m_oDamageType = DamageTypeTN.Plasma;
                    break;

                /// <summary>
                /// Railguns have a higher damage than lasers of equal size, though it is spread out over many hits.
                /// Likewise railguns are not suitable for turrets, lastly railguns don't have the full tech progression that lasers have.
                /// Railguns and especially advanced railguns are also power efficient as far as the damage that they do.
                /// </summary>
                case ComponentTypeTN.Rail:
                    m_oShotCount = 4;

                    size = (float)Constants.BeamWeaponTN.RailGunSize[m_oWeaponSizeTech];
                    m_oRange = (float)Constants.BeamWeaponTN.RailGunDamage[m_oWeaponSizeTech] * 10000.0f * (m_oWeaponRangeTech + 1);
                    m_lDamage.Add((ushort)Constants.BeamWeaponTN.RailGunDamage[m_oWeaponSizeTech]);
                    m_oPowerRequirement = (ushort)(Constants.BeamWeaponTN.RailGunDamage[m_oWeaponSizeTech] * 3);
                    RangeIncrement = (m_oWeaponRangeTech + 1) * Constants.BeamWeaponTN.RailGunDamage[m_oWeaponSizeTech];
                    CalcDamageTable(RangeIncrement);

                    m_oDamageType = DamageTypeTN.Kinetic;
                    break;

                /// <summary>
                /// Only difference here is 1 more shot.
                /// </summary>
                case ComponentTypeTN.AdvRail:
                    m_oShotCount = 5;

                    size = (float)Constants.BeamWeaponTN.RailGunSize[m_oWeaponSizeTech];
                    m_oRange = (float)Constants.BeamWeaponTN.RailGunDamage[m_oWeaponSizeTech] * 10000.0f * (m_oWeaponRangeTech + 1);
                    m_lDamage.Add((ushort)Constants.BeamWeaponTN.RailGunDamage[m_oWeaponSizeTech]);
                    m_oPowerRequirement = (ushort)(Constants.BeamWeaponTN.RailGunDamage[m_oWeaponSizeTech] * 3);
                    RangeIncrement = (m_oWeaponRangeTech + 1) * Constants.BeamWeaponTN.RailGunDamage[m_oWeaponSizeTech];
                    CalcDamageTable(RangeIncrement);

                    m_oDamageType = DamageTypeTN.Kinetic;
                    break;

                /// <summary>
                /// Mesons have half the range that lasers have, and only do 1 damage, but always pass through armor and shields.
                /// </summary>
                case ComponentTypeTN.Meson:

                    size = (float)Constants.BeamWeaponTN.LaserSize[m_oWeaponSizeTech];
                    m_oRange = (float)Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech] * 5000.0f * (float)(m_oWeaponRangeTech + 1);

                    m_lDamage.Add(1);
                    m_oPowerRequirement = (ushort)Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech];
                    RangeIncrement = (((m_oWeaponRangeTech + 1) * Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech]) / 2);
                    for (int loop = 1; loop < RangeIncrement; loop++)
                    {
                        m_lDamage.Add(1);
                    }
                    m_lDamage.Add(0);

                    m_oDamageType = DamageTypeTN.Meson;
                    break;

                /// <summary>
                /// Microwaves do electronic only damage, though they do triple against shields. this isn't very useful though as they don't do triple normal laser damage vs shields, just 3.
                /// They share 1/2 range with mesons, but can't be turreted.
                /// </summary>
                case ComponentTypeTN.Microwave:

                    size = (float)Constants.BeamWeaponTN.LaserSize[m_oWeaponSizeTech];
                    m_oRange = (float)Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech] * 5000.0f * (float)(m_oWeaponRangeTech + 1);

                    m_lDamage.Add(1);
                    m_oPowerRequirement = (ushort)Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech];

                    RangeIncrement = (((m_oWeaponRangeTech + 1) * Constants.BeamWeaponTN.LaserDamage[m_oWeaponSizeTech]) / 2);
                    for (int loop = 1; loop < RangeIncrement; loop++)
                    {
                        m_lDamage.Add(1);
                    }
                    m_lDamage.Add(0);

                    m_oDamageType = DamageTypeTN.Microwave;
                    break;

                /// <summary>
                /// Particle Beams suffer no range dissipation so will out damage lasers at their maximum range, but are shorter ranged than lasers.
                /// WeaponSizeTech for particle beams is their warhead strength, not any focal lense size as with lasers.
                /// </summary>
                case ComponentTypeTN.Particle:

                    size = (float)Constants.BeamWeaponTN.ParticleSize[m_oWeaponSizeTech];
                    m_oRange = (float)Constants.BeamWeaponTN.ParticleRange[m_oWeaponRangeTech] * 10000.0f;
                    m_oPowerRequirement = (ushort)Constants.BeamWeaponTN.ParticlePower[m_oWeaponSizeTech];

                    m_lDamage.Add(Constants.BeamWeaponTN.ParticleDamage[m_oWeaponSizeTech]);
                    RangeIncrement = Constants.BeamWeaponTN.ParticleRange[m_oWeaponRangeTech];
                    for (int loop = 1; loop < RangeIncrement; loop++)
                    {
                        m_lDamage.Add(Constants.BeamWeaponTN.ParticleDamage[m_oWeaponSizeTech]);
                    }
                    m_lDamage.Add(0);

                    m_oDamageType = DamageTypeTN.Kinetic;
                    break;

                /// <summary>
                /// More damage is the only change for advanced particle beams.
                /// </summary>
                case ComponentTypeTN.AdvParticle:

                    size = (float)Constants.BeamWeaponTN.ParticleSize[m_oWeaponSizeTech];
                    m_oRange = (float)Constants.BeamWeaponTN.ParticleRange[m_oWeaponRangeTech] * 10000.0f;
                    m_oPowerRequirement = (ushort)Constants.BeamWeaponTN.ParticlePower[m_oWeaponSizeTech];

                    m_lDamage.Add(Constants.BeamWeaponTN.AdvancedParticleDamage[m_oWeaponSizeTech]);
                    RangeIncrement = Constants.BeamWeaponTN.ParticleRange[m_oWeaponRangeTech];
                    for (int loop = 1; loop < RangeIncrement; loop++)
                    {
                        m_lDamage.Add(Constants.BeamWeaponTN.AdvancedParticleDamage[m_oWeaponSizeTech]);
                    }
                    m_lDamage.Add(0);

                    m_oDamageType = DamageTypeTN.Kinetic;
                    break;

                /// <summary>
                /// Gauss Cannons differ substantially from other beam weapons. Size is determined directly from a size vs accuracy choice.
                /// Likewise capacitor refers to shotcount rather than any capacitor technology.
                /// </summary>
                case ComponentTypeTN.Gauss:
                    size = Constants.BeamWeaponTN.GaussSize[m_oWeaponSizeTech];
                    m_oRange = (m_oWeaponRangeTech + 1) * 10000.0f;
                    m_oShotCount = Constants.BeamWeaponTN.GaussShots[CapacitorTech];
                    m_oBaseAccuracy = Constants.BeamWeaponTN.GaussAccuracy[m_oWeaponSizeTech];
                    m_oPowerRequirement = 0;
                    m_lDamage.Add(1);
                    RangeIncrement = (m_oWeaponRangeTech + 1);
                    for (int loop = 1; loop < RangeIncrement; loop++)
                    {
                        m_lDamage.Add(1);
                    }
                    m_lDamage.Add(0);

                    m_oDamageType = DamageTypeTN.Kinetic;
                    break;
            }

            /// <summary>
            /// Gauss cannons just have to be different.
            /// </summary>
            if (componentType != ComponentTypeTN.Gauss)
            {
                htk = (byte)(size / 2.0f);
                crew = (byte)(size * 2.0f);

                /// <summary>
                /// This isn't how aurora does cost, I couldn't quite figure that out. seems like it might be a table.
                /// well in any event cost is simply the hit to kill * weapon tech level + 1 * weapon capacitor tech level + 1.
                /// </summary>
                cost = (decimal)((int)htk * (int)(m_oWeaponRangeTech + 1) * (int)(m_oWeaponCapacitor + 1));

                m_oROF = (ushort)((ushort)Math.Ceiling((float)((float)m_oPowerRequirement / (float)m_oWeaponCapacitor)) * 5);
            }
            else
            {
                /// <summary>
                /// Gauss data here.
                /// </summary>
                if (size == 6.0 || size == 5.0 || size == 4.0)
                    htk = 2;
                else if (size >= 1.0)
                    htk = 1;
                else
                    htk = 0;

                crew = (byte)(size * 2.0f);
                cost = (decimal)((size * 2.0f) * (float)(m_oWeaponRangeTech + 1) * (float)(m_oWeaponCapacitor + 1));

                m_oROF = 5;
            }

            minerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                minerialsCost[mineralIterator] = 0;
            }
            switch (componentType)
            {
                case ComponentTypeTN.Laser:
                case ComponentTypeTN.AdvLaser:
                case ComponentTypeTN.Plasma:
                case ComponentTypeTN.AdvPlasma:
                case ComponentTypeTN.Particle:
                case ComponentTypeTN.AdvParticle:
                case ComponentTypeTN.Meson:
                case ComponentTypeTN.Microwave: //20%D 20%B 60%C
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = cost * 0.2m;
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = cost * 0.2m;
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = cost * 0.6m;
                    break;

                case ComponentTypeTN.Rail:
                case ComponentTypeTN.AdvRail: //20%D 20%B 60%N
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = cost * 0.2m;
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = cost * 0.2m;
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = cost * 0.6m;
                    break;

                case ComponentTypeTN.Gauss: //100%V
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Vendarite] = cost;
                    break;
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
            for (int RangeIncrementIterator = 1; RangeIncrementIterator < RangeIncrement; RangeIncrementIterator++)
            {
                ushort IncrementDamage = 0;
                if (RangeIncrementIterator < (m_oWeaponRangeTech + 1))
                {
                    IncrementDamage = m_lDamage[0];
                }
                else
                {
                    IncrementDamage = (ushort)Math.Floor(((float)((float)(m_oWeaponRangeTech + 1) / (float)(RangeIncrementIterator + 1)) * (float)m_lDamage[0]));

                    if (IncrementDamage == 0)
                        IncrementDamage = 1;
                }
                m_lDamage.Add(IncrementDamage);
            }
            m_lDamage.Add(0);
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
        private BeamDefTN m_oBeamDef;
        public BeamDefTN beamDef
        {
            get { return m_oBeamDef; }
        }

        /// <summary>
        /// Which fire control component is this beamweapon linked to?
        /// </summary>
        private BeamFireControlTN m_oFireController;
        public BeamFireControlTN fireController
        {
            get { return m_oFireController; }
            set { m_oFireController = value; }
        }

        /// <summary>
        /// What is the state of this beam weapon's capacitor?
        /// </summary>
        private float m_oCurrentCapacitor;
        public float currentCapacitor
        {
            get { return m_oCurrentCapacitor; }
            set { m_oCurrentCapacitor = value; }
        }

        /// <summary>
        /// Has this Beam fired in defense, and if so how many of its shots were used. this will only be applicable to Gauss Cannons and Railguns, and only for PD.
        /// </summary>
        private int m_oShotsExpended;
        public int shotsExpended
        {
            get { return m_oShotsExpended; }
            set { m_oShotsExpended = value; }
        }

        /// <summary>
        /// Constructor for beam weapons. FireController is null for the time being as no BFC is assigned by default, and CurrentCapacitor is set to filled and ready to fire.
        /// </summary>
        /// <param name="definition">Beam definition to use for this weapon.</param>
        public BeamTN(BeamDefTN definition)
        {
            m_oBeamDef = definition;

            m_oFireController = null;

            m_oCurrentCapacitor = m_oBeamDef.powerRequirement;

            isDestroyed = false;
        }

        /// <summary>
        /// ReadyToFire determines if the beam weapon has the ability to fire. Gauss may always fire, everything else needs to have capacitor charge equal to their power requirement.
        /// </summary>
        /// <returns>Whether the weapon can fire or not(true or false)</returns>
        public bool readyToFire()
        {
            if (m_oBeamDef.componentType == ComponentTypeTN.Gauss && isDestroyed == false)
            {
                if (m_oShotsExpended == 0)
                    return true;
                else
                    return false;
            }
            else if (m_oCurrentCapacitor == m_oBeamDef.powerRequirement && isDestroyed == false)
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
                m_oCurrentCapacitor = 0;
            }

            return ready;
        }

        /// <summary>
        /// timeToFire calculates how many seconds until this beam weapon is charged.
        /// </summary>
        /// <returns>number of seconds(not ticks) until this gun is recharged. the smallest unit of time is the 5 second increment for recharging, so round up.</returns>
        public int timeToFire()
        {
            int SecondsToCharge = (int)Math.Ceiling((((float)m_oBeamDef.powerRequirement - m_oCurrentCapacitor) / m_oBeamDef.weaponCapacitor));
            SecondsToCharge = SecondsToCharge * (int)Constants.TimeInSeconds.FiveSeconds;
            return SecondsToCharge;

        }
    }
}
