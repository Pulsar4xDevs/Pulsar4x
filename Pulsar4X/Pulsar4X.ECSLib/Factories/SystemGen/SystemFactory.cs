using System.Collections.Generic;
using System.Security.Cryptography;

namespace Pulsar4X.ECSLib
{
    public class StarSystemFactory
    {
        private GalaxyFactory _galaxyGen;
        private SystemBodyFactory _systemBodyFactory;
        private StarFactory _starFactory;

        public StarSystemFactory(GalaxyFactory galaxyGen)
        {
            _galaxyGen = galaxyGen;
            _systemBodyFactory = new SystemBodyFactory(_galaxyGen);
            _starFactory = new StarFactory(_galaxyGen);
        }

        public StarSystemFactory(Game game)
        {
            _galaxyGen = game.GalaxyGen;
            _systemBodyFactory = new SystemBodyFactory(_galaxyGen);
            _starFactory = new StarFactory(_galaxyGen);
        }

        public StarSystem CreateSystem(Game game, string name, int seed = -1)
        {
            // create new RNG with Seed.
            if (seed == -1)
            {
                seed = _galaxyGen.SeedRNG.Next();
            }

            StarSystem newSystem = new StarSystem(game, name, seed);

            int numStars = newSystem.RNG.Next(1, 5);
            List<Entity> stars = _starFactory.CreateStarsForSystem(newSystem, numStars, game.CurrentDateTime);

            foreach (Entity star in stars)
            {
                _systemBodyFactory.GenerateSystemBodiesForStar(game.StaticData, newSystem, star, game.CurrentDateTime);
            }

            // < @todo generate JumpPoints
            //JumpPointFactory.GenerateJumpPoints(newSystem, numJumpPoints);

            //add this system to the GameMaster's Known Systems list.
            Entity gameMaster;
            game.GlobalManager.FindEntityByGuid(game.GameMasterFaction, out gameMaster);
            gameMaster.GetDataBlob<FactionInfoDB>().KnownSystems.Add(newSystem);
            return newSystem;
        }

        #region Create Sol

        /// <summary>
        /// Creates our own solar system.
        /// This probibly needs to be Json! (since we're getting atmo stuff)
        /// Adds sol to game.StarSystems.
        /// </summary>
        public StarSystem CreateSol(Game game)
        {
            // WIP Function. Not complete.
            StarSystem sol = new StarSystem(game, "Sol", -1);

            Entity sun = _starFactory.CreateStar(sol, GameConstants.Units.SolarMassInKG, GameConstants.Units.SolarRadiusInAu, 4.6E9, "G", 5778, 1, SpectralType.G, "Sol");

            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyDB mercuryBodyDB = new SystemBodyDB {Type = BodyType.Terrestrial, SupportsPopulations = true};
            MassVolumeDB mercuryMVDB = MassVolumeDB.NewFromMassAndRadius(3.3022E23, Distance.ToAU(2439.7));
            NameDB mercuryNameDB = new NameDB("Mercury");
            double mercurySemiMajAxis = 0.387098;
            double mercuryEccentricity = 0.205630;
            double mercuryInclination = 0;
            double mercuryLoAN = 48.33167;
            double mercuryLoP = 77.45645;
            double mercuryMeanLongd = 252.25084;
            OrbitDB mercuryOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, mercuryMVDB.Mass, mercurySemiMajAxis, mercuryEccentricity, mercuryInclination, mercuryLoAN, mercuryLoP, mercuryMeanLongd, _galaxyGen.Settings.J2000);
            PositionDB mercuryPositionDB = new PositionDB();
            mercuryPositionDB.Position = OrbitProcessor.GetPosition(mercuryOrbitDB, game.CurrentDateTime);
            Entity mercury = new Entity(sol.SystemManager, new List<BaseDataBlob>{mercuryPositionDB, mercuryBodyDB, mercuryMVDB, mercuryNameDB, mercuryOrbitDB});
            _systemBodyFactory.MineralGeneration(game.StaticData, sol, mercury);

