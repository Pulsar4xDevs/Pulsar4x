using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using Pulsar4X.Helpers.GameMath;

namespace Pulsar4X.Entities
{
    public class SensorModel
    {
        /// <summary>
        /// Galaxy wide sensor sweep of all systems in which this faction has a presence. Here is where things start to get complicated.
        /// </summary>
        public static void SensorSweep(Faction fact)
        {
            /// <summary>
            /// clear the fleet preempt list.
            /// </summary>
            GameState.SE.ClearFleetPreemptList();

            /// <summary>
            /// Loop through all population centers with deep space tracking arrays.
            /// </summary>
            for (int loop = 0; loop < fact.Populations.Count; loop++)
            {
                /// <summary>
                /// No point in running the detection statistics if this population does not have a DSTS.
                /// </summary>
                int DSTS = (int)Math.Floor(fact.Populations[loop].Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number);
                if (DSTS != 0)
                {
                    Population CurrentPopulation = fact.Populations[loop];
                    StarSystem System = CurrentPopulation.Planet.Position.System;

                    int SensorTech = fact.FactionTechLevel[(int)Faction.FactionTechnology.DSTSSensorStrength];
                    if (SensorTech > Constants.Colony.DeepSpaceMax)
                        SensorTech = Constants.Colony.DeepSpaceMax;
                    int ScanStrength = DSTS * Constants.Colony.DeepSpaceStrength[SensorTech];

                    /// <summary>
                    /// iterate through every contact, thermal.Count will be equal to systemContacts.Count
                    /// </summary>
                    for (int detListIterator = 0; detListIterator < System.FactionDetectionLists[fact.FactionID].Thermal.Count; detListIterator++)
                    {
                        /// <summary>
                        /// This System contact is not owned by my faction, and it isn't fully detected yet. Populations only have thermal and EM detection characteristics here.
                        /// </summary>
                        if (fact != System.SystemContactList[detListIterator].faction && (System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond ||
                        System.FactionDetectionLists[fact.FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond))
                        {
                            /// <summary>
                            /// Get the distance from the current population to systemContactList[detListIterator] and store it for this tick.
                            /// </summary>
                            float dist = -1.0f;
                            CurrentPopulation.Contact.DistTable.GetDistance(System.SystemContactList[detListIterator], out dist);

                            /// <summary>
                            /// Now to find the biggest thermal signature in the contact. The biggest for planets is just the planetary pop itself since
                            /// multiple colonies really shouldn't happen.
                            /// </summary>
                            int sig = -1;
                            int detection = -1;

                            /// <summary>
                            /// Planetary Detection.
                            /// </summary>
                            #region Population to Population Detection
                            if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.Population)
                            {
                                Population DetectedPopulation = System.SystemContactList[detListIterator].Entity as Population;
                                /// <summary>
                                /// If this population has already been detected via thermals do not attempt to recalculate whether it has been detected or not.
                                /// </summary>
                                if (System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    sig = DetectedPopulation.ThermalSignature;
                                    double rangeAdj = (((double)sig) / 1000.0);

                                    /// <summary>
                                    /// Detection is ScanStrength multiplied by the range adjustment, multiplied by 100. These numbers come from the overall scan 
                                    /// strength of a population's DSTS arrays, the signature adjustment(signatures larger than 1000 are easier to detect, while 
                                    /// signatures smaller than 1000 are harder to detect, planets will often be much much larger), and the 100, for the adjustment 
                                    /// to 1M km. it is only 100 though because units are assumed to be in 10k lots, so 100 * 10,000 = 1,000,000
                                    /// </summary>
                                    detection = (int)Math.Floor(ScanStrength * rangeAdj * 100.0);

                                    /// <summary>
                                    /// LargeDetection handles determining if dist or detection go beyond INTMAX and acts accordingly.
                                    /// </summary>
                                    bool det = fact.LargeDetection(dist, detection);

                                    /// <summary>
                                    /// Mark this contact as detected for this time slice via thermal for both the contact, and for the faction as a whole.
                                    /// </summary>
                                    if (det == true)
                                    {
                                        DetectedPopulation.ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                        System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] = GameState.Instance.CurrentSecond;
                                        if (fact.DetPopList.Contains(DetectedPopulation) == false)
                                            fact.DetPopList.Add(DetectedPopulation);
                                    }
                                }

                                /// <summary>
                                /// if this population has already been detected in EM then obviously there is no need to attempt to find it again.
                                /// </summary>
                                if (System.FactionDetectionLists[fact.FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    /// <summary>
                                    /// As signature can be different, detection should be recalculated. passive population based detection can be put into a table if need be, I am not
                                    /// sure how useful that would be however.
                                    /// </summary>
                                    sig = DetectedPopulation.EMSignature;
                                    double rangeAdj = (((double)sig) / 1000.0);
                                    detection = (int)Math.Floor(ScanStrength * rangeAdj * 100.0);

                                    bool det = fact.LargeDetection(dist, detection);

                                    if (det == true)
                                    {
                                        DetectedPopulation.EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                        System.FactionDetectionLists[fact.FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                                        if (fact.DetPopList.Contains(DetectedPopulation) == false)
                                            fact.DetPopList.Add(DetectedPopulation);
                                    }
                                }
                            }
                            #endregion

                            /// <summary>
                            /// Taskgroup Detection.
                            /// </summary>
                            #region Population To TaskGroup Detection
                            if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.TaskGroup && (System.SystemContactList[detListIterator].Entity as TaskGroupTN).Ships.Count != 0)
                            {
                                TaskGroupTN DetectedTaskGroup = System.SystemContactList[detListIterator].Entity as TaskGroupTN;
                                bool noDetection = false;
                                bool allDetection = false;
                                #region Population to Taskgroup thermal detection
                                if (System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    /// <summary>
                                    /// Get the best detection range for thermal signatures in loop.
                                    /// </summary>
                                    int ShipID = DetectedTaskGroup.ThermalSortList.Last();
                                    ShipTN scratch = DetectedTaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentThermalSignature;

                                    double rangeAdj = (((double)sig) / 1000.0);

                                    /// <summary>
                                    /// Detection is ScanStrength multiplied by the range adjustment, multiplied by 100. These numbers come from the overall scan strength of a population's
                                    /// DSTS arrays, the signature adjustment(signatures larger than 1000 are easier to detect, while signatures smaller than 1000 are harder to detect, planets will
                                    /// often be much much larger), and the 100, for the adjustment to 1M km. it is only 100 though because units are assumed to be in 10k lots, so 100 * 10,000 = 1,000,000
                                    /// </summary>
                                    detection = (int)Math.Floor(ScanStrength * rangeAdj * 100.0);

                                    /// <summary>
                                    /// LargeDetection handles determining if dist or detection go beyond INTMAX and acts accordingly.
                                    /// </summary>
                                    bool det = fact.LargeDetection(dist, detection);

                                    /// <summary>
                                    /// Good case, none of the ships are detected.
                                    /// </summary>
                                    if (det == false)
                                    {
                                        noDetection = true;
                                    }

                                    /// <summary>
                                    /// Atleast the biggest ship is detected.
                                    /// </summary
                                    if (noDetection == false)
                                    {
                                        ShipID = DetectedTaskGroup.ThermalSortList.First();
                                        scratch = DetectedTaskGroup.Ships[ShipID];
                                        sig = scratch.CurrentThermalSignature;

                                        rangeAdj = (((double)sig) / 1000.0);
                                        detection = (int)Math.Floor(ScanStrength * rangeAdj * 100.0);
                                        det = fact.LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < DetectedTaskGroup.Ships.Count; loop3++)
                                            {
                                                DetectedTaskGroup.Ships[loop3].ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                                if (fact.DetShipList.Contains(DetectedTaskGroup.Ships[loop3]) == false)
                                                {
                                                    fact.DetShipList.Add(DetectedTaskGroup.Ships[loop3]);
                                                }
                                            }
                                            System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] = GameState.Instance.CurrentSecond;
                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>
                                            for (int loop3 = 0; loop3 < DetectedTaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = DetectedTaskGroup.ThermalSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = DetectedTaskGroup.Ships[node.Value];

                                                    if (scratch.ThermalDetection[fact.FactionID] != GameState.Instance.CurrentSecond)
                                                    {
                                                        sig = scratch.CurrentThermalSignature;
                                                        rangeAdj = (((double)sig) / 1000.0);
                                                        detection = (int)Math.Floor(ScanStrength * rangeAdj * 100.0);

                                                        /// <summary>
                                                        /// Test each ship until I get to one I don't see.
                                                        /// </summary>
                                                        det = fact.LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                                            if (fact.DetShipList.Contains(scratch) == false)
                                                            {
                                                                fact.DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == DetectedTaskGroup.ThermalSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial Thermal detect for Pops looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, fact.Populations[loop].Position.System, fact.Populations[loop].Contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        fact.MessageLog.Add(NMsg);
                                                        done = true;
                                                        break;
                                                    }
                                                    node = node.Previous;
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion

                                #region Population to taskgroup EM detection
                                if (System.FactionDetectionLists[fact.FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    noDetection = false;
                                    allDetection = false;

                                    /// <summary>
                                    /// Get the best detection range for EM signatures in loop.
                                    /// </summary>
                                    int ShipID = DetectedTaskGroup.EMSortList.Last();
                                    ShipTN scratch = DetectedTaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentEMSignature;

                                    double rangeAdj = (((double)sig) / 1000.0);
                                    detection = (int)Math.Floor(ScanStrength * rangeAdj * 100.0);

                                    /// <summary>
                                    /// LargeDetection handles determining if dist or detection go beyond INTMAX and acts accordingly.
                                    /// </summary>
                                    bool det = fact.LargeDetection(dist, detection);

                                    /// <summary>
                                    /// Good case, none of the ships are detected.
                                    /// </summary>
                                    if (det == false)
                                    {
                                        noDetection = true;
                                    }

                                    /// <summary>
                                    /// Atleast the biggest ship is detected.
                                    /// </summary
                                    if (noDetection == false)
                                    {
                                        ShipID = DetectedTaskGroup.EMSortList.First();
                                        scratch = DetectedTaskGroup.Ships[ShipID];
                                        sig = scratch.CurrentEMSignature;

                                        rangeAdj = (((double)sig) / 1000.0);
                                        detection = (int)Math.Floor(ScanStrength * rangeAdj * 100.0);

                                        /// <summary>
                                        /// LargeDetection handles determining if dist or detection go beyond INTMAX and acts accordingly.
                                        /// </summary>
                                        det = fact.LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < DetectedTaskGroup.Ships.Count; loop3++)
                                            {
                                                DetectedTaskGroup.Ships[loop3].EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                                if (fact.DetShipList.Contains(DetectedTaskGroup.Ships[loop3]) == false)
                                                {
                                                    fact.DetShipList.Add(DetectedTaskGroup.Ships[loop3]);
                                                }
                                            }
                                            System.FactionDetectionLists[fact.FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>
                                            for (int loop3 = 0; loop3 < DetectedTaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = DetectedTaskGroup.EMSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = DetectedTaskGroup.Ships[node.Value];

                                                    if (scratch.EMDetection[fact.FactionID] != GameState.Instance.CurrentSecond)
                                                    {
                                                        sig = scratch.CurrentEMSignature;

                                                        /// <summary>
                                                        /// here is where EM detection differs from Thermal detection:
                                                        /// If a ship has a signature of 0 by this point(and we didn't already hit noDetection above,
                                                        /// it means that one ship is emitting a signature, but that no other ships are.
                                                        /// Mark the group as totally detected, but not the ships, this serves to tell me that the ships are undetectable
                                                        /// in this case.
                                                        /// </summary>
                                                        if (sig == 0)
                                                        {
                                                            /// <summary>
                                                            /// The last signature we looked at was the ship emitting an EM sig, and this one is not.
                                                            /// Mark the entire group as "spotted" because no other detection will occur.
                                                            /// </summary>
                                                            if (DetectedTaskGroup.Ships[node.Next.Value].EMDetection[fact.FactionID] == GameState.Instance.CurrentSecond)
                                                            {
                                                                System.FactionDetectionLists[fact.FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                                                                //since the ships aren't actually detected, don't add them to the detected ships list.
                                                            }
                                                            break;
                                                        }

                                                        rangeAdj = (((double)sig) / 1000.0);
                                                        detection = (int)Math.Floor(ScanStrength * rangeAdj * 100.0);

                                                        det = fact.LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                                            if (fact.DetShipList.Contains(scratch) == false)
                                                            {
                                                                fact.DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == DetectedTaskGroup.EMSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial EM detect for Pops looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, fact.Populations[loop].Position.System, fact.Populations[loop].Contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        fact.MessageLog.Add(NMsg);
                                                        done = true;
                                                        break;
                                                    }
                                                    node = node.Previous;
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion

                            }
                            #endregion

                            /// <summary>
                            /// Missile Detection.
                            /// </summary>
                            #region Population To Missile Detection
                            if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.Missile)
                            {
                                OrdnanceGroupTN MissileGroup = (System.SystemContactList[detListIterator].Entity as OrdnanceGroupTN);
                                OrdnanceTN Missile = MissileGroup.missiles[0];
                                if (System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    sig = (int)Math.Ceiling(Missile.missileDef.totalThermalSignature);
                                    double rangeAdj = (((double)sig) / 1000.0);

                                    /// <summary>
                                    /// Detection is ScanStrength multiplied by the range adjustment, multiplied by 100. These numbers come from the overall scan strength of a population's
                                    /// DSTS arrays, the signature adjustment(signatures larger than 1000 are easier to detect, while signatures smaller than 1000 are harder to detect, planets will
                                    /// often be much much larger), and the 100, for the adjustment to 1M km. it is only 100 though because units are assumed to be in 10k lots, so 100 * 10,000 = 1,000,000
                                    /// </summary>
                                    detection = (int)Math.Floor(ScanStrength * rangeAdj * 100.0);

                                    /// <summary>
                                    /// LargeDetection handles determining if dist or detection go beyond INTMAX and acts accordingly.
                                    /// </summary>
                                    bool det = fact.LargeDetection(dist, detection);

                                    /// <summary>
                                    /// Mark this contact as detected for this time slice via thermal for both the contact, and for the faction as a whole.
                                    /// </summary>
                                    if (det == true)
                                    {
                                        for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                        {
                                            MissileGroup.missiles[loop3].ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                        }

                                        if (fact.DetMissileList.Contains(MissileGroup) == false)
                                        {
                                            fact.DetMissileList.Add(MissileGroup);
                                        }

                                        System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] = GameState.Instance.CurrentSecond;
                                    }
                                }
                                if (System.FactionDetectionLists[fact.FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    int EMSignature = 0;
                                    if (Missile.missileDef.activeStr != 0.0f)
                                    {
                                        EMSignature = Missile.missileDef.aSD.gps;
                                    }

                                    if (EMSignature != 0)
                                    {
                                        /// <summary>
                                        /// As signature can be different, detection should be recalculated. passive population based detection can be put into a table if need be, I am not
                                        /// sure how useful that would be however.
                                        /// </summary>
                                        sig = EMSignature;
                                        double rangeAdj = (((double)sig) / 1000.0);
                                        detection = (int)Math.Floor(ScanStrength * rangeAdj * 100.0);

                                        bool det = fact.LargeDetection(dist, detection);

                                        if (det == true)
                                        {
                                            for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                            {
                                                MissileGroup.missiles[loop3].EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                            }

                                            if (fact.DetMissileList.Contains(MissileGroup) == false)
                                            {
                                                fact.DetMissileList.Add(MissileGroup);
                                            }

                                            System.FactionDetectionLists[fact.FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                                        }
                                    }
                                }
                            }
                            #endregion
                        }//end if not owned and not detected
                    }//end for thermal.Count
                }//end if DSTS != 0
            }//end for populations

            /// <summary>
            /// Loop through all faction taskgroups.
            /// </summary>
            #region Faction Taskgroup Loop
            foreach (TaskGroupTN CurrentTaskGroup in fact.TaskGroups)
            {
                /// <summary>
                /// This Taskgroup can't perform a sensor sweep until jump sickness is gone. It stands to reason that if ship[0] is sick they all will be, but clever taskgroup reorganizing
                /// may thwart that. I'd recommend just banning taskgroup reorganization while jumpsick.
                /// </summary>
                if (CurrentTaskGroup.IsJumpSick())
                    continue;

                /// <summary>
                /// A taskgroup with no ships cannot detect anything.
                /// </summary>
                if (CurrentTaskGroup.Ships.Count == 0)
                    continue;

                StarSystem System = CurrentTaskGroup.Contact.Position.System;
                /// <summary>
                /// Loop through the global contacts list for the system. thermal.Count is equal to SystemContacts.Count. or should be.
                /// </summary>
                for (int detListIterator = 0; detListIterator < System.FactionDetectionLists[fact.FactionID].Thermal.Count; detListIterator++)
                {
                    /// <summary>
                    /// Check if System.SystemContactList[detListIterator] is in the same faction, and it hasn't been fully detected yet.
                    /// </summary>
                    if (fact != System.SystemContactList[detListIterator].faction && (System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond ||
                        System.FactionDetectionLists[fact.FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond || System.FactionDetectionLists[fact.FactionID].Active[detListIterator] != GameState.Instance.CurrentSecond))
                    {
                        float dist;
                        // Check to see if our distance table is updated for this contact.
                        if (!CurrentTaskGroup.Contact.DistTable.GetDistance(System.SystemContactList[detListIterator], out dist))
                        {
                            /// <summary>
                            /// Handle fleet interception check here.
                            /// </summary>
                            if (GameState.SE.FleetInterceptionPreemptTick != GameState.Instance.CurrentSecond && System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.TaskGroup)
                            {
                                TaskGroupTN TaskGroup = System.SystemContactList[detListIterator].Entity as TaskGroupTN;
                                /// <summary>
                                /// player created empty taskgroups should not get checked here.
                                /// </summary>
                                if (TaskGroup.Ships.Count != 0)
                                {
                                    /// <summary>
                                    /// how far could this TG travel within a single day?
                                    /// </summary>
                                    float TaskGroupDistance = (float)Distance.ToAU(CurrentTaskGroup.CurrentSpeed) * Constants.TimeInSeconds.Day;

#warning fleet intercept preemption magic number here, if less than 5 days travel time currently.
#warning fleet intercept needs to only process for hostile or unknown taskgroups, not all taskgroups.

                                    int ShipID = TaskGroup.ActiveSortList.Last();
                                    ShipTN LargestContactTCS = TaskGroup.Ships[ShipID];

                                    /// <summary>
                                    /// If this Taskgroup isn't already detected, and the distance is short enough, put it in the fleet intercept preempt list.
                                    /// </summary>
                                    if (TaskGroupDistance >= (dist / 5.0) && (fact.DetectedContactLists.ContainsKey(System) == false ||
                                        (fact.DetectedContactLists.ContainsKey(System) == true && (!fact.DetectedContactLists[System].DetectedContacts.ContainsKey(LargestContactTCS) ||
                                         fact.DetectedContactLists[System].DetectedContacts[LargestContactTCS].active == false))))
                                    {
#warning Update this fleet intercept list for planets/populations
                                        GameState.SE.FleetInterceptionPreemptTick = GameState.Instance.CurrentSecond;

                                        GameState.SE.AddFleetToPreemptList(CurrentTaskGroup);
                                        if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.TaskGroup)
                                        {
                                            GameState.SE.AddFleetToPreemptList(TaskGroup);
                                        }
                                    }
                                }
                                else
                                {
                                    /// <summary>
                                    /// TaskGroupToTest is going to be empty so it can't be detected.
                                    /// </summary>
                                    continue;
                                }

                            } // /Fleet Interception Check
                        } // distance Table Update.


                        /// <summary>
                        /// Now to find the biggest signature in the contact. The biggest for planets is just the planetary pop itself since
                        /// multiple colonies really shouldn't happen.
                        /// </summary>
                        int sig = -1;
                        int detection = -1;

                        /// <summary>
                        /// Handle population detection
                        /// </summary>
                        if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.Population)
                        {
                            #region TaskGroup to Population detection
                            Population Pop = System.SystemContactList[detListIterator].Entity as Population;
                            if (System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond)
                            {
                                sig = Pop.ThermalSignature;
                                detection = CurrentTaskGroup.BestThermal.pSensorDef.GetPassiveDetectionRange(sig);

                                /// <summary>
                                /// LargeDetection handles determining if dist or detection go beyond INTMAX and acts accordingly.
                                /// </summary>
                                bool det = fact.LargeDetection(dist, detection);

                                /// <summary>
                                /// Mark this contact as detected for this time slice via thermal for both the contact, and for the faction as a whole.
                                /// </summary>
                                if (det == true)
                                {
                                    Pop.ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                    System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] = GameState.Instance.CurrentSecond;
                                    if (fact.DetPopList.Contains(Pop) == false)
                                        fact.DetPopList.Add(Pop);
                                }
                            }

                            if (System.FactionDetectionLists[fact.FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond)
                            {
                                sig = Pop.EMSignature;
                                detection = CurrentTaskGroup.BestEM.pSensorDef.GetPassiveDetectionRange(sig);

                                bool det = fact.LargeDetection(dist, detection);

                                if (det == true)
                                {
                                    Pop.EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                    System.FactionDetectionLists[fact.FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                                    if (fact.DetPopList.Contains(Pop) == false)
                                        fact.DetPopList.Add(Pop);
                                }
                            }

                            if (System.FactionDetectionLists[fact.FactionID].Active[detListIterator] != GameState.Instance.CurrentSecond && CurrentTaskGroup.ActiveSensorQue.Count > 0)
                            {
                                sig = Constants.ShipTN.ResolutionMax - 1;
                                /// <summary>
                                /// The -1 is because a planet is most certainly not a missile.
                                /// </summary>
                                detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                                /// <summary>
                                /// Do detection calculations here.
                                /// </summary>
                                bool det = fact.LargeDetection(dist, detection);

                                if (det == true)
                                {
                                    Pop.ActiveDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                    System.FactionDetectionLists[fact.FactionID].Active[detListIterator] = GameState.Instance.CurrentSecond;
                                    if (fact.DetPopList.Contains(Pop) == false)
                                        fact.DetPopList.Add(Pop);
                                }
                            }
                            #endregion
                        }
                        else if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.TaskGroup)
                        {
                            TaskGroupTN TaskGroupToTest = System.SystemContactList[detListIterator].Entity as TaskGroupTN;

                            if (TaskGroupToTest.Ships.Count != 0)
                            {
                                /// <summary>
                                /// Taskgroups have multiple signatures, so noDetection and allDetection become important.
                                /// </summary>

                                /// <summary>
                                /// Thermal detection code.
                                /// </summary>
                                TaskGroupThermalDetection(fact, System, CurrentTaskGroup, TaskGroupToTest, dist, detListIterator);

                                /// <summary>
                                /// EM detection is done in this function, this only differs from the above in the various datatypes used as well as sensor types.
                                /// </summary>
                                TaskGroupEMDetection(fact, System, CurrentTaskGroup, TaskGroupToTest, dist, detListIterator);

                                /// <summary>
                                /// Active detection, this is different from passive detection in that resolution of the sensor as well as size of the target ship matter,
                                /// rather than any signature.
                                /// </summary>
                                TaskGroupActiveDetection(fact, System, CurrentTaskGroup, TaskGroupToTest, dist, detListIterator);

                            }


                        }
                        else if (System.SystemContactList[detListIterator].SSEntity == StarSystemEntityType.Missile)
                        {
                            OrdnanceGroupTN MissileGroup = System.SystemContactList[detListIterator].Entity as OrdnanceGroupTN;

                            if (MissileGroup.missiles.Count != 0)
                            {
                                /// <summary>
                                /// Do Missile Detection here:
                                /// </summary>
                                #region Missile Detection
                                OrdnanceTN Missile = MissileGroup.missiles[0];
                                if (System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    int ThermalSignature = (int)Math.Ceiling(Missile.missileDef.totalThermalSignature);
                                    detection = -1;

                                    /// <summary>
                                    /// Check to make sure the taskgroup has a thermal sensor available, otherwise use the default.
                                    /// </summary>
                                    if (CurrentTaskGroup.BestThermalCount != 0)
                                    {
                                        detection = CurrentTaskGroup.BestThermal.pSensorDef.GetPassiveDetectionRange(ThermalSignature);
                                    }
                                    else
                                    {
                                        detection = fact.ComponentList.DefaultPassives.GetPassiveDetectionRange(ThermalSignature);
                                    }

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = fact.LargeDetection(dist, detection);

                                    /// <summary>
                                    /// If one missile is detected, all are.
                                    /// </summary>
                                    if (det == true)
                                    {
                                        for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                        {
                                            MissileGroup.missiles[loop3].ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                        }

                                        if (fact.DetMissileList.Contains(MissileGroup) == false)
                                        {
                                            fact.DetMissileList.Add(MissileGroup);
                                        }

                                        System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] = GameState.Instance.CurrentSecond;
                                    }
                                }

                                if (System.FactionDetectionLists[fact.FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond)
                                {
                                    int EMSignature = 0;
                                    if (Missile.missileDef.activeStr != 0.0f)
                                    {
                                        EMSignature = Missile.missileDef.aSD.gps;
                                    }

                                    if (EMSignature != 0)
                                    {
                                        /// <summary>
                                        /// Check to see if the taskgroup has an em sensor, and that said em sensor is not destroyed.
                                        /// otherwise use the default passive detection range.
                                        /// </summary>
                                        if (CurrentTaskGroup.BestEMCount > 0)
                                        {
                                            detection = CurrentTaskGroup.BestEM.pSensorDef.GetPassiveDetectionRange(EMSignature);
                                        }
                                        else
                                        {
                                            detection = fact.ComponentList.DefaultPassives.GetPassiveDetectionRange(EMSignature);
                                        }

                                        bool det = fact.LargeDetection(dist, detection);

                                        /// <summary>
                                        /// If one missile is detected, all are.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                            {
                                                MissileGroup.missiles[loop3].EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                            }

                                            if (fact.DetMissileList.Contains(MissileGroup) == false)
                                            {
                                                fact.DetMissileList.Add(MissileGroup);
                                            }


                                            System.FactionDetectionLists[fact.FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                                        }
                                    }
                                }

                                if (System.FactionDetectionLists[fact.FactionID].Active[detListIterator] != GameState.Instance.CurrentSecond && CurrentTaskGroup.ActiveSensorQue.Count > 0)
                                {
                                    int TotalCrossSection_MSP = (int)Math.Ceiling(Missile.missileDef.size);
                                    sig = -1;
                                    detection = -1;

                                    if (TotalCrossSection_MSP < ((Constants.OrdnanceTN.MissileResolutionMaximum + 6) + 1))
                                    {
                                        if (TotalCrossSection_MSP <= (Constants.OrdnanceTN.MissileResolutionMinimum + 6))
                                        {
                                            sig = Constants.OrdnanceTN.MissileResolutionMinimum;
                                        }
                                        else if (TotalCrossSection_MSP <= (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                                        {
                                            sig = TotalCrossSection_MSP - 6;
                                        }
                                        detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(0, sig);
                                    }
                                    else
                                    {
                                        /// <summary>
                                        /// Big missiles will be treated in HS terms: 21-40 MSP = 2 HS, 41-60 = 3 HS, 61-80 = 4 HS, 81-100 = 5 HS. The same should hold true for greater than 100 sized missiles.
                                        /// but those are impossible to build.
                                        /// </summary>
                                        sig = (int)Math.Ceiling((float)TotalCrossSection_MSP / 20.0f);

                                        detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);
                                    }

                                    bool det = fact.LargeDetection(dist, detection);

                                    if (det == true)
                                    {
                                        for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                        {
                                            MissileGroup.missiles[loop3].ActiveDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                        }

                                        if (fact.DetMissileList.Contains(MissileGroup) == false)
                                        {
                                            fact.DetMissileList.Add(MissileGroup);
                                        }

                                        System.FactionDetectionLists[fact.FactionID].Active[detListIterator] = GameState.Instance.CurrentSecond;
                                    }
                                }
                                #endregion
                            }
                        }
                        /// <summary>
                        /// End if planet,Task Group, or Missile
                        /// </sumamry>
                    }
                    /// <summary>
                    /// End if not globally detected.
                    /// </summary>

                }
                /// <summary>
                /// End for Faction Contact Lists
                /// </summary>

            }
            #endregion
            /// <summary>
            /// End for Faction TaskGroups.
            /// </summary>


            /// <summary>
            /// Loop through all missile groups.
            /// </summary>
            #region Faction Missile Loop
            for (int loop = 0; loop < fact.MissileGroups.Count; loop++)
            {
                /// <summary>
                /// Hopefully I won't get into this situation ever. 
                /// </summary>
                if (fact.MissileGroups[loop].missiles.Count == 0)
                {
                    String ErrorMessage = string.Format("Missile group {0} has no missiles and is still in the list of missile groups.", fact.MissileGroups[loop].Name);
                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, fact.MissileGroups[loop].contact.Position.System, fact.MissileGroups[loop].contact,
                                                 GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);

                    fact.MessageLog.Add(NMsg);
                    continue;
                }

                /// <summary>
                /// Missile groups have homogeneous missile complements, so will only have 1 of each sensor to check against.
                /// </summary>
                StarSystem System = fact.MissileGroups[loop].contact.Position.System;
                OrdnanceTN Missile = fact.MissileGroups[loop].missiles[0];

                /// <summary>
                /// If this missile has no sensors don't process all this.
                /// </summary>
                if (Missile.missileDef.tHD == null && Missile.missileDef.eMD == null && Missile.missileDef.aSD == null)
                    continue;

                /// <summary>
                /// Loop through the global contacts list for the system. thermal.Count is equal to SystemContacts.Count. or should be.
                /// </summary>
                for (int loop2 = 0; loop2 < System.FactionDetectionLists[fact.FactionID].Thermal.Count; loop2++)
                {
                    /// <summary>
                    /// I don't own loop2, and it hasn't been fully detected yet. And this missile can actually detect things.
                    /// </summary>
                    if (fact != System.SystemContactList[loop2].faction && (System.FactionDetectionLists[fact.FactionID].Thermal[loop2] != GameState.Instance.CurrentSecond ||
                        System.FactionDetectionLists[fact.FactionID].EM[loop2] != GameState.Instance.CurrentSecond || System.FactionDetectionLists[fact.FactionID].Active[loop2] != GameState.Instance.CurrentSecond) &&
                        (Missile.missileDef.thermalStr != 0.0f || Missile.missileDef.eMStr != 0.0f || Missile.missileDef.activeStr != 0.0f))
                    {
                        float dist;
                        fact.MissileGroups[loop].contact.DistTable.GetDistance(System.SystemContactList[loop2], out dist);

                        /// <summary>
                        /// Now to find the biggest thermal signature in the contact. The biggest for planets is just the planetary pop itself since
                        /// multiple colonies really shouldn't happen.
                        /// </summary>
                        int sig = -1;
                        int detection = -1;

                        /// <summary>
                        /// Check for each detection method whether or not this missile can actually detect things, then copy the taskgroup detection code here.
                        /// Update detected contacts as needed:
                        /// </summary>
                        if (System.SystemContactList[loop2].SSEntity == StarSystemEntityType.Population)
                        {
                            #region Planetary detection by missiles
                            Population Pop = System.SystemContactList[loop2].Entity as Population;
                            /// <summary>
                            /// Does this missile have a thermal sensor suite? by default the answer is no, this is different from Aurora which gives them the basic ship suite.
                            /// </summary>
                            if (Missile.missileDef.tHD != null && System.FactionDetectionLists[fact.FactionID].Thermal[loop2] != GameState.Instance.CurrentSecond)
                            {
                                sig = Pop.ThermalSignature;
                                detection = Missile.missileDef.tHD.GetPassiveDetectionRange(sig);

                                bool det = fact.LargeDetection(dist, detection);

                                /// <summary>
                                /// Mark this contact as detected for this time slice via thermal for both the contact, and for the faction as a whole.
                                /// </summary>
                                if (det == true)
                                {
                                    Pop.ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                    System.FactionDetectionLists[fact.FactionID].Thermal[loop2] = GameState.Instance.CurrentSecond;
                                    if (fact.DetPopList.Contains(Pop) == false)
                                        fact.DetPopList.Add(Pop);
                                }
                            }

                            /// <summary>
                            /// Does this missile have an EM sensor suite? by default again the answer is no, which is again different from Aurora.
                            /// </summary>
                            if (Missile.missileDef.eMD != null && System.FactionDetectionLists[fact.FactionID].EM[loop2] != GameState.Instance.CurrentSecond)
                            {
                                sig = Pop.EMSignature;
                                detection = Missile.missileDef.eMD.GetPassiveDetectionRange(sig);

                                bool det = fact.LargeDetection(dist, detection);

                                /// <summary>
                                /// Mark this contact as detected for this time slice via EM for both the contact, and for the faction as a whole.
                                /// </summary>
                                if (det == true)
                                {
                                    Pop.EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                    System.FactionDetectionLists[fact.FactionID].EM[loop2] = GameState.Instance.CurrentSecond;
                                    if (fact.DetPopList.Contains(Pop) == false)
                                        fact.DetPopList.Add(Pop);
                                }
                            }

                            /// <summary>
                            /// Lastly does this missile have an active sensor?
                            /// </summary>
                            if (Missile.missileDef.aSD != null && System.FactionDetectionLists[fact.FactionID].Active[loop2] != GameState.Instance.CurrentSecond)
                            {
                                sig = Constants.ShipTN.ResolutionMax - 1;
                                detection = Missile.missileDef.aSD.GetActiveDetectionRange(sig, -1);

                                bool det = fact.LargeDetection(dist, detection);

                                /// <summary>
                                /// Mark this contact as detected for this time slice via Active for both the contact, and for the faction as a whole.
                                /// </summary>
                                if (det == true)
                                {
                                    Pop.ActiveDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                    System.FactionDetectionLists[fact.FactionID].Active[loop2] = GameState.Instance.CurrentSecond;
                                    if (fact.DetPopList.Contains(Pop) == false)
                                        fact.DetPopList.Add(Pop);
                                }
                            }
                            #endregion
                        }
                        else if (System.SystemContactList[loop2].SSEntity == StarSystemEntityType.TaskGroup)
                        {
                            TaskGroupTN TaskGroup = System.SystemContactList[loop2].Entity as TaskGroupTN;

                            if (TaskGroup.Ships.Count != 0)
                            {
                                #region Taskgroup Detection by Missiles
                                /// <summary>
                                /// Taskgroups have multiple signatures, so noDetection and allDetection become important.
                                /// </summary>

                                bool noDetection = false;
                                bool allDetection = false;

                                #region Ship Thermal Detection Code
                                if (System.FactionDetectionLists[fact.FactionID].Thermal[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.tHD != null)
                                {
                                    int ShipID = TaskGroup.ThermalSortList.Last();
                                    ShipTN scratch = TaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentThermalSignature;

                                    detection = Missile.missileDef.tHD.GetPassiveDetectionRange(sig);

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = fact.LargeDetection(dist, detection);

                                    /// <summary>
                                    /// Good case, none of the ships are detected.
                                    /// </summary>
                                    if (det == false)
                                    {
                                        noDetection = true;
                                    }

                                    /// <summary>
                                    /// Atleast the biggest ship is detected.
                                    /// </summary
                                    if (noDetection == false)
                                    {
                                        ShipID = TaskGroup.ThermalSortList.First();
                                        scratch = TaskGroup.Ships[ShipID];
                                        sig = scratch.CurrentThermalSignature;

                                        /// <summary>
                                        /// Now for the smallest vs the best.
                                        /// </summary>
                                        detection = Missile.missileDef.tHD.GetPassiveDetectionRange(sig);
                                        det = fact.LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                TaskGroup.Ships[loop3].ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                                if (fact.DetShipList.Contains(TaskGroup.Ships[loop3]) == false)
                                                {
                                                    fact.DetShipList.Add(TaskGroup.Ships[loop3]);
                                                }
                                            }
                                            System.FactionDetectionLists[fact.FactionID].Thermal[loop2] = GameState.Instance.CurrentSecond;
                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>
                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = TaskGroup.ThermalSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = TaskGroup.Ships[node.Value];

                                                    if (scratch.ThermalDetection[fact.FactionID] != GameState.Instance.CurrentSecond)
                                                    {
                                                        sig = scratch.CurrentThermalSignature;
                                                        detection = Missile.missileDef.tHD.GetPassiveDetectionRange(sig);

                                                        /// <summary>
                                                        /// Test each ship until I get to one I don't see.
                                                        /// </summary>
                                                        det = fact.LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                                            if (fact.DetShipList.Contains(scratch) == false)
                                                            {
                                                                fact.DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == TaskGroup.ThermalSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial Thermal detect for missiles looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, fact.MissileGroups[loop].contact.Position.System, fact.MissileGroups[loop].contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        fact.MessageLog.Add(NMsg);
                                                        done = true;
                                                        break;
                                                    }
                                                    node = node.Previous;
                                                }//end while
                                            }//end for
                                        }//end else if (noDetection == false && allDetection == false)
                                    }//end if noDetection == false
                                }//end if not detected and missile can detect thermally
                                #endregion

                                #region Ship EM Detection Code
                                if (System.FactionDetectionLists[fact.FactionID].EM[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.eMD != null)
                                {
                                    int ShipID = TaskGroup.EMSortList.Last();
                                    ShipTN scratch = TaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentEMSignature;

                                    detection = Missile.missileDef.eMD.GetPassiveDetectionRange(sig);

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = fact.LargeDetection(dist, detection);

                                    /// <summary>
                                    /// Good case, none of the ships are detected.
                                    /// </summary>
                                    if (det == false)
                                    {
                                        noDetection = true;
                                    }

                                    /// <summary>
                                    /// Atleast the biggest ship is detected.
                                    /// </summary
                                    if (noDetection == false)
                                    {
                                        ShipID = TaskGroup.EMSortList.First();
                                        scratch = TaskGroup.Ships[ShipID];
                                        sig = scratch.CurrentEMSignature;

                                        /// <summary>
                                        /// Now for the smallest vs the best.
                                        /// </summary>
                                        detection = Missile.missileDef.eMD.GetPassiveDetectionRange(sig);
                                        det = fact.LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                TaskGroup.Ships[loop3].EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                                if (fact.DetShipList.Contains(TaskGroup.Ships[loop3]) == false)
                                                {
                                                    fact.DetShipList.Add(TaskGroup.Ships[loop3]);
                                                }
                                            }
                                            System.FactionDetectionLists[fact.FactionID].EM[loop2] = GameState.Instance.CurrentSecond;
                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>
                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = TaskGroup.EMSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = TaskGroup.Ships[node.Value];

                                                    if (scratch.EMDetection[fact.FactionID] != GameState.Instance.CurrentSecond)
                                                    {
                                                        sig = scratch.CurrentEMSignature;

                                                        /// <summary>
                                                        /// here is where EM detection differs from Thermal detection:
                                                        /// If a ship has a signature of 0 by this point(and we didn't already hit noDetection above,
                                                        /// it means that one ship is emitting a signature, but that no other ships are.
                                                        /// Mark the group as totally detected, but not the ships, this serves to tell me that the ships are undetectable
                                                        /// in this case.
                                                        /// Also, sig will never be 0 for the first iteration of the loop, that has already been tested, so I don't need to worry about
                                                        /// node.Next.Value blowing up on me.
                                                        /// </summary>
                                                        if (sig == 0)
                                                        {
                                                            /// <summary>
                                                            /// The last signature we looked at was the ship emitting an EM sig, and this one is not.
                                                            /// Mark the entire group as "spotted" because no other detection will occur.
                                                            /// </summary>
                                                            if (TaskGroup.Ships[node.Next.Value].EMDetection[fact.FactionID] == GameState.Instance.CurrentSecond)
                                                            {
                                                                System.FactionDetectionLists[fact.FactionID].EM[loop2] = GameState.Instance.CurrentSecond;
                                                            }
                                                            break;
                                                        }


                                                        detection = Missile.missileDef.eMD.GetPassiveDetectionRange(sig);

                                                        /// <summary>
                                                        /// Test each ship until I get to one I don't see.
                                                        /// </summary>
                                                        det = fact.LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                                            if (fact.DetShipList.Contains(scratch) == false)
                                                            {
                                                                fact.DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == TaskGroup.EMSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial EM detect for missiles looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, fact.MissileGroups[loop].contact.Position.System, fact.MissileGroups[loop].contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        fact.MessageLog.Add(NMsg);
                                                        done = true;
                                                        break;
                                                    }
                                                    node = node.Previous;
                                                }//end while
                                            }//end for
                                        }//end else if (noDetection == false && allDetection == false)
                                    }//end if noDetection == false
                                }//end if not detected and missile can detect EM
                                #endregion

                                #region Ship Active Detection Code
                                if (System.FactionDetectionLists[fact.FactionID].Active[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.aSD != null)
                                {
                                    int ShipID = TaskGroup.ActiveSortList.Last();
                                    ShipTN scratch = TaskGroup.Ships[ShipID];
                                    sig = scratch.TotalCrossSection;

                                    detection = Missile.missileDef.aSD.GetActiveDetectionRange(sig, -1);

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = fact.LargeDetection(dist, detection);

                                    /// <summary>
                                    /// Good case, none of the ships are detected.
                                    /// </summary>
                                    if (det == false)
                                    {
                                        noDetection = true;
                                    }

                                    /// <summary>
                                    /// Atleast the biggest ship is detected.
                                    /// </summary
                                    if (noDetection == false)
                                    {
                                        ShipID = TaskGroup.ActiveSortList.First();
                                        scratch = TaskGroup.Ships[ShipID];
                                        sig = scratch.TotalCrossSection;

                                        /// <summary>
                                        /// Now for the smallest vs the best.
                                        /// </summary>
                                        detection = Missile.missileDef.aSD.GetActiveDetectionRange(sig, -1);
                                        det = fact.LargeDetection(dist, detection);

                                        /// <summary>
                                        /// Best case, everything is detected.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            allDetection = true;

                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                TaskGroup.Ships[loop3].ActiveDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                                if (fact.DetShipList.Contains(TaskGroup.Ships[loop3]) == false)
                                                {
                                                    fact.DetShipList.Add(TaskGroup.Ships[loop3]);
                                                }
                                            }
                                            System.FactionDetectionLists[fact.FactionID].Active[loop2] = GameState.Instance.CurrentSecond;
                                        }
                                        else if (noDetection == false && allDetection == false)
                                        {
                                            /// <summary>
                                            /// Worst case. some are detected, some aren't.
                                            /// </summary>
                                            for (int loop3 = 0; loop3 < TaskGroup.Ships.Count; loop3++)
                                            {
                                                LinkedListNode<int> node = TaskGroup.ActiveSortList.Last;
                                                bool done = false;

                                                while (!done)
                                                {
                                                    scratch = TaskGroup.Ships[node.Value];

                                                    if (scratch.ActiveDetection[fact.FactionID] != GameState.Instance.CurrentSecond)
                                                    {
                                                        sig = scratch.TotalCrossSection;
                                                        detection = Missile.missileDef.aSD.GetActiveDetectionRange(sig, -1);

                                                        /// <summary>
                                                        /// Test each ship until I get to one I don't see.
                                                        /// </summary>
                                                        det = fact.LargeDetection(dist, detection);

                                                        if (det == true)
                                                        {
                                                            scratch.ActiveDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                                            if (fact.DetShipList.Contains(scratch) == false)
                                                            {
                                                                fact.DetShipList.Add(scratch);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            done = true;
                                                            break;
                                                        }
                                                    }
                                                    if (node == TaskGroup.ActiveSortList.First)
                                                    {
                                                        /// <summary>
                                                        /// This should not happen.
                                                        /// </summary>
                                                        String ErrorMessage = string.Format("Partial Active detect for missiles looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, fact.MissileGroups[loop].contact.Position.System, fact.MissileGroups[loop].contact,
                                                                                             GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                                        fact.MessageLog.Add(NMsg);
                                                        done = true;
                                                        break;
                                                    }
                                                    node = node.Previous;
                                                }//end while
                                            }//end for
                                        }//end else if (noDetection == false && allDetection == false)
                                    }//end if noDetection == false
                                }//end if not detected and missile can detect TCS
                                #endregion
                                #endregion
                            }
                        }
                        else if (System.SystemContactList[loop2].SSEntity == StarSystemEntityType.Missile)
                        {
                            OrdnanceGroupTN MissileGroup = System.SystemContactList[loop2].Entity as OrdnanceGroupTN;

                            if (MissileGroup.missiles.Count != 0)
                            {

                                /// <summary>
                                /// Do Missile Detection here:
                                /// </summary>
                                #region Missile Detection
                                OrdnanceTN MissileTarget = MissileGroup.missiles[0];
                                if (System.FactionDetectionLists[fact.FactionID].Thermal[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.tHD != null)
                                {
                                    int ThermalSignature = (int)Math.Ceiling(MissileTarget.missileDef.totalThermalSignature);

                                    detection = Missile.missileDef.tHD.GetPassiveDetectionRange(ThermalSignature);

                                    /// <summary>
                                    /// Test the biggest signature against the best sensor.
                                    /// </summary>
                                    bool det = fact.LargeDetection(dist, detection);

                                    /// <summary>
                                    /// If one missile is detected, all are.
                                    /// </summary>
                                    if (det == true)
                                    {
                                        for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                        {
                                            MissileGroup.missiles[loop3].ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                        }

                                        if (fact.DetMissileList.Contains(MissileGroup) == false)
                                        {
                                            fact.DetMissileList.Add(MissileGroup);
                                        }

                                        System.FactionDetectionLists[fact.FactionID].Thermal[loop2] = GameState.Instance.CurrentSecond;
                                    }
                                }

                                if (System.FactionDetectionLists[fact.FactionID].EM[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.eMD != null)
                                {
                                    int EMSignature = 0;
                                    if (MissileTarget.missileDef.activeStr != 0.0f && MissileTarget.missileDef.aSD != null)
                                    {
                                        EMSignature = MissileTarget.missileDef.aSD.gps;
                                    }

                                    if (EMSignature != 0)
                                    {
                                        detection = Missile.missileDef.eMD.GetPassiveDetectionRange(EMSignature);

                                        bool det = fact.LargeDetection(dist, detection);

                                        /// <summary>
                                        /// If one missile is detected, all are.
                                        /// </summary>
                                        if (det == true)
                                        {
                                            for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                            {
                                                MissileGroup.missiles[loop3].EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                            }

                                            if (fact.DetMissileList.Contains(MissileGroup) == false)
                                            {
                                                fact.DetMissileList.Add(MissileGroup);
                                            }

                                            System.FactionDetectionLists[fact.FactionID].EM[loop2] = GameState.Instance.CurrentSecond;
                                        }
                                    }
                                }

                                if (System.FactionDetectionLists[fact.FactionID].Active[loop2] != GameState.Instance.CurrentSecond && Missile.missileDef.aSD != null)
                                {
                                    int TotalCrossSection_MSP = (int)Math.Ceiling(MissileTarget.missileDef.size);
                                    sig = -1;
                                    detection = -1;

                                    if (TotalCrossSection_MSP < ((Constants.OrdnanceTN.MissileResolutionMaximum + 6) + 1))
                                    {
                                        if (TotalCrossSection_MSP <= (Constants.OrdnanceTN.MissileResolutionMinimum + 6))
                                        {
                                            sig = Constants.OrdnanceTN.MissileResolutionMinimum;
                                        }
                                        if (TotalCrossSection_MSP <= (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                                        {
                                            sig = TotalCrossSection_MSP - 6;
                                        }
                                        detection = Missile.missileDef.aSD.GetActiveDetectionRange(0, sig);
                                    }
                                    else
                                    {
                                        /// <summary>
                                        /// Big missiles will be treated in HS terms: 21-40 MSP = 2 HS, 41-60 = 3 HS, 61-80 = 4 HS, 81-100 = 5 HS. The same should hold true for greater than 100 sized missiles.
                                        /// but those are impossible to build.
                                        /// </summary>
                                        sig = (int)Math.Ceiling((float)TotalCrossSection_MSP / 20.0f);
                                        detection = Missile.missileDef.aSD.GetActiveDetectionRange(sig, -1);
                                    }

                                    bool det = fact.LargeDetection(dist, detection);

                                    if (det == true)
                                    {
                                        for (int loop3 = 0; loop3 < MissileGroup.missiles.Count; loop3++)
                                        {
                                            MissileGroup.missiles[loop3].ActiveDetection[fact.FactionID] = GameState.Instance.CurrentSecond;
                                        }

                                        if (fact.DetMissileList.Contains(MissileGroup) == false)
                                        {
                                            fact.DetMissileList.Add(MissileGroup);
                                        }

                                        System.FactionDetectionLists[fact.FactionID].Active[loop2] = GameState.Instance.CurrentSecond;
                                    }
                                }
                                #endregion
                            }
                        }//end else if SSE = missile && ordnance group has missiles in it
                    }//end if contact not detected and can be detected
                }//end for faction detection list contacts
            }//end for missile groups
            #endregion



            /// <summary>
            /// Detected contacts logic. If a ship has been newly detected this tick, create a contact entry for it.
            /// Otherwise update the existing one. Messages to the message log should be handled there(at the top of this very file.
            /// if a ship is no longer detected this tick then remove it from the detected contacts list.
            /// 
            /// This can actually be improved by turning detectedContact into a linked list, and putting updated contacts in front.
            /// this way unupdated contacts would be at the end, and I would not have to loop through all ships here.
            /// </summary>
            for (int loop3 = 0; loop3 < fact.DetShipList.Count; loop3++)
            {
                ShipTN detectedShip = fact.DetShipList[loop3];
                StarSystem System = detectedShip.ShipsTaskGroup.Contact.Position.System;

                /// <summary>
                /// Sanity check to keep allied ships out of the DetectedContacts list.
                /// </summary>
                if (detectedShip.ShipsFaction != fact)
                {
                    bool inDict = fact.DetectedContactLists.ContainsKey(System);

                    if (inDict == false)
                    {
                        DetectedContactsList newDCL = new DetectedContactsList();
                        fact.DetectedContactLists.Add(System, newDCL);
                    }

                    inDict = fact.DetectedContactLists[System].DetectedContacts.ContainsKey(detectedShip);

                    bool th = (detectedShip.ThermalDetection[fact.FactionID] == GameState.Instance.CurrentSecond);
                    bool em = (detectedShip.EMDetection[fact.FactionID] == GameState.Instance.CurrentSecond);
                    bool ac = (detectedShip.ActiveDetection[fact.FactionID] == GameState.Instance.CurrentSecond);

                    if (inDict == true)
                    {
                        int EMSig = -1;
                        if (em == true)
                        {
                            EMSig = detectedShip.CurrentEMSignature;
                        }

                        fact.DetectedContactLists[System].DetectedContacts[detectedShip].updateFactionContact(fact, th, em, EMSig, ac, (uint)GameState.Instance.CurrentSecond);

                        if (th == false && em == false && ac == false)
                        {
                            fact.DetectedContactLists[System].DetectedContacts.Remove(detectedShip);
                        }
                    }
                    else if (inDict == false && (th == true || em == true || ac == true))
                    {
                        int EMSig = -1;
                        if (em == true)
                        {
                            EMSig = detectedShip.CurrentEMSignature;
                        }

                        FactionContact newContact = new FactionContact(fact, detectedShip, th, em, EMSig, ac, (uint)GameState.Instance.CurrentSecond);
                        fact.DetectedContactLists[System].DetectedContacts.Add(detectedShip, newContact);
                    }
                }
            }

            for (int loop3 = 0; loop3 < fact.DetMissileList.Count; loop3++)
            {
                OrdnanceTN Missile = fact.DetMissileList[loop3].missiles[0];
                StarSystem System = fact.DetMissileList[loop3].contact.Position.System;

                /// <summary>
                /// Sanity check to keep allied missiles out of the DetectedContacts list.
                /// </summary>
                if (Missile.missileGroup.ordnanceGroupFaction != fact)
                {
                    bool inDict = fact.DetectedContactLists.ContainsKey(System);

                    if (inDict == false)
                    {
                        DetectedContactsList newDCL = new DetectedContactsList();
                        fact.DetectedContactLists.Add(System, newDCL);
                    }

                    inDict = fact.DetectedContactLists[System].DetectedMissileContacts.ContainsKey(Missile.missileGroup);

                    bool th = (Missile.ThermalDetection[fact.FactionID] == GameState.Instance.CurrentSecond);
                    bool em = (Missile.EMDetection[fact.FactionID] == GameState.Instance.CurrentSecond);
                    bool ac = (Missile.ActiveDetection[fact.FactionID] == GameState.Instance.CurrentSecond);

                    if (inDict == true)
                    {
                        int EMSig = 0;
                        if (em == true)
                        {
                            EMSig = Missile.missileDef.aSD.gps;
                        }
                        fact.DetectedContactLists[System].DetectedMissileContacts[Missile.missileGroup].updateFactionContact(fact, th, em, EMSig, ac, (uint)GameState.Instance.CurrentSecond);

                        if (th == false && em == false && ac == false)
                        {
                            fact.DetectedContactLists[System].DetectedMissileContacts.Remove(Missile.missileGroup);
                        }
                    }
                    else if (inDict == false && (th == true || em == true || ac == true))
                    {
                        FactionContact newContact = new FactionContact(fact, Missile.missileGroup, th, em, ac, (uint)GameState.Instance.CurrentSecond);
                        fact.DetectedContactLists[System].DetectedMissileContacts.Add(Missile.missileGroup, newContact);
                    }
                }
            }

            for (int loop3 = 0; loop3 < fact.DetPopList.Count; loop3++)
            {
                Population CurrentPopulation = fact.DetPopList[loop3];
                StarSystem System = fact.DetPopList[loop3].Contact.Position.System;

                if (CurrentPopulation.Faction != fact)
                {
                    bool inDict = fact.DetectedContactLists.ContainsKey(System);

                    if (inDict == false)
                    {
                        DetectedContactsList newDCL = new DetectedContactsList();
                        fact.DetectedContactLists.Add(System, newDCL);
                    }

                    inDict = fact.DetectedContactLists[System].DetectedPopContacts.ContainsKey(CurrentPopulation);

                    bool th = (CurrentPopulation.ThermalDetection[fact.FactionID] == GameState.Instance.CurrentSecond);
                    bool em = (CurrentPopulation.EMDetection[fact.FactionID] == GameState.Instance.CurrentSecond);
                    bool ac = (CurrentPopulation.ActiveDetection[fact.FactionID] == GameState.Instance.CurrentSecond);

                    if (inDict == true)
                    {
                        fact.DetectedContactLists[System].DetectedPopContacts[CurrentPopulation].updateFactionContact(fact, th, em, CurrentPopulation.EMSignature, ac, (uint)GameState.Instance.CurrentSecond);

                        if (th == false && em == false && ac == false)
                        {
                            fact.DetectedContactLists[System].DetectedPopContacts.Remove(CurrentPopulation);
                        }
                    }
                    else if (inDict == false && (th == true || em == true || ac == true))
                    {
                        FactionContact newContact = new FactionContact(fact, CurrentPopulation, th, em, ac, (uint)GameState.Instance.CurrentSecond);
                        fact.DetectedContactLists[System].DetectedPopContacts.Add(CurrentPopulation, newContact);
                    }
                }
            }
        }


        #region Detection Code
        /// <summary>
        /// TaskGroupThermal Detection runs the thermal detection routine on TaskGroupToTest from CurrentTaskGroup
        /// </summary>
        /// <param name="System">Current System this is taking place in.</param>
        /// <param name="CurrentTaskGroup">TaskGroup that is performing the sensor sweep.</param>
        /// <param name="TaskGroupToTest">TaskGroup being tested against.</param>
        /// <param name="dist">Distance between these two taskgroups.</param>
        /// <param name="detListIterator">Iterator for where the TaskGroupToTest is in the various detection lists and the system contact list.</param>
        /// <param name="DetShipList">If a ship is detected it must be put into this list for the detectedContactsList later on.</param>
        private static void TaskGroupThermalDetection(Faction fact, StarSystem System, TaskGroupTN CurrentTaskGroup, TaskGroupTN TaskGroupToTest, float dist, int detListIterator)
        {
            if (System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] != GameState.Instance.CurrentSecond)
            {
                int sig = -1;
                int detection = -1;
                bool noDetection = false;
                bool allDetection = false;
                /// <summary>
                /// Get the best detection range for thermal signatures in loop.
                /// </summary>
                int ShipID = TaskGroupToTest.ThermalSortList.Last();
                ShipTN scratch = TaskGroupToTest.Ships[ShipID];
                sig = scratch.CurrentThermalSignature;

                /// <summary>
                /// Check to make sure the taskgroup has a thermal sensor available, otherwise use the default.
                /// </summary>
                if (CurrentTaskGroup.BestThermalCount != 0)
                {
                    detection = CurrentTaskGroup.BestThermal.pSensorDef.GetPassiveDetectionRange(sig);
                }
                else
                {
                    detection = fact.ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                }

                /// <summary>
                /// Test the biggest signature against the best sensor.
                /// </summary>
                bool det = fact.LargeDetection(dist, detection);

                /// <summary>
                /// Good case, none of the ships are detected.
                /// </summary>
                if (det == false)
                {
                    noDetection = true;
                }

                /// <summary>
                /// Atleast the biggest ship is detected.
                /// </summary
                if (noDetection == false)
                {
                    ShipID = TaskGroupToTest.ThermalSortList.First();
                    scratch = TaskGroupToTest.Ships[ShipID];
                    sig = scratch.CurrentThermalSignature;

                    /// <summary>
                    /// Check to make sure the taskgroup has a thermal sensor available, otherwise use the default.
                    /// </summary>
                    if (CurrentTaskGroup.BestThermalCount != 0)
                    {
                        detection = CurrentTaskGroup.BestThermal.pSensorDef.GetPassiveDetectionRange(sig);
                    }
                    else
                    {
                        detection = fact.ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                    }

                    /// <summary>
                    /// Now for the smallest vs the best.
                    /// </summary>
                    det = fact.LargeDetection(dist, detection);

                    /// <summary>
                    /// Best case, everything is detected.
                    /// </summary>
                    if (det == true)
                    {
                        allDetection = true;

                        for (int loop3 = 0; loop3 < TaskGroupToTest.Ships.Count; loop3++)
                        {
                            TaskGroupToTest.Ships[loop3].ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                            if (fact.DetShipList.Contains(TaskGroupToTest.Ships[loop3]) == false)
                            {
                                fact.DetShipList.Add(TaskGroupToTest.Ships[loop3]);
                            }
                        }
                        System.FactionDetectionLists[fact.FactionID].Thermal[detListIterator] = GameState.Instance.CurrentSecond;


                    }
                    else if (noDetection == false && allDetection == false)
                    {
                        /// <summary>
                        /// Worst case. some are detected, some aren't.
                        /// </summary>

                        for (int loop3 = 0; loop3 < TaskGroupToTest.Ships.Count; loop3++)
                        {
                            LinkedListNode<int> node = TaskGroupToTest.ThermalSortList.Last;
                            bool done = false;

                            while (!done)
                            {
                                scratch = TaskGroupToTest.Ships[node.Value];

                                if (scratch.ThermalDetection[fact.FactionID] != GameState.Instance.CurrentSecond)
                                {
                                    sig = scratch.CurrentThermalSignature;
                                    if (CurrentTaskGroup.BestThermalCount != 0)
                                    {
                                        detection = CurrentTaskGroup.BestThermal.pSensorDef.GetPassiveDetectionRange(sig);
                                    }
                                    else
                                    {
                                        detection = fact.ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                    }

                                    /// <summary>
                                    /// Test each ship until I get to one I don't see.
                                    /// </summary>
                                    det = fact.LargeDetection(dist, detection);

                                    if (det == true)
                                    {
                                        scratch.ThermalDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                        if (fact.DetShipList.Contains(scratch) == false)
                                        {
                                            fact.DetShipList.Add(scratch);
                                        }
                                    }
                                    else
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                if (node == TaskGroupToTest.ThermalSortList.First)
                                {
                                    /// <summary>
                                    /// This should not happen.
                                    /// </summary>
                                    String ErrorMessage = string.Format("Partial Thermal detect for TGs looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);
                                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, TaskGroupToTest.Contact.Position.System, TaskGroupToTest.Contact,
                                                                         GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                    fact.MessageLog.Add(NMsg);
                                    done = true;
                                    break;
                                }
                                node = node.Previous;
                            }
                        }
                    }
                    /// <summary>
                    /// End else
                    /// </summary>
                }
            }
        }

        /// <summary>
        /// TaskGroupEM Detection runs the EM detection routine on TaskGroupToTest from CurrentTaskGroup. This differs from Thermal in that we use the ship EM linked list as well as the faction EM
        /// Detection list.
        /// </summary>
        /// <param name="System">Current System this is taking place in.</param>
        /// <param name="CurrentTaskGroup">TaskGroup that is performing the sensor sweep.</param>
        /// <param name="TaskGroupToTest">TaskGroup being tested against.</param>
        /// <param name="dist">Distance between these two taskgroups.</param>
        /// <param name="detListIterator">Iterator for where the TaskGroupToTest is in the various detection lists and the system contact list.</param>
        /// <param name="DetShipList">If a ship is detected it must be put into this list for the detectedContactsList later on.</param>
        private static void TaskGroupEMDetection(Faction fact, StarSystem System, TaskGroupTN CurrentTaskGroup, TaskGroupTN TaskGroupToTest, float dist, int detListIterator)
        {
            if (System.FactionDetectionLists[fact.FactionID].EM[detListIterator] != GameState.Instance.CurrentSecond)
            {
                int sig = -1;
                int detection = -1;
                bool noDetection = false;
                bool allDetection = false;

                /// <summary>
                /// Get the best detection range for EM signatures in loop.
                /// </summary>
                int ShipID = TaskGroupToTest.EMSortList.Last();
                ShipTN scratch = TaskGroupToTest.Ships[ShipID];
                sig = scratch.CurrentEMSignature;

                /// <summary>
                /// Check to see if the taskgroup has an em sensor, and that said em sensor is not destroyed.
                /// otherwise use the default passive detection range.
                /// </summary>
                if (CurrentTaskGroup.BestEMCount > 0)
                {
                    detection = CurrentTaskGroup.BestEM.pSensorDef.GetPassiveDetectionRange(sig);
                }
                else
                {
                    detection = fact.ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                }

                bool det = fact.LargeDetection(dist, detection);

                /// <summary>
                /// Good case, none of the ships are detected.
                /// </summary>
                if (det == false)
                {
                    noDetection = true;
                }

                /// <summary>
                /// Atleast the biggest ship is detected.
                /// </summary
                if (noDetection == false)
                {
                    ShipID = TaskGroupToTest.EMSortList.First();
                    scratch = TaskGroupToTest.Ships[ShipID];
                    sig = scratch.CurrentEMSignature;

                    /// <summary>
                    /// once again we must check here to make sure that the taskgroup does have a passive suite, or else use the default one.
                    /// </summary>
                    if (CurrentTaskGroup.BestEMCount > 0)
                    {
                        detection = CurrentTaskGroup.BestEM.pSensorDef.GetPassiveDetectionRange(sig);
                    }
                    else
                    {
                        detection = fact.ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                    }

                    det = fact.LargeDetection(dist, detection);

                    /// <summary>
                    /// Best case, everything is detected.
                    /// </summary>
                    if (det == true)
                    {
                        allDetection = true;

                        for (int loop3 = 0; loop3 < TaskGroupToTest.Ships.Count; loop3++)
                        {
                            TaskGroupToTest.Ships[loop3].EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                            if (fact.DetShipList.Contains(TaskGroupToTest.Ships[loop3]) == false)
                            {
                                fact.DetShipList.Add(TaskGroupToTest.Ships[loop3]);
                            }
                        }
                        System.FactionDetectionLists[fact.FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                    }
                    else if (noDetection == false && allDetection == false)
                    {
                        /// <summary>
                        /// Worst case. some are detected, some aren't.
                        /// </summary>


                        for (int loop3 = 0; loop3 < TaskGroupToTest.Ships.Count; loop3++)
                        {
                            LinkedListNode<int> node = TaskGroupToTest.EMSortList.Last;
                            bool done = false;

                            while (!done)
                            {
                                scratch = TaskGroupToTest.Ships[node.Value];

                                if (scratch.EMDetection[fact.FactionID] != GameState.Instance.CurrentSecond)
                                {
                                    sig = scratch.CurrentEMSignature;

                                    /// <summary>
                                    /// here is where EM detection differs from Thermal detection:
                                    /// If a ship has a signature of 0 by this point(and we didn't already hit noDetection above,
                                    /// it means that one ship is emitting a signature, but that no other ships are.
                                    /// Mark the group as totally detected, but not the ships, this serves to tell me that the ships are undetectable
                                    /// in this case.
                                    /// </summary>
                                    if (sig == 0)
                                    {
                                        /// <summary>
                                        /// The last signature we looked at was the ship emitting an EM sig, and this one is not.
                                        /// Mark the entire group as "spotted" because no other detection will occur.
                                        /// </summary>
                                        if (TaskGroupToTest.Ships[node.Next.Value].EMDetection[fact.FactionID] == GameState.Instance.CurrentSecond)
                                        {
                                            System.FactionDetectionLists[fact.FactionID].EM[detListIterator] = GameState.Instance.CurrentSecond;
                                        }
                                        break;
                                    }

                                    if (CurrentTaskGroup.BestEMCount > 0)
                                    {
                                        detection = CurrentTaskGroup.BestEM.pSensorDef.GetPassiveDetectionRange(sig);
                                    }
                                    else
                                    {
                                        detection = fact.ComponentList.DefaultPassives.GetPassiveDetectionRange(sig);
                                    }

                                    det = fact.LargeDetection(dist, detection);

                                    if (det == true)
                                    {
                                        scratch.EMDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                        if (fact.DetShipList.Contains(scratch) == false)
                                        {
                                            fact.DetShipList.Add(scratch);
                                        }
                                    }
                                    else
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                if (node == TaskGroupToTest.EMSortList.First)
                                {
                                    /// <summary>
                                    /// This should not happen.
                                    /// </summary>
                                    String ErrorMessage = string.Format("Partial EM detect for TGs looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);

                                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, CurrentTaskGroup.Contact.Position.System, CurrentTaskGroup.Contact,
                                                                         GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                    fact.MessageLog.Add(NMsg);
                                    done = true;
                                    break;
                                }
                                node = node.Previous;
                            }
                        }
                    }
                    /// <summary>
                    /// End else
                    /// </summary>
                }
            }
        }

        /// <summary>
        /// Taskgroup active detection will handle the active detection sensor sweeps, Resolution represents the biggest difference for TG Active detection vs passive detection.
        /// There isn't simply a best active sensor, and the correct one has to be searched for. luckily that work is already done by the taskgroup itself.
        /// </summary>
        /// <param name="System">Current System.</param>
        /// <param name="CurrentTaskGroup">Scanning Taskgroup</param>
        /// <param name="TaskGroupToTest">Taskgroup to test if detected.</param>
        /// <param name="dist">Distance between the two taskgroups</param>
        /// <param name="detListIterator">location of the test taskgroup in the detection lists</param>
        private static void TaskGroupActiveDetection(Faction fact, StarSystem System, TaskGroupTN CurrentTaskGroup, TaskGroupTN TaskGroupToTest, float dist, int detListIterator)
        {
            if (System.FactionDetectionLists[fact.FactionID].Active[detListIterator] != GameState.Instance.CurrentSecond && CurrentTaskGroup.ActiveSensorQue.Count > 0)
            {
                int sig = -1;
                int detection = -1;
                bool noDetection = false;
                bool allDetection = false;

                /// <summary>
                /// Get the best detection range for thermal signatures in loop.
                /// </summary>
                int ShipID = TaskGroupToTest.ActiveSortList.Last();
                ShipTN scratch = TaskGroupToTest.Ships[ShipID];
                sig = scratch.TotalCrossSection - 1;

                if (sig > Constants.ShipTN.ResolutionMax - 1)
                    sig = Constants.ShipTN.ResolutionMax - 1;

                detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                bool det = fact.LargeDetection(dist, detection);

                /// <summary>
                /// Good case, none of the ships are detected.
                /// </summary>
                if (det == false)
                {
                    noDetection = true;
                }

                /// <summary>
                /// Atleast the biggest ship is detected.
                /// </summary
                if (noDetection == false)
                {
                    ShipID = TaskGroupToTest.ActiveSortList.First();
                    scratch = TaskGroupToTest.Ships[ShipID];
                    sig = scratch.TotalCrossSection - 1;

                    if (sig > Constants.ShipTN.ResolutionMax - 1)
                        sig = Constants.ShipTN.ResolutionMax - 1;

                    detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                    det = fact.LargeDetection(dist, detection);

                    /// <summary>
                    /// Best case, everything is detected.
                    /// </summary>
                    if (det == true)
                    {
                        allDetection = true;

                        for (int loop3 = 0; loop3 < TaskGroupToTest.Ships.Count; loop3++)
                        {
                            TaskGroupToTest.Ships[loop3].ActiveDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                            if (fact.DetShipList.Contains(TaskGroupToTest.Ships[loop3]) == false)
                            {
                                fact.DetShipList.Add(TaskGroupToTest.Ships[loop3]);
                            }
                        }
                        /// <summary>
                        /// FactionSystemDetection entry. I hope to deprecate this at some point.
                        /// Be sure to erase the factionDetectionSystem entry first, to track down everywhere this overbloated thing is.
                        /// update, not happening. FactionDetectionList is too important.
                        /// </summary>
                        System.FactionDetectionLists[fact.FactionID].Active[detListIterator] = GameState.Instance.CurrentSecond;
                    }
                    else if (noDetection == false && allDetection == false)
                    {
                        /// <summary>
                        /// Worst case. some are detected, some aren't.
                        /// </summary>


                        for (int loop3 = 0; loop3 < TaskGroupToTest.Ships.Count; loop3++)
                        {
                            LinkedListNode<int> node = TaskGroupToTest.ActiveSortList.Last;
                            bool done = false;

                            while (!done)
                            {
                                scratch = TaskGroupToTest.Ships[node.Value];

                                if (scratch.ActiveDetection[fact.FactionID] != GameState.Instance.CurrentSecond)
                                {
                                    sig = scratch.TotalCrossSection - 1;

                                    if (sig > Constants.ShipTN.ResolutionMax - 1)
                                        sig = Constants.ShipTN.ResolutionMax - 1;

                                    detection = CurrentTaskGroup.ActiveSensorQue[CurrentTaskGroup.TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                                    det = fact.LargeDetection(dist, detection);

                                    if (det == true)
                                    {
                                        scratch.ActiveDetection[fact.FactionID] = GameState.Instance.CurrentSecond;

                                        if (fact.DetShipList.Contains(scratch) == false)
                                        {
                                            fact.DetShipList.Add(scratch);
                                        }
                                    }
                                    else
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                if (node == TaskGroupToTest.ActiveSortList.First)
                                {
                                    /// <summary>
                                    /// This should not happen.
                                    /// </summary>
                                    String ErrorMessage = string.Format("Partial Active detect for TGs looped through every ship. {0} {1} {2} {3}", dist, detection, noDetection, allDetection);

                                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.Error, CurrentTaskGroup.Contact.Position.System, CurrentTaskGroup.Contact,
                                                                         GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, ErrorMessage);
                                    fact.MessageLog.Add(NMsg);
                                    done = true;
                                    break;
                                }
                                node = node.Previous;
                            }
                        }
                    }
                    /// <summary>
                    /// End else
                    /// </summary>
                }
            }
        }
        #endregion
    }
}
