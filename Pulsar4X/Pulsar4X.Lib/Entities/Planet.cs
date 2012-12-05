using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class Planet : OrbitingEntity
    {
        public BindingList<Planet> Moons { get; set; } //moons orbiting the planet
        public BindingList<Gas> Gases { get; set; } //gases in atmosphere
        public BindingList<Population> Populations { get; set; } // list of Populations (colonies) on this planet.

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

        

        public Planet() : base()
        {
            Moons = new BindingList<Planet>();
            Gases = new BindingList<Gas>();
        }
    }
}
