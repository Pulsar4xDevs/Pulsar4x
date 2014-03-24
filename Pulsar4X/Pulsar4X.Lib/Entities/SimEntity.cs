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
    /// Most of this class is deprecated and can be removed.
    /// I'll do that at some point in the future.
    /// </summary>
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
                    int MissilesToRemove = P[loop].MissileGroups[loop2].ProcessOrder((uint)(CurrentTick - lastTick), RNG);
                    

                    /// <summary>
                    /// This necessarily indicates ship destruction.
                    /// </summary>
                    if (MissilesToRemove != 0)
                    {
                        if (P[loop].MissileGroups[loop2].missiles[0].target.targetType == StarSystemEntityType.TaskGroup)
                        {
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
                        }
                        else if (P[loop].MissileGroups[loop2].missiles[0].target.targetType == StarSystemEntityType.Missile)
                        {
                            OrdnanceGroupTN MissilesTarget = P[loop].MissileGroups[loop2].missiles[0].target.missileGroup;

                            if (MissilesTarget != null)
                            {
                                if (MissilesTarget.missiles.Count == 0)
                                {
                                    RemoveOrdnanceGroupFromSim(MissilesTarget.ordnanceGroupFaction, MissilesTarget);
                                }
                            }
                        }

                        /// <summary>
                        /// Test this, I think it should return MissileCount-1 if all missiles are destroyed.
                        /// </summary>
                        for (int loop3 = 0; loop3 <= MissilesToRemove; loop3++)
                        {
                            P[loop].MissileGroups[loop2].RemoveMissile(P[loop].MissileGroups[loop2].missiles[loop3]);
                        }
                    }

                    /// <summary>
                    /// Either through running out of fuel, or hitting their target, all the missiles in this ordnance group are gone.
                    /// </summary>
                    if (P[loop].MissileGroups[loop2].missiles.Count == 0)
                    {
                        RemoveOrdnanceGroupFromSim(P[loop], P[loop].MissileGroups[loop2]);
                        
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
            /// attempt to fire weapons at target here.
            /// Initiative will have to be implemented here for "fairness". right now lower P numbers have the advantage.
            /// Check for destroyed ships as well.
            /// </summary>
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
                                /// Same System as target.
                                /// </summary>
                                if (pair.Value.ShipsTaskGroup.Contact.CurrentSystem == Target.ShipsTaskGroup.Contact.CurrentSystem)
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
                                if (pair.Value.ShipsTaskGroup.Contact.CurrentSystem == Target.contact.CurrentSystem)
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

                                                if (Target.missiles.Count == 0)
                                                {
                                                    RemoveOrdnanceGroupFromSim(Target.ordnanceGroupFaction, Target);
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

            /// <summary>
            /// Do simulation maintenance here, shields,reload,recharge,etc.
            /// </summary>
            uint TimeValue = (uint)(CurrentTick - lastTick);
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
                        int ret = pair.Key.RechargeBeamWeapons(TimeValue);

                        ushort amt = (ushort)(Math.Floor((float)TimeValue / 5.0f));
                        int PowerComp = pair.Key.CurrentPowerGen * amt;

                        bool allTubesLoaded = pair.Key.ReloadLaunchTubes(TimeValue);

                        /// <summary>
                        /// When all tubes are loaded and have remained loaded for atleast 1 tick reloadLaunchTubes should return true. 
                        /// Likewise when no beam weapon recharging is to be done power will sit at full for at least one tick.
                        /// This should keep continuously firing weapons in this list even if they are considered recharged for a single sliver of time.
                        /// </summary>
                        if (ret == PowerComp && allTubesLoaded == true)
                        {
                            P[loop].RechargeList[pair.Key] = P[loop].RechargeList[pair.Key] - (int)Faction.RechargeStatus.Weapons;

                            if (P[loop].RechargeList[pair.Key] == 0)
                            {
                                P[loop].RechargeList.Remove(pair.Key);
                                loop--;
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
                        break;
                    }
                }
            }

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
        private void RemoveOrdnanceGroupFromSim(Faction Owner, OrdnanceGroupTN OGRemove)
        {
            /// <summary>
            /// This ordnance group needs to be removed.
            /// Ships Can be targeted on this ordnance group, from these ships missiles in flight can be tracked and informed.
            /// </summary>
           
            for (int loop = 0; loop < OGRemove.shipsTargetting.Count; loop++)
            {
                ShipTN nextShip = OGRemove.shipsTargetting[loop];
                for (int loop2 = 0; loop2 < nextShip.ShipBFC.Count; loop2++)
                {
                    if (nextShip.ShipBFC[loop2].getTarget().targetType == StarSystemEntityType.Missile && nextShip.ShipBFC[loop2].getTarget().missileGroup == OGRemove)
                    {
                        nextShip.ShipBFC[loop2].clearTarget();
                        nextShip.ShipBFC[loop2].openFire = false;
                        nextShip.ShipsFaction.OpenFireFC.Remove(nextShip.ShipBFC[loop2]);
                        nextShip.ShipsFaction.OpenFireFCType.Remove(nextShip.ShipBFC[loop2]);
                    }
                }

                for (int loop2 = 0; loop2 < nextShip.ShipMFC.Count; loop2++)
                {
                    if (nextShip.ShipMFC[loop2].getTarget().targetType == StarSystemEntityType.Missile)
                    {
                        if (nextShip.ShipMFC[loop2].getTarget().missileGroup == OGRemove)
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
            OGRemove.contact.ContactElementCreated = SystemContact.CEState.Delete;
            Owner.MissileGroups.Remove(OGRemove);
        }
        #endregion

        public SimEntity()
        {
            SimCreated = false;
        }

        public SimEntity(int factCount, int factStart)
        {
            SimCreated = true;
            factionStart = factStart;
            factionCount = factCount;
            TGStart = 0;
            TGCount = 0;
        }
    }
}
