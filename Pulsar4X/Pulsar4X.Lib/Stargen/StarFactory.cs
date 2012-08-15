using System;
using System.Collections.Generic;
using Pulsar4X.Entities;

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

        public List<Star> Create(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty.");

            var numberOfStars = GetNumberOfStars();
            var stars = new List<Star>();
            for (var i = 1; i <= numberOfStars; i++)
            {
                var star = new Star();

                //TODO: All of the variable generation below this can be redone to be more realistic or playable in the future.
                //TODO: This is generated based on data scrapped from wikipedia on Star Spectrums and their distribution
                star.Name = numberOfStars == 1 ? name : string.Format("{0} {1}", name, GetPostfix(i));
                star.Spectrum = GenerateSpectrum();
                star.Mass = GenerateMass(star.Spectrum); //MathUtilities.Random.NextDouble(_minimumMass, _maximumMass);
                star.Luminosity = AccreteUtilities.Luminosity(star.Mass);
                star.EcoSphereRadius = AccreteUtilities.EcoSphereRadius(star.Luminosity);
                star.Life = AccreteUtilities.StellarLife(star.Mass, star.Luminosity);
                star.Age = MathUtilities.Random.NextDouble(_minimumAge, Math.Max(_maximumAge, star.Life));
                star.SpectrumAdjustment = MathUtilities.Random.Next(0, 10);

                stars.Add(star);
            }

            return stars;
        }

        private double GenerateMass(StarSpectrum spectrum)
        {
            switch (spectrum)
            {
                case StarSpectrum.O:
                    return  MathUtilities.Random.NextDouble(16.0, 150.0);
                case StarSpectrum.B:
                    return  MathUtilities.Random.NextDouble(2.1, 16.0);
                case StarSpectrum.A:
                    return  MathUtilities.Random.NextDouble(1.4, 2.1);
                case StarSpectrum.F:
                    return  MathUtilities.Random.NextDouble(1.04, 1.4);
                case StarSpectrum.G:
                    return  MathUtilities.Random.NextDouble(.8, 1.04);
                case StarSpectrum.K:
                    return  MathUtilities.Random.NextDouble(.45, .8);
                case StarSpectrum.M:
                    return  MathUtilities.Random.NextDouble(.07, .45);
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
            switch (i)
            {
                case 1:
                    return "A";
                case 2:
                    return "B";
                case 3:
                    return "C";
                default:
                    throw new ArgumentException("Unexpected number of stars.");
            }
        }
    }
}
