using System;
using System.Collections.Generic;
using Pulsar4X.Entities;
using System.Drawing;

namespace Pulsar4X.Stargen
{
    public class StarFactory
    {
        private readonly double _minimumAge;
        private readonly double _maximumAge;

        public StarFactory(double minAge, double maxAge)
        {
            _minimumAge = minAge;
            _maximumAge = maxAge;
        }

        /// <summary>
        /// Star Class to temperature lookup.  From Universe.
        /// </summary>
        public static Dictionary<StarSpectrum, List<double>> TempLookup = new Dictionary<StarSpectrum, List<double>>()
            {
                {StarSpectrum.B, new List<double> { 25000.0, 23600.0, 22200.0, 20800.0, 19400.0, 18000.0, 16600.0, 15200.0, 13800.0, 12400.0 }},
                {StarSpectrum.A, new List<double> { 11000.0, 10650.0, 10300.0, 9950.0, 9600.0, 9250.0, 8900.0, 8550.0, 8200.0, 7850.0}},
                {StarSpectrum.F, new List<double> { 7500.0, 7350.0, 7200.0, 7050.0, 6900.0, 6750.0, 6600.0, 6450.0, 6300.0, 6150.0}},
                {StarSpectrum.G, new List<double> { 6000.0, 5900.0, 5800.0, 5700.0, 5600.0, 5500.0, 5400.0, 5300.0, 5200.0, 5100.0}},
                {StarSpectrum.K, new List<double> { 5000.0, 4850.0, 4700.0, 4550.0, 4400.0, 4250.0, 4100.0, 3950.0, 3800.0, 3650.0}},
                {StarSpectrum.M, new List<double> { 3500.0, 3200.0, 2900.0, 2600.0, 2300.0, 2000.0, 1700.0, 1400.0, 1100.0, 800.0}}
            };

        /// <summary>
        /// Look up table for star colors
        /// Incomplete, assumes spectral adjustment of 5
        /// Should be expanded
        /// </summary>
        public static Dictionary<StarSpectrum, List<Color>> ColorLookup = new Dictionary<StarSpectrum, List<Color>>()
            {
                {StarSpectrum.O, new List<Color> { Color.FromArgb(255, 155, 176, 255) }},
                {StarSpectrum.B, new List<Color> { Color.FromArgb(255, 170, 191, 255) }},
                {StarSpectrum.A, new List<Color> { Color.FromArgb(255, 202, 215, 255) }},
                {StarSpectrum.F, new List<Color> { Color.FromArgb(255, 248, 247, 255) }},
                {StarSpectrum.G, new List<Color> { Color.FromArgb(255, 255, 244, 234) }},
                {StarSpectrum.K, new List<Color> { Color.FromArgb(255, 255, 210, 161) }},
                {StarSpectrum.M, new List<Color> { Color.FromArgb(255, 255, 204, 111) }}
            };


        /// <summary>
        /// Creates a collection of stars for use in a StarSystem. By default, it will generate a 
        /// weighted random number of stars between 1 and 3. If the overrideNumberOfStars parameter 
        /// is set to a value greater than 0, it will generate that many stars.
        /// </summary>
        /// <param name="name">The name the star or collection of stars should derive from</param>
        /// <param name="overrideNumberOfStars">0 by default to allow for a weighted random number of stars, set to a value if a specific number of stars are needed</param>
        /// <returns>List<Star></Star></returns>
        public List<Star> Create(string name, int overrideNumberOfStars = 0)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty.");

            int numberOfStars = overrideNumberOfStars > 0 ? overrideNumberOfStars : GetNumberOfStars();
            var stars = new List<Star>();
            for (var i = 1; i <= numberOfStars; i++)
            {
                var star = new Star();

                star.Id = Guid.NewGuid();
                //TODO: All of the variable generation below this can be redone to be more realistic or playable in the future.
                //TODO: This is generated based on data scrapped from wikipedia on Star Spectrums and their distribution
                star.Name = numberOfStars == 1 ? name : string.Format("{0} {1}", name, GetPostfix(i));
                star.Spectrum = GenerateSpectrum();
                star.Mass = GenerateMass(star.Spectrum); //MathUtilities.Random.NextDouble(_minimumMass, _maximumMass);
                star.Luminosity = Luminosity(star.Mass);
                star.EcoSphereRadius = EcoSphereRadius(star.Luminosity);
                star.Life = StellarLife(star.Mass, star.Luminosity);
                star.Age = MathUtilities.Random.NextDouble(_minimumAge, Math.Max(_maximumAge, star.Life));
                star.SpectrumAdjustment = MathUtilities.Random.Next(0, 10);

                star.Temperature = TempLookup[star.Spectrum][star.SpectrumAdjustment];
                star.Temperature += MathUtilities.Random.randomNormal() * (star.Temperature / 200.0);

                star.Radius = Radius(star.Luminosity, star.Temperature);

                star.Color = ColorLookup[star.Spectrum][0];

                if (i > 1)
                    star.OrbitalRadius = MathUtilities.Random.NextDouble(0.5, 50);
                else
                    star.OrbitalRadius = 0.0;

                stars.Add(star);
            }

            return stars;
        }

