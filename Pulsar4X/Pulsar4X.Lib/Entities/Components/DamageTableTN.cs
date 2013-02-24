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
    public enum DamageTypeTN
    {
        Beam,
        Kinetic,
        Plasma,
        Missile,
        TypeCount
    }
    public class DamageTableTN : GameEntity
    {
        /// <summary>
        /// What type of damage is this?
        /// </summary>
        private DamageTypeTN Type;
        public DamageTypeTN type
        {
            get { return Type; }
        }

        /// <summary>
        /// How much damage is done for the corresponding template.
        /// </summary>
        private ushort DamageTotal;
        public ushort damageTotal
        {
            get { return DamageTotal; }
        }

        /// <summary>
        /// How much damage goes to armor penetration, and how much to the side columns?
        /// </summary>
        private byte Penetration;
        public byte penetration
        {
            get { return Penetration; }
        }

        /// <summary>
        /// Across how many columns of armor does this damage spread?
        /// </summary>
        private int Spread;
        public int spread
        {
            get { return Spread; }
        }

        /// <summary>
        /// The template for damage from this weapon.
        /// </summary>
        private BindingList<ushort> DamageTemplate;
        public BindingList<ushort> damageTemplate
        {
            get { return DamageTemplate; }
        }


        public DamageTableTN(DamageTypeTN DamageType, ushort Damage)
        {
            Type = DamageType;
            DamageTotal = Damage;
            Spread = 1;

            switch (Type)
            {
                /// <summary>
                /// Beams are the best at penetrating armor.
                /// </summary>
                case DamageTypeTN.Beam :
                    
                    /// <summary>
                    /// As near as I can tell higher power beams are better at penetration overall than lower power beams.
                    /// </summary>
                    if (DamageTotal > 128)
                    {
                        Penetration = 5;
                    }
                    else if (DamageTotal > 64)
                    {
                        Penetration = 4;
                    }
                    else
                    {
                        Penetration = 3;
                    }
                break;
                /// <summary>
                /// Railguns and particle beams are not as good at penetrating armor, but have other characteristics that are valuable.
                /// </summary>
                case DamageTypeTN.Kinetic:
                    Penetration = 2;
                break;
                /// <summary>
                /// Plasma likewise roils over armor, but is cheap and does plenty of damage.
                /// Actual penetration is in effect 1/2.
                /// </summary>
                case DamageTypeTN.Plasma:
                    Penetration = 1;
                break;
                /// <summary>
                /// Missiles are the premier weapon of the game, and have the weakest penetration overall.
                /// </summary>
                case DamageTypeTN.Missile:
                    Penetration = 1;
                break;
            }

            /// <summary>
            /// Initialize the damage template.
            /// </summary>
            for (int loop = 0; loop < 50; loop++)
            {
                DamageTemplate.Add(0);
            }

            /// <summary>
            /// Plasmas actually have what is effectively 1/2 penetration.
            if (Type != DamageTypeTN.Plasma)
            {
                int RemainingDamage = DamageTotal;

                if (RemainingDamage > Penetration)
                {
                    DamageTemplate[25] = Penetration;
                    RemainingDamage = RemainingDamage - Penetration;
                }
                else
                {
                    DamageTemplate[25] = (ushort)RemainingDamage;
                    RemainingDamage = 0;
                }

                /// <summary>
                /// Do damage spread.
                /// </summary>
                while (RemainingDamage != 0)
                {
                    float DamagePerColumn = (float)((float)RemainingDamage / (float)(Spread + 2));
                    if (DamagePerColumn >= Penetration)
                    {
                        int HalfSpread = (int)((float)(Spread + 1) / 2.0f);
                        /// <summary>
                        /// 25 is the arbitrary midpoint for the damage template for the time being, and two is added to spread value every iteration of this
                        /// while loop.
                        /// </summary>
                        for (int loop = (25 - HalfSpread); loop < (25 + HalfSpread); loop++)
                        {
                            DamageTemplate[loop] = (ushort)(DamageTemplate[loop] + (ushort)Penetration);
                            RemainingDamage = RemainingDamage - Penetration;
                        }

                        Spread = Spread + 2;
                    }
                    else
                    {
                        int BasePerColumn = (int)Math.Floor(DamagePerColumn);
                        int HalfSpread = (int)((float)(Spread + 1) / 2.0f);

                        if (BasePerColumn > 0)
                        {
                            for (int loop = (25 - HalfSpread); loop < (25 + HalfSpread); loop++)
                            {
                                DamageTemplate[loop] = (ushort)(DamageTemplate[loop] + (ushort)BasePerColumn);
                                RemainingDamage = RemainingDamage - BasePerColumn;
                            }
                        }

                        for (int loop = (25 - HalfSpread); loop < (25 + HalfSpread); loop++)
                        {
                            DamageTemplate[loop] = (ushort)(DamageTemplate[loop] + 1);
                            RemainingDamage = RemainingDamage - 1;

                            if (RemainingDamage == 0)
                                break;
                        }
                    }
                }
            }
            else
            {
                /// <summary>
                /// Plasma damage has to be handled a little differently from beams,particle weapons, and missiles. Penetration value is effectively 1/2, and damage to armor spreads out more.
                /// </summary>

                int RemainingDamage = DamageTotal;

                if (RemainingDamage == 1)
                {
                    DamageTemplate[25] = 1;
                    RemainingDamage = 0;
                }
                else
                {
                    DamageTemplate[25] = 1;
                    DamageTemplate[26] = 1;
                    RemainingDamage = RemainingDamage - 2;

                    Spread = 2;

                    while (RemainingDamage != 0)
                    {
                        int HalfSpread = Spread / 2;
                        for (int loop = (25 - HalfSpread); loop < (26 + HalfSpread); loop++)
                        {
                            DamageTemplate[loop] = (ushort)(DamageTemplate[loop] + (ushort)1);
                            RemainingDamage = RemainingDamage - 1;

                            if (RemainingDamage == 0)
                                break;
                        }
                        Spread = Spread + 2;
                    }
                }

                
            }
        }
    }
}
