using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public class ActiveSensorDefTN
    {
        /// <summary>
        /// Title of active sensor class
        /// </summary>
        private string Name;
        public string name
        {
            get { return Name; }
        }

        /// <summary>
        /// Sensor Strength component of range.
        /// </summary>
        private byte ActiveStrength;
        public byte activeStrength
        {
            get { return ActiveStrength; }
        }

        /// <summary>
        /// EM listening portion of sensor.
        /// </summary>
        private byte EMRecv;
        public byte eMRecv
        {
            get { return EMRecv; }
        }

        /// <summary>
        /// Sensor wavelength resolution. What size of ship this sensor is best suited to detecting.
        /// </summary>
        private ushort Resolution;
        public ushort resolution
        {
            get { return Resolution; }
        }

        /// <summary>
        /// Size of the Sensor
        /// </summary>
        private float Size;
        public float size
        {
            get { return Size; }
        }

        /// <summary>
        /// Grav Pulse Signature/Strength
        /// </summary>
        private int GPS;
        public int gps
        {
            get { return GPS; }
        }
        
        /// <summary>
        /// Range at which sensor can detect craft of same HS as resolution.
        /// </summary>
        private int MaxRange;
        public int maxRange
        {
            get { return MaxRange; }
        }

        /// <summary>
        /// Lookup table for ship resolutions
        /// </summary>
        private BindingList<int> LookUpST;
        public BindingList<int> lookUPST
        {
            get { return LookUpST; }
        }

        /// <summary>
        /// Limit to the number of resolutions that a sensor may possess.
        /// </summary>
        private ushort ResolutionMax;

        /// <summary>
        /// Lookup table for missile resolutions
        /// </summary>
        private BindingList<int> LookUpMT;
        public BindingList<int> lookUpMT
        {
            get { return LookUpMT; }
        }

        /// <summary>
        /// Likelyhood of destruction due to normal damage.
        /// </summary>
        private byte HTK;
        public byte htk
        {
            get { return HTK; }
        }

        /// <summary>
        /// Cost of the sensor in wealth and minerals.
        /// </summary>
        private decimal Cost;
        public decimal cost
        {
            get { return Cost; }
        }

        /// <summary>
        /// Crew required to operate the sensor.
        /// </summary>
        private byte Crew;
        public byte crew
        {
            get { return Crew; }
        }

        /// <summary>
        /// Small sensors are not military, and thus don't incur maintenance failures.
        /// </summary>
        private bool IsMilitary;
        public bool isMilitary
        {
            get { return IsMilitary; }
        }

        /// <summary>
        /// Is this a Missile fire control or an active sensor? false = sensor, true = MFC. MFCs have 3x the range of search sensors.
        /// </summary>
        private bool IsMFC;
        public bool isMFC
        {
            get { return IsMFC; }
        }

        /// <summary>
        /// Likelyhood of destruction from electronic(microwave) damage.
        /// </summary>
        private float Hardening;
        public float hardening
        {
            get { return Hardening; }
        }

        /// <summary>
        /// ActiveSensorDefTN builds a sensor definition based on the following parameters.
        /// </summary>
        /// <param name="desc">Name of the sensor that will be displayed to the player.</param>
        /// <param name="HS">Size in HS.</param>
        /// <param name="actStr">Active Strength of the sensor.</param>
        /// <param name="EMR">EM Listening portion of the sensor.</param>
        /// <param name="Res">Resolution of the sensor, what size of target is being searched for.</param>
        /// <param name="MFC">Is this sensor a search sensor, or a Missile Fire Control?</param>
        /// <param name="hard">Percent chance of destruction due to electronic damage.</param>
        /// <param name="hardTech">Level of electronic hardening tech. Adjusted downwards by 1, so that level 0 is level 1, and so on.</param>
        public ActiveSensorDefTN(string desc, float HS, byte actStr, byte EMR, ushort Res, bool MFC, float hard, byte hardTech)
        {
            /// <summary>
            /// basic sensor statistics.
            /// </summary>
            Name = desc;
            Size = HS;
            ActiveStrength = actStr;
            EMRecv = EMR;
            Resolution = Res;
            IsMFC = MFC;
            Hardening = hard;

            /// <summary>
            /// Crew and cost are related to size, ActiveStrength, and hardening.
            /// </summary>
            Crew = (byte)(Size * 2.0);
            Cost = (decimal)((Size * (float)ActiveStrength) + ((Size * (float)ActiveStrength) * 0.25f * (float)(hardTech - 1)));


            ///<summary>
            ///Small sensors are civilian, large are military.
            ///</summary>
            if (Size <= 1.0)
                IsMilitary = false;
            else
                IsMilitary = true;

            ///<summary>
            ///HTK is either 1 or 0, because all sensors are very weak to damage, especially electronic damage.
            ///</summary>
            if (Size >= 1.0)
                HTK = 1;
            else
                HTK = 0;

            ///<summary>
            ///GPS is the value that a ship's EM signature will be increased by when this sensor is active.
            ///</summary>
            GPS = (int)((float)ActiveStrength * Size * (float)Resolution);

            MaxRange = (int)((float)ActiveStrength * Size * (float)Math.Sqrt((double)Resolution) * (float)EMRecv * 10000.0f);

            if (IsMFC == true)
            {
                MaxRange = MaxRange * 3;
                GPS = 0;
            }

            LookUpST = new BindingList<int>();
            LookUpMT = new BindingList<int>();

            ResolutionMax = 500;

            ///<summary>
            ///Initialize the ship lookup Table.
            ///</summary>
            for (int loop = 0; loop < ResolutionMax; loop++)
            {
                ///<summary>
                ///Sensor Resolution can't resolve this target at its MaxRange due to the target's smaller size
                ///</summary>
                if ((loop + 1) < Resolution)
                {
                    int NewRange = (int)((float)MaxRange * (float)Math.Pow(((double)(loop + 1) / (float)Resolution), 2.0f));
                    LookUpST.Add(NewRange);
                }
                else if ((loop + 1) >= Resolution)
                {
                    LookUpST.Add(MaxRange);
                }
            }

            ///<summary>
            ///Initialize the missile lookup Table.
            ///Missile size is in MSP, and no missile may be a fractional MSP in size. Each MSP is 0.05 HS in size.
            ///</summary>
            for (int loop = 0; loop < 15; loop++)
            {
                ///<summary>
                ///Missile size never drops below 0.33, and missiles above 1 HS are atleast 1 HS. if I have to deal with 2HS missiles I can go to LookUpST
                ///</summary>
                if (loop == 0)
                {
                    int NewRange = (int)((float)MaxRange * (float)Math.Pow( ( 0.33 / (float)Resolution ),2.0f));
                    LookUpMT.Add(NewRange);
                }
                else if( loop != 14 )
                {
                    float msp = ((float)loop + 6.0f) * 0.05f;
                    int NewRange = (int)((float)MaxRange * Math.Pow( ( msp / (float)Resolution ),2.0f ));
                    LookUpMT.Add(NewRange);
                }
                else if( loop == 14 )
                {
                    lookUpMT.Add(LookUpST[0]);//size 1 is size 1
                }
            }
        }
        ///<summary>
        ///End ActiveSensorDefTN()
        ///</summary>

        /// <summary>
        /// GetActiveDetectionRange returns the range of either the ship or missile
        /// <param name="TCS">TCS is the ship total cross section. I want the function to return at what range this ship is detected at.</param>
        /// <param name="MSP">MSP is Missile Size Point. How big of a missile am I trying to find with this function.</param>
        /// <returns>Range at which the missile or ship is detected.</returns>
        /// </summary>
        public int GetActiveDetectionRange(int TCS, int MSP)
        {
            ///<summary>
            ///limits of the arrays
            ///</summary>
            if ( (TCS > 499 || TCS < 0) || (MSP > 14 || MSP < -1 ) )
            {
                return -1;
            }


            int DetRange;
            if (MSP == -1)
            {
                DetRange = LookUpST[TCS];
            }
            else
            {
                DetRange = LookUpMT[MSP];
            }
            return DetRange;
        }
        ///<summary>
        ///End GetActiveDetectionRange
        ///</summary>

    }
    /// <summary>
    /// End Class ActiveSensorDefTN
    /// </summary>

    /// <summary>
    /// Active sensor component definition.
    /// </summary>
    public class ActiveSensorTN
    {
        /// <summary>
        /// What statistics define this sensor?
        /// </summary>
        private ActiveSensorDefTN ASensorDef;
        public ActiveSensorDefTN aSensorDef
        {
            get { return ASensorDef; }
        }

        /// <summary>
        /// Is this component destroyed?
        /// </summary>
        private bool IsDestroyed;
        public bool isDestroyed
        {
            get { return IsDestroyed; }
        }

        /// <summary>
        /// Is this sensor active and thus both searching, and emitting an EM signature?
        /// </summary>
        private bool IsActive;
        public bool isActive
        {
            get { return IsActive; }
        }


        /// <summary>
        /// The active sensor component itself. It is initialized to not destroyed, and not active.
        /// </summary>
        /// <param name="define">Definition for the sensor.</param>
        public ActiveSensorTN(ActiveSensorDefTN define)
        {
            ASensorDef = define;
            IsDestroyed = false;
            IsActive = false;
        }
    }
    /// <summary>
    ///End ActiveSensorTN
    /// </summary>

}
