using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public class ComponentDefListTN
    {
        /// <summary>
        /// Crew Quarter components.
        /// </summary>
        public BindingList<GeneralComponentDefTN> CrewQuarters { get; set; }

        /// <summary>
        /// Fuel Tank components.
        /// </summary>
        public BindingList<GeneralComponentDefTN> FuelStorage { get; set; }

        /// <summary>
        /// Engineering bay components.
        /// </summary>
        public BindingList<GeneralComponentDefTN> EngineeringSpaces { get; set; }

        /// <summary>
        /// other specialized general components such as the bridge.
        /// </summary>
        public BindingList<GeneralComponentDefTN> OtherComponents { get; set; }

        /// <summary>
        /// Engine Components.
        /// </summary>
        public BindingList<EngineDefTN> Engines { get; set; }

        /// <summary>
        /// Passive Sensor components.
        /// </summary>
        public BindingList<PassiveSensorDefTN> PassiveSensorDef { get; set; }

        /// <summary>
        /// Default passive sensor suite that all ships must as a minimum possess.
        /// </summary>
        public PassiveSensorDefTN DefaultPassives { get; set; }

        /// <summary>
        /// Active Sensor Components.
        /// </summary>
        public BindingList<ActiveSensorDefTN> ActiveSensorDef { get; set; }

        /// <summary>
        /// Cargo holds,small, regular and perhaps larger variants.
        /// </summary>
        public BindingList<CargoDefTN> CargoHoldDef { get; set; }

        /// <summary>
        /// Cryogenic storage: standard, small, and emergency.
        /// </summary>
        public BindingList<ColonyDefTN> ColonyBayDef { get; set; }

        /// <summary>
        /// Cargo handling systems which decrease load times.
        /// </summary>
        public BindingList<CargoHandlingDefTN> CargoHandleSystemDef { get; set; }



        /// <summary>
        /// ComponentDefList creates lists for every TN component. ClassTN and Faction should eventually both use this, but only faction does right now.
        /// </summary>
        public ComponentDefListTN()
        {
            CrewQuarters = new BindingList<GeneralComponentDefTN>();
            FuelStorage = new BindingList<GeneralComponentDefTN>();
            EngineeringSpaces = new BindingList<GeneralComponentDefTN>();
            OtherComponents = new BindingList<GeneralComponentDefTN>();

            Engines = new BindingList<EngineDefTN>();
            PassiveSensorDef = new BindingList<PassiveSensorDefTN>();
            ActiveSensorDef = new BindingList<ActiveSensorDefTN>();

            CargoHoldDef = new BindingList<CargoDefTN>();
            ColonyBayDef = new BindingList<ColonyDefTN>();
            CargoHandleSystemDef = new BindingList<CargoHandlingDefTN>();



            DefaultPassives = new PassiveSensorDefTN("Default, Don't display this one.", 1.0f, 1, PassiveSensorType.Thermal, 1.0f, 1);
        }

        /// <summary>
        /// Every faction will start with some components defined and ready to use, though the engines and sensors shouldn't be here just yet.
        /// </summary>
        public void AddInitialComponents()
        {
            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, ComponentTypeTN.Crew);
            GeneralComponentDefTN CrewQS = new GeneralComponentDefTN("Crew Quarters - Small", 0.2f, 0, 2.0m, ComponentTypeTN.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, ComponentTypeTN.Fuel);
            GeneralComponentDefTN FuelTS = new GeneralComponentDefTN("Fuel Storage - Small", 0.2f, 0, 3.0m, ComponentTypeTN.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, ComponentTypeTN.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, ComponentTypeTN.Bridge);

            CrewQuarters.Add(CrewQ);
            CrewQuarters.Add(CrewQS);
            FuelStorage.Add(FuelT);
            FuelStorage.Add(FuelTS);
            EngineeringSpaces.Add(EBay);
            OtherComponents.Add(Bridge);

            /// <summary>
            /// These components aren't really basic, but I'll put them in anyway for the time being.
            /// </summary>
            EngineDefTN EngDef = new EngineDefTN("25 EP Nuclear Thermal Engine", 5, 1.0f, 1.0f, 1.0f, 1, 5, -1.0f);
            ActiveSensorDefTN ActDef = new ActiveSensorDefTN("Search 5M - 5000", 1.0f, 10, 5, 100, false, 1.0f, 1);
            PassiveSensorDefTN ThPasDef = new PassiveSensorDefTN("Thermal Sensor TH1-5", 1.0f, 5, PassiveSensorType.Thermal, 1.0f, 1);
            PassiveSensorDefTN EMPasDef = new PassiveSensorDefTN("EM Sensor EM1-5", 1.0f, 5, PassiveSensorType.EM, 1.0f, 1);

            Engines.Add(EngDef);
            ActiveSensorDef.Add(ActDef);
            PassiveSensorDef.Add(ThPasDef);
            PassiveSensorDef.Add(EMPasDef);


            /// <summary>
            /// Everyone starts with cargoholds.
            /// </summary>
            CargoDefTN CargoStandard = new CargoDefTN("Cargo Hold - Standard", 500.0f, 50.0m, 5);
            CargoDefTN CargoSmall = new CargoDefTN("Cargo Hold - Small", 100.0f, 12.5m, 2);

            CargoHoldDef.Add(CargoStandard);
            CargoHoldDef.Add(CargoSmall);

            /// <summary>
            /// Cryostorage is a TN only starting option. otherwise it must be researched.
            /// </summary>
            ColonyDefTN ColonyStandard = new ColonyDefTN("Cryogenic Storage - Standard", 50.0f, 100.0m, 10);
            ColonyDefTN ColonySmall = new ColonyDefTN("Cryogenic Storage - Small", 5.0f, 20.0m, 2);
            ColonyDefTN ColonyEmergency = new ColonyDefTN("Cryogenic Storage - Emergency", 1.0f, 5.0m, 0);

            ColonyBayDef.Add(ColonyStandard);
            ColonyBayDef.Add(ColonySmall);
            ColonyBayDef.Add(ColonyEmergency);


            /// <summary>
            /// Only TN starts begin with this component for now. the improved,advanced, and grav-assisted variants have to be researched.
            /// </summary>
            CargoHandlingDefTN CHS = new CargoHandlingDefTN("Cargo Handling System", 5);
            CargoHandleSystemDef.Add(CHS);
        }
    }
}
