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

        public double CloudDensity
        {
            get
            {
                return Constants.Stargen.DUST_DENSITY_COEFF * Math.Sqrt(Star.Mass) * Math.Exp(-Constants.Stargen.ALPHA * Math.Pow(SemiMajorAxis, (1.0 / Constants.Stargen.N)));
            }
        }

        public ProtoPlanet()
        {
            Planet = new Planet();
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

        public double CriticalLimit
        {
            get
            {
                var perihelionDist = (SemiMajorAxis - SemiMajorAxis * Eccentricity);
                var temp = perihelionDist * Math.Sqrt(Star.Luminosity);
                return (Constants.Stargen.B * Math.Pow(temp, -0.75));
            }
        }

        public double InnerEffectLimit
        {
            get
            {
                return (SemiMajorAxis * (1.0 - Eccentricity) * (1.0 - Mass) / (1.0 + Constants.Stargen.CLOUD_ECCENTRICITY));
            }
        }

        public double OuterEffectLimit
        {
            get
            {
                return (SemiMajorAxis * (1.0 + Eccentricity) * (1.0 + Mass) / (1.0 - Constants.Stargen.CLOUD_ECCENTRICITY));
            }
        }

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
        }

    }
}