            PositionDB venusPositionDB = new PositionDB();
            SystemBodyDB venusBodyDB = new SystemBodyDB { Type = BodyType.Terrestrial, SupportsPopulations = true };
            MassVolumeDB venusMVDB = MassVolumeDB.NewFromMassAndRadius(4.8676E24, Distance.ToAU(6051.8));
            NameDB venusNameDB = new NameDB("Venus");
            double venusSemiMajAxis = 0.72333199;
            double venusEccentricity = 0.00677323;
            double venusInclination = 0;
            double venusLoAN = 76.68069;
            double venusLoP = 131.53298;
            double venusMeanLongd = 181.97973;
            OrbitDB venusOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, venusMVDB.Mass, venusSemiMajAxis, venusEccentricity, venusInclination, venusLoAN, venusLoP, venusMeanLongd, _galaxyGen.Settings.J2000);
            venusPositionDB.Position = OrbitProcessor.GetPosition(venusOrbitDB, game.CurrentDateTime);        
            Entity venus = new Entity(sol.SystemManager, new List<BaseDataBlob> { venusPositionDB, venusBodyDB, venusMVDB, venusNameDB, venusOrbitDB });
            _systemBodyFactory.MineralGeneration(game.StaticData, sol, venus);

            PositionDB earthPositionDB = new PositionDB();
            SystemBodyDB earthBodyDB = new SystemBodyDB { Type = BodyType.Terrestrial, SupportsPopulations = true };
            MassVolumeDB earthMVDB = MassVolumeDB.NewFromMassAndRadius(5.9726E24, Distance.ToAU(6378.1));
            NameDB earthNameDB = new NameDB("Earth");
            double earthSemiMajAxis = 1.00000011;
            double earthEccentricity = 0.01671022;
            double earthInclination = 0;
            double earthLoAN = -11.26064;
            double earthLoP = 102.94719;
            double earthMeanLongd = 100.46435;
            OrbitDB earthOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, venusMVDB.Mass, earthSemiMajAxis, earthEccentricity, earthInclination, earthLoAN, earthLoP, earthMeanLongd, _galaxyGen.Settings.J2000);
            earthBodyDB.Tectonics = TectonicActivity.EarthLike;
            earthPositionDB.Position = OrbitProcessor.GetPosition(earthOrbitDB, game.CurrentDateTime);
            JDictionary<AtmosphericGasSD, float> atmoGasses = new JDictionary<AtmosphericGasSD, float>();
            atmoGasses.Add(game.StaticData.AtmosphericGases.SelectAt(6), 0.78f);
            atmoGasses.Add(game.StaticData.AtmosphericGases.SelectAt(9), 0.12f);
            atmoGasses.Add(game.StaticData.AtmosphericGases.SelectAt(11), 0.01f);
            AtmosphereDB earthAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 0.3f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?

            Entity earth = new Entity(sol.SystemManager, new List<BaseDataBlob> { earthPositionDB, earthBodyDB, earthMVDB, earthNameDB, earthOrbitDB, earthAtmosphereDB });
            _systemBodyFactory.HomeworldMineralGeneration(game.StaticData, sol, earth);


            PositionDB moonPositionDB = new PositionDB();
            SystemBodyDB moonBodyDB = new SystemBodyDB { Type = BodyType.Terrestrial, SupportsPopulations = true };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius(0.073E24, Distance.ToAU(1738.14));
            NameDB moonNameDB = new NameDB("Venus");
            double moonSemiMajAxis = 0.3844;
            double moonEccentricity = 0.0549;
            double moonInclination = 0;
            double moonLoAN = 125.08;
            double moonLoP = 318.0634; //check
            double moonMeanAnomaly = 115.3654; //check
            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(earth, earthMVDB.Mass, moonMVDB.Mass, moonSemiMajAxis, moonEccentricity, moonInclination, moonLoAN, moonLoP, moonMeanAnomaly, _galaxyGen.Settings.J2000);
            moonPositionDB.Position = OrbitProcessor.GetPosition(moonOrbitDB, game.CurrentDateTime);
            Entity moon = new Entity(sol.SystemManager, new List<BaseDataBlob> { moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            _systemBodyFactory.MineralGeneration(game.StaticData, sol, moon);
            //public static OrbitDB FromAsteroidFormat([NotNull] Entity parent, double parentMass, double myMass, double semiMajorAxis, double eccentricity, double inclination,
                                               // double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)

            //        public static Orbit FromMajorPlanetFormat(double mass, double parentMass, double semiMajorAxis, double eccentricity, double inclination,
            //                                        double longitudeOfAscendingNode, double longitudeOfPeriapsis, double meanLongitude, DateTime epoch)

    
            /*

            SystemBody Mars = new SystemBody(sun, SystemBody.PlanetType.Terrestrial);
            Mars.Name = "Mars";
            Mars.Orbit = Orbit.FromMajorPlanetFormat(0.64174E24, sun.Orbit.Mass, 1.52366231, 0.09341233, 1.85061, 49.57854, 336.04084, 355.45332, GalaxyGen.J2000);
            Mars.Radius = Distance.ToAU(3396.2);
            Mars.BaseTemperature = (float)CalculateBaseTemperatureOfBody(sun, Mars.Orbit.SemiMajorAxis);// 210.1f + (float)Constants.Units.KELVIN_TO_DEGREES_C;
            Mars.Tectonics = SystemBody.TectonicActivity.Dead;
            Mars.SurfaceGravity = 3.71f;
            Mars.Atmosphere = new Atmosphere(Mars);
            Mars.Atmosphere.Albedo = 0.250f;
            Mars.Atmosphere.SurfaceTemperature = Mars.BaseTemperature;
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(12), 0.95f * 0.01f);  // C02% * Mars Atms
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(6), 0.027f * 0.01f);  // N% * Mars Atms
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(9), 0.007f * 0.01f);  // O% * Mars Atms
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(11), 0.016f * 0.01f);  // Ar% * Mars Atms
            Mars.Atmosphere.UpdateState();
            Mars.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Mars.Position.System = Sol;
            Mars.Position.X = x;
            Mars.Position.Y = y;
            sun.Planets.Add(Mars);

            SystemBody Jupiter = new SystemBody(sun, SystemBody.PlanetType.GasGiant);
            Jupiter.Name = "Jupiter";
            Jupiter.Orbit = Orbit.FromMajorPlanetFormat(1898.3E24, sun.Orbit.Mass, 5.20336301, 0.04839266, 1.30530, 100.55615, 14.75385, 34.40438, GalaxyGen.J2000);
            Jupiter.Radius = Distance.ToAU(71492);
            Jupiter.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Jupiter.Position.System = Sol;
            Jupiter.Position.X = x;
            Jupiter.Position.Y = y;
            sun.Planets.Add(Jupiter);

            SystemBody Saturn = new SystemBody(sun, SystemBody.PlanetType.GasGiant);
            Saturn.Name = "Saturn";
            Saturn.Orbit = Orbit.FromMajorPlanetFormat(568.36E24, sun.Orbit.Mass, 9.53707032, 0.05415060, 2.48446, 113.71504, 92.43194, 49.94432, GalaxyGen.J2000);
            Saturn.Radius = Distance.ToAU(60268);
            Saturn.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Saturn.Position.System = Sol;
            Saturn.Position.X = x;
            Saturn.Position.Y = y;
            sun.Planets.Add(Saturn);

            SystemBody Uranus = new SystemBody(sun, SystemBody.PlanetType.IceGiant);
            Uranus.Name = "Uranus";
            Uranus.Orbit = Orbit.FromMajorPlanetFormat(86.816E24, sun.Orbit.Mass, 19.19126393, 0.04716771, 0.76986, 74.22988, 170.96424, 313.23218, GalaxyGen.J2000);
            Uranus.Radius = Distance.ToAU(25559);
            Uranus.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Uranus.Position.System = Sol;
            Uranus.Position.X = x;
            Uranus.Position.Y = y;
            sun.Planets.Add(Uranus);

            SystemBody Neptune = new SystemBody(sun, SystemBody.PlanetType.IceGiant);
            Neptune.Name = "Neptune";
            Neptune.Orbit = Orbit.FromMajorPlanetFormat(102E24, sun.Orbit.Mass, Distance.ToAU(4495.1E6), 0.011, 1.8, 131.72169, 44.97135, 304.88003, GalaxyGen.J2000);
            Neptune.Radius = Distance.ToAU(24764);
            Neptune.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Neptune.Position.System = Sol;
            Neptune.Position.X = x;
            Neptune.Position.Y = y;
            sun.Planets.Add(Neptune);

            SystemBody Pluto = new SystemBody(sun, SystemBody.PlanetType.DwarfPlanet);
            Pluto.Name = "Pluto";
            Pluto.Orbit = Orbit.FromMajorPlanetFormat(0.0131E24, sun.Orbit.Mass, Distance.ToAU(5906.38E6), 0.24880766, 17.14175, 110.30347, 224.06676, 238.92881, GalaxyGen.J2000);
            Pluto.Radius = Distance.ToAU(1195);
            Pluto.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Pluto.Position.System = Sol;
            Pluto.Position.X = x;
            Pluto.Position.Y = y;
            sun.Planets.Add(Pluto);

            GenerateJumpPoints(Sol);

            // Clean up cached RNG:
            m_RNG = null;
            GameState.Instance.StarSystems.Add(Sol);
            GameState.Instance.StarSystemCurrentIndex++;
            */
            game.StarSystems.Add(sol);
            Entity gameMaster;
            game.GlobalManager.FindEntityByGuid(game.GameMasterFaction, out gameMaster);
            gameMaster.GetDataBlob<FactionInfoDB>().KnownSystems.Add(sol);
            return sol;
        }

        #endregion

        #region TestSystems

        /// <summary>
        /// Creates an test system with planets of varying eccentricity.
        /// Adds to game.StarSystems
        /// </summary>
        public StarSystem CreateEccTest(Game game)
        {
            StarSystem system = new StarSystem(game, "Eccentricity test", -1);

            Entity sun = _starFactory.CreateStar(system, GameConstants.Units.SolarMassInKG, GameConstants.Units.SolarRadiusInAu, 4.6E9, "G", 5778, 1, SpectralType.G, "_ecc");

            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();


            double planetSemiMajAxis = 0.387098;
            double planetEccentricity = 0.205630;
            double planetInclination = 0;
            double planetLoAN = 48.33167;
            double planetLoP = 77.45645;
            double planetMeanLongd = 252.25084;
            

            for (int i = 0; i < 16; i++)
            {
                NameDB planetNameDB = new NameDB("planet"+i);

                SystemBodyDB planetBodyDB = new SystemBodyDB { Type = BodyType.Terrestrial, SupportsPopulations = true };
                MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius(3.3022E23, Distance.ToAU(2439.7));
                PositionDB planetPositionDB = new PositionDB();
                planetEccentricity = i / 16.0;
                OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, planetMVDB.Mass, planetSemiMajAxis, planetEccentricity, planetInclination, planetLoAN, planetLoP, planetMeanLongd, _galaxyGen.Settings.J2000);
                planetPositionDB.Position = OrbitProcessor.GetPosition(planetOrbitDB, game.CurrentDateTime);
                Entity planet = new Entity(system.SystemManager, new List<BaseDataBlob> { planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB });
            }
            game.StarSystems.Add(system);
            Entity gameMaster;
            game.GlobalManager.FindEntityByGuid(game.GameMasterFaction, out gameMaster);
            gameMaster.GetDataBlob<FactionInfoDB>().KnownSystems.Add(system);
            return system;
        }

        /// <summary>
        /// Creates an test system with planets of varying longitude of periapsis.
        /// Adds to game.StarSystem.
        /// </summary>
        public StarSystem CreateLongitudeTest(Game game) {
            StarSystem system = new StarSystem(game, "Longitude test", -1);

            Entity sun = _starFactory.CreateStar(system, GameConstants.Units.SolarMassInKG, GameConstants.Units.SolarRadiusInAu, 4.6E9, "G", 5778, 1, SpectralType.G, "_lop");

            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            double planetSemiMajAxis = 0.387098;
            double planetEccentricity = 0.9;// 0.205630;
            double planetInclination = 0;
            double planetLoAN = 48.33167;
            double planetLoP = 77.45645;
            double planetMeanLongd = 252.25084;
            

            for (int i = 0; i < 13; i++)
            {
                NameDB planetNameDB = new NameDB("planet"+i);
                SystemBodyDB planetBodyDB = new SystemBodyDB { Type = BodyType.Terrestrial, SupportsPopulations = true };
                MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius(3.3022E23, Distance.ToAU(2439.7));
                PositionDB planetPositionDB = new PositionDB();
                planetLoP = i * 15;
                OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, planetMVDB.Mass, planetSemiMajAxis, planetEccentricity, planetInclination, planetLoAN, planetLoP, planetMeanLongd, _galaxyGen.Settings.J2000);
                planetPositionDB.Position = OrbitProcessor.GetPosition(planetOrbitDB, game.CurrentDateTime);
                Entity planet = new Entity(system.SystemManager, new List<BaseDataBlob> { planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB });
            }
            game.StarSystems.Add(system);
            Entity gameMaster;
            game.GlobalManager.FindEntityByGuid(game.GameMasterFaction, out gameMaster);
            gameMaster.GetDataBlob<FactionInfoDB>().KnownSystems.Add(system);
            return system;
        }

        #endregion

    }
}
