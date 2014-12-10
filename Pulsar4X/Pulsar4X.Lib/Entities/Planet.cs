using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

#if LOG4NET_ENABLED
using log4net;
#endif

namespace Pulsar4X.Entities
{
    public class Planet : OrbitingEntity
    {
        public enum Tectonics
        {
            Dead,
            Minimal,
            Earthlike,
            Volcanic,
            Count
        }

#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(Planet));
#endif

        public BindingList<Planet> Moons { get; set; } //moons orbiting the planet
        public BindingList<Gas> Gases { get; set; } //gases in atmosphere
        public BindingList<Population> Populations { get; set; } // list of Populations (colonies) on this planet.

        /// <summary>
        /// dictionary listing of which faction has surveyed this planetary body. dictionary might be superfluous, and this could merely be
        /// accomplished by a bindinglist of factions, since only factions present will have surveyed the body.
        /// </summary>
        public Dictionary<Faction, bool> GeoSurveyList { get; set; }

        /// <summary>
        /// Has a geological team surveyed this world?
        /// </summary>
        public bool GeoTeamSurvey { get; set; }

        //TODO: Currently Id is only unique in the star it belongs to, not unique across multiple stars
        public PlanetTypes PlanetType { get; set; }
        public bool IsGasGiant { get; set; }
        public override double Age { get; set; }

        public double AxialTilt { get; set; } //unit of degrees
        public int OrbitZone { get; set; } //the zone of the planet
        public double LengthOfDay { get; set; } //length of local day (hours)
        public bool IsInResonantRotation { get; set; } //tidally locked

        public override double Mass { get { return MassOfDust + MassOfGas; } set { } } //mass (in solar masses)
        [JsonIgnore]
        public double MassInEarthMasses { get { return Mass * Constants.Units.SUN_MASS_IN_EARTH_MASSES; } } //mass (in earth masses)
        public double MassOfDust { get; set; } //mass, ignoring gas
        [JsonIgnore]
        public double MassOfDustInEarthMasses { get { return MassOfDust * Constants.Units.SUN_MASS_IN_EARTH_MASSES; } } //mass (in earth masses)
        public double MassOfGas { get; set; } //mass, ignoring dust
        [JsonIgnore]
        public double MassOfGasInEarthMasses { get { return MassOfGas * Constants.Units.SUN_MASS_IN_EARTH_MASSES; } } //mass (in earth masses)

        public double RadiusOfCore { get; set; } //radius of the rocky core (in km)
        public double Density { get; set; } //density (in g/cc)
        public double SurfaceArea { get; set; }//area in km2
        public double EscapeVelocity { get; set; } //units of cm/sec
        public double SurfaceAcceleration { get; set; } //units of cm/sec2
        public double SurfaceGravity { get; set; } //units of Earth Gravities
        public double RootMeanSquaredVelocity { get; set; } //root mean squared velocity of gas
        public double MolecularWeightRetained { get; set; } //smallest molecular weight retained
        public double VolatileGasInventory { get; set; }
        public double SurfacePressure { get; set; }//units of millibars (mb)
        public bool HasGreenhouseEffect { get; set; } // runaway greenhouse effect
        public double BoilingPoint { get; set; }//boiling point of water(K)
        public double Albedo { get; set; } //albedo of planet
        public double ExoSphericTemperature { get; set; } // degrees of Kelvin
        public double EstimatedTemperature { get; set; } //non iterative estimate  (K)
        public double EstimatedTerrestrialTemperature { get; set; } //non iterative estimate for moons (K)
        public double SurfaceTemperature { get; set; } //surface temp in (K)
        public double RiseInTemperatureDueToGreenhouse { get; set; }
        public double HighTemperature { get; set; } //day time temp
        public double LowTemperature { get; set; } //night time temp
        public double MaxTemperature { get; set; } //summer/day temp
        public double MinTemperature { get; set; } // winter/night temp
        public double HydrosphereCover { get; set; } //percent of surface covered
        public double CloudCover { get; set; } //percent of surface covered
        public double IceCover { get; set; } //percent of surface covered

        /// <summary>
        /// Are any taskgroups orbiting with this body?
        /// </summary>
        public BindingList<TaskGroupTN> TaskGroupsInOrbit { get; set; }

