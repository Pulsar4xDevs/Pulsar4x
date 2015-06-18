using System.Collections.Generic;

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
            List<Entity> stars = _starFactory.CreateStarsForSystem(newSystem, numStars);

            foreach (Entity star in stars)
            {
                _systemBodyFactory.GenerateSystemBodiesForStar(newSystem, star, game.CurrentDateTime);
            }

            // < @todo generate JumpPoints
            //JumpPointFactory.GenerateJumpPoints(newSystem, numJumpPoints);

            return newSystem;
        }

        #region Create Sol

        /// <summary>
        /// Creates our own solar system.
        /// </summary>
        public StarSystem CreateSol(Game game)
        {
            // WIP Function. Not complete.
            StarSystem sol = new StarSystem(game, "Sol", -1);

            Entity sun = _starFactory.CreateStar(sol, GameSettings.Units.SolarMassInKG, GameSettings.Units.SolarRadiusInKm, 4.6E9, "G", 5778, 1, SpectralType.G, "Sol");

            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyDB mercuryBodyDB = new SystemBodyDB {Type = BodyType.Terrestrial, SupportsPopulations = true};
            MassVolumeDB mercuryMVDB = new MassVolumeDB(3.3022E23, MassVolumeDB.GetVolumeFromRadius(2439.7));
            NameDB mercuryNameDB = new NameDB(Entity.InvalidEntity, "Mercury");
            OrbitDB mercuryOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB, mercuryMVDB, 0.387098, 0.205630, 0, 48.33167, 29.124, 252.25084, _galaxyGen.Settings.J2000);
            PositionDB mercuryPositionDB = new PositionDB();

            Entity mercury = new Entity(sol.SystemManager, new List<BaseDataBlob>{mercuryPositionDB, mercuryBodyDB, mercuryMVDB, mercuryNameDB, mercuryOrbitDB});

            /*
            SystemBody Venus = new SystemBody(sun, SystemBody.PlanetType.Terrestrial);
            Venus.Name = "Venus";
            Venus.Orbit = Orbit.FromMajorPlanetFormat(4.8676E24, sun.Orbit.Mass, 0.72333199, 0.00677323, 0, 76.68069, 131.53298, 181.97973, GalaxyGen.J2000);
            Venus.Radius = Distance.ToAU(6051.8);
            Venus.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Venus.Position.System = Sol;
            Venus.Position.X = x;
            Venus.Position.Y = y;
            sun.Planets.Add(Venus);

            SystemBody Earth = new SystemBody(sun, SystemBody.PlanetType.Terrestrial);
            Earth.Name = "Earth";
            Earth.Orbit = Orbit.FromMajorPlanetFormat(5.9726E24, sun.Orbit.Mass, 1.00000011, 0.01671022, 0, -11.26064, 102.94719, 100.46435, GalaxyGen.J2000);
            Earth.Radius = Distance.ToAU(6378.1);
            Earth.BaseTemperature = Temperature.ToCelsius(279.3f);  //(float)CalculateBaseTemperatureOfBody(Sun, Earth.Orbit.SemiMajorAxis);
            Earth.Tectonics = SystemBody.TectonicActivity.EarthLike;
            Earth.SurfaceGravity = 9.8f;
            Earth.Atmosphere = new Atmosphere(Earth);
            Earth.Atmosphere.Albedo = 0.306f;
            Earth.Atmosphere.SurfaceTemperature = Earth.BaseTemperature;
            AddGasToAtmoSafely(Earth.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(6), 0.78f);  // N
            AddGasToAtmoSafely(Earth.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(9), 0.21f);  // O
            AddGasToAtmoSafely(Earth.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(11), 0.01f);  // Ar
            Earth.Atmosphere.UpdateState();
            Earth.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Earth.Position.System = Sol;
            Earth.Position.X = x;
            Earth.Position.Y = y;
            sun.Planets.Add(Earth);

            SystemBody Moon = new SystemBody(Earth, SystemBody.PlanetType.Moon);
            Moon.Name = "Moon";
            Moon.Orbit = Orbit.FromAsteroidFormat(0.073E24, Earth.Orbit.Mass, Distance.ToAU(384748), 0.0549006, 0, 0, 0, 0, GalaxyGen.J2000);
            Moon.Radius = Distance.ToAU(1738.14);
            Moon.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Moon.Position.System = Sol;
            Moon.Position.X = Earth.Position.X + x;
            Moon.Position.Y = Earth.Position.Y + y;
            Earth.Moons.Add(Moon);

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

            return sol;
        }


        #endregion
    }
}
