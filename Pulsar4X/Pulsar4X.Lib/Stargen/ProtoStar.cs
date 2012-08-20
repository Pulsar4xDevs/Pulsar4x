using System.Collections.Generic;
using Pulsar4X.Entities;
using System.Linq;
using System;
using log4net;

namespace Pulsar4X.Stargen
{
    public class ProtoStar
    {
        public static readonly ILog logger = LogManager.GetLogger(typeof(StarSystemFactory));

        public double Luminosity { get { return Star.Luminosity; } set { Star.Luminosity = value; } }
        public double Mass { get { return Star.Mass; } set { Star.Mass = value; } }
        public double EcoSphereRadius { get { return Star.EcoSphereRadius; } set { Star.EcoSphereRadius = value; } }
        public double OuterDustLimit { get; set; }
        public double InnerDustLimit { get; set; }
        public double PlanetInnerBound { get; set; }
        public double PlanetOuterBound { get; set; }
        //public bool DustLeft { get; set; }
        public double DustDensityCoeff { get; set; }
        public double CloudEccentricity { get; set; }
        public double DustRatio { get; set; }

        public List<AccretionBand> Bands { get; set; }
        //list of planets that are created from protoplanets in the process of accretion
        public List<ProtoPlanet> Planets { get; set; }

        public Star Star { get; set; }

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
            
            InnerDustLimit = 0.0D;
            OuterDustLimit = StellarDustLimit;
            PlanetInnerBound = NearestPlanet;
            PlanetOuterBound = FarthestPlanet;

            Planets = new List<ProtoPlanet>();
            Bands = new List<AccretionBand>
                {
                    new AccretionBand { DustPresent = true, GasPresent = true, InnerEdge = InnerDustLimit, OuterEdge = OuterDustLimit }
                };
        }

        public double FarthestPlanet
        {
            get
            {
                return (50.0 * Math.Pow(Luminosity, (1.0 / 3.0)));
            }
        }

        public double NearestPlanet
        {
            get
            {
                return (0.3 * Math.Pow(Luminosity, (1.0 / 3.0)));
            }
        }

        /*public bool DustAvailable(ProtoPlanet p)
        {
            foreach (AccretionBand band in Bands)
            {
                if (band.Intersect(p))
                {
                    if (band.DustPresent)
                        return true;
                }
            }
            return false;
        }*/

        public double StellarDustLimit
        {
            get
            {
                return (200.0 * Math.Pow(Mass, (1.0 / 3.0)));
            }
        }

        public bool DustAvailable
        {
            get
            {
                return IsDustLeftRange(PlanetInnerBound, PlanetOuterBound);
            }
        }

        public bool IsDustAvailable(ProtoPlanet p)
        {
            return IsDustLeftRange(p.InnerEffectLimit, p.OuterEffectLimit);
        }

        public bool GasAvailable
        {
            get
            {
                return IsGasLeftRange(PlanetInnerBound, PlanetOuterBound);
            }
        }

        public bool IsGasAvailable(ProtoPlanet p)
        {
            return IsGasLeftRange(p.InnerEffectLimit, p.OuterEffectLimit);
        }

        public bool IsDustLeftRange(double inner, double outer)
        {
            foreach (AccretionBand band in Bands)
            {
                if (band.OuterEdge <= inner) continue;
                if (band.InnerEdge >= outer) continue;

                if (band.DustPresent) return true;
            }
            return false;
        }

        public bool IsGasLeftRange(double inner, double outer)
        {
            foreach (AccretionBand band in Bands)
            {
                if (band.OuterEdge <= inner) continue;
                if (band.InnerEdge >= outer) continue;

                if (band.GasPresent) return true;
            }
            return false;
        }

        public void UpdateDust(ProtoPlanet p)
        {
            UpdateDust(p.InnerEffectLimit, p.OuterEffectLimit, p.Mass > p.CriticalLimit);
        }

