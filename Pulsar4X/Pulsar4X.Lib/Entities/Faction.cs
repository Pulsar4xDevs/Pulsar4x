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
    public class Contacts
    {
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

        public Contacts()
        {
            Thermal = new BindingList<int>();
            EM = new BindingList<int>();
            Active = new BindingList<int>();
        }
    }


    public class Faction : GameEntity
    {
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
        public BindingList<Contacts> SystemContacts { get; set; }
        

        public Faction()
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
        }

        public Faction(string a_oName, Species a_oSpecies)
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
    }
}