        // I'm not happy with this... This is View Code in the Model, but... easiest way to do it
        [JsonIgnore]
        public string SurfaceTemperatureView
        {
            get
            {
                if (SurfaceTemperature == Constants.Units.INCREDIBLY_LARGE_NUMBER)
                    return "N/A";
                return SurfaceTemperature.ToString("N1");
            }
        }

        [JsonIgnore]
        public string SurfaceGravityView
        {
            get
            {
                if (SurfaceGravity == Constants.Units.INCREDIBLY_LARGE_NUMBER)
                    return "N/A";
                return SurfaceGravity.ToString("N4");
            }
        }

        [JsonIgnore]
        public string MassOfGasInEarthMassesView
        {
            get
            {
                if (MassOfGasInEarthMasses == 0.0)
                    return "None";
                return MassOfGasInEarthMasses.ToString("N4");
            }
        }

        [JsonIgnore]
        public string MassInEarthMassesView
        {
            get
            {
                if (MassInEarthMasses <= 0.0001)
                    return MassInEarthMasses.ToString("#0.0e-0");
                return MassInEarthMasses.ToString("N4");
            }
        }

        [JsonIgnore]
        public string SurfacePressureView
        {
            get
            {
                if (SurfacePressure == 0.0)
                    return "None";
                if (SurfacePressure == Constants.Units.INCREDIBLY_LARGE_NUMBER)
                    return "N/A";
                if (SurfacePressure < 0.0001)
                    return "Trace";
                return SurfacePressure.ToString("N4");
            }
        }

        [JsonIgnore]
        public string PlanetTypeView
        {
            get
            {
                if (IsMoon)
                    return "Moon:" + PlanetType.ToString();
                return PlanetType.ToString();
            }
        }

        [JsonIgnore]
        public double HydrosphereCoverInPercent { get { return HydrosphereCover * 100.0; } set { HydrosphereCover = value / 100.0; } }

        [JsonIgnore]
        public double CloudCoverInPercent { get { return CloudCover * 100.0; } set { CloudCover = value / 100.0; } }

        [JsonIgnore]
        public double IceCoverInPercent { get { return IceCover * 100.0; } set { IceCover = value / 100.0; } }

        /// <summary>
        /// Entry for whether or not this planet has ruins on it.
        /// </summary>
        public Ruins PlanetaryRuins { get; set; }


        /// <summary>
        /// How geologically active is this planet. will this be used?
        /// </summary>
        public Tectonics PlanetaryTectonics { get; set; }

        public Planet(Star primary, OrbitingEntity parent) : base()
        {
            /// <summary>
            /// create these or else anything that relies on a unique global id will break.
            /// </summary>
            Id = Guid.NewGuid();

            Moons = new BindingList<Planet>();
            Gases = new BindingList<Gas>();
            Populations = new BindingList<Population>();

            GeoSurveyList = new Dictionary<Faction, bool>();
            GeoTeamSurvey = false;

            SSEntity = StarSystemEntityType.Body;

            Primary = primary;
            Parent = parent;

            TaskGroupsInOrbit = new BindingList<TaskGroupTN>();

#warning planet generation needs minerals, anomalies, and ruins generation.
            PlanetaryRuins = new Ruins();

            PlanetaryTectonics = Tectonics.Dead;
        }

        /// <summary>
        /// Update the planet's position, Parent positions must be updated in sequence obviously.
        /// </summary>
        /// <param name="tickValue"></param>
        public void UpdatePosition(int tickValue)
        {
            Pulsar4X.Lib.OrbitTable.Instance.UpdatePosition(this, tickValue);

            /// <summary>
            /// Adjust planet position based on the primary. Right now XSystem and YSystem assume orbiting around 0,0. secondary stars, and eventually moons will have this issue.
            /// </summary>
            XSystem = XSystem + Parent.XSystem;
            YSystem = YSystem + Parent.YSystem;

            /// <summary>
            /// Update all the moons.
            /// </summary>
            foreach (Planet CurrentMoon in Moons)
            {
                CurrentMoon.UpdatePosition(tickValue);
            }

            ///<summary>
            ///Update taskgroup positions.
            ///</summary>
            foreach (TaskGroupTN TaskGroup in TaskGroupsInOrbit)
            {
                TaskGroup.Contact.XSystem = XSystem;
                TaskGroup.Contact.YSystem = YSystem;
            }
        }
    }
}