        private double GenerateMass(StarSpectrum spectrum)
        {
            switch (spectrum)
            {
                case StarSpectrum.O:
                    return MathUtilities.Random.NextDouble(16.0, 150.0);
                case StarSpectrum.B:
                    return MathUtilities.Random.NextDouble(2.1, 16.0);
                case StarSpectrum.A:
                    return MathUtilities.Random.NextDouble(1.4, 2.1);
                case StarSpectrum.F:
                    return MathUtilities.Random.NextDouble(1.04, 1.4);
                case StarSpectrum.G:
                    return MathUtilities.Random.NextDouble(.8, 1.04);
                case StarSpectrum.K:
                    return MathUtilities.Random.NextDouble(.45, .8);
                case StarSpectrum.M:
                    return MathUtilities.Random.NextDouble(.07, .45);
                default:
                    throw new ArgumentException(string.Format("Unknown Spectrum: {0}", spectrum.ToString()));
            }
        }

        private const double SpectrumOChance = 1 / 3000000;
        private const double SpectrumBChance = 1 / 800;
        private const double SpectrumAChance = 1 / 160;
        private const double SpectrumFChance = 1 / 33;
        private const double SpectrumGChance = 1 / 13;
        private const double SpectrumKChance = 1 / 8;
        private const double SpectrumMChance = 76 / 100;

        private StarSpectrum GenerateSpectrum()
        {
            //TODO: Remove, temporary hack to create more Sol-sized stars
            return StarSpectrum.G;

            //TODO: Add support for age of galaxy if we want that, right now it just does medium age
            var chance = MathUtilities.Random.NextDouble();
            if (chance < SpectrumOChance)
                return StarSpectrum.O;
            if (chance < SpectrumBChance)
                return StarSpectrum.B;
            if (chance < SpectrumAChance)
                return StarSpectrum.A;
            if (chance < SpectrumFChance)
                return StarSpectrum.F;
            if (chance < SpectrumGChance)
                return StarSpectrum.G;
            if (chance < SpectrumKChance)
                return StarSpectrum.K;
            return StarSpectrum.M;

            #region Spectrum by Age of Galaxy
            //switch (age)
            //{
            //    case GalaxyAge.Young:
            //        if (chance < (SpectrumOChance * 3.0))
            //            return StarSpectrum.O;
            //        if (chance < (SpectrumBChance * 2.5))
            //            return StarSpectrum.B;
            //        if (chance < (SpectrumAChance * 2.0))
            //            return StarSpectrum.A;
            //        if (chance < (SpectrumFChance * 1.8))
            //            return StarSpectrum.F;
            //        if (chance < (SpectrumGChance * 1.5))
            //            return StarSpectrum.G;
            //        if (chance < (SpectrumKChance * 1.25))
            //            return StarSpectrum.K;
            //        return StarSpectrum.M;
            //    case GalaxyAge.Medium:
            //        if (chance < SpectrumOChance)
            //            return StarSpectrum.O;
            //        if (chance < SpectrumBChance)
            //            return StarSpectrum.B;
            //        if (chance < SpectrumAChance)
            //            return StarSpectrum.A;
            //        if (chance < SpectrumFChance)
            //            return StarSpectrum.F;
            //        if (chance < SpectrumGChance)
            //            return StarSpectrum.G;
            //        if (chance < SpectrumKChance)
            //            return StarSpectrum.K;
            //        return StarSpectrum.M;
            //    case GalaxyAge.Old:
            //        if (chance < (SpectrumOChance / 2.0))
            //            return StarSpectrum.O;
            //        if (chance < (SpectrumBChance / 1.7))
            //            return StarSpectrum.B;
            //        if (chance < (SpectrumAChance / 1.3))
            //            return StarSpectrum.A;
            //        if (chance < (SpectrumFChance / .9))
            //            return StarSpectrum.F;
            //        if (chance < (SpectrumGChance / .75))
            //            return StarSpectrum.G;
            //        if (chance < (SpectrumKChance / .5))
            //            return StarSpectrum.K;
            //        return StarSpectrum.M;
            //    default:
            //        return StarSpectrum.M;
            //}
            #endregion
        }

        private int GetNumberOfStars()
        {
            // 2/3rds of star systems in the Milky Way are single stars
            // 1/3rd are binary or multiple (trinary or higher is unstable)
            // will break the 1/3rd into two parts, 2/9ths binary, 1/9th trinary
            var rand = MathUtilities.Random.Next(1, 10);
            switch (rand)
            {
                case 1:
                    return 3;
                case 2:
                case 3:
                    return 2;
                default:
                    return 1;
            }
        }

        private string GetPostfix(int i)
        {
            var c = (char)(65 + (i - 1));
            return c.ToString();
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

        public static double Luminosity(double massRatio)
        {
            double n;
            if (massRatio < 1.0)
                n = 1.75 * (massRatio - 0.1) + 3.325;
            else
                n = 0.5 * (2.0 - massRatio) + 4.4;

            return Math.Pow(massRatio, n);
        }

        public static double Radius(double luminosity, double temperature)
        {
            return Math.Sqrt(luminosity) * ((6100.0 / temperature) * (6100.0 / temperature));
        }

    }
}
