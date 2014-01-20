using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;
using System.Drawing;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public int ShipCount { get; set; }
        public int CurrentTick { get; set; }
        public int lastTick { get; set; }
        public int ShipsDestroyed { get; set; }
        public int TGDestroyed { get; set; }
        public Orders MoveToCenter { get; set; }
        public bool SimCreated { get; set; }

        /// <summary>
        /// create factions fills in the factions, tgs, and ships. The design for ships should be modified here.
        /// </summary>
        /// <param name="P">Factions</param>
        /// <param name="Sol">Starting starsystem</param>
        /// <param name="factionCount"># of factions</param>
        /// <param name="TGCount"># of tgs</param>
        /// <param name="ShipCount"># of ships</param>
        /// <param name="RNG">"global" rng since it has to be done that way.</param>
        private void createFactions(BindingList<Faction> P, StarSystem Sol, int factionCount, int TGCount, int ShipCount, Random RNG)
        {
            for (int loop = factionStart; loop < factionCount; loop++)
            {
                Faction P1;
                if (loop == factionStart)
                {
                    P1 = P[0];
                }
                else
                {
                    String Race = "Human Federation" + loop.ToString();
                    Species NewSpecies = new Species();
                    P1 = new Faction(Race, NewSpecies, loop);
                    Waypoint Start = new Waypoint("WP Start",Sol, 0.0, 0.0,P1.FactionID);

                    Planet Pl1 = new Planet(Sol.Stars[0]);
                    Pl1.XSystem = 0.0;
                    Pl1.YSystem = 0.0;

                    P1.AddNewTaskGroup("Shipyard TG", Pl1, Sol);
                }

                P1.AddNewContactList(Sol);
                P1.AddNewShipDesign("Blucher");

                P1.ShipDesigns[0].AddEngine(P1.ComponentList.Engines[0], 3);
                P1.ShipDesigns[0].AddCrewQuarters(P1.ComponentList.CrewQuarters[0], 1);
                P1.ShipDesigns[0].AddFuelStorage(P1.ComponentList.FuelStorage[0], 1);
                P1.ShipDesigns[0].AddEngineeringSpaces(P1.ComponentList.EngineeringSpaces[0], 1);
                P1.ShipDesigns[0].AddOtherComponent(P1.ComponentList.OtherComponents[0], 1);
                P1.ShipDesigns[0].AddActiveSensor(P1.ComponentList.ActiveSensorDef[0], 1);
                P1.ShipDesigns[0].AddBeamFireControl(P1.ComponentList.BeamFireControlDef[0], 1);
                P1.ShipDesigns[0].AddBeamWeapon(P1.ComponentList.BeamWeaponDef[0], 2);
                P1.ShipDesigns[0].AddReactor(P1.ComponentList.ReactorDef[0], 2);
                P1.ShipDesigns[0].NewArmor("Duranium", 5, 4);


                for (int loop2 = 1; loop2 <= TGCount; loop2++)
                {
                    int randx = RNG.Next(0, 100000);
                    int randy = RNG.Next(0, 100000);

                    float wx = ((float)randx / 50000.0f) - 1.0f;
                    float wy = ((float)randy / 50000.0f) - 1.0f;

                    Planet Pl2 = new Planet(Sol.Stars[0]);
                    Pl2.XSystem = wx;
                    Pl2.YSystem = wy;

                    string ID1 = loop.ToString();

                    string TGName = "P" + ID1 + "TG" + loop2.ToString();

                    P1.AddNewTaskGroup(TGName, Pl2, Sol);

                    for (int loop3 = 0; loop3 < ShipCount; loop3++)
                    {
                        P1.TaskGroups[loop2].AddShip(P1.ShipDesigns[0], 0);
                        P1.TaskGroups[loop2].Ships[loop3].Name = "P" + loop.ToString() + " TG" + loop2.ToString() + " Ship" + loop3.ToString();
                        P1.TaskGroups[loop2].Ships[loop3].Refuel(200000.0f);
                        P1.TaskGroups[loop2].SetActiveSensor(loop3, 0, true);
                    }

                }

                if(loop != factionStart)
                    P.Add(P1);
            }
        }

        /// <summary>
        /// InitShips stars all taskgroups moving towards the center of the map, and links all beam weapons(currently just 1 weapon to 1 bfc)
        /// </summary>
        /// <param name="P">Faction List</param>
        /// <param name="MoveToCenter">Order to move to center.</param>
        /// <param name="factionCount"># of factions.</param>
        /// <param name="TGCount"># of tgs.</param>
        /// <param name="ShipCount"># of ships</param>
        private void initShips(BindingList<Faction> P)
        {
            for (int loop = 0; loop < factionCount; loop++)
            {
                for (int loop2 = TGStart; loop2 <= TGCount; loop2++)
                {
                    P[loop].TaskGroups[loop2].IsOrbiting = false;
                    P[loop].TaskGroups[loop2].IssueOrder(MoveToCenter);

                    /// <summary>
                    /// Weapon linking is also handled here for the time being, adding more weapons will be problematic.
                    /// </summary>
                    for (int loop3 = 0; loop3 < ShipCount; loop3++)
                    {
                        P[loop].TaskGroups[loop2].Ships[loop3].LinkWeaponToBeamFC(P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0], P[loop].TaskGroups[loop2].Ships[loop3].ShipBeam[0]);
                        P[loop].TaskGroups[loop2].Ships[loop3].LinkWeaponToBeamFC(P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0], P[loop].TaskGroups[loop2].Ships[loop3].ShipBeam[1]);
                    }
                }
            }
        }

        /// <summary>
        /// Target Acquisition assigns new targets to ships that have destroyed their current target.
        /// </summary>
        /// <param name="P">Faction list.</param>
        /// <param name="factionCount"># of factions</param>
        private void TargetAcquisition(BindingList<Faction> P, int factionCount)
        {
            for (int loop = 0; loop < factionCount; loop++)
            {
                for (int loop2 = 0; loop2 < P[loop].TaskGroups.Count; loop2++)
                {
                    for (int loop3 = 0; loop3 < P[loop].TaskGroups[loop2].Ships.Count; loop3++)
                    {
                        if (P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].target != null)
                        {
                            if (P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].target.IsDestroyed == true)
                                P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].clearTarget();
                        }

                        if (P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].target == null && P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].isDestroyed == false)
                        {
                            ShipTN newTarget = P[loop].TaskGroups[loop2].getNewTarget();

                            if (newTarget != null)
                            {
                                P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].assignTarget(newTarget);
                                P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].openFire = true;

                                if (P[loop].TaskGroups[loop2].TaskGroupOrders.Count != 0)
                                {
                                    if (P[loop].TaskGroups[loop2].TaskGroupOrders[0] == MoveToCenter &&
                                        (newTarget.ShipsTaskGroup.Contact.XSystem != 0.0 || newTarget.ShipsTaskGroup.Contact.YSystem != 0.0))
                                    {
                                        /// <summary>
                                        /// Issue a new order to move to this target.
                                        /// </summary>
                                        Orders MoveToTarget = new Orders(Constants.ShipTN.OrderType.MoveTo, 0, 0, 0, newTarget.ShipsTaskGroup);

                                        P[loop].TaskGroups[loop2].clearAllOrders();
                                        P[loop].TaskGroups[loop2].IssueOrder(MoveToTarget);
                                    }
                                    else if (newTarget.ShipsTaskGroup.Contact.XSystem != 0.0 || newTarget.ShipsTaskGroup.Contact.YSystem != 0.0)
                                    {
                                        /// <summary>
                                        /// Que a new order for the TG to persue this new target after dealing with the current one.
                                        /// </summary>
                                        Orders MoveToTarget = new Orders(Constants.ShipTN.OrderType.MoveTo, 0, 0, 0, newTarget.ShipsTaskGroup);
                                        P[loop].TaskGroups[loop2].IssueOrder(MoveToTarget);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fire Weapons fires each ship's weapons at their target, or if there is no target redirects the taskgroup to the center of the map.
        /// </summary>
        /// <param name="P">Factions</param>
        /// <param name="factionCount">Count of factions</param>
        /// <param name="MoveToCenter">Order to move to center of map</param>
        /// <param name="tick">Current sim tick</param>
        /// <param name="RNG">"Global" random.</param>
        /// <param name="done">boolean that determines if the simulation is finished.</param>
        /// <returns></returns>
        private bool FireWeapons(BindingList<Faction> P, int factionCount, int tick, Random RNG, bool done)
        {
            for (int loop = 0; loop < factionCount; loop++)
            {
                for (int loop2 = 0; loop2 < P[loop].TaskGroups.Count; loop2++)
                {
                    for (int loop3 = 0; loop3 < P[loop].TaskGroups[loop2].Ships.Count; loop3++)
                    {
                        if (P[loop].TaskGroups[loop2].Ships[loop3].IsDestroyed == false)
                        {
                            ShipTN target = P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].getTarget();

                            if (target != null)
                            {
                                if (target.IsDestroyed == true)
                                {
                                    target = null;
                                    P[loop].TaskGroups[loop2].clearAllOrders();
                                    if (P[loop].TaskGroups[loop2].Contact.XSystem != 0.0 && P[loop].TaskGroups[loop2].Contact.YSystem != 0.0)
                                        P[loop].TaskGroups[loop2].IssueOrder(MoveToCenter);
                                }
                            }

                            if (target != null)
                            {
                                if (P[loop].DetectedContactLists.ContainsKey(target.ShipsTaskGroup.Contact.CurrentSystem))
                                {
                                    if (P[loop].DetectedContactLists[target.ShipsTaskGroup.Contact.CurrentSystem].DetectedContacts.ContainsKey(target))
                                    {
                                        if (P[loop].DetectedContactLists[target.ShipsTaskGroup.Contact.CurrentSystem].DetectedContacts[target].active == true)
                                        {
                                            if (P[loop].TaskGroups[loop2].Ships[loop3].IsDestroyed == false)
                                            {
                                                P[loop].TaskGroups[loop2].Ships[loop3].ShipFireWeapons(tick, RNG);
                                            }
                                        }
                                        else
                                        {
                                            P[loop].TaskGroups[loop2].clearAllOrders();
                                            if (P[loop].TaskGroups[loop2].Contact.XSystem != 0.0 && P[loop].TaskGroups[loop2].Contact.YSystem != 0.0)
                                                P[loop].TaskGroups[loop2].IssueOrder(MoveToCenter);
                                        }
                                    }
                                    else
                                    {
                                        P[loop].TaskGroups[loop2].clearAllOrders();
                                        if (P[loop].TaskGroups[loop2].Contact.XSystem != 0.0 && P[loop].TaskGroups[loop2].Contact.YSystem != 0.0)
                                            P[loop].TaskGroups[loop2].IssueOrder(MoveToCenter);
                                    }
                                }
                            }
                            P[loop].TaskGroups[loop2].Ships[loop3].RechargeBeamWeapons(5);
                        }
                    }
                }

                /// <summary>
                /// Get ending condition here: no more targets anywhere.
                /// </summary>

                if (P[loop].TaskGroups.Count != 0)
                {
                    if (P[loop].DetectedContactLists[P[loop].TaskGroups[0].Contact.CurrentSystem].DetectedContacts.Count == 0 && P[loop].TaskGroups[0].TaskGroupOrders.Count == 0)
                    {
                        if (loop == (factionCount - 1) && done == true)
                        {
                            done = true;
                        }
                        else if (loop != (factionCount - 1))
                        {
                            done = true;
                        }
                        else
                        {
                            done = false;
                        }

                    }
                    else
                    {
                        done = false;
                    }
                }
                else
                {
                    done = true;
                }
            }

            return done;
        }

        public void InitSim(StarSystem Sol, BindingList<Faction> P, Random RNG)
        {
            factionStart = 0;
            factionCount = 16;
            TGStart = 1;
            TGCount = 10;
            ShipCount = 8;

            /// <summary>
            /// Create all the factions here. later add new ships and tgs here as well.
            /// </summary>
            createFactions(P, Sol, factionCount, TGCount, ShipCount, RNG);


            /// <summary>
            /// Order every ship to proceed to the center. this waypoint has an owning ID of 0, but everyone shares it. in any event I don't want it displayed.
            /// </summary>
            Waypoint Center = new Waypoint("WP Center",Sol, 0.0, 0.0, 0);
            MoveToCenter = new Orders(Constants.ShipTN.OrderType.MoveTo, 0, 0, 0, Center);

            initShips(P);

            CurrentTick = 0;
            lastTick = 0;
            ShipsDestroyed = 0;
            TGDestroyed = 0;
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
            /// Follow orders here.
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
            /// Do sensor sweeps here. Sensors must be done after movement, not before.
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
                            ShipTN Target = pair.Value.ShipBFC[pair.Key.componentIndex].target;

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
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        //pair.Value.ShipMFC[pair.Key.componentIndex]
                    }
                }
            }

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

                        if (ret == PowerComp)
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
                    /// </summary>
                    if((value & (int)Faction.RechargeStatus.Destroyed) == (int)Faction.RechargeStatus.Destroyed)
                    {
                        for(int loop4 = 0; loop4 < pair.Key.TaskGroupsOrdered.Count; loop4++)
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

                                        String Entry = String.Format("Taskgroup {0} cannot find target, orders canceled.",pair.Key.TaskGroupsOrdered[loop4].Name);
                                        MessageEntry Entry2 = new MessageEntry(MessageEntry.MessageType.OrdersNotCompleted,pair.Key.TaskGroupsOrdered[loop4].Contact.CurrentSystem, pair.Key.TaskGroupsOrdered[loop4].Contact, 
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

                        for(int loop5 = 0; loop5 < pair.Key.ShipsTargetting.Count; loop5++)
                        {
                            ShipTN nextShip = pair.Key.ShipsTargetting[loop5];
                            for (int loop6 = 0; loop6 < nextShip.ShipBFC.Count; loop6++)
                            {
                                if (nextShip.ShipBFC[loop6].getTarget() == pair.Key)
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

                        P[loop].RechargeList.Remove(pair.Key);

                        /// <summary>
                        /// Have to re-run loop since a ship was removed from all kinds of things.
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

        public void RunSim(BindingList<Faction> P, Random RNG, int tickValue)
        {
            bool done = false;
            /// <summary>
            /// Advance the game tick:
            /// </summary>
            lastTick = CurrentTick;
            CurrentTick += tickValue;
                /// <summary>
                /// Do sensor loop.
                /// Follow orders.
                /// I need to be able to know what targets are available. In system? per taskgroup?
                /// Dictionary in faction of system, and binding list of contacts?
                /// Attempt to fire.
                /// If one ship is destroyed exit loop.
                /// </summary>
                
                /// <summary>
                /// 1st do the sensor sweep:
                /// </summary>
                for (int loop = factionStart; loop < factionCount; loop++)
                {
                    P[loop].SensorSweep(CurrentTick);
                }


                /// <summary>
                /// Target selection:
                /// As with follow orders more taskgroups means another loop, likewise for if different ships in each TG want different targets.
                /// What conditions cause target loss? destruction of target and disappearance of target from sensors.
                /// </summary>
                TargetAcquisition(P, factionCount);


                /// <summary>
                /// Follow orders here.
                /// </summary>
                for (int loop = 0; loop < factionCount; loop++)
                {
                    for (int loop2 = 0; loop2 < P[loop].TaskGroups.Count; loop2++)
                    {
                        /// <summary>
                        /// Adding new taskgroups means adding a loop here to run through them all.
                        /// </summary>
                        if (P[loop].TaskGroups[loop2].TaskGroupOrders.Count != 0)
                            P[loop].TaskGroups[loop2].FollowOrders((uint)(CurrentTick-lastTick));
                    }
                }

                /// <summary>
                /// attempt to fire weapons at target here.
                /// Initiative will have to be implemented here for "fairness". right now lower P numbers have the advantage.
                /// </summary>
                done = FireWeapons(P, factionCount, CurrentTick, RNG, done);                

                /// <summary>
                /// Ending print report and preliminary ship/tg destruction handler.
                /// </summary>
                for (int loop = factionStart; loop < factionCount; loop++)
                {
                    for (int loop2 = 0; loop2 < P[loop].TaskGroups.Count; loop2++)
                    {
                        for (int loop3 = 0; loop3 < P[loop].TaskGroups[loop2].Ships.Count; loop3++)
                        {
                            if (P[loop].TaskGroups[loop2].Ships[loop3].IsDestroyed == true)
                            {
                                for (int loop4 = 0; loop4 < factionCount; loop4++)
                                {
                                    StarSystem CurSystem = P[loop].TaskGroups[loop2].Contact.CurrentSystem;
                                    if (P[loop4].DetectedContactLists.ContainsKey(CurSystem))
                                    {
                                        if (P[loop4].DetectedContactLists[CurSystem].DetectedContacts.ContainsKey(P[loop].TaskGroups[loop2].Ships[loop3]))
                                        {
                                            P[loop4].DetectedContactLists[CurSystem].DetectedContacts.Remove(P[loop].TaskGroups[loop2].Ships[loop3]);
                                        }
                                    }
                                }
                                bool nodeGone = P[loop].TaskGroups[loop2].Ships[loop3].OnDestroyed();
                                P[loop].TaskGroups[loop2].Ships[loop3].ShipClass.ShipsInClass.Remove(P[loop].TaskGroups[loop2].Ships[loop3]);
                                P[loop].TaskGroups[loop2].Ships[loop3].ShipsTaskGroup.Ships.Remove(P[loop].TaskGroups[loop2].Ships[loop3]);

                                ShipsDestroyed++;

                                if (loop3 != (P[loop].TaskGroups[loop2].Ships.Count - 1))
                                    loop3--;

                                if (P[loop].TaskGroups[loop2].Ships.Count == 0)
                                {
                                    P[loop].TaskGroups[loop2].clearAllOrders();
                                    P[loop].TaskGroups[loop2].Contact.CurrentSystem.RemoveContact(P[loop].TaskGroups[loop2].Contact);
                                    P[loop].TaskGroups.Remove(P[loop].TaskGroups[loop2]);

                                    TGDestroyed++;

                                    if (loop2 != (P[loop].TaskGroups.Count - 1))
                                        loop2--;

                                    break;
                                }

                                P[loop].DetectedContactLists[P[loop].TaskGroups[loop2].Contact.CurrentSystem].DetectedContacts.Clear();
                            }
                        }
                        if (P[loop].TaskGroups.Count == 0)
                            break;
                    }
                }
            }

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
