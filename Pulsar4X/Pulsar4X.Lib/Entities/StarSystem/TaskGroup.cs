using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;
using System.ComponentModel;
using Pulsar4X;
using Pulsar4X.Helpers.GameMath;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
using Pulsar4X.Helpers.GameMath;
#endif

/// <summary>
/// Need a unified component list and component definition list for
/// CargoListEntry in CargoTN.cs
/// eventual onDamage function for Ship.cs
/// onDestroyed function for Ship.cs
/// 
/// Move component and Cargo loads to be on ship by ship basis
/// SystemContact list for faction.
/// </summary>

namespace Pulsar4X.Entities
{

    public class TaskGroupTN : StarSystemEntity
    {
#if LOG4NET_ENABLED
        /// <summary>
        /// TG Logger:
        /// </summary>
        public static readonly ILog logger = LogManager.GetLogger(typeof(TaskGroupTN));
#endif

        /// <summary>
        /// Unused at the moment.
        /// </summary>
        public TaskForce TaskForce { get; set; }

        /// <summary>
        /// Faction this taskgroup will be a member of.
        /// </summary>
        public Faction TaskGroupFaction { get; set; }

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
        public OrbitingEntity OrbitingBody { get; set; }

        /// <summary>
        /// Taskgroup speed. All speeds are in KM/S
        /// </summary>
        public int CurrentSpeed { get; set; }

        /// <summary>
        /// Maximum possible taskgroup speed.
        /// </summary>
        public int MaxSpeed { get; set; }

        /// <summary>
        /// Orders that this taskgroup is under.
        /// </summary>
        public BindingList<Order> TaskGroupOrders { get; set; }

        /// <summary>
        /// What is the state of this taskgroup's ability to accept orders?
        /// </summary>
        public Constants.ShipTN.OrderState CanOrder { get; set; }

        /// <summary>
        /// Is this a set of new orders that various housekeeping needs to be done for?
        /// </summary>
        public bool NewOrders { get; set; }

        /// <summary>
        /// Total distance ship will travel under current orders in AUs.
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
        public BindingList<ActiveSensorTN> ActiveSensorQue { get; set; }
        public BindingList<int> ActiveSensorCount { get; set; }

        /// <summary>
        /// The best thermal sensor, and the number present in the fleet and undamaged.
        /// </summary>
        public PassiveSensorTN BestThermal { get; set; }
        public int BestThermalCount { get; set; }

        /// <summary>
        /// The best EM sensor, and the number present in the fleet and undamaged.
        /// </summary>
        public PassiveSensorTN BestEM { get; set; }
        public int BestEMCount { get; set; }

        /// <summary>
        /// Each sensor stores its own lookup characteristics for at what range a particular signature is detected, but the purpose of the taskgroup 
        /// lookup tables will be to store which sensor in the taskgroup active que is best at detecting a ship with the specified TCS.
        /// </summary>
        public BindingList<int> TaskGroupLookUpST { get; set; }
        public BindingList<int> TaskGroupLookUpMT { get; set; }

        /// <summary>
        /// Each of these linked lists stores the index of the ships in the taskgroup, in the order from least to greatest of each respective signature.
        /// </summary>
        public LinkedList<int> ThermalSortList { get; set; }
        public LinkedList<int> EMSortList { get; set; }
        public LinkedList<int> ActiveSortList { get; set; }



        /// <summary>
        /// Sum total of all cargo holds in the taskgroup.
        /// </summary>
        public int TotalCargoTonnage { get; set; }

        /// <summary>
        /// Space currently occupied in the taskgroup's holds.
        /// </summary>
        public int CurrentCargoTonnage { get; set; }

        /// <summary>
        /// Sum total of all cryo bays in the taskgroup.
        /// </summary>
        public int TotalCryoCapacity { get; set; }

        /// <summary>
        /// Currently occupied cryostorage.
        /// </summary>
        public int CurrentCryoStorage { get; set; }

        /// <summary>
        /// Taskgroups with orders to this ship.
        /// </summary>
        public BindingList<TaskGroupTN> TaskGroupsOrdered { get; set; }


        /// <summary>
        /// Controls whether or not the travel line will be set. 0 means draw the line, 1 means set last position to current position. 2 means that last position has been set to current position.
        /// 3 means that the line has been updated on the system map as being not drawn, so do not display it again.
        /// This is referenced in ContactElement and maybe simentity,SystemMap as well as here.
        /// </summary>
        public byte DrawTravelLine { get; set; }

        /// <summary>
        /// Taskgroup has its own copy of what PD are active. Change this on transit, ship destruction, and taskgroup ship adding/subtracting.
        /// </summary>
        public PointDefenseList TaskGroupPDL { get; set; }

        /// <summary>
        /// This taskgroup should not be allowed to move if it is in a shipyard.
        /// </summary>
        public bool IsInShipyard { get; set; }


        /// <summary>
        /// When was the last time this taskgroup had its sensors changed? Activation, deactivation, ships joining or leaving, and sensor component destruction should be everything.
        /// This is not actually a timer, it is merely a counter that will be incremented every time a sensor change happens. there is a similar counter in the UI that should track this, and
        /// this lets the UI know when to update the sensor display.
#warning Sensor component destruction should be looked at. for now SensorUpdateAck will be handled inside TaskGroup.cs
        /// </summary>
        public uint SensorUpdateAck { get; set; }

        /// <summary>
        /// Constructor for the taskgroup, sets name, faction, planet the TG starts in orbit of.
        /// </summary>
        /// <param name="Title">Name</param>
        /// <param name="FID">Faction</param>
        /// <param name="StartingBody">body taskgroup will orbit at creation.</param>
        public TaskGroupTN(string Title, Faction FID, OrbitingEntity StartingBody, StarSystem StartingSystem)
        {
            Name = Title;
            /// <summary>
            /// create these or else anything that relies on a unique global id will break.
            /// </summary>
            Id = Guid.NewGuid();

            TaskGroupFaction = FID;

            IsOrbiting = true;
            OrbitingBody = StartingBody;
            (OrbitingBody as SystemBody).TaskGroupsInOrbit.Add(this);

            SSEntity = StarSystemEntityType.TaskGroup;

            Position.System = StartingSystem;
            Position.X = OrbitingBody.Position.X;
            Position.Y = OrbitingBody.Position.Y;

            Contact = new SystemContact(TaskGroupFaction, this);

            Contact.LastPosition.X = Contact.Position.X;
            Contact.LastPosition.Y = Contact.Position.Y;

            StartingSystem.SystemContactList.Add(Contact);
            DrawTravelLine = 3;

            CurrentSpeed = 1;
            MaxSpeed = 1;

            CurrentSpeedX = 0.0;
            CurrentSpeedY = 0.0;
            CurrentHeading = 0.0;
            TimeRequirement = 0;

            TaskGroupOrders = new BindingList<Order>();

            TotalOrderDistance = 0.0;

            /// <summary>
            /// Change this for PDCS and starbases.
            /// </summary>
            CanOrder = Constants.ShipTN.OrderState.AcceptOrders;

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

            TotalCargoTonnage = 0;
            CurrentCargoTonnage = 0;

            TotalCryoCapacity = 0;
            CurrentCryoStorage = 0;

            TaskGroupsOrdered = new BindingList<TaskGroupTN>();

            TaskGroupPDL = new PointDefenseList();

            IsInShipyard = false;

            SensorUpdateAck = 0;

            //add default legal order for targeting TGs.
            _legalOrders.Add(Constants.ShipTN.OrderType.Follow);
            _legalOrders.Add(Constants.ShipTN.OrderType.Join);
            _legalOrders.Add(Constants.ShipTN.OrderType.Absorb);
            _legalOrders.Add(Constants.ShipTN.OrderType.RefuelTargetFleet);
            _legalOrders.Add(Constants.ShipTN.OrderType.ResupplyTargetFleet);
            _legalOrders.Add(Constants.ShipTN.OrderType.ReloadTargetFleet);



            _legalOrders.Add(Constants.ShipTN.OrderType.LandOnAssignedMothership);
            _legalOrders.Add(Constants.ShipTN.OrderType.LandOnMotherShipNoAssign);
            _legalOrders.Add(Constants.ShipTN.OrderType.LandOnMothershipAssign);
            _legalOrders.Add(Constants.ShipTN.OrderType.TractorSpecifiedShip);
            _legalOrders.Add(Constants.ShipTN.OrderType.TractorSpecifiedShipyard);


        }


        public override List<Constants.ShipTN.OrderType> LegalOrders(Faction faction)
        {
            List<Constants.ShipTN.OrderType> legalOrders = new List<Constants.ShipTN.OrderType>();
            legalOrders.AddRange(_legalOrders);
            ShipTN[] shipsArray = this.Ships.ToArray();
            if (Array.Exists(shipsArray, x => x.ShipClass.IsTanker)) //if this fleet is targeted and has a IsTanker.
                legalOrders.Add(Constants.ShipTN.OrderType.RefuelFromTargetFleet);
            if (Array.Exists(shipsArray, x => x.ShipClass.IsSupply))//if this fleet is targeted and has a IsSupply.
                legalOrders.Add(Constants.ShipTN.OrderType.ResupplyFromTargetFleet);
            if (Array.Exists(shipsArray, x => x.ShipClass.IsCollier))//if this fleet is targeted and has a IsCollier.
                legalOrders.Add(Constants.ShipTN.OrderType.ReloadFromTargetFleet);
            return legalOrders;
        }

