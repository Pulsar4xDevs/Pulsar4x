using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.Entities
{
    public class FactionSystemDetection
    {
        /// <summary>
        /// Faction that owns this list.
        /// </summary>
        public Faction Faction { get; set; }

        /// <summary>
        /// The system these contacts are stored for.
        /// </summary>
        public StarSystem System { get; set; }

        /// <summary>
        /// Last time this contact index was spotted via thermals.
        /// </summary>
        public BindingList<int> Thermal { get; set; }

        /// <summary>
        /// Last time this contact index was spotted via EM.
        /// </summary>
        public BindingList<int> EM { get; set; }

        /// <summary>
        /// Last time this contact index was spotted via Active.
        /// </summary>
        public BindingList<int> Active { get; set; }

        /// <summary>
        /// Creates a faction contact list for the specified system.
        /// </summary>
        /// <param name="system">System indicated.</param>
        public FactionSystemDetection(Faction Fact,StarSystem system)
        {
            Faction = Fact;
            System = system;

            Thermal = new BindingList<int>();
            EM = new BindingList<int>();
            Active = new BindingList<int>();

            Thermal.RaiseListChangedEvents = false;
            EM.RaiseListChangedEvents = false;
            Active.RaiseListChangedEvents = false;

            for (int loop = 0; loop < system.SystemContactList.Count; loop++)
            {
                Thermal.Add(0);
                EM.Add(0);
                Active.Add(0);
            }

            Thermal.RaiseListChangedEvents = true;
            EM.RaiseListChangedEvents = true;
            Active.RaiseListChangedEvents = true;

            Thermal.ResetBindings();
            EM.ResetBindings();
            Active.ResetBindings();
        }

        /// <summary>
        /// pushes a contact onto the end of the list since that is where all new contacts will be added.
        /// </summary>
        public void AddContact()
        {
            Thermal.Add(0);
            EM.Add(0);
            Active.Add(0);
        }

        /// <summary>
        /// Removes a contact at the specified index.
        /// </summary>
        /// <param name="RemIndex">index to be removed.</param>
        public void RemoveContact(int RemIndex)
        {
            Thermal.RemoveAt(RemIndex);
            EM.RemoveAt(RemIndex);
            Active.RemoveAt(RemIndex);
        }
    }


    public class Faction : GameEntity
    {
        /// <summary>
        /// Factions have to know their own ID. this should be a 0 based index of the factionList where ever that ends up being.
        /// </summary>
        public int FactionID;

        public string Title { get; set; }
        public Species Species { get; set; }

        public FactionTheme FactionTheme { get; set; }
        public FactionCommanderTheme CommanderTheme { get; set; }

        public BindingList<StarSystem> KnownSystems { get; set; }
        public BindingList<TaskForce> TaskForces { get; set; }
        public BindingList<Commander> Commanders { get; set; }
        public BindingList<Population> Populations { get; set; }

        /// <summary>
        /// List of all components the faction has defined.
        /// </summary>
        public ComponentDefListTN ComponentList { get; set; }
        
        /// <summary>
        /// The faction's ship designs are stored here.
        /// </summary>
        public BindingList<ShipClassTN> ShipDesigns { get; set; }

        /// <summary>
        /// Taskgroups for this faction are here.
        /// </summary>
        public BindingList<TaskGroupTN> TaskGroups { get; set; }

        /// <summary>
        /// I'll just store every contact in every system potentially here right now.
        /// </summary>
        public Dictionary<StarSystem,FactionSystemDetection> SystemContacts { get; set; }
        

        public Faction(int ID)
        {
            Name = "Human Federation";
            Species = new Species(); // go with the default Species!

            KnownSystems = new BindingList<StarSystem>();
            TaskForces = new BindingList<TaskForce>();
            Commanders = new BindingList<Commander>();
            Populations = new BindingList<Population>();

            ComponentList = new ComponentDefListTN();
            ShipDesigns = new BindingList<ShipClassTN>();
            TaskGroups = new BindingList<TaskGroupTN>();

            AddInitialComponents();

            SystemContacts = new Dictionary<StarSystem,FactionSystemDetection>();

            FactionID = ID;

        }

        public Faction(string a_oName, Species a_oSpecies, int ID)
        {
            Name = a_oName;
            Species = a_oSpecies;

            KnownSystems = new BindingList<StarSystem>();
            TaskForces = new BindingList<TaskForce>();
            Commanders = new BindingList<Commander>();
            Populations = new BindingList<Population>();

            ComponentList = new ComponentDefListTN();
            ShipDesigns = new BindingList<ShipClassTN>();
            TaskGroups = new BindingList<TaskGroupTN>();

            AddInitialComponents();

            SystemContacts = new Dictionary<StarSystem, FactionSystemDetection>();

            FactionID = ID;

        }

        /// <summary>
        /// Every faction will start with some components defined and ready to use, though the engines and sensors shouldn't be here just yet.
        /// </summary>
        public void AddInitialComponents()
        {
            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, GeneralType.Crew);
            GeneralComponentDefTN CrewQS = new GeneralComponentDefTN("Crew Quarters - Small", 0.2f, 0, 2.0m, GeneralType.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, GeneralType.Fuel);
            GeneralComponentDefTN FuelTS = new GeneralComponentDefTN("Fuel Storage - Small", 0.2f, 0, 3.0m, GeneralType.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, GeneralType.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, GeneralType.Bridge);
            
            ComponentList.CrewQuarters.Add(CrewQ);
            ComponentList.CrewQuarters.Add(CrewQS);
            ComponentList.FuelStorage.Add(FuelT);
            ComponentList.FuelStorage.Add(FuelTS);
            ComponentList.EngineeringSpaces.Add(EBay);
            ComponentList.OtherComponents.Add(Bridge);

            /// <summary>
            /// These components aren't really basic, but I'll put them in anyway.
            /// </summary>
            EngineDefTN EngDef = new EngineDefTN("25 EP Nuclear Thermal Engine", 5, 1.0f, 1.0f, 1.0f, 1, 5, -1.0f);
            ActiveSensorDefTN ActDef = new ActiveSensorDefTN("Search 5M - 5000", 1.0f, 10, 5, 100, false, 1.0f, 1);
            PassiveSensorDefTN ThPasDef = new PassiveSensorDefTN("Thermal Sensor TH1-5", 1.0f, 5, PassiveSensorType.Thermal, 1.0f, 1);
            PassiveSensorDefTN EMPasDef = new PassiveSensorDefTN("EM Sensor EM1-5", 1.0f, 5, PassiveSensorType.EM, 1.0f, 1);

            ComponentList.Engines.Add(EngDef);
            ComponentList.ActiveSensorDef.Add(ActDef);
            ComponentList.PassiveSensorDef.Add(ThPasDef);
            ComponentList.PassiveSensorDef.Add(EMPasDef);
        }

        /// <summary>
        /// Adds a new taskgroup to the taskgroups list at location StartBody in System StartSystem.
        /// </summary>
        /// <param name="Title">Name.</param>
        /// <param name="StartBody">Body with population that built the ship that will be put into the TG.</param>
        /// <param name="StartSystem">System in which the TG starts in.</param>
        public void AddNewTaskGroup(String Title,StarSystemEntity StartBody, StarSystem StartSystem)
        {
            TaskGroupTN TG = new TaskGroupTN(Title, this, StartBody, StartSystem);
            TaskGroups.Add(TG);
        }

        public void AddNewShipDesign(String Title)
        {
            ShipClassTN Ship = new ShipClassTN(Title);
            ShipDesigns.Add(Ship);
        }

        /// <summary>
        /// Adds a list of contacts to the faction.
        /// </summary>
        /// <param name="system">Starsystem for the contacts.</param>
        public void AddNewContactList(StarSystem system)
        {
            FactionSystemDetection NewContact = new FactionSystemDetection(this,system);
            system.FactionDetectionLists.Add(NewContact);
            SystemContacts.Add(system,NewContact);
        
        }

        /// <summary>
        /// Removes a list of contacts from the faction and from the system lists.
        /// </summary>
        /// <param name="ContactList">List to be removed</param>
        public void RemoveContactList(FactionSystemDetection ContactList)
        {
            ContactList.System.FactionDetectionLists.Remove(ContactList);
            SystemContacts.Remove(ContactList.System);
        }


        /// <summary>
        /// Galaxy wide sensor sweep of all systems in which this faction has a presence. Here is where things start to get complicated.
        /// </summary>
        /// <param name="TimeSlice">Current TimeSlice, not sure about the exact units yet.</param>
        public void SensorSweep(int TimeSlice)
        {
            /// <summary>
            /// Loop through all DSTS. ***
            /// </summary>

            /// <summary>
            /// Loop through all faction taskgroups.
            /// </summary>
            for (int loop = 0; loop < TaskGroups.Count; loop++)
            {
                StarSystem System = TaskGroups[loop].Contact.CurrentSystem;

                /// <summary>
                /// Loop through the global contacts list for the system. thermal.Count is equal to SystemContacts.Count. or should be.
                /// </summary>
                for (int loop2 = 0; loop2 < System.FactionDetectionLists[FactionID].Thermal.Count; loop2++)
                {
                    /// <summary>
                    /// I don't own loop2, and it hasn't been fully detected yet.
                    /// </summary>
                    if (this != System.SystemContactList[loop2].faction && System.FactionDetectionLists[FactionID].Thermal[loop2] != TimeSlice &&
                        System.FactionDetectionLists[FactionID].EM[loop2] != TimeSlice && System.FactionDetectionLists[FactionID].Active[loop2] != TimeSlice)
                    {
                        float dist;
                        if (TaskGroups[loop].Contact.DistanceUpdate[loop2] == TimeSlice)
                        {
                            dist = TaskGroups[loop].Contact.DistanceTable[loop2];
                        }
                        else
                        {
                            float distX = (TaskGroups[loop].Contact.SystemKmX - System.SystemContactList[loop2].SystemKmX);
                            float distY = (TaskGroups[loop].Contact.SystemKmY - System.SystemContactList[loop2].SystemKmY);
                            dist = (float)Math.Sqrt((double)((distX * distX) + (distY * distY)));

                            TaskGroups[loop].Contact.DistanceTable[loop2] = dist;
                            TaskGroups[loop].Contact.DistanceUpdate[loop2] = TimeSlice;

                            int TGID = System.SystemContactList.IndexOf(TaskGroups[loop].Contact);

                            System.SystemContactList[loop2].DistanceTable[TGID] = dist;
                            System.SystemContactList[loop2].DistanceUpdate[TGID] = TimeSlice;
                        }


                        

                        /// <summary>
                        /// Now to find the biggest thermal signature in the contact. The biggest for planets is just the planetary pop itself since
                        /// multiple colonies really shouldn't happen.
                        /// </summary>
                        int sig = -1;
                        int detection = -1;

                        /// <summary>
                        /// Handle population detection
                        /// </summary>
                        if (System.SystemContactList[loop2].SSEntity == StarSystemEntityType.Population)
                        {
                            sig = System.SystemContactList[loop2].Pop.ThermalSignature;
                            detection = TaskGroups[loop].BestThermal.pSensorDef.GetPassiveDetectionRange(sig);

                            /// <summary>
                            /// Mark this contact as detected for this time slice via thermal for both the contact, and for the faction as a whole.
                            /// </summary>
                            if (dist < (float)detection)
                            {
                                System.SystemContactList[loop2].Pop.ThermalDetection[FactionID] = TimeSlice;
                                System.FactionDetectionLists[FactionID].Thermal[loop2] = TimeSlice;
                            }

                            sig = System.SystemContactList[loop2].Pop.EMSignature;
                            detection = TaskGroups[loop].BestEM.pSensorDef.GetPassiveDetectionRange(sig);

                            if (dist < (float)detection)
                            {
                                System.SystemContactList[loop2].Pop.EMDetection[FactionID] = TimeSlice;
                                System.FactionDetectionLists[FactionID].EM[loop2] = TimeSlice;
                            }

                            sig = Constants.ShipTN.ResolutionMax - 1;
                            /// <summary>
                            /// The -1 is because a planet is most certainly not a missile.
                            /// </summary>
                            detection = TaskGroups[loop].ActiveSensorQue[ TaskGroups[loop].TaskGroupLookUpST[ sig ]].aSensorDef.GetActiveDetectionRange(sig,-1);

                            if (dist < (float)detection)
                            {
                                System.SystemContactList[loop2].Pop.ActiveDetection[FactionID] = TimeSlice;
                                System.FactionDetectionLists[FactionID].Active[loop2] = TimeSlice;
                            }
                        }
                        else if (System.SystemContactList[loop2].SSEntity == StarSystemEntityType.TaskGroup )
                        {

                            /// <summary>
                            /// Taskgroups have multiple signatures, so noDetection and allDetection become important.
                            /// </summary>
                            
                            bool noDetection = false;
                            bool allDetection = false;

                            #region Ship Thermal Detection Code.

                            if (System.FactionDetectionLists[FactionID].Thermal[loop2] != TimeSlice)
                            {

                                /// <summary>
                                /// Get the best detection range for thermal signatures in loop.
                                /// </summary>
                                int ShipID = System.SystemContactList[loop2].TaskGroup.ThermalSortList.Last();
                                ShipTN scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                sig = scratch.CurrentThermalSignature;
                                detection = TaskGroups[loop].BestThermal.pSensorDef.GetPassiveDetectionRange(sig);



                                /// <summary>
                                /// Good case, none of the ships are detected.
                                /// </summary>
                                if (dist > (float)detection)
                                {
                                    noDetection = true;
                                }

                                /// <summary>
                                /// Atleast the biggest ship is detected.
                                /// </summary
                                if (noDetection == false)
                                {
                                    ShipID = System.SystemContactList[loop2].TaskGroup.ThermalSortList.First();
                                    scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentThermalSignature;
                                    detection = TaskGroups[loop].BestThermal.pSensorDef.GetPassiveDetectionRange(sig);

                                    /// <summary>
                                    /// Best case, everything is detected.
                                    /// </summary>
                                    if (dist < (float)detection)
                                    {
                                        allDetection = true;

                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            System.SystemContactList[loop2].TaskGroup.Ships[loop3].ThermalDetection[FactionID] = TimeSlice;
                                        }
                                        System.FactionDetectionLists[FactionID].Thermal[loop2] = TimeSlice;
                                    }
                                    else if (noDetection == false && allDetection == false)
                                    {
                                        /// <summary>
                                        /// Worst case. some are detected, some aren't.
                                        /// </summary>


                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            LinkedListNode<int> node = System.SystemContactList[loop2].TaskGroup.ThermalSortList.Last;
                                            bool done = false;

                                            while (!done)
                                            {
                                                scratch = System.SystemContactList[loop2].TaskGroup.Ships[node.Value];

                                                if (scratch.ThermalDetection[FactionID] != TimeSlice)
                                                {
                                                    sig = scratch.CurrentThermalSignature;
                                                    detection = TaskGroups[loop].BestThermal.pSensorDef.GetPassiveDetectionRange(sig);

                                                    if (dist <= (float)detection)
                                                    {
                                                        scratch.ThermalDetection[FactionID] = TimeSlice;
                                                    }
                                                    else
                                                    {
                                                        done = true;
                                                        break;
                                                    }
                                                    node = node.Previous;
                                                }
                                            }
                                        }
                                    }
                                    /// <summary>
                                    /// End else
                                    /// </summary>
                                }
                            }
                            #endregion

                            #region Ship EM Detection Code.

                            if (System.FactionDetectionLists[FactionID].EM[loop2] != TimeSlice)
                            {
                                noDetection = false;
                                allDetection = false;

                                /// <summary>
                                /// Get the best detection range for EM signatures in loop.
                                /// </summary>
                                int ShipID = System.SystemContactList[loop2].TaskGroup.EMSortList.Last();
                                ShipTN scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                sig = scratch.CurrentEMSignature;
                                detection = TaskGroups[loop].BestEM.pSensorDef.GetPassiveDetectionRange(sig);


                                /// <summary>
                                /// Good case, none of the ships are detected.
                                /// </summary>
                                if (dist > (float)detection)
                                {
                                    noDetection = true;
                                }

                                /// <summary>
                                /// Atleast the biggest ship is detected.
                                /// </summary
                                if (noDetection == false)
                                {
                                    ShipID = System.SystemContactList[loop2].TaskGroup.EMSortList.First();
                                    scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                    sig = scratch.CurrentEMSignature;
                                    detection = TaskGroups[loop].BestEM.pSensorDef.GetPassiveDetectionRange(sig);

                                    /// <summary>
                                    /// Best case, everything is detected.
                                    /// </summary>
                                    if (dist < (float)detection)
                                    {
                                        allDetection = true;

                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            System.SystemContactList[loop2].TaskGroup.Ships[loop3].EMDetection[FactionID] = TimeSlice;
                                        }
                                        System.FactionDetectionLists[FactionID].EM[loop2] = TimeSlice;
                                    }
                                    else if (noDetection == false && allDetection == false)
                                    {
                                        /// <summary>
                                        /// Worst case. some are detected, some aren't.
                                        /// </summary>


                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            LinkedListNode<int> node = System.SystemContactList[loop2].TaskGroup.EMSortList.Last;
                                            bool done = false;

                                            while (!done)
                                            {
                                                scratch = System.SystemContactList[loop2].TaskGroup.Ships[node.Value];

                                                if (scratch.EMDetection[FactionID] != TimeSlice)
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
                                                        if (System.SystemContactList[loop2].TaskGroup.Ships[node.Previous.Value].EMDetection[FactionID] == TimeSlice)
                                                        {
                                                            System.FactionDetectionLists[FactionID].EM[loop2] = TimeSlice;
                                                        }
                                                        break;
                                                    }

                                                    detection = TaskGroups[loop].BestEM.pSensorDef.GetPassiveDetectionRange(sig);

                                                    if (dist <= (float)detection)
                                                    {
                                                        scratch.EMDetection[FactionID] = TimeSlice;
                                                    }
                                                    else
                                                    {
                                                        done = true;
                                                        break;
                                                    }
                                                    node = node.Previous;
                                                }
                                            }
                                        }
                                    }
                                    /// <summary>
                                    /// End else
                                    /// </summary>
                                }
                            }
                            #endregion

                            #region Ship Active Detection Code.

                            if (System.FactionDetectionLists[FactionID].Active[loop2] != TimeSlice && TaskGroups[loop].ActiveSensorQue.Count > 0)
                            {
                                noDetection = false;
                                allDetection = false;

                                /// <summary>
                                /// Get the best detection range for thermal signatures in loop.
                                /// </summary>
                                int ShipID = System.SystemContactList[loop2].TaskGroup.ActiveSortList.Last();
                                ShipTN scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                sig = scratch.TotalCrossSection - 1;

                                if (sig > Constants.ShipTN.ResolutionMax - 1)
                                    sig = Constants.ShipTN.ResolutionMax - 1;

                                detection = TaskGroups[loop].ActiveSensorQue[ TaskGroups[loop].TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig,-1);

                                /// <summary>
                                /// Good case, none of the ships are detected.
                                /// </summary>
                                if (dist > (float)detection)
                                {
                                    noDetection = true;
                                }

                                /// <summary>
                                /// Atleast the biggest ship is detected.
                                /// </summary
                                if (noDetection == false)
                                {
                                    ShipID = System.SystemContactList[loop2].TaskGroup.ActiveSortList.First();
                                    scratch = System.SystemContactList[loop2].TaskGroup.Ships[ShipID];
                                    sig = scratch.TotalCrossSection - 1;

                                    if (sig > Constants.ShipTN.ResolutionMax - 1)
                                        sig = Constants.ShipTN.ResolutionMax - 1;

                                    detection = TaskGroups[loop].ActiveSensorQue[TaskGroups[loop].TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                                    /// <summary>
                                    /// Best case, everything is detected.
                                    /// </summary>
                                    if (dist < (float)detection)
                                    {
                                        allDetection = true;

                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            System.SystemContactList[loop2].TaskGroup.Ships[loop3].ActiveDetection[FactionID] = TimeSlice;
                                        }
                                        System.FactionDetectionLists[FactionID].Active[loop2] = TimeSlice;
                                    }
                                    else if (noDetection == false && allDetection == false)
                                    {
                                        /// <summary>
                                        /// Worst case. some are detected, some aren't.
                                        /// </summary>


                                        for (int loop3 = 0; loop3 < System.SystemContactList[loop2].TaskGroup.Ships.Count; loop3++)
                                        {
                                            LinkedListNode<int> node = System.SystemContactList[loop2].TaskGroup.ActiveSortList.Last;
                                            bool done = false;

                                            while (!done)
                                            {
                                                scratch = System.SystemContactList[loop2].TaskGroup.Ships[node.Value];

                                                if (scratch.ActiveDetection[FactionID] != TimeSlice)
                                                {
                                                    sig = scratch.TotalCrossSection - 1;

                                                    if (sig > Constants.ShipTN.ResolutionMax - 1)
                                                        sig = Constants.ShipTN.ResolutionMax - 1;

                                                    detection = TaskGroups[loop].ActiveSensorQue[TaskGroups[loop].TaskGroupLookUpST[sig]].aSensorDef.GetActiveDetectionRange(sig, -1);

                                                    if (dist <= (float)detection)
                                                    {
                                                        scratch.ActiveDetection[FactionID] = TimeSlice;
                                                    }
                                                    else
                                                    {
                                                        done = true;
                                                        break;
                                                    }
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
                            #endregion
                        }
                        /// <summary>
                        /// End if planet or Task Group
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
            /// <summary>
            /// End for Faction TaskGroups.
            /// </summary>
        }
    }
}
