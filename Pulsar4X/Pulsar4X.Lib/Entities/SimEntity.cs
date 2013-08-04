using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

namespace Pulsar4X.Entities
{
    public class SimEntity
    {
        public int factionCount { get; set; }
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
            for (int loop = 0; loop < factionCount; loop++)
            {
                Faction P1;
                if (loop == 0)
                    P1 = P[0];
                else
                    P1 = new Faction(loop);

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


                for (int loop2 = 0; loop2 < TGCount; loop2++)
                {
                    int randx = RNG.Next(0, 100000);
                    int randy = RNG.Next(0, 100000);

                    float wx = ((float)randx / 50000.0f) - 1.0f;
                    float wy = ((float)randy / 50000.0f) - 1.0f;

                    Waypoint Start = new Waypoint(Sol, wx, wy);

                    string ID1 = loop.ToString();

                    string TGName = "P" + ID1 + "TG" + loop2.ToString();

                    P1.AddNewTaskGroup(TGName, Start, Sol);

                    for (int loop3 = 0; loop3 < ShipCount; loop3++)
                    {
                        P1.TaskGroups[loop2].AddShip(P1.ShipDesigns[0], 0);
                        P1.TaskGroups[loop2].Ships[loop3].Refuel(200000.0f);
                        P1.TaskGroups[loop2].SetActiveSensor(loop3, 0, true);
                    }

                }
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
                for (int loop2 = 0; loop2 < TGCount; loop2++)
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
                                if (P[loop].DetectedContacts.ContainsKey(target))
                                {
                                    if (P[loop].DetectedContacts[target].active == true)
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
                            P[loop].TaskGroups[loop2].Ships[loop3].RechargeBeamWeapons(5);
                        }
                    }
                }

                /// <summary>
                /// Get ending condition here: no more targets anywhere.
                /// </summary>

                if (P[loop].TaskGroups.Count != 0)
                {
                    if (P[loop].DetectedContacts.Count == 0 && P[loop].TaskGroups[0].TaskGroupOrders.Count == 0)
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
            factionCount = 16;
            TGCount = 10;
            ShipCount = 8;

            /// <summary>
            /// Create all the factions here. later add new ships and tgs here as well.
            /// </summary>
            createFactions(P, Sol, factionCount, TGCount, ShipCount, RNG);


            /// <summary>
            /// Order every ship to proceed to the center.
            /// </summary>
            Waypoint Center = new Waypoint(Sol, 0.0, 0.0);
            MoveToCenter = new Orders(Constants.ShipTN.OrderType.MoveTo, 0, 0, 0, Center);

            initShips(P);

            CurrentTick = 0;
            lastTick = 0;
            ShipsDestroyed = 0;
            TGDestroyed = 0;
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
                for (int loop = 0; loop < factionCount; loop++)
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
                for (int loop = 0; loop < factionCount; loop++)
                {
                    for (int loop2 = 0; loop2 < P[loop].TaskGroups.Count; loop2++)
                    {
                        for (int loop3 = 0; loop3 < P[loop].TaskGroups[loop2].Ships.Count; loop3++)
                        {
                            if (P[loop].TaskGroups[loop2].Ships[loop3].IsDestroyed == true)
                            {
                                for (int loop4 = 0; loop4 < factionCount; loop4++)
                                {
                                    if (P[loop4].DetectedContacts.ContainsKey(P[loop].TaskGroups[loop2].Ships[loop3]))
                                    {
                                        P[loop4].DetectedContacts.Remove(P[loop].TaskGroups[loop2].Ships[loop3]);
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

                                P[loop].DetectedContacts.Clear();
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
    }
}
