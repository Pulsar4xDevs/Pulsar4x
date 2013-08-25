using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    public class MagazineDefTN : ComponentDefTN
    {
        /// <summary>
        /// tech level for how much magazine space is used by the feeding mechanism.
        /// </summary>
        private int FeedEfficiencyTech;
        public int feedEfficiencyTech
        {
            get { return FeedEfficiencyTech; }
        }

        /// <summary>
        /// Tech level for ejection system of this magazine.
        /// </summary>
        private int EjectionTech;
        public int ejectionTech
        {
            get { return EjectionTech; }
        }

        /// <summary>
        /// Armor tech available for this magazine. only comes into play with HTK >= 2.
        /// </summary>
        private int ArmorTech;
        public int armorTech
        {
            get { return ArmorTech; }
        }

        /// <summary>
        /// chance of explosion from this magazine.
        /// </summary>
        private float ExpRisk;
        public float expRisk
        {
            get { return ExpRisk; }
        }

        /// <summary>
        /// Missile size points this magazine can hold.
        /// </summary>
        private int Capacity;
        public int capacity
        {
            get { return Capacity; }
        }



        /// <summary>
        /// Definition constructor.
        /// </summary>
        /// <param name="title">Name of magazine.</param>
        /// <param name="hs">size in HS(50 tons = 1).</param>
        /// <param name="desiredHTK">wanted htk level, 1-10.</param>
        /// <param name="FeedTech">Tech level of feed mechanism.</param>
        /// <param name="EjectTech">Tech level of ejection mechanism.</param>
        /// <param name="ArmorHSTech">Internal armor level, same as faction armor tech. or should be.</param>
        public MagazineDefTN(string title, float hs, byte desiredHTK, int FeedTech, int EjectTech, int ArmorHSTech)
        {
            Id = Guid.NewGuid();

            componentType = ComponentTypeTN.Magazine;
            
            Name = title;
            size = hs;

            if (desiredHTK < 1)
                desiredHTK = 1;

            if (desiredHTK > 10)
                desiredHTK = 10;

            htk = desiredHTK;
            
            
            crew = (byte)(size / 2.0f);

            if (crew == 0)
                crew = 1;

            /// <summary>
            /// Cost is size and armor related.
            /// </summary>
            cost = (decimal)(size * 5.0f);

            FeedEfficiencyTech = FeedTech;
            EjectionTech = EjectTech;
            ArmorTech = ArmorHSTech;

            float ArmorFactor = 0.0f;

            /// <summary>
            /// have some rounding to do here:
            /// </summary>
            if(desiredHTK >= 2)
            {
                ArmorFactor = 1.0f;
                float pi = 3.14159654f;
                float temp1 = (1.0f / 3.0f);

                float radius3 = (3.0f * size) / (4.0f * pi);
                float radius = (float)Math.Pow(radius3, temp1);
                float radius2 = (float)Math.Pow(radius, 2.0f);
                float area = (4.0f * pi) * radius2;
                float StrReq = area / 8.0f;

                int mult = desiredHTK - 1;

                cost = cost + (decimal)(StrReq * mult);

                ArmorFactor = (StrReq / Constants.MagazineTN.MagArmor[ArmorTech]) * (float)mult;
            }

            float modifiedSize = size - ArmorFactor;
            if (modifiedSize < 0.0f)
                modifiedSize = 0.0f;

            Capacity = (int)(modifiedSize * 20.0f * Constants.MagazineTN.FeedMechanism[FeedEfficiencyTech]);

            ExpRisk = (1.0f - Constants.MagazineTN.Ejection[EjectionTech]) * 100.0f;

            isMilitary = true;
            isObsolete = false;
            isSalvaged = false;
            isElectronic = false;
            isDivisible = false;
        }
    }

    public class MagazineTN : ComponentTN
    {
        /// <summary>
        /// loaded missiles need to be tracked at some level.
        /// </summary>

        /// <summary>
        /// Definition of this magazine.
        /// </summary>
        private MagazineDefTN MagazineDef;
        public MagazineDefTN magazineDef
        {
            get { return MagazineDef; }
        }

        /// <summary>
        /// Missiles stored by this magazine.
        /// </summary>
        private Dictionary<OrdnanceDefTN, int> MagOrdnance;
        public Dictionary<OrdnanceDefTN, int> magOrdnance
        {
            get { return MagOrdnance; }
        }

        /// <summary>
        /// Current missiles stored.
        /// </summary>
        private int CurCapacity;
        public int curCapacity
        {
            get { return CurCapacity; }
        }

        /// <summary>
        /// Constructor for this particular component.
        /// </summary>
        /// <param name="definition">definition of magazine.</param>
        public MagazineTN(MagazineDefTN definition)
        {
            MagazineDef = definition;

            MagOrdnance = new Dictionary<OrdnanceDefTN, int>();

            CurCapacity = 0;

            isDestroyed = false;
        }

        /// <summary>
        /// In the event of magazine destruction, or unload events.
        /// </summary>
        public void ClearMagazine()
        {
            MagOrdnance.Clear();
        }

        /// <summary>
        /// Load Missiles to this magazine. can also unload missiles if amt is negative.
        /// </summary>
        /// <param name="Missile">Missile to be loaded.</param>
        /// <param name="amt">number of missiles to load.</param>
        /// <returns>Number of missiles actually loaded.</returns>
        public int LoadMagazine(OrdnanceDefTN Missile, int amt)
        {
            int MissileTonnage = (int)Missile.size * amt;
            int loadAmt = 0;

            if (amt > 0)
            {
                if (CurCapacity + MissileTonnage <= MagazineDef.capacity)
                {
                    loadAmt = amt;
                }
                else
                {
                    if (CurCapacity == MagazineDef.capacity)
                    {
                        return 0;
                    }
                    else
                    {
                        int capRemaining = MagazineDef.capacity - CurCapacity;
                        loadAmt = (int)Math.Floor(((float)capRemaining / Missile.size));
                    }
                }

                if (MagOrdnance.ContainsKey(Missile))
                {
                    MagOrdnance[Missile] = MagOrdnance[Missile] + loadAmt;
                }
                else
                {
                    MagOrdnance.Add(Missile, loadAmt);
                }

                return loadAmt;
            }
            else
            {
                if (MagOrdnance.ContainsKey(Missile) == false)
                {
                    return 0;
                }
                else
                {
                    /// <summary>
                    /// amt is negative here.
                    /// </summary>
                    MagOrdnance[Missile] = MagOrdnance[Missile] + amt;

                    if (MagOrdnance[Missile] <= 0)
                    {
                        MagOrdnance.Remove(Missile);
                    }
                }
                return amt;
            }
        }

    }
}
