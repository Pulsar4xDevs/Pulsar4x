using System;
using Pulsar4X.Entities;
using log4net;


namespace Pulsar4X.Stargen
{
    public class StarSystemFactory
    {
        private const double MinAge = 1.0E9;
        private const double MaxAge = 6.0E9;

        public static readonly ILog logger = LogManager.GetLogger(typeof(StarSystemFactory));

        private readonly double _minimumStellarAge;
        private readonly double _maximumStellarAge;
        private readonly bool _generateMoons;

        public StarSystemFactory(bool genMoons) : this(MinAge, MaxAge, genMoons)
        {
        }

        public StarSystemFactory(double minStellarAge, double maxStellarAge,  bool genMoons)
        {
            _minimumStellarAge = minStellarAge;
            _maximumStellarAge = maxStellarAge;
            _generateMoons = genMoons;
        }
        
        public StarSystem Create(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty.");

            var accrete = new Accrete(_minimumStellarAge, _maximumStellarAge,  _generateMoons);
            var ss = accrete.Create(name);

            if (logger.IsDebugEnabled)
            {
                logger.Debug("Star System Generated!");
                for(int i = 0; i<ss.Stars.Count; i++)
                {
                    Star star = ss.Stars[i];
                    logger.Debug(string.Format("Star {0}: Mass {1:N4}, Luminosity {2:N4}, Spectrum {3}{4}, {5} planets", i, star.Mass, star.Luminosity, star.Spectrum, star.SpectrumAdjustment, star.Planets.Count));
                    for (int j = 0; j < star.Planets.Count; j++)
                    {
                        Planet p = star.Planets[j];
                        if(j==0)
                            logger.Debug(string.Format("P{0,-6}{1,9}AU {2,10}{3,10}{4,12}{5,5}", "#", "Orbit", "Mass", "GasMass", "PlanetType", "Moons"));
                        logger.Debug(string.Format("P{0,-6}{1,9:N4}AU {2,10:N4}{3,10}{4,12}{5,5}", j, p.SemiMajorAxis, p.MassInEarthMasses, p.MassOfGasInEarthMasses > 0 ? p.MassOfGasInEarthMasses.ToString("N4") : "None", p.PlanetType, p.Moons.Count));
                        for(int k=0; k<p.Moons.Count; k++)
                        {
                            Planet m = p.Moons[k];
                            logger.Debug(string.Format("P{0,-6}{1,9:N4}AU {2,10:N4}{3,10}{4,12}", j+"-"+k, m.SemiMajorAxis, m.MassInEarthMasses, m.MassOfGasInEarthMasses > 0 ? m.MassOfGasInEarthMasses.ToString("N4") : "None", m.PlanetType));
                        }
                    }
                }
            }

            return ss;
        }
    }
}
