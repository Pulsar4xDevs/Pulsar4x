using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

namespace Pulsar4X.Entities
{
    public enum OrderType
    {
        /// <summary>
        /// General Ship Orders:
        /// </summary>
        MoveTo,
        ExtendedOrbit,
        Picket,
        Recrew,
        Refuel,
        Resupply,
        SendMessage,
        EqualizeFuel,
        EqualizeMSP,
        ActivateTransponder,
        DeactivateTransponder,

        /// <summary>
        /// Any Taskgroup of more than one vessel.
        /// </summary>
        DivideFleetToSingleShips,

        /// <summary>
        /// Any taskgroup that has sub task groups created from it, such as by a divide order.
        /// </summary>
        IncorporateSubfleet,

        /// <summary>
        /// Military Ship Specific orders:
        /// </summary>
        BeginOverhaul,


        /// <summary>
        /// Targeted on taskforce specific orders:
        /// </summary>
        Follow,
        Join,
        Absorb,

        /// <summary>
        /// JumpPoint Capable orders only:
        /// </summary>
        StandardTransit,
        SquadronTransit,
        TransitAndDivide,

        /// <summary>
        /// Cargo Hold specific orders when targeted on population/planet:
        /// </summary>
        LoadInstallation,
        LoadShipComponent,
        UnloadInstallation,
        UnloadShipComponent,
        UnloadAll,
        LoadAllMinerals,
        UnloadAllMinerals,
        LoadMineral,
        LoadMineralWhenX,
        UnloadMineral,
        LoadOrUnloadMineralsToReserve,

        /// <summary>
        /// Colony ship specific orders:
        /// </summary>
        LoadColonists,
        UnloadColonists,

        /// <summary>
        /// GeoSurvey specific orders:
        /// </summary>
        GeoSurvey,
        DetachNonGeoSurvey,

        /// <summary>
        /// Grav survey specific orders:
        /// </summary>
        GravSurvey,
        DetachNonGravSurvey,

        /// <summary>
        /// Jump Gate Construction Module specific orders:
        /// </summary>
        BuildJumpGate,

        /// <summary>
        /// Tanker Specific:
        /// </summary>
        RefuelTargetFleet,
        DetachTankers,
        
        /// <summary>
        /// Supply Ship specific:
        /// </summary>
        ResupplyTargetFleet,
        DetachSupplyShips,

        /// <summary>
        /// Collier Specific
        /// </summary>
        ReloadTargetFleet,
        DetachColliers,

        /// <summary>
        /// Any taskgroup, but the target must be a TG with the appropriate ship to fulfill this order.
        /// </summary>
        RefuelFromTargetFleet,
        ResupplyFromTargetFleet,
        ReloadFromTargetFleet,

        /// <summary>
        /// Any taskgroup, but target must have hangar bays, perhaps check to see if capacity is available.
        /// </summary>
        LandOnAssignedMothership,
        LandOnMotherShipNoAssign,
        LandOnMothershipAssign,


        /// <summary>
        /// Number of orders available.
        /// </summary>
        TypeCount
    }

    /// <summary>
    /// What state is the taskgroup in regarding accepting additional orders?
    /// </summary>
    public enum OrderState
    {
        AcceptOrders,
        DisallowOrdersPDC,
        DisallowOrdersSB,
        DisallowOrdersUnknownJump,
        TypeCount
    }

    public class TaskGroupTN : StarSystemEntity
    {
        /// <summary>
        /// Unused at the moment.
        /// </summary>
        public TaskForce TaskForce { get; set; }
        
        /// <summary>
        /// Faction this taskgroup will be a member of.
        /// </summary>
        public Faction Faction { get; set; }

        /// <summary>
        /// This TaskGroup's System Contact, which stores location about where the contact is.
        /// </summary>
        public SystemContact Contact { get; set; }

        /// <summary>
        /// Are we orbiting a body?
        /// </summary>
        public bool IsOrbiting { get; set; }

        /// <summary>
        /// If the taskgroup is orbiting a body it will be this one. and position must be derived from here.
        /// </summary>
        public StarSystemEntity OrbitingBody { get; set; }

        /// <summary>
        /// Taskgroup speed. All speeds are in KM/S
        /// </summary>
        public int CurrentSpeed { get; set; }

        /// <summary>
        /// Maximum possible taskgroup speed.
        /// </summary>
        public int MaxSpeed { get; set; }

