using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
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
        /// Range tech level.
        /// </summary>
        private int RangeTech;
        public int rangeTech
        {
            get { return RangeTech; }
        }
        /// <summary>
        /// Tracking tech level
        /// </summary>
        private int TrackTech;
        private int trackTech
        {
            get { return TrackTech; }
        }

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
            get { return Hardening; }
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
        public BeamFireControlDefTN(string Title, int techRange, int techTrack, float ModRange, float ModTracking, bool PDC, bool Fighter, float hard, byte hardTech)
        {
            Id = Guid.NewGuid();
            componentType = ComponentTypeTN.BeamFireControl;

            Name = Title;
            size = 1.0f;

            RangeTech = techRange;
            TrackTech = techTrack;

            RangeBase = Constants.BFCTN.BeamFireControlRange[RangeTech] * 1000.0f;
            TrackBase = Constants.BFCTN.BeamFireControlTracking[TrackTech];

            RangeMod = ModRange;
            TrackMod = ModTracking;

            size = size * ModRange * ModTracking;

            if (size >= 1.0f)
                htk = 1;
            else
                htk = 0;

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

            Range = RangeBase * RangeMod;
            Tracking = TrackBase * TrackMod;

            crew = (byte)(size * 2.0f);

            /// <summary>
            /// Not the exact cost calculation, or close for that matter. I think cost is just a data base entry.
            /// </summary>

            float RTechAdjust = 10666.6667f / (float)(RangeTech+1);
            float TTechAdjust = 1666.6667f / (float)(TrackTech+1);


            float R = (RangeBase / RTechAdjust);
            float T = (TrackBase / TTechAdjust);
            float res = (float)Math.Round((R + T) * size);
            res = (float)Math.Round(res + ((float)res * 0.25f * (float)(hardTech - 1)));

            cost = (decimal)(res); 

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
        private TargetTN Target;
        public TargetTN target
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
            TargetTN NewShipTarget = new TargetTN(ShipTarget);
            Target = NewShipTarget;
        }

        /// <summary>
        /// Assignment of a missile group as a target.
        /// </summary>
        /// <param name="OrdGroupTarget">ordnance group to be targetted</param>
        public void assignTarget(OrdnanceGroupTN OrdGroupTarget)
        {
            TargetTN NewOrdTarget = new TargetTN(OrdGroupTarget);
            Target = NewOrdTarget;
        }

        /// <summary>
        /// assignment of a population as the target
        /// </summary>
        /// <param name="PopTarget">Population to be targetted</param>
        public void assignTarget(Population PopTarget)
        {
            TargetTN NewPopTarget = new TargetTN(PopTarget);
            Target = NewPopTarget;
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
        public TargetTN getTarget()
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
        /// <param name="track">Base empire tracking or ship speed ,whichever is higher. Turrets should set track to their tracking speed.</param>
        /// <returns>Whether or not a weapon was able to fire.</returns>
        public bool FireWeapons(float DistanceToTarget, Random RNG, int track, ShipTN FiringShip)
        {
            if (DistanceToTarget > BeamFireControlDef.range || LinkedWeapons.Count == 0 || isDestroyed == true)
            {
                if (DistanceToTarget > BeamFireControlDef.range)
                {
                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringZeroHitChance, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                         GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), (this.Name + " Zero % chance to hit."));

                    FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                }

                return false;
            }
            else
            {
                /// <summary>
                /// Range * 2 / 10000.0;
                /// </summary>

                int RangeIncrement = (int)Math.Floor(DistanceToTarget / 5000.0f);
                float FireAccuracy = GetFiringAccuracy(RangeIncrement,track);

                
                /// <summary>
                /// Fire Accuracy is the likelyhood of a shot hitting from this FC at this point.
                /// ECM vs ECCM needs to be done around here as well.
                /// </summary>
                bool weaponFired = false;

                if (Target.targetType == StarSystemEntityType.TaskGroup)
                {

                    int toHit = (int)Math.Floor(FireAccuracy * 100.0f);
                    ushort Columns = Target.ship.ShipArmor.armorDef.cNum;

                    for (int loop = 0; loop < LinkedWeapons.Count; loop++)
                    {
                        if (LinkedWeapons[loop].beamDef.range > DistanceToTarget && LinkedWeapons[loop].readyToFire() == true)
                        {
                            RangeIncrement = (int)Math.Floor(DistanceToTarget / 10000.0f);

                            weaponFired = LinkedWeapons[loop].Fire();

                            if (weaponFired == true)
                            {
                                for (int loop2 = 0; loop2 < LinkedWeapons[loop].beamDef.shotCount; loop2++)
                                {

                                    int Hit = RNG.Next(1, 100);

                                    if (toHit >= Hit)
                                    {

                                        String WeaponFireS = String.Format("{0} hit {1} damage at {2}% tohit", LinkedWeapons[loop].Name, LinkedWeapons[loop].beamDef.damage[RangeIncrement], toHit);

                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringHit, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                             GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                        FiringShip.ShipsFaction.MessageLog.Add(NMsg);


                                        ushort location = (ushort)RNG.Next(0, Columns);
                                        bool ShipDest = Target.ship.OnDamaged(LinkedWeapons[loop].beamDef.damageType, LinkedWeapons[loop].beamDef.damage[RangeIncrement], location, FiringShip);

                                        if (ShipDest == true)
                                        {
                                            Target = null;
                                            OpenFire = false;
                                            return weaponFired;
                                        }
                                    }
                                    else
                                    {
                                        String WeaponFireS = String.Format("{0} missed at {1}% tohit", LinkedWeapons[loop].Name, toHit);

                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringMissed, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                             GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                        FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                                    }
                                }
                            }
                            else if (LinkedWeapons[loop].isDestroyed == false)
                            {
                                String WeaponFireS = String.Format("{0} Recharging {1}/{2} Power", LinkedWeapons[loop].Name, LinkedWeapons[loop].currentCapacitor, LinkedWeapons[loop].beamDef.weaponCapacitor);

                                MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringRecharging, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                     GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                            }
                        }
                    }

                    return weaponFired;
                }
                else if (Target.targetType == StarSystemEntityType.Missile)
                {
                    /// <summary>
                    /// this is for beam targetting on missiles.
                    /// </summary>
                    int toHit = (int)Math.Floor(FireAccuracy * 100.0f);

                    /// <summary>
                    /// For all weapons linked to this BFC
                    /// </summary>
                    for (int loop = 0; loop < LinkedWeapons.Count; loop++)
                    {
                        /// <summary>
                        /// if range > distance and the weapon is ready to fire.
                        /// </summary>
                        if (LinkedWeapons[loop].beamDef.range > DistanceToTarget && LinkedWeapons[loop].readyToFire() == true)
                        {
                            RangeIncrement = (int)Math.Floor(DistanceToTarget / 10000.0f);

                            weaponFired = LinkedWeapons[loop].Fire();

                            if (weaponFired == true)
                            {
                                /// <summary>
                                /// Some weapons have multiple shots, but most will have just 1.
                                /// </summary>
                                for (int loop2 = 0; loop2 < LinkedWeapons[loop].beamDef.shotCount; loop2++)
                                {
                                    int Hit = RNG.Next(1, 100);

                                    /// <summary>
                                    /// Did the weapon hit?
                                    /// </summary>
                                    if (toHit >= Hit)
                                    {
                                        ushort ToDestroy;
                                        if (Target.missileGroup.missiles[0].missileDef.armor == 0)
                                            ToDestroy = 100;
                                        else
                                            ToDestroy = (ushort)(Math.Round((LinkedWeapons[loop].beamDef.damage[RangeIncrement] / (Target.missileGroup.missiles[0].missileDef.armor + LinkedWeapons[loop].beamDef.damage[RangeIncrement]))) * 100.0f);
                                        ushort DestChance = (ushort)RNG.Next(1, 100);


                                        /// <summary>
                                        /// Does the weapon have the power to make a kill?
                                        /// </summary>
                                        if (ToDestroy >= DestChance)
                                        {
                                            String WeaponFireS = String.Format("{0} and destroyed a missile at {1}% tohit", LinkedWeapons[loop].Name, toHit);

                                            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringHit, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                                 GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                            FiringShip.ShipsFaction.MessageLog.Add(NMsg);

                                            /// <summary>
                                            /// Set destruction of targeted missile here. This is used by sim entity to determine how to handle missile group cleanup.
                                            /// </summary>
                                            Target.missileGroup.missilesDestroyed = Target.missileGroup.missilesDestroyed + 1;

                                            if (Target.missileGroup.missilesDestroyed  == Target.missileGroup.missiles.Count)
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            String WeaponFireS = String.Format("{0} and failed to destroyed a missile at {1}% tohit", LinkedWeapons[loop].Name, toHit);

                                            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringHit, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                                 GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                            FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                                        }
                                    }
                                    else
                                    {
                                        String WeaponFireS = String.Format("{0} missed at {1}% tohit", LinkedWeapons[loop].Name, toHit);

                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringMissed, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                             GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                        FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                                    }
                                }//end for shot count
                            }
                            else if (LinkedWeapons[loop].isDestroyed == false)
                            {
                                String WeaponFireS = String.Format("{0} Recharging {1}/{2} Power", LinkedWeapons[loop].Name, LinkedWeapons[loop].currentCapacitor, LinkedWeapons[loop].beamDef.weaponCapacitor);

                                MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringRecharging, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                     GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                            }
                        }//end if in range and weapon can fire

                        if (Target.missileGroup.missilesDestroyed == Target.missileGroup.missiles.Count)
                        {
                            break;
                        }

                    }// end for linked weapons

                    
                    return weaponFired;
                }
                else
                {
                    /// <summary>
                    /// Planet section eventually goes here.
                    /// </summary>
                    return weaponFired;
                }
            }       
        }


        /// <summary>
        /// Get the accuracy at which this BFC can fire upon its target.
        /// </summary>
        /// <param name="RangeIncrement">Distance to target</param>
        /// <returns>Firing accuracy.</returns>
        private float GetFiringAccuracy(int RangeIncrement, int track)
        {
            float FireAccuracy = BeamFireControlDef.rangeAccuracyTable[RangeIncrement];

            /// <summary>
            /// Get Target_CurrentSpeed for accuracy calculations. Planets do not move so this can remain at 0 for that.
            int Target_CurrentSpeed = 0;
            switch (Target.targetType)
            {
                case StarSystemEntityType.TaskGroup:
                    Target_CurrentSpeed = Target.ship.CurrentSpeed;
                    break;
                case StarSystemEntityType.Missile:
                    Target_CurrentSpeed = (int)Math.Round(Target.missileGroup.missiles[0].missileDef.maxSpeed);
                    break;
            }


            /// <summary>
            /// 100% accuracy due to tracking at this speed.
            /// </summary>
            if ((float)Target_CurrentSpeed > BeamFireControlDef.trackingAccuracyTable[99])
            {
                if ((float)Target_CurrentSpeed >= BeamFireControlDef.trackingAccuracyTable[0])
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
                        if ((float)Target_CurrentSpeed > BeamFireControlDef.trackingAccuracyTable[Base])
                        {
                            if (Cur == 1)
                            {
                                float t1 = (float)Target_CurrentSpeed - BeamFireControlDef.trackingAccuracyTable[Base];
                                float t2 = (float)Target_CurrentSpeed - BeamFireControlDef.trackingAccuracyTable[Base - 1];
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
                        else if ((float)Target_CurrentSpeed < BeamFireControlDef.trackingAccuracyTable[Base])
                        {
                            if (Cur == 1)
                            {
                                float t1 = BeamFireControlDef.trackingAccuracyTable[Base] - (float)Target_CurrentSpeed;
                                float t2 = BeamFireControlDef.trackingAccuracyTable[Base + 1] - (float)Target_CurrentSpeed;
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
            if (track < BeamFireControlDef.tracking)
            {
                float fireAccMod = (float)track / BeamFireControlDef.tracking;

                FireAccuracy = FireAccuracy * fireAccMod;
            }

            return FireAccuracy;
        }
    }
}
