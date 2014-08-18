using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;
using System.Drawing;
using System.Collections.ObjectModel;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Simple helper class for the sub pulse handler.
    /// </summary>
    public class SubPulseTimeList
    {
        public int TimeInSeconds { get; set; }
        public int SubPulses { get; set; }

        public SubPulseTimeList(int Time, int Sub)
        {
            TimeInSeconds = Time;
            SubPulses = Sub;
        }
    }

    public class SimEntity
    {
        public int factionStart { get; set; }
        public int factionCount { get; set; }
        public int TGStart { get; set; }
        public int TGCount { get; set; }
        public int CurrentTick { get; set; }
        public int lastTick { get; set; }
        public bool SimCreated { get; set; }

        /// <summary>
        /// Does the potential for a fleet interception event exist this tick? if this is set to currentTick the answer is true
        /// </summary>
        public int FleetInterceptionPreemptTick { get; set; }

        /// <summary>
        /// this is the list of fleets that have caused a fleet intercept preempt this tick.
        /// </summary>
        public BindingList<TaskGroupTN> FleetInterceptPreemptList { get; set; }

        /// <summary>
        /// if this is set to current Tick, then missile time to hit is valid.
        /// </summary>
        public int MissileInterceptPreemptTick { get; set; }

        /// <summary>
        /// How long until the next missile hits? how far should simentity allow the game to progress?
        /// </summary>
        public int MissileTimeToHit { get; set; }

        /// <summary>
        /// List of standard times in seconds, paired with what the subpulse should be and how many of them there are.
        /// </summary>
        public Dictionary<int,SubPulseTimeList> SubPulse { get; set; }

        /// <summary>
        /// Clears the fleet preempt list.
        /// </summary>
        public void ClearFleetPreemptList()
        {
            FleetInterceptPreemptList.Clear();
        }

        /// <summary>
        /// Adds a TG to the fleet intercept preempt list.
        /// </summary>
        /// <param name="TG">taskgroup to be added</param>
        public void AddFleetToPreemptList(TaskGroupTN TG)
        {
            FleetInterceptPreemptList.Add(TG);
        }


        /// <summary>
        /// Subpulse handler will decide what the subpulse/time setting should be.
        /// </summary>
        /// <param name="P">List of factions that will be passed to advanceSim</param>
        /// <param name="RNG">RNG that will be passed to advanceSim</param>
        /// <param name="tickValue">user entered time value, may bear no resemblance to what actually happens however.</param>
        /// <returns>time in seconds that the subpulse handler processes.</returns>
        public int SubpulseHandler(BindingList<Faction> P, Random RNG, int tickValue)
        {
#warning todo: Determine fleet interception, check fire controls, jump transits into new systems, completed orders.
            /// <summary>
            /// right now all subpulses are doing is giving multiple finer time slices rather than one large time slice. interruptions are not yet handled.
            /// I want to interrupt not only the sub pulse timer, but also the loop when appropriate.
            /// Jumps to unknown systems will have to be a list, probably here in SE.
            /// fire controls I already have, I just need to check for recharged weapons here.
            /// Completed orders should again be a list. how should order failure be handled?
            /// Fleet interception, in the sensor model?
            /// Fleet Interception Preempt: are fleets within 1 days travel time of each other.
            /// Sensor detection preempt: try to find exact sensor detection time.
            /// </summary>

            /// <summary>
            /// How much time should pass? this can be modified by a missile event.
            /// </summary>
            int desiredTime = tickValue;

            /// <summary>
            /// how much time has passed?
            /// </summary>
            int elapsedTime = 0;

            /// <summary>
            /// Last game tick I found that a fleet was within 5 days travel time of another factions fleet.
            /// </summary>
            if (FleetInterceptionPreemptTick == CurrentTick)
            {
                if (tickValue >= Constants.TimeInSeconds.Day)
                {
                    tickValue = (int)Constants.TimeInSeconds.Day;

#warning this goes in the SM Log.
                    String Entry = String.Format("Subpulse shortened due to potential fleet interception. This should go in the SM Log when that exists.");
                    MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.PotentialFleetInterception, null, null, GameState.Instance.GameDateTime,
                                                       (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                    GameState.Instance.Factions[0].MessageLog.Add(Msg);
                }
            }

            /// <summary>
            /// Default subpulse data, how many seconds should be advanced for each Pulses iteration of the loop.
            /// </summary>
            int AdvanceTime = SubPulse[tickValue].TimeInSeconds;
            int Pulses = SubPulse[tickValue].SubPulses;


            /// <summary>
            /// Sensor pre-empt check.
            /// I need to get the best range active(passives are already done. use sensor model code?
            /// Need to calculate distance to traverse each others sensor bubble? or distance to sensor bubble edge?
            /// also make sure already detected ships aren't here.
            /// All detection methods have to be checked for: thermal, EM, and active.
            /// </summary>

            /// <summary>
            /// A missile intercept preemption event has been detected.
            /// </summary>
            if (MissileInterceptPreemptTick == CurrentTick)
            {
                if (MissileTimeToHit <= tickValue)
                {
                    /// <summary>
                    /// How many 5 second ticks until this missile hits?
                    /// </summary>
                    int FiveSecondIncrements = (int)Math.Floor((float)MissileTimeToHit / 5.0f);
                    desiredTime = FiveSecondIncrements * 5;

                    /// <summary>
                    /// I want to pause right before the missile hits.
                    /// </summary>
                    if (desiredTime == MissileTimeToHit && desiredTime != (int)Constants.TimeInSeconds.FiveSeconds)
                    {
                        desiredTime = desiredTime - 5;
                    }

                    PreemptCheck(desiredTime, out AdvanceTime, out Pulses);
                }
            }

            int FCInterruptTime = -1;
            bool done = false;

            /// <summary>
            /// no point in calculating the subpulse if we're at 5 seconds.
            /// </summary>
            if (desiredTime != (int)Constants.TimeInSeconds.FiveSeconds)
            {
                /// <summary>
                /// Check if a weapon attached to a fire controller is about to be ready to fire.
                /// </summary>
                for (int loop = 0; loop < P.Count; loop++)
                {
                    foreach (KeyValuePair<ComponentTN, bool> pair in P[loop].OpenFireFCType)
                    {
                        /// <summary>
                        /// BFC
                        /// </summary>
                        if (pair.Value == false)
                        {
                            for (int loop2 = 0; loop2 < P[loop].OpenFireFC[pair.Key].ShipBFC[pair.Key.componentIndex].linkedWeapons.Count; loop2++)
                            {
                                if (P[loop].OpenFireFC[pair.Key].ShipBFC[pair.Key.componentIndex].linkedWeapons[loop2].readyToFire() == true)
                                {
                                    FCInterruptTime = (int)Constants.TimeInSeconds.FiveSeconds;
                                    done = true;
                                    break;
                                }
                                else if (FCInterruptTime != ((int)Constants.TimeInSeconds.FiveSeconds * 2))
                                {
                                    FCInterruptTime = P[loop].OpenFireFC[pair.Key].ShipBFC[pair.Key.componentIndex].linkedWeapons[loop2].timeToFire();
                                }
                            }

                            if (done == true)
                                break;

                            for (int loop2 = 0; loop2 < P[loop].OpenFireFC[pair.Key].ShipBFC[pair.Key.componentIndex].linkedTurrets.Count; loop2++)
                            {
                                if (P[loop].OpenFireFC[pair.Key].ShipBFC[pair.Key.componentIndex].linkedTurrets[loop2].readyToFire() == true)
                                {
                                    FCInterruptTime = (int)Constants.TimeInSeconds.FiveSeconds;
                                    done = true;
                                    break;
                                }
                                else if (FCInterruptTime != ((int)Constants.TimeInSeconds.FiveSeconds * 2))
                                {
                                    FCInterruptTime = P[loop].OpenFireFC[pair.Key].ShipBFC[pair.Key.componentIndex].linkedTurrets[loop2].timeToFire();
                                }
                            }

                            if (done == true)
                                break;
                        }
                        /// <summary>
                        /// MFC
                        /// </summary>
                        else if (pair.Value == true)
                        {
                            for (int loop2 = 0; loop2 < P[loop].OpenFireFC[pair.Key].ShipMFC[pair.Key.componentIndex].linkedWeapons.Count; loop2++)
                            {
                                if (P[loop].OpenFireFC[pair.Key].ShipMFC[pair.Key.componentIndex].linkedWeapons[loop2].readyToFire() == true)
                                {
                                    FCInterruptTime = (int)Constants.TimeInSeconds.FiveSeconds;
                                    done = true;
                                    break;
                                }
                                else if (FCInterruptTime != ((int)Constants.TimeInSeconds.FiveSeconds * 2))
                                {
                                    FCInterruptTime = P[loop].OpenFireFC[pair.Key].ShipMFC[pair.Key.componentIndex].linkedWeapons[loop2].timeToFire();
                                }
                            }

                            if (done == true)
                                break;
                        }
                    }

                    if (done == true)
                        break;
                }

                if (FCInterruptTime != -1 && FCInterruptTime < desiredTime)
                {
                    desiredTime = FCInterruptTime;
                    PreemptCheck(desiredTime, out AdvanceTime, out Pulses);
                }
            }

            /// <summary>
            /// missile intercepts need a way of stepping down the sub pulse and advance time values, hence this loop.
            /// </summary>
            while (elapsedTime != desiredTime)
            {
                for (int loop = 0; loop < Pulses; loop++)
                {
                    AdvanceSim(P, RNG, AdvanceTime);

                    elapsedTime = elapsedTime + AdvanceTime;

                    /// <summary>
                    /// after advance sim is a missile intercept still in progress?
                    if (MissileInterceptPreemptTick == CurrentTick)
                    {
                        bool test = PreemptCheck((desiredTime - elapsedTime), out AdvanceTime, out Pulses);

                        /// <summary>
                        /// better just get out of here if this is ever false.
                        /// </summary>
                        if (test == false)
                        {
                            return elapsedTime;
                        }
                    }
                    /// <summary>
                    /// after running advance sim we find a potential fleet interception event occurred.
                    /// </summary>
                    else if (FleetInterceptionPreemptTick == CurrentTick)
                    {
#warning this goes in the SM Log.
                        String Entry2 = String.Format("Subpulse shortened due to potential fleet interception. This should go in the SM Log when that exists.");
                        MessageEntry Msg2 = new MessageEntry(MessageEntry.MessageType.PotentialFleetInterception, null, null, GameState.Instance.GameDateTime,
                                                           (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry2);
                        GameState.Instance.Factions[0].MessageLog.Add(Msg2);

                        return elapsedTime;
                    }
                }
            }
            return elapsedTime;
        }


        /// <summary>
        /// What sub pulse time slice best serves the DesiredTime requirement?
        /// </summary>
        /// <param name="DesiredTime">Time I want to advance. This should be in seconds, but will always be divisible by 5. It should already be slightly less than missile impact time.</param>
        /// <param name="Advance">subpulse length</param>
        /// <param name="Pulse">subpulses</param>
        /// <returns>If we handled DesiredTime correctly or not.</returns>
        public bool PreemptCheck(int DesiredTime, out int Advance, out int Pulse)
        {
            int FiveSecondIncrements = DesiredTime / 5;

            if (DesiredTime == Constants.TimeInSeconds.FiveSeconds)
            {
                Advance = (int)Constants.TimeInSeconds.FiveSeconds;
                Pulse = FiveSecondIncrements;
            }
            else if (DesiredTime <= Constants.TimeInSeconds.ThirtySeconds)
            {
                Advance = (int)Constants.TimeInSeconds.FiveSeconds;
                Pulse = FiveSecondIncrements;
            }
            else if (DesiredTime <= Constants.TimeInSeconds.TwoMinutes)
            {
                Advance = (int)Constants.TimeInSeconds.ThirtySeconds;
                Pulse = (int)Math.Floor((float)DesiredTime / (int)Constants.TimeInSeconds.ThirtySeconds);
            }
            else if (DesiredTime <= Constants.TimeInSeconds.FiveMinutes)
            {
                Advance = (int)Constants.TimeInSeconds.Minute;
                Pulse = (int)Math.Floor((float)DesiredTime / (int)Constants.TimeInSeconds.Minute);
            }
            else if (DesiredTime <= Constants.TimeInSeconds.TwentyMinutes)
            {
                Advance = (int)Constants.TimeInSeconds.FiveMinutes;
                Pulse = (int)Math.Floor((float)DesiredTime / (int)Constants.TimeInSeconds.FiveMinutes);
            }
            else if (DesiredTime <= Constants.TimeInSeconds.Hour)
            {
                Advance = (int)Constants.TimeInSeconds.TwentyMinutes;
                Pulse = (int)Math.Floor((float)DesiredTime / (int)Constants.TimeInSeconds.TwentyMinutes);
            }
            else if (DesiredTime <= Constants.TimeInSeconds.ThreeHours)
            {
                Advance = (int)Constants.TimeInSeconds.Hour;
                Pulse = (int)Math.Floor((float)DesiredTime / (int)Constants.TimeInSeconds.Hour);
            }
            else if (DesiredTime <= Constants.TimeInSeconds.EightHours)
            {
                Advance = (int)(Constants.TimeInSeconds.Hour * 2);
                Pulse = (int)Math.Floor((float)DesiredTime / (int)(Constants.TimeInSeconds.Hour * 2));
            }
            else if (DesiredTime <= Constants.TimeInSeconds.Day)
            {
                Advance = (int)(Constants.TimeInSeconds.EightHours);
                Pulse = (int)Math.Floor((float)DesiredTime / (int)Constants.TimeInSeconds.EightHours);
            }
            else if (DesiredTime <= Constants.TimeInSeconds.FiveDays)
            {
                Advance = (int)(Constants.TimeInSeconds.Day);
                Pulse = (int)Math.Floor((float)DesiredTime / (int)Constants.TimeInSeconds.Day);
            }
            else if (DesiredTime <= Constants.TimeInSeconds.Month)
            {
                Advance = (int)(Constants.TimeInSeconds.FiveDays);
                Pulse = (int)Math.Floor((float)DesiredTime / (int)Constants.TimeInSeconds.FiveDays);
            }
            else
            {
                /// <summary>
                /// this should not happen.
                /// </summary>
#warning SM log this
                String Entry = String.Format("Subpulse Error with desiredTime {0}, sub pulse set to 5 seconds. This should go in the SM log eventually.",DesiredTime);
                MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.Error, null, null, GameState.Instance.GameDateTime,
                                                   (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                GameState.Instance.Factions[0].MessageLog.Add(Msg);

                Advance = (int)Constants.TimeInSeconds.FiveSeconds;
                Pulse = 1;

                return false;
            }

            return true;
        }

        

        /// <summary>
        /// AdvanceSim is a more general pulsar simulation than runsim. This is the Current Time advancement function in Pulsar 4X
        /// </summary>
        /// <param name="P"></param>
        /// <param name="RNG"></param>
        /// <param name="tickValue"></param>
        public void AdvanceSim(BindingList<Faction> P, Random RNG, int tickValue)
        {
            if (CurrentTick > 1000000000)
            {
                CurrentTick = CurrentTick - 1000000000;
            }
            lastTick = CurrentTick;
            CurrentTick += tickValue;

            /// <summary>
            /// Missiles should check to see if they have a target, move towards it, and hit it. If they have no target then they should check their sensor and either move to new target,
            /// or more towards last known firing location. ProcessOrder should handle all of these.
            /// </summary>
            for (int loop = factionStart; loop < factionCount; loop++)
            {
                for (int loop2 = 0; loop2 < P[loop].MissileGroups.Count; loop2++)
                {
                    P[loop].MissileGroups[loop2].ProcessOrder((uint)(CurrentTick - lastTick), RNG);

                    /// <summary>
                    /// Handle missile interception sub pulse preemption here.
                    /// </summary>
                    if (MissileInterceptPreemptTick != CurrentTick)
                    {
                        MissileInterceptPreemptTick = CurrentTick;
                        MissileTimeToHit = (int)P[loop].MissileGroups[loop2].timeReq;
                    }
                    else if (MissileInterceptPreemptTick == CurrentTick && P[loop].MissileGroups[loop2].timeReq < MissileTimeToHit)
                    {
                        MissileTimeToHit = (int)P[loop].MissileGroups[loop2].timeReq;
                    }


                    if (P[loop].MissileGroups[loop2].missilesDestroyed != 0 && P[loop].MissileRemoveList.Contains(P[loop].MissileGroups[loop2]) == false )
                    {

                        switch (P[loop].MissileGroups[loop2].missiles[0].target.targetType)
                        {
#warning should any planet/pop stuff be taken care of here?
                            case StarSystemEntityType.TaskGroup:
                                ShipTN MissileTarget = P[loop].MissileGroups[loop2].missiles[0].target.ship;
                                if (MissileTarget != null)
                                {
                                    if (MissileTarget.IsDestroyed == true)
                                    {

                                        if (MissileTarget.ShipsFaction.RechargeList.ContainsKey(MissileTarget) == true)
                                        {
                                            MissileTarget.ShipsFaction.RechargeList[MissileTarget] = (int)Faction.RechargeStatus.Destroyed;
                                        }
                                        else
                                        {
                                            MissileTarget.ShipsFaction.RechargeList.Add(MissileTarget, (int)Faction.RechargeStatus.Destroyed);
                                        }
                                    }
                                }
                            break;
                        }
                        P[loop].MissileRemoveList.Add(P[loop].MissileGroups[loop2]);
                    }
                }
            }

            /// <summary>
            /// Taskgroup Follow orders here.
            /// </summary>
            for (int loop = factionStart; loop < factionCount; loop++)
            {
                for (int loop2 = 0; loop2 < P[loop].TaskGroups.Count; loop2++)
                {
                    /// <summary>
                    /// Adding new taskgroups means adding a loop here to run through them all.
                    /// </summary>
                    if (P[loop].TaskGroups[loop2].TaskGroupOrders.Count != 0)
                    {
                        P[loop].TaskGroups[loop2].FollowOrders((uint)(CurrentTick - lastTick));
                    }
                    else if(P[loop].TaskGroups[loop2].DrawTravelLine == 1)
                    {
                        P[loop].TaskGroups[loop2].Contact.LastXSystem = P[loop].TaskGroups[loop2].Contact.XSystem;
                        P[loop].TaskGroups[loop2].Contact.LastYSystem = P[loop].TaskGroups[loop2].Contact.YSystem;

                        P[loop].TaskGroups[loop2].DrawTravelLine = 2;
                    }
                }
            }

            /// <summary>
            /// Do sensor sweeps here. Sensors must be done after movement, not before. Missile sensors should also be here, but they need an individual check if they have no target early on.
            /// </summary>
            for (int loop = factionStart; loop < factionCount; loop++)
            {
                P[loop].SensorSweep(CurrentTick);
            }

            /// <summary>
            /// Insert Area Defense/ AMM Defense here.
            /// </summary>
            for (int loop = factionStart; loop < factionCount; loop++)
            {
                AreaDefensiveFire(P[loop],RNG);
            }

            /// <summary>
            /// attempt to fire weapons at target here.
            /// Initiative will have to be implemented here for "fairness". right now lower P numbers have the advantage.
            /// Check for destroyed ships as well.
            /// </summary>
            #region Fire Weapons
            for (int loop = factionStart; loop < factionCount; loop++)
            {
                foreach (KeyValuePair<ComponentTN, ShipTN> pair in P[loop].OpenFireFC)
                {
                    /// <summary>
                    /// Is BFC
                    /// </summary>
                    if (P[loop].OpenFireFCType[pair.Key] == true)
                    {
                        /// <summary>
                        /// Open fire and not destroyed.
                        /// </summary>
                        if (pair.Value.ShipBFC[pair.Key.componentIndex].openFire == true && pair.Value.ShipBFC[pair.Key.componentIndex].isDestroyed == false &&
                            pair.Value.ShipBFC[pair.Key.componentIndex].target != null)
                        {
                            if (pair.Value.ShipBFC[pair.Key.componentIndex].target.targetType == StarSystemEntityType.TaskGroup)
                            {
                                ShipTN Target = pair.Value.ShipBFC[pair.Key.componentIndex].target.ship;

                                /// <summary>
                                /// Same System as target and target exists.
                                /// </summary>
                                if (pair.Value.ShipsTaskGroup.Contact.CurrentSystem == Target.ShipsTaskGroup.Contact.CurrentSystem && Target.IsDestroyed == false)
                                {

                                    StarSystem CurSystem = pair.Value.ShipsTaskGroup.Contact.CurrentSystem;
                                    int MyID = CurSystem.SystemContactList.IndexOf(pair.Value.ShipsTaskGroup.Contact);
                                    int TargetID = CurSystem.SystemContactList.IndexOf(Target.ShipsTaskGroup.Contact);

                                    if (pair.Value.ShipsFaction.DetectedContactLists.ContainsKey(CurSystem) == true)
                                    {
                                        if (pair.Value.ShipsFaction.DetectedContactLists[CurSystem].DetectedContacts.ContainsKey(Target) == true)
                                        {
                                            /// <summary>
                                            /// This tick active detection.
                                            /// </summary>
                                            if (pair.Value.ShipsFaction.DetectedContactLists[CurSystem].DetectedContacts[Target].active == true)
                                            {
                                                bool WF = pair.Value.ShipFireWeapons(CurrentTick, RNG);

                                                if (Target.IsDestroyed == true)
                                                {
                                                    if (Target.ShipsFaction.RechargeList.ContainsKey(Target) == true)
                                                    {
                                                        Target.ShipsFaction.RechargeList[Target] = (int)Faction.RechargeStatus.Destroyed;
                                                    }
                                                    else
                                                    {
                                                        Target.ShipsFaction.RechargeList.Add(Target, (int)Faction.RechargeStatus.Destroyed);
                                                    }
                                                }

                                                /*String Fire = String.Format("Weapons Fired: {0}", WF );
                                                MessageEntry Entry = new MessageEntry(P[loop].TaskGroups[0].Contact.CurrentSystem, P[loop].TaskGroups[0].Contact, GameState.Instance.GameDateTime, (int)CurrentTick, Fire);
                                                P[loop].MessageLog.Add(Entry);*/

                                                if (WF == true)
                                                {
                                                    if (P[loop].RechargeList.ContainsKey(pair.Value) == true)
                                                    {
                                                        int value = P[loop].RechargeList[pair.Value];

                                                        if ((value & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                                        {
                                                            P[loop].RechargeList[pair.Value] = value + (int)Faction.RechargeStatus.Weapons;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        P[loop].RechargeList.Add(pair.Value, (int)Faction.RechargeStatus.Weapons);
                                                    }
                                                }
                                            }//end if active detection
                                        }//end if is detected
                                    }//end if system has detected contacts
                                }//end if in same system
                            }//end if targetType == TaskGroup
                            else if (pair.Value.ShipBFC[pair.Key.componentIndex].target.targetType == StarSystemEntityType.Missile)
                            {
                                OrdnanceGroupTN Target = pair.Value.ShipBFC[pair.Key.componentIndex].target.missileGroup;

                                /// <summary>
                                /// Same system, and target has missiles to be destroyed.
                                /// </summary>
                                if (pair.Value.ShipsTaskGroup.Contact.CurrentSystem == Target.contact.CurrentSystem &&( Target.missilesDestroyed != Target.missiles.Count))
                                {
                                    StarSystem CurSystem = pair.Value.ShipsTaskGroup.Contact.CurrentSystem;
                                    int MyID = CurSystem.SystemContactList.IndexOf(pair.Value.ShipsTaskGroup.Contact);
                                    int TargetID = CurSystem.SystemContactList.IndexOf(Target.contact);

                                    if (pair.Value.ShipsFaction.DetectedContactLists.ContainsKey(CurSystem) == true)
                                    {
                                        if (pair.Value.ShipsFaction.DetectedContactLists[CurSystem].DetectedMissileContacts.ContainsKey(Target) == true)
                                        {
                                            /// <summary>
                                            /// This tick active detection.
                                            /// </summary>
                                            if (pair.Value.ShipsFaction.DetectedContactLists[CurSystem].DetectedMissileContacts[Target].active == true)
                                            {
                                                bool WF = pair.Value.ShipFireWeapons(CurrentTick, RNG);

                                                if (Target.missilesDestroyed != 0 && Target.ordnanceGroupFaction.MissileRemoveList.Contains(Target) == false)
                                                {
                                                    Target.ordnanceGroupFaction.MissileRemoveList.Add(Target);
                                                }

                                                if (WF == true)
                                                {
                                                    if (P[loop].RechargeList.ContainsKey(pair.Value) == true)
                                                    {
                                                        int value = P[loop].RechargeList[pair.Value];

                                                        if ((value & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                                        {
                                                            P[loop].RechargeList[pair.Value] = value + (int)Faction.RechargeStatus.Weapons;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        P[loop].RechargeList.Add(pair.Value, (int)Faction.RechargeStatus.Weapons);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }//end if same system
                            }
                        }//end if isOpenFire isDestroyed=false, target!= null
                    }//end if isBFC = true
                    else
                    {
                        /// <summary>
                        /// Missile fire controls should be fairly simple, the missile itself does most of the lifting.
                        /// </summary>
                        if (pair.Value.ShipMFC[pair.Key.componentIndex].openFire == true && pair.Value.ShipMFC[pair.Key.componentIndex].isDestroyed == false &&
                            pair.Value.ShipMFC[pair.Key.componentIndex].target != null)
                        {
                            bool WF = pair.Value.ShipMFC[pair.Key.componentIndex].FireWeapons(pair.Value.ShipsTaskGroup, pair.Value);

                            /// <summary>
                            /// Since this ship has fired its missile launch tubes, they will need to be reloaded, put this ship in the recharge list.
                            /// </summary>
                            if (WF == true)
                            {
                                if (P[loop].RechargeList.ContainsKey(pair.Value) == true)
                                {
                                    int value = P[loop].RechargeList[pair.Value];

                                    if ((value & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                    {
                                        P[loop].RechargeList[pair.Value] = value + (int)Faction.RechargeStatus.Weapons;
                                    }
                                }
                                else
                                {
                                    P[loop].RechargeList.Add(pair.Value, (int)Faction.RechargeStatus.Weapons);
                                }
                            }
                        }
                    }//end if isBFC or isMFC
                }//end foreach component,ship in OpenFireFC
            }// end for each faction
            #endregion

            /// <summary>
            /// Do simulation maintenance here, shields,reload,recharge,etc.
            /// </summary>
            #region Simulation Maintenance
            uint TimeValue = (uint)(CurrentTick - lastTick);
            bool loopBreak = false;
            for (int loop = factionStart; loop < factionCount; loop++)
            {
                foreach (KeyValuePair<ShipTN, int> pair in P[loop].RechargeList)
                {
                    int value = pair.Value;

                    if ((value & (int)Faction.RechargeStatus.Shields) == (int)Faction.RechargeStatus.Shields)
                    {
                        pair.Key.RechargeShields(TimeValue);
                    }


                    if ((value & (int)Faction.RechargeStatus.Weapons) == (int)Faction.RechargeStatus.Weapons)
                    {
                        int ShotsExp;
                        int ret = pair.Key.RechargeBeamWeapons(TimeValue,out ShotsExp);

                        ushort amt = (ushort)(Math.Floor((float)TimeValue / 5.0f));
                        int PowerComp = pair.Key.CurrentPowerGen * amt;

                        bool allTubesLoaded = pair.Key.ReloadLaunchTubes(TimeValue);

                        /// <summary>
                        /// When all tubes are loaded and have remained loaded for atleast 1 tick reloadLaunchTubes should return true. 
                        /// Likewise when no beam weapon recharging is to be done power will sit at full for at least one tick.
                        /// This should keep continuously firing weapons in this list even if they are considered recharged for a single sliver of time.
                        /// ShotsExp is to handle gauss cannon "reloading". Point defense imposes this requirement. A ShotsExp of zero means that no gauss cannon fired
                        /// in point defense during the last tick. also will come up for multibarrel turrets.
                        /// </summary>
                        if (ret == PowerComp && allTubesLoaded == true && ShotsExp == 0)
                        {
                            P[loop].RechargeList[pair.Key] = P[loop].RechargeList[pair.Key] - (int)Faction.RechargeStatus.Weapons;

                            if (P[loop].RechargeList[pair.Key] == 0)
                            {
                                P[loop].RechargeList.Remove(pair.Key);
                                loop--;
                                loopBreak = true;
                                break;
                            }
                        }
                    }

                    /// <summary>
                    /// recharge all CIWS on this ship.
                    /// </summary>
                    if( (value & (int)Faction.RechargeStatus.CIWS) == (int)Faction.RechargeStatus.CIWS)
                    {
                        int shots = pair.Key.RechargeCIWS();

                        /// <summary>
                        /// I've recharged this ship twice, but its CIWS have not fired on anything in the mean time. so remove it from the list.
                        /// </summary>
                        if (shots == 0)
                        {
                            P[loop].RechargeList[pair.Key] = P[loop].RechargeList[pair.Key] - (int)Faction.RechargeStatus.CIWS;

                            /// <summary>
                            /// If no flags are present at all for this ship, remove it entirely.
                            /// </summary>

                            if (P[loop].RechargeList[pair.Key] == 0)
                            {
                                P[loop].RechargeList.Remove(pair.Key);
                                loop--;
                                loopBreak = true;
                                break;
                            }
                        }
                    }

                    /// <summary>
                    /// Ship destruction, very involving.
                    /// All Taskgroups ordered to move to the destroyed ship have to have their orders canceled.
                    /// System detected contacts have to be updated. this includes both the detected list and the FactionSystemDetection map as a whole. 
                    /// FSD is handled under RemoveFriendlyTaskGroupOrdered() by the removeContact functionality.
                    /// </summary>
                    if((value & (int)Faction.RechargeStatus.Destroyed) == (int)Faction.RechargeStatus.Destroyed)
                    {
                        RemoveTaskGroupsOrdered(pair);                        

                        for (int loop4 = factionStart; loop4 < factionCount; loop4++)
                        {
                            StarSystem CurSystem = pair.Key.ShipsTaskGroup.Contact.CurrentSystem;
                            if(P[loop4].DetectedContactLists.ContainsKey(CurSystem) == true)
                            {
                                if (P[loop4].DetectedContactLists[CurSystem].DetectedContacts.ContainsKey(pair.Key) == true)
                                {
                                    P[loop4].DetectedContactLists[CurSystem].DetectedContacts.Remove(pair.Key);
                                }
                            }
                        }

                        bool nodeGone = pair.Key.OnDestroyed();
                        pair.Key.ShipClass.ShipsInClass.Remove(pair.Key);
                        pair.Key.ShipsTaskGroup.Ships.Remove(pair.Key);
                        pair.Key.ShipsFaction.Ships.Remove(pair.Key);

                        if (pair.Key.ShipsTaskGroup.Ships.Count == 0)
                        {
                            RemoveFriendlyTaskGroupsOrdered(pair);
                        }

                        RemoveShipsTargetting(pair);

                        P[loop].RechargeList.Remove(pair.Key);

                        /// <summary>
                        /// Have to re-run loop since a ship was removed from all kinds of things.
                        /// </summary>
                        loop--;
                        loopBreak = true;
                        break;
                    }
                }

                /// <summary>
                /// Skip this section of code if loop was broken. the loop will be reprocessed so everything will be done eventually.
                /// </summary>
                if (loopBreak == false)
                {

                    for (int loop2 = 0; loop2 < P[loop].MissileRemoveList.Count; loop2++)
                    {
                        /// <summary>
                        /// every missile in this list will either have missiles removed, or needs to be deleted as an ordnance group.
                        /// </summary>
                        if (P[loop].MissileRemoveList[loop2].missiles.Count > P[loop].MissileRemoveList[loop2].missilesDestroyed)
                        {
                            for (int loop3 = 0; loop3 < P[loop].MissileRemoveList[loop2].missilesDestroyed; loop3++)
                            {
                                P[loop].MissileRemoveList[loop2].RemoveMissile(P[loop].MissileRemoveList[loop2].missiles[0]);
                            }

                            P[loop].MissileRemoveList[loop2].missilesDestroyed = 0;
                        }
                        else
                        {
                            RemoveOrdnanceGroupFromSim(P[loop].MissileRemoveList[loop2],P);
                        }
                    }

                    P[loop].MissileRemoveList.Clear();
                }
            }
            #endregion

            /// <summary>
            /// eventually move every planet/moon/star/asteroid
            /// </summary>
            //foreach(StarSystem System in GameState.Instance.StarSystems)
            //{
                //foreach (Planet oPlanet in System.Stars[0].Planets)
                //{
                    //Pulsar4X.Lib.OrbitTable.Instance.UpdatePosition(oPlanet, tickValue);
                    //oPlanet.XSystem = oPlanet.XSystem + 1.0;
                    //oPlanet.YSystem = oPlanet.YSystem + 1.0;
                //}
            //}
              
        }

        #region AdvanceSim Ship/ordnance group destruction related Private functions

        /// <summary>
        /// All taskgroups ordered on the current destroyed ship have to have those orders canceled.
        /// This is for hostile ships, tugs may also eventually make use of this.
        /// </summary>
        /// <param name="pair">KeyValuePair of the ship involved</param>
        private void RemoveTaskGroupsOrdered(KeyValuePair<ShipTN, int> pair)
        {
            for (int loop4 = 0; loop4 < pair.Key.TaskGroupsOrdered.Count; loop4++)
            {
                for (int loop5 = 0; loop5 < pair.Key.TaskGroupsOrdered[loop4].TaskGroupOrders.Count; loop5++)
                {
                    if (pair.Key.TaskGroupsOrdered[loop4].TaskGroupOrders[loop5].target.SSEntity == StarSystemEntityType.TaskGroup)
                    {
                        if (pair.Key.TaskGroupsOrdered[loop4].TaskGroupOrders[loop5].taskGroup == pair.Key.ShipsTaskGroup)
                        {
                            /// <summary>
                            /// At this point it has been established that the destroyed ship has TGs ordered to it some how(enemy contact ordering).
                            /// That the ordered TG has multiple TG orders
                            /// That the current order target is a taskgroup, and in fact this taskgroup.
                            /// </summary>

                            String Entry = String.Format("Taskgroup {0} cannot find target, orders canceled.", pair.Key.TaskGroupsOrdered[loop4].Name);
                            MessageEntry Entry2 = new MessageEntry(MessageEntry.MessageType.OrdersNotCompleted, pair.Key.TaskGroupsOrdered[loop4].Contact.CurrentSystem, pair.Key.TaskGroupsOrdered[loop4].Contact,
                                                                   GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                            pair.Key.TaskGroupsOrdered[loop4].TaskGroupFaction.MessageLog.Add(Entry2);

                            int lastOrder = pair.Key.TaskGroupsOrdered[loop4].TaskGroupOrders.Count - 1;
                            for (int loop6 = lastOrder; loop6 >= loop5; loop6--)
                            {
                                pair.Key.TaskGroupsOrdered[loop4].TaskGroupOrders.RemoveAt(loop6);
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// any friendly ships that have orders to do anything at this taskgroup need to have those orders canceled as well. this is separate from the
        /// removal of ship specific ordering.
        /// </summary>
        /// <param name="pair"></param>
        private void RemoveFriendlyTaskGroupsOrdered(KeyValuePair<ShipTN, int> pair)
        {
            for (int loop4 = 0; loop4 < pair.Key.ShipsTaskGroup.TaskGroupsOrdered.Count; loop4++)
            {
                for (int loop5 = 0; loop5 < pair.Key.ShipsTaskGroup.TaskGroupsOrdered[loop4].TaskGroupOrders.Count; loop5++)
                {
                    if (pair.Key.ShipsTaskGroup.TaskGroupsOrdered[loop4].TaskGroupOrders[loop5].target.SSEntity == StarSystemEntityType.TaskGroup)
                    {
                        if (pair.Key.ShipsTaskGroup.TaskGroupsOrdered[loop4].TaskGroupOrders[loop5].taskGroup == pair.Key.ShipsTaskGroup)
                        {
                            /// <summary>
                            /// At this point it has been established that the destroyed TG has TGs ordered to it, friendly TGs.
                            /// That the ordered TG has multiple TG orders
                            /// That the current order target is a taskgroup, and in fact this taskgroup.
                            /// </summary>

                            String Entry = String.Format("Taskgroup {0} cannot find target, orders canceled.", pair.Key.ShipsTaskGroup.TaskGroupsOrdered[loop4].Name);
                            MessageEntry Entry2 = new MessageEntry(MessageEntry.MessageType.OrdersNotCompleted, pair.Key.ShipsTaskGroup.TaskGroupsOrdered[loop4].Contact.CurrentSystem, pair.Key.ShipsTaskGroup.TaskGroupsOrdered[loop4].Contact,
                                                                   GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                            pair.Key.ShipsTaskGroup.TaskGroupsOrdered[loop4].TaskGroupFaction.MessageLog.Add(Entry2);

                            int lastOrder = pair.Key.ShipsTaskGroup.TaskGroupsOrdered[loop4].TaskGroupOrders.Count - 1;
                            for (int loop6 = lastOrder; loop6 >= loop5; loop6--)
                            {
                                pair.Key.ShipsTaskGroup.TaskGroupsOrdered[loop4].TaskGroupOrders.RemoveAt(loop6);
                            }
                            break;
                        }
                    }
                }
            }

            pair.Key.ShipsTaskGroup.clearAllOrders();
            pair.Key.ShipsTaskGroup.Contact.CurrentSystem.RemoveContact(pair.Key.ShipsTaskGroup.Contact);
            pair.Key.ShipsFaction.TaskGroups.Remove(pair.Key.ShipsTaskGroup);        
        }

        /// <summary>
        /// Any ships that want to fire upon this craft have to be updated to reflect destruction
        /// </summary>
        /// <param name="pair">Key value pair of the ship itself.</param>
        private void RemoveShipsTargetting(KeyValuePair<ShipTN, int> pair)
        {
            for (int loop5 = 0; loop5 < pair.Key.ShipsTargetting.Count; loop5++)
            {
                ShipTN nextShip = pair.Key.ShipsTargetting[loop5];
                for (int loop6 = 0; loop6 < nextShip.ShipBFC.Count; loop6++)
                {
                    if (nextShip.ShipBFC[loop6].getTarget().targetType == StarSystemEntityType.TaskGroup && nextShip.ShipBFC[loop6].getTarget().ship == pair.Key)
                    {
                        nextShip.ShipBFC[loop6].clearTarget();
                        nextShip.ShipBFC[loop6].openFire = false;
                        nextShip.ShipsFaction.OpenFireFC.Remove(nextShip.ShipBFC[loop6]);
                        nextShip.ShipsFaction.OpenFireFCType.Remove(nextShip.ShipBFC[loop6]);
                    }
                }

                for (int loop6 = 0; loop6 < nextShip.ShipMFC.Count; loop6++)
                {
                    if (nextShip.ShipMFC[loop6].getTarget().targetType == StarSystemEntityType.TaskGroup)
                    {
                        if (nextShip.ShipMFC[loop6].getTarget().ship == pair.Key)
                        {
                            nextShip.ShipMFC[loop6].clearTarget();
                            nextShip.ShipMFC[loop6].openFire = false;
                            nextShip.ShipsFaction.OpenFireFC.Remove(nextShip.ShipMFC[loop6]);
                            nextShip.ShipsFaction.OpenFireFCType.Remove(nextShip.ShipMFC[loop6]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove an ordnance group from the sim, and inform everyone targetted on or moving towards this of destruction.
        /// </summary>
        /// <param name="Owner">Owning faction</param>
        /// <param name="OGRemove">Ordnance group to remove</param>
        public void RemoveOrdnanceGroupFromSim(OrdnanceGroupTN OGRemove, BindingList<Faction> P)
        {
            /// <summary>
            /// This ordnance group needs to be removed.
            /// Ships Can be targeted on this ordnance group, from these ships missiles in flight can be tracked and informed.
            /// </summary>
           
            /// <summary>
            /// Clear manually targetted Beam fire controls. neither area, nor final defense need to be cleared here.
            /// </summary>
            for (int loop = 0; loop < OGRemove.shipsTargetting.Count; loop++)
            {
                ShipTN nextShip = OGRemove.shipsTargetting[loop];
                for (int loop2 = 0; loop2 < nextShip.ShipBFC.Count; loop2++)
                {
                    TargetTN BFCTarget = nextShip.ShipBFC[loop2].getTarget();
                    if (BFCTarget != null)
                    {
                        if (BFCTarget.targetType == StarSystemEntityType.Missile && nextShip.ShipBFC[loop2].pDState == PointDefenseState.None)
                        {
                            if (BFCTarget.missileGroup == OGRemove)
                            {
                                nextShip.ShipBFC[loop2].clearTarget();
                                nextShip.ShipBFC[loop2].openFire = false;
                                nextShip.ShipsFaction.OpenFireFC.Remove(nextShip.ShipBFC[loop2]);
                                nextShip.ShipsFaction.OpenFireFCType.Remove(nextShip.ShipBFC[loop2]);
                            }
                        }
                    }
                }

                /// <summary>
                /// Clear manually targeted missile fire controls.
                /// </summary>
                for (int loop2 = 0; loop2 < nextShip.ShipMFC.Count; loop2++)
                {
                    TargetTN MFCTarget = nextShip.ShipMFC[loop2].getTarget();
                    if (MFCTarget != null)
                    {
                        if (MFCTarget.targetType == StarSystemEntityType.Missile && nextShip.ShipMFC[loop2].pDState == PointDefenseState.None)
                        {
                            if (MFCTarget.missileGroup == OGRemove)
                            {
                                /// <summary>
                                /// Clear the target, set open fire to false, update the openFireFC list.
                                /// </summary>
                                nextShip.ShipMFC[loop2].clearTarget();
                                nextShip.ShipMFC[loop2].openFire = false;
                                nextShip.ShipsFaction.OpenFireFC.Remove(nextShip.ShipMFC[loop2]);
                                nextShip.ShipsFaction.OpenFireFCType.Remove(nextShip.ShipMFC[loop2]);

                                /// <summary>
                                /// Set all missiles to their own sensors.
                                /// </summary>
                                for (int loop3 = 0; loop3 < nextShip.ShipMFC[loop2].missilesInFlight.Count; loop++)
                                {
                                    nextShip.ShipMFC[loop2].missilesInFlight[loop3].CheckTracking();
                                }
                            }
                        }
                    }
                }

                /// <summary>
                /// Clear the point defense missiles.
                /// </summary>
                for (int loop2 = 0; loop2 < OGRemove.ordGroupsTargetting.Count; loop2++)
                {
                    OGRemove.ordGroupsTargetting[loop2].CheckTracking();
                }
            }
            /// <summary>
            /// Finally I need to remove the ordnance group from its faction list, all detection lists, from the system contact list, inform the Sceen to delete this contact, and clear the missile binding list.
            /// Complicated stuff.
            /// </summary>
            for (int loop4 = factionStart; loop4 < factionCount; loop4++)
            {
                StarSystem CurSystem = OGRemove.contact.CurrentSystem;
                if (P[loop4].DetectedContactLists.ContainsKey(CurSystem) == true)
                {
                    if (P[loop4].DetectedContactLists[CurSystem].DetectedMissileContacts.ContainsKey(OGRemove) == true)
                    {
                        P[loop4].DetectedContactLists[CurSystem].DetectedMissileContacts.Remove(OGRemove);
                    }
                }
            }
            OGRemove.missilesDestroyed = 0;
            OGRemove.missiles.Clear();
            OGRemove.contact.ContactElementCreated = SystemContact.CEState.Delete;

            Faction Owner = OGRemove.ordnanceGroupFaction;
            StarSystem CurrentSystem = OGRemove.contact.CurrentSystem;

            CurrentSystem.RemoveContact(OGRemove.contact);
            Owner.MissileGroups.Remove(OGRemove);
        }
        #endregion

        /// <summary>
        /// this is probably still deprecated.
        /// </summary>
        public SimEntity()
        {
            SimCreated = false;
        }

        /// <summary>
        /// Constructor for sim entity. as with all constructors, lots of initialization happens here.
        /// </summary>
        /// <param name="factCount"></param>
        /// <param name="factStart"></param>
        public SimEntity(int factCount, int factStart)
        {
            SimCreated = true;
            factionStart = factStart;
            factionCount = factCount;
            TGStart = 0;
            TGCount = 0;
            FleetInterceptionPreemptTick = -1;

            MissileInterceptPreemptTick = -1;
            MissileTimeToHit = 0;

#warning subpulse related magic numbers
            SubPulse = new Dictionary<int, SubPulseTimeList>();
            SubPulse.Add((int)Constants.TimeInSeconds.FiveSeconds, new SubPulseTimeList((int)Constants.TimeInSeconds.FiveSeconds, 1));
            SubPulse.Add((int)Constants.TimeInSeconds.ThirtySeconds, new SubPulseTimeList((int)Constants.TimeInSeconds.FiveSeconds, 6));
            SubPulse.Add((int)Constants.TimeInSeconds.TwoMinutes, new SubPulseTimeList((int)Constants.TimeInSeconds.ThirtySeconds, 4));
            SubPulse.Add((int)Constants.TimeInSeconds.FiveMinutes, new SubPulseTimeList((int)Constants.TimeInSeconds.Minute, 5));
            SubPulse.Add((int)Constants.TimeInSeconds.TwentyMinutes, new SubPulseTimeList((int)Constants.TimeInSeconds.FiveMinutes, 4));
            SubPulse.Add((int)Constants.TimeInSeconds.Hour, new SubPulseTimeList((int)Constants.TimeInSeconds.TwentyMinutes, 3));
            SubPulse.Add((int)Constants.TimeInSeconds.ThreeHours, new SubPulseTimeList((int)Constants.TimeInSeconds.Hour, 3));
            SubPulse.Add((int)Constants.TimeInSeconds.EightHours, new SubPulseTimeList((int)(Constants.TimeInSeconds.Hour * 2), 4));
            SubPulse.Add((int)Constants.TimeInSeconds.Day, new SubPulseTimeList((int)Constants.TimeInSeconds.EightHours, 3));
            SubPulse.Add((int)Constants.TimeInSeconds.FiveDays, new SubPulseTimeList((int)Constants.TimeInSeconds.Day, 5));
            SubPulse.Add((int)Constants.TimeInSeconds.Month, new SubPulseTimeList((int)Constants.TimeInSeconds.FiveDays, 6));

            FleetInterceptPreemptList = new BindingList<TaskGroupTN>();
        }

        /// <summary>
        /// Final defensive fire scans through all potential FCs that could fire defensively on the incoming missile to see if it is intercepeted.
        /// All PD enabled FCs will attempt to shoot down this missile except ones from the same faction, as this missile is practically right on top of said FC.
        /// In other words allied/neutral status isn't taken into account.
        /// </summary>
        /// <param name="P">Faction list</param>
        /// <param name="Missile">Missile to try to intercept</param>
        /// <param name="RNG">Random Number Generator</param>
        /// <returns>Whether the missile has been intercepted</returns>
        public bool FinalDefensiveFire(BindingList<Faction> P, OrdnanceTN Missile, Random RNG)
        {
            bool Intercept = false;
            StarSystem CurrentSystem = Missile.missileGroup.contact.CurrentSystem;
            float PointBlank = 10000.0f / (float)Constants.Units.KM_PER_AU;

            /// <summary>
            /// loop through every faction.
            /// </summary>
            for (int loop = 0; loop < P.Count; loop++)
            {
                /// <summary>
                /// Is the current faction different from the missile group faction, and does the faction have a detected contacts list for the current system?
                /// </summary>
                if (P[loop] != Missile.missileGroup.ordnanceGroupFaction && P[loop].DetectedContactLists.ContainsKey(CurrentSystem) == true )
                {
                    /// <summary>
                    /// Is the Missile group in this detected contact list?
                    /// </summary>
                    if (P[loop].DetectedContactLists[CurrentSystem].DetectedMissileContacts.ContainsKey(Missile.missileGroup) == true)
                    {
                        /// <summary>
                        /// Is the detection an active detection?
                        /// </summary>
                        if (P[loop].DetectedContactLists[CurrentSystem].DetectedMissileContacts[Missile.missileGroup].active == true)
                        {
                            /// <summary>
                            /// Does this faction have any point defense enabled FCs in this system?
                            /// </summary>
                            if (P[loop].PointDefense.ContainsKey(CurrentSystem) == true)
                            {
                                /// <summary>
                                /// loop through all the possible PD enabled FC.
                                /// </summary>
                                foreach (KeyValuePair<ComponentTN, ShipTN> pair in P[loop].PointDefense[CurrentSystem].PointDefenseFC)
                                {
                                    /// <summary>
                                    /// Only want BFCs in FDF mode for now.
                                    /// </summary>
                                    if (P[loop].PointDefense[CurrentSystem].PointDefenseType[pair.Key] == false && pair.Value.ShipBFC[pair.Key.componentIndex].pDState == PointDefenseState.FinalDefensiveFire)
                                    {
                                        /// <summary>
                                        /// Do a distance check on pair.Value vs the missile itself. if that checks out to be less than 10k km(or equal to zero), then
                                        /// check to see if the FC can shoot down said missile. This should never be run before a sensor sweep
                                        /// </summary>
                                        float dist = -1;

                                        int MissileID = CurrentSystem.SystemContactList.IndexOf(Missile.missileGroup.contact);
                                        int TGID = CurrentSystem.SystemContactList.IndexOf(pair.Value.ShipsTaskGroup.Contact);

                                        /// <summary>
                                        /// dist is in AU.
                                        /// </summary>
                                        dist = CurrentSystem.SystemContactList[MissileID].DistanceTable[TGID];

                                        /// <summary>
                                        /// if distance is less than the 10k km threshold attempt to intercept at Point blank range.
                                        /// </summary>
                                        if (dist < PointBlank)
                                        {
                                            /// <summary>
                                            /// Finally intercept the target.
                                            /// </summary>
                                            bool WF = false;
                                            Intercept = pair.Value.ShipBFC[pair.Key.componentIndex].InterceptTarget(RNG, 0, Missile, pair.Value.ShipsFaction,
                                                                                                                    pair.Value.ShipsTaskGroup.Contact, pair.Value, out WF);
                                            /// <summary>
                                            /// Add this ship to the weapon recharge list since it has fired. This is done here in Sim, or for FDF_Self in Ship.cs
                                            /// </summary>
                                            if (WF == true)
                                            {
                                                if (P[loop].RechargeList.ContainsKey(pair.Value) == true)
                                                {
                                                    /// <summary>
                                                    /// If our recharge value does not have Recharge beams in it(bitflag 2 for now), then add it.
                                                    /// </summary>
                                                    if ((P[loop].RechargeList[pair.Value] & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                                    {
                                                        P[loop].RechargeList[pair.Value] = (P[loop].RechargeList[pair.Value] + (int)Faction.RechargeStatus.Weapons);
                                                    }
                                                }
                                                else
                                                {
                                                    P[loop].RechargeList.Add(pair.Value, (int)Faction.RechargeStatus.Weapons);
                                                }
                                            }

                                            /// <summary>
                                            /// break out of the first foreach loop.
                                            /// </summary>
                                            if (Intercept == true)
                                                break;
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                /// <summary>
                /// now break out of the faction loop as this missile has been shot down.
                /// </summary>
                if (Intercept == true)
                    break;
            }
            return Intercept;
        }

        /// <summary>
        /// Area defensive fire will sweep through a faction's list of BFCs and MFCs to fire at detected ordnance in range.
        /// </summary>
        /// <param name="Fact">Faction to search for fire controls of</param>
        /// <param name="RNG">"global" rng from further up.</param>
        public void AreaDefensiveFire(Faction Fact, Random RNG)
        {
            /// <summary>
            /// No point defense set FCs, just return.
            /// </summary>
            if (Fact.PointDefense.Count == 0)
            {
                return;
            }

            /// <summary>
            /// Look through each starsystem with a point defense list.
            /// </summary>
            foreach (KeyValuePair<StarSystem, PointDefenseList> pair in Fact.PointDefense)
            {
                StarSystem CurrentSystem = pair.Key;

                /// <summary>
                /// No detected contacts in this system.
                /// </summary>
                if (Fact.DetectedContactLists.ContainsKey(CurrentSystem) == false)
                {
                    break;
                }

                /// <summary>
                /// No missile contacts in this system.
                /// </summary>
                if (Fact.DetectedContactLists[CurrentSystem].DetectedMissileContacts.Count == 0)
                {
                    break;
                }

                /// <summary>
                /// now loop through each FC in the current starsystem.
                /// </summary>
                foreach (KeyValuePair<ComponentTN, ShipTN> pair2 in pair.Value.PointDefenseFC)
                {
                    /// <summary>
                    /// BFC set to Area defense mode
                    /// </summary>
                    if (pair.Value.PointDefenseType[pair2.Key] == false && pair2.Value.ShipBFC[pair2.Key.componentIndex].pDState == PointDefenseState.AreaDefense)
                    {
                        /// <summary>
                        /// loop through every missile contact. will have to do distance checks here.
                        /// </summary>
                        foreach (KeyValuePair<OrdnanceGroupTN, FactionContact> MisPair in Fact.DetectedContactLists[CurrentSystem].DetectedMissileContacts)
                        {
                            /// <summary>
                            /// This missile group is already destroyed and will be cleaned up by sim later.
                            if (MisPair.Key.missilesDestroyed == MisPair.Key.missiles.Count)
                                break;

                            /// <summary>
                            /// Do a distance check on pair.Value vs the missile itself. if that checks out to be less than 10k km(or equal to zero), then
                            /// check to see if the FC can shoot down said missile. This should never be run before a sensor sweep
                            /// </summary>
                            float dist = -1;

                            int MissileID = CurrentSystem.SystemContactList.IndexOf(MisPair.Key.contact);
                            int TGID = CurrentSystem.SystemContactList.IndexOf(pair2.Value.ShipsTaskGroup.Contact);

                            /// <summary>
                            /// dist is in AU.
                            /// </summary>
                            dist = CurrentSystem.SystemContactList[MissileID].DistanceTable[TGID];

                            /// <summary>
                            /// Only bother with checks here that are within the maximum beam distance.
                            /// </summary>
                            if (dist <= Constants.Units.BEAM_AU_MAX)
                            {
                                /// <summary>
                                /// Value is in units of 10k km
                                /// </summary>
                                float rangeAreaDefenseKm;

                                /// <summary>
                                /// The user put in an absurdly large value.
                                /// </summary>

                                if (pair2.Value.ShipBFC[pair2.Key.componentIndex].pDRange > (float)Constants.Units.TEN_KM_MAX)
                                {
                                    /// <summary>
                                    /// Max possible beam range in KM.
                                    /// </summary>
                                    rangeAreaDefenseKm = (float)Constants.Units.BEAM_KM_MAX;
                                }
                                else
                                {
#warning magic number related to 10k
                                    rangeAreaDefenseKm = pair2.Value.ShipBFC[pair2.Key.componentIndex].pDRange * 10000.0f;
                                } 

                                float distKM = dist * (float)Constants.Units.KM_PER_AU;

                                /// <summary>
                                /// Additional paranoia check of range, I need to fix Area defense PD range values to ship bfc range in any event, that hasn't been done yet.
                                /// </summary>
#warning magic number for total bfc range.
                                float totalRange = pair2.Value.ShipBFC[pair2.Key.componentIndex].beamFireControlDef.range * 2.0f;

                                float range = Math.Min(totalRange, rangeAreaDefenseKm);

                                /// <summary>
                                /// the BFC is set for range defense and is in range of this missile.
                                /// </summary>
                                if (distKM <= range)
                                {
#warning magic number related to 10k km increments.
                                    /// <summary>
                                    /// Increment is a 10k km unit, so distance must be divided by 10000 to yield the appropriate number.
                                    /// </summary>
                                    int increment = (int)Math.Floor((float)distKM / 10000.0f);

                                    bool Intercept = false;
                                    int MissilesToDestroy = 0;
                                    for (int loop = MisPair.Key.missilesDestroyed; loop < MisPair.Key.missiles.Count; loop++)
                                    {
                                        bool WF = false;
                                        Intercept = pair2.Value.ShipBFC[pair2.Key.componentIndex].InterceptTarget(RNG, increment, MisPair.Key.missiles[loop], pair2.Value.ShipsFaction,
                                                                                                                  pair2.Value.ShipsTaskGroup.Contact, pair2.Value, out WF);

                                        /// <summary>
                                        /// Add this ship to the weapon recharge list since it has fired. This is done here in Sim, or for FDF_Self in Ship.cs
                                        /// </summary>
                                        if (WF == true)
                                        {
                                            if (Fact.RechargeList.ContainsKey(pair2.Value) == true)
                                            {
                                                /// <summary>
                                                /// If our recharge value does not have Recharge beams in it(bitflag 2 for now), then add it.
                                                /// </summary>
                                                if ((Fact.RechargeList[pair2.Value] & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                                {
                                                    Fact.RechargeList[pair2.Value] = (Fact.RechargeList[pair2.Value] + (int)Faction.RechargeStatus.Weapons);
                                                }
                                            }
                                            else
                                            {
                                                Fact.RechargeList.Add(pair2.Value, (int)Faction.RechargeStatus.Weapons);
                                            }
                                        }

                                        if (Intercept == true)
                                        {
                                            /// <summary>
                                            /// Destroy the missile, check if the ordnance group should be removed, if its gone also remove it from the detected contacts list and break that loop.
                                            /// </summary>
                                            
                                            MissilesToDestroy++;
                                        }
                                        else if (Intercept == false)
                                        {
                                            /// <summary>
                                            /// This FC can't intercept any more missiles, advance to the next one.
                                            /// </summary>
                                            break;
                                        }
                                    }

                                    /// <summary>
                                    /// Set the missiles destroyed count as appropriate.
                                    /// </summary>
                                    MisPair.Key.missilesDestroyed = MisPair.Key.missilesDestroyed + MissilesToDestroy;

                                    if (MisPair.Key.missilesDestroyed != 0 && Fact.MissileRemoveList.Contains(MisPair.Key) == false)
                                    {
                                        /// <summary>
                                        /// Tell sim to remove missiles from this group, or remove it entirely.
                                        /// </summary>
                                        Fact.MissileRemoveList.Add(MisPair.Key);
                                    }

                                    if (Intercept == false)
                                    {
                                        /// <summary>
                                        /// This condition means advance to the next FC.
                                        /// </summary>
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    /// <summary>
                    /// MFC set to any pd mode that can be applied: 1v2 up to 5v1.
                    /// </summary>
                    else if (pair.Value.PointDefenseType[pair2.Key] == true && pair2.Value.ShipMFC[pair2.Key.componentIndex].pDState >= PointDefenseState.AMM1v2 &&
                        pair2.Value.ShipMFC[pair2.Key.componentIndex].pDState <= PointDefenseState.AMM5v1)
                    {
                        int MissilesLaunched = 0;
                        int MissilesToLaunch = 0;
                        foreach (KeyValuePair<OrdnanceGroupTN, FactionContact> MisPair in Fact.DetectedContactLists[CurrentSystem].DetectedMissileContacts)
                        {
                            /// <summary>
                            /// Advance to next missile group.
                            /// </summary>
                            if (MisPair.Key.missilesDestroyed == MisPair.Key.missiles.Count)
                            {
                                break;
                            }

                            /// <summary>
                            /// Do a distance check on pair.Value vs the missile itself. if that checks out to be less than 10k km(or equal to zero), then
                            /// check to see if the FC can shoot down said missile. This should never be run before a sensor sweep
                            /// </summary>
                            float dist = -1;

                            int MissileID = CurrentSystem.SystemContactList.IndexOf(MisPair.Key.contact);
                            int TGID = CurrentSystem.SystemContactList.IndexOf(pair2.Value.ShipsTaskGroup.Contact);

                            /// <summary>
                            /// dist is in AU.
                            /// </summary>
                            dist = CurrentSystem.SystemContactList[MissileID].DistanceTable[TGID];


                            float MFCEngageDistKm = pair2.Value.ShipMFC[pair2.Key.componentIndex].mFCSensorDef.maxRange;
                            float rangeAreaDefenseKm = pair2.Value.ShipMFC[pair2.Key.componentIndex].pDRange;

                            /// <summary>
                            /// Range is in 10K units so it has to be adjusted to AU for later down.
                            /// </summary>
#warning magic 10k number here
                            float range = (Math.Min(MFCEngageDistKm, rangeAreaDefenseKm) / (float)Constants.Units.KM_PER_AU) ;
                            range = range * 10000.0f;

                            int MSize = 0;
                            int AltMSize = 0;

#warning the +6 is another magic number.
                            if ((int)Math.Ceiling(MisPair.Key.missiles[0].missileDef.size) <= (Constants.OrdnanceTN.MissileResolutionMinimum + 6))
                            {
                                MSize = Constants.OrdnanceTN.MissileResolutionMinimum;
                                AltMSize = 0;
                            }
                            else if ((int)Math.Ceiling(MisPair.Key.missiles[0].missileDef.size) <= (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                            {
                                MSize = (int)Math.Ceiling(MisPair.Key.missiles[0].missileDef.size);
                                AltMSize = 0;
                            }
                            else if ((int)Math.Ceiling(MisPair.Key.missiles[0].missileDef.size) > (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                            {
                                MSize = -1;
                                AltMSize = (int)Math.Ceiling(Math.Ceiling(MisPair.Key.missiles[0].missileDef.size) / (Constants.OrdnanceTN.MissileResolutionMaximum + 6));
                            }

                            int MFCRange = pair2.Value.ShipMFC[pair2.Key.componentIndex].mFCSensorDef.GetActiveDetectionRange(AltMSize, MSize);

                            bool CanDetect = Fact.LargeDetection(CurrentSystem, dist, MFCRange);

                            /// <summary>
                            /// Can this MFC fire on the targetted missile?
                            /// </summary>
                            if (CanDetect == true && dist <= range)
                            {
                                /// <summary>
                                /// Do AMM defense here. Check to see how many amms are targeted on this missile, if less than defense setting fire more.
                                /// How do I handle 1v2 mode? rounding obviously. if more than 1 missile in group send half, send atleast 1 for 1, and round for odd missile amounts.
                                /// missiles won't be destroyed here, as they were in beam fire mode, this will just launch amms at missile groups.
                                /// </summary>
                                
                                /// <summary>
                                /// Get total missiles currently targetted on this group. Keeping track of a total missiles incoming variable would mean handling a lot of interactions where
                                /// missiles can be destroyed, run out of fuel, etc. so I'll just loop through this for now.
                                /// </summary>
#warning this missile count can be optimized, but it would be difficult to do so.
                                int TotalCount = 0;
                                for (int loopMP = 0; loopMP < MisPair.Key.ordGroupsTargetting.Count; loopMP++)
                                {
                                    TotalCount = TotalCount + MisPair.Key.ordGroupsTargetting[loopMP].missiles.Count;
                                }

                                float Value = TotalCount / MisPair.Key.missiles.Count;

                                switch (pair2.Value.ShipMFC[pair2.Key.componentIndex].pDState)
                                {
#warning more magic numbers in how point defense states are handled.
                                    case PointDefenseState.AMM1v2:
                                        if (Value < 0.5f)
                                        {
                                            int Max = (int)Math.Ceiling((float)MisPair.Key.missiles.Count / 2.0f);
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                    case PointDefenseState.AMM1v1:
                                        if (Value < 1.0f)
                                        {
                                            int Max = MisPair.Key.missiles.Count;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                    case PointDefenseState.AMM2v1:
                                        if (Value < 2.0f)
                                        {
                                            int Max = MisPair.Key.missiles.Count * 2;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                    case PointDefenseState.AMM3v1:
                                        if (Value < 3.0f)
                                        {
                                            int Max = MisPair.Key.missiles.Count * 3;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                    case PointDefenseState.AMM4v1:
                                        if (Value < 4.0f)
                                        {
                                            int Max = MisPair.Key.missiles.Count * 4;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                    case PointDefenseState.AMM5v1:
                                        if (Value < 5.0f)
                                        {
                                            int Max = MisPair.Key.missiles.Count * 5;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                }

                                if (MissilesToLaunch != 0)
                                {
                                    /// <summary>
                                    /// launch up to MissilesToLaunch amms in a new ord group at the target.
                                    /// <summary>
                                    MissilesLaunched = pair2.Value.ShipMFC[pair2.Key.componentIndex].FireWeaponsPD(pair2.Value.ShipsTaskGroup, pair2.Value, MisPair.Key, MissilesToLaunch);


                                    /// <summary>
                                    /// Add this ship to the weapon recharge list since it has fired. This is done here in Sim, or for FDF_Self in Ship.cs
                                    /// </summary>
                                    if (MissilesLaunched != 0)
                                    {
                                        if (Fact.RechargeList.ContainsKey(pair2.Value) == true)
                                        {
                                            /// <summary>
                                            /// If our recharge value does not have Recharge beams in it(bitflag 2 for now), then add it.
                                            /// </summary>
                                            if ((Fact.RechargeList[pair2.Value] & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                            {
                                                Fact.RechargeList[pair2.Value] = (Fact.RechargeList[pair2.Value] + (int)Faction.RechargeStatus.Weapons);
                                            }
                                        }
                                        else
                                        {
                                            Fact.RechargeList.Add(pair2.Value, (int)Faction.RechargeStatus.Weapons);
                                        }
                                    }

                                    /// <summary>
                                    /// This FC can no longer fire at ordnance groups in range.
                                    /// </summary>
                                    if (MissilesLaunched != MissilesToLaunch)
                                    {
                                        break;
                                    }
                                }
                            }

                            /// <summary>
                            /// advance to the next FC.
                            /// </summary>
                            if (MissilesLaunched != MissilesToLaunch && MissilesToLaunch != 0)
                            {
                                break;
                            }

                        }
                    }
                }
            }
        }
    }
}