        public void UpdateDust(double inner, double outer, bool gas)
        {
            List<AccretionBand> dustCopy = new List<AccretionBand>(Bands);
            foreach (AccretionBand band in dustCopy)
            {
                if (band.OuterEdge <= inner) continue;
                if (band.InnerEdge >= outer) continue;

                // Situation where band is fully contained
                if (band.InnerEdge >= inner && band.OuterEdge <= outer) band.DustPresent = false;
                else if (band.OuterEdge <= outer && band.InnerEdge < inner)
                {
                    // Upper split
                    AccretionBand newband = new AccretionBand() { DustPresent = false, GasPresent = (band.GasPresent && gas), InnerEdge = inner, OuterEdge = band.OuterEdge };
                    band.OuterEdge = inner;
                    Bands.Insert(Bands.IndexOf(band) + 1, newband);
                }
                else if (band.InnerEdge >= inner && band.OuterEdge > outer)
                {
                    // Lower split
                    AccretionBand newband = new AccretionBand() { DustPresent = false, GasPresent = (band.GasPresent && gas), InnerEdge = band.InnerEdge, OuterEdge = outer };
                    band.InnerEdge = outer;
                    Bands.Insert(Bands.IndexOf(band), newband);
                }
                else
                {
                    // Internal split
                    AccretionBand low_band = new AccretionBand() { DustPresent = band.DustPresent, GasPresent = band.GasPresent, InnerEdge = band.InnerEdge, OuterEdge = inner };
                    AccretionBand high_band = new AccretionBand() { DustPresent = band.DustPresent, GasPresent = band.GasPresent, InnerEdge = outer, OuterEdge = band.OuterEdge };
                    band.DustPresent = false;
                    band.GasPresent = band.GasPresent && gas;
                    band.InnerEdge = inner;
                    band.OuterEdge = outer;
                    Bands.Insert(Bands.IndexOf(band), low_band);
                    Bands.Insert(Bands.IndexOf(band) + 1, high_band);
                }
            }

            LinkedList<AccretionBand> dustList = new LinkedList<AccretionBand>(Bands);
            LinkedListNode<AccretionBand> current = dustList.First;
            LinkedListNode<AccretionBand> next = null;
            while (current != null)
            {
                next = current.Next;
                if (next != null)
                    if ((current.Value.DustPresent == next.Value.DustPresent) && (current.Value.GasPresent == next.Value.GasPresent))
                    {
                        // Merge dustbands
                        current.Value.OuterEdge = next.Value.OuterEdge;
                        dustList.Remove(next);
                        Bands.Remove(next.Value);
                    }
                current = current.Next;
            }
        }

