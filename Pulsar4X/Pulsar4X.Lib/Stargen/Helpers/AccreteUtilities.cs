using System;

namespace Pulsar4X.Stargen
{
    public static class AccreteUtilities
    {
        /// <summary>
        /// Orbital radius is in AU, eccentricity is unitless, and the stellar	
        /// luminosity ratio is with respect to the sun.  The value returned is the 
        /// mass at which the planet begins to accrete gas as well as dust, and is	
        /// in units of solar masses.												
        /// </summary>
        /// <param name="orbRadius"> </param>
        /// <param name="eccentricity"></param>
        /// <param name="stellLuminosityRatio"></param>
        /// <returns></returns>
        public static double CriticalLimit(double orbRadius, double eccentricity, double stellLuminosityRatio)
        {
            var perihelionDist = (orbRadius - orbRadius * eccentricity);
            var temp = perihelionDist * Math.Sqrt(stellLuminosityRatio);
            return (Constants.Stargen.B * Math.Pow(temp, -0.75));
        }

        /// <summary>
        /// Determines if dust is available in the orbit the planetoid is sweeping
        /// </summary>
        /// <returns></returns>
        public static bool DustAvailable(ProtoStar protoStar, ProtoPlanet protoPlanet)
        {
            var dustHere = false;
            var innerEffectLimit = InnerEffectLimit(protoPlanet.SemiMajorAxis, protoPlanet.Eccentricity, protoPlanet.Mass, protoStar.CloudEccentricity);
            var outerEffectLimit = OuterEffectLimit(protoPlanet.SemiMajorAxis, protoPlanet.Eccentricity, protoPlanet.Mass, protoStar.CloudEccentricity);
            var dustIndex = protoStar.Bands.FindIndex(x => x.OuterEdge >= innerEffectLimit);
            if (dustIndex != -1)
            {
                dustHere = protoStar.Bands[dustIndex].DustPresent;
                if (dustHere) return true;

                for (dustIndex = dustIndex + 1; dustIndex < protoStar.Bands.Count; dustIndex++)
                {
                    if (protoStar.Bands[dustIndex].InnerEdge >= outerEffectLimit)
                        break;

                    dustHere = dustHere || protoStar.Bands[dustIndex].DustPresent;
                }
            }
            return dustHere;
        }

        public static double RandomEccentricity()
        {
            return 1.0 - Math.Pow(MathUtilities.Random.NextDouble(0.0, 1.0), Constants.Units.ECCENTRICITY_COEFF);
        }

        /// <summary>
        /// Calculates the inner distance from the planetoid's orbit that will be swept free of dust and gas
        /// during accretion.
        /// </summary>
        /// <param name="a">Semimajor axis of the planetoid's orbit in AU</param>
        /// <param name="e">Eccentricity of the planetoid's orbit</param>
        /// <param name="mass">Mass of the planetoid</param>
        /// <param name="cloudEccentricity"> </param>
        /// <returns></returns>
        public static double InnerEffectLimit(double a, double e, double mass, double cloudEccentricity)
        {
            return (a * (1.0 - e) * (1.0 - mass) / (1.0 + cloudEccentricity));
        }

        /// <summary>
        /// Calculates the outer distance from the planetoid's orbit that will be swept free of dust and gas
        /// during accretion.
        /// </summary>
        /// <param name="a">Semimajor axis of the planetoid's orbit in AU</param>
        /// <param name="e">Eccentricity of the planetoid's orbit</param>
        /// <param name="mass">Mass of the planetoid</param>
        /// <param name="cloudEccentricity"> </param>
        /// <returns></returns>
        public static double OuterEffectLimit(double a, double e, double mass, double cloudEccentricity)
        {
            return (a * (1.0 + e) * (1.0 + mass) / (1.0 - cloudEccentricity));
        }

