using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class StarFactory
    {
        private GalaxyFactory _galaxyGen;

        public StarFactory(GalaxyFactory galaxyGen)
        {
            _galaxyGen = galaxyGen;
        }

        /// <summary>
        /// Creates a star entity in the system.
        /// Does not initialize an orbit.
        /// </summary>
        public Entity CreateStar(StarSystem system, double mass, double radius, double age, string starClass, double temperature, float luminosity, SpectralType spectralType, string starName = null)
        {
            double tempRange = temperature / _galaxyGen.Settings.StarTemperatureBySpectralType[spectralType].Max; // temp range from 0 to 1.
            ushort subDivision = (ushort)Math.Round((1 - tempRange) * 10);
            LuminosityClass luminosityClass = LuminosityClass.V;

            if (starName == null)
            {
                starName = system.NameDB.DefaultName;
            }

            int starIndex = system.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>().Count;

            starName += " " + (char)('A' + starIndex) + " " + spectralType + subDivision + luminosityClass;

            MassVolumeDB starMassVolumeDB = MassVolumeDB.NewFromMassAndRadius(mass, radius);
            StarInfoDB starInfoDB = new StarInfoDB {Age = age, Class = starClass, Luminosity = luminosity, SpectralType = spectralType, Temperature = temperature, LuminosityClass = luminosityClass, SpectralSubDivision = subDivision};
            PositionDB starPositionDB = new PositionDB();
            NameDB starNameDB = new NameDB(starName);
            OrbitDB starOrbitDB = new OrbitDB();

            return new Entity(system.SystemManager, new List<BaseDataBlob> {starOrbitDB, starMassVolumeDB, starInfoDB, starNameDB, starPositionDB});
        }

        /// <summary>
        /// Generates an entire group of stars for a starSystem.
        /// </summary>
        /// <remarks>
        /// Stars created with this method are sorted by mass.
        /// Stars created with this method are added to the newSystem's EntityManager.
        /// </remarks>
        /// <param name="system">The Star System the new stars belongs to.</param>
        /// <param name="numStars">The number of stars to create.</param>
        /// <returns>A mass-sorted list of entity ID's for the generated stars.</returns>
        public List<Entity> CreateStarsForSystem(StarSystem system, int numStars, DateTime currentDateTime)
        {
            // Argument Validation.
            if (system == null)
            {
                throw new ArgumentNullException("system");
            }

            if (numStars <= 0)
            {
                throw new ArgumentOutOfRangeException("numStars", "numStars must be greater than 0.");
            }

            // List of stars we'll be creating.
            var stars = new List<Entity>();

            while (stars.Count < numStars)
            {
                // Generate a SpectralType for the star.
                SpectralType starType;
                if (_galaxyGen.Settings.RealStarSystems)
                {
                    starType = _galaxyGen.Settings.StarTypeDistributionForRealStars.Select(system.RNG.NextDouble());
                }
                else
                {
                    starType = _galaxyGen.Settings.StarTypeDistributionForFakeStars.Select(system.RNG.NextDouble());
                }

                // We will use the one random number to select from all the spectral type ranges. Should give us saner numbers for stars.
                double randomSelection = system.RNG.NextDouble();

                // Generate the star's datablobs.
                MassVolumeDB starMVDB = MassVolumeDB.NewFromMassAndRadius(
                    GMath.SelectFromRange(_galaxyGen.Settings.StarMassBySpectralType[starType], randomSelection),
                    GMath.SelectFromRange(_galaxyGen.Settings.StarRadiusBySpectralType[starType], randomSelection));
                
                StarInfoDB starData = GenerateStarInfo(starMVDB, starType, randomSelection);

                // Initialize Position as 0,0,0. It will be updated when the star's orbit is calculated.
                PositionDB positionData = new PositionDB(0, 0, 0);

                var baseDataBlobs = new List<BaseDataBlob> {starMVDB, starData, positionData};

                stars.Add(Entity.Create(system.SystemManager, baseDataBlobs));
            }

            // The root star must be the most massive. Find it.
            Entity rootStar = stars[0];

            double rootStarMass = rootStar.GetDataBlob<MassVolumeDB>().Mass;

            foreach (Entity currentStar in stars)
            {
                double currentStarMass = currentStar.GetDataBlob<MassVolumeDB>().Mass;

                if (rootStarMass < currentStarMass)
                {
                    rootStar = currentStar;
                    rootStarMass = rootStar.GetDataBlob<MassVolumeDB>().Mass;
                }
            }

            // Swap the root star to index 0.
            int rootIndex = stars.IndexOf(rootStar);
            Entity displacedStar = stars[0];

            stars[rootIndex] = displacedStar;
            stars[0] = rootStar;

            // Generate orbits.
            Entity anchorStar = stars[0];
            MassVolumeDB anchorMVDB = anchorStar.GetDataBlob<MassVolumeDB>();
            Entity previousStar = stars[0];
            previousStar.SetDataBlob(new OrbitDB());

            int starIndex = 0;
            foreach (Entity currentStar in stars)
            {
                StarInfoDB currentStarInfo = currentStar.GetDataBlob<StarInfoDB>();
                NameDB currentStarNameDB = new NameDB(system.NameDB.DefaultName + " " + (char)('A' + starIndex) + " " + currentStarInfo.SpectralType + currentStarInfo.SpectralSubDivision + currentStarInfo.LuminosityClass);
                currentStar.SetDataBlob(currentStarNameDB);

                if (previousStar == currentStar)
                {
                    // This is the "Anchor Star"
                    continue;
                }

                OrbitDB previousOrbit = previousStar.GetDataBlob<OrbitDB>();
                StarInfoDB previousStarInfo = previousStar.GetDataBlob<StarInfoDB>();

                double minDistance = _galaxyGen.Settings.OrbitalDistanceByStarSpectralType[previousStarInfo.SpectralType].Max + _galaxyGen.Settings.OrbitalDistanceByStarSpectralType[currentStarInfo.SpectralType].Max + previousOrbit.SemiMajorAxis;

                double sma = minDistance * Math.Pow(system.RNG.NextDouble(), 3);
                double eccentricity = Math.Pow(system.RNG.NextDouble() * 0.8, 3);

                OrbitDB currentOrbit = OrbitDB.FromAsteroidFormat(anchorStar, anchorMVDB.Mass, currentStar.GetDataBlob<MassVolumeDB>().Mass, sma, eccentricity, _galaxyGen.Settings.MaxBodyInclination * system.RNG.NextDouble(), system.RNG.NextDouble() * 360, system.RNG.NextDouble() * 360, system.RNG.NextDouble() * 360, currentDateTime);
                currentStar.SetDataBlob(currentOrbit);

                previousStar = currentStar;
                starIndex++;
            }
            return stars;
        }

        /// <summary>
        /// Generates Data for a star based on it's spectral type and populates it with the data.
        /// </summary>
        /// <remarks>
        /// This function randomly generates the Radius, Temperature, Luminosity, Mass and Age of a star and then returns a star populated with those generated values.
        /// What follows is a brief description of how that is done for each data point:
        /// <list type="Bullet">
        /// <item>
        /// <b>Temperature:</b> The Temp. of the star is obtained by using the Randon.Next(min, max) function to get a random Temp. in the range a star of the given
        /// spectral type.
        /// </item>
        /// <item>
        /// <b>Luminosity:</b> The Luminosity of a star is calculated by using the RNG_NextDoubleRange() function to get a random Luminosity in the range a star of the
        /// given spectral type.
        /// </item>
        /// <item>
        /// <b>Age:</b> The possible ages for a star depend largely on its mass. The bigger and heaver the star the more pressure is put on its core where fusion occur
        /// which increases the rate that it burns Hydrogen which reduces the life of the star. The Big O class stars only last a few million years before either
        /// going Hyper Nova or devolving into a class B star. on the other hand a class G star (like Sol) has a life expectancy of about 10 billion years while a
        /// little class M star could last 100 billion years or more (hard to tell given that the Milky way is 13.2 billion years old and the universe is only
        /// about a billion years older then that). Given this we first use the mass of the star to produce a number between 0 and 1 that we can use to pick a
        /// possible age from the range (just like all the above). To get the number between 0 and 1 we use the following formula:
        /// <c>1 - Mass / MaxMassOfStarOfThisType</c>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="starMVDB">The SystemBodyDB of the star.</param>
        /// <param name="spectralType">The Spectral Type of the star.</param>
        /// <param name="randomSelection">Random selection to generate consistent values.</param>
        /// <returns>A StarInfoDB Populated with data generated based on Spectral Type and SystemBodyDB information provided.</returns>
        private StarInfoDB GenerateStarInfo(MassVolumeDB starMVDB, SpectralType spectralType, double randomSelection)
        {
            double maxStarAge = _galaxyGen.Settings.StarAgeBySpectralType[spectralType].Max;

            StarInfoDB starData = new StarInfoDB {// for star age we will make it proportional to the inverse of the stars mass ratio (for that type of star).
                // while this will produce the same age for the same mass/type of star the chances of getting the same
                // mass/type are tiny. Tho there will still be the obvious inverse relationship here.
                Age = (1 - starMVDB.Mass / _galaxyGen.Settings.StarMassBySpectralType[spectralType].Max) * maxStarAge,
                SpectralType = spectralType,
                Temperature = (uint)Math.Round(GMath.SelectFromRange(_galaxyGen.Settings.StarTemperatureBySpectralType[spectralType], randomSelection)),
                Luminosity = (float)GMath.SelectFromRange(_galaxyGen.Settings.StarLuminosityBySpectralType[spectralType], randomSelection)
            };

            // Generate a string specifying the full spectral class form a star.
            // start by getting the sub-division, which is based on temp.
            double sub = starData.Temperature / _galaxyGen.Settings.StarTemperatureBySpectralType[starData.SpectralType].Max; // temp range from 0 to 1.
            starData.SpectralSubDivision = (ushort)Math.Round((1 - sub) * 10); // invert temp range as 0 is hottest, 9 is coolest.

            // now get the luminosity class
            //< @todo For right now everthing is just main sequence. see http://en.wikipedia.org/wiki/Stellar_classification
            // on how this should be done. For right now tho class V is fine (its just flavor text).
            starData.LuminosityClass = LuminosityClass.V;

            // finally add them all up to get the class string:
            starData.Class = starData.SpectralType + starData.SpectralSubDivision.ToString() + "-" + starData.LuminosityClass;

            return starData;
        }
    }
}