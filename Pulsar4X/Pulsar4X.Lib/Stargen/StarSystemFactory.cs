using System;
using Pulsar4X.Entities;

#if LOG4NET_ENABLED
using log4net;
#endif


namespace Pulsar4X.Stargen
{
    public class StarSystemFactory
    {
        private const double MinAge = 1.0E9;
        private const double MaxAge = 6.0E9;

#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(StarSystemFactory));
#endif

        private readonly double _minimumStellarAge;
        private readonly double _maximumStellarAge;
        private readonly bool _generateMoons;

        public StarSystemFactory(bool genMoons = true)
            : this(MinAge, MaxAge, genMoons)
        {
        }

        public StarSystemFactory(double minStellarAge, double maxStellarAge, bool genMoons)
        {
            _minimumStellarAge = minStellarAge;
            _maximumStellarAge = maxStellarAge;
            _generateMoons = genMoons;
        }

        public StarSystem Create(string name, int seed = -1)
        {
            Random rnd;
            if (seed > -1)
                rnd = new Random(seed);
            else
                rnd = new Random();

            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty.");

            var accrete = new Accrete(_minimumStellarAge, _maximumStellarAge, _generateMoons, rnd);
            var ss = accrete.Create(name);

            // save seed to star system:
            ss.Seed = seed;

            /// <summary>
            /// redundancy:
            /// </summary>
#if LOG4NET_ENABLED
            if (logger.IsDebugEnabled)
            {
                /*
                logger.Debug("Star System Generated!");
                for(int i = 0; i<ss.Stars.Count; i++)
                {
                    Star star = ss.Stars[i];
                    logger.Debug(string.Format("Star {0}: Mass {1:N4}, Luminosity {2:N4}, Spectrum {3}{4}, {5} planets", i, star.Mass, star.Luminosity, star.Spectrum, star.SpectrumAdjustment, star.Planets.Count));
                    for (int j = 0; j < star.Planets.Count; j++)
                    {
                        Planet p = star.Planets[j];
                        if(j==0)
                            logger.Debug(string.Format("P{0,-6}{1,9}AU {2,10}{3,10}{4,12}{5,6}", "#", "Orbit", "Mass", "GasMass", "Type", "Moons"));
                        logger.Debug(string.Format("P{0,-6}{1,9:N4}AU {2,10:N4}{3,10}{4,12}{5,6}", j, p.SemiMajorAxis, p.MassInEarthMasses, p.MassOfGasInEarthMasses > 0 ? p.MassOfGasInEarthMasses.ToString("N4") : "None", p.PlanetType, p.Moons.Count));
                        for(int k=0; k<p.Moons.Count; k++)
                        {
                            Planet m = p.Moons[k];
                            logger.Debug(string.Format("P{0,-6}{1,9:N4}AU {2,10:N4}{3,10}{4,12}", j+"-"+k, m.SemiMajorAxis, m.MassInEarthMasses, m.MassOfGasInEarthMasses > 0 ? m.MassOfGasInEarthMasses.ToString("N4") : "None", m.PlanetType));
                        }
                    }
                }
                */
            }
#endif

            // Create the faciton contact information for each faction.
            foreach (Faction f in GameState.Instance.Factions)
            {
                f.AddNewContactList(ss);
            }
            return ss;
        }
    }
}
