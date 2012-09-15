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
        //public Star Primary { get; set; }

        //public long XSystem { get; set; }
        //public long YSystem { get; set; }

        //TODO: Currently Id is only unique in the star it belongs to, not unique across multiple stars
        //public Guid Id { get; set; }
        //public string Name { get; set; }
        public PlanetTypes PlanetType { get; set; }
        public bool IsGasGiant { get; set; }
        public bool IsMoon { get; set; }
        public override double Age { get; set; }

        //public double SemiMajorAxis { get; set; } //semi-major axis of solar orbit (in AU)
        //public double Eccentricity { get; set; } //eccentricity of solar orbit
        //public double AxialTilt { get; set; } //unit of degrees
        //public int OrbitZone { get; set; } //the zone of the planet
        //public double OrbitalPeriod { get; set; } //length of local year (in days)
        //public double LengthOfDay { get; set; } //length of local day (hours)
        //public bool IsInResonantRotation { get; set; } //tidally locked

        public override double Mass { get { return MassOfDust + MassOfGas; } set { } } //mass (in solar masses)
        public double MassInEarthMasses { get { return Mass * Constants.Units.SUN_MASS_IN_EARTH_MASSES; } } //mass (in earth masses)
        public double MassOfDust { get; set; } //mass, ignoring gas
        public double MassOfDustInEarthMasses { get { return MassOfDust * Constants.Units.SUN_MASS_IN_EARTH_MASSES; } } //mass (in earth masses)
        public double MassOfGas { get; set; } //mass, ignoring dust
        public double MassOfGasInEarthMasses { get { return MassOfGas * Constants.Units.SUN_MASS_IN_EARTH_MASSES; } } //mass (in earth masses)

        public double RadiusOfCore { get; set; } //radius of the rocky core (in km)
        //public double Radius { get; set; } //equitorial radius (in km)
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

        

        public Planet() : base()
        {
            Moons = new BindingList<Planet>();
            Gases = new BindingList<Gas>();
        }
    }
}
