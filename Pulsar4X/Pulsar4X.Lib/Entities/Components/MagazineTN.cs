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
        /// Internal armour HS
        /// </summary>
        private float ArmourFactor;
        public float armourFactor
        {
            get { return ArmourFactor; }
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


            minerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                minerialsCost[mineralIterator] = 0;
            }
            minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = cost * 0.75m;
            minerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = cost * 0.25m;

            /// <summary>
            /// have some rounding to do here:
            /// </summary>
            if (desiredHTK >= 2)
            {
                ArmorFactor = 1.0f;
                float pi = 3.14159654f;
                float temp1 = (1.0f / 3.0f);

                float radius3 = (3.0f * size) / (4.0f * pi);
                float radius = (float)Math.Pow(radius3, temp1);
                float radius2 = (float)Math.Pow(radius, 2.0f);
                float area = (4.0f * pi) * radius2;
                float StrReq = area / 10.0f;

                StrReq = StrReq * 100.0f;
                StrReq = (float)Math.Round(StrReq);
                StrReq = StrReq / 100.0f;

                int mult = desiredHTK - 1;

                decimal ArmorCost = (decimal)(StrReq * mult);

                cost = cost + ArmorCost;

                /// <summary>
                /// Copied and pasted from the ArmorDefTN Section:
                /// </summary>
                //0-1.0 1-1.0 2-1.0 3- D:9/10 N:1/10 ... 12 - D:1/10 N:9/10
                int fraction = 13 - ArmorTech;
                //13 12 11 10 9 8 7 6 5 4 3 2 1
                float DuraniumFraction = (float)fraction / 10.0f;
                //1.3 1.2 1.1 1.0 .9 .8 .7 .6 .5 .4 .3 .2 .1
                float NeutroniumFraction = (10.0f - (float)fraction) / 10.0f;
                //-.3  -.2  -.1 0 .1 .2 .3 .4 .5 .6 .7 .8 .9   
                if (DuraniumFraction >= 1.0)
                {
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] + cost;
                }
                else
                {
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] + (cost * (decimal)DuraniumFraction);
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = cost * (decimal)NeutroniumFraction;
                }

                ArmorFactor = (StrReq / Constants.MagazineTN.MagArmor[ArmorTech]) * (float)mult;
            }

            ArmorFactor = ArmorFactor * 100.0f;
            ArmorFactor = (float)Math.Round(ArmorFactor);
            ArmorFactor = ArmorFactor / 100.0f;

            float modifiedSize = size - ArmorFactor;
            if (modifiedSize < 0.0f)
                modifiedSize = 0.0f;

            ArmourFactor = ArmorFactor;

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
        /// Constructor for this particular component.
        /// </summary>
        /// <param name="definition">definition of magazine.</param>
        public MagazineTN(MagazineDefTN definition)
        {
            MagazineDef = definition;

            Name = definition.Name;

            //MagOrdnance = new Dictionary<OrdnanceDefTN, int>();

            //CurCapacity = 0;

            isDestroyed = false;
        }
    }
}