        /// <summary>
        /// What orders is this taskforce currently under?
        /// </summary>
        public BindingList<OrderType> Orders { get; set; }

        /// <summary>
        /// What entity are those orders pointed at?
        /// </summary>
        public BindingList<StarSystemEntity> OrderTarget { get; set; }

        /// <summary>
        /// Which system do these orders take place in?
        /// </summary>
        public BindingList<StarSystem> OrderSystem { get; set; }

        /// <summary>
        /// What is the state of this taskgroup's ability to accept orders?
        /// </summary>
        public OrderState CanOrder { get; set; }

        /// <summary>
        /// Is this a set of new orders that various housekeeping needs to be done for?
        /// </summary>
        public bool NewOrders { get; set; }

        /// <summary>
        /// Total Distance ship will travel under current orders in AUs.
        /// </summary>
        public double TotalOrderDistance { get; set; }

        /// <summary>
        /// Speed along the X axis. 
        /// </summary>
        public double CurrentSpeedX { get; set; }

        /// <summary>
        /// Speed along the Y axis.
        /// </summary>
        public double CurrentSpeedY { get; set; }

        /// <summary>
        /// Time to Complete current order.
        /// </summary>
        public uint TimeRequirement { get; set; }


        /// <summary>
        /// Direction of Travel.
        /// </summary>
        public double CurrentHeading { get; set; }

        /// <summary>
        /// List of the ships in this taskgroup.
        /// </summary>
        public BindingList<ShipTN> Ships { get; set; }
        public bool ShipOutOfFuel { get; set; }

        /// <summary>
        /// List of all active sensors in this taskgroup, and the number of each.
        /// </summary>
        public BindingList<ActiveSensorTN> ActiveSensorQue;
        public BindingList<int> ActiveSensorCount;

        /// <summary>
        /// The best thermal sensor, and the number present in the fleet and undamaged.
        /// </summary>
        public PassiveSensorTN BestThermal;
        public int BestThermalCount;

        /// <summary>
        /// The best EM sensor, and the number present in the fleet and undamaged.
        /// </summary>
        public PassiveSensorTN BestEM;
        public int BestEMCount;

        /// <summary>
        /// Each sensor stores its own lookup characteristics for at what range a particular signature is detected, but the purpose of the taskgroup 
        /// lookup tables will be to store which sensor in the taskgroup active que is best at detecting a ship with the specified TCS.
        /// </summary>
        public BindingList<int> TaskGroupLookUpST;
        public BindingList<int> TaskGroupLookUpMT;

        /// <summary>
        /// Each of these linked lists stores the index of the ships in the taskgroup, in the order from least to greatest of each respective signature.
        /// </summary>
        public LinkedList<int> ThermalSortList;
        public LinkedList<int> EMSortList;
        public LinkedList<int> ActiveSortList;


        public override double Mass
        {
            get { return 0.0; }
            set { value = 0.0; }
        }

