using System.Collections.Generic;
using Pulsar4X.Entities;

namespace Pulsar4X.Stargen
{
    public class ProtoStar
    {
        public double Luminosity { get; set; }
        public double Mass { get; set; }
        public double EcoSphereRadius { get; set; }
        public double OuterDustLimit { get; set; }
        public double InnerDustLimit { get; set; }
        public double PlanetInnerBound { get; set; }
        public double PlanetOuterBound { get; set; }
        public bool DustLeft { get; set; }
        public double DustDensityCoeff { get; set; }
        public double CloudEccentricity { get; set; }
        public double DustRatio { get; set; }

        public List<AccretionBand> Bands { get; set; }
        //list of planets that are created from protoplanets in the process of accretion
        public List<Planet> Planets { get; set; }

        public Star Star { get; set; }

        public ProtoStar(Star star)
        {
            Star = star;
            Mass = star.Mass;
            Luminosity = star.Luminosity;
            EcoSphereRadius = star.EcoSphereRadius;
            DustLeft = true;
            CloudEccentricity = 0.2D;
            DustRatio = 1.0D; //TODO: Parameterize this
            DustDensityCoeff = Constants.Stargen.DUST_DENSITY_COEFF * DustRatio;
            
            InnerDustLimit = 0.0D;
            OuterDustLimit = AccreteUtilities.StellarDustLimit(Mass);
            PlanetInnerBound = AccreteUtilities.NearestPlanet(Mass);
            PlanetOuterBound = AccreteUtilities.FarthestPlanet(Mass);

            Planets = new List<Planet>();
            Bands = new List<AccretionBand>
                {
                    new AccretionBand { DustPresent = true, GasPresent = true, InnerEdge = InnerDustLimit, OuterEdge = OuterDustLimit }
                };
        }
    }
}