        /*public void CoalescePlanetesimals(ProtoPlanet p)
        {
            Planets.Add(p);
            doCollisions();
        }

        private void doCollisions()
        {
            double miu1, miu2;
            double delta = 1, deltaMin = 0;
            double newE, newA;

            bool collision;
            do
            {
                collision = false;
                Planets = new List<ProtoPlanet>(Planets.OrderBy(x => x.SemiMajorAxis));
                for (int i = 0; i < Planets.Count - 1; i++)
                {
                    ProtoPlanet aPlanet = Planets[i];
                    ProtoPlanet bPlanet = Planets[i + 1];

                    miu1 = aPlanet.Mass / Star.Mass;
                    miu2 = bPlanet.Mass / Star.Mass;

                    deltaMin = 2.4 * (Math.Pow(miu1 + miu2, 1.0 / 3.0));
                    delta = bPlanet.SemiMajorAxis - aPlanet.SemiMajorAxis / aPlanet.SemiMajorAxis;

                    if (delta <= deltaMin)
                    {
                        // New orbital distance
                        newA = (aPlanet.Mass + bPlanet.Mass) / ((aPlanet.Mass / aPlanet.SemiMajorAxis) + (bPlanet.Mass / bPlanet.SemiMajorAxis));

                        logger.Debug(String.Format("Collision between two planetesimals! {0:N4} AU ({1:N5}) + {2:N4} AU ({3:N5}) -> {4:N4} AU", bPlanet.SemiMajorAxis, bPlanet.MassInEarthMasses, aPlanet.SemiMajorAxis, aPlanet.MassInEarthMasses, newA));

                        // Compute new eccentricity
                        double temp = aPlanet.Mass * Math.Sqrt(aPlanet.SemiMajorAxis) * Math.Sqrt(1.0 - Math.Pow(aPlanet.Eccentricity, 2.0));
                        temp = temp + (bPlanet.Mass * Math.Sqrt(bPlanet.SemiMajorAxis) * Math.Sqrt(Math.Sqrt(1.0 - Math.Pow(bPlanet.Eccentricity, 2.0))));
                        temp = temp / ((aPlanet.Mass + bPlanet.Mass) * Math.Sqrt(newA));
                        temp = 1.0 - Math.Pow(temp, 2.0);

                        temp = Math.Min(Math.Max(temp, 0.0), 1.0);

                        newE = Math.Sqrt(temp);

                        // Create a new Protoplanet to accrete additional material
                        var newP = new ProtoPlanet()
                        {
                            SemiMajorAxis = newA,
                            Eccentricity = newE,
                            DustMass = aPlanet.DustMass + bPlanet.DustMass,
                            GasMass = aPlanet.GasMass + bPlanet.GasMass,
                            Star = this
                        };

                        // TODO: Readd!
                        AccreteDust(newP);

                        logger.Debug(string.Format("New planet at {0:N4} AU with mass {1:N5}!", newP.SemiMajorAxis, newP.MassInEarthMasses));

                        Planets.Remove(aPlanet);
                        Planets.Remove(bPlanet);
                        Planets.Add(newP);

                        collision = true;
                        break;
                    }
                }

            }
            while (collision == true);
        }

        private void AccreteDust(ProtoPlanet p)
        {
            double startDustMass = p.DustMass;
            double startGasMass = p.GasMass;
            //double minAccretion = 0.0001 * startMass;

            double gatherLast = 0.0;

            double rInner = 0;
            double rOuter = 0;

            do
            {
                //gatherLast = gatherNow;
                gatherLast = p.Mass;

                p.ReduceMass();
                rInner = p.InnerEffectLimit;
                rOuter = p.OuterEffectLimit;

                p.DustMass = startDustMass;
                p.GasMass = startGasMass;

                foreach (AccretionBand band in Bands)
                {
                    band.CollectDust(rInner, rOuter, p, gatherLast);
                }

            }
            while ((p.Mass - gatherLast) >= (0.0001 * p.Mass));

            UpdateDust(p.InnerEffectLimit, p.OuterEffectLimit, p.Mass < p.CriticalLimit); // Clear dust only on reduced mass?

        }

        public void DistributePlanetaryMasses(Random rnd)
        {
            int counter = 0;
            do
            {
                ProtoPlanet p = new ProtoPlanet()
                {
                    SemiMajorAxis = rnd.NextDouble(PlanetInnerBound, PlanetOuterBound),
                    Eccentricity = rnd.RandomEccentricity(),
                    DustMass = Constants.Stargen.PROTOPLANET_MASS,
                    Star = this
                };

                if (IsDustLeftRange(p.InnerEffectLimit, p.OuterEffectLimit))
                {
                    AccreteDust(p);

                    if (p.Mass != Constants.Stargen.PROTOPLANET_MASS)
                    {
                        CoalescePlanetesimals(p);
                    }
                    else
                    {
                        logger.Debug("Planet at " + p.SemiMajorAxis + " failed due to large neighbor!");
                    }
                }
                counter++;
                if (counter == 10000)
                    logger.Debug("Exceeded 10000 attempts to create a planet! Will continue!");
            } while (DustAvailable && counter < 10000);

        }*/
    }
}