        /// <summary>
        /// Constructor for the taskgroup, sets name, faction, planet the TG starts in orbit of.
        /// </summary>
        /// <param name="Title">Name</param>
        /// <param name="FID">Faction</param>
        /// <param name="StartingBody">body taskgroup will orbit at creation.</param>
        public TaskGroupTN(string Title, Faction FID, StarSystemEntity StartingBody, StarSystem StartingSystem)
        {
            Name = Title;

            Faction = FID;

            IsOrbiting = true;
            OrbitingBody = StartingBody;

            SSEntity = StarSystemEntityType.TaskGroup;

            Contact = new SystemContact(Faction,this);

            Contact.XSystem = OrbitingBody.XSystem;
            Contact.YSystem = OrbitingBody.YSystem;
            Contact.ZSystem = OrbitingBody.ZSystem;
            Contact.SystemKmX = (long)(OrbitingBody.XSystem * Constants.Units.KM_PER_AU);
            Contact.SystemKmY = (long)(OrbitingBody.YSystem * Constants.Units.KM_PER_AU);
            Contact.CurrentSystem = StartingSystem;
            StartingSystem.AddContact(Contact);
            
            m_dMass = 0.0;



            CurrentSpeed = 0;
            MaxSpeed = 0;

            CurrentSpeedX = 0.0;
            CurrentSpeedY = 0.0;
            CurrentHeading = 0.0;
            TimeRequirement = 0;

            Orders = new BindingList<OrderType>();
            OrderTarget = new BindingList<StarSystemEntity>();
            OrderSystem = new BindingList<StarSystem>();
            TotalOrderDistance = 0.0;

            /// <summary>
            /// Change this for PDCS and starbases.
            /// </summary>
            CanOrder = OrderState.AcceptOrders;

            /// <summary>
            /// Ships start in the unordered state, so new orders will have to have GetHeading/speed/other functionality performed.
            /// </summary>
            NewOrders = true;

            Ships = new BindingList<ShipTN>();
            ShipOutOfFuel = false;

            ActiveSensorQue = new BindingList<ActiveSensorTN>();
            ActiveSensorCount = new BindingList<int>();

            TaskGroupLookUpST = new BindingList<int>();

            /// <summary>
            /// Resolution Max needs to go into global constants, or ship constants I think.
            /// </summary>
            for (int loop = 0; loop < Constants.ShipTN.ResolutionMax; loop++)
            {
                /// <summary>
                /// TaskGroupLookUpST will be initialized to zero.
                /// </summary>
                TaskGroupLookUpST.Add(0);
            }

            TaskGroupLookUpMT = new BindingList<int>();
            for (int loop = 0; loop < 15; loop++)
            {
                /// <summary>
                /// TaskGroupLookUpMT will be initialized to zero.
                /// </summary>
                TaskGroupLookUpMT.Add(0);
            }

            BestThermalCount = 0;
            BestEMCount = 0;

            ThermalSortList = new LinkedList<int>();
            EMSortList = new LinkedList<int>();
            ActiveSortList = new LinkedList<int>();

        }

        /// <summary>
        /// Adds a ship to a taskgroup, will call sorting and sensor handling.
        /// </summary>
        /// <param name="shipDef">definition of the ship to be added.</param>
        public void AddShip(ShipClassTN shipDef, int CurrentTimeSlice)
        {
            ShipTN ship = new ShipTN(shipDef,Ships.Count, CurrentTimeSlice);
            Ships.Add(ship);

            /// <summary>
            /// inform the ship of the taskgroup it belongs to.
            /// </summary>
            ship.ShipsTaskGroup = this;

            if (Ships.Count == 1)
            {
                MaxSpeed = ship.ShipClass.MaxSpeed;
                CurrentSpeed = MaxSpeed;
            }
            else
            {
                if (ship.ShipClass.MaxSpeed < MaxSpeed)
                {
                    MaxSpeed = ship.ShipClass.MaxSpeed;
                    CurrentSpeed = MaxSpeed;
                }
            }

            for (int loop = 0; loop < Ships.Count; loop++)
            {
                Ships[loop].SetSpeed(CurrentSpeed);
            }

            UpdatePassiveSensors(ship);

            AddShipToSort(ship);
        }

        /// <summary>
        /// UpdatePassiveSensors alters the best Thermal/EM detection rating if the newly added ship has a better detector than previously available.
        /// </summary>
        /// <param name="ship">ship to search for sensors.</param>
        public void UpdatePassiveSensors(ShipTN ship)
        {
            /// <summary>
            /// Loop through every passive sensor on the ship, and if this sensor is better than the best it is the new best.
            /// if it is equal to the best, then increment the number of the best sensor available.
            /// </summary>
            for (int loop = 0; loop < ship.ShipPSensor.Count; loop++)
            {
                if (ship.ShipPSensor[loop].pSensorDef.thermalOrEM == PassiveSensorType.Thermal)
                {
                    if ( (BestThermalCount == 0) || (ship.ShipPSensor[loop].pSensorDef.rating > BestThermal.pSensorDef.rating) )
                    {
                        BestThermal = ship.ShipPSensor[loop];
                        BestThermalCount = 1;
                    }
                    else if (ship.ShipPSensor[loop].pSensorDef.rating == BestThermal.pSensorDef.rating)
                    {
                        BestThermalCount++;
                    }
                }
                else if (ship.ShipPSensor[loop].pSensorDef.thermalOrEM == PassiveSensorType.EM)
                {
                    if ((BestEMCount == 0) || (ship.ShipPSensor[loop].pSensorDef.rating > BestEM.pSensorDef.rating))
                    {
                        BestEM = ship.ShipPSensor[loop];
                        BestEMCount = 1;
                    }
                    else if (ship.ShipPSensor[loop].pSensorDef.rating == BestEM.pSensorDef.rating)
                    {
                        BestEMCount++;
                    }
                }
            }
        }
        /// <summary>
        /// End UpdatePassiveSensors
        /// </summary>