        /// <summary>
        /// list of legal orders this TG can use againsed other entitys
        /// </summary>
        /// <returns></returns>
        public List<Constants.ShipTN.OrderType> LegalOrdersTG()
        {

            ShipTN[] shipsArray = this.Ships.ToArray();
            List<Constants.ShipTN.OrderType> legalOrders = new List<Constants.ShipTN.OrderType>();
            legalOrders.Add(Constants.ShipTN.OrderType.MoveTo);
            legalOrders.Add(Constants.ShipTN.OrderType.ExtendedOrbit);
            legalOrders.Add(Constants.ShipTN.OrderType.Picket);
            legalOrders.Add(Constants.ShipTN.OrderType.RefuelFromColony);
            legalOrders.Add(Constants.ShipTN.OrderType.RefuelFromTargetFleet);
            legalOrders.Add(Constants.ShipTN.OrderType.ResupplyFromColony);
            legalOrders.Add(Constants.ShipTN.OrderType.ResupplyFromTargetFleet);
            legalOrders.Add(Constants.ShipTN.OrderType.SendMessage);

            if (Array.Exists(shipsArray, x => x.CurrentCrew < x.ShipClass.TotalRequiredCrew))
                legalOrders.Add(Constants.ShipTN.OrderType.LoadCrewFromColony);

            legalOrders.Add(Constants.ShipTN.OrderType.ActivateTransponder);
            legalOrders.Add(Constants.ShipTN.OrderType.DeactivateTransponder);

            /// <summary>
            /// TaskGroups with active sensors:
            /// </summary>
            if (Array.Exists(shipsArray, x => x.ActiveList.List.Count > 0))
            {
                legalOrders.Add(Constants.ShipTN.OrderType.ActivateSensors);
                legalOrders.Add(Constants.ShipTN.OrderType.DeactivateSensors);
            }
            /// <summary>
            /// Taskgroups with shield equipped ships:
            /// </summary>
            if (Array.Exists(shipsArray, x => x.ShipShield.Count > 0))
            {
                legalOrders.Add(Constants.ShipTN.OrderType.ActivateShields);
                legalOrders.Add(Constants.ShipTN.OrderType.DeactivateShields);
            }
            /// <summary>
            /// Any Taskgroup of more than one vessel.
            /// </summary>
            if (this.Ships.Count() > 1)
            {
                legalOrders.Add(Constants.ShipTN.OrderType.EqualizeFuel);
                legalOrders.Add(Constants.ShipTN.OrderType.EqualizeMSP);
                legalOrders.Add(Constants.ShipTN.OrderType.DivideFleetToSingleShips);
            }

            /// <summary>
            /// Any taskgroup that has sub task groups created from it, such as by a divide order.
            /// </summary>
            //IncorporateSubfleet,

            /// <summary>
            /// Military Ship Specific orders:
            /// </summary>
            if (Array.Exists(shipsArray, x => x.ShipClass.IsMilitary))
                legalOrders.Add(Constants.ShipTN.OrderType.BeginOverhaul);

            /// <summary>
            /// Targeted on taskforce specific orders:
            /// </summary>
            legalOrders.Add(Constants.ShipTN.OrderType.Follow);
            legalOrders.Add(Constants.ShipTN.OrderType.Join);
            legalOrders.Add(Constants.ShipTN.OrderType.Absorb);
            /// <summary>
            /// JumpPoint Capable orders only:
            /// </summary>
            legalOrders.Add(Constants.ShipTN.OrderType.StandardTransit);
            legalOrders.Add(Constants.ShipTN.OrderType.SquadronTransit);
            legalOrders.Add(Constants.ShipTN.OrderType.TransitAndDivide);


            /// <summary>
            /// Cargo Hold specific orders when targeted on population/planet:
            /// </summary>
            if (this.TotalCargoTonnage > 0)
            {
                legalOrders.Add(Constants.ShipTN.OrderType.LoadInstallation);
                legalOrders.Add(Constants.ShipTN.OrderType.LoadShipComponent);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadInstallation);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadShipComponent);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadAll);
                legalOrders.Add(Constants.ShipTN.OrderType.LoadAllMinerals);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadAllMinerals);
                legalOrders.Add(Constants.ShipTN.OrderType.LoadMineral);
                legalOrders.Add(Constants.ShipTN.OrderType.LoadMineralWhenX);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadMineral);
                legalOrders.Add(Constants.ShipTN.OrderType.LoadOrUnloadMineralsToReserve);
            }

            /// <summary>
            /// Colony ship specific orders:
            /// </summary>
            if (this.TotalCryoCapacity > 0)
            {
                legalOrders.Add(Constants.ShipTN.OrderType.LoadColonists);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadColonists);

            }

            /// <summary>
            /// GeoSurvey specific orders:
            /// </summary>
            //if (hasgeo)
            //    legalOrders.Add(Constants.ShipTN.OrderType.DetachNonGeoSurvey);
            /// <summary>
            /// Grav survey specific orders:
            /// </summary>
            //if (hasGrav)
            //    legalOrders.Add(Constants.ShipTN.OrderType.DetachNonGravSurvey);


            /// <summary>
            /// Jump Gate Construction Module specific orders:
            /// </summary>
            //if (Array.Exists(shipsArray, x=> x.
            //
            //legalOrders.Add(Constants.ShipTN.OrderType.BuildJumpGate);

            /// <summary>
            /// Tanker Specific:
            /// </summary>
            if (Array.Exists(shipsArray, x => x.ShipClass.IsTanker))
            {
                legalOrders.Add(Constants.ShipTN.OrderType.RefuelFromOwnTankers);
                legalOrders.Add(Constants.ShipTN.OrderType.RefuelTargetFleet);
                legalOrders.Add(Constants.ShipTN.OrderType.DetachTankers);
            }
            /// <summary>
            /// Supply Ship specific:
            /// </summary>
            if (Array.Exists(shipsArray, x => x.ShipClass.IsSupply))
            {
                legalOrders.Add(Constants.ShipTN.OrderType.ResupplyFromOwnSupplyShips);
                legalOrders.Add(Constants.ShipTN.OrderType.ResupplyTargetFleet);
                legalOrders.Add(Constants.ShipTN.OrderType.DetachSupplyShips);
            }
            /// <summary>
            /// Collier Specific:
            /// </summary>
            if (Array.Exists(shipsArray, x => x.ShipClass.IsCollier))
            {
                legalOrders.Add(Constants.ShipTN.OrderType.ReloadFromOwnColliers);
                legalOrders.Add(Constants.ShipTN.OrderType.ReloadTargetFleet);
                legalOrders.Add(Constants.ShipTN.OrderType.DetachColliers);
            }

            /// <summary>
            /// Any ship with a magazine or launch tube:
            /// </summary>
            if (Array.Exists(shipsArray, x => x.CurrentMagazineCapacityMax > 0))
            {
                legalOrders.Add(Constants.ShipTN.OrderType.LoadOrdnanceFromColony);
                legalOrders.Add(Constants.ShipTN.OrderType.UnloadOrdnanceToColony);
            }

            /// <summary>
            /// Only want to have ships with magazines to have this order. box launch setups have to reload from planets.
            /// </summary>
            if (Array.Exists(shipsArray, x => x.CurrentMagazineMagCapacityMax > 0))
            {
                legalOrders.Add(Constants.ShipTN.OrderType.ReloadFromTargetFleet);
            }

            /// <summary>
            /// Any taskgroup, but target must have hangar bays, perhaps check to see if capacity is available.
            /// </summary>
            //if (Array.Exists(shipsArray, x => x.
            //legalOrders.Add(Constants.ShipTN.OrderType.LandOnAssignedMothership);
            //legalOrders.Add(Constants.ShipTN.OrderType.LandOnMotherShipNoAssign);
            //legalOrders.Add(Constants.ShipTN.OrderType.LandOnMothershipAssign);

            //if (Array.Exists(shipsArray, x=> x.
            //legalOrders.Add(Constants.ShipTN.OrderType.TractorSpecifiedShip);
            //legalOrders.Add(Constants.ShipTN.OrderType.TractorSpecifiedShipyard);
            //legalOrders.Add(Constants.ShipTN.OrderType.ReleaseAt);

            return legalOrders;
        }

        #region Add Ship To TaskGroup

        /// <summary>
        /// Adds a ship to a taskgroup, will call sorting and sensor handling.
        /// </summary>
        /// <param name="shipDef">definition of the ship to be added.</param>
        public void AddShip(ShipClassTN shipDef, String Title)
        {
            ShipTN ship = new ShipTN(shipDef, Ships.Count, GameState.Instance.CurrentSecond, this, TaskGroupFaction, Title);
            Ships.Add(ship);

            /// <summary>
            /// Refuel and ReCrew this ship here?
            /// </summary>

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

            TotalCargoTonnage = TotalCargoTonnage + shipDef.TotalCargoCapacity;
            TotalCryoCapacity = TotalCryoCapacity + shipDef.SpareCryoBerths;

            UpdatePassiveSensors(ship);

            AddShipToSort(ship);

            /// <summary>
            /// Let the UI know it should re check this taskgroup's sensors.
            /// </summary>
            SensorUpdateAck++;
        }

        /// <summary>
        /// Separate from AddShip, this will add an existing ship to this taskgroup.
        /// </summary>
        /// <param name="Ship">Ship to be added</param>
        public void AddShipTo(ShipTN Ship)
        {
            Ships.Add(Ship);

            if(Ships.Count == 1)
            {
                MaxSpeed = Ship.ShipClass.MaxSpeed;
                CurrentSpeed = MaxSpeed;
            }
            else
            {
                if (CurrentSpeed == MaxSpeed)
                {
                    if (Ship.ShipClass.MaxSpeed < MaxSpeed)
                    {
                        MaxSpeed = Ship.ShipClass.MaxSpeed;
                        CurrentSpeed = MaxSpeed;
                    }
                }
                /// <summary>
                /// Current Speed is lower than the maxspeed which means that the user has set current speed below max speed already.
                /// </summary>
                else
                {
                    /// <summary>
                    /// Set the new max speed of the taskgroup if there is one.
                    /// </summary>
                    if (Ship.ShipClass.MaxSpeed < MaxSpeed)
                    {
                        MaxSpeed = Ship.ShipClass.MaxSpeed;
                    }

                    /// <summary>
                    /// Max speed is less than the set current speed.
                    /// </summary>
                    if (MaxSpeed < CurrentSpeed)
                    {
                        CurrentSpeed = MaxSpeed;
                    }
                }
            }

            for (int loop = 0; loop < Ships.Count; loop++)
            {
                Ships[loop].SetSpeed(CurrentSpeed);
            }

            /// <summary>
            /// Update the taskgroupwide statistics here, consider moving this to its own function should it grow beyond this.
            /// </summary>
            TotalCargoTonnage = TotalCargoTonnage + Ship.ShipClass.TotalCargoCapacity;
            TotalCryoCapacity = TotalCryoCapacity + Ship.ShipClass.SpareCryoBerths;

            /// <summary>
            /// Add this ship to the sensor datagroups of this taskgroup
            /// </summary>
            UpdatePassiveSensors(Ship);
            AddShipToSort(Ship);

            /// <summary>
            /// Let the UI know it should re check this taskgroup's sensors.
            /// </summary>
            SensorUpdateAck++;
        }

        /// <summary>
        /// Remove ship will remove a ship from this taskgroup, typically in preparation for adding it to another taskgroup. ship destruction logic is handled elsewhere.
        /// </summary>
        /// <param name="Ship">Ship to be removed</param>
        public void RemoveShipFrom(ShipTN Ship)
        {
            /// <summary>
            /// Update taskgroup wide statistics here. eventually this may need to be its own function.
            /// </summary>
            TotalCargoTonnage = TotalCargoTonnage - Ship.ShipClass.TotalCargoCapacity;
            TotalCryoCapacity = TotalCryoCapacity - Ship.ShipClass.SpareCryoBerths;

            /// <summary>
            /// This can return false. not sure what the implications of such a return would be. other then that this function handles removing this ship from the taskgroup's sensor
            /// data structures.
            /// </summary>
            RemoveShipFromTaskGroup(Ship);

            Ships.Remove(Ship);

            if (Ships.Count == 0)
            {
                MaxSpeed = 1;
                CurrentSpeed = 1;
            }
            else if (Ships.Count == 1)
            {
                if (CurrentSpeed == MaxSpeed)
                {
                    MaxSpeed = Ships[0].ShipClass.MaxSpeed;
                    CurrentSpeed = MaxSpeed;
                }
                else
                    MaxSpeed = Ships[0].ShipClass.MaxSpeed;
            }
            else
            {
                /// <summary>
                /// Only set the speed to a new potentially higher max speed if there is one, and if the user has set this taskgroup to its max speed.
                /// The assumption is that the user will want to keep his taskgroup at its maximum speed.
                /// </summary>
                int lowestMax = Ships[0].ShipClass.MaxSpeed;
                for (int loop = 1; loop < Ships.Count; loop++)
                {
                    if (Ships[loop].ShipClass.MaxSpeed < lowestMax)
                    {
                        lowestMax = Ships[loop].ShipClass.MaxSpeed;
                    }
                }
                if (CurrentSpeed == MaxSpeed)
                {
                    MaxSpeed = lowestMax;
                    CurrentSpeed = MaxSpeed;
                }
                else
                    MaxSpeed = lowestMax;
            }

            for (int loop = 0; loop < Ships.Count; loop++)
            {
                Ships[loop].SetSpeed(CurrentSpeed);
            }

            /// <summary>
            /// Let the UI know it should re check this taskgroup's sensors.
            /// </summary>
            SensorUpdateAck++;
        }

        /// <summary>
        /// Given a ship in this taskgroup, transfer said ship to TaskGroupTo
        /// </summary>
        /// <param name="Ship">Ship present in this taskgroup that should be transfered</param>
        /// <param name="TaskGroupTo">Taskgroup that will receive this ship</param>
        public void TransferShipToTaskGroup(ShipTN Ship, TaskGroupTN TaskGroupTo)
        {
            /// <summary>
            /// This ship is not in the taskgroup so return immediately.
            /// </summary>
            if (Ships.Contains(Ship) == false)
                return;

            BindingList<int> activeSensorIndices = new BindingList<int>();
            for (int activeIterator = 0; activeIterator < Ship.ShipASensor.Count; activeIterator++)
            {
                /// <summary>
                /// This active sensor is both active and intact, so preserve its index, and set isActive to false(the sensor will be reactivated in its new TG.
                /// Destroyed sensors can keep their isActive state, as when they are repaired the repair function should handle reactivating them.
                /// </summary>
                if (Ship.ShipASensor[activeIterator].isActive == true && Ship.ShipASensor[activeIterator].isDestroyed == false)
                {
                    activeSensorIndices.Add(activeIterator);
                    Ship.ShipASensor[activeIterator].isActive = false;
                }
            }

            /// <summary>
            /// Move the ship between the two.
            /// </summary>
            RemoveShipFrom(Ship);
            TaskGroupTo.AddShipTo(Ship);

            /// <summary>
            /// reactivate any sensors that were active in the previous taskgroup.
            /// </summary>
            int ShipIndex = TaskGroupTo.Ships.IndexOf(Ship);
            foreach(int activeIndex in activeSensorIndices)
               TaskGroupTo.SetActiveSensor(ShipIndex, activeIndex, true);
        }

        /// <summary>
        /// Sets taskgroup speed.
        /// </summary>
        /// <param name="Speed">New speed</param>
        public void SetSpeed(int Speed)
        {
            if (Speed > MaxSpeed)
            {
                CurrentSpeed = MaxSpeed;
            }
            else if (Speed < 0)
            {
                CurrentSpeed = 0;
            }
            else
            {
                CurrentSpeed = Speed;
            }

            for (int loop = 0; loop < Ships.Count; loop++)
                Ships[loop].SetSpeed(CurrentSpeed);
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
                    if ((BestThermalCount == 0) || (ship.ShipPSensor[loop].pSensorDef.rating > BestThermal.pSensorDef.rating))
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
                int value = -1, Last = -1, First = -1, NewValue = -1, LastValue = -1;
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

                if (value >= Last)
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
                    LastValue = First;

                    NextNode = NextNode.Next;

                    bool done = false;
                    while (done == false)
                    {
                        switch (TEA)
                        {
                            case 0: NewValue = Ships[NextNode.Value].CurrentThermalSignature;
                                break;
                            case 1: NewValue = Ships[NextNode.Value].CurrentEMSignature;
                                break;
                            case 2: NewValue = Ships[NextNode.Value].TotalCrossSection;
                                break;
                        }

                        if (value < LastValue)
                        {
#if LOG4NET_ENABLED
#warning faction messagelog this?
                            String MSG = String.Format("Taskgroup {0} Sort messed up between {1} and {2}, current NextNodeValue is {3}\n. Condition One with V < LV", Name, LastValue, NewValue, NextNode.Value);
                            logger.Debug(MSG);
#endif
                        }
                        else if (value == LastValue && value > NewValue)
                        {
#if LOG4NET_ENABLED
#warning faction messagelog this?
                            String MSG = String.Format("Taskgroup {0} Sort messed up between {1} and {2}, current NextNodeValue is {3}. Condition Two with V = LV V > NV\n", Name, LastValue, NewValue, NextNode.Value);
                            logger.Debug(MSG);
#endif
                        }

                        if (value <= NewValue && value >= LastValue)
                        {
                            SortList.AddBefore(NextNode, Sort);
                            done = true;
                        }

                        NextNode = NextNode.Next;
                        LastValue = NewValue;

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
            AddNodeToSort(ThermalSortList, Ship.ThermalList, 0);
            AddNodeToSort(EMSortList, Ship.EMList, 1);
            AddNodeToSort(ActiveSortList, Ship.ActiveList, 2);
        }
        #endregion


        #region Taskgroup Sensor activation and emissions sorting
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
                        else if (ActiveSensorQue.Count != 1)
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

                        ActiveSensorQue.RemoveAt((ActiveSensorQue.Count - 1));
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
                SortShipBySignature(Ships[ShipIndex].EMList, EMSortList, 1);
            }

            /// <summary>
            /// Let the UI know it should re check this taskgroup's sensors.
            /// </summary>
            SensorUpdateAck++;
        }
        /// <summary>
        /// End SetActiveSensor
        /// </summary>

        /// <summary>
        /// Sort ship by signature takes a ship's linkedList node and puts it in the appropriate place in SortList. this does not sort signatures, that must be done elsewhere.
        /// </summary>
        /// <param name="ShipSignatureNode">The node for the ship.</param>
        /// <param name="SortList">The overall taskgroup list.</param>
        /// <param name="TEA">Whether this is a thermal,em, or active sorting.</param>
        public void SortShipBySignature(LinkedListNode<int> ShipSignatureNode, LinkedList<int> SortList, int TEA)
        {

            /// <summary>
            /// Before the sortlist can be worked on, the values must be adjusted accordingly.
            /// Node.Value is this ship's placement within the sortList, not its signature, this is an important distinction to be aware of.
            /// </summary>

            LinkedListNode<int> Prev = ShipSignatureNode.Previous;
            LinkedListNode<int> Next = ShipSignatureNode.Next;

            if (ShipSignatureNode == SortList.First)
                Prev = ShipSignatureNode;
            else
                Prev = ShipSignatureNode.Previous;

            if (ShipSignatureNode == SortList.Last)
                Next = ShipSignatureNode;
            else
                Next = ShipSignatureNode.Next;

            switch (TEA)
            {
                case 0:
                    if (Ships[ShipSignatureNode.Value].CurrentThermalSignature < Ships[Prev.Value].CurrentThermalSignature)
                    {
                        /// <summary>
                        /// Thermal signature was lowered, and we need to adjust value downwards. Engine destruction most likely.
                        /// </summary>

                        while (Ships[ShipSignatureNode.Value].CurrentThermalSignature < Ships[Prev.Value].CurrentThermalSignature)
                        {
                            if (Prev == SortList.First)
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddBefore(Prev, ShipSignatureNode);
                                break;
                            }
                            else
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddBefore(Prev, ShipSignatureNode);
                                Prev = ShipSignatureNode.Previous;
                            }
                        }
                    }
                    else if (Ships[ShipSignatureNode.Value].CurrentThermalSignature > Ships[Next.Value].CurrentThermalSignature)
                    {
                        /// <summary>
                        /// Thermal signature went up, engine repair is the only condition that would cause this that I can think of.
                        /// </summary>
                        while (Ships[ShipSignatureNode.Value].CurrentThermalSignature > Ships[Next.Value].CurrentThermalSignature)
                        {
                            if (Next == SortList.Last)
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddAfter(Next, ShipSignatureNode);
                                break;
                            }
                            else
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddAfter(Next, ShipSignatureNode);
                                Next = ShipSignatureNode.Next;
                            }
                        }
                    }
                    else
                    {
                        /// <summary>
                        /// Thermal signature may have changed, but not enough to alter Value placement.
                        /// </summary>
                    }
                    break;
                case 1:
                    if (Ships[ShipSignatureNode.Value].CurrentEMSignature < Ships[Prev.Value].CurrentEMSignature)
                    {
                        /// <summary>
                        /// EM signature was lowered, and we need to adjust value downwards. Shield/sensor deactivation/destruction are probable causes.
                        /// </summary>
                        while (Ships[ShipSignatureNode.Value].CurrentEMSignature < Ships[Prev.Value].CurrentEMSignature)
                        {
                            if (Prev == SortList.First)
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddBefore(Prev, ShipSignatureNode);
                                break;
                            }
                            else
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddBefore(Prev, ShipSignatureNode);
                                Prev = ShipSignatureNode.Previous;
                            }
                        }
                    }
                    else if (Ships[ShipSignatureNode.Value].CurrentEMSignature > Ships[Next.Value].CurrentEMSignature)
                    {
                        /// <summary>
                        /// EM signature went up, Shield/sensor activation or repair are probable causes of this.
                        /// </summary>
                        while (Ships[ShipSignatureNode.Value].CurrentEMSignature > Ships[Next.Value].CurrentEMSignature)
                        {
                            if (Next == SortList.Last)
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddAfter(Next, ShipSignatureNode);
                                break;
                            }
                            else
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddAfter(Next, ShipSignatureNode);
                                Next = ShipSignatureNode.Next;
                            }
                        }
                    }
                    else
                    {
                        /// <summary>
                        /// EM signature may have changed, but not enough to alter Value placement.
                        /// </summary>
                    }
                    break;
                case 2:
                    if (Ships[ShipSignatureNode.Value].TotalCrossSection < Ships[Prev.Value].TotalCrossSection)
                    {
                        /// <summary>
                        /// TCS signature was lowered, a cloaking device got repaired.
                        /// </summary>
                        while (Ships[ShipSignatureNode.Value].TotalCrossSection < Ships[Prev.Value].TotalCrossSection)
                        {
                            if (Prev == SortList.First)
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddBefore(Prev, ShipSignatureNode);
                                break;
                            }
                            else
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddBefore(Prev, ShipSignatureNode);
                                Prev = ShipSignatureNode.Previous;
                            }
                        }
                    }
                    else if (Ships[ShipSignatureNode.Value].TotalCrossSection > Ships[Next.Value].TotalCrossSection)
                    {
                        /// <summary>
                        /// TCS signature went up, cloaking device destruction is basically it.
                        /// </summary>
                        while (Ships[ShipSignatureNode.Value].TotalCrossSection > Ships[Next.Value].TotalCrossSection)
                        {
                            if (Next == SortList.Last)
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddAfter(Next, ShipSignatureNode);
                                break;
                            }
                            else
                            {
                                SortList.Remove(ShipSignatureNode);
                                SortList.AddAfter(Next, ShipSignatureNode);
                                Next = ShipSignatureNode.Next;
                            }
                        }
                    }
                    else
                    {
                        /// <summary>
                        /// TCS may have changed, but not enough to alter Value placement.
                        /// </summary>
                    }
                    break;

            };
        }
        /// <summary>
        /// End SortShipBySignature
        /// </summary>

        /// <summary>
        /// RemoveShipFromTaskGroup handles removal of the ship from the various detection linked lists. It also traverses the linked lists
        /// and decrements node values as appropriate to reflect the new state of the linked list ids.
        /// </summary>
        /// <param name="Ship">Ship to be removed.</param>
        /// <returns>Were nodes removed from the various linkedLists?</returns>
        public bool RemoveShipFromTaskGroup(ShipTN Ship)
        {
            /// <summary>
            /// Both active and passive sensors need to be updated to reflect that this ship is no longer part of the taskgroup.
            /// </summary>
            for(int activeIterator = 0; activeIterator < Ship.ShipASensor.Count; activeIterator++)
            {
                SetActiveSensor(Ships.IndexOf(Ship), activeIterator, false);
            }

            for (int passiveIterator = 0; passiveIterator < Ship.ShipPSensor.Count; passiveIterator++)
            {
                PassiveSensorTN PassiveS = Ship.ShipPSensor[passiveIterator];
                /// <summary>
                /// Performance could be improved here by storing a sorted linked list of all passive sensors if need be.
                /// I don't believe that sensor destruction events will be common enough to necessitate that however.
                /// </summary>
                if (PassiveS.pSensorDef.thermalOrEM == PassiveSensorType.EM)
                {
                    if (PassiveS.pSensorDef.rating == BestEM.pSensorDef.rating)
                    {
                        BestEMCount--;

                        if (BestEMCount == 0)
                        {
                            for (int loop = 0; loop < Ships.Count; loop++)
                            {
                                for (int loop2 = 0; loop2 < Ships[loop].ShipPSensor.Count; loop2++)
                                {
                                    if (Ships[loop].ShipPSensor[loop2].pSensorDef.thermalOrEM == PassiveSensorType.EM &&
                                        Ships[loop].ShipPSensor[loop2].isDestroyed == false)
                                    {
                                        if (BestEMCount == 0 || Ships[loop].ShipPSensor[loop2].pSensorDef.rating > BestEM.pSensorDef.rating)
                                        {
                                            BestEM = Ships[loop].ShipPSensor[loop2];
                                            BestEMCount = 1;
                                        }
                                        else if (Ships[loop].ShipPSensor[loop2].pSensorDef.rating == BestEM.pSensorDef.rating)
                                        {
                                            BestEMCount++;
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
                else
                {
                    if (PassiveS.pSensorDef.rating == BestThermal.pSensorDef.rating)
                    {
                        BestThermalCount--;

                        if (BestThermalCount == 0)
                        {
                            for (int loop = 0; loop < Ships.Count; loop++)
                            {
                                for (int loop2 = 0; loop2 < Ships[loop].ShipPSensor.Count; loop2++)
                                {
                                    if (Ships[loop].ShipPSensor[loop2].pSensorDef.thermalOrEM == PassiveSensorType.Thermal &&
                                        Ships[loop].ShipPSensor[loop2].isDestroyed == false)
                                    {
                                        if (BestThermalCount == 0 || Ships[loop].ShipPSensor[loop2].pSensorDef.rating > BestThermal.pSensorDef.rating)
                                        {
                                            BestThermal = Ships[loop].ShipPSensor[loop2];
                                            BestThermalCount = 1;
                                        }
                                        else if (Ships[loop].ShipPSensor[loop2].pSensorDef.rating == BestThermal.pSensorDef.rating)
                                        {
                                            BestThermalCount++;
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Now on to the detection linked lists. Ship must be removed, and the lists must be altered to reflect the fact that ship is gone.
            /// </summary>
            ThermalSortList.Remove(Ship.ThermalList);
            EMSortList.Remove(Ship.EMList);
            ActiveSortList.Remove(Ship.ActiveList);

            if (ThermalSortList.Count == 0 || EMSortList.Count == 0 || ActiveSortList.Count == 0)
                return true;

            LinkedListNode<int> node;

            node = ThermalSortList.First;

            if (node.Value > Ship.ThermalList.Value)
                node.Value--;

            bool done = false;
            if (ThermalSortList.First == ThermalSortList.Last)
                done = true;

            while (done == false)
            {
                node = node.Next;

                if (node.Value > Ship.ThermalList.Value)
                    node.Value--;

                if (node == ThermalSortList.Last)
                    done = true;

            }

            node = EMSortList.First;

            if (node.Value > Ship.EMList.Value)
                node.Value--;

            done = false;
            if (EMSortList.First == EMSortList.Last)
                done = true;

            while (done == false)
            {
                node = node.Next;

                if (node.Value > Ship.EMList.Value)
                    node.Value--;

                if (node == EMSortList.Last)
                    done = true;

            }

            node = ActiveSortList.First;

            if (node.Value > Ship.ActiveList.Value)
                node.Value--;

            done = false;
            if (ActiveSortList.First == ActiveSortList.Last)
                done = true;

            while (done == false)
            {
                node = node.Next;

                if (node.Value > Ship.ActiveList.Value)
                    node.Value--;

                if (node == ActiveSortList.Last)
                    done = true;

            }

            /// <summary>
            /// Let the UI know it should re check this taskgroup's sensors. SimEntity can call just this function by itself for ship destruction. while ship transfers call this
            /// as part of another function. So SensorUpdateTick can be set twice. this shouldn't cause any issues.
            /// </summary>
            SensorUpdateAck++;

            return false;
        }
        #endregion


        #region Taskgroup movement and position as well as time requirement,heading and speed.
        /// <summary>
        /// GetPositionFromOrbit returns systemKm from SystemAU. If a ship is orbiting a body it will move with that body.
        /// </summary>
        public void GetPositionFromOrbit()
        {
            /// <summary>
            /// Classic: I commented this out to make secondary star orbits work, but left the rest of the adjustments in.
            /// In 0.3 I changed how planets store their position to use only their data, not their primary's data.
            /// </summary>
            Contact.Position.X = OrbitingBody.Position.X;
            Contact.Position.Y = OrbitingBody.Position.Y;
            Position.X = Contact.Position.X;
            Position.Y = Contact.Position.Y;
        }

        /// <summary>
        /// Update last position, current position, order distance, fuel counter, Time requirement, and timeslice:
        /// </summary>
        public void UpdateLastPosition()
        {
            Contact.LastPosition.X = Contact.Position.X;
            Contact.LastPosition.Y = Contact.Position.Y;
        }

        /// <summary>
        /// GetHeading determines the direction the ship should face to get to its current ordered target.
        /// </summary>
        public void GetHeading()
        {
            if (IsOrbiting)
            {
                GetPositionFromOrbit();
                SystemBody OrbitingPlanet = OrbitingBody as SystemBody;
                if (OrbitingPlanet.TaskGroupsInOrbit.Contains(this))
                {
                    OrbitingPlanet.TaskGroupsInOrbit.Remove(this);
                }
                IsOrbiting = false;
            }

            if (TaskGroupOrders.Count > 0)
            {
                double dX, dY;

                /// <summary>
                /// planets (and populations on planets) positions are stored relative to their star.
                /// I'll need to change all of this AGAIN when moons get rolled out. Gah. do it right then.
                /// Don't even need this switch anymore, but I'll leave it in for the time being. Note: Positions do not rely on parent objects anymore, other than in the calculation function.
                /// Position.X,Position.Y is exactly where every object is and should be with no adjustment needed.
                /// </summary>
                switch (TaskGroupOrders[0].target.SSEntity)
                {
                    case StarSystemEntityType.Body:
                        dX = Contact.Position.X - TaskGroupOrders[0].target.Position.X;
                        dY = Contact.Position.Y - TaskGroupOrders[0].target.Position.Y;
                        break;
                    case StarSystemEntityType.Population:
                        dX = Contact.Position.X - TaskGroupOrders[0].pop.Planet.Position.X;
                        dY = Contact.Position.Y - TaskGroupOrders[0].pop.Planet.Position.Y;
                        break;
                    case StarSystemEntityType.Invalid:
                        throw new InvalidOperationException("SSEntity Invalid");
                        break;
                    default:
                        dX = Contact.Position.X - TaskGroupOrders[0].target.Position.X;
                        dY = Contact.Position.Y - TaskGroupOrders[0].target.Position.Y;
                        break;
                }

                CurrentHeading = Pulsar4X.Helpers.GameMath.Angle.ToDegrees(Math.Atan(dY / dX));
            }
            else
                CurrentHeading = 0.0;
        }

        /// <summary>
        /// GetSpeed gets the current X and Y velocities required by the current heading.
        /// </summary>
        public void GetSpeed()
        {
            if (TaskGroupOrders.Count > 0)
            {
                double dX, dY;

                /// <summary>
                /// planets (and populations on planets) positions are stored relative to their star.
                /// Unnecessary if statement here due to 0.3 orbit change.
                /// </summary>
                if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                {
                    dX = Contact.Position.X - TaskGroupOrders[0].target.Position.X;
                    dY = Contact.Position.Y - TaskGroupOrders[0].target.Position.Y;
                }
                else if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Population)
                {
                    dX = Contact.Position.X - TaskGroupOrders[0].pop.Planet.Position.X;
                    dY = Contact.Position.Y - TaskGroupOrders[0].pop.Planet.Position.Y;
                }
                else
                {
                    dX = Contact.Position.X - TaskGroupOrders[0].target.Position.X;
                    dY = Contact.Position.Y - TaskGroupOrders[0].target.Position.Y;
                }

                double sign = 1.0;
                if (dX > 0.0)
                {
                    sign = -1.0;
                }

                /// <summary>
                /// minor matrix multiplication here.
                /// </summary>
                CurrentSpeedX = CurrentSpeed * Math.Cos(CurrentHeading * Constants.Units.Radian) * sign;
                CurrentSpeedY = CurrentSpeed * Math.Sin(CurrentHeading * Constants.Units.Radian) * sign;
            }
            else
            {
                CurrentSpeedX = 0.0;
                CurrentSpeedY = 0.0;
            }
        }

        /// <summary>
        /// How long will this order take given the TaskGroup's current speed?
        /// </summary>
        public void GetTimeRequirement()
        {
            if (TaskGroupOrders.Count > 0)
            {
                double dX, dY;

                /// <summary>
                /// planets (and populations on planets) positions are stored relative to their star.
                /// Lol no. not any more they aren't as of 0.3.
                /// </summary>
                if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                {
                    dX = Math.Abs(TaskGroupOrders[0].target.Position.X  - Contact.Position.X);
                    dY = Math.Abs(TaskGroupOrders[0].target.Position.Y  - Contact.Position.Y);
                }
                else if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Population)
                {
                    dX = Math.Abs(TaskGroupOrders[0].pop.Planet.Position.X  - Contact.Position.X);
                    dY = Math.Abs(TaskGroupOrders[0].pop.Planet.Position.Y - Contact.Position.Y);
                }
                else
                {
                    dX = Math.Abs(TaskGroupOrders[0].target.Position.X - Contact.Position.X);
                    dY = Math.Abs(TaskGroupOrders[0].target.Position.Y - Contact.Position.Y);
                }



                double dZ = Math.Sqrt(((dX * dX) + (dY * dY)));

                /// <summary>
                /// In this case there exists a possibility that TimeReq will overflow.
                /// </summary>
                if (dZ >= Constants.Units.MAX_KM_IN_AU)
                {
                    double Count = dZ / Constants.Units.MAX_KM_IN_AU;

                    /// <summary>
                    /// TimeRequirement is safe to calculate.
                    /// </summary>
                    if (Count < (double)CurrentSpeed)
                    {
                        TimeRequirement = (uint)Math.Ceiling(dZ / Distance.ToAU(CurrentSpeed));
                    }
                    else
                    {
                        /// <summary>
                        /// even though TimeReq is a uint I'll treat it as a "signed" int in this case.
                        /// </summary>
                        TimeRequirement = 2147483649;
                    }
                }
                else
                {

                    /// <summary>
                    /// This line hosed me with parenthesis requirements, should do things more explicitly.
                    /// </summary>
                    TimeRequirement = (uint)Math.Ceiling(dZ / Distance.ToAU(CurrentSpeed));
                }

            }
            else
            {
                TimeRequirement = 0;
            }
        }

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
                    Ships[loop].FuelCounter = Ships[loop].FuelCounter - (3600 * hours);
                    Ships[loop].CurrentFuel = Ships[loop].CurrentFuel - (Ships[loop].CurrentFuelUsePerHour * hours);

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

        #endregion


        #region Taskgroup orders issuing,following,performing,clearing
        /// <summary>
        /// Places an order into the que of orders.
        /// </summary>
        /// <param name="Order">Order is the order to be carried out.</param>
        /// <param name="Destination">Destination of the waypoint/planet/TaskGroup we are moving towards.</param>
        /// <param name="Secondary">Secondary will be an enum ID for facility type,component,troop formation, or tractorable ship/shipyard. -1 if not present.</param>
        public void IssueOrder(Order OrderToTaskGroup, int index = -1)
        {
            if (TaskGroupOrders.Count == 0)
            {
                NewOrders = true;
                DrawTravelLine = 0;
            }
            if (index == -1)
                TaskGroupOrders.Add(OrderToTaskGroup);
            else
                TaskGroupOrders.Insert(index, OrderToTaskGroup);

            int OrderCount = TaskGroupOrders.Count - 1;
            double dX = 0.0, dY = 0.0, dZ;

            if (TaskGroupOrders[OrderCount].typeOf == Constants.ShipTN.OrderType.StandardTransit ||
                TaskGroupOrders[OrderCount].typeOf == Constants.ShipTN.OrderType.SquadronTransit ||
                TaskGroupOrders[OrderCount].typeOf == Constants.ShipTN.OrderType.TransitAndDivide)
            {
                if (TaskGroupOrders[OrderCount].jumpPoint.Connect == null)
                {
                    CanOrder = Constants.ShipTN.OrderState.DisallowOrdersUnknownJump;
                }
            }

            if (CanOrder != Constants.ShipTN.OrderState.AcceptOrders)
            {
                return;
            }

            if (OrderCount == 0)
            {
                if (IsOrbiting)
                    GetPositionFromOrbit();

                /// <summary>
                /// planets (and populations on planets) positions are stored relative to their star.
                /// As of 0.3 this is not the case anymore. they are calculated relative to their parent star, but their position is set in their data structure.
                /// Totally unnecessary if statement block by the way.
                /// </summary>
                if (TaskGroupOrders[OrderCount].target.SSEntity == StarSystemEntityType.Body)
                {
                    dX = Math.Abs(TaskGroupOrders[OrderCount].target.Position.X  - Contact.Position.X);
                    dY = Math.Abs(TaskGroupOrders[OrderCount].target.Position.Y  - Contact.Position.Y);
                }
                else if (TaskGroupOrders[OrderCount].target.SSEntity == StarSystemEntityType.Population)
                {
                    dX = Math.Abs(TaskGroupOrders[OrderCount].pop.Planet.Position.X - Contact.Position.X);
                    dY = Math.Abs(TaskGroupOrders[OrderCount].pop.Planet.Position.Y - Contact.Position.Y);
                }
                else
                {
                    dX = Math.Abs(TaskGroupOrders[OrderCount].target.Position.X - Contact.Position.X);
                    dY = Math.Abs(TaskGroupOrders[OrderCount].target.Position.Y - Contact.Position.Y);
                }

            }
            else if (TaskGroupOrders[OrderCount - 1].typeOf == Constants.ShipTN.OrderType.StandardTransit ||
                     TaskGroupOrders[OrderCount - 1].typeOf == Constants.ShipTN.OrderType.SquadronTransit ||
                     TaskGroupOrders[OrderCount - 1].typeOf == Constants.ShipTN.OrderType.TransitAndDivide)
            {

                /// <summary>
                /// planets (and populations on planets) positions are stored relative to their star.
                /// As of 0.3 this is not the case anymore. they are calculated relative to their parent star, but their position is set in their data structure.
                /// Totally unnecessary if statement block by the way.
                /// </summary>
                if (TaskGroupOrders[OrderCount].target.SSEntity == StarSystemEntityType.Body)
                {
                    dX = Math.Abs(TaskGroupOrders[OrderCount].target.Position.X  - TaskGroupOrders[OrderCount - 1].jumpPoint.Connect.Position.X);
                    dY = Math.Abs(TaskGroupOrders[OrderCount].target.Position.Y  - TaskGroupOrders[OrderCount - 1].jumpPoint.Connect.Position.Y);
                }
                else if (TaskGroupOrders[OrderCount].target.SSEntity == StarSystemEntityType.Population)
                {
                    dX = Math.Abs(TaskGroupOrders[OrderCount].pop.Planet.Position.X  - TaskGroupOrders[OrderCount - 1].jumpPoint.Connect.Position.X);
                    dY = Math.Abs(TaskGroupOrders[OrderCount].pop.Planet.Position.Y  - TaskGroupOrders[OrderCount - 1].jumpPoint.Connect.Position.Y);
                }
                else
                {
                    dX = Math.Abs(TaskGroupOrders[OrderCount].target.Position.X - TaskGroupOrders[OrderCount - 1].jumpPoint.Connect.Position.X);
                    dY = Math.Abs(TaskGroupOrders[OrderCount].target.Position.Y - TaskGroupOrders[OrderCount - 1].jumpPoint.Connect.Position.Y);
                }
            }
            else
            {
                dX = Math.Abs(TaskGroupOrders[OrderCount].target.Position.X - TaskGroupOrders[OrderCount - 1].target.Position.X);
                dY = Math.Abs(TaskGroupOrders[OrderCount].target.Position.Y - TaskGroupOrders[OrderCount - 1].target.Position.Y);

                /// <summary>
                /// planets (and populations on planets) positions are stored relative to their star.
                /// Not any more etc,etc. The starting taskgroup is still too slow to catch a secondary star, but you can ambush them with waypoints placed along their orbits now.
                /// </summary>
                if (TaskGroupOrders[OrderCount].target.SSEntity == StarSystemEntityType.Body)
                {
                    dX = TaskGroupOrders[OrderCount].target.Position.X;
                    dY = TaskGroupOrders[OrderCount].target.Position.Y;
                }
                else if (TaskGroupOrders[OrderCount].target.SSEntity == StarSystemEntityType.Population)
                {
                    dX = TaskGroupOrders[OrderCount].pop.Planet.Position.X;
                    dY = TaskGroupOrders[OrderCount].pop.Planet.Position.Y;
                }
                else
                {
                    dX = TaskGroupOrders[OrderCount].target.Position.X;
                    dY = TaskGroupOrders[OrderCount].target.Position.Y;
                }

                if (TaskGroupOrders[OrderCount - 1].target.SSEntity == StarSystemEntityType.Body)
                {
                    dX = Math.Abs(dX - TaskGroupOrders[OrderCount - 1].target.Position.X);
                    dY = Math.Abs(dY - TaskGroupOrders[OrderCount - 1].target.Position.Y);
                }
                else if (TaskGroupOrders[OrderCount - 1].target.SSEntity == StarSystemEntityType.Population)
                {
                    dX = Math.Abs(dX - TaskGroupOrders[OrderCount - 1].pop.Planet.Position.X);
                    dY = Math.Abs(dY - TaskGroupOrders[OrderCount - 1].pop.Planet.Position.Y);
                }
                else
                {
                    dX = Math.Abs(dX - TaskGroupOrders[OrderCount - 1].target.Position.X);
                    dY = Math.Abs(dY - TaskGroupOrders[OrderCount - 1].target.Position.Y);
                }

            }
            dZ = Math.Sqrt(((dX * dX) + (dY * dY)));
            TotalOrderDistance = TotalOrderDistance + dZ;
        }


        /// <summary>
        /// TaskGroup order handling function 
        /// </summary>
        /// <param name="TimeSlice">How much time the taskgroup is alloted to perform its orders. unit is in seconds</param>
        public void FollowOrders(uint TimeSlice)
        {
            //if (NewOrders == true)
            //{
            GetHeading();
            GetSpeed();

            GetTimeRequirement();
            //NewOrders = false;
            //}

            if (TimeRequirement < TimeSlice)
            {
                /// <summary>
                /// Is the movement phase over?
                /// </summary>
                if (TimeRequirement != 0)
                {
                    /// <summary>
                    /// increase the taskgroup's fuel use counter.
                    /// </summary>
                    UseFuel(TimeRequirement);

                    /// <summary>
                    /// Move the taskgroup to the targeted location.
                    /// </summary>

                    double dX, dY;

                    if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                    {
                        dX = TaskGroupOrders[0].target.Position.X;
                        dY = TaskGroupOrders[0].target.Position.Y;
                    }
                    else if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Population)
                    {
                        dX = TaskGroupOrders[0].pop.Planet.Position.X;
                        dY = TaskGroupOrders[0].pop.Planet.Position.Y;
                    }
                    else
                    {
                        dX = TaskGroupOrders[0].target.Position.X;
                        dY = TaskGroupOrders[0].target.Position.Y;
                    }

                    Contact.UpdateLocationInSystem(dX, dY);
                    TotalOrderDistance = TotalOrderDistance - (double)(CurrentSpeed * TimeRequirement);

                    /// <summary>
                    /// Time requirement is the movement portion of the orders. subtract it here.
                    /// </summary>
                    TimeSlice = TimeSlice - TimeRequirement;
                    TimeRequirement = 0;

                    /// <summary>
                    /// Did we pull into orbit?
                    /// </summary>
                    if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body || TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Population)
                    {
                        IsOrbiting = true;

                        if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                            OrbitingBody = TaskGroupOrders[0].body;
                        else
                            OrbitingBody = TaskGroupOrders[0].pop.Planet;

                    }
                }

                /// <summary>
                /// By now time requirement is 0 and the program has moved on to perform orders.
                /// this will require additional time beyond time requirement.
                /// </summary>

                TimeSlice = PerformOrders(TimeSlice);

                /// <summary>
                /// move on to next order if possible
                /// </summary>
                if (TimeSlice > 0)
                {
                    if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.TaskGroup)
                    {

                        /// <summary>
                        /// Same TG, so TG to TG order
                        /// </summary>
                        if (TaskGroupOrders[0].taskGroup.TaskGroupFaction == TaskGroupFaction)
                        {
                            if (TaskGroupOrders[0].taskGroup.TaskGroupsOrdered.IndexOf(this) != -1)
                            {
                                TaskGroupOrders[0].taskGroup.TaskGroupsOrdered.Remove(this);
                            }
                        }
                        else
                        {
                            int check = -1;
                            for (int loop = 0; loop < TaskGroupOrders[0].taskGroup.Ships.Count; loop++)
                            {
                                if (TaskGroupOrders[0].taskGroup.Ships[loop].TaskGroupsOrdered.IndexOf(this) != -1)
                                {
                                    check = loop;
                                    TaskGroupOrders[0].taskGroup.Ships[loop].TaskGroupsOrdered.Remove(this);
                                    break;
                                }
                            }

                            if (check == -1)
                            {
                                String Entry = String.Format("Ship Error, no TaskGroupsOrdered found for TG {0} in Faction {1}, {2} has completed an order to move.", TaskGroupOrders[0].taskGroup.Name,
                                    TaskGroupOrders[0].taskGroup.TaskGroupFaction.Name, Name);
                                MessageEntry NME = new MessageEntry(MessageEntry.MessageType.Error, Contact.Position.System, Contact, GameState.Instance.GameDateTime, GameState.Instance.CurrentSecond, Entry);
                                TaskGroupFaction.MessageLog.Add(NME);
                            }
                        }
                    }
                    TaskGroupOrders.RemoveAt(0);

                    if (TaskGroupOrders.Count > 0)
                    {
                        NewOrders = true;
                        FollowOrders(TimeSlice);
                    }
                    else
                    {
                        DrawTravelLine = 1;
                        CanOrder = Constants.ShipTN.OrderState.AcceptOrders;
                    }
                }
            }
            else
            {

                Contact.Position.X = Contact.Position.X + Distance.ToAU(TimeSlice * CurrentSpeedX);
                Contact.Position.Y = Contact.Position.Y + Distance.ToAU(TimeSlice * CurrentSpeedY);

                TotalOrderDistance = TotalOrderDistance - Distance.ToAU(CurrentSpeed * TimeSlice);

                UseFuel(TimeSlice);

                /// <summary>
                /// This probably isn't needed since timeReqs are constantly recalculated.
                /// </summary>
                TimeRequirement = TimeRequirement - TimeSlice;

                TimeSlice = 0;
            }

        }
        /// <summary>
        /// End FollowOrders()
        /// </summary>

        /// <summary>
        /// What order should be performed at the target location?
        /// Update the state of the taskgroup if it is doing a blocking order. also fill out the timer for how long the ship will be blocked.
        /// overhaul follow orders to make sure that this is handled.
        /// </summary>
        public uint PerformOrders(uint TimeSlice)
        {
            Order currentOrder = TaskGroupOrders[0];
            /// <summary>
            /// Handle orbiting planets here. breaking orbits is done elsewhere.
            /// </summary>
            if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body || TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Population)
            {
                IsOrbiting = true;
                if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                {
                    OrbitingBody = TaskGroupOrders[0].body;
                }
                else
                {
                    OrbitingBody = TaskGroupOrders[0].pop.Planet;
                }
                SystemBody OrbitingPlanet = OrbitingBody as SystemBody;
                if (!OrbitingPlanet.TaskGroupsInOrbit.Contains(this))
                {
                    OrbitingPlanet.TaskGroupsInOrbit.Add(this);
                }
            }

            if (TaskGroupOrders[0].orderDelay >= TimeSlice)
            {
                TaskGroupOrders[0].orderDelay = TaskGroupOrders[0].orderDelay - (int)TimeSlice;
                TimeSlice = 0;
            }
            else if (TaskGroupOrders[0].orderDelay < TimeSlice)
            {
                TimeSlice = TimeSlice - (uint)TaskGroupOrders[0].orderDelay;
                TaskGroupOrders[0].orderDelay = 0;

                switch ((int)TaskGroupOrders[0].typeOf)
                {

                    #region MoveTo
                    /// <summary>
                    /// Perform no orders for moveto:
                    /// </summary>
                    case (int)Constants.ShipTN.OrderType.MoveTo:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        break;
                    #endregion

                    #region Refuel From Colony
                    case (int)Constants.ShipTN.OrderType.RefuelFromColony:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        for (int loop = 0; loop < Ships.Count; loop++)
                        {
                            if (TaskGroupOrders[0].pop.FuelStockpile == 0.0f)
                            {
                                /// <summary>
                                /// Orders could not be carried out. A sample message log is below, something like this will need to be done for every type of message.
                                /// </summary>
                                /*DateTime TimeOfMessage = new DateTime();
                                MessageEntry NewMessage = new MessageEntry(Contact.Position.System, Contact, TimeOfMessage, (int)TimeSlice, "Refueling order could not be completed.");
                                this.Faction.MessageLog.Add(NewMessage);*/
                                break;
                            }
                            TaskGroupOrders[0].pop.FuelStockpile = Ships[loop].Refuel(TaskGroupOrders[0].pop.FuelStockpile);
                        }
                        break;
                    #endregion

                    #region Resupply From Colony
                    case (int)Constants.ShipTN.OrderType.ResupplyFromColony:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        for (int loop = 0; loop < Ships.Count; loop++)
                        {
                            if (TaskGroupOrders[0].pop.MaintenanceSupplies == 0)
                            {
                                /// <summary>
                                /// Order could not be carried out.
                                /// </summary>
                                break;
                            }
                            TaskGroupOrders[0].pop.MaintenanceSupplies = (float)Ships[loop].Resupply((int)Math.Floor(TaskGroupOrders[0].pop.MaintenanceSupplies));
                        }
                        break;
                    #endregion

                    #region Load Installation
                    /// <summary>
                    /// Load Installation:
                    /// </summary>
                    case (int)Constants.ShipTN.OrderType.LoadInstallation:
                        if (TaskGroupOrders[0].orderTimeRequirement == -1)
                        {
                            CanOrder = Constants.ShipTN.OrderState.CurrentlyLoading;
                            int TaskGroupLoadTime = CalcTaskGroupLoadTime(Constants.ShipTN.LoadType.Cargo);
                            int PlanetaryLoadTime = TaskGroupOrders[0].pop.CalculateLoadTime(TaskGroupLoadTime);

                            TaskGroupOrders[0].orderTimeRequirement = PlanetaryLoadTime;
                        }

                        if (TimeSlice > TaskGroupOrders[0].orderTimeRequirement)
                        {
                            TimeSlice = TimeSlice - (uint)TaskGroupOrders[0].orderTimeRequirement;
                            TaskGroupOrders[0].orderTimeRequirement = 0;
                            LoadCargo(TaskGroupOrders[0].pop, (Installation.InstallationType)TaskGroupOrders[0].secondary, TaskGroupOrders[0].tertiary);
                            CanOrder = Constants.ShipTN.OrderState.AcceptOrders;
                        }
                        else
                        {
                            TaskGroupOrders[0].orderTimeRequirement = TaskGroupOrders[0].orderTimeRequirement - (int)TimeSlice;
                            TimeSlice = 0;
                        }
                        break;
                    #endregion

                    #region Load Ship Component
                    /// <summary>
                    /// Load Ship Component:
                    /// </summary>
                    case (int)Constants.ShipTN.OrderType.LoadShipComponent:
                        if (TaskGroupOrders[0].orderTimeRequirement == -1)
                        {
                            CanOrder = Constants.ShipTN.OrderState.CurrentlyLoading;
                            int TaskGroupLoadTime = CalcTaskGroupLoadTime(Constants.ShipTN.LoadType.Cargo);
                            int PlanetaryLoadTime = TaskGroupOrders[0].pop.CalculateLoadTime(TaskGroupLoadTime);

                            TaskGroupOrders[0].orderTimeRequirement = PlanetaryLoadTime;
                        }

                        if (TimeSlice > TaskGroupOrders[0].orderTimeRequirement)
                        {
                            TimeSlice = TimeSlice - (uint)TaskGroupOrders[0].orderTimeRequirement;
                            TaskGroupOrders[0].orderTimeRequirement = 0;
                            LoadComponents(TaskGroupOrders[0].pop, TaskGroupOrders[0].secondary, TaskGroupOrders[0].tertiary);
                            CanOrder = Constants.ShipTN.OrderState.AcceptOrders;
                        }
                        else
                        {
                            TaskGroupOrders[0].orderTimeRequirement = TaskGroupOrders[0].orderTimeRequirement - (int)TimeSlice;
                            TimeSlice = 0;
                        }
                        break;
                    #endregion

                    #region Unload Installation
                    /// <summary>
                    /// Unload installation:
                    /// </summary>
                    case (int)Constants.ShipTN.OrderType.UnloadInstallation:
                        if (TaskGroupOrders[0].orderTimeRequirement == -1)
                        {
                            CanOrder = Constants.ShipTN.OrderState.CurrentlyUnloading;
                            int TaskGroupLoadTime = CalcTaskGroupLoadTime(Constants.ShipTN.LoadType.Cargo);
                            int PlanetaryLoadTime = TaskGroupOrders[0].pop.CalculateLoadTime(TaskGroupLoadTime);

                            TaskGroupOrders[0].orderTimeRequirement = PlanetaryLoadTime;
                        }

                        if (TimeSlice > TaskGroupOrders[0].orderTimeRequirement)
                        {
                            TimeSlice = TimeSlice - (uint)TaskGroupOrders[0].orderTimeRequirement;
                            TaskGroupOrders[0].orderTimeRequirement = 0;
                            UnloadCargo(TaskGroupOrders[0].pop, (Installation.InstallationType)TaskGroupOrders[0].secondary, TaskGroupOrders[0].tertiary);
                            CanOrder = Constants.ShipTN.OrderState.AcceptOrders;
                        }
                        else
                        {
                            TaskGroupOrders[0].orderTimeRequirement = TaskGroupOrders[0].orderTimeRequirement - (int)TimeSlice;
                            TimeSlice = 0;
                        }
                        break;
                    #endregion

                    #region Unload Ship Component
                    /// <summary>
                    /// Unload Ship Component:
                    /// </summary>
                    case (int)Constants.ShipTN.OrderType.UnloadShipComponent:
                        if (TaskGroupOrders[0].orderTimeRequirement == -1)
                        {
                            CanOrder = Constants.ShipTN.OrderState.CurrentlyUnloading;
                            int TaskGroupLoadTime = CalcTaskGroupLoadTime(Constants.ShipTN.LoadType.Cargo);
                            int PlanetaryLoadTime = TaskGroupOrders[0].pop.CalculateLoadTime(TaskGroupLoadTime);

                            TaskGroupOrders[0].orderTimeRequirement = PlanetaryLoadTime;
                        }

                        if (TimeSlice > TaskGroupOrders[0].orderTimeRequirement)
                        {
                            TimeSlice = TimeSlice - (uint)TaskGroupOrders[0].orderTimeRequirement;
                            TaskGroupOrders[0].orderTimeRequirement = 0;

                            ComponentDefTN UnloadOrder = null;
                            int TGComponentCount = 0;

                            for (int loop = 0; loop < Ships.Count; loop++)
                            {
                                if (Ships[loop].CargoComponentList.Count != 0)
                                {
                                    foreach (KeyValuePair<ComponentDefTN, CargoListEntryTN> pair in Ships[loop].CargoComponentList)
                                    {
                                        if (TaskGroupOrders[0].secondary == TGComponentCount)
                                        {
                                            UnloadOrder = pair.Key;
                                            TGComponentCount = -1;
                                            break;
                                        }

                                        TGComponentCount++;
                                    }

                                    if (TGComponentCount == -1)
                                        break;
                                }
                            }

                            UnloadComponents(TaskGroupOrders[0].pop, UnloadOrder, TaskGroupOrders[0].tertiary);
                            CanOrder = Constants.ShipTN.OrderState.AcceptOrders;
                        }
                        else
                        {
                            TaskGroupOrders[0].orderTimeRequirement = TaskGroupOrders[0].orderTimeRequirement - (int)TimeSlice;
                            TimeSlice = 0;
                        }

                        break;
                    #endregion

                    #region Load Colonists
                    /// <summary>
                    /// Load Colonists:
                    /// </summary>
                    case (int)Constants.ShipTN.OrderType.LoadColonists:
                        if (TaskGroupOrders[0].orderTimeRequirement == -1)
                        {
                            CanOrder = Constants.ShipTN.OrderState.CurrentlyLoading;
                            int TaskGroupLoadTime = CalcTaskGroupLoadTime(Constants.ShipTN.LoadType.Cryo);
                            int PlanetaryLoadTime = TaskGroupOrders[0].pop.CalculateLoadTime(TaskGroupLoadTime);

                            TaskGroupOrders[0].orderTimeRequirement = PlanetaryLoadTime;
                        }

                        if (TimeSlice > TaskGroupOrders[0].orderTimeRequirement)
                        {
                            TimeSlice = TimeSlice - (uint)TaskGroupOrders[0].orderTimeRequirement;
                            TaskGroupOrders[0].orderTimeRequirement = 0;
                            LoadColonists(TaskGroupOrders[0].pop, TaskGroupOrders[0].tertiary);
                            CanOrder = Constants.ShipTN.OrderState.AcceptOrders;
                        }
                        else
                        {
                            TaskGroupOrders[0].orderTimeRequirement = TaskGroupOrders[0].orderTimeRequirement - (int)TimeSlice;
                            TimeSlice = 0;
                        }
                        break;
                    #endregion

                    #region Unload Colonists
                    /// <summary>
                    /// Unload Colonists:
                    /// </summary>
                    case (int)Constants.ShipTN.OrderType.UnloadColonists:
                        if (TaskGroupOrders[0].orderTimeRequirement == -1)
                        {
                            CanOrder = Constants.ShipTN.OrderState.CurrentlyUnloading;
                            int TaskGroupLoadTime = CalcTaskGroupLoadTime(Constants.ShipTN.LoadType.Cryo);
                            int PlanetaryLoadTime = TaskGroupOrders[0].pop.CalculateLoadTime(TaskGroupLoadTime);

                            TaskGroupOrders[0].orderTimeRequirement = PlanetaryLoadTime;
                        }

                        if (TimeSlice > TaskGroupOrders[0].orderTimeRequirement)
                        {
                            TimeSlice = TimeSlice - (uint)TaskGroupOrders[0].orderTimeRequirement;
                            TaskGroupOrders[0].orderTimeRequirement = 0;
                            UnloadColonists(TaskGroupOrders[0].pop, TaskGroupOrders[0].tertiary);
                            CanOrder = Constants.ShipTN.OrderState.AcceptOrders;
                        }
                        else
                        {
                            TaskGroupOrders[0].orderTimeRequirement = TaskGroupOrders[0].orderTimeRequirement - (int)TimeSlice;
                            TimeSlice = 0;
                        }
                        break;
                    #endregion

                    #region Refuel Target Fleet
                    case (int)Constants.ShipTN.OrderType.RefuelTargetFleet:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        /// <summary>
                        /// A specific tanker list could come in handy here. But this shouldn't be run every tick so it won't be that big an issue.
                        /// </summary>
                        int FuelPlace = 0;
                        for (int loop = 0; loop < Ships.Count; loop++)
                        {

                            if (Ships[loop].ShipClass.IsTanker == true)
                            {
                                float FuelCutoff = Ships[loop].ShipClass.TotalFuelCapacity / 10.0f;
                                float AvailableFuel = Ships[loop].CurrentFuel - FuelCutoff;
                                for (int loop2 = FuelPlace; loop2 < TaskGroupOrders[0].taskGroup.Ships.Count; loop2++)
                                {
                                    if (AvailableFuel <= 0.0f)
                                    {
                                        /// <summary>
                                        /// This tanker is done.
                                        /// </summary>
                                        break;
                                    }

                                    /// <summary>
                                    /// I want refuel target fleet to refuel all ships, even tankers.
                                    /// </summary>
                                    AvailableFuel = TaskGroupOrders[0].taskGroup.Ships[loop2].Refuel(AvailableFuel);
                                    FuelPlace++;
                                }

                                /// <summary>
                                /// If the ship was below the fuel cutoff originally, that will be reflected in available fuel so this should cause no issues.
                                /// </summary>
                                Ships[loop].CurrentFuel = FuelCutoff + AvailableFuel;
                            }
                        }

                        if (FuelPlace != TaskGroupOrders[0].taskGroup.Ships.Count ||
                            TaskGroupOrders[0].taskGroup.Ships.Last().CurrentFuel != TaskGroupOrders[0].taskGroup.Ships.Last().ShipClass.TotalFuelCapacity)
                        {
                            /// <summary>
                            /// Order could not be carried out.
                            /// </summary>
                        }
                        break;
                    #endregion

                    #region Refuel From Own Tankers
                    case (int)Constants.ShipTN.OrderType.RefuelFromOwnTankers:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        FuelPlace = 0;
                        for (int loop = 0; loop < Ships.Count; loop++)
                        {
                            if (Ships[loop].ShipClass.IsTanker == true)
                            {
                                float FuelCutoff = Ships[loop].ShipClass.TotalFuelCapacity / 10.0f;
                                float AvailableFuel = Ships[loop].CurrentFuel - FuelCutoff;

                                for (int loop2 = FuelPlace; loop2 < Ships.Count; loop2++)
                                {
                                    /// <summary>
                                    /// Don't refuel tankers from each other.
                                    /// </summary>
                                    if (Ships[loop2].ShipClass.IsTanker == false)
                                    {
                                        if (AvailableFuel <= 0.0f)
                                        {
                                            /// <summary>
                                            /// This Tanker is finished.
                                            /// </sumamry>
                                            break;
                                        }

                                        AvailableFuel = Ships[loop2].Refuel(AvailableFuel);
                                        FuelPlace++;
                                    }
                                }
                                Ships[loop].CurrentFuel = FuelCutoff + AvailableFuel;
                            }
                        }

                        if (FuelPlace != Ships.Count || (Ships.Last().CurrentFuel != Ships.Last().ShipClass.TotalFuelCapacity && Ships.Last().ShipClass.IsTanker == false))
                        {
                            /// <summary>
                            /// Order could not be carried out.
                            /// </summary>
                        }
                        break;
                    #endregion

                    #region Refuel From Target Fleet
                    case (int)Constants.ShipTN.OrderType.RefuelFromTargetFleet:
                        currentOrder.orderTimeRequirement = 0;
                        FuelPlace = 0;

                        TaskGroupTN targetTG = currentOrder.taskGroup as TaskGroupTN;

                        foreach(ShipTN tankerShip in targetTG.Ships)
                        {
                            if (tankerShip.ShipClass.IsTanker)
                            {
                                float FuelCutoff = tankerShip.ShipClass.TotalFuelCapacity / 10.0f;
                                float AvailableFuel = tankerShip.CurrentFuel - FuelCutoff;

                                foreach (ShipTN myShip in this.Ships)
                                {
                                    if (AvailableFuel <= 0.0f)
                                    {
                                        /// <summary>
                                        /// This Tanker is finished.
                                        /// </summary>
                                        break;
                                    }

                                    AvailableFuel = myShip.Refuel(AvailableFuel);
                                    FuelPlace++;
                                }
                                tankerShip.CurrentFuel = FuelCutoff + AvailableFuel;
                            }
                        }

                        if (FuelPlace != Ships.Count || Ships.Last().CurrentFuel != Ships.Last().ShipClass.TotalFuelCapacity)
                        {
                            /// <summary>
                            /// Order could not be carried out.
                            /// </summary>
                        }
                        break;
                    #endregion

                    #region Unload 90% Of Fuel To SystemBody
                    case (int)Constants.ShipTN.OrderType.UnloadFuelToPlanet:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        for (int loop = 0; loop < Ships.Count; loop++)
                        {

                            if (Ships[loop].ShipClass.IsTanker == true)
                            {
                                float FuelCutoff = Ships[loop].ShipClass.TotalFuelCapacity / 10.0f;
                                float AvailableFuel = Ships[loop].CurrentFuel - FuelCutoff;

                                if (AvailableFuel > 0.0f)
                                {
                                    TaskGroupOrders[0].pop.FuelStockpile = TaskGroupOrders[0].pop.FuelStockpile + AvailableFuel;
                                    Ships[loop].CurrentFuel = Ships[loop].CurrentFuel - AvailableFuel;
                                }
                            }
                        }
                        break;
                    #endregion

                    #region Resupply Target Fleet
                    case (int)Constants.ShipTN.OrderType.ResupplyTargetFleet:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        int SupplyPlace = 0;
                        /// <summary>
                        /// Likewise a Supply ship specific list could come in handy.
                        /// </summary>
                        for (int loop = 0; loop < Ships.Count; loop++)
                        {
                            if (Ships[loop].ShipClass.IsSupply == true)
                            {
                                int MSPCutoff = (Ships[loop].ShipClass.TotalMSPCapacity / 10);
                                int AvailableMSP = Ships[loop].CurrentMSP - MSPCutoff;

                                for (int loop2 = SupplyPlace; loop2 < TaskGroupOrders[0].taskGroup.Ships.Count; loop2++)
                                {
                                    if (AvailableMSP <= 0)
                                    {
                                        ///<summary>
                                        ///This supply ship is finished.
                                        ///</summary>
                                        break;
                                    }
                                    AvailableMSP = TaskGroupOrders[0].taskGroup.Ships[loop2].Resupply(AvailableMSP);
                                    SupplyPlace++;
                                }
                                Ships[loop].CurrentMSP = MSPCutoff + AvailableMSP;
                            }
                        }

                        if (SupplyPlace != TaskGroupOrders[0].taskGroup.Ships.Count ||
                            TaskGroupOrders[0].taskGroup.Ships.Last().CurrentMSP != TaskGroupOrders[0].taskGroup.Ships.Last().ShipClass.TotalMSPCapacity)
                        {
                            /// <summary>
                            /// Order could not be carried out.
                            /// </summary>
                        }
                        break;
                    #endregion

                    #region Resupply From Own Supply Ships
                    case (int)Constants.ShipTN.OrderType.ResupplyFromOwnSupplyShips:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        SupplyPlace = 0;
                        for (int loop = 0; loop < Ships.Count; loop++)
                        {
                            if (Ships[loop].ShipClass.IsSupply == true)
                            {
                                int MSPCutoff = (Ships[loop].ShipClass.TotalMSPCapacity / 10);
                                int AvailableMSP = Ships[loop].CurrentMSP - MSPCutoff;

                                for (int loop2 = SupplyPlace; loop2 < Ships.Count; loop2++)
                                {
                                    /// <summary>
                                    /// Don't resupply supply ships from each other, or juggle as it will henceforth be refered to.
                                    /// </summary>
                                    if (Ships[loop2].ShipClass.IsSupply == false)
                                    {
                                        if (AvailableMSP <= 0)
                                        {
                                            /// <summary>
                                            /// This supply ship is finished.
                                            /// </summary>
                                            break;
                                        }

                                        AvailableMSP = Ships[loop2].Resupply(AvailableMSP);
                                        SupplyPlace++;
                                    }
                                }
                                Ships[loop].CurrentMSP = MSPCutoff + AvailableMSP;
                            }
                        }

                        if (SupplyPlace != Ships.Count || (Ships.Last().CurrentMSP != Ships.Last().ShipClass.TotalMSPCapacity && Ships.Last().ShipClass.IsSupply == false))
                        {
                            /// <summary>
                            /// Order could not be carried out.
                            /// </summary>
                        }
                        break;
                    #endregion

                    #region Resupply From Target Fleet
                    case (int)Constants.ShipTN.OrderType.ResupplyFromTargetFleet:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        SupplyPlace = 0;
                        for (int loop = 0; loop < TaskGroupOrders[0].taskGroup.Ships.Count; loop++)
                        {
                            if (TaskGroupOrders[0].taskGroup.Ships[loop].ShipClass.IsSupply == true)
                            {
                                int MSPCutoff = (TaskGroupOrders[0].taskGroup.Ships[loop].ShipClass.TotalMSPCapacity / 10);
                                int AvailableMSP = TaskGroupOrders[0].taskGroup.Ships[loop].CurrentMSP - MSPCutoff;

                                for (int loop2 = SupplyPlace; loop2 < Ships.Count; loop2++)
                                {
                                    if (AvailableMSP <= 0)
                                    {
                                        /// <summary>
                                        /// This supply ship is done.
                                        /// </summary>
                                        break;
                                    }

                                    AvailableMSP = Ships[loop2].Resupply(AvailableMSP);
                                    SupplyPlace++;
                                }
                                TaskGroupOrders[0].taskGroup.Ships[loop].CurrentMSP = MSPCutoff + AvailableMSP;
                            }
                        }

                        if (SupplyPlace != Ships.Count || Ships.Last().CurrentMSP != Ships.Last().ShipClass.TotalMSPCapacity)
                        {
                            /// <summary>
                            /// Order could not be carried out.
                            /// </summary>
                        }
                        break;
                    #endregion

                    #region Unload 90% Of Supplies To SystemBody
                    case (int)Constants.ShipTN.OrderType.UnloadSuppliesToPlanet:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        for (int loop = 0; loop < Ships.Count; loop++)
                        {
                            if (Ships[loop].ShipClass.IsSupply == true)
                            {
                                int MSPCutoff = (Ships[loop].ShipClass.TotalMSPCapacity / 10);
                                int AvailableMSP = Ships[loop].CurrentMSP - MSPCutoff;

                                if (AvailableMSP > 0)
                                {
                                    TaskGroupOrders[0].pop.MaintenanceSupplies = TaskGroupOrders[0].pop.MaintenanceSupplies + AvailableMSP;
                                    Ships[loop].CurrentMSP = Ships[loop].CurrentMSP - AvailableMSP;
                                }
                            }
                        }
                        break;
                    #endregion

                    #region Load Ordnance From Colony
                    case (int)Constants.ShipTN.OrderType.LoadOrdnanceFromColony:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        for (int loop = 0; loop < Ships.Count; loop++)
                        {
                            if (Ships[loop].ShipMagazines.Count != 0)
                            {
                                Ships[loop].LoadOrdnance(TaskGroupOrders[0].pop);
                            }
                        }
                        break;
                    #endregion

                    #region Unload Ordnance To Colony
                    case (int)Constants.ShipTN.OrderType.UnloadOrdnanceToColony:
                        TaskGroupOrders[0].orderTimeRequirement = 0;
                        for (int loop = 0; loop < Ships.Count; loop++)
                        {
                            if (Ships[loop].ShipMagazines.Count != 0)
                            {
                                Ships[loop].UnloadOrdnance(TaskGroupOrders[0].pop);
                            }
                        }
                        break;
                    #endregion

                    #region Standard Transit
                    case (int)Constants.ShipTN.OrderType.StandardTransit:
                        {
                            TaskGroupOrders[0].orderTimeRequirement = 0;

                            JumpPoint CurrentJP = TaskGroupOrders[0].target as JumpPoint;

                            // Check if we can jump.
                            Dictionary<JumpEngineTN, ShipTN> usedJumpEngines;
                            if (CurrentJP.CanJump(this, true, out usedJumpEngines))
                            {
                                // Handle the jump here.
                                ///< @todo Put jump transition in it's own function within TaskGroup.
                                SystemPosition newPos = CurrentJP.Connect.Position;

                                Position.System.SystemContactList.Remove(Contact);
                                newPos.System.SystemContactList.Add(Contact);

                                Position = newPos;
                                Contact.UpdateLocationAfterTransit();

                                /// <summary>
                                /// Handle PDList stuff.
                                /// </summary>
                                foreach (KeyValuePair<ComponentTN, ShipTN> pair in TaskGroupPDL.PointDefenseFC)
                                {
                                    /// <summary>
                                    /// If these aren't true... whoops.
                                    /// </summary>
                                    if (TaskGroupFaction.PointDefense.ContainsKey(CurrentJP.Position.System) == true)
                                    {
                                        if (TaskGroupFaction.PointDefense[CurrentJP.Position.System].PointDefenseFC.ContainsKey(pair.Key) == true)
                                        {
                                            ShipTN Ship = pair.Value;
                                            ComponentTN Comp = pair.Key;
                                            bool type = TaskGroupFaction.PointDefense[CurrentJP.Position.System].PointDefenseType[pair.Key];
                                            TaskGroupFaction.PointDefense[CurrentJP.Position.System].RemoveComponent(pair.Key);

                                            if (TaskGroupFaction.PointDefense.ContainsKey(CurrentJP.Connect.Position.System) == false)
                                            {
                                                PointDefenseList NewPDL = new PointDefenseList();
                                                TaskGroupFaction.PointDefense.Add(CurrentJP.Connect.Position.System, NewPDL);
                                            }

                                            TaskGroupFaction.PointDefense[CurrentJP.Connect.Position.System].AddComponent(Comp, Ship, type);
                                        }
                                    }
                                }

                                /// <summary>
                                /// No jump gate was used.
                                /// Set the jump engines used to transit.
                                /// Set the ship to undergo jump recharge.
                                /// Set every ship to have jump sickness
                                /// There should be at most one civ and one military in this situation. assuming a mixed group.
                                /// </summary>
                                if (usedJumpEngines != null)
                                {
                                    foreach (KeyValuePair<JumpEngineTN,ShipTN> JumpShipPair in usedJumpEngines)
                                    {
                                        JumpShipPair.Key.Transit();
                                        if (TaskGroupFaction.RechargeList.ContainsKey(JumpShipPair.Value) == true)
                                        {
                                            if((TaskGroupFaction.RechargeList[JumpShipPair.Value] & (int)Faction.RechargeStatus.JumpRecharge) != (int)Faction.RechargeStatus.JumpRecharge)
                                            {
                                                TaskGroupFaction.RechargeList[JumpShipPair.Value] = TaskGroupFaction.RechargeList[JumpShipPair.Value] + (int)Faction.RechargeStatus.JumpRecharge;
                                            }
                                        }
                                        else
                                        {
                                            TaskGroupFaction.RechargeList.Add(JumpShipPair.Value, (int)Faction.RechargeStatus.JumpRecharge);
                                            
                                        }
                                        
                                    }
                                }

                                /// <summary>
                                /// Set every ship as jump sick, this is done even if a gate is used.
                                /// </summary>
                                foreach (ShipTN CurrentShip in Ships)
                                {
                                    CurrentShip.StandardTransit();
                                    if (TaskGroupFaction.RechargeList.ContainsKey(CurrentShip) == true)
                                    {
                                        if ((TaskGroupFaction.RechargeList[CurrentShip] & (int)Faction.RechargeStatus.JumpStandardSickness) != (int)Faction.RechargeStatus.JumpStandardSickness)
                                        {
                                            TaskGroupFaction.RechargeList[CurrentShip] = TaskGroupFaction.RechargeList[CurrentShip] + (int)Faction.RechargeStatus.JumpStandardSickness;
                                        }
                                    }
                                    else
                                    {
                                        TaskGroupFaction.RechargeList.Add(CurrentShip, (int)Faction.RechargeStatus.JumpStandardSickness);

                                    }
                                }
                            }
                        }
                        break;
                    #endregion

                    #region Squadron Transit
                    case (int)Constants.ShipTN.OrderType.SquadronTransit:
                        {
                            TaskGroupOrders[0].orderTimeRequirement = 0;

                            // Check if we can jump.
                            Dictionary<JumpEngineTN, ShipTN> usedJumpEngines;
                            if ((TaskGroupOrders[0].target as JumpPoint).CanJump(this, false, out usedJumpEngines))
                            {
                                // Handle the jump here.
                                ///< @todo Put jump transition in it's own function within TaskGroup.
                                SystemPosition newPos = (TaskGroupOrders[0].target as JumpPoint).Connect.Position;

                                Position.System.SystemContactList.Remove(Contact);
                                newPos.System.SystemContactList.Add(Contact);

                                Position = newPos;
                                Contact.UpdateLocationAfterTransit();

                                /// <summary>
                                /// Handle PDList stuff.
                                /// </summary>
                                JumpPoint CurrentJP = TaskGroupOrders[0].target as JumpPoint;
                                foreach (KeyValuePair<ComponentTN, ShipTN> pair in TaskGroupPDL.PointDefenseFC)
                                {
                                    /// <summary>
                                    /// If these aren't true... whoops.
                                    /// </summary>
                                    if (TaskGroupFaction.PointDefense.ContainsKey(CurrentJP.Position.System) == true)
                                    {
                                        if (TaskGroupFaction.PointDefense[CurrentJP.Position.System].PointDefenseFC.ContainsKey(pair.Key) == true)
                                        {
                                            ShipTN Ship = pair.Value;
                                            ComponentTN Comp = pair.Key;
                                            bool type = TaskGroupFaction.PointDefense[CurrentJP.Position.System].PointDefenseType[pair.Key];
                                            TaskGroupFaction.PointDefense[CurrentJP.Position.System].RemoveComponent(pair.Key);

                                            if (TaskGroupFaction.PointDefense.ContainsKey(CurrentJP.Connect.Position.System) == false)
                                            {
                                                PointDefenseList NewPDL = new PointDefenseList();
                                                TaskGroupFaction.PointDefense.Add(CurrentJP.Connect.Position.System, NewPDL);
                                            }

                                            TaskGroupFaction.PointDefense[CurrentJP.Connect.Position.System].AddComponent(Comp, Ship, type);
                                        }
                                    }
                                }

                                /// <summary>
                                /// if Jump Engine is null here, it means there was a problem.
                                /// Set the jump engines used to transit.
                                /// Set the ship to undergo jump recharge.
                                /// Set every ship to have jump sickness
                                /// There should be at least one civ or one military in this situation. potentially more.
                                /// </summary>
                                int MinJumpRadius = -1;
                                if (usedJumpEngines != null)
                                {
                                    foreach (KeyValuePair<JumpEngineTN, ShipTN> JumpShipPair in usedJumpEngines)
                                    {
                                        if (MinJumpRadius == -1 || JumpShipPair.Key.jumpEngineDef.jumpRadius < MinJumpRadius)
                                        {
                                            MinJumpRadius = JumpShipPair.Key.jumpEngineDef.jumpRadius;
                                        }
                                        JumpShipPair.Key.Transit();
                                        if (TaskGroupFaction.RechargeList.ContainsKey(JumpShipPair.Value) == true)
                                        {
                                            if ((TaskGroupFaction.RechargeList[JumpShipPair.Value] & (int)Faction.RechargeStatus.JumpRecharge) != (int)Faction.RechargeStatus.JumpRecharge)
                                            {
                                                TaskGroupFaction.RechargeList[JumpShipPair.Value] = TaskGroupFaction.RechargeList[JumpShipPair.Value] + (int)Faction.RechargeStatus.JumpRecharge;
                                            }
                                        }
                                        else
                                        {
                                            TaskGroupFaction.RechargeList.Add(JumpShipPair.Value, (int)Faction.RechargeStatus.JumpRecharge);
                                        }

                                    }
                                }
                                else
                                {
                                    String STE = "Squadron transit happened with no jump engines.";
                                    MessageEntry nMsg = new MessageEntry(MessageEntry.MessageType.Error, Position.System, this, GameState.Instance.GameDateTime,
                                                   GameState.Instance.LastTimestep, STE);
                                    TaskGroupFaction.MessageLog.Add(nMsg);
                                }

                                /// <summary>
                                /// Set every ship as jump sick, this is done even if a gate is used.
                                /// </summary>
                                foreach (ShipTN CurrentShip in Ships)
                                {
                                    CurrentShip.SquadronTransit();
                                    if (TaskGroupFaction.RechargeList.ContainsKey(CurrentShip) == true)
                                    {
                                        if ((TaskGroupFaction.RechargeList[CurrentShip] & (int)Faction.RechargeStatus.JumpSquadronSickness) != (int)Faction.RechargeStatus.JumpSquadronSickness)
                                        {
                                            TaskGroupFaction.RechargeList[CurrentShip] = TaskGroupFaction.RechargeList[CurrentShip] + (int)Faction.RechargeStatus.JumpSquadronSickness;
                                        }
                                    }
                                    else
                                    {
                                        TaskGroupFaction.RechargeList.Add(CurrentShip, (int)Faction.RechargeStatus.JumpSquadronSickness);

                                    }
                                }

                                /// <summary>
                                /// MinJumpRadius is the number of raw km this taskgroup should jump by.
                                /// </summary>
                                int degree = GameState.RNG.Next(0, 359);
                                int Jump = GameState.RNG.Next((int)Math.Floor((float)MinJumpRadius * 0.95f), MinJumpRadius);
                                double X = Jump * Math.Cos(degree * Constants.Units.Radian);
                                double Y = Jump * Math.Sin(degree * Constants.Units.Radian);

                                X = Distance.ToAU(X);
                                Y = Distance.ToAU(Y);

                                Position.X = Position.X + X;
                                Position.Y = Position.Y + Y;
                                Contact.UpdateLocationAfterTransit();
                            }
                        }
                        break;
                    #endregion



                }
            }
            return TimeSlice;
        }

        /// <summary>
        /// Clears the current orders for this TG. Make sure to check to see if we are at a planet, if so we are orbiting.
        /// </summary>
        public void clearAllOrders()
        {
            if (TaskGroupOrders.Count > 0)
            {
                if ((TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body || TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Population)
                    && (Contact.Position.X == TaskGroupOrders[0].target.Position.X && Contact.Position.Y == TaskGroupOrders[0].target.Position.Y))
                {
                    IsOrbiting = true;

                    if (TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                    {
                        OrbitingBody = TaskGroupOrders[0].body;
                    }
                    else
                    {
                        OrbitingBody = TaskGroupOrders[0].pop.Planet;
                    }

                    SystemBody OrbitingPlanet = OrbitingBody as SystemBody;

                    if (!OrbitingPlanet.TaskGroupsInOrbit.Contains(this))
                    {
                        OrbitingPlanet.TaskGroupsInOrbit.Add(this);
                    }
                }
            }
            TaskGroupOrders.Clear();
            TimeRequirement = 0;
            TotalOrderDistance = 0;
        }


#warning potentially unused 3 functions here, ContainsTanker,ContainsSupply, ContainsCollier
        /// <summary>
        /// Does this taskgroup have a tanker in it?
        /// </summary>
        /// <returns>is there a tanker true/false</returns>
        bool ContainsTanker()
        {
            for (int loop = 0; loop < Ships.Count; loop++)
            {
                if (Ships[loop].ShipClass.IsTanker == true)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Does this taskgroup have a supply in it?
        /// </summary>
        /// <returns>is there a supply ship true/false</returns>
        bool ContainsSupply()
        {
            for (int loop = 0; loop < Ships.Count; loop++)
            {
                if (Ships[loop].ShipClass.IsSupply == true)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Does this taskgroup have a collier in it?
        /// </summary>
        /// <returns>is there a collier true/false</returns>
        bool ContainsCollier()
        {
            for (int loop = 0; loop < Ships.Count; loop++)
            {
                if (Ships[loop].ShipClass.IsCollier == true)
                    return true;
            }

            return false;
        }

        #endregion


        #region Taskgroup Cargo/Cryo/Component (/Troop Not finished yet) loading and unloading.
        /// <summary>
        /// Load cargo loads a specified installation type from a population, up to the limit in installations if possible.
        /// </summary>
        /// <param name="Pop">Population to load from.</param>
        /// <param name="InstType">installation type to load.</param>
        /// <param name="Limit">Limit in number of facilities to load.</param>
        public void LoadCargo(Population Pop, Installation.InstallationType InstType, int Limit)
        {
            int RemainingTaskGroupTonnage = TotalCargoTonnage - CurrentCargoTonnage;
            int TotalMass = TaskGroupFaction.InstallationTypes[(int)InstType].Mass * Limit;
            int AvailableMass = (int)(Pop.Installations[(int)InstType].Number * (float)TaskGroupFaction.InstallationTypes[(int)InstType].Mass);

            int MassToLoad = 0;

            /// <summary>
            /// In this case load as much as possible up to AvailableMass.
            /// </summary>
            if (Limit == 0)
            {
                MassToLoad = Math.Min(RemainingTaskGroupTonnage, AvailableMass);

            }
            /// <summary>
            /// In this case only load up to Total mass.
            /// </summary>
            else
            {
                MassToLoad = Math.Min(RemainingTaskGroupTonnage, TotalMass);
            }

            /// <summary>
            /// Mark the taskgroup total cargo tonnage
            /// </summary>
            CurrentCargoTonnage = CurrentCargoTonnage + MassToLoad;

            /// <summary>
            /// Decrement the installation count on the planet.
            /// </summary>
            Pop.LoadInstallation(InstType, MassToLoad);

            /// <summary>
            /// Now start loading mass onto each ship.
            /// </summary>
            for (int loop = 0; loop < Ships.Count; loop++)
            {
                int RemainingShipTonnage = Ships[loop].ShipClass.TotalCargoCapacity - Ships[loop].CurrentCargoTonnage;
                if (Ships[loop].ShipClass.TotalCargoCapacity != 0 && RemainingShipTonnage != 0)
                {
                    int ShipMassToLoad = Math.Min(MassToLoad, RemainingShipTonnage);

                    /// <summary>
                    /// Load the mass onto the taskgroup as a whole for display purposes.
                    /// The actual mass will go into the ship cargoholds.
                    /// </summary>
                    if (Ships[loop].CargoList.ContainsKey(InstType))
                    {
                        CargoListEntryTN CLE = Ships[loop].CargoList[InstType];
                        CLE.tons = CLE.tons + ShipMassToLoad;

                    }
                    else
                    {
                        CargoListEntryTN CargoListEntry = new CargoListEntryTN(InstType, ShipMassToLoad);
                        Ships[loop].CargoList.Add(InstType, CargoListEntry);
                    }

                    MassToLoad = MassToLoad - ShipMassToLoad;
                    Ships[loop].CurrentCargoTonnage = ShipMassToLoad;
                }
            }
        }

        /// <summary>
        /// Unload Cargo unloads a specified installation type from the taskgroup to a population. either all installations in this type are unloaded, or merely the limit are unloaded.
        /// </summary>
        /// <param name="Pop">Population to unload to.</param>
        /// <param name="InstType">Installation type.</param>
        /// <param name="Limit">Number of installations to unload.</param>
        public void UnloadCargo(Population Pop, Installation.InstallationType InstType, int Limit)
        {
            int TotalMass = TaskGroupFaction.InstallationTypes[(int)InstType].Mass * Limit;
            for (int loop = 0; loop < Ships.Count; loop++)
            {
                if (Ships[loop].ShipClass.TotalCargoCapacity != 0 && Ships[loop].CurrentCargoTonnage != 0 && Ships[loop].CargoList.ContainsKey(InstType) == true)
                {
                    CargoListEntryTN CLE = Ships[loop].CargoList[InstType];
                    int ShipMassToUnload = 0;

                    if (Limit == 0)
                    {
                        ShipMassToUnload = CLE.tons;
                    }
                    else
                    {
                        ShipMassToUnload = Math.Min(CLE.tons, TotalMass);
                        TotalMass = TotalMass - ShipMassToUnload;
                    }

                    if (ShipMassToUnload == CLE.tons)
                    {
                        Ships[loop].CargoList.Remove(InstType);
                    }

                    CLE.tons = CLE.tons - ShipMassToUnload;
                    CurrentCargoTonnage = CurrentCargoTonnage - ShipMassToUnload;

                    Pop.UnloadInstallation(InstType, ShipMassToUnload);
                }
            }

        }

        /// <summary>
        /// LoadColonists loads the specified population from pop into the taskgroup's cryo bays.
        /// </summary>
        /// <param name="Pop">Population to load from.</param>
        /// <param name="Limit">Limit on colonists who can be put onto taskgroup.</param>
        public void LoadColonists(Population Pop, int Limit)
        {
            for (int loop = 0; loop < Ships.Count; loop++)
            {
                if (Ships[loop].ShipClass.SpareCryoBerths != 0)
                {
                    int Colonists = 0;

                    int RemainingTaskGroupCryo = TotalCryoCapacity - CurrentCryoStorage;
                    int RemainingShipCryo = Ships[loop].ShipClass.SpareCryoBerths - Ships[loop].CurrentCryoStorage;
                    int AvailablePopulation = (int)Math.Floor(Pop.CivilianPopulation * 1000000.0f);

                    if (Limit == 0)
                    {
                        Colonists = Math.Min(RemainingShipCryo, AvailablePopulation);
                    }
                    else
                    {
                        if (Limit > AvailablePopulation)
                            Limit = AvailablePopulation;

                        Colonists = Math.Min(RemainingShipCryo, Limit);
                    }

                    /// <summary>
                    /// Add colonists to taskgroup cryo storage and ship current cryo storage. I'll keep the information duplication for the time being.
                    /// </summary>
                    CurrentCryoStorage = CurrentCryoStorage + Colonists;
                    Ships[loop].CurrentCryoStorage = Ships[loop].CurrentCryoStorage + Colonists;

                    /// <summary>
                    /// Remove civilian population in accordance with the colonists loaded.
                    /// </summary>
                    Pop.CivilianPopulation = Pop.CivilianPopulation - ((float)Colonists / 1000000.0f);
                }
            }
        }

        /// <summary>
        /// Unload colonists transfers colonists from the taskgroup to a population.
        /// </summary>
        /// <param name="Pop">Receiving population.</param>
        /// <param name="Limit">Colonists limit.</param>
        public void UnloadColonists(Population Pop, int Limit)
        {
            if (Limit > CurrentCryoStorage)
                Limit = CurrentCryoStorage;

            for (int loop = 0; loop < Ships.Count; loop++)
            {
                if (Ships[loop].CurrentCryoStorage != 0)
                {
                    int Colonists;

                    if (Limit == 0)
                    {
                        Colonists = Ships[loop].CurrentCryoStorage;
                        CurrentCryoStorage = CurrentCryoStorage - Ships[loop].CurrentCryoStorage;
                        Ships[loop].CurrentCryoStorage = 0;
                    }
                    else
                    {
                        Colonists = Math.Min(Limit, Ships[loop].CurrentCryoStorage);

                        Ships[loop].CurrentCryoStorage = Ships[loop].CurrentCryoStorage - Colonists;
                        CurrentCryoStorage = CurrentCryoStorage - Colonists;
                    }
                    Pop.CivilianPopulation = Pop.CivilianPopulation + ((float)Colonists / 1000000.0f);
                }
            }
        }

        /// <summary>
        /// LoadComponents picks up a set number of components from population pop's component stockpile.
        /// </summary>
        /// <param name="Pop">Population of the component pickup.</param>
        /// <param name="ComponentIndex">location in pop.ComponentStockpile.</param>
        /// <param name="Limit">Number of said components to pick up if not all of them.</param>
        public void LoadComponents(Population Pop, int ComponentIndex, int Limit)
        {
            int RemainingTonnage = TotalCargoTonnage - CurrentCargoTonnage;
            int TotalMass = (int)(Pop.ComponentStockpile[ComponentIndex].size * Constants.ShipTN.TonsPerHS * (float)Limit);
            int AvailableMass = (int)(Pop.ComponentStockpile[ComponentIndex].size * Constants.ShipTN.TonsPerHS * Pop.ComponentStockpileCount[ComponentIndex]);

            int MassToLoad = 0;

            /// <summary>
            /// In this case load as much as possible up to AvailableMass.
            /// </summary>
            if (Limit == 0)
            {
                MassToLoad = Math.Min(RemainingTonnage, AvailableMass);

            }
            /// <summary>
            /// In this case only load up to Total mass.
            /// </summary>
            else
            {
                MassToLoad = Math.Min(RemainingTonnage, TotalMass);
            }

            /// <summary>
            /// All of these may not be loaded.
            float ComponentLoadCount = (float)MassToLoad / (Pop.ComponentStockpile[ComponentIndex].size * Constants.ShipTN.TonsPerHS);

            for (int loop = 0; loop < Ships.Count; loop++)
            {
                if (Ships[loop].ShipClass.TotalCargoCapacity != 0)
                {
                    int RemainingShipTonnage = Ships[loop].ShipClass.TotalCargoCapacity - Ships[loop].CurrentCargoTonnage;
                    int ShipMassToLoad = Math.Min(MassToLoad, RemainingShipTonnage);
                    float ShipComponentLoadCount = (float)ShipMassToLoad / (Pop.ComponentStockpile[ComponentIndex].size * Constants.ShipTN.TonsPerHS);

                    /// <summary>
                    /// Don't break up these components.
                    /// </summary>
                    if (Pop.ComponentStockpile[ComponentIndex].isDivisible == false)
                    {
                        ShipComponentLoadCount = (float)Math.Floor(ShipComponentLoadCount);
                    }


                    /// <summary>
                    /// Tell the population to remove the specified components. ComponentLoadCount may be too high, in which case the lower value will be returned.
                    /// Likewise mass to load will be updated based on this.
                    /// </summary>
                    ShipComponentLoadCount = Pop.TakeComponentsFromStockpile(Pop.ComponentStockpile[ComponentIndex], ShipComponentLoadCount);
                    ShipMassToLoad = (int)(ShipComponentLoadCount * (Pop.ComponentStockpile[ComponentIndex].size * Constants.ShipTN.TonsPerHS));

                    if (Ships[loop].CargoComponentList.ContainsKey(Pop.ComponentStockpile[ComponentIndex]))
                    {
                        CargoListEntryTN CLE = Ships[loop].CargoComponentList[Pop.ComponentStockpile[ComponentIndex]];
                        CLE.tons = CLE.tons + ShipMassToLoad;

                    }
                    else
                    {
                        CargoListEntryTN CargoListEntry = new CargoListEntryTN(Pop.ComponentStockpile[ComponentIndex], ShipMassToLoad);
                        Ships[loop].CargoComponentList.Add(Pop.ComponentStockpile[ComponentIndex], CargoListEntry);
                    }

                    /// <summary>
                    /// CurrentCargoTonnage has to be added here, as non-divisible components may not be loadable.
                    /// </summary>
                    CurrentCargoTonnage = CurrentCargoTonnage + ShipMassToLoad;
                    Ships[loop].CurrentCargoTonnage = Ships[loop].CurrentCargoTonnage + ShipMassToLoad;

                    MassToLoad = MassToLoad - ShipMassToLoad;
                }
            }
        }

        /// <summary>
        /// Unloads the specified component to population pop.
        /// </summary>
        /// <param name="Pop">Population receiving shipment.</param>
        /// <param name="Component">Component to be unloaded. I'm not particularly happy with how this is being done right now.</param>
        /// <param name="Limit">Limit to unloading.</param>
        public void UnloadComponents(Population Pop, ComponentDefTN Component, int Limit)
        {
            int TotalMass = (int)(Component.size * Constants.ShipTN.TonsPerHS * (float)Limit);

            for (int loop = 0; loop < Ships.Count; loop++)
            {
                if (Ships[loop].CurrentCargoTonnage != 0 && Ships[loop].CargoComponentList.ContainsKey(Component) == true)
                {
                    CargoListEntryTN CLE = Ships[0].CargoComponentList[Component];

                    int ShipMassToUnload;
                    /// <summary>
                    /// Limit == 0 means unload all, else unload to limit if limit is lower than total tonnage.
                    /// </summary>
                    if (Limit == 0)
                    {
                        ShipMassToUnload = CLE.tons;
                    }
                    else
                    {
                        ShipMassToUnload = Math.Min(CLE.tons, TotalMass);
                        TotalMass = TotalMass - ShipMassToUnload;
                    }

                    if (ShipMassToUnload == CLE.tons)
                    {
                        Ships[0].CargoComponentList.Remove(Component);
                    }

                    CLE.tons = CLE.tons - ShipMassToUnload;
                    CurrentCargoTonnage = CurrentCargoTonnage - ShipMassToUnload;

                    float ComponentUnloadCount = (float)(ShipMassToUnload / (Component.size * Constants.ShipTN.TonsPerHS));

                    Pop.AddComponentsToStockpile(Component, ComponentUnloadCount);
                }
            }
        }

        /// <summary>
        /// The taskgroup's longest load time member needs to be found, so this function will get the taskgroup side of things.
        /// Another function on Population will handle pop's side of the calculation.
        /// </summary>
        /// <param name="Type">Type of cargo to load.</param>
        /// <returns>Load time in seconds.</returns>
        public int CalcTaskGroupLoadTime(Constants.ShipTN.LoadType Type)
        {
            int MaxLoadTime = 0;
            float LogisticsBonus = 1.0f;
            for (int loop = 0; loop < Ships.Count; loop++)
            {
                if (Ships[loop].ShipCommanded)
                {
                    LogisticsBonus = Ships[loop].ShipCommander.LogisticsBonus;
                }
                else
                {
                    LogisticsBonus = 1.0f;
                }

                int ShipLoadTime = 0;

                switch ((int)Type)
                {
                    case (int)Constants.ShipTN.LoadType.Cargo: ShipLoadTime = (int)((float)Ships[loop].ShipClass.CargoLoadTime / LogisticsBonus);
                        break;
                    case (int)Constants.ShipTN.LoadType.Cryo: ShipLoadTime = (int)((float)Ships[loop].ShipClass.CryoLoadTime / LogisticsBonus);
                        break;
                    case (int)Constants.ShipTN.LoadType.Troop: ShipLoadTime = (int)((float)Ships[loop].ShipClass.TroopLoadTime / LogisticsBonus);
                        break;
                }
                if (ShipLoadTime > MaxLoadTime)
                {
                    MaxLoadTime = ShipLoadTime;
                }
            }

            return MaxLoadTime;
        }

        #endregion


        #region Fire Control targetting
        /// <summary>
        /// Clear all targeting info for this taskgroup
        /// </summary>
        public void clearAllTargets()
        {
            for (int loop = 0; loop < Ships.Count; loop++)
            {
                if (Ships[loop].ShipBFC.Count != 0)
                {
                    for (int loop2 = 0; loop2 < Ships[loop].ShipBFC.Count; loop2++)
                    {
                        Ships[loop].ShipBFC[loop2].clearTarget();
                    }
                }
            }
        }

        /// <summary>
        /// This function returns the closest active contact to this taskgroup. MAY BE DEPRECATED
        /// </summary>
        /// <returns>ShipTN targeted, null if no ship meets criteria.</returns>
        public ShipTN getNewTarget()
        {
            /// <summary>
            /// Target Selection Logic:
            /// pair.key = Ship
            /// pair.value = FactionContact
            /// pair.Value.active = bool for if this ship is detected on actives
            /// How much should proximity matter? find closest?
            /// </summary

            ShipTN min = null;
            float minDist = -1.0f;
            if (TaskGroupFaction.DetectedContactLists.ContainsKey(Contact.Position.System))
            {
                foreach (KeyValuePair<ShipTN, FactionContact> pair in TaskGroupFaction.DetectedContactLists[Contact.Position.System].DetectedContacts)
                {
                    /// <summary>
                    /// Only active targets are considered for this. Is this for BFC targeting, or general things to head for?
                    /// This should be commented out if I want to investigate rather than blow up.
                    /// </summary>
                    if (pair.Value.active == true)
                    {
                        float dist;

                        Contact.DistTable.GetDistance(pair.Key.ShipsTaskGroup.Contact, out dist);

                        if (dist < minDist || min == null)
                        {
                            min = pair.Key;
                            minDist = dist;
                        }
                    }
                }
            }

            return min;
        }
        #endregion


        #region Jump Transit taskgroup functions.
        /// <summary>
        /// This function counts the number of ships of each type in a taskgroup. Yes this can be hardcoded for efficiency on taskgroup formation, but then
        /// must be tracked through taskgroup destruction.
        /// </summary>
        /// <param name="MilCount">Count of military craft</param>
        /// <param name="ComCount">Count of commercial craft</param>
        /// <param name="MilMaxHS">Largest military Ship to transit</param>
        /// <param name="ComMaxHS">Largest commercial Ship to transit</param>
        public void CountShips(out int MilCount, out int ComCount, out float MilMaxHS, out float ComMaxHS)
        {
            MilCount = 0;
            ComCount = 0;
            MilMaxHS = 0.0f;
            ComMaxHS = 0.0f;
            foreach (ShipTN Ship in Ships)
            {
                if (Ship.ShipClass.IsMilitary == true)
                {
                    MilCount++;

                    if (Ship.ShipClass.SizeHS > MilMaxHS)
                        MilMaxHS = Ship.ShipClass.SizeHS;
                }
                else
                {
                    ComCount++;

                    if (Ship.ShipClass.SizeHS > ComMaxHS)
                        ComMaxHS = Ship.ShipClass.SizeHS;
                }
            }
        }

        /// <summary>
        /// Get the Commercial and military jump drive accomodation, and get the list of jump engines used.
        /// </summary>
        /// <param name="MilMaxHS">largest military hull to jump</param>
        /// <param name="MilCount">military craft count</param>
        /// <param name="ComMaxHS">largest commercial hull to jump</param>
        /// <param name="ComCount">commercial craft count</param>
        /// <param name="MilAccom">how many military ships can this taskgroup's jump engines accommodate</param>
        /// <param name="ComAccom">how many commercial ships can this taskgroup's jump engines accommodate</param>
        /// <param name="usedJumpEngines">jumpengines this will use.</param>
        public void GetJDAccom(float MilMaxHS, int MilCount, float ComMaxHS, int ComCount, out int MilAccom, out int ComAccom, out Dictionary<JumpEngineTN,ShipTN> usedJumpEngines)
        {
            MilAccom = 0;
            ComAccom = 0;
            usedJumpEngines = new Dictionary<JumpEngineTN,ShipTN>();

            foreach (ShipTN Ship in Ships)
            {
                if (Ship.ShipJumpEngine.Count != 0)
                {
                    foreach (JumpEngineTN JE in Ship.ShipJumpEngine)
                    {
                        /// <summary>
                        /// Can't use this jump engine.
                        /// </summary>
                        if (JE.CanJump() == false)
                            continue;

                        /// <summary>
                        /// if Ship size is less than max jump rating the max jump rating is shipsize.
                        /// </summary>
                        float JumpCap = 0;
                        if (JE.jumpEngineDef.maxJumpRating > Ship.ShipClass.SizeTons)
                        {
                            JumpCap = Ship.ShipClass.SizeHS;
                        }
                        else
                        {
                            JumpCap = (float)JE.jumpEngineDef.maxJumpRating / Constants.ShipTN.TonsPerHS;
                        }

                        if (JE.jumpEngineDef.isMilitary == true)
                        {
                            /// <summary>
                            /// This jump can accomodate the military craft in this taskgroup
                            if (JumpCap >= MilMaxHS)
                            {
                                /// <summary>
                                /// This many ships can make the jump.
                                /// </summary>
                                MilAccom = MilAccom + JE.jumpEngineDef.squadronSize;
                                usedJumpEngines.Add(JE,Ship);

                            }
                        }
                        else
                        {
                            if (JumpCap >= ComMaxHS)
                            {
                                ComAccom = ComAccom + JE.jumpEngineDef.squadronSize;
                                usedJumpEngines.Add(JE,Ship);
                            }
                        }


                        /// <summary>
                        /// We're done here.
                        /// </summary>
                        if (MilAccom >= MilCount && ComAccom >= ComCount)
                            break;
                    }

                    /// <summary>
                    /// We're done here.
                    /// </summary>
                    if (MilAccom >= MilCount && ComAccom >= ComCount)
                        break;
                }
            }
        }

        /// <summary>
        /// Checks if the TG is jump Sick.
        /// </summary>
        /// <returns>True if the TG is jump sick, false otherwise.</returns>
        public bool IsJumpSick()
        {
            if (Ships != null && Ships.Count > 0)
            {
                return Ships.First().IsJumpSick();
                ///< @todo do we need to check all ships or is just the one fine? Do some ships recover more slowly due to crew grade??
            }

            return false;
        }
        #endregion

    }
    /// <summary>
    /// End TaskGroupTN
    /// </summary>
}
