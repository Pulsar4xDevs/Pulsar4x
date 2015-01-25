using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public enum DamageTypeTN
    {
        Beam,
        Kinetic,
        Plasma,
        Missile,
        Meson,
        Microwave,
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
        /// Since damage fans out from a point of initial impact, halfspread is useful for certain calculations.
        /// </summary>
        private int HalfSpread;
        public int halfSpread
        {
            get { return HalfSpread; }
        }

        /// <summary>
        /// The template for damage from this weapon.
        /// </summary>
        private BindingList<ushort> DamageTemplate;
        public BindingList<ushort> damageTemplate
        {
            get { return DamageTemplate; }
        }

        /// <summary>
        /// Where in the template is the point of the highest penetration?
        /// </summary>
        private byte HitPoint;
        public byte hitPoint
        {
            get { return HitPoint; }
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
                case DamageTypeTN.Beam:

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
            DamageTemplate = new BindingList<ushort>();
            for (int loop = 0; loop < 200; loop++)
            {
                DamageTemplate.Add(0);
            }

            HitPoint = 100;

            /// <summary>
            /// Plasmas actually have what is effectively 1/2 penetration.
            if (Type != DamageTypeTN.Plasma)
            {
                int RemainingDamage = DamageTotal;

                if (RemainingDamage > Penetration)
                {
                    DamageTemplate[HitPoint] = Penetration;
                    RemainingDamage = RemainingDamage - Penetration;
                }
                else
                {
                    DamageTemplate[HitPoint] = (ushort)RemainingDamage;
                    RemainingDamage = 0;
                }

                /// <summary>
                /// Do damage spread.
                /// </summary>
                while (RemainingDamage > 0)
                {
                    float DamagePerColumn = (float)((float)RemainingDamage / (float)(Spread + 2));
                    if (DamagePerColumn >= Penetration)
                    {
                        HalfSpread = (int)((float)(Spread + 1) / 2.0f);
                        /// <summary>
                        /// 100 is the arbitrary midpoint for the damage template for the time being, and two is added to spread value every iteration of this
                        /// while loop.
                        /// </summary>
                        for (int loop = (HitPoint - HalfSpread); loop <= (HitPoint + HalfSpread); loop++)
                        {
                            DamageTemplate[loop] = (ushort)(DamageTemplate[loop] + (ushort)Penetration);
                            RemainingDamage = RemainingDamage - Penetration;
                        }

                        Spread = Spread + 2;
                    }
                    else
                    {
                        int BasePerColumn = (int)Math.Floor(DamagePerColumn);
                        HalfSpread = (int)((float)(Spread + 1) / 2.0f);
                        if (BasePerColumn > 0)
                        {
                            for (int loop = (HitPoint - HalfSpread); loop <= (HitPoint + HalfSpread); loop++)
                            {
                                DamageTemplate[loop] = (ushort)(DamageTemplate[loop] + (ushort)BasePerColumn);
                                RemainingDamage = RemainingDamage - BasePerColumn;
                            }
                        }

                        for (int loop = (HitPoint - HalfSpread); loop <= (HitPoint + HalfSpread); loop++)
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
                    DamageTemplate[HitPoint] = 1;
                    RemainingDamage = 0;
                }
                else
                {
                    DamageTemplate[HitPoint] = 1;
                    DamageTemplate[HitPoint + 1] = 1;
                    RemainingDamage = RemainingDamage - 2;

                    Spread = 2;

                    while (RemainingDamage != 0)
                    {
                        HalfSpread = Spread / 2;

                        for (int loop = HalfSpread; loop >= 0; loop--)
                        {
                            DamageTemplate[HitPoint - loop] = (ushort)(DamageTemplate[HitPoint - loop] + (ushort)1);

                            RemainingDamage = RemainingDamage - 1;
                            if (RemainingDamage == 0)
                                break;

                            DamageTemplate[HitPoint + loop + 1] = (ushort)(DamageTemplate[HitPoint + loop + 1] + (ushort)1);

                            RemainingDamage = RemainingDamage - 1;
                            if (RemainingDamage == 0)
                                break;
                        }

                        Spread = Spread + 2;
                    }
                }
            }

            /// <summary>
            /// Remove the excess 0s from the template.
            /// </summary>

            int limit = DamageTemplate.Count;
            for (int loop = limit - 1; loop >= 0; loop--)
            {
                if (DamageTemplate[loop] == 0)
                {
                    DamageTemplate.RemoveAt(loop);

                    if (loop < HitPoint)
                        HitPoint--;
                }
            }
        }
    }

    /// <summary>
    /// Damage Table kludge.
    /// </summary>
    public static class DamageValuesTN
    {
        public static BindingList<DamageTableTN> EnergyTable;

        public static BindingList<DamageTableTN> PlasmaTable;

        public static BindingList<DamageTableTN> KineticTable;

        public static BindingList<DamageTableTN> MissileTable;

        /// <summary>
        /// Provisional Damage table location. Check out program.cs for where this is initialized.
        /// </summary>
        public static void init()
        {
            EnergyTable = new BindingList<DamageTableTN>();
            PlasmaTable = new BindingList<DamageTableTN>();
            KineticTable = new BindingList<DamageTableTN>();
            MissileTable = new BindingList<DamageTableTN>();
            for (int loop = 0; loop < 210; loop++)
            {
                DamageTableTN EV = new DamageTableTN(DamageTypeTN.Beam, (ushort)(loop + 1));
                DamageTableTN PV = new DamageTableTN(DamageTypeTN.Plasma, (ushort)(loop + 1));
                EnergyTable.Add(EV);
                PlasmaTable.Add(PV);
            }

            for (int loop = 0; loop < 80; loop++)
            {
                DamageTableTN KV = new DamageTableTN(DamageTypeTN.Kinetic, (ushort)(loop + 1));
                KineticTable.Add(KV);
            }

            for (int loop = 0; loop < 3000; loop++)
            {
                DamageTableTN MV = new DamageTableTN(DamageTypeTN.Missile, (ushort)(loop + 1));
                MissileTable.Add(MV);
            }
        }
    }
}