        /// <summary>
        /// Use the swept disk of the planetoid to break up the overall accretion disk into
        /// multiple disks and then collapse any disks that are adjacent and identical in contents.
        /// </summary>
        /// <param name="protoStar"> </param>
        /// <param name="protoPlanet"> </param>
        /// <param name="previousMass"> </param>
        /// <param name="critMass">The mass at which a planetoid collects gas</param>
        public static void UpdateDustLanes(ProtoStar protoStar, ProtoPlanet protoPlanet, double previousMass, double critMass)
        {
            protoStar.DustLeft = false;
            var canCollectGas = !(protoPlanet.Mass > critMass);
            var reducedMass = Math.Pow(previousMass/(1.0 + previousMass), (1.0/4.0));
            var min = InnerEffectLimit(protoPlanet.SemiMajorAxis, protoPlanet.Eccentricity, reducedMass, protoStar.CloudEccentricity);
            var max = OuterEffectLimit(protoPlanet.SemiMajorAxis, protoPlanet.Eccentricity, reducedMass, protoStar.CloudEccentricity);
            AccretionBand band;
            var i = 0;
            while (i < protoStar.Bands.Count)
            {
                band = protoStar.Bands[i];
                if (band.InnerEdge < min && band.OuterEdge > max)
                {
                    //the swept band is in the middle of this band
                    //split this band up into 2 additional bands
                    var innerBand = new AccretionBand { InnerEdge = min, OuterEdge = max, GasPresent = band.GasPresent && canCollectGas, DustPresent = false };
                    var outerBand = new AccretionBand { InnerEdge = max, OuterEdge = band.OuterEdge, GasPresent = band.GasPresent, DustPresent = band.DustPresent };
                    band.OuterEdge = min;

                    protoStar.Bands.Insert(i + 1, innerBand);
                    protoStar.Bands.Insert(i + 2, outerBand);
                    i += 3;
                }
                else if (band.InnerEdge < max && band.OuterEdge > max)
                {
                    //swept band overlaps the current band to the inside
                    var outerBand = new AccretionBand { InnerEdge = max, OuterEdge = band.OuterEdge, GasPresent = band.GasPresent, DustPresent = band.DustPresent };
                    band.OuterEdge = max;
                    band.GasPresent = band.GasPresent && canCollectGas;
                    band.DustPresent = false;
                    protoStar.Bands.Insert(i + 1, outerBand);
                    i += 2;
                }
                else if (band.InnerEdge < min && band.OuterEdge > min)
                {
                    var outerBand = new AccretionBand { InnerEdge = min, OuterEdge = band.OuterEdge, GasPresent = band.GasPresent && canCollectGas, DustPresent = false };
                    band.OuterEdge = min;
                    protoStar.Bands.Insert(i + 1, outerBand);
                    i += 2;
                }
                else if (band.InnerEdge >= min && band.OuterEdge <= max)
                {
                    if (band.GasPresent)
                        band.GasPresent = canCollectGas;

                    band.DustPresent = false;
                    i += 1;
                }
                else if (band.OuterEdge < min || band.InnerEdge > max)
                {
                    i += 1;
                }
                else
                {
                    i += 1;
                }
            }

            i = 0;
            while (i < protoStar.Bands.Count)
            {
                band = protoStar.Bands[i];
                if (band.DustPresent && (band.OuterEdge >= protoStar.PlanetInnerBound) && (band.InnerEdge <= protoStar.PlanetOuterBound))
                {
                    protoStar.DustLeft = true;
                }
                if (i + 1 < protoStar.Bands.Count)
                {
                    AccretionBand nextBand = protoStar.Bands[i + 1];
                    if (band.DustPresent == nextBand.DustPresent && band.GasPresent == nextBand.GasPresent)
                    {
                        band.OuterEdge = nextBand.OuterEdge;
                        protoStar.Bands.RemoveAt(i + 1);
                    }
                }
                i += 1;
            }
        }

        /// <summary>
        /// Calculate the luminosity of a star based on its mass
        /// </summary>
        /// <param name="massRatio">The mass of the star in Solar Masses</param>
        /// <returns></returns>
        public static double Luminosity(double massRatio)
        {
            double n;
            if (massRatio < 1.0)
                n = 1.75 * (massRatio - 0.1) + 3.325;
            else
                n = 0.5 * (2.0 - massRatio) + 4.4;

            return Math.Pow(massRatio, n);
        }

        public static double EcoSphereRadius(double luminosity)
        {
            return Math.Sqrt(luminosity);
        }

        public static double StellarLife(double mass, double luminosity)
        {
            var life = 1.0E10 * (mass / luminosity);
            return life;
        }

        /// <summary>
        /// This function, given the orbital radius of a planet in AU, returns
        ///	 the orbital 'zone' of the particle.
        /// </summary>
        /// <param name="luminosity"></param>
        /// <param name="orbitalRadius"></param>
        /// <returns></returns>
        public static int OrbitalZone(double luminosity, double orbitalRadius)
        {
            if (orbitalRadius < (4.0 * Math.Sqrt(luminosity)))
                return 1;
            if (orbitalRadius < (15.0 * Math.Sqrt(luminosity)))
                return 2;
            return 3;
        }

        public static double DustDensity(ProtoStar protoStar, ProtoPlanet protoPlanet)
        {
            return protoStar.DustDensityCoeff * Math.Sqrt(protoStar.Mass)
                    * Math.Exp(-Constants.Stargen.ALPHA * Math.Pow(protoPlanet.SemiMajorAxis, (1.0 / Constants.Stargen.N)));
        }

        /// <summary>
        /// Returns the furthest distance that the accretion disk extends based on the mass of the star.
        /// </summary>
        /// <returns></returns>
        public static double StellarDustLimit(double mass)
        {
            return (200.0 * Math.Pow(mass, (1.0 / 3.0)));
        }

        /// <summary>
        /// Nearest distance at which a planet could form for this star.
        /// Distance returned is in AU
        /// </summary>
        /// <param name="stellMassRatio">Mass of the star in Solar Masses</param>
        /// <returns></returns>
        public static double NearestPlanet(double stellMassRatio)
        {
            return (0.3 * Math.Pow(stellMassRatio, (1.0 / 3.0)));
        }

        /// <summary>
        /// Furthest distance a planet can form in the disk, based on the mass of the star.
        /// Returned distance is in AU.
        /// </summary>
        /// <param name="stellMassRatio">Mass of the star in Solar Masses.</param>
        /// <returns></returns>
        public static double FarthestPlanet(double stellMassRatio)
        {
            return (50.0 * Math.Pow(stellMassRatio, (1.0 / 3.0)));
        }
    }
}
