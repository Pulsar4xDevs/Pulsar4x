using System.Collections.Generic;
using Pulsar4X.Entities;
using System.Linq;
using System;

#if LOG4NET_ENABLED
using log4net;
#endif

namespace Pulsar4X.Stargen
{
    public class ProtoStar : AccreteDisc
    {
        #if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(StarSystemFactory));
        #endif

        public double Luminosity { get { return Star.Luminosity; } set { Star.Luminosity = value; } }
        public override double Mass { get { return Star.Mass; } }
        public double EcoSphereRadius { get { return Star.EcoSphereRadius; } set { Star.EcoSphereRadius = value; } }
        //public double OuterDustLimit { get; set; }
        //public double InnerDustLimit { get; set; }
        //public double PlanetInnerBound { get; set; }
        //public double PlanetOuterBound { get; set; }
        //public bool DustLeft { get; set; }
        public double DustDensityCoeff { get; set; }
        public double CloudEccentricity { get; set; }
        public double DustRatio { get; set; }

        //public List<AccreteBand> Bands { get; set; }
        //public AccreteDisc Disc { get; set; }
        //list of planets that are created from protoplanets in the process of accretion
        //public List<ProtoPlanet> Planets { get; set; }

        public override Star Star { get;  set; }

        public ProtoStar(Star star)
        {
            Star = star;
            //Mass = star.Mass;
            //Luminosity = star.Luminosity;
            //EcoSphereRadius = star.EcoSphereRadius;
            //DustLeft = true;
            CloudEccentricity = 0.2D;
            DustRatio = 1.0D; //TODO: Parameterize this
            DustDensityCoeff = Constants.Stargen.DUST_DENSITY_COEFF * DustRatio;
            
            //PlanetInnerBound = NearestPlanet;
            //PlanetOuterBound = FarthestPlanet;

            initDisc(0.0D, StellarDustLimit, false);
        }

        public double StellarDustLimit
        {
            get
            {
                return (200.0 * Math.Pow(Mass, (1.0 / 3.0)));
            }
        }

    }
}
