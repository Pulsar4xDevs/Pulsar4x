using System;
using Pulsar4X.Entities;
namespace Pulsar4X.Stargen
{
    public class ProtoPlanet : AccreteDisc
    {
        public override Star Star { get; set; }
        public Planet Planet { get; set; }

        public double SemiMajorAxis { get { return Planet.SemiMajorAxis; } set { Planet.SemiMajorAxis = value; } }
        public double Eccentricity { get { return Planet.Eccentricity; } set { Planet.Eccentricity = value; } }
        public double MassInEarthMasses { get { return Mass * Constants.Units.SUN_MASS_IN_EARTH_MASSES; } } //mass (in earth masses)
        public override double Mass { get { return Planet.MassOfDust + Planet.MassOfGas; } }
        public double MassOfDustInEarthMasses { get { return DustMass * Constants.Units.SUN_MASS_IN_EARTH_MASSES; } } //mass (in earth masses)
        public double DustMass { get { return Planet.MassOfDust; } set { Planet.MassOfDust = value; } }
        public double MassOfGasInEarthMasses { get { return GasMass * Constants.Units.SUN_MASS_IN_EARTH_MASSES; } } //mass (in earth masses)
        public double GasMass { get { return Planet.MassOfGas; } set { Planet.MassOfGas = value; } }
        //public double MassToCollectGas { get; set; }

        public bool IsMoon { get { return Planet.IsMoon; } set { Planet.IsMoon = value; } }
        public ProtoPlanet MoonOf { get; set; }

        public double CloudDensity { get; set; }

        public ProtoPlanet(Star Primary, OrbitingEntity Parent)
        {
            Planet = new Planet(Primary,Parent);
        }

        public void init()
        {
            SetCloudDensity();
            SetCriticalLimit();
            if (IsMoon)
                Planet.Parent = MoonOf.Planet;
            else
                Planet.Parent = Star;
        }

        private void SetCriticalLimit()
        {
            if (IsMoon)
            {
                // Use planet's critical limit instead
                CriticalLimit = MoonOf.CriticalLimit;
            }
            else
            {
                var perihelionDist = (SemiMajorAxis - SemiMajorAxis * Eccentricity);
                var temp = perihelionDist * Math.Sqrt(Star.Luminosity);
                CriticalLimit = (Constants.Stargen.B * Math.Pow(temp, -0.75));
            }
        }

        private void SetCloudDensity()
        {
            if(IsMoon)
                CloudDensity = Constants.Stargen.DUST_DENSITY_COEFF * Math.Sqrt(MoonOf.Mass) * Math.Exp(-Constants.Stargen.ALPHA * Math.Pow(SemiMajorAxis, (1.0 / Constants.Stargen.N)));
            CloudDensity = Constants.Stargen.DUST_DENSITY_COEFF * Math.Sqrt(Star.Mass) * Math.Exp(-Constants.Stargen.ALPHA * Math.Pow(SemiMajorAxis, (1.0 / Constants.Stargen.N)));
        }

        /*
        public ProtoPlanet(double a, double e, double mass)
        {
            SemiMajorAxis = a;
            Eccentricity = e;
            Mass = mass;
            DustMass = 0;
            GasMass = 0.0;
        }
        */

        public double CriticalLimit { get; set; }

        public double InnerEffectLimit { get; set; }

        public double OuterEffectLimit { get; set; }

        /*public double DustDensity
        {
            get
            {
                if (Mass < CriticalLimit) return CloudDensity;
                return (Constants.Stargen.K * CloudDensity) / (1.0 + (Math.Sqrt(CriticalLimit / Mass) * (Constants.Stargen.K - 1.0)));
            }
        }
        public double GasDensity
        {
            get
            {
                if (Mass < CriticalLimit) return 0.0;
                return CloudDensity - DustDensity;
            }
        }*/

        public double ReducedMass { get; protected set; }
        public void ReduceMass()
        {
            if (Mass <= 0.0) ReducedMass = 0.0;
            else
            {
                double temp = Mass / (1.0 + Mass);
                ReducedMass = Math.Pow(temp, (1.0 / 4.0));
            }
            InnerEffectLimit = (SemiMajorAxis * (1.0 - Eccentricity) * (1.0 - ReducedMass) / (1.0 + Constants.Stargen.CLOUD_ECCENTRICITY));
            OuterEffectLimit = (SemiMajorAxis * (1.0 + Eccentricity) * (1.0 + ReducedMass) / (1.0 - Constants.Stargen.CLOUD_ECCENTRICITY));
        }

    }
}
