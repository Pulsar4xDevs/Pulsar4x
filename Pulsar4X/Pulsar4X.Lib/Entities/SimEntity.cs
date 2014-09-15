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

    public enum InterruptType
    {
        NewSensorContact,
        Count
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
        /// Should subpulses be interrupted?
        /// </summary>
        public bool Interrupt { get; set; }

        /// <summary>
        /// What caused the interrupt?
        /// </summary>
        public InterruptType TypeOfInterrupt { get; set; }

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
        /// Allow other modules to inform simEntity that it should interrupt the sub pulse process, and why.
        /// </summary>
        /// <param name="Type"></param>
        public void SetInterrupt(InterruptType Type)
        {
            Interrupt = true;
            TypeOfInterrupt = Type;
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
            Interrupt = false;

            /// <summary>
            /// Update all last positions here, since this shouldn't be done mid subpulse.
            /// </summary>
            foreach(Faction faction in P)
            {
                foreach(TaskGroupTN TaskGroup in faction.TaskGroups)
                {
                    TaskGroup.UpdateLastPosition();
                }
            }

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
            /// just check active for now.
            /// The idea here is that the time it would take a fleet to travel through the entire detection radius of the opposing fleet should be the subpulse value.
            /// this should make sure a spot event happens for now. I'll likely have to revise this though.
            /// </summary>

            /// <summary>
            /// can't foreach this one.
            /// </summary>
            for (int FleetInterceptIterator = 0; FleetInterceptIterator < FleetInterceptPreemptList.Count; FleetInterceptIterator++)
            {
                TaskGroupTN FleetIntercept1 = FleetInterceptPreemptList[FleetInterceptIterator];
                TaskGroupTN FleetIntercept2 = FleetInterceptPreemptList[FleetInterceptIterator + 1];

                /// <summary>
                /// get the largest TCS ship ID from the active sort list.
                /// </summary>
                int ShipID1 = FleetIntercept1.ActiveSortList.Last();
                int ShipID2 = FleetIntercept2.ActiveSortList.Last();

                ShipTN Large1 = FleetIntercept1.Ships[ShipID1];
                ShipTN Large2 = FleetIntercept2.Ships[ShipID2];

                StarSystem CurrentSystem = FleetIntercept1.Contact.CurrentSystem;

                /// <summary>
                /// get the distance, which involves going to the distance table(as opposed to recalculating distance here).
                /// </summary>
                int TGID1 = CurrentSystem.SystemContactList.IndexOf(FleetIntercept1.Contact);
                int TGID2 = CurrentSystem.SystemContactList.IndexOf(FleetIntercept2.Contact);

                float dist = CurrentSystem.SystemContactList[TGID1].DistanceTable[TGID2];

                int sig1 = Large1.TotalCrossSection - 1;
                int sig2 = Large2.TotalCrossSection - 1;

                if (sig1 > Constants.ShipTN.ResolutionMax - 1)
                    sig1 = Constants.ShipTN.ResolutionMax - 1;

                if (sig2 > Constants.ShipTN.ResolutionMax - 1)
                    sig2 = Constants.ShipTN.ResolutionMax - 1;

                int detection1 = -1;
                int detection2 = -1;

                int TimeToCross1 = -1;
                int TimeToCross2 = -1;

                /// <summary>
                /// here I want to find out what the detection factors are, and how much time each taskgroup requires to cross the detection factor of the opposing taskgroup.
                /// </summary>
                if (FleetIntercept1.ActiveSensorQue.Count != 0)
                {
                    detection1 = FleetIntercept1.ActiveSensorQue[FleetIntercept1.TaskGroupLookUpST[sig2]].aSensorDef.GetActiveDetectionRange(sig2, -1);
                    float speedAdj = FleetIntercept2.CurrentSpeed / 10000.0f;

                    TimeToCross1 = detection1 / (int)Math.Floor(speedAdj);
                }
                if (FleetIntercept2.ActiveSensorQue.Count != 0)
                {
                    detection2 = FleetIntercept2.ActiveSensorQue[FleetIntercept2.TaskGroupLookUpST[sig1]].aSensorDef.GetActiveDetectionRange(sig1, -1);
                    float speedAdj = FleetIntercept1.CurrentSpeed / 10000.0f;

                    TimeToCross2 = detection2 / (int)Math.Floor(speedAdj);
                }

                if (TimeToCross1 == -1 && TimeToCross2 == -1)
                    continue;
                else
                {
                    /// <summary>
                    /// if lower than the current advance time, this should be set as the current advance time.
                    /// </summary>
                    int time = -1;
                    if (TimeToCross1 == -1)
                        time = TimeToCross2;
                    else if (TimeToCross2 == -1)
                        time = TimeToCross1;
                    else
                        time = Math.Min(TimeToCross1, TimeToCross2);

                    if (time < AdvanceTime)
                    {
                        AdvanceTime = time;
                        Pulses = (int)(Constants.TimeInSeconds.Day / AdvanceTime);
                    }
                }



            }

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
                foreach(Faction faction in P)
                {
                    foreach (KeyValuePair<ComponentTN, bool> pair in faction.OpenFireFCType)
                    {
                        /// <summary>
                        /// BFC
                        /// </summary>
                        if (pair.Value == false)
                        {
                            foreach(BeamTN BeamWeapon in faction.OpenFireFC[pair.Key].ShipBFC[pair.Key.componentIndex].linkedWeapons) 
                            {
                                if (BeamWeapon.readyToFire() == true)
                                {
                                    FCInterruptTime = (int)Constants.TimeInSeconds.FiveSeconds;
                                    done = true;
                                    break;
                                }
                                else if (FCInterruptTime != ((int)Constants.TimeInSeconds.FiveSeconds * 2))
                                {
                                    FCInterruptTime = BeamWeapon.timeToFire();
                                }
                            }

                            if (done == true)
                                break;

                            foreach (TurretTN Turret in faction.OpenFireFC[pair.Key].ShipBFC[pair.Key.componentIndex].linkedTurrets)
                            {
                                if (Turret.readyToFire() == true)
                                {
                                    FCInterruptTime = (int)Constants.TimeInSeconds.FiveSeconds;
                                    done = true;
                                    break;
                                }
                                else if (FCInterruptTime != ((int)Constants.TimeInSeconds.FiveSeconds * 2))
                                {
                                    FCInterruptTime = Turret.timeToFire();
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
                            foreach(MissileLauncherTN LaunchTube in faction.OpenFireFC[pair.Key].ShipMFC[pair.Key.componentIndex].linkedWeapons)
                            {
                                if (LaunchTube.readyToFire() == true)
                                {
                                    FCInterruptTime = (int)Constants.TimeInSeconds.FiveSeconds;
                                    done = true;
                                    break;
                                }
                                else if (FCInterruptTime != ((int)Constants.TimeInSeconds.FiveSeconds * 2))
                                {
                                    FCInterruptTime = LaunchTube.timeToFire();
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
                    else if (Interrupt == true)
                    {
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
#warning magic number 1Billion overflow prevention kludge here.
            if (CurrentTick > 1000000000)
            {
                CurrentTick = CurrentTick - 1000000000;
            }
            lastTick = CurrentTick;
            CurrentTick += tickValue;

            /// <summary>
            /// Update the position of all planets. This should probably be in something like the construction tick in Aurora.
            /// </summary>
            foreach(StarSystem CurrentSystem in GameState.Instance.StarSystems)
            {
                foreach (Star CurrentStar in CurrentSystem.Stars) 
                {
                    /// <summary>
                    /// The system primary will cause a divide by zero error currently as it has no orbit.
                    /// </summary>
                    if (CurrentStar != CurrentSystem.Stars[0])
                        Pulsar4X.Lib.OrbitTable.Instance.UpdatePosition(CurrentStar, tickValue);

                    foreach (Planet CurrentPlanet in CurrentStar.Planets) 
                    {
                        Pulsar4X.Lib.OrbitTable.Instance.UpdatePosition(CurrentPlanet, tickValue);

                        /// <summary>
                        /// Adjust planet position based on the primary. Right now XSystem and YSystem assume orbiting around 0,0. secondary stars, and eventually moons will have this issue.
                        /// </summary>
                        CurrentPlanet.XSystem = CurrentPlanet.XSystem + CurrentStar.XSystem;
                        CurrentPlanet.YSystem = CurrentPlanet.YSystem + CurrentStar.YSystem;
                    }
                    
                }
            }

            /// <summary>
            /// Missiles should check to see if they have a target, move towards it, and hit it. If they have no target then they should check their sensor and either move to new target,
            /// or more towards last known firing location. ProcessOrder should handle all of these.
            /// </summary>
            foreach(Faction faction in P) 
            {
                foreach(OrdnanceGroupTN OrdnanceGroup in faction.MissileGroups) 
                {
                    OrdnanceGroup.ProcessOrder((uint)(CurrentTick - lastTick), RNG);

                    /// <summary>
                    /// Handle missile interception sub pulse preemption here.
                    /// </summary>
                    if (MissileInterceptPreemptTick != CurrentTick)
                    {
                        MissileInterceptPreemptTick = CurrentTick;
                        MissileTimeToHit = (int)OrdnanceGroup.timeReq;
                    }
                    else if (MissileInterceptPreemptTick == CurrentTick && OrdnanceGroup.timeReq < MissileTimeToHit)
                    {
                        MissileTimeToHit = (int)OrdnanceGroup.timeReq;
                    }


                    /// <summary>
                    /// If a missile destroyed its target, handle that here by informing the recharge list that said target must be removed from the sim.
                    /// Also put this missile group in the missile remove list, which either removes missiles from the group, or deletes the entire group if it has no more misisles left.
                    /// </summary>
                    if (OrdnanceGroup.missilesDestroyed != 0 && faction.MissileRemoveList.Contains(OrdnanceGroup) == false)
                    {

                        switch (OrdnanceGroup.missiles[0].target.targetType)
                        {
#warning should any planet/pop stuff be taken care of here?
                            case StarSystemEntityType.TaskGroup:
                                ShipTN MissileTarget = OrdnanceGroup.missiles[0].target.ship;
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
                        faction.MissileRemoveList.Add(OrdnanceGroup);
                    }
                }
            }

            /// <summary>
            /// Taskgroup Follow orders here. perform orders, and update the travel line which tells the gui how to draw the line denoting where this tg has traveled.
            /// </summary>
            foreach(Faction faction in P)  
            {
                foreach(TaskGroupTN TaskGroup in faction.TaskGroups) 
                {
                    /// <summary>
                    /// Adding new taskgroups means adding a loop here to run through them all.
                    /// </summary>
                    if (TaskGroup.TaskGroupOrders.Count != 0)
                    {
                        TaskGroup.FollowOrders((uint)(CurrentTick - lastTick));
                    }
                    else if (TaskGroup.DrawTravelLine == 1)
                    {
                        TaskGroup.UpdateLastPosition();

                        TaskGroup.DrawTravelLine = 2;
                    }
                }
            }

            /// <summary>
            /// Do sensor sweeps here. Sensors must be done after movement, not before. Missile sensors should also be here, but they need an individual check if they have no target early on.
            /// </summary>
            foreach (Faction faction in P)
            {
                faction.SensorSweep(CurrentTick);
            }

            /// <summary>
            /// Insert Area Defense/ AMM Defense here.
            /// </summary>
            foreach (Faction faction in P)
            {
                AreaDefensiveFire(faction,RNG);
            }

            /// <summary>
            /// attempt to fire weapons at target here.
            /// Initiative will have to be implemented here for "fairness". right now lower P numbers have the advantage.
            /// Check for destroyed ships as well.
            /// </summary>
#warning ships fire in order, initiative does not play a part
            #region Fire Weapons
            foreach (Faction faction in P) //for (int loop = factionStart; loop < factionCount; loop++)
            {
                foreach (KeyValuePair<ComponentTN, ShipTN> pair in faction.OpenFireFC)
                {
                    ShipTN ShipToFire = pair.Value;
                    
                    /// <summary>
                    /// Is BFC
                    /// </summary>
                    if (faction.OpenFireFCType[pair.Key] == true)
                    {
                        BeamFireControlTN ShipFireControl = pair.Value.ShipBFC[pair.Key.componentIndex];

                        /// <summary>
                        /// Open fire and not destroyed.
                        /// </summary>
                        if (ShipFireControl.openFire == true && ShipFireControl.isDestroyed == false &&
                            ShipFireControl.target != null)
                        {
                            if (ShipFireControl.target.targetType == StarSystemEntityType.TaskGroup)
                            {
                                ShipTN Target = ShipFireControl.target.ship;

                                /// <summary>
                                /// Same System as target and target exists.
                                /// </summary>
                                if (ShipToFire.ShipsTaskGroup.Contact.CurrentSystem == Target.ShipsTaskGroup.Contact.CurrentSystem && Target.IsDestroyed == false)
                                {

                                    StarSystem CurSystem = ShipToFire.ShipsTaskGroup.Contact.CurrentSystem;
                                    int MyID = CurSystem.SystemContactList.IndexOf(ShipToFire.ShipsTaskGroup.Contact);
                                    int TargetID = CurSystem.SystemContactList.IndexOf(Target.ShipsTaskGroup.Contact);

                                    if (ShipToFire.ShipsFaction.DetectedContactLists.ContainsKey(CurSystem) == true)
                                    {
                                        if (ShipToFire.ShipsFaction.DetectedContactLists[CurSystem].DetectedContacts.ContainsKey(Target) == true)
                                        {
                                            /// <summary>
                                            /// This tick active detection.
                                            /// </summary>
                                            if (ShipToFire.ShipsFaction.DetectedContactLists[CurSystem].DetectedContacts[Target].active == true)
                                            {
                                                bool WF = ShipToFire.ShipFireWeapons(CurrentTick, RNG);

                                                /// <summary>
                                                /// Update the recharge list since the target must be destroyed.
                                                /// </summary>
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
                                                    if (faction.RechargeList.ContainsKey(pair.Value) == true)
                                                    {
                                                        int value = faction.RechargeList[pair.Value];

                                                        if ((value & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                                        {
                                                            faction.RechargeList[pair.Value] = value + (int)Faction.RechargeStatus.Weapons;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        faction.RechargeList.Add(pair.Value, (int)Faction.RechargeStatus.Weapons);
                                                    }
                                                }
                                            }//end if active detection
                                        }//end if is detected
                                    }//end if system has detected contacts
                                }//end if in same system
                            }//end if targetType == TaskGroup
                            else if (ShipFireControl.target.targetType == StarSystemEntityType.Missile)
                            {
                                OrdnanceGroupTN Target = ShipFireControl.target.missileGroup;

                                /// <summary>
                                /// Same system, and target has missiles to be destroyed.
                                /// </summary>
                                if (ShipToFire.ShipsTaskGroup.Contact.CurrentSystem == Target.contact.CurrentSystem &&( Target.missilesDestroyed != Target.missiles.Count))
                                {
                                    StarSystem CurSystem = ShipToFire.ShipsTaskGroup.Contact.CurrentSystem;
                                    int MyID = CurSystem.SystemContactList.IndexOf(ShipToFire.ShipsTaskGroup.Contact);
                                    int TargetID = CurSystem.SystemContactList.IndexOf(Target.contact);

                                    if (ShipToFire.ShipsFaction.DetectedContactLists.ContainsKey(CurSystem) == true)
                                    {
                                        if (ShipToFire.ShipsFaction.DetectedContactLists[CurSystem].DetectedMissileContacts.ContainsKey(Target) == true)
                                        {
                                            /// <summary>
                                            /// This tick active detection.
                                            /// </summary>
                                            if (ShipToFire.ShipsFaction.DetectedContactLists[CurSystem].DetectedMissileContacts[Target].active == true)
                                            {
                                                bool WF = ShipToFire.ShipFireWeapons(CurrentTick, RNG);

                                                if (Target.missilesDestroyed != 0 && Target.ordnanceGroupFaction.MissileRemoveList.Contains(Target) == false)
                                                {
                                                    Target.ordnanceGroupFaction.MissileRemoveList.Add(Target);
                                                }

                                                if (WF == true)
                                                {
                                                    if (faction.RechargeList.ContainsKey(pair.Value) == true)
                                                    {
                                                        int value = faction.RechargeList[pair.Value];

                                                        if ((value & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                                        {
                                                            faction.RechargeList[ShipToFire] = value + (int)Faction.RechargeStatus.Weapons;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        faction.RechargeList.Add(ShipToFire, (int)Faction.RechargeStatus.Weapons);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }//end if same system
                            }
                        }//end if isOpenFire isDestroyed=false, target!= null
                    }//end if isBFC = true
                    /// <summary>
                    /// Therefore this is a missile fire control.
                    /// </summary>
                    else
                    {
                        MissileFireControlTN ShipMFireControl = pair.Value.ShipMFC[pair.Key.componentIndex];

                        /// <summary>
                        /// Missile fire controls should be fairly simple, the missile itself does most of the lifting.
                        /// </summary>
                        if (ShipMFireControl.openFire == true && ShipMFireControl.isDestroyed == false &&
                            ShipMFireControl.target != null)
                        {
                            bool WF = ShipMFireControl.FireWeapons(ShipToFire.ShipsTaskGroup, pair.Value);

                            /// <summary>
                            /// Since this ship has fired its missile launch tubes, they will need to be reloaded, put this ship in the recharge list.
                            /// </summary>
                            if (WF == true)
                            {
                                if (faction.RechargeList.ContainsKey(pair.Value) == true)
                                {
                                    int value = faction.RechargeList[pair.Value];

                                    if ((value & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                    {
                                        faction.RechargeList[pair.Value] = value + (int)Faction.RechargeStatus.Weapons;
                                    }
                                }
                                else
                                {
                                    faction.RechargeList.Add(pair.Value, (int)Faction.RechargeStatus.Weapons);
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

            /// <summary>
            /// can't foreach this one, I do some horrible stuff here that relies on being able to manipulate factionIterator.
            for(int factionIterator = factionStart; factionIterator < factionCount; factionIterator++)
            {
                Faction faction = P[factionIterator];
                loopBreak = false;

                foreach (KeyValuePair<ShipTN, int> pair in faction.RechargeList)
                {
                    ShipTN Ship = pair.Key;

                    /// <summary>
                    /// What does this ship need to have done to it this tick?
                    /// </summary>
                    int value = pair.Value;

                    if ((value & (int)Faction.RechargeStatus.Shields) == (int)Faction.RechargeStatus.Shields)
                    {
                        Ship.RechargeShields(TimeValue);
                    }


                    if ((value & (int)Faction.RechargeStatus.Weapons) == (int)Faction.RechargeStatus.Weapons)
                    {
                        int ShotsExp;
                        int ret = Ship.RechargeBeamWeapons(TimeValue, out ShotsExp);

                        ushort amt = (ushort)(Math.Floor((float)TimeValue / 5.0f));
                        int PowerComp = Ship.CurrentPowerGen * amt;

                        bool allTubesLoaded = Ship.ReloadLaunchTubes(TimeValue);

                        /// <summary>
                        /// When all tubes are loaded and have remained loaded for atleast 1 tick reloadLaunchTubes should return true. 
                        /// Likewise when no beam weapon recharging is to be done power will sit at full for at least one tick.
                        /// This should keep continuously firing weapons in this list even if they are considered recharged for a single sliver of time.
                        /// ShotsExp is to handle gauss cannon "reloading". Point defense imposes this requirement. A ShotsExp of zero means that no gauss cannon fired
                        /// in point defense during the last tick. also will come up for multibarrel turrets.
                        /// </summary>
                        if (ret == PowerComp && allTubesLoaded == true && ShotsExp == 0)
                        {
                            faction.RechargeList[Ship] = faction.RechargeList[pair.Key] - (int)Faction.RechargeStatus.Weapons;

                            if (faction.RechargeList[Ship] == 0)
                            {
                                faction.RechargeList.Remove(Ship);

                                /// <summary>
                                /// Specifically this horrible thing:
                                /// </summary>
                                factionIterator--;
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
                        int shots = Ship.RechargeCIWS();

                        /// <summary>
                        /// I've recharged this ship twice, but its CIWS have not fired on anything in the mean time. so remove it from the list.
                        /// </summary>
                        if (shots == 0)
                        {
                            faction.RechargeList[Ship] = faction.RechargeList[Ship] - (int)Faction.RechargeStatus.CIWS;

                            /// <summary>
                            /// If no flags are present at all for this ship, remove it entirely.
                            /// </summary>

                            if (faction.RechargeList[Ship] == 0)
                            {
                                faction.RechargeList.Remove(Ship);
                                factionIterator--;
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

                        foreach(Faction CurrentFaction in P)
                        {
                            StarSystem CurSystem = Ship.ShipsTaskGroup.Contact.CurrentSystem;
                            if(CurrentFaction.DetectedContactLists.ContainsKey(CurSystem) == true)
                            {
                                if (CurrentFaction.DetectedContactLists[CurSystem].DetectedContacts.ContainsKey(Ship) == true)
                                {
                                    CurrentFaction.DetectedContactLists[CurSystem].DetectedContacts.Remove(Ship);
                                }
                            }
                        }

                        bool nodeGone = Ship.OnDestroyed();
                        Ship.ShipClass.ShipsInClass.Remove(pair.Key);
                        Ship.ShipsTaskGroup.Ships.Remove(pair.Key);
                        Ship.ShipsFaction.Ships.Remove(pair.Key);

                        if (Ship.ShipsTaskGroup.Ships.Count == 0)
                        {
                            RemoveFriendlyTaskGroupsOrdered(pair);
                        }

                        RemoveShipsTargetting(pair);

                        faction.RechargeList.Remove(pair.Key);

                        /// <summary>
                        /// Have to re-run loop since a ship was removed from all kinds of things.
                        /// </summary>
                        factionIterator--;
                        loopBreak = true;
                        break;
                    }
                }

                /// <summary>
                /// Skip this section of code if loop was broken. the loop will be reprocessed so everything will be done eventually.
                /// </summary>
                if (loopBreak == false)
                {

                    foreach(OrdnanceGroupTN MissileRemove in faction.MissileRemoveList)
                    {
                        /// <summary>
                        /// every missile in this list will either have missiles removed, or needs to be deleted as an ordnance group.
                        /// </summary>
                        if (MissileRemove.missiles.Count > MissileRemove.missilesDestroyed)
                        {
                            for (int missileIterator = 0; missileIterator < MissileRemove.missilesDestroyed; missileIterator++)
                            {
                                MissileRemove.RemoveMissile(MissileRemove.missiles[0]);
                            }

                            MissileRemove.missilesDestroyed = 0;
                        }
                        else
                        {
                            RemoveOrdnanceGroupFromSim(MissileRemove, P);
                        }
                    }

                    faction.MissileRemoveList.Clear();
                }
            }
            #endregion
              
        }

        #region AdvanceSim Ship/ordnance group destruction related Private functions

        /// <summary>
        /// All taskgroups ordered on the current destroyed ship have to have those orders canceled.
        /// This is for hostile ships, tugs may also eventually make use of this.
        /// </summary>
        /// <param name="pair">KeyValuePair of the ship involved</param>
        private void RemoveTaskGroupsOrdered(KeyValuePair<ShipTN, int> pair)
        {
            foreach(TaskGroupTN TaskGroupOrdered in pair.Key.TaskGroupsOrdered)
            {
                for(int orderIterator = 0; orderIterator < TaskGroupOrdered.TaskGroupOrders.Count; orderIterator++) 
                {
                    Orders TaskGroupOrder = TaskGroupOrdered.TaskGroupOrders[orderIterator];

                    if (TaskGroupOrder.target.SSEntity == StarSystemEntityType.TaskGroup)
                    {
                        if (TaskGroupOrder.taskGroup == pair.Key.ShipsTaskGroup)
                        {
                            /// <summary>
                            /// At this point it has been established that the destroyed ship has TGs ordered to it some how(enemy contact ordering).
                            /// That the ordered TG has multiple TG orders
                            /// That the current order target is a taskgroup, and in fact this taskgroup.
                            /// </summary>

                            String Entry = String.Format("Taskgroup {0} cannot find target, orders canceled.", TaskGroupOrdered.Name);
                            MessageEntry Entry2 = new MessageEntry(MessageEntry.MessageType.OrdersNotCompleted, TaskGroupOrdered.Contact.CurrentSystem, TaskGroupOrdered.Contact,
                                                                   GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                            TaskGroupOrdered.TaskGroupFaction.MessageLog.Add(Entry2);

                            int lastOrder = TaskGroupOrdered.TaskGroupOrders.Count - 1;
                            for (int orderListIterator = lastOrder; orderListIterator >= orderIterator; orderListIterator--)
                            {
                                TaskGroupOrdered.TaskGroupOrders.RemoveAt(orderListIterator);
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
            ShipTN Ship = pair.Key;

            foreach (TaskGroupTN TaskGroupOrdered in Ship.TaskGroupsOrdered) 
            {
                for (int orderIterator = 0; orderIterator < TaskGroupOrdered.TaskGroupOrders.Count; orderIterator++)
                {
                    Orders TaskGroupOrder = TaskGroupOrdered.TaskGroupOrders[orderIterator];

                    if (TaskGroupOrder.target.SSEntity == StarSystemEntityType.TaskGroup)
                    {
                        if (TaskGroupOrder.taskGroup == Ship.ShipsTaskGroup)
                        {
                            /// <summary>
                            /// At this point it has been established that the destroyed TG has TGs ordered to it, friendly TGs.
                            /// That the ordered TG has multiple TG orders
                            /// That the current order target is a taskgroup, and in fact this taskgroup.
                            /// </summary>

                            String Entry = String.Format("Taskgroup {0} cannot find target, orders canceled.", TaskGroupOrdered.Name);
                            MessageEntry Entry2 = new MessageEntry(MessageEntry.MessageType.OrdersNotCompleted, TaskGroupOrdered.Contact.CurrentSystem, TaskGroupOrdered.Contact,
                                                                   GameState.Instance.GameDateTime, (GameState.SE.CurrentTick - GameState.SE.lastTick), Entry);
                            TaskGroupOrdered.TaskGroupFaction.MessageLog.Add(Entry2);

                            int lastOrder = TaskGroupOrdered.TaskGroupOrders.Count - 1;
                            for (int orderListIterator = lastOrder; orderListIterator >= orderIterator; orderListIterator--)
                            {
                                TaskGroupOrdered.TaskGroupOrders.RemoveAt(orderListIterator);
                            }
                            break;
                        }
                    }
                }
            }

            Ship.ShipsTaskGroup.clearAllOrders();
            Ship.ShipsTaskGroup.Contact.CurrentSystem.RemoveContact(Ship.ShipsTaskGroup.Contact);
            Ship.ShipsFaction.TaskGroups.Remove(Ship.ShipsTaskGroup);        
        }

        /// <summary>
        /// Any ships that want to fire upon this craft have to be updated to reflect destruction
        /// </summary>
        /// <param name="pair">Key value pair of the ship itself.</param>
        private void RemoveShipsTargetting(KeyValuePair<ShipTN, int> pair)
        {
            ShipTN Ship = pair.Key;

            foreach(ShipTN nextShip in Ship.ShipsTargetting) 
            {
                foreach(BeamFireControlTN ShipBeamFC in nextShip.ShipBFC) 
                {
                    if (ShipBeamFC.getTarget().targetType == StarSystemEntityType.TaskGroup && ShipBeamFC.getTarget().ship == Ship)
                    {
                        ShipBeamFC.clearTarget();
                        ShipBeamFC.openFire = false;
                        nextShip.ShipsFaction.OpenFireFC.Remove(ShipBeamFC);
                        nextShip.ShipsFaction.OpenFireFCType.Remove(ShipBeamFC);
                    }
                }

                foreach(MissileFireControlTN ShipMissileFC in nextShip.ShipMFC) 
                {
                    if (ShipMissileFC.getTarget().targetType == StarSystemEntityType.TaskGroup)
                    {
                        if (ShipMissileFC.getTarget().ship == pair.Key)
                        {
                            ShipMissileFC.clearTarget();
                            ShipMissileFC.openFire = false;
                            nextShip.ShipsFaction.OpenFireFC.Remove(ShipMissileFC);
                            nextShip.ShipsFaction.OpenFireFCType.Remove(ShipMissileFC);
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
            foreach(ShipTN nextShip in OGRemove.shipsTargetting) 
            {
                foreach(BeamFireControlTN ShipBeamFC in nextShip.ShipBFC)
                {
                    TargetTN BFCTarget = ShipBeamFC.getTarget();
                    if (BFCTarget != null)
                    {
                        if (BFCTarget.targetType == StarSystemEntityType.Missile && ShipBeamFC.pDState == PointDefenseState.None)
                        {
                            if (BFCTarget.missileGroup == OGRemove)
                            {
                                ShipBeamFC.clearTarget();
                                ShipBeamFC.openFire = false;
                                nextShip.ShipsFaction.OpenFireFC.Remove(ShipBeamFC);
                                nextShip.ShipsFaction.OpenFireFCType.Remove(ShipBeamFC);
                            }
                        }
                    }
                }

                /// <summary>
                /// Clear manually targeted missile fire controls.
                /// </summary>
                foreach(MissileFireControlTN ShipMissileFC in nextShip.ShipMFC ) 
                {
                    TargetTN MFCTarget = ShipMissileFC.getTarget();
                    if (MFCTarget != null)
                    {
                        if (MFCTarget.targetType == StarSystemEntityType.Missile && ShipMissileFC.pDState == PointDefenseState.None)
                        {
                            if (MFCTarget.missileGroup == OGRemove)
                            {
                                /// <summary>
                                /// Clear the target, set open fire to false, update the openFireFC list.
                                /// </summary>
                                ShipMissileFC.clearTarget();
                                ShipMissileFC.openFire = false;
                                nextShip.ShipsFaction.OpenFireFC.Remove(ShipMissileFC);
                                nextShip.ShipsFaction.OpenFireFCType.Remove(ShipMissileFC);

                                /// <summary>
                                /// Set all missiles to their own sensors.
                                /// </summary>
                                foreach(OrdnanceGroupTN MissileGroupInFlight in ShipMissileFC.missilesInFlight)
                                {
                                    MissileGroupInFlight.CheckTracking();
                                }
                            }
                        }
                    }
                }

                /// <summary>
                /// Clear the point defense missiles.
                /// </summary>
                foreach(OrdnanceGroupTN OrdnanceGroupTargetting in OGRemove.ordGroupsTargetting)
                {
                    OrdnanceGroupTargetting.CheckTracking();
                }
            }
            /// <summary>
            /// Finally I need to remove the ordnance group from its faction list, all detection lists, from the system contact list, inform the Sceen to delete this contact, and clear the missile binding list.
            /// Complicated stuff.
            /// </summary>
            foreach(Faction faction in P)
            {
                StarSystem CurSystem = OGRemove.contact.CurrentSystem;
                if (faction.DetectedContactLists.ContainsKey(CurSystem) == true)
                {
                    if (faction.DetectedContactLists[CurSystem].DetectedMissileContacts.ContainsKey(OGRemove) == true)
                    {
                        faction.DetectedContactLists[CurSystem].DetectedMissileContacts.Remove(OGRemove);
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
            foreach(Faction faction in P)
            {
                /// <summary>
                /// Is the current faction different from the missile group faction, and does the faction have a detected contacts list for the current system?
                /// </summary>
                if (faction != Missile.missileGroup.ordnanceGroupFaction && faction.DetectedContactLists.ContainsKey(CurrentSystem) == true)
                {
                    /// <summary>
                    /// Is the Missile group in this detected contact list?
                    /// </summary>
                    if (faction.DetectedContactLists[CurrentSystem].DetectedMissileContacts.ContainsKey(Missile.missileGroup) == true)
                    {
                        /// <summary>
                        /// Is the detection an active detection?
                        /// </summary>
                        if (faction.DetectedContactLists[CurrentSystem].DetectedMissileContacts[Missile.missileGroup].active == true)
                        {
                            /// <summary>
                            /// Does this faction have any point defense enabled FCs in this system?
                            /// </summary>
                            if (faction.PointDefense.ContainsKey(CurrentSystem) == true)
                            {
                                /// <summary>
                                /// loop through all the possible PD enabled FC.
                                /// </summary>
                                foreach (KeyValuePair<ComponentTN, ShipTN> pair in faction.PointDefense[CurrentSystem].PointDefenseFC)
                                {
                                    ShipTN Ship = pair.Value;

                                    /// <summary>
                                    /// Only want BFCs in FDF mode for now.
                                    /// </summary>
                                    if (faction.PointDefense[CurrentSystem].PointDefenseType[pair.Key] == false && pair.Value.ShipBFC[pair.Key.componentIndex].pDState == PointDefenseState.FinalDefensiveFire)
                                    {
                                        BeamFireControlTN ShipBeamFC = pair.Value.ShipBFC[pair.Key.componentIndex];

                                        /// <summary>
                                        /// Do a distance check on pair.Value vs the missile itself. if that checks out to be less than 10k km(or equal to zero), then
                                        /// check to see if the FC can shoot down said missile. This should never be run before a sensor sweep
                                        /// </summary>
                                        float dist = -1;

                                        int MissileID = CurrentSystem.SystemContactList.IndexOf(Missile.missileGroup.contact);
                                        int TGID = CurrentSystem.SystemContactList.IndexOf(Ship.ShipsTaskGroup.Contact);

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
                                            Intercept = ShipBeamFC.InterceptTarget(RNG, 0, Missile, Ship.ShipsFaction,
                                                                                                                    Ship.ShipsTaskGroup.Contact, Ship, out WF);
                                            /// <summary>
                                            /// Add this ship to the weapon recharge list since it has fired. This is done here in Sim, or for FDF_Self in Ship.cs
                                            /// </summary>
                                            if (WF == true)
                                            {
                                                if (faction.RechargeList.ContainsKey(Ship) == true)
                                                {
                                                    /// <summary>
                                                    /// If our recharge value does not have Recharge beams in it(bitflag 2 for now), then add it.
                                                    /// </summary>
                                                    if ((faction.RechargeList[pair.Value] & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                                    {
                                                        faction.RechargeList[pair.Value] = (faction.RechargeList[pair.Value] + (int)Faction.RechargeStatus.Weapons);
                                                    }
                                                }
                                                else
                                                {
                                                    faction.RechargeList.Add(pair.Value, (int)Faction.RechargeStatus.Weapons);
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
                    /// The firing ship.
                    /// </summary>
                    ShipTN Ship = pair2.Value;

                    /// <summary>
                    /// BFC set to Area defense mode
                    /// </summary>
                    if (pair.Value.PointDefenseType[pair2.Key] == false && pair2.Value.ShipBFC[pair2.Key.componentIndex].pDState == PointDefenseState.AreaDefense)
                    {
                        BeamFireControlTN ShipBeamFC = pair2.Value.ShipBFC[pair2.Key.componentIndex];
                        /// <summary>
                        /// loop through every missile contact. will have to do distance checks here.
                        /// </summary>
                        foreach (KeyValuePair<OrdnanceGroupTN, FactionContact> MisPair in Fact.DetectedContactLists[CurrentSystem].DetectedMissileContacts)
                        {
                            OrdnanceGroupTN DetectedMissileGroup = MisPair.Key;
                            /// <summary>
                            /// This missile group is already destroyed and will be cleaned up by sim later.
                            /// </summary>
                            if (DetectedMissileGroup.missilesDestroyed == DetectedMissileGroup.missiles.Count)
                                break;

                            /// <summary>
                            /// Do a distance check on pair.Value vs the missile itself. if that checks out to be less than 10k km(or equal to zero), then
                            /// check to see if the FC can shoot down said missile. This should never be run before a sensor sweep
                            /// </summary>
                            float dist = -1;

                            int MissileID = CurrentSystem.SystemContactList.IndexOf(DetectedMissileGroup.contact);
                            int TGID = CurrentSystem.SystemContactList.IndexOf(Ship.ShipsTaskGroup.Contact);

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
                                /// The user put in an absurdly large value. for convienence, just do this to get maximum range.
                                /// </summary>

                                if (ShipBeamFC.pDRange > (float)Constants.Units.TEN_KM_MAX)
                                {
                                    /// <summary>
                                    /// Max possible beam range in KM.
                                    /// </summary>
                                    rangeAreaDefenseKm = (float)Constants.Units.BEAM_KM_MAX;
                                }
                                else
                                {
#warning magic number related to 10k
                                    rangeAreaDefenseKm = ShipBeamFC.pDRange * 10000.0f;
                                } 

                                float distKM = dist * (float)Constants.Units.KM_PER_AU;

                                /// <summary>
                                /// Additional paranoia check of range, I need to fix Area defense PD range values to ship bfc range in any event, that hasn't been done yet.
                                /// </summary>
#warning magic number for total bfc range.
                                float totalRange = ShipBeamFC.beamFireControlDef.range * 2.0f;

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

                                    /// <summary>
                                    /// Can't foreach this one, as I do not want every missile, only the ones not destroyed.
                                    /// </summary>
                                    for (int MissileIterator = DetectedMissileGroup.missilesDestroyed; MissileIterator < DetectedMissileGroup.missiles.Count; MissileIterator++)
                                    {
                                        bool WF = false;
                                        Intercept = ShipBeamFC.InterceptTarget(RNG, increment, DetectedMissileGroup.missiles[MissileIterator], Ship.ShipsFaction,
                                                                                                                  Ship.ShipsTaskGroup.Contact, Ship, out WF);

                                        /// <summary>
                                        /// Add this ship to the weapon recharge list since it has fired. This is done here in Sim, or for FDF_Self in Ship.cs
                                        /// </summary>
                                        if (WF == true)
                                        {
                                            if (Fact.RechargeList.ContainsKey(Ship) == true)
                                            {
                                                /// <summary>
                                                /// If our recharge value does not have Recharge beams in it(bitflag 2 for now), then add it.
                                                /// </summary>
                                                if ((Fact.RechargeList[Ship] & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                                {
                                                    Fact.RechargeList[Ship] = (Fact.RechargeList[Ship] + (int)Faction.RechargeStatus.Weapons);
                                                }
                                            }
                                            else
                                            {
                                                Fact.RechargeList.Add(Ship, (int)Faction.RechargeStatus.Weapons);
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
                                    DetectedMissileGroup.missilesDestroyed = DetectedMissileGroup.missilesDestroyed + MissilesToDestroy;

                                    if (DetectedMissileGroup.missilesDestroyed != 0 && Fact.MissileRemoveList.Contains(DetectedMissileGroup) == false)
                                    {
                                        /// <summary>
                                        /// Tell sim to remove missiles from this group, or remove it entirely.
                                        /// </summary>
                                        Fact.MissileRemoveList.Add(DetectedMissileGroup);
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

                        MissileFireControlTN ShipMissileFC = pair2.Value.ShipMFC[pair2.Key.componentIndex];

                        int MissilesLaunched = 0;
                        int MissilesToLaunch = 0;
                        foreach (KeyValuePair<OrdnanceGroupTN, FactionContact> MisPair in Fact.DetectedContactLists[CurrentSystem].DetectedMissileContacts)
                        {
                            OrdnanceGroupTN DetectedMissileGroup = MisPair.Key;

                            /// <summary>
                            /// Advance to next missile group.
                            /// </summary>
                            if (DetectedMissileGroup.missilesDestroyed == DetectedMissileGroup.missiles.Count)
                            {
                                break;
                            }

                            /// <summary>
                            /// Do a distance check on pair.Value vs the missile itself. if that checks out to be less than 10k km(or equal to zero), then
                            /// check to see if the FC can shoot down said missile. This should never be run before a sensor sweep
                            /// </summary>
                            float dist = -1;

                            int MissileID = CurrentSystem.SystemContactList.IndexOf(DetectedMissileGroup.contact);
                            int TGID = CurrentSystem.SystemContactList.IndexOf(Ship.ShipsTaskGroup.Contact);

                            /// <summary>
                            /// dist is in AU.
                            /// </summary>
                            dist = CurrentSystem.SystemContactList[MissileID].DistanceTable[TGID];


                            float MFCEngageDistKm = ShipMissileFC.mFCSensorDef.maxRange;
                            float rangeAreaDefenseKm = ShipMissileFC.pDRange;

                            /// <summary>
                            /// Range is in 10K units so it has to be adjusted to AU for later down.
                            /// </summary>
#warning magic 10k number here
                            float range = (Math.Min(MFCEngageDistKm, rangeAreaDefenseKm) / (float)Constants.Units.KM_PER_AU) ;
                            range = range * 10000.0f;

                            int MSize = 0;
                            int AltMSize = 0;

#warning the +6 is another magic number.
                            if ((int)Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size) <= (Constants.OrdnanceTN.MissileResolutionMinimum + 6))
                            {
                                MSize = Constants.OrdnanceTN.MissileResolutionMinimum;
                                AltMSize = 0;
                            }
                            else if ((int)Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size) <= (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                            {
                                MSize = (int)Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size);
                                AltMSize = 0;
                            }
                            else if ((int)Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size) > (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                            {
                                MSize = -1;
                                AltMSize = (int)Math.Ceiling(Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size) / (Constants.OrdnanceTN.MissileResolutionMaximum + 6));
                            }

                            int MFCRange = ShipMissileFC.mFCSensorDef.GetActiveDetectionRange(AltMSize, MSize);

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
                                foreach(OrdnanceGroupTN OrdGroupTargetting in DetectedMissileGroup.ordGroupsTargetting)
                                {
                                    TotalCount = TotalCount + OrdGroupTargetting.missiles.Count;
                                }

                                float Value = TotalCount / DetectedMissileGroup.missiles.Count;

                                switch (ShipMissileFC.pDState)
                                {
#warning more magic numbers in how point defense states are handled.
                                    case PointDefenseState.AMM1v2:
                                        if (Value < 0.5f)
                                        {
                                            int Max = (int)Math.Ceiling((float)DetectedMissileGroup.missiles.Count / 2.0f);
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                    case PointDefenseState.AMM1v1:
                                        if (Value < 1.0f)
                                        {
                                            int Max = DetectedMissileGroup.missiles.Count;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                    case PointDefenseState.AMM2v1:
                                        if (Value < 2.0f)
                                        {
                                            int Max = DetectedMissileGroup.missiles.Count * 2;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                    case PointDefenseState.AMM3v1:
                                        if (Value < 3.0f)
                                        {
                                            int Max = DetectedMissileGroup.missiles.Count * 3;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                    case PointDefenseState.AMM4v1:
                                        if (Value < 4.0f)
                                        {
                                            int Max = DetectedMissileGroup.missiles.Count * 4;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                    case PointDefenseState.AMM5v1:
                                        if (Value < 5.0f)
                                        {
                                            int Max = DetectedMissileGroup.missiles.Count * 5;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                    break;
                                }

                                if (MissilesToLaunch != 0)
                                {
                                    /// <summary>
                                    /// launch up to MissilesToLaunch amms in a new ord group at the target.
                                    /// <summary>
                                    MissilesLaunched = ShipMissileFC.FireWeaponsPD(Ship.ShipsTaskGroup, Ship, DetectedMissileGroup, MissilesToLaunch);


                                    /// <summary>
                                    /// Add this ship to the weapon recharge list since it has fired. This is done here in Sim, or for FDF_Self in Ship.cs
                                    /// </summary>
                                    if (MissilesLaunched != 0)
                                    {
                                        if (Fact.RechargeList.ContainsKey(Ship) == true)
                                        {
                                            /// <summary>
                                            /// If our recharge value does not have Recharge beams in it(bitflag 2 for now), then add it.
                                            /// </summary>
                                            if ((Fact.RechargeList[Ship] & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                            {
                                                Fact.RechargeList[Ship] = (Fact.RechargeList[Ship] + (int)Faction.RechargeStatus.Weapons);
                                            }
                                        }
                                        else
                                        {
                                            Fact.RechargeList.Add(Ship, (int)Faction.RechargeStatus.Weapons);
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
