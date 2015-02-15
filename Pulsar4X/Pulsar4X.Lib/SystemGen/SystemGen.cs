using Pulsar4X.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X
{
    public static class SystemGen
    {
        private static Random m_SeedRNG = new Random();

        public static StarSystem CreateSystem(string name)
        {
            return CreateSystem(name, m_SeedRNG.Next());
        }

        public static StarSystem CreateSystem(string name, int seed)
        {
            Random RNG = new Random(seed);

            StarSystem newSystem = new StarSystem(name, seed);

            int noOfStars = RNG.Next(1, 5);
            for (int i = 0; i < noOfStars; ++i)
            {
                // Generate star quick and dirty:
                double radius = 0.00465475877 * 0.1 + RNG.NextDouble() * (0.00465475877 * 250 - 0.00465475877 * 0.1);
                uint temp = (uint)RNG.Next(3500, 60000);
                float luminosity = (float)(0.0001 + RNG.NextDouble() * (10000000.0 - 0.0001));

                string starName = name + " ";
                if (i == 0)
                    starName += "A";
                else if (i == 1)
                    starName += "B";
                else if (i == 2)
                    starName += "C";
                else if (i == 3)
                    starName += "D";
                else if (i == 4)
                    starName += "E";

                Star star = new Star(starName, radius, temp, luminosity, newSystem);

                star.Age = (double)RNG.Next(10, 10000) * 1000000.0;

                newSystem.Stars.Add(star);
            }

            GameState.Instance.StarSystems.Add(newSystem);
            GameState.Instance.StarSystemCurrentIndex++;
            return newSystem;
        }

        public static StarSystem CreateSol()
        {
            StarSystem Sol = new StarSystem("Sol", -1);

            Star Sun = new Star("Sol", 0.00465475877, 5778, 1, Sol);
            Sun.Age = 4.6E9;
            Sun.Class = "G2";

            Sol.Stars.Add(Sun);

            Planet Earth = new Planet(Sun);

            Earth.Name = "Earth";
            Earth.Eccentricity = 1.671022E-2;
            Earth.LongitudeOfPeriapsis = 102.94719;
            Earth.IsMoon = false;
            Earth.Mass = 5.97219E24;
            Earth.OrbitalPeriod = 365;

            Earth.Radius = 6378.1;
            Earth.SemiMajorAxis = 1;
            Earth.TimeSinceApogee = 0;
            Earth.TimeSinceApogeeRemainder = 0;

            Sun.Planets.Add(Earth);

            GameState.Instance.StarSystems.Add(Sol);
            GameState.Instance.StarSystemCurrentIndex++;
            return Sol;
        }
    }
}
