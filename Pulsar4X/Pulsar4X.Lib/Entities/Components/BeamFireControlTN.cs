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
        private int m_oRangeTech;
        public int rangeTech
        {
            get { return m_oRangeTech; }
        }
        /// <summary>
        /// Tracking tech level
        /// </summary>
        private int m_oTrackTech;
        private int trackTech
        {
            get { return m_oTrackTech; }
        }

        /// <summary>
        /// Base range tech this BFC is built with.
        /// </summary>
        private float m_oRangeBase;
        public float rangeBase
        {
            get { return m_oRangeBase; }
        }

        /// <summary>
        /// Base tracking tech this BFC is built with.
        /// </summary>
        private float m_oTrackBase;
        public float trackBase
        {
            get { return m_oTrackBase; }
        }

        /// <summary>
        /// Size and Range adjusting modification to RangeBase.
        /// </summary>
        private float m_oRangeMod;
        public float rangeMod
        {
            get { return m_oRangeMod; }
        }

        /// <summary>
        /// Size and Tracking adjustment modification to TrackBase.
        /// </summary>
        private float m_oTrackMod;
        public float trackMod
        {
            get { return m_oTrackMod; }
        }

        /// <summary>
        /// Overall range for 50% accuracy.
        /// </summary>
        private float m_oRange;
        public float range
        {
            get { return m_oRange; }
        }

        /// <summary>
        /// Overall Tracking for 100% accuracy.
        /// </summary>
        private float m_oTracking;
        public float tracking
        {
            get { return m_oTracking; }
        }

        /// <summary>
        /// Electronic hardening 1.0 to 0.1.
        /// </summary>
        private float m_oHardening;
        public float hardening
        {
            get { return m_oHardening; }
        }

        /// <summary>
        /// PDCs get 50% bonus to range. Mutually exclusive with IsFighter.
        /// </summary>
        private bool m_oIsPDC;
        public bool isPDC
        {
            get { return m_oIsPDC; }
        }

        /// <summary>
        /// fighters get 200% bonus to tracking. mutually exclusive with IsPDC.
        /// </summary>
        private bool m_oIsFighter;
        public bool isFighter
        {
            get { return m_oIsFighter; }
        }

        /// <summary>
        /// List of accuracy in 1KM increments. 0 = 100, Range = 50, 2xRange = 0.
        /// What is more, from 0-10k is point blank accuracy, it is not 100%, but 10k-MaxRange/MaxRange percent, the next is 10-20k, and so on.
        /// </summary>
        private BindingList<float> m_lRangeAccuracyTable;
        public BindingList<float> rangeAccuracyTable
        {
            get { return m_lRangeAccuracyTable; }
        }

        /// <summary>
        /// tracking accuracy lookup table, this one works a little differently from the range accuracy table, this one will be 0(1% accuracy) to 99(100% accuracy).
        /// </summary>
        private BindingList<float> m_lTrackingAccuracyTable;
        public BindingList<float> trackingAccuracyTable
        {
            get { return m_lTrackingAccuracyTable; }
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

            m_oRangeTech = techRange;
            m_oTrackTech = techTrack;

            m_oRangeBase = Constants.BFCTN.BeamFireControlRange[m_oRangeTech] * 1000.0f;
            m_oTrackBase = Constants.BFCTN.BeamFireControlTracking[m_oTrackTech];

            m_oRangeMod = ModRange;
            m_oTrackMod = ModTracking;

            size = size * ModRange * ModTracking;

            if (size >= 1.0f)
                htk = 1;
            else
                htk = 0;

            m_oIsPDC = PDC;
            m_oIsFighter = Fighter;

            m_oHardening = hard;

            if (m_oIsPDC == true && m_oIsFighter == true)
            {
                m_oIsFighter = false;
            }

            if (m_oIsPDC == true)
            {
                m_oRangeBase = m_oRangeBase * 1.5f;
            }

            if (m_oIsFighter == true)
            {
                m_oTrackBase = m_oTrackBase * 4.0f;
            }

            m_oRange = m_oRangeBase * m_oRangeMod;
            m_oTracking = m_oTrackBase * m_oTrackMod;

            crew = (byte)(size * 2.0f);

            /// <summary>
            /// Not the exact cost calculation, or close for that matter. I think cost is just a data base entry.
            /// </summary>

            float RTechAdjust = 10666.6667f / (float)(m_oRangeTech + 1);
            float TTechAdjust = 1666.6667f / (float)(m_oTrackTech + 1);


            float R = (m_oRangeBase / RTechAdjust);
            float T = (m_oTrackBase / TTechAdjust);
            float res = (float)Math.Round((R + T) * size);
            res = (float)Math.Round(res + ((float)res * 0.25f * (float)(hardTech - 1)));

            cost = (decimal)(res);

            minerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                minerialsCost[mineralIterator] = 0;
            }
            minerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = cost;

            /// <summary>
            /// Range * 2 / 10000.0
            /// </summary>
            int RangeIncrement = (int)Math.Floor(m_oRange / 5000.0f);

            m_lRangeAccuracyTable = new BindingList<float>();

            /// <summary>
            /// range is in 10k increments.
            /// </summary>
            for (int RangeIncrementIterator = 1; RangeIncrementIterator < RangeIncrement; RangeIncrementIterator++)
            {
                float MaxRange = RangeIncrementIterator * 2.0f;
                float CurRange = RangeIncrementIterator * 10000.0f;

                float Accuracy = (MaxRange - CurRange) / MaxRange;
                m_lRangeAccuracyTable.Add(Accuracy);
            }

            m_lRangeAccuracyTable.Add(0.0f);

            m_lTrackingAccuracyTable = new BindingList<float>();

            /// <summary>
            /// tracking is a percentage from 0 to 100.
            /// </summary>
            for (int TrackIncrementIterator = 0; TrackIncrementIterator < 100; TrackIncrementIterator++)
            {

                float Factor = (float)((float)TrackIncrementIterator + 1.0f) / 100.0f;
                float Accuracy = m_oTracking / Factor;
                m_lTrackingAccuracyTable.Add(Accuracy);
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
        private BeamFireControlDefTN m_oBeamFireControlDef;
        public BeamFireControlDefTN beamFireControlDef
        {
            get { return m_oBeamFireControlDef; }
        }

        /// <summary>
        /// ECCMs Linked to this BFC. Update DestroyComponents when eccm is added.
        /// </summary>

        /// <summary>
        /// Weapons linked to this BFC.
        /// </summary>
        private BindingList<BeamTN> m_lLinkedWeapons;
        public BindingList<BeamTN> linkedWeapons
        {
            get { return m_lLinkedWeapons; }
        }

        /// <summary>
        /// Additional linked turrets to this BFC.
        /// </summary>
        private BindingList<TurretTN> m_lLinkedTurrets;
        public BindingList<TurretTN> linkedTurrets
        {
            get { return m_lLinkedTurrets; }
        }

        /// <summary>
        /// Target Assigned to this BFC
        /// </summary>
        private TargetTN m_oTarget;
        public TargetTN target
        {
            get { return m_oTarget; }
            set { m_oTarget = value; }
        }

        /// <summary>
        /// Whether this FC is authorized to fire on its target.
        /// </summary>
        private bool m_oOpenFire;
        public bool openFire
        {
            get { return m_oOpenFire; }
            set { m_oOpenFire = value; }
        }

        /// <summary>
        /// Point defense state this BFC will fire to.
        /// </summary>
        private PointDefenseState m_oPDState;
        public PointDefenseState pDState
        {
            get { return m_oPDState; }
        }

        /// <summary>
        /// range at which area defense will engage targets
        /// </summary>
        private float m_oPDRange;
        public float pDRange
        {
            get { return m_oPDRange; }
        }

        /// <summary>
        /// Constructor for BFC components.
        /// </summary>
        /// <param name="definition">Definition of this component</param>
        public BeamFireControlTN(BeamFireControlDefTN definition)
        {
            m_oBeamFireControlDef = definition;
            isDestroyed = false;

            m_lLinkedWeapons = new BindingList<BeamTN>();
            m_lLinkedTurrets = new BindingList<TurretTN>();

            m_oTarget = null;
            m_oOpenFire = false;
            m_oPDState = PointDefenseState.None;
            m_oPDRange = 0;
        }

        /// <summary>
        /// Set the fire control to the desired point defense state.
        /// </summary>
        /// <param name="State">State the BFC is to be set to.</param>
        public void SetPointDefenseMode(PointDefenseState State)
        {
            if (State <= PointDefenseState.FinalDefensiveFireSelf)
                m_oPDState = State;
            else
            {
                /// <summary>
                /// Bad PDState for BFCs.
                /// </summary>
            }
        }

        /// <summary>
        /// Set the FC pd range that area defense will engage targets at.
        /// </summary>
        /// <param name="range">range to engage targets at.</param>
        public void SetPointDefenseRange(float range)
        {
            m_oPDRange = range;
        }

        /// <summary>
        /// Simple assignment of a ship as a target to this bfc.
        /// </summary>
        /// <param name="ShipTarget">Ship to be targeted.</param>
        public void assignTarget(ShipTN ShipTarget)
        {
            TargetTN NewShipTarget = new TargetTN(ShipTarget);
            m_oTarget = NewShipTarget;
        }

        /// <summary>
        /// Assignment of a missile group as a target.
        /// </summary>
        /// <param name="OrdGroupTarget">ordnance group to be targetted</param>
        public void assignTarget(OrdnanceGroupTN OrdGroupTarget)
        {
            TargetTN NewOrdTarget = new TargetTN(OrdGroupTarget);
            m_oTarget = NewOrdTarget;
        }

        /// <summary>
        /// assignment of a population as the target
        /// </summary>
        /// <param name="PopTarget">Population to be targetted</param>
        public void assignTarget(Population PopTarget)
        {
            TargetTN NewPopTarget = new TargetTN(PopTarget);
            m_oTarget = NewPopTarget;
        }

        /// <summary>
        /// Simple deassignment of target to this bfc.
        /// </summary>
        public void clearTarget()
        {
            m_oTarget = null;
        }

        /// <summary>
        /// Simple return of the target of this BFC.
        /// </summary>
        public TargetTN getTarget()
        {
            return m_oTarget;
        }

        /// <summary>
        /// Clears all weapon links.
        /// </summary>
        public void clearWeapons()
        {
            foreach (BeamTN LinkedWeapon in m_lLinkedWeapons)
            {
                LinkedWeapon.fireController = null;
            }
            m_lLinkedWeapons.Clear();

            foreach (TurretTN LinkedTurret in m_lLinkedTurrets)
            {
                LinkedTurret.fireController = null;
            }
            m_lLinkedTurrets.Clear();
        }

        /// <summary>
        /// Links a weapon to this fc
        /// </summary>
        /// <param name="beam">beam weapon to link</param>
        public void linkWeapon(BeamTN beam)
        {
            if (m_lLinkedWeapons.Contains(beam) == false)
            {
                m_lLinkedWeapons.Add(beam);

                beam.fireController = this;
            }
        }

        /// <summary>
        /// Links a weapon to this fc
        /// </summary>
        /// <param name="beam">beam weapon to link</param>
        public void linkWeapon(TurretTN beam)
        {
            if (m_lLinkedTurrets.Contains(beam) == false)
            {
                m_lLinkedTurrets.Add(beam);

                beam.fireController = this;
            }
        }

        /// <summary>
        /// removes a weapon from this FC
        /// </summary>
        /// <param name="beam">beamweapon to unlink</param>
        public void unlinkWeapon(BeamTN beam)
        {
            if (m_lLinkedWeapons.Contains(beam) == true)
            {
                m_lLinkedWeapons.Remove(beam);

                beam.fireController = null;
            }
        }

        /// <summary>
        /// removes a weapon from this FC
        /// </summary>
        /// <param name="beam">beamweapon to unlink</param>
        public void unlinkWeapon(TurretTN beam)
        {
            if (m_lLinkedTurrets.Contains(beam) == true)
            {
                m_lLinkedTurrets.Remove(beam);

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
            if (DistanceToTarget > m_oBeamFireControlDef.range || (m_lLinkedWeapons.Count == 0 && m_lLinkedTurrets.Count == 0) || isDestroyed == true)
            {
                if (DistanceToTarget > m_oBeamFireControlDef.range)
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
                float FireAccuracy = GetFiringAccuracy(RangeIncrement, track);


                /// <summary>
                /// Fire Accuracy is the likelyhood of a shot hitting from this FC at this point.
                /// ECM vs ECCM needs to be done around here as well.
                /// </summary>
                bool weaponFired = false;

                if (m_oTarget.targetType == StarSystemEntityType.TaskGroup)
                {

                    int toHit = (int)Math.Floor(FireAccuracy * 100.0f);
                    ushort Columns = m_oTarget.ship.ShipArmor.armorDef.cNum;

                    foreach (BeamTN LinkedWeapon in m_lLinkedWeapons)
                    {
                        if (LinkedWeapon.beamDef.range > DistanceToTarget && LinkedWeapon.readyToFire() == true)
                        {
                            RangeIncrement = (int)Math.Floor(DistanceToTarget / 10000.0f);

                            weaponFired = LinkedWeapon.Fire();

                            if (weaponFired == true)
                            {
                                for (int BeamShotIterator = 0; BeamShotIterator < LinkedWeapon.beamDef.shotCount; BeamShotIterator++)
                                {
                                    int Hit = RNG.Next(1, 100);

                                    if (toHit >= Hit)
                                    {
                                        String WeaponFireS = String.Format("{0} hit {1} damage at {2}% tohit", LinkedWeapon.Name, LinkedWeapon.beamDef.damage[RangeIncrement], toHit);

                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringHit, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                             GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                        FiringShip.ShipsFaction.MessageLog.Add(NMsg);


                                        ushort location = (ushort)RNG.Next(0, Columns);
                                        bool ShipDest = m_oTarget.ship.OnDamaged(LinkedWeapon.beamDef.damageType, LinkedWeapon.beamDef.damage[RangeIncrement], location, FiringShip);

                                        if (ShipDest == true)
                                        {
                                            m_oTarget = null;
                                            m_oOpenFire = false;
                                            return weaponFired;
                                        }
                                    }
                                    else
                                    {
                                        String WeaponFireS = String.Format("{0} missed at {1}% tohit", LinkedWeapon.Name, toHit);

                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringMissed, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                             GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                        FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                                    }
                                }
                            }
                            else if (LinkedWeapon.isDestroyed == false)
                            {
                                String WeaponFireS = String.Format("{0} Recharging {1}/{2} Power", LinkedWeapon.Name, LinkedWeapon.currentCapacitor, LinkedWeapon.beamDef.weaponCapacitor);

                                MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringRecharging, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                     GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                            }
                        }
                    }

                    foreach (TurretTN LinkedTurret in m_lLinkedTurrets)
                    {
                        /// <summary>
                        /// turrets have changed tracking and therefore tohit from regular beams.
                        /// </summary>
                        FireAccuracy = GetFiringAccuracy(RangeIncrement, LinkedTurret.turretDef.tracking);
                        toHit = (int)Math.Floor(FireAccuracy * 100.0f);

                        if (LinkedTurret.turretDef.baseBeamWeapon.range > DistanceToTarget && LinkedTurret.readyToFire() == true)
                        {
                            RangeIncrement = (int)Math.Floor(DistanceToTarget / 10000.0f);

                            weaponFired = LinkedTurret.Fire();

                            if (weaponFired == true)
                            {
                                for (int TurretShotIterator = 0; TurretShotIterator < LinkedTurret.turretDef.totalShotCount; TurretShotIterator++)
                                {
                                    int Hit = RNG.Next(1, 100);

                                    if (toHit >= Hit)
                                    {
                                        String WeaponFireS = String.Format("{0} hit {1} damage at {2}% tohit", LinkedTurret.Name, LinkedTurret.turretDef.baseBeamWeapon.damage[RangeIncrement], toHit);

                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringHit, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                             GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                        FiringShip.ShipsFaction.MessageLog.Add(NMsg);


                                        ushort location = (ushort)RNG.Next(0, Columns);
                                        bool ShipDest = m_oTarget.ship.OnDamaged(LinkedTurret.turretDef.baseBeamWeapon.damageType, LinkedTurret.turretDef.baseBeamWeapon.damage[RangeIncrement], location, FiringShip);

                                        if (ShipDest == true)
                                        {
                                            m_oTarget = null;
                                            m_oOpenFire = false;
                                            return weaponFired;
                                        }
                                    }
                                    else
                                    {
                                        String WeaponFireS = String.Format("{0} missed at {1}% tohit", LinkedTurret.Name, toHit);

                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringMissed, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                             GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                        FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                                    }
                                }
                            }
                            else if (LinkedTurret.isDestroyed == false)
                            {
                                String WeaponFireS = String.Format("{0} Recharging {1}/{2} Power", LinkedTurret.Name, LinkedTurret.currentCapacitor,
                                                                                                  (LinkedTurret.turretDef.baseBeamWeapon.weaponCapacitor * LinkedTurret.turretDef.multiplier));

                                MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringRecharging, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                     GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                            }
                        }
                    }

                    return weaponFired;
                }
                else if (m_oTarget.targetType == StarSystemEntityType.Missile)
                {
                    /// <summary>
                    /// this is for beam targetting on missiles.
                    /// </summary>
                    int toHit = (int)Math.Floor(FireAccuracy * 100.0f);

                    /// <summary>
                    /// have all targeted missiles been destroyed.
                    /// </summary>
                    bool noMissilesLeft = false;

                    /// <summary>
                    /// For all weapons linked to this BFC
                    /// </summary>
                    foreach (BeamTN LinkedWeapon in m_lLinkedWeapons)
                    {
                        /// <summary>
                        /// if range > distance and the weapon is ready to fire.
                        /// </summary>
                        if (LinkedWeapon.beamDef.range > DistanceToTarget && LinkedWeapon.readyToFire() == true)
                        {
                            RangeIncrement = (int)Math.Floor(DistanceToTarget / 10000.0f);

                            weaponFired = LinkedWeapon.Fire();

                            if (weaponFired == true)
                            {
                                /// <summary>
                                /// Some weapons have multiple shots, but most will have just 1.
                                /// </summary>
                                for (int BeamShotIterator = 0; BeamShotIterator < LinkedWeapon.beamDef.shotCount; BeamShotIterator++)
                                {
                                    int Hit = RNG.Next(1, 100);

                                    /// <summary>
                                    /// Did the weapon hit?
                                    /// </summary>
                                    if (toHit >= Hit)
                                    {
                                        ushort ToDestroy;
                                        if (m_oTarget.missileGroup.missiles[0].missileDef.armor == 0)
                                            ToDestroy = 100;
                                        else
                                            ToDestroy = (ushort)(Math.Round((LinkedWeapon.beamDef.damage[RangeIncrement] / (m_oTarget.missileGroup.missiles[0].missileDef.armor + LinkedWeapon.beamDef.damage[RangeIncrement]))) * 100.0f);
                                        ushort DestChance = (ushort)RNG.Next(1, 100);


                                        /// <summary>
                                        /// Does the weapon have the power to make a kill?
                                        /// </summary>
                                        if (ToDestroy >= DestChance)
                                        {
                                            String WeaponFireS = String.Format("{0} and destroyed a missile at {1}% tohit", LinkedWeapon.Name, toHit);

                                            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringHit, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                                 GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                            FiringShip.ShipsFaction.MessageLog.Add(NMsg);

                                            /// <summary>
                                            /// Set destruction of targeted missile here. This is used by sim entity to determine how to handle missile group cleanup.
                                            /// </summary>
                                            m_oTarget.missileGroup.missilesDestroyed = m_oTarget.missileGroup.missilesDestroyed + 1;

                                            if (m_oTarget.missileGroup.missilesDestroyed == m_oTarget.missileGroup.missiles.Count)
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            String WeaponFireS = String.Format("{0} and failed to destroyed a missile at {1}% tohit", LinkedWeapon.Name, toHit);

                                            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringHit, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                                 GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                            FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                                        }
                                    }
                                    else
                                    {
                                        String WeaponFireS = String.Format("{0} missed at {1}% tohit", LinkedWeapon.Name, toHit);

                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringMissed, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                             GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                        FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                                    }
                                }//end for shot count
                            }
                            else if (LinkedWeapon.isDestroyed == false)
                            {
                                String WeaponFireS = String.Format("{0} Recharging {1}/{2} Power", LinkedWeapon.Name, LinkedWeapon.currentCapacitor, LinkedWeapon.beamDef.weaponCapacitor);

                                MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringRecharging, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                     GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                            }
                        }//end if in range and weapon can fire

                        if (m_oTarget.missileGroup.missilesDestroyed == m_oTarget.missileGroup.missiles.Count)
                        {
                            noMissilesLeft = true;
                            break;
                        }

                    }// end for linked weapons

                    if (noMissilesLeft == false)
                    {
                        foreach (TurretTN LinkedTurret in m_lLinkedTurrets) //(int loop = 0; loop < LinkedTurrets.Count; loop++)
                        {
                            FireAccuracy = GetFiringAccuracy(RangeIncrement, LinkedTurret.turretDef.tracking);
                            toHit = (int)Math.Floor(FireAccuracy * 100.0f);

                            if (LinkedTurret.turretDef.baseBeamWeapon.range > DistanceToTarget && LinkedTurret.readyToFire() == true)
                            {
                                RangeIncrement = (int)Math.Floor(DistanceToTarget / 10000.0f);

                                weaponFired = LinkedTurret.Fire();

                                if (weaponFired == true)
                                {
                                    /// <summary>
                                    /// Some weapons have multiple shots, but most will have just 1.
                                    /// </summary>
                                    for (int TurretShotIterator = 0; TurretShotIterator < LinkedTurret.turretDef.totalShotCount; TurretShotIterator++)
                                    {
                                        int Hit = RNG.Next(1, 100);

                                        /// <summary>
                                        /// Did the weapon hit?
                                        /// </summary>
                                        if (toHit >= Hit)
                                        {
                                            /// <summary>
                                            /// Did the weapon destroy its target?
                                            /// </summary>
                                            ushort ToDestroy;
                                            if (m_oTarget.missileGroup.missiles[0].missileDef.armor == 0)
                                                ToDestroy = 100;
                                            else
                                                ToDestroy = (ushort)(Math.Round((LinkedTurret.turretDef.baseBeamWeapon.damage[RangeIncrement] / (m_oTarget.missileGroup.missiles[0].missileDef.armor + LinkedTurret.turretDef.baseBeamWeapon.damage[RangeIncrement]))) * 100.0f);
                                            ushort DestChance = (ushort)RNG.Next(1, 100);

                                            /// <summary>
                                            /// Does the weapon have the power to make a kill?
                                            /// </summary>
                                            if (ToDestroy >= DestChance)
                                            {
                                                String WeaponFireS = String.Format("{0} and destroyed a missile at {1}% tohit", LinkedTurret.Name, toHit);

                                                MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringHit, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                                     GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                                FiringShip.ShipsFaction.MessageLog.Add(NMsg);

                                                /// <summary>
                                                /// Set destruction of targeted missile here. This is used by sim entity to determine how to handle missile group cleanup.
                                                /// </summary>
                                                m_oTarget.missileGroup.missilesDestroyed = m_oTarget.missileGroup.missilesDestroyed + 1;

                                                if (m_oTarget.missileGroup.missilesDestroyed == m_oTarget.missileGroup.missiles.Count)
                                                {
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                String WeaponFireS = String.Format("{0} and failed to destroyed a missile at {1}% tohit", LinkedTurret.Name, toHit);

                                                MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringHit, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                                     GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                                FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                                            }
                                        }
                                        else
                                        {
                                            String WeaponFireS = String.Format("{0} missed at {1}% tohit", LinkedTurret.Name, toHit);

                                            MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.FiringMissed, FiringShip.ShipsTaskGroup.Contact.CurrentSystem, FiringShip.ShipsTaskGroup.Contact,
                                                                                 GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), WeaponFireS);

                                            FiringShip.ShipsFaction.MessageLog.Add(NMsg);
                                        }

                                    }
                                }//end if weapon fired
                            }//end if in range and can fire

                            if (m_oTarget.missileGroup.missilesDestroyed == m_oTarget.missileGroup.missiles.Count)
                            {
                                /// <summary>
                                /// This is cargo culting and this variable does not need to be set here, but for completeness sake I'll include it.
                                /// </summary>
                                noMissilesLeft = true;

                                break;
                            }
                        }//end for linkedturrets.
                    }//end if noMissilesLeft = true


                    return weaponFired;
                }
                else
                {
#warning Beam Fire control on planet/population section not implemented.
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
        /// <param name="track">Tracking capability of beam weapon that accuracy is desired for.</param>
        /// <param name="Override">For FCs in PD mode, there will be no target, they should use override instead.</param>
        /// <returns>Firing accuracy.</returns>
        private float GetFiringAccuracy(int RangeIncrement, int track, TargetTN Override = null)
        {
            float FireAccuracy = m_oBeamFireControlDef.rangeAccuracyTable[RangeIncrement];

            TargetTN MyTarget = m_oTarget;

            if (MyTarget == null && Override != null)
            {
                MyTarget = Override;
            }

            /// <summary>
            /// Get Target_CurrentSpeed for accuracy calculations. Planets do not move so this can remain at 0 for that.
            int Target_CurrentSpeed = 0;
            switch (MyTarget.targetType)
            {
                case StarSystemEntityType.TaskGroup:
                    Target_CurrentSpeed = MyTarget.ship.CurrentSpeed;
                    break;
                case StarSystemEntityType.Missile:
                    Target_CurrentSpeed = (int)Math.Round(MyTarget.missileGroup.missiles[0].missileDef.maxSpeed);
                    break;
            }


            /// <summary>
            /// 100% accuracy due to tracking at this speed if this condition fails and target speed is less than or equal to tracking[99]
            /// </summary>
            if ((float)Target_CurrentSpeed > m_oBeamFireControlDef.trackingAccuracyTable[99])
            {
                /// <summary>
                /// 1% chance to hit  thanks to tracking being totally out classed.
                /// </summary>
                if ((float)Target_CurrentSpeed >= m_oBeamFireControlDef.trackingAccuracyTable[0])
                {
                    FireAccuracy = FireAccuracy * 0.01f;
                }
                else
                {
                    bool done = false;
                    int Base = 50;
                    int Cur = 50;
                    /// <summary>
                    /// Binary search through the tracking accuracy table. Base is base accuracy of 50, and Cur is where we are in the binary search.
                    /// The tracking accuracy table has 100 entries.
                    /// </summary>

                    while (!done)
                    {
                        /// <summary>
                        /// divide Cur by 2 as per binary search.
                        /// </summary>
                        Cur = Cur / 2;

                        /// <summary>
                        /// if the targets speed is greater than tracking at base go to the next condition. basically these two advance base forward by cur until Cur is reduced to 1 which 
                        /// indicates that there is no more granularity to be observed.
                        /// </summary>
                        if ((float)Target_CurrentSpeed > m_oBeamFireControlDef.trackingAccuracyTable[Base])
                        {
                            /// <summary>
                            /// If Cur is equal to one then either Base or Base - 1 is the accuracy we want. If its not one then it can be further subdivided.
                            /// </summary>
                            if (Cur == 1)
                            {
                                /// <summary>
                                /// Is the speed of the target closer to tracking[base] or tracking[base-1]?
                                /// </summary>
                                float t1 = (float)Target_CurrentSpeed - m_oBeamFireControlDef.trackingAccuracyTable[Base];
                                float t2 = (float)Target_CurrentSpeed - m_oBeamFireControlDef.trackingAccuracyTable[Base - 1];
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
                        else if ((float)Target_CurrentSpeed < m_oBeamFireControlDef.trackingAccuracyTable[Base])
                        {
                            if (Cur == 1)
                            {
                                float t1 = m_oBeamFireControlDef.trackingAccuracyTable[Base] - (float)Target_CurrentSpeed;
                                float t2 = m_oBeamFireControlDef.trackingAccuracyTable[Base + 1] - (float)Target_CurrentSpeed;
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
            if (track < m_oBeamFireControlDef.tracking)
            {
                float fireAccMod = (float)track / m_oBeamFireControlDef.tracking;

                FireAccuracy = FireAccuracy * fireAccMod;
            }

            return FireAccuracy;
        }


        /// <summary>
        /// This function calculates whether a given BFC can intercept a missile
        /// </summary>
        /// <param name="RNG">RNG to use, should be the global one in _SE_</param>
        /// <param name="IncrementDistance">Range to the target missile</param>
        /// <param name="Ordnance">Ordnance we want to shoot at.</param>
        /// <param name="ShipFaction">Faction of the ship this BFC is on.</param>
        /// <param name="Contact">Contact of the taskgroup this BFC is in.</param>
        /// <param name="ShipOn">Ship this BFC is on.</param>
        /// <param name="WeaponsFired">Whether or not a weapon was fired. this is for the recharge list further up</param>
        /// <returns>whether the missile was intercepted.</returns>
        public bool InterceptTarget(Random RNG, int IncrementDistance, OrdnanceTN Ordnance, Faction ShipFaction, SystemContact Contact, ShipTN ShipOn, out bool WeaponsFired)
        {
            WeaponsFired = false;

            float ShipSpeed = ShipOn.CurrentSpeed;

            float track = (float)ShipFaction.BaseTracking;
            if (ShipSpeed > track)
                track = ShipSpeed;
            if (m_oBeamFireControlDef.tracking < track)
                track = m_oBeamFireControlDef.tracking;

            /// <summary>
            /// Throwaway target for point defense purposes.
            /// </summary>
            TargetTN OverrideTarget = new TargetTN(Ordnance.missileGroup);

            float Acc = GetFiringAccuracy(IncrementDistance, (int)track, OverrideTarget);
            int toHit = (int)Math.Floor(Acc * 100.0f);
            int range = (IncrementDistance + 1) * 10000;
            String Range = range.ToString("#,###0");

            foreach (BeamTN LinkedWeapon in m_lLinkedWeapons)
            {
                /// <summary>
                /// Certain weapons will have already fired one or more of their shots, but may still have more available.
                /// </summary>
                bool AcceptPartialFire = (LinkedWeapon.beamDef.componentType == ComponentTypeTN.Rail || LinkedWeapon.beamDef.componentType == ComponentTypeTN.AdvRail ||
                        LinkedWeapon.beamDef.componentType == ComponentTypeTN.Gauss) && (LinkedWeapon.shotsExpended < LinkedWeapon.beamDef.shotCount);

                if (LinkedWeapon.readyToFire() == true || AcceptPartialFire == true)
                {
                    if (LinkedWeapon.beamDef.componentType == ComponentTypeTN.Rail || LinkedWeapon.beamDef.componentType == ComponentTypeTN.AdvRail ||
                        LinkedWeapon.beamDef.componentType == ComponentTypeTN.Gauss)
                    {

                        WeaponsFired = LinkedWeapon.Fire();

                        /// <summary>
                        /// multi-hit weapons will be a little wierd as far as PD goes.
                        /// </summary>
                        if (WeaponsFired == false && AcceptPartialFire == true)
                            WeaponsFired = true;


                        int expended = LinkedWeapon.shotsExpended;
                        int ShotCount = LinkedWeapon.beamDef.shotCount;

                        for (int BeamShotIterator = expended; BeamShotIterator < ShotCount; BeamShotIterator++)
                        {
                            ushort Hit = (ushort)RNG.Next(1, 100);
                            LinkedWeapon.shotsExpended++;

                            if (toHit >= Hit)
                            {
                                String Entry = String.Format("{0} Fired at {1} km and hit.", LinkedWeapon.Name, Range);
                                MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.FiringHit, Contact.CurrentSystem, Contact, GameState.Instance.GameDateTime,
                                                                   (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                                ShipFaction.MessageLog.Add(Msg);
                                return true;
                            }
                            else
                            {
                                String Entry = String.Format("{0} Fired at {1} km and missed.", LinkedWeapon.Name, Range);
                                MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.FiringHit, Contact.CurrentSystem, Contact, GameState.Instance.GameDateTime,
                                                                   (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                                ShipFaction.MessageLog.Add(Msg);
                            }
                        }

                    }
                    else
                    {
                        ushort Hit = (ushort)RNG.Next(1, 100);

                        WeaponsFired = LinkedWeapon.Fire();

                        if (toHit >= Hit)
                        {
                            String Entry = String.Format("{0} Fired at {1} km and hit.", LinkedWeapon.Name, Range);
                            MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.FiringHit, Contact.CurrentSystem, Contact, GameState.Instance.GameDateTime,
                                                               (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                            ShipFaction.MessageLog.Add(Msg);
                            return true;
                        }
                        else
                        {
                            String Entry = String.Format("{0} Fired at {1} km and missed.", LinkedWeapon.Name, Range);
                            MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.FiringHit, Contact.CurrentSystem, Contact, GameState.Instance.GameDateTime,
                                                               (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                            ShipFaction.MessageLog.Add(Msg);
                        }
                    }
                }
            }

            foreach (TurretTN LinkedTurret in m_lLinkedTurrets)
            {
                /// <summary>
                /// Double, triple, and quad turrets have multiple shots.
                /// </summary>
                bool AcceptPartialFire = (LinkedTurret.shotsExpended < LinkedTurret.turretDef.totalShotCount);
                if (LinkedTurret.readyToFire() == true || AcceptPartialFire == true)
                {
                    WeaponsFired = LinkedTurret.Fire();

                    /// <summary>
                    /// multi-hit weapons will be a little wierd as far as PD goes.
                    /// </summary>
                    if (WeaponsFired == false && AcceptPartialFire == true)
                        WeaponsFired = true;

                    int expended = LinkedTurret.shotsExpended;
                    int ShotCount = LinkedTurret.turretDef.totalShotCount;

                    for (int TurretShotIterator = expended; TurretShotIterator < ShotCount; TurretShotIterator++)
                    {
                        ushort Hit = (ushort)RNG.Next(1, 100);
                        LinkedTurret.shotsExpended++;

                        if (toHit >= Hit)
                        {
                            String Entry = String.Format("{0} Fired at {1} km and hit.", LinkedTurret.Name, Range);
                            MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.FiringHit, Contact.CurrentSystem, Contact, GameState.Instance.GameDateTime,
                                                               (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                            ShipFaction.MessageLog.Add(Msg);
                            return true;
                        }
                        else
                        {
                            String Entry = String.Format("{0} Fired at {1} km and missed.", LinkedTurret.Name, Range);
                            MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.FiringHit, Contact.CurrentSystem, Contact, GameState.Instance.GameDateTime,
                                                               (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                            ShipFaction.MessageLog.Add(Msg);
                        }
                    }
                }
            }

            return false;
        }
    }
}
