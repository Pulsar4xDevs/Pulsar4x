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
        /// Active Sensor Components.
        /// </summary>
        public BindingList<ActiveSensorDefTN> ActiveSensorDef { get; set; }

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
        }
    }
}
