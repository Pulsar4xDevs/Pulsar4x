using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using log4net;
using System.ComponentModel;

/// <summary>
/// PassiveSensor describes those listening sensors which do not betray their existence to other observers.
/// Sensors can be either thermal detectors, or EM detectors, which detect engine flares and planetary industry signatures, and 
/// active sensors,shields, and planetary population signatures respectively.
/// </summary>

namespace Pulsar4X.Entities.Components
{
    public enum PassiveSensorType
    {
        Thermal,
        EM,
        TypeCount
    }

    /// <summary>
    /// Class definitions for passive sensors. this follows TN rules, NA rules change ship signatures, but I don't know about sensor mechanics themselves.
    /// </summary>
    public class PassiveSensorDefTN : ComponentDefTN
    {
        /// <summary>
        /// Is this sensor a thermal sensor, or an EM one?
        /// </summary>
        private PassiveSensorType ThermalOrEM;
        public PassiveSensorType thermalOrEM
        {
            get { return ThermalOrEM; }
        }

        /// <summary>
        /// Sensitivity per HS. a sensor with a sensitivity of 5 can detect a signature of 1000 at a range of 1M * 5 * Size.
        /// </summary>
        private byte Sensitivity;
        public byte sensitivity
        {
            get { return Sensitivity; }
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
        /// Size * sensitivity
        /// </summary>
        private float Rating;
        public float rating
        {
            get { return Rating; }
        }

        /// <summary>
        /// Range is the sensor's rating * 100 * 10,000, for 1M * rating, the 10,000 is handled elsewhere however.
        /// </summary>
        private int Range;
        public int range
        {
            get { return Range; }
        }

        /// <summary>
        /// fast lookup table for most signatures this sensor will encounter.
        /// </summary>
        private BindingList<int> LookUpTable;
        public BindingList<int> lookUpTable
        {
            get { return LookUpTable; }
        }

        /// <summary>
        /// Maximum number of entries in the lookup table.
        /// </summary>
        private short LookUpTableMax;

        /// <summary>
        /// Given the input that aurora expects, this constructor will build an appropriate passive sensor.
        /// </summary>
        /// <param name="title">Name of the sensor.</param>
        /// <param name="HS">Size of the sensor in HS. 0.1 to 50</param>
        /// <param name="sens">Sensitivity per HS of the sensor.</param>
        /// <param name="TOrE">Thermal/EM identifier for this sensor. false = thermal, true = EM</param>
        /// <param name="hard">Chance of destruction from electronic damage. 1.0 to 0.1</param>
        /// <param name="hardTech">Tech level of sensor hardening. This is to be adjusted downward by one, so level 0 is level 1.</param>
        public PassiveSensorDefTN(string title, float HS, byte sens, PassiveSensorType TOrE, float hard, byte hardTech)
        {
            Id = Guid.NewGuid();
            componentType = ComponentTypeTN.PassiveSensor;
           
            Name = title;
            size = HS;
            Sensitivity = sens;
            ThermalOrEM = TOrE;
            Hardening = hard;

            LookUpTableMax = 10000;

            /// <summary>
            /// Rating is the best way to compare sensors against each other.
            /// while Range tells me at what distance a signature of 1000 will be detected in KM.
            /// </summary>
            Rating = size * (float)Sensitivity;
            Range = (int)(Rating * 100.0f);

            /// <summary>
            /// Populate the lookup table.
            /// </summary>
            LookUpTable = new BindingList<int>();
            for(ushort loop = 0; loop < LookUpTableMax; loop++)
            {
                int DetectionRange = CalcUnknownSignature(loop);
                LookUpTable.Add(DetectionRange);
            }

            ///<summary>
            ///HTK is either 1 or 0, because all sensors are very weak to damage, especially electronic damage.
            ///</summary>
            if(size >= 1.0)
                htk = 1;
            else
                htk = 0;

            /// <summary>
            /// Crew and cost are related to size, sensitivity, and hardening.
            /// </summary>
            crew = (byte)(size * 2.0);
            cost = (decimal)((size * (float)Sensitivity) + ((size * (float)Sensitivity) * 0.25f * (float)(hardTech-1)));

            if (size <= 1.0)
                isMilitary = false;
            else
                isMilitary = true;

            isObsolete = false;
            isSalvaged = false;
            isDivisible = false;

            isElectronic = true;
        }

        /// <summary>
        /// Missile passive sensor constructor. This is actually not a component.
        /// </summary>
        /// <param name="PassiveStrength">Missile Passive Strength</param>
        /// <param name="TorE">Thermal or EM?</param>
        public PassiveSensorDefTN(float PassiveStrength, PassiveSensorType TOrE)
        {
            Id = Guid.NewGuid();
            componentType = ComponentTypeTN.TypeCount;

            Name = "MissilePassive";
            Sensitivity = 0;
            Hardening = 0;

            ThermalOrEM = TOrE;

            LookUpTableMax = 10000;

            /// <summary>
            /// Rating is the best way to compare sensors against each other.
            /// while Range tells me at what distance a signature of 1000 will be detected in KM.
            /// </summary>
            Rating = PassiveStrength;
            Range = (int)(Rating*100.0f);

            /// <summary>
            /// Populate the lookup table.
            /// </summary>
            LookUpTable = new BindingList<int>();
            for (ushort loop = 0; loop < LookUpTableMax; loop++)
            {
                int DetectionRange = CalcUnknownSignature(loop);
                LookUpTable.Add(DetectionRange);
            }

            crew = 0;
            cost = 0;
            htk = 0;
            size = 0;
            isMilitary = false;
            isObsolete = false;
            isSalvaged = false;
            isDivisible = false;
            isElectronic = false;
        }


        /// <summary>
        /// CalcUnknownSignature is useful in those situations where the desired signature is outside of the precalculated lookup table.
        /// </summary>
        /// <param name="Signature">The signature to be detected.</param>
        /// <returns>Range at which that signature is detected.</returns>
        public int CalcUnknownSignature(int Signature)
        {
            double rangeAdj = (((double)Signature) / 1000.0);
            int newRange = (int)((double)Range * rangeAdj);

            return newRange;
        }

        /// <summary>
        /// GetPassiveDetectionRange determines the range at which this sensor will detect any signature.
        /// Either by looking up the signature in the table, or by outright calculating it. Ship signature must be adjusted downward by 1
        /// for the lookup table and the calculation function.
        /// </summary>
        /// <param name="Signature">Signature to be detected.</param>
        /// <returns>Range at which this sensor can detect this signature.</returns>
        public int GetPassiveDetectionRange(int Signature)
        {
            int shipDet=0;
            int shipSig = Signature;

            if (shipSig >= LookUpTableMax)
                shipDet = CalcUnknownSignature(shipSig);
            else
                shipDet = LookUpTable[shipSig];

            return shipDet;
        }
    }
    /// <summary>
    /// End of PassiveSensorDefTN
    /// </summary>

    /// < summary>
    /// PassiveSensorTN contains the relevant and modifiable data that this sensor has, as well as a pointer to its definitions.
    /// </summary>
    public class PassiveSensorTN : ComponentTN
    {
        /// <summary>
        /// Related definitions for this sensor.
        /// </summary>
        private PassiveSensorDefTN PSensorDef;
        public PassiveSensorDefTN pSensorDef
        {
            get { return PSensorDef; }
        }

        /// <summary>
        /// This constructor sets the sensor to not damaged, and loads its definition.
        /// </summary>
        /// <param name="definition">Related passive sensor statistics.</param>
        public PassiveSensorTN(PassiveSensorDefTN definition)
        {
            PSensorDef = definition;

            Name = definition.Name;

            isDestroyed = false;
        }
    }
    ///<summary>
    ///end PassiveSensorTN class
    ///</summary>
}
