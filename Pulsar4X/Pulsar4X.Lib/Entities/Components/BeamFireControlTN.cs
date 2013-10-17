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
    public enum PointDefenseState
    {
        None,
        AreaDefense,
        FinalDefensiveFire,
        FinalDefensiveFireSelf,
        AMM1v2,
        AMM1v1,
        AMM2v1,
        AMM3v1,
        AMM4v1,
        AMM5v1,
        TypeCount
    }
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
            get { return TrackingAccuracyTable; }
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

            RangeAccuracyTable.Add(0.0f);

            TrackingAccuracyTable = new BindingList<float>();

            for (int loop = 0; loop < 100; loop++)
            {

                float Factor = (float)((float)loop+1.0f) /100.0f;
                float Accuracy = Tracking / Factor;
                TrackingAccuracyTable.Add(Accuracy);
            }

            isMilitary = true;

            isElectronic = true;

            isSalvaged = false;
            isObsolete = false;
            isDivisible = false;

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
        /// ECCMs Linked to this BFC. Update DestroyComponents when eccm is added.
        /// </summary>

        /// <summary>
        /// Weapons linked to this BFC.
        /// </summary>
        private BindingList<BeamTN> LinkedWeapons;
        public BindingList<BeamTN> linkedWeapons
        {
            get { return LinkedWeapons; }
            set { LinkedWeapons = value; }
        }

        /// <summary>
        /// Target Assigned to this BFC
        /// </summary>
        private ShipTN Target;
        public ShipTN target
        {
            get { return Target; }
            set { Target = value; }
        }

        /// <summary>
        /// Whether this FC is authorized to fire on its target.
        /// </summary>
        private bool OpenFire;
        public bool openFire
        {
            get { return OpenFire; }
            set { OpenFire = value; }
        }

        /// <summary>
        /// Point defense state this BFC will fire to.
        /// </summary>
        private PointDefenseState PDState;
        public PointDefenseState pDState
        {
            get { return PDState; }
        }

        /// <summary>
        /// Constructor for BFC components.
        /// </summary>
        /// <param name="definition">Definition of this component</param>
        public BeamFireControlTN(BeamFireControlDefTN definition)
        {
            BeamFireControlDef = definition;
            isDestroyed = false;

            LinkedWeapons = new BindingList<BeamTN>();
            
            Target = null;
            OpenFire = false;
            PDState = PointDefenseState.None;
        }

        /// <summary>
        /// Set the fire control to the desired point defense state.
        /// </summary>
        /// <param name="State">State the BFC is to be set to.</param>
        public void SetPointDefenseMode(PointDefenseState State)
        {
            if (State <= PointDefenseState.FinalDefensiveFireSelf)
                PDState = State;
            else
            {
                /// <summary>
                /// Bad PDState for BFCs.
                /// </summary>
            }
        }

        /// <summary>
        /// Simple assignment of a ship as a target to this bfc.
        /// </summary>
        /// <param name="ShipTarget">Ship to be targeted.</param>
        public void assignTarget(ShipTN ShipTarget)
        {
            Target = ShipTarget;
        }

        /// <summary>
        /// Simple deassignment of target to this bfc.
        /// </summary>
        public void clearTarget()
        {
            Target = null;
        }

        /// <summary>
        /// Simple return of the target of this BFC.
        /// </summary>
        public ShipTN getTarget()
        {
            return Target;
        }

        /// <summary>
        /// Clears all weapon links.
        /// </summary>
        public void clearWeapons()
        {
            for (int loop = 0; loop < LinkedWeapons.Count; loop++)
            {
                LinkedWeapons[loop].fireController = null;
            }
            LinkedWeapons.Clear();
        }

        /// <summary>
        /// Links a weapon to this fc
        /// </summary>
        /// <param name="beam">beam weapon to link</param>
        public void linkWeapon(BeamTN beam)
        {
            if (linkedWeapons.Contains(beam) == false)
            {
                linkedWeapons.Add(beam);

                beam.fireController = this;
            }
        }

        /// <summary>
        /// removes a weapon from this FC
        /// </summary>
        /// <param name="beam">beamweapon to unlink</param>
        public void unlinkWeapon(BeamTN beam)
        {
            if (linkedWeapons.Contains(beam) == true)
            {
                linkedWeapons.Remove(beam);

                beam.fireController = null;
            }
        }

        /// <summary>
        /// The Fire control itself must determine if the target is in range of both itself and its weapons.
        /// </summary>
        /// <param name="DistanceToTarget">Distance in KM to target.</param>
        /// <param name="RNG">RNG passed to this function from source further up the chain.</param>
        /// <returns>Whether or not a weapon was able to fire.</returns>
        public bool FireWeapons(float DistanceToTarget, Random RNG)
        {
            if (DistanceToTarget > BeamFireControlDef.range || LinkedWeapons.Count == 0 || isDestroyed == true)
            {
                return false;
            }
            else
            {
                /// <summary>
                /// Range * 2 / 10000.0;
                /// </summary>

                int RangeIncrement = (int)Math.Floor(DistanceToTarget / 5000.0f);
                float FireAccuracy = BeamFireControlDef.rangeAccuracyTable[RangeIncrement];
                /// <summary>
                /// 100% accuracy due to tracking at this speed.
                /// </summary>
                if (Target.CurrentSpeed > BeamFireControlDef.trackingAccuracyTable[99])
                {
                    if (Target.CurrentSpeed >= BeamFireControlDef.trackingAccuracyTable[0])
                    {
                        FireAccuracy = FireAccuracy * 0.01f;
                    }
                    else
                    {
                        bool done = false;
                        int Base = 50;
                        int Cur = 50;
                        /// <summary>
                        /// Binary search through the tracking accuracy table.
                        /// </summary>
                        
                        while (!done)
                        {
                            Cur = Cur / 2;
                            if (Target.CurrentSpeed > BeamFireControlDef.trackingAccuracyTable[Base])
                            {
                                if (Cur == 1)
                                {
                                    float t1 = Target.CurrentSpeed - BeamFireControlDef.trackingAccuracyTable[Base];
                                    float t2 = Target.CurrentSpeed - BeamFireControlDef.trackingAccuracyTable[Base - 1];
                                    if (t1 <= t2)
                                    {
                                        FireAccuracy = FireAccuracy * ((float)Base / 100.0f);
                                        done = true;
                                    }
                                    else
                                    {
                                        FireAccuracy = FireAccuracy * ((float)(Base - 1) / 100.0f);
                                        done = true;
                                    }
                                }
                                Base = Base - Cur;
                            }
                            else if (Target.CurrentSpeed < BeamFireControlDef.trackingAccuracyTable[Base])
                            {
                                if (Cur == 1)
                                {
                                    float t1 = BeamFireControlDef.trackingAccuracyTable[Base] - Target.CurrentSpeed;
                                    float t2 = BeamFireControlDef.trackingAccuracyTable[Base + 1] - Target.CurrentSpeed;
                                    if (t1 <= t2)
                                    {
                                        FireAccuracy = FireAccuracy * ((float)Base / 100.0f);
                                        done = true;
                                    }
                                    else
                                    {
                                        FireAccuracy = FireAccuracy * ((float)(Base + 1) / 100.0f);
                                        done = true;
                                    }
                                }
                                Base = Base + Cur;
                            }
                            else
                            {
                                FireAccuracy = FireAccuracy * ((float)Base / 100.0f);
                                done = true;
                            }
                        }
                    }
                }
                
                /// <summary>
                /// Fire Accuracy is the likelyhood of a shot hitting from this FC at this point.
                /// ECM vs ECCM needs to be done around here as well.
                /// </summary>
                bool weaponFired = false;

                int toHit = (int)Math.Floor(FireAccuracy * 100.0f);
                ushort Columns = Target.ShipArmor.armorDef.cNum;

                for (int loop = 0; loop < LinkedWeapons.Count; loop++)
                {
                    if (LinkedWeapons[loop].beamDef.range > DistanceToTarget && LinkedWeapons[loop].readyToFire() == true)
                    {
                        RangeIncrement = (int)Math.Floor(DistanceToTarget / 10000.0f);

                        int Hit = RNG.Next(1, 100);


                        weaponFired = LinkedWeapons[loop].Fire();

                        if(toHit >= Hit && weaponFired == true)
                        {
                            ushort location = (ushort)RNG.Next(0,Columns);
                            bool ShipDest = Target.OnDamaged(LinkedWeapons[loop].beamDef.damageType, LinkedWeapons[loop].beamDef.damage[RangeIncrement], location);

                            if (ShipDest == true)
                            {
                                Target = null;
                                OpenFire = false;
                                return weaponFired;
                            }
                        }
                    }
                }

                return weaponFired;
            }       
        }
    }
}
