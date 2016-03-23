using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class StarSystemFactory
    {
        internal GalaxyFactory GalaxyGen;
        private SystemBodyFactory _systemBodyFactory;
        private StarFactory _starFactory;

        public StarSystemFactory(GalaxyFactory galaxyGen)
        {
            GalaxyGen = galaxyGen;
            _systemBodyFactory = new SystemBodyFactory(GalaxyGen);
            _starFactory = new StarFactory(GalaxyGen);
        }

        public StarSystemFactory(Game game)
        {
            GalaxyGen = game.GalaxyGen;
            _systemBodyFactory = new SystemBodyFactory(GalaxyGen);
            _starFactory = new StarFactory(GalaxyGen);
        }

        public StarSystem CreateSystem(Game game, string name, int seed = -1)
        {
            // create new RNG with Seed.
            if (seed == -1)
            {
                seed = GalaxyGen.SeedRNG.Next();
            }

            StarSystem newSystem = new StarSystem(game, name, seed);

            int numStars = newSystem.RNG.Next(1, 5);
            List<Entity> stars = _starFactory.CreateStarsForSystem(newSystem, numStars, game.CurrentDateTime);

            foreach (Entity star in stars)
            {
                _systemBodyFactory.GenerateSystemBodiesForStar(game.StaticData, newSystem, star, game.CurrentDateTime);
            }


            // Generate Jump Points
            JPSurveyFactory.GenerateJPSurveyPoints(newSystem);
            JPFactory.GenerateJumpPoints(this, newSystem);

            //add this system to the GameMaster's Known Systems list.
            game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(newSystem.Guid);

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
            OrbitDB mercuryOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, mercuryMVDB.Mass, mercurySemiMajAxis, mercuryEccentricity, mercuryInclination, mercuryLoAN, mercuryLoP, mercuryMeanLongd, GalaxyGen.Settings.J2000);
            PositionDB mercuryPositionDB = new PositionDB(OrbitProcessor.GetPosition(mercuryOrbitDB, game.CurrentDateTime), sol.Guid);
            Entity mercury = new Entity(sol.SystemManager, new List<BaseDataBlob>{mercuryPositionDB, mercuryBodyDB, mercuryMVDB, mercuryNameDB, mercuryOrbitDB});
            _systemBodyFactory.MineralGeneration(game.StaticData, sol, mercury);

            SystemBodyDB venusBodyDB = new SystemBodyDB { Type = BodyType.Terrestrial, SupportsPopulations = true };
            MassVolumeDB venusMVDB = MassVolumeDB.NewFromMassAndRadius(4.8676E24, Distance.ToAU(6051.8));
            NameDB venusNameDB = new NameDB("Venus");
            double venusSemiMajAxis = 0.72333199;
            double venusEccentricity = 0.00677323;
            double venusInclination = 0;
            double venusLoAN = 76.68069;
            double venusLoP = 131.53298;
            double venusMeanLongd = 181.97973;
            OrbitDB venusOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, venusMVDB.Mass, venusSemiMajAxis, venusEccentricity, venusInclination, venusLoAN, venusLoP, venusMeanLongd, GalaxyGen.Settings.J2000);
            PositionDB venusPositionDB = new PositionDB(OrbitProcessor.GetPosition(venusOrbitDB, game.CurrentDateTime), sol.Guid);
            Entity venus = new Entity(sol.SystemManager, new List<BaseDataBlob> { venusPositionDB, venusBodyDB, venusMVDB, venusNameDB, venusOrbitDB });
            _systemBodyFactory.MineralGeneration(game.StaticData, sol, venus);

            SystemBodyDB earthBodyDB = new SystemBodyDB { Type = BodyType.Terrestrial, SupportsPopulations = true };
            MassVolumeDB earthMVDB = MassVolumeDB.NewFromMassAndRadius(5.9726E24, Distance.ToAU(6378.1));
            NameDB earthNameDB = new NameDB("Earth");
            double earthSemiMajAxis = 1.00000011;
            double earthEccentricity = 0.01671022;
            double earthInclination = 0;
            double earthLoAN = -11.26064;
            double earthLoP = 102.94719;
            double earthMeanLongd = 100.46435;
            OrbitDB earthOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, venusMVDB.Mass, earthSemiMajAxis, earthEccentricity, earthInclination, earthLoAN, earthLoP, earthMeanLongd, GalaxyGen.Settings.J2000);
            earthBodyDB.Tectonics = TectonicActivity.EarthLike;
            PositionDB earthPositionDB = new PositionDB(OrbitProcessor.GetPosition(earthOrbitDB, game.CurrentDateTime), sol.Guid);
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>();
            atmoGasses.Add(game.StaticData.AtmosphericGases.SelectAt(6), 0.78f);
            atmoGasses.Add(game.StaticData.AtmosphericGases.SelectAt(9), 0.12f);
            atmoGasses.Add(game.StaticData.AtmosphericGases.SelectAt(11), 0.01f);
            AtmosphereDB earthAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 0.3f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?

            Entity earth = new Entity(sol.SystemManager, new List<BaseDataBlob> { earthPositionDB, earthBodyDB, earthMVDB, earthNameDB, earthOrbitDB, earthAtmosphereDB });
            _systemBodyFactory.HomeworldMineralGeneration(game.StaticData, sol, earth);

            SystemBodyDB lunaBodyDB = new SystemBodyDB { Type = BodyType.Moon, SupportsPopulations = true };
            MassVolumeDB lunaMVDB = MassVolumeDB.NewFromMassAndRadius(0.073E24, Distance.ToAU(1738.14));
            NameDB lunaNameDB = new NameDB("Luna");
            double lunaSemiMajAxis = Distance.ToAU(0.3844E6);
            double lunaEccentricity = 0.0549;
            double lunaInclination = 5.1;
            // Next three values are unimportant. Luna's LoAN and AoP regress/progress by one revolution every 18.6/8.85 years respectively.
            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression. 
            double lunaLoAN = 125.08;
            double lunaAoP = 318.0634;
            double lunaMeanAnomaly = 115.3654;
            OrbitDB lunaOrbitDB = OrbitDB.FromAsteroidFormat(earth, earthMVDB.Mass, lunaMVDB.Mass, lunaSemiMajAxis, lunaEccentricity, lunaInclination, lunaLoAN, lunaAoP, lunaMeanAnomaly, GalaxyGen.Settings.J2000);
            PositionDB lunaPositionDB = new PositionDB(OrbitProcessor.GetPosition(lunaOrbitDB, game.CurrentDateTime), sol.Guid);
            Entity luna = new Entity(sol.SystemManager, new List<BaseDataBlob> { lunaPositionDB, lunaBodyDB, lunaMVDB, lunaNameDB, lunaOrbitDB });
            _systemBodyFactory.MineralGeneration(game.StaticData, sol, luna);

            SystemBodyDB marsBodyDB = new SystemBodyDB { Type = BodyType.Terrestrial, SupportsPopulations = true };
            MassVolumeDB marsMVDB = MassVolumeDB.NewFromMassAndRadius(0.64174E24, Distance.ToAU(3396.2));
            NameDB marsNameDB = new NameDB("Mars");
            double marsSemiMajAxis = Distance.ToAU(227.92E6);
            double marsEccentricity = 0.0935;
            double marsInclination = 1.85;
            double marsLoAN = 49.57854;
            double marsAoP = 336.04084;
            double marsMeanLong = 355.45332;
            OrbitDB marsOrbitDB =OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, marsMVDB.Mass, marsSemiMajAxis, marsEccentricity, marsInclination, marsLoAN, marsAoP, marsMeanLong, GalaxyGen.Settings.J2000);
            PositionDB marsPositionDB = new PositionDB(OrbitProcessor.GetPosition(marsOrbitDB, game.CurrentDateTime), sol.Guid);
            Entity mars = new Entity(sol.SystemManager, new List<BaseDataBlob> { marsPositionDB, marsBodyDB, marsMVDB, marsNameDB, marsOrbitDB} );
            _systemBodyFactory.MineralGeneration(game.StaticData, sol, mars);
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
            
            JPSurveyFactory.GenerateJPSurveyPoints(sol);

            game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(sol.Guid);
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
                PositionDB planetPositionDB = new PositionDB(system.Guid);
                planetEccentricity = i / 16.0;
                OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, planetMVDB.Mass, planetSemiMajAxis, planetEccentricity, planetInclination, planetLoAN, planetLoP, planetMeanLongd, GalaxyGen.Settings.J2000);
                planetPositionDB.Position = OrbitProcessor.GetPosition(planetOrbitDB, game.CurrentDateTime);
                Entity planet = new Entity(system.SystemManager, new List<BaseDataBlob> { planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB });
            }
            game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(system.Guid);
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
                PositionDB planetPositionDB = new PositionDB(system.Guid);
                planetLoP = i * 15;
                OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.Mass, planetMVDB.Mass, planetSemiMajAxis, planetEccentricity, planetInclination, planetLoAN, planetLoP, planetMeanLongd, GalaxyGen.Settings.J2000);
                planetPositionDB.Position = OrbitProcessor.GetPosition(planetOrbitDB, game.CurrentDateTime);
                Entity planet = new Entity(system.SystemManager, new List<BaseDataBlob> { planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB });
            }
            game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(system.Guid);
            return system;
        }

        #endregion

    }
}
