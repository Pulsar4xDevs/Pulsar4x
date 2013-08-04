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

            float ArmorFactor = 1.0f;

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
        /// Constructor for this particular component.
        /// </summary>
        /// <param name="definition">definition of magazine.</param>
        public MagazineTN(MagazineDefTN definition)
        {
            MagazineDef = definition;

            isDestroyed = false;
        }

    }
}
