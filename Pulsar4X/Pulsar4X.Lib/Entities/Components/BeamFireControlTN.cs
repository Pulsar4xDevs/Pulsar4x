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
    public class BeamFireControlDefTN : ComponentDefTN
    {
        /// <summary>
        /// Base range tech this BFC is built with.
        /// </summary>
        private float RangeBase;
        public float rangeBase
        {
            get { return RangeBase; }
        }

        /// <summary>
        /// Base tracking tech this BFC is built with.
        /// </summary>
        private float TrackBase;
        public float trackBase
        {
            get { return TrackBase; }
        }

        /// <summary>
        /// Size and Range adjusting modification to RangeBase.
        /// </summary>
        private float RangeMod;
        public float rangeMod
        {
            get { return RangeMod; }
        }

        /// <summary>
        /// Size and Tracking adjustment modification to TrackBase.
        /// </summary>
        private float TrackMod;
        public float trackMod
        {
            get { return TrackMod; }
        }

        /// <summary>
        /// Overall range for 50% accuracy.
        /// </summary>
        private float Range;
        public float range
        {
            get { return Range; }
        }

        /// <summary>
        /// Overall Tracking for 100% accuracy.
        /// </summary>
        private float Tracking;
        public float tracking
        {
            get { return Tracking; }
        }

        /// <summary>
        /// Electronic hardening 1.0 to 0.1.
        /// </summary>
        private float Hardening;
        public float hardening
        {
            get { return hardening; }
        }

        /// <summary>
        /// PDCs get 50% bonus to range. Mutually exclusive with IsFighter.
        /// </summary>
        private bool IsPDC;
        public bool isPDC
        {
            get { return IsPDC; }
        }

        /// <summary>
        /// fighters get 200% bonus to tracking. mutually exclusive with IsPDC.
        /// </summary>
        private bool IsFighter;
        public bool isFighter
        {
            get { return IsFighter; }
        }

        /// <summary>
        /// List of accuracy in 1KM increments. 0 = 100, Range = 50, 2xRange = 0.
        /// What is more, from 0-10k is point blank accuracy, it is not 100%, but 10k-MaxRange/MaxRange percent, the next is 10-20k, and so on.
        /// </summary>
        private BindingList<float> RangeAccuracyTable;
        public BindingList<float> rangeAccuracyTable
        {
            get { return RangeAccuracyTable; }
        }

        /// <summary>
        /// tracking accuracy lookup table, this one works a little differently from the range accuracy table, this one will be 0(1% accuracy) to 99(100% accuracy).
        /// </summary>
        private BindingList<float> TrackingAccuracyTable;
        public BindingList<float> trackingAccuracyTable
        {
            get { return trackingAccuracyTable; }
        }


        /// <summary>
        /// Constructor for BFC definitions.
        /// </summary>
        /// <param name="Title">Name of FC displayed to player.</param>
        /// <param name="BaseRange">Base Range Technology of Device.</param>
        /// <param name="BaseTracking">Base Tracking Tech.</param>
        /// <param name="ModRange">Size and Range modification. 0.25-4x</param>
        /// <param name="ModTracking">Size and Tracking modifications. 0.5-4x.</param>
        /// <param name="PDC">Is this a PDC BFC? if so +50% range.</param>
        /// <param name="Fighter">Is this a FTR BFC? if so +200% tracking.</param>
        /// <param name="hard">Chance of damage due to electronic damage.</param>
        /// <param name="hardTech">Tech level for electronic hardening.</param>
        public BeamFireControlDefTN(string Title, float BaseRange, float BaseTracking, float ModRange, float ModTracking, bool PDC, bool Fighter, float hard, byte hardTech)
        {
            Id = Guid.NewGuid();
            componentType = ComponentTypeTN.BeamFireControl;

            Name = Title;
            size = 1.0f;

            RangeBase = BaseRange;
            TrackBase = BaseTracking;

            RangeMod = ModRange;
            TrackMod = ModTracking;

            size = size * ModRange * ModTracking;

            Range = RangeBase * RangeMod;
            Tracking = TrackBase * TrackMod;

            IsPDC = PDC;
            IsFighter = Fighter;

            Hardening = hard;

            if (IsPDC == true && IsFighter == true)
            {
                IsFighter = false;
            }

            if (IsPDC == true)
            {
                RangeBase = RangeBase * 1.5f;
            }

            if (IsFighter == true)
            {
                TrackBase = TrackBase * 4.0f;
            }

            crew = (byte)(size * 2.0f);

            /// <summary>
            /// Not the exact cost calculation but close.
            /// </summary>
            cost = (decimal)(5.0f * (Range / 16000.0f) * (Tracking / 2000.0f)); 
            cost = cost + (decimal)((float)cost * 0.25f * (float)(hardTech - 1));

            /// <summary>
            /// Range * 2 / 10000.0
            /// </summary>
            int RangeIncrement = (int)Math.Floor(Range / 5000.0f);

            RangeAccuracyTable = new BindingList<float>();

            for (int loop = 1; loop < RangeIncrement; loop++)
            {
                float MaxRange = Range * 2.0f;
                float CurRange = loop * 10000.0f;

                float Accuracy = (MaxRange - CurRange) / MaxRange;
                RangeAccuracyTable.Add(Accuracy);
            }

            TrackingAccuracyTable = new BindingList<float>();

            for (int loop = 0; loop < 100; loop++)
            {
                float Accuracy = Tracking / (float)(((float)loop+1.0f)/100.0f);
                TrackingAccuracyTable.Add(Accuracy);
            }
        }
    }

    public class BeamFireControlTN : ComponentTN
    {
        /// <summary>
        /// definition of BFC.
        /// </summary>
        private BeamFireControlDefTN BeamFireControlDef;
        public BeamFireControlDefTN beamFireControlDef
        {
            get { return BeamFireControlDef; }
        }

        /// <summary>
        /// Weapon and ECM Link.
        /// </summary>


        public BeamFireControlTN(BeamFireControlDefTN definition)
        {
            BeamFireControlDef = definition;
            isDestroyed = false;
        }
    }
}