        /// <summary>
        /// Helper private function that handles adding a new node to a linked list.
        /// </summary>
        /// <param name="SortList">LinkedList to add the node to.</param>
        /// <param name="Sort">LinkedListNode to be added.</param>
        /// <param name="TEA">Thermal,EM,Active.</param>
        private void AddNodeToSort(LinkedList<int> SortList, LinkedListNode<int> Sort, int TEA)
        {
            if (SortList.Count == 0)
            {
                SortList.AddFirst(Sort);
            }
            else
            {
                int value = -1, Last = -1, First = -1, NewValue = -1;
                switch (TEA)
                {
                    case 0: value = Ships[Sort.Value].CurrentThermalSignature;
                        Last = Ships[SortList.Last()].CurrentThermalSignature;
                        First = Ships[SortList.First()].CurrentThermalSignature;
                        break;
                    case 1: value = Ships[Sort.Value].CurrentEMSignature;
                        Last = Ships[SortList.Last()].CurrentEMSignature;
                        First = Ships[SortList.First()].CurrentEMSignature;
                        break;
                    case 2: value = Ships[Sort.Value].TotalCrossSection;
                        Last = Ships[SortList.Last()].TotalCrossSection;
                        First = Ships[SortList.First()].TotalCrossSection;
                        break;
                }

                if (value > Last)
                {
                    SortList.AddLast(Sort);
                }
                else if (value <= First)
                {
                    SortList.AddFirst(Sort);
                }
                else
                {
                    LinkedListNode<int> NextNode = SortList.First;

                    bool done = false;
                    while (done == false)
                    {
                        NextNode = NextNode.Next;

                        switch (TEA)
                        {
                            case 0: NewValue = Ships[NextNode.Next.Value].CurrentThermalSignature;
                            break;
                            case 1: NewValue = Ships[NextNode.Next.Value].CurrentEMSignature;
                            break;
                            case 2: NewValue = Ships[NextNode.Next.Value].TotalCrossSection;
                            break;
                        }

                        if (value >= NewValue)
                        {
                            SortList.AddAfter(NextNode, Sort);
                            done = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the specified ship to each of the sorting lists.
        /// </summary>
        /// <param name="Ship">Ship to be added.</param>
        public void AddShipToSort(ShipTN Ship)
        {
            AddNodeToSort(ThermalSortList, Ship.ThermalList,0);
            AddNodeToSort(EMSortList, Ship.EMList,1);
            AddNodeToSort(ActiveSortList, Ship.ActiveList,2);
        }



        /// <summary>
        /// GetPositionFromOrbit returns systemKm from SystemAU. If a ship is orbiting a body it will move with that body.
        /// </summary>
        public void GetPositionFromOrbit()
        {
            Contact.XSystem = OrbitingBody.XSystem;
            Contact.YSystem = OrbitingBody.YSystem;
            Contact.SystemKmX = (long)(OrbitingBody.XSystem * Constants.Units.KM_PER_AU);
            Contact.SystemKmY = (long)(OrbitingBody.YSystem * Constants.Units.KM_PER_AU);
        }


        /// <summary>
        /// SetActiveSensor activates a sensor on a ship in the taskforce, modifies the active que, and resorts EM if appropriate.
        /// </summary>
        /// <param name="ShipIndex">Ship the sensor is on.</param>
        /// <param name="ShipSensorIndex">Which sensor we want to switch.</param>
        /// <param name="state">State the sensor will be set to.</param>
        public void SetActiveSensor(int ShipIndex, int ShipSensorIndex, bool state)
        {
            int oldEMSignature = Ships[ShipIndex].CurrentEMSignature;
            Ships[ShipIndex].SetSensor(Ships[ShipIndex].ShipASensor[ShipSensorIndex], state);

            /// <summary>
            /// Sensor is active.
            /// </summary>
            if (state == true)
            {
                /// <summary>
                /// First I want to determine whether or not this sensor is in the active que already
                /// </summary>
                int inQue = -1;

                for (int loop = 0; loop < ActiveSensorQue.Count; loop++)
                {
                    if (Ships[ShipIndex].ShipASensor[ShipSensorIndex].aSensorDef.resolution == ActiveSensorQue[loop].aSensorDef.resolution)
                    {
                        if (Ships[ShipIndex].ShipASensor[ShipSensorIndex].aSensorDef.maxRange == ActiveSensorQue[loop].aSensorDef.maxRange)
                        {
                            inQue = loop;
                            break;
                        }
                    }
                }

                /// <summary>
                /// if this sensor is not in the active sensor que.
                /// </summary>
                if (inQue == -1)
                {
                    ActiveSensorQue.Add(Ships[ShipIndex].ShipASensor[ShipSensorIndex]);
                    ActiveSensorCount.Add(1);

                    /// <summary>
                    /// this is not the first sensor added to the que.
                    /// </summary>
                    if (ActiveSensorQue.Count != 1)
                    {
                        /// <summary>
                        /// Update the taskgroup lookup ship table if this sensor is better than the previous ones for any particular resolution.
                        /// </summary>
                        for (int loop = 0; loop < Constants.ShipTN.ResolutionMax; loop++)
                        {
                            if (ActiveSensorQue[(ActiveSensorQue.Count - 1)].aSensorDef.lookUpST[loop] > ActiveSensorQue[TaskGroupLookUpST[loop]].aSensorDef.lookUpST[loop])
                            {
                                TaskGroupLookUpST[loop] = (ActiveSensorQue.Count - 1);
                            }
                        }

                        /// <summary>
                        /// Update the taskgroup lookup missile table if this sensor is better than the previous ones for any particular resolution.
                        /// </summary>
                        for (int loop = 0; loop < 15; loop++)
                        {
                            if (ActiveSensorQue[(ActiveSensorQue.Count - 1)].aSensorDef.lookUpMT[loop] > ActiveSensorQue[TaskGroupLookUpMT[loop]].aSensorDef.lookUpMT[loop])
                            {
                                TaskGroupLookUpMT[loop] = (ActiveSensorQue.Count - 1);
                            }
                        }
                    }
                }
                else
                {
                    ActiveSensorCount[inQue]++;
                }
            }

            /// <summary>
            /// Sensor is inactive.
            /// <summary>
            else if (state == false)
            {
                int inQue = -1;

                for (int loop = 0; loop < ActiveSensorQue.Count; loop++)
                {
                    if (Ships[ShipIndex].ShipASensor[ShipSensorIndex].aSensorDef.resolution == ActiveSensorQue[loop].aSensorDef.resolution)
                    {
                        if (Ships[ShipIndex].ShipASensor[ShipSensorIndex].aSensorDef.maxRange == ActiveSensorQue[loop].aSensorDef.maxRange)
                        {
                            inQue = loop;
                            break;
                        }
                    }
                }

                /// <summary>
                /// The sensor is present hopefully. that would be bad if we sent an off command to a non-existent sensor.
                /// </summary>
                if (inQue != -1)
                {
                    ActiveSensorCount[inQue]--;

                    /// <summary>
                    /// Now it starts, this sensor has to be removed from the sensor que.
                    /// </summary>
                    if (ActiveSensorCount[inQue] == 0)
                    {
                        /// <summary>
                        /// Reset the lookup tables to 0.
                        /// </summary>
                        if (ActiveSensorQue.Count == 2)
                        {
                            for (int loop = 0; loop < Constants.ShipTN.ResolutionMax; loop++)
                                TaskGroupLookUpST[loop] = 0;

                            for (int loop = 0; loop < 15; loop++)
                                TaskGroupLookUpMT[loop] = 0;
                        }
                        /// <summary>
                        /// Search through the lookuptables to replace instances of inQue and Count+1
                        /// </summary>
                        else if(ActiveSensorQue.Count != 1)
                        {
                            /// <summary>
                            /// Reassign the ship table
                            /// </summary>
                            for (int loop = 0; loop < Constants.ShipTN.ResolutionMax; loop++)
                            {
                                if (TaskGroupLookUpST[loop] == inQue)
                                {
                                    /// <summary>
                                    /// Set relevant part of the lookup table to 1st active sensor.
                                    /// </summary>
                                    TaskGroupLookUpST[loop] = 0;

                                    /// <summary>
                                    /// loop through the rest of the sensor que, replace any instances of inQue with the best remaining sensor.
                                    /// </summary>
                                    for (int loop2 = 1; loop2 < ActiveSensorQue.Count; loop2++)
                                    {
                                        if (ActiveSensorQue[loop2].aSensorDef.lookUpST[loop] > ActiveSensorQue[TaskGroupLookUpST[loop]].aSensorDef.lookUpST[loop] && loop2 != inQue)
                                        {
                                            TaskGroupLookUpST[loop] = loop2;
                                        }
                                    }
                                }
                                else if (TaskGroupLookUpST[loop] == (ActiveSensorQue.Count - 1))
                                {
                                    TaskGroupLookUpST[loop] = inQue;
                                }
                            }

                            /// <summary>
                            /// Missile Table reassignment.
                            /// </summary>
                            for (int loop = 0; loop < 15; loop++)
                            {
                                if (TaskGroupLookUpMT[loop] == inQue)
                                {
                                    /// <summary>
                                    /// Set relevant part of the lookup table to 1st active sensor.
                                    /// </summary>
                                    TaskGroupLookUpMT[loop] = 0;

                                    /// <summary>
                                    /// loop through the rest of the sensor que, replace any instances of inQue with the best remaining sensor.
                                    /// </summary>
                                    for (int loop2 = 1; loop2 < ActiveSensorQue.Count; loop2++)
                                    {
                                        if (ActiveSensorQue[loop2].aSensorDef.lookUpMT[loop] > ActiveSensorQue[TaskGroupLookUpMT[loop]].aSensorDef.lookUpMT[loop] && loop2 != inQue)
                                        {
                                            TaskGroupLookUpMT[loop] = loop2;
                                        }
                                    }
                                }
                                else if (TaskGroupLookUpMT[loop] == (ActiveSensorQue.Count - 1))
                                {
                                    TaskGroupLookUpMT[loop] = inQue;
                                }
                            }
                        }

                        /// <summary>
                        /// Replace inQue with the last item entry, and remove the last entry. I could have removed the entry in place, but then I couldn't just copy my code.
                        /// </summary>
                        ActiveSensorQue[inQue] = ActiveSensorQue[(ActiveSensorQue.Count - 1)];
                        ActiveSensorCount[inQue] = ActiveSensorCount[(ActiveSensorCount.Count - 1)];

                        ActiveSensorQue.RemoveAt((ActiveSensorQue.Count-1));
                        ActiveSensorCount.RemoveAt((ActiveSensorCount.Count - 1));
                    }

                }
                /// <summary>
                /// End if inQue != -1
                /// </summary>
            }
            /// <summary>
            /// End if State == false | sensor is inactive
            /// </summary>

            if (Ships[ShipIndex].CurrentEMSignature != oldEMSignature)
            {
                SortShipBySignature(Ships[ShipIndex].EMList,EMSortList);
            }
        }
        /// <summary>
        /// End SetActiveSensor
        /// </summary>

        /// <summary>
        /// Sort ship by signature takes a ship's linkedList node and puts it in the appropriate place in SortList.
        /// </summary>
        /// <param name="ShipSignatureNode">The node for the ship.</param>
        /// <param name="SortList">The overall taskgroup list.</param>
        public void SortShipBySignature(LinkedListNode<int> ShipSignatureNode, LinkedList<int> SortList)
        {
            bool sorted = false;

            /// <summary>
            /// First check if new sorting needs to be done.
            /// </summary>
            if (ShipSignatureNode == SortList.First && SortList.First.Next != null)
            {
                if (ShipSignatureNode.Value < SortList.First.Next.Value)
                    sorted = true;
            }
            else if (ShipSignatureNode == SortList.Last && SortList.Last.Previous != null)
            {
                if (ShipSignatureNode.Value > SortList.Last.Previous.Value)
                    sorted = true;
            }

            /// <summary>
            /// The list needs to be resorted.
            /// </summary>
            if (sorted == false)
            {
                if (ShipSignatureNode.Value <= SortList.First.Value && ShipSignatureNode != SortList.First)
                {
                    SortList.Remove(ShipSignatureNode);
                    SortList.AddBefore(SortList.First, ShipSignatureNode);
                }
                else if (ShipSignatureNode.Value >= SortList.Last.Value && ShipSignatureNode != SortList.Last)
                {
                    SortList.Remove(ShipSignatureNode);
                    SortList.AddAfter(SortList.Last, ShipSignatureNode);
                }
                else
                {
                    bool done = false;
                    LinkedListNode<int> Temp = SortList.First;
                    if (SortList.First == SortList.Last)
                        done = true;

                    

                    while (done == false)
                    {
                        Temp = Temp.Next;
                        if (ShipSignatureNode.Value >= Temp.Value && ShipSignatureNode != Temp)
                        {
                            SortList.Remove(ShipSignatureNode);
                            SortList.AddAfter(Temp, ShipSignatureNode);
                            done = true;
                        }
                        /// <summary>
                        /// Hopefully this error condition won't come up.
                        /// </summary>
                        if (Temp == SortList.Last)
                        {
                            done = true;
                        }
                    }
                }
            }
            /// <summary>
            /// End if !sorted
            /// </summary>
        }
        /// <summary>
        /// End SortShipBySignature
        /// </summary>


        /// <summary>
        /// Places an order into the que of orders.
        /// </summary>
        /// <param name="Order">Order is the order to be carried out.</param>
        /// <param name="Destination">Destination of the waypoint/planet/TaskGroup we are moving towards.</param>
        public void IssueOrder(OrderType Order, StarSystemEntity Destination)
        {
            Orders.Add(Order);
            OrderTarget.Add(Destination);

            int OrderCount = Orders.Count - 1;
            double dX=0.0,dY=0.0, dZ;

            if (Orders[OrderCount] == OrderType.StandardTransit || Orders[OrderCount] == OrderType.SquadronTransit || Orders[OrderCount] == OrderType.TransitAndDivide)
            {
                for (int loop = 0; loop < OrderSystem[OrderCount].JumpPoints.Count; loop++)
                {
                    if (OrderSystem[OrderCount].JumpPoints[loop].XSystem == Destination.XSystem && OrderSystem[OrderCount].JumpPoints[loop].YSystem == Destination.YSystem)
                    {
                        if (OrderSystem[OrderCount].JumpPoints[loop].IsExplored == false)
                            CanOrder = OrderState.DisallowOrdersUnknownJump;

                        break;
                    }
                }
            }

            if (OrderCount == 0)
            {
                if (IsOrbiting)
                    GetPositionFromOrbit();

                dX = Math.Abs(OrderTarget[OrderCount].XSystem - Contact.XSystem);
                dY = Math.Abs(OrderTarget[OrderCount].YSystem - Contact.YSystem);

                OrderSystem.Add(Contact.CurrentSystem);

            }
            else if (Orders[OrderCount-1] == OrderType.StandardTransit || Orders[OrderCount-1] == OrderType.SquadronTransit || Orders[OrderCount-1] == OrderType.TransitAndDivide)
            {
                for (int loop = 0; loop < OrderSystem[OrderCount-1].JumpPoints.Count; loop++)
                {
                    if (OrderSystem[OrderCount - 1].JumpPoints[loop].XSystem == Destination.XSystem && OrderSystem[OrderCount - 1].JumpPoints[loop].YSystem == Destination.YSystem)
                    {
                        dX = Math.Abs(OrderTarget[OrderCount].XSystem - OrderSystem[OrderCount - 1].JumpPoints[loop].Connect.XSystem);
                        dY = Math.Abs(OrderTarget[OrderCount].YSystem - OrderSystem[OrderCount - 1].JumpPoints[loop].Connect.YSystem);
                        OrderSystem.Add(OrderSystem[OrderCount - 1].JumpPoints[loop].Connect.System);
                        break;
                    }
                }
            }
            else
            {
                dX = Math.Abs(OrderTarget[OrderCount].XSystem - OrderTarget[OrderCount - 1].XSystem);
                dY = Math.Abs(OrderTarget[OrderCount].YSystem - OrderTarget[OrderCount - 1].YSystem);

                OrderSystem.Add(OrderSystem[OrderCount-1]);
            }
            dZ = Math.Sqrt(((dX * dX) + (dY * dY)));
            TotalOrderDistance = TotalOrderDistance + dZ;
        }

        /// <summary>
        /// GetHeading determines the direction the ship should face to get to its current ordered target.
        /// </summary>
        public void GetHeading()
        {
            if (IsOrbiting)
            {
                GetPositionFromOrbit();
                IsOrbiting = false;
            }

            double dX = Contact.SystemKmX - (OrderTarget[0].XSystem * Constants.Units.KM_PER_AU);
            double dY = Contact.SystemKmY - (OrderTarget[0].YSystem * Constants.Units.KM_PER_AU);

            CurrentHeading = (Math.Atan((dY / dX)) / Constants.Units.RADIAN);
        }

        /// <summary>
        /// GetSpeed gets the current X and Y velocities required by the current heading.
        /// </summary>
        public void GetSpeed()
        {
            double dX = Contact.SystemKmX - (OrderTarget[0].XSystem * Constants.Units.KM_PER_AU);
            double dY = Contact.SystemKmY - (OrderTarget[0].YSystem * Constants.Units.KM_PER_AU);

            double sign = 1.0;
            if (dX > 0.0)
            {
                sign = -1.0;
            }

            /// <summary>
            /// minor matrix multiplication here.
            /// </summary>
            CurrentSpeedX = CurrentSpeed * Math.Cos(CurrentHeading * Constants.Units.RADIAN) * sign;
            CurrentSpeedY = CurrentSpeed * Math.Sin(CurrentHeading * Constants.Units.RADIAN) * sign;
        }

        /// <summary>
        /// How long will this order take given the TaskGroup's current speed?
        /// </summary>
        public void GetTimeRequirement()
        {
            double dX = Math.Abs((OrderTarget[0].XSystem * Constants.Units.KM_PER_AU) - Contact.SystemKmX);
            double dY = Math.Abs((OrderTarget[0].XSystem * Constants.Units.KM_PER_AU) - Contact.SystemKmY);
            double dZ = Math.Sqrt(((dX * dX) + (dY * dY)));

            TimeRequirement = (uint)Math.Ceiling((dZ / (double)CurrentSpeed));
        }


        /// <summary>
        /// TaskGroup order handling function 
        /// </summary>
        /// <param name="TimeSlice">How much time the taskgroup is alloted to perform its orders. unit is in seconds</param>
        public void FollowOrders(uint TimeSlice)
        {
            if (NewOrders == true)
            {
                GetHeading();
                GetSpeed();

                GetTimeRequirement();
                NewOrders = false;
            }

            if (TimeRequirement < TimeSlice)
            {
                Contact.UpdateLocationInSystem(OrderTarget[0].XSystem, OrderTarget[0].YSystem);

                if (OrderTarget[0].SSEntity == StarSystemEntityType.Body)
                    IsOrbiting = true;

                OrderTarget.RemoveAt(0);
                Orders.RemoveAt(0);


                UseFuel(TimeRequirement);

                /// <summary>
                /// move on to next order if possible
                /// </summary>
                TimeSlice = TimeSlice - TimeRequirement;
                if (TimeSlice > 0 && Orders.Count > 0)
                {
                    NewOrders = true;
                    FollowOrders(TimeSlice);
                }
            }
            else
            {
                Contact.SystemKmX = Contact.SystemKmX + (float)((double)TimeSlice * CurrentSpeedX);
                Contact.SystemKmY = Contact.SystemKmY + (float)((double)TimeSlice * CurrentSpeedY);

                Contact.XSystem = Contact.SystemKmX / Constants.Units.KM_PER_AU;
                Contact.YSystem = Contact.SystemKmY / Constants.Units.KM_PER_AU;

                UseFuel(TimeSlice);

                TimeRequirement = TimeRequirement - TimeSlice;
            }

        }
        /// <summary>
        /// End FollowOrders()
        /// </summary>

        /// <summary>
        /// UseFuel decrements ship fuel storage over time TimeSlice.
        /// </summary>
        /// <param name="TimeSlice">TimeSlice to use fuel over.</param>
        public void UseFuel(uint TimeSlice)
        {
            for (int loop = 0; loop < Ships.Count; loop++)
            {
                Ships[loop].FuelCounter = Ships[loop].FuelCounter + (int)TimeSlice;

                int hours = (int)Math.Floor((float)Ships[loop].FuelCounter / 3600.0f);

                if (hours >= 1)
                {
                    Ships[loop].FuelCounter = Ships[loop].FuelCounter - (3600*hours);
                    Ships[loop].CurrentFuel = Ships[loop].CurrentFuel - (Ships[loop].CurrentFuelUsePerHour*hours);

                    /// <summary>
                    /// Ships have a grace period depending on TimeSlice choices, say they were running on fumes.
                    /// or it can be done absolutely accurately if required.
                    /// </summary>
                    if (Ships[loop].CurrentFuel < 0.0f)
                    {
                        Ships[loop].CurrentFuel = 0.0f;
                        ShipOutOfFuel = true;
                    }
                }
            }
        }
    }
    /// <summary>
    /// End TaskGroupTN
    /// </summary>
}
