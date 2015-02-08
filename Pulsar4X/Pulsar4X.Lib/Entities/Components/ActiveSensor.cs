using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public class ActiveSensorDefTN : ComponentDefTN
    {

        /// <summary>
        /// Sensor Strength component of range.
        /// </summary>
        private byte m_oActiveStrength;
        public byte activeStrength
        {
            get { return m_oActiveStrength; }
        }

        /// <summary>
        /// EM listening portion of sensor.
        /// </summary>
        private byte m_oEMRecv;
        public byte eMRecv
        {
            get { return m_oEMRecv; }
        }

        /// <summary>
        /// Sensor wavelength resolution. What size of ship this sensor is best suited to detecting.
        /// </summary>
        private ushort m_oResolution;
        public ushort resolution
        {
            get { return m_oResolution; }
        }

        /// <summary>
        /// Grav Pulse Signature/Strength
        /// </summary>
        private int m_oGPS;
        public int gps
        {
            get { return m_oGPS; }
        }

        /// <summary>
        /// Range at which sensor can detect craft of same HS as resolution. multiply this by 10000 to get value in km.
        /// </summary>
        private int m_oMaxRange;
        public int maxRange
        {
            get { return m_oMaxRange; }
        }

        /// <summary>
        /// Lookup table for ship resolutions
        /// </summary>
        private BindingList<int> m_lLookUpST;
        public BindingList<int> lookUpST
        {
            get { return m_lLookUpST; }
        }

        /// <summary>
        /// Lookup table for missile resolutions
        /// </summary>
        private BindingList<int> m_lLookUpMT;
        public BindingList<int> lookUpMT
        {
            get { return m_lLookUpMT; }
        }

        /// <summary>
        /// Is this a Missile fire control or an active sensor? false = sensor, true = MFC. MFCs have 3x the range of search sensors.
        /// </summary>
        private bool m_oIsMFC;
        public bool isMFC
        {
            get { return m_oIsMFC; }
        }

        /// <summary>
        /// Likelyhood of destruction from electronic(microwave) damage.
        /// </summary>
        private float m_oHardening;
        public float hardening
        {
            get { return m_oHardening; }
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
            Id = Guid.NewGuid();

            if (MFC == false)
                componentType = ComponentTypeTN.ActiveSensor;
            else if (MFC == true)
                componentType = ComponentTypeTN.MissileFireControl;

            /// <summary>
            /// basic sensor statistics.
            /// </summary>
            Name = desc;
            size = HS;
            m_oActiveStrength = actStr;
            m_oEMRecv = EMR;
            m_oResolution = Res;
            m_oIsMFC = MFC;
            m_oHardening = hard;

            /// <summary>
            /// Crew and cost are related to size, ActiveStrength, and hardening.
            /// </summary>
            crew = (byte)(size * 2.0);
            cost = (decimal)((size * (float)m_oActiveStrength) + ((size * (float)m_oActiveStrength) * 0.25f * (float)(hardTech - 1)));

            minerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                minerialsCost[mineralIterator] = 0;
            }
            minerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = cost;

            ///<summary>
            ///Small sensors are civilian, large are military.
            ///</summary>
            if (size <= 1.0)
                isMilitary = false;
            else
                isMilitary = true;

            ///<summary>
            ///HTK is either 1 or 0, because all sensors are very weak to damage, especially electronic damage.
            ///</summary>
            if (size >= 1.0)
                htk = 1;
            else
                htk = 0;

            ///<summary>
            ///GPS is the value that a ship's EM signature will be increased by when this sensor is active.
            ///</summary>
            m_oGPS = (int)((float)m_oActiveStrength * size * (float)m_oResolution);

            /// <summary>
            /// MaxRange omits a 10,000 km adjustment factor due to integer limitations.
            /// </summary>
            m_oMaxRange = (int)((float)m_oActiveStrength * size * (float)Math.Sqrt((double)m_oResolution) * (float)m_oEMRecv);

            if (m_oIsMFC == true)
            {
                m_oMaxRange = m_oMaxRange * 3;
                m_oGPS = 0;
                isMilitary = true;
            }

            m_lLookUpST = new BindingList<int>();
            m_lLookUpMT = new BindingList<int>();

            ///<summary>
            ///Initialize the ship lookup Table.
            ///</summary>
            for (int ShipResolution = 0; ShipResolution < Constants.ShipTN.ResolutionMax; ShipResolution++)
            {
                ///<summary>
                ///Sensor Resolution can't resolve this target at its MaxRange due to the target's smaller size
                ///</summary>
                if ((ShipResolution + 1) < m_oResolution)
                {
                    int NewRange = (int)((float)m_oMaxRange * (float)Math.Pow(((double)(ShipResolution + 1) / (float)m_oResolution), 2.0f));
                    m_lLookUpST.Add(NewRange);
                }
                else if ((ShipResolution + 1) >= m_oResolution)
                {
                    m_lLookUpST.Add(m_oMaxRange);
                }
            }

            ///<summary>
            ///Initialize the missile lookup Table.
            ///Missile size is in MSP, and no missile may be a fractional MSP in size. Each MSP is 0.05 HS in size.
            ///</summary>
            for (int MissileResolution = Constants.OrdnanceTN.MissileResolutionMinimum; MissileResolution < (Constants.OrdnanceTN.MissileResolutionMaximum + 1); MissileResolution++)
            {
#warning magic numbers related to missile resolution here
                ///<summary>
                ///Missile size never drops below 0.33, and missiles above 1 HS are atleast 1 HS. if I have to deal with 2HS missiles I can go to LookUpST
                ///</summary>
                if (MissileResolution == Constants.OrdnanceTN.MissileResolutionMinimum)
                {
                    int NewRange = (int)((float)m_oMaxRange * (float)Math.Pow((0.33 / (float)m_oResolution), 2.0f));
                    m_lLookUpMT.Add(NewRange);
                }
                else if (MissileResolution != Constants.OrdnanceTN.MissileResolutionMaximum)
                {
                    float msp = ((float)MissileResolution + 6.0f) * 0.05f;
                    int NewRange = (int)((float)m_oMaxRange * Math.Pow((msp / (float)m_oResolution), 2.0f));
                    m_lLookUpMT.Add(NewRange);
                }
                else if (MissileResolution == 14)
                {
                    lookUpMT.Add(m_lLookUpST[0]);//size 1 is size 1
                }
            }

            isSalvaged = false;
            isObsolete = false;
            isDivisible = false;

            isElectronic = true;
        }
        ///<summary>
        ///End ActiveSensorDefTN()
        ///</summary>

        /// <summary>
        /// Missile active sensor definition
        /// </summary>
        /// <param name="ActiveStr">Active strength value in total.</param>
        /// <param name="EMS">EM sensitivity same as regular active sensors.</param>
        /// <param name="Resolution">Active sensor resolution.</param>
        public ActiveSensorDefTN(float ActiveStr, byte EMS, ushort AResolution)
        {
            Id = Guid.NewGuid();
            componentType = ComponentTypeTN.TypeCount;
            m_oEMRecv = EMS;

            m_oResolution = AResolution;

            Name = "MissileActive";

            m_oGPS = (int)(m_oActiveStrength * m_oResolution);

            m_oMaxRange = (int)((float)ActiveStr * EMS * (float)Math.Sqrt((double)m_oResolution));

            m_lLookUpST = new BindingList<int>();
            m_lLookUpMT = new BindingList<int>();

            ///<summary>
            ///Initialize the ship lookup Table.
            ///</summary>
            for (int ShipResolution = 0; ShipResolution < Constants.ShipTN.ResolutionMax; ShipResolution++)
            {
                ///<summary>
                ///Sensor Resolution can't resolve this target at its MaxRange due to the target's smaller size
                ///</summary>
                if ((ShipResolution + 1) < m_oResolution)
                {
                    int NewRange = (int)((float)m_oMaxRange * (float)Math.Pow(((double)(ShipResolution + 1) / (float)m_oResolution), 2.0f));
                    m_lLookUpST.Add(NewRange);
                }
                else if ((ShipResolution + 1) >= m_oResolution)
                {
                    m_lLookUpST.Add(m_oMaxRange);
                }
            }

            ///<summary>
            ///Initialize the missile lookup Table.
            ///Missile size is in MSP, and no missile may be a fractional MSP in size. Each MSP is 0.05 HS in size.
            ///</summary>
            for (int MissileResolution = 0; MissileResolution < (Constants.OrdnanceTN.MissileResolutionMaximum + 1); MissileResolution++)
            {
                ///<summary>
                ///Missile size never drops below 0.33, and missiles above 1 HS are atleast 1 HS. if I have to deal with 2HS missiles I can go to LookUpST
                ///</summary>
                if (MissileResolution == Constants.OrdnanceTN.MissileResolutionMinimum)
                {
                    int NewRange = (int)((float)m_oMaxRange * (float)Math.Pow((0.33 / (float)m_oResolution), 2.0f));
                    m_lLookUpMT.Add(NewRange);
                }
                else if (MissileResolution != Constants.OrdnanceTN.MissileResolutionMaximum)
                {
                    float msp = ((float)MissileResolution + 6.0f) * 0.05f;
                    int NewRange = (int)((float)m_oMaxRange * Math.Pow((msp / (float)m_oResolution), 2.0f));
                    m_lLookUpMT.Add(NewRange);
                }
                else if (MissileResolution == Constants.OrdnanceTN.MissileResolutionMaximum)
                {
                    lookUpMT.Add(m_lLookUpST[0]);//size 1 is size 1
                }
            }


            m_oActiveStrength = 0;
            m_oHardening = 0;
            m_oIsMFC = false;
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
            if ((TCS > (Constants.ShipTN.ResolutionMax - 1) || TCS < 0) || (MSP > Constants.OrdnanceTN.MissileResolutionMaximum || MSP < -1))
            {
                return -1;
            }


            int DetRange;
            if (MSP == -1)
            {
                DetRange = m_lLookUpST[TCS];
            }
            else
            {
                DetRange = m_lLookUpMT[MSP];
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
    public class ActiveSensorTN : ComponentTN
    {
        /// <summary>
        /// What statistics define this sensor?
        /// </summary>
        private ActiveSensorDefTN m_oASensorDef;
        public ActiveSensorDefTN aSensorDef
        {
            get { return m_oASensorDef; }
        }

        /// <summary>
        /// Is this sensor active and thus both searching, and emitting an EM signature?
        /// </summary>
        private bool m_oIsActive;
        public bool isActive
        {
            get { return m_oIsActive; }
            set { m_oIsActive = value; }
        }


        /// <summary>
        /// The active sensor component itself. It is initialized to not destroyed, and not active.
        /// </summary>
        /// <param name="define">Definition for the sensor.</param>
        public ActiveSensorTN(ActiveSensorDefTN define)
        {
            m_oASensorDef = define;
            isDestroyed = false;
            m_oIsActive = false;
        }
    }
    /// <summary>
    ///End ActiveSensorTN
    /// </summary>


    public class MissileFireControlTN : ComponentTN
    {
        /// <summary>
        /// Missile Fire Controls are basically active sensors, this definition has the MFC range data for this sensor.
        /// </summary>
        private ActiveSensorDefTN m_oMFCSensorDef;
        public ActiveSensorDefTN mFCSensorDef
        {
            get { return m_oMFCSensorDef; }
        }

        /// <summary>
        /// ECCMs Linked to this BFC. Update DestroyComponents when eccm is added.
        /// </summary>

        /// <summary>
        /// Weapons linked to this MFC.
        /// </summary>
        private BindingList<MissileLauncherTN> m_lLinkedWeapons;
        public BindingList<MissileLauncherTN> linkedWeapons
        {
            get { return m_lLinkedWeapons; }
        }

        /// <summary>
        /// Target Assigned to this MFC
        /// </summary>
        private TargetTN m_oTarget;
        public TargetTN target
        {
            get { return m_oTarget; }
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
        /// Ordnance in fly but still connected to this MFC.
        /// </summary>
        private BindingList<OrdnanceGroupTN> m_lMissilesInFlight;
        public BindingList<OrdnanceGroupTN> missilesInFlight
        {
            get { return m_lMissilesInFlight; }
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
        /// ECCM needs to be handled eventually
        /// </summary>

        /// <summary>
        /// Constructor for Missile Fire Controls
        /// </summary>
        /// <param name="MFCDef">MFC definition.</param>
        public MissileFireControlTN(ActiveSensorDefTN MFCDef)
        {
            m_oMFCSensorDef = MFCDef;
            isDestroyed = false;

            m_lLinkedWeapons = new BindingList<MissileLauncherTN>();

            m_lMissilesInFlight = new BindingList<OrdnanceGroupTN>();

            m_oOpenFire = false;
            m_oTarget = null;
            m_oPDState = PointDefenseState.None;
            m_oPDRange = 0;
        }

        /// <summary>
        /// Set the fire control to the desired point defense state.
        /// </summary>
        /// <param name="State">State the MFC is to be set to.</param>
        public void SetPointDefenseMode(PointDefenseState State)
        {
            m_oPDState = State;
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
        /// Simple assignment of a ship as a target to this mfc.
        /// </summary>
        /// <param name="ShipTarget">Ship to be targeted.</param>
        public void assignTarget(ShipTN ShipTarget)
        {
            m_oTarget = new TargetTN(ShipTarget);
        }

        /// <summary>
        /// Simple assignment of a missile group as the target of this MFC
        /// </summary>
        /// <param name="OrdnanceTarget">missile group to be targetted.</param>
        public void assignTarget(OrdnanceGroupTN OrdnanceTarget)
        {
            m_oTarget = new TargetTN(OrdnanceTarget);
        }

        /// <summary>
        /// Target assignment of planets.
        /// </summary>
        /// <param name="PlanetTarget">planet</param>
        public void assignTarget(Planet PlanetTarget)
        {
            m_oTarget = new TargetTN(PlanetTarget);
        }

        /// <summary>
        /// Target assignment of populations
        /// </summary>
        /// <param name="PopTarget">Population</param>
        public void assignTarget(Population PopTarget)
        {
            m_oTarget = new TargetTN(PopTarget);
        }

        /// <summary>
        /// Target assignment of waypoints.
        /// </summary>
        /// <param name="WPTarget">Waypoint</param>
        public void assignTarget(Waypoint WPTarget)
        {
            m_oTarget = new TargetTN(WPTarget);
        }

        /// <summary>
        /// Simple launch tube assignment.
        /// </summary>
        /// <param name="tube">launch tube to be assigned.</param>
        public void assignLaunchTube(MissileLauncherTN tube)
        {
            if (m_lLinkedWeapons.Contains(tube) == false)
            {
                m_lLinkedWeapons.Add(tube);

                if (tube.mFC != this)
                    tube.AssignMFC(this);
            }
        }

        /// <summary>
        /// Launch tube removal
        /// </summary>
        /// <param name="tube">tube to be removed.</param>
        public void removeLaunchTube(MissileLauncherTN tube)
        {
            if (m_lLinkedWeapons.Contains(tube) == true)
            {
                m_lLinkedWeapons.Remove(tube);

                if (tube.mFC == this)
                    tube.ClearMFC();
            }

        }

        /// <summary>
        /// Clears all assigned launch tubes.
        /// </summary>
        public void ClearAllWeapons()
        {
            foreach (MissileLauncherTN LaunchTube in m_lLinkedWeapons)
            {
                LaunchTube.ClearMFC();
            }
            m_lLinkedWeapons.Clear();
        }

        /// <summary>
        /// If an MFC is destroyed set all missiles to have no MFC.
        /// </summary>
        public void ClearAllMissiles()
        {
            foreach (OrdnanceGroupTN MissileGroup in m_lMissilesInFlight)
            {
                foreach (OrdnanceTN Missile in MissileGroup.missiles)
                {
                    Missile.mFC = null;
                }
            }
        }

        /// <summary>
        /// Simple deassignment of target to this mfc.
        /// </summary>
        public void clearTarget()
        {
            m_oTarget = null;
        }

        /// <summary>
        /// Simple return of the target of this MFC.
        /// </summary>
        public TargetTN getTarget()
        {
            return m_oTarget;
        }

        /// <summary>
        /// Fire Weapons spawns new missiles groups or adds missiles to existing ones.
        /// </summary>
        /// <param name="TG">Taskgroup this MFC is in.</param>
        /// <param name="FiredFrom">Ship these missiles were fired from.</param>
        /// <returns>If missiles were fired at all from this MFC. true = atleast 1 missile(and therefore missile group, false = no missiles.</returns>
        public bool FireWeapons(TaskGroupTN TG, ShipTN FiredFrom)
        {
            bool retv = false;
            if (m_oTarget != null)
            {
                /// <summary>
                /// Just a temporary variable for this function.
                /// </summary>
                BindingList<OrdnanceGroupTN> LocalMissileGroups = new BindingList<OrdnanceGroupTN>();

                foreach (MissileLauncherTN LaunchTube in m_lLinkedWeapons)
                {
                    if (LaunchTube.isDestroyed == false && LaunchTube.loadTime == 0 && LaunchTube.loadedOrdnance != null)
                    {
                        if (FiredFrom.ShipOrdnance.ContainsKey(LaunchTube.loadedOrdnance) == true)
                        {
                            OrdnanceTN newMissile = new OrdnanceTN(this, LaunchTube.loadedOrdnance, FiredFrom);

                            /// <summary>
                            /// Create a new missile group
                            /// </summary>
                            if (LocalMissileGroups.Count == 0)
                            {
                                OrdnanceGroupTN newMissileGroup = new OrdnanceGroupTN(TG, newMissile);
                                LocalMissileGroups.Add(newMissileGroup);
                                TG.TaskGroupFaction.MissileGroups.Add(newMissileGroup);
                            }
                            /// <summary>
                            /// An existing missile group may be useable.
                            /// </summary>
                            else
                            {
                                bool foundGroup = false;
                                foreach (OrdnanceGroupTN OrdGroup in LocalMissileGroups)
                                {
                                    /// <summary>
                                    /// All Missile groups should be composed of just 1 type of missile for convienence.
                                    if (OrdGroup.missiles[0].missileDef.Id == LaunchTube.loadedOrdnance.Id)
                                    {
                                        OrdGroup.AddMissile(newMissile);
                                        foundGroup = true;
                                        break;
                                    }
                                }

                                /// <summary>
                                /// Have to create a new missile group after all.
                                /// </summary>
                                if (foundGroup == false)
                                {
                                    OrdnanceGroupTN newMissileGroup = new OrdnanceGroupTN(TG, newMissile);
                                    LocalMissileGroups.Add(newMissileGroup);
                                    TG.TaskGroupFaction.MissileGroups.Add(newMissileGroup);
                                }
                            }
                            /// <summary>
                            /// Decrement the loaded ordnance count, and remove the type entirely if this was the last one.
                            /// </summary>
                            FiredFrom.ShipOrdnance[LaunchTube.loadedOrdnance] = FiredFrom.ShipOrdnance[LaunchTube.loadedOrdnance] - 1;
                            if (FiredFrom.ShipOrdnance[LaunchTube.loadedOrdnance] == 0)
                            {
                                FiredFrom.ShipOrdnance.Remove(LaunchTube.loadedOrdnance);
                            }

                            /// <summary>
                            /// Set the launch tube cooldown time as a missile was just fired from it.
                            /// </summary>
                            LaunchTube.loadTime = LaunchTube.missileLauncherDef.rateOfFire;

                            /// <summary>
                            /// return that a missile was launched.
                            /// </summary>
                            retv = true;
                        }
                        else
                        {
                            String Msg = String.Format("No ordnance {0} on ship {1} is available for Launch Tube {2}", LaunchTube.Name, FiredFrom.Name, LaunchTube.Name);
                            MessageEntry newMessage = new MessageEntry(MessageEntry.MessageType.FiringNoAvailableOrdnance, TG.Contact.Position.System, TG.Contact,
                                                                       GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Msg);
                            TG.TaskGroupFaction.MessageLog.Add(newMessage);
                        }

                    }
                    else if (LaunchTube.isDestroyed == true)
                    {
                        String Msg = String.Format("Destroyed launch tube {0} is still attached to {1}'s MFC", LaunchTube.Name, FiredFrom.Name);
                        MessageEntry newMessage = new MessageEntry(MessageEntry.MessageType.Error, TG.Contact.Position.System, TG.Contact,
                                                                   GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Msg);
                        TG.TaskGroupFaction.MessageLog.Add(newMessage);
                    }
                    else if (LaunchTube.loadedOrdnance == null)
                    {
                        String Msg = String.Format("No loaded ordnance for launch tube {0} on ship {1}", LaunchTube.Name, FiredFrom.Name);
                        MessageEntry newMessage = new MessageEntry(MessageEntry.MessageType.FiringNoLoadedOrdnance, TG.Contact.Position.System, TG.Contact,
                                                                   GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Msg);
                        TG.TaskGroupFaction.MessageLog.Add(newMessage);
                    }
                }

                return retv;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Fire this MFC in point defense mode.
        /// </summary>
        /// <param name="TG">Taskgroup the MFC is in</param>
        /// <param name="FiredFrom">Ship the MFC is on</param>
        /// <param name="Target">Target of point defense fire.</param>
        /// <param name="MissilesToFire">Number of missiles to fire at it</param>
        /// <returns></returns>
        public int FireWeaponsPD(TaskGroupTN TG, ShipTN FiredFrom, OrdnanceGroupTN Target, int MissilesToFire)
        {
            /// <summary>
            /// simple stupid sanity check.
            /// </summary>
            if (MissilesToFire == 0)
            {
                return 0;
            }

            int LaunchCount = 0;
            /// <summary>
            /// Just a temporary variable for this function.
            /// </summary>
            BindingList<OrdnanceGroupTN> LocalMissileGroups = new BindingList<OrdnanceGroupTN>();

            foreach (MissileLauncherTN LaunchTube in m_lLinkedWeapons) //int loop = 0; loop < LinkedWeapons.Count; loop++)
            {
                if (LaunchTube.isDestroyed == false && LaunchTube.loadTime == 0 && LaunchTube.loadedOrdnance != null)
                {
                    if (FiredFrom.ShipOrdnance.ContainsKey(LaunchTube.loadedOrdnance) == true)
                    {
                        OrdnanceTN newMissile = new OrdnanceTN(this, LaunchTube.loadedOrdnance, FiredFrom);

                        /// <summary>
                        /// Point defense does not go by MFC targetting. have to add target here.
                        /// </summary>
                        newMissile.target = new TargetTN(Target);

                        LaunchCount++;

                        /// <summary>
                        /// Create a new missile group
                        /// </summary>
                        if (LocalMissileGroups.Count == 0)
                        {
                            OrdnanceGroupTN newMissileGroup = new OrdnanceGroupTN(TG, newMissile);
                            LocalMissileGroups.Add(newMissileGroup);
                            TG.TaskGroupFaction.MissileGroups.Add(newMissileGroup);

                            /// <summary>
                            /// Add this ordnance group to the ord groups targetting list for the intended target missile group.
                            /// This is only necessary here as Manually fired MFC missiles are connected to their MFC.
                            /// </summary>
                            Target.ordGroupsTargetting.Add(newMissileGroup);
                        }
                        /// <summary>
                        /// An existing missile group may be useable.
                        /// </summary>
                        else
                        {
                            bool foundGroup = false;
                            foreach (OrdnanceGroupTN OrdGroup in LocalMissileGroups)
                            {
                                /// <summary>
                                /// All Missile groups should be composed of just 1 type of missile for convienence.
                                if (OrdGroup.missiles[0].missileDef.Id == LaunchTube.loadedOrdnance.Id)
                                {
                                    OrdGroup.AddMissile(newMissile);
                                    foundGroup = true;
                                    break;
                                }
                            }

                            /// <summary>
                            /// Have to create a new missile group after all.
                            /// </summary>
                            if (foundGroup == false)
                            {
                                OrdnanceGroupTN newMissileGroup = new OrdnanceGroupTN(TG, newMissile);
                                LocalMissileGroups.Add(newMissileGroup);
                                TG.TaskGroupFaction.MissileGroups.Add(newMissileGroup);

                                /// <summary>
                                /// Add this ordnance group to the ord groups targetting list for the intended target missile group.
                                /// This is only necessary here as Manually fired MFC missiles are connected to their MFC.
                                /// </summary>
                                Target.ordGroupsTargetting.Add(newMissileGroup);
                            }
                        }
                        /// <summary>
                        /// Decrement the loaded ordnance count, and remove the type entirely if this was the last one.
                        /// </summary>
                        FiredFrom.ShipOrdnance[LaunchTube.loadedOrdnance] = FiredFrom.ShipOrdnance[LaunchTube.loadedOrdnance] - 1;
                        if (FiredFrom.ShipOrdnance[LaunchTube.loadedOrdnance] == 0)
                        {
                            FiredFrom.ShipOrdnance.Remove(LaunchTube.loadedOrdnance);
                        }

                        /// <summary>
                        /// Set the launch tube cooldown time as a missile was just fired from it.
                        /// </summary>
                        LaunchTube.loadTime = LaunchTube.missileLauncherDef.rateOfFire;

                        if (LaunchCount == MissilesToFire)
                            break;
                    }
                    else
                    {
                        String Msg = String.Format("No ordnance {0} on ship {1} is available for Launch Tube {2} in PD Mode", LaunchTube.Name, FiredFrom.Name, LaunchTube.Name);
                        MessageEntry newMessage = new MessageEntry(MessageEntry.MessageType.FiringNoAvailableOrdnance, TG.Contact.Position.System, TG.Contact,
                                                                   GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Msg);
                        TG.TaskGroupFaction.MessageLog.Add(newMessage);
                    }

                }
                else if (LaunchTube.isDestroyed == true)
                {
                    String Msg = String.Format("Destroyed launch tube {0} is still attached to {1}'s MFC in PD Mode", LaunchTube.Name, FiredFrom.Name);
                    MessageEntry newMessage = new MessageEntry(MessageEntry.MessageType.Error, TG.Contact.Position.System, TG.Contact,
                                                               GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Msg);
                    TG.TaskGroupFaction.MessageLog.Add(newMessage);
                }
                else if (LaunchTube.loadedOrdnance == null)
                {
                    String Msg = String.Format("No loaded ordnance for launch tube {0} on ship {1} in PD Mode", LaunchTube.Name, FiredFrom.Name);
                    MessageEntry newMessage = new MessageEntry(MessageEntry.MessageType.FiringNoLoadedOrdnance, TG.Contact.Position.System, TG.Contact,
                                                               GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Msg);
                    TG.TaskGroupFaction.MessageLog.Add(newMessage);
                }
            }
            return LaunchCount;
        }
    }
}
