using System;
using System.Linq;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Engine.Sol;
using Pulsar4X.Engine.Factories;

namespace Pulsar4X.Engine
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

        public StarSystem CreateSystem(Game game, string name, int seed, bool initialiseAllMinerals = false)
        {
            StarSystem newSystem = new StarSystem();
            newSystem.Initialize(game, name, seed);

            var rngValue = newSystem.RNGNextDouble();
            int numStars = game.Settings.StarChances.Select(rngValue);
            List<Entity> stars = _starFactory.CreateStarsForSystem(newSystem, numStars, game.TimePulse.GameGlobalDateTime);

            foreach (Entity star in stars)
            {
                _systemBodyFactory.GenerateSystemBodiesForStar(game.StartingGameData, newSystem, star, game.TimePulse.GameGlobalDateTime);
            }

            // Generate Jump Points
            JPSurveyFactory.GenerateJPSurveyPoints(newSystem);
            JPFactory.GenerateJumpPoints(this, newSystem);

            //add this system to the GameMaster's Known Systems list.
            game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(newSystem.Guid);
            OrbitProcessor.UpdateSystemOrbits(newSystem, game.TimePulse.GameGlobalDateTime); //sets the positions of all the entites.

            // Generate Minerals if requested
            if (initialiseAllMinerals)
            {
                foreach (BodyType bodyType in Enum.GetValues(typeof(BodyType)))
                {
                    foreach (Entity entity in newSystem.GetEntitiesOfAllBodiesOfType(bodyType))
                    {
                        _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), newSystem, entity);
                    }
                }
            }

            return newSystem;
        }


        public StarSystem CreateTestSystem(Game game, int x = 0, int y = 0)
        {

            StarSystem sol = new StarSystem();
            sol.Initialize(game, "something", -1);

            Entity sun = _starFactory.CreateStar(sol, UniversalConstants.Units.SolarMassInKG, UniversalConstants.Units.SolarRadiusInAu, 4.6E9, "G", 5778, 1, SpectralType.G, "something");
            sun.GetDataBlob<PositionDB>().AbsolutePosition += new Vector3(x, y, 0);

            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB mercuryBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true, Albedo = 0.068f }; //Albedo = 0.068f
            MassVolumeDB mercuryMVDB = MassVolumeDB.NewFromMassAndRadius_AU(3.3022E23, Distance.KmToAU(2439.7));
            NameDB mercuryNameDB = new NameDB("LOLXDWTF");
            double mercurySemiMajAxis = 0.387098;
            double mercuryEccentricity = 0.205630;
            double mercuryInclination = 0;
            double mercuryLoAN = 48.33167;
            double mercuryLoP = 77.45645;
            double mercuryMeanLongd = 252.25084;
            OrbitDB mercuryOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, mercuryMVDB.MassDry, mercurySemiMajAxis, mercuryEccentricity, mercuryInclination, mercuryLoAN, mercuryLoP, mercuryMeanLongd, GalaxyGen.Settings.J2000);
            PositionDB mercuryPositionDB = new PositionDB(mercuryOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, sun);
            //AtmosphereDB mercuryAtmo = new AtmosphereDB();
            SensorProfileDB sensorProfile = new SensorProfileDB();
            mercuryPositionDB.AbsolutePosition += new Vector3(x, y, 0);
            Entity mercury = Entity.Create();
            sol.AddEntity(mercury, new List<BaseDataBlob>{sensorProfile, mercuryPositionDB, mercuryBodyDB, mercuryMVDB, mercuryNameDB, mercuryOrbitDB});
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, mercury);
            SensorTools.PlanetEmmisionSig(sensorProfile, mercuryBodyDB, mercuryMVDB);

            SystemBodyInfoDB venusBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true, Albedo = 0.77f };
            MassVolumeDB venusMVDB = MassVolumeDB.NewFromMassAndRadius_AU(4.8676E24, Distance.KmToAU(6051.8));
            NameDB venusNameDB = new NameDB("AYLMAOROFL");
            double venusSemiMajAxis = 0.72333199;
            double venusEccentricity = 0.00677323;
            double venusInclination = 0;
            double venusLoAN = 76.68069;
            double venusLoP = 131.53298;
            double venusMeanLongd = 181.97973;
            OrbitDB venusOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, venusMVDB.MassDry, venusSemiMajAxis, venusEccentricity, venusInclination, venusLoAN, venusLoP, venusMeanLongd, GalaxyGen.Settings.J2000);
            PositionDB venusPositionDB = new PositionDB(venusOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, sun);
            sensorProfile = new SensorProfileDB();
            Entity venus = Entity.Create();
            sol.AddEntity(venus, new List<BaseDataBlob> { sensorProfile, venusPositionDB, venusBodyDB, venusMVDB, venusNameDB, venusOrbitDB });
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, venus);
            SensorTools.PlanetEmmisionSig(sensorProfile, venusBodyDB, venusMVDB);

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true, Albedo = 0.306f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(5.9726E24, Distance.KmToAU(6378.1));
            NameDB planetNameDB = new NameDB("OMG");
            double planetSemiMajorAxisAU = 1.00000011;
            double planetEccentricity = 0.01671022;
            double planetEclipticInclination = 0;
            double planetLoAN = -11.26064;
            double planetAoP = 102.94719;
            double planetMeanAnomaly = 100.46435;
            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, GalaxyGen.Settings.J2000);
            planetBodyDB.Tectonics = TectonicActivity.EarthLike;
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, sun);
            var atmoGasses = new Dictionary<string, float>();
            atmoGasses.Add(game.StartingGameData.AtmosphericGas["nitrogen"].UniqueID, 0.78f);
            atmoGasses.Add(game.StartingGameData.AtmosphericGas["oxygen"].UniqueID, 0.12f);
            atmoGasses.Add(game.StartingGameData.AtmosphericGas["water-vapour"].UniqueID, 0.01f);
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses);
            sensorProfile = new SensorProfileDB();
            Entity planet = Entity.Create();
            sol.AddEntity(planet, new List<BaseDataBlob> {sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            _systemBodyFactory.HomeworldMineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, planet);
            SensorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);


            SystemBodyInfoDB lunaBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true };
            MassVolumeDB lunaMVDB = MassVolumeDB.NewFromMassAndRadius_AU(0.073E24, Distance.KmToAU(1738.14));
            NameDB lunaNameDB = new NameDB("NOWAY");
            double lunaSemiMajAxis = Distance.KmToAU(0.3844E6);
            double lunaEccentricity = 0.0549;
            double lunaInclination = 0;//5.1;
            // Next three values are unimportant. Luna's LoAN and AoP regress/progress by one revolution every 18.6/8.85 years respectively.
            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression.
            double lunaLoAN = 125.08;
            double lunaAoP = 318.0634;
            double lunaMeanAnomaly = 115.3654;
            OrbitDB lunaOrbitDB = OrbitDB.FromAsteroidFormat(planet, planetMVDB.MassDry, lunaMVDB.MassDry, lunaSemiMajAxis, lunaEccentricity, lunaInclination, lunaLoAN, lunaAoP, lunaMeanAnomaly, GalaxyGen.Settings.J2000);
            PositionDB lunaPositionDB = new PositionDB(lunaOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime) + planetPositionDB.AbsolutePosition, sol.Guid, planet);
            sensorProfile = new SensorProfileDB();
            Entity luna = Entity.Create();
            sol.AddEntity(luna, new List<BaseDataBlob> {sensorProfile, lunaPositionDB, lunaBodyDB, lunaMVDB, lunaNameDB, lunaOrbitDB });
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, luna);
            SensorTools.PlanetEmmisionSig(sensorProfile, lunaBodyDB, lunaMVDB);


            SystemBodyInfoDB halleysBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Comet, SupportsPopulations = false, Albedo = 0.04f  }; //Albedo = 0.04f
            MassVolumeDB halleysMVDB = MassVolumeDB.NewFromMassAndRadius_AU(2.2e14, Distance.KmToAU(11));
            NameDB halleysNameDB = new NameDB("ASSHOLE");
            double halleysSemiMajAxis = 17.834; //AU
            double halleysEccentricity = 0.96714;
            double halleysInclination = 180; //162.26° note retrograde orbit.
            double halleysLoAN = 58.42; //°
            double halleysAoP = 111.33;//°
            double halleysMeanAnomaly = 38.38;//°
            OrbitDB halleysOrbitDB = OrbitDB.FromAsteroidFormat(sun, sunMVDB.MassDry, halleysMVDB.MassDry, halleysSemiMajAxis, halleysEccentricity, halleysInclination, halleysLoAN, halleysAoP, halleysMeanAnomaly, new System.DateTime(1994, 2, 17));
            PositionDB halleysPositionDB = new PositionDB(halleysOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, sun); // + planetPositionDB.AbsolutePosition_AU, sol.ID);
            sensorProfile = new SensorProfileDB();
            Entity halleysComet = Entity.Create();
            sol.AddEntity(halleysComet, new List<BaseDataBlob> { sensorProfile, halleysPositionDB, halleysBodyDB, halleysMVDB, halleysNameDB, halleysOrbitDB });
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, halleysComet);
            SensorTools.PlanetEmmisionSig(sensorProfile, halleysBodyDB, halleysMVDB);

            JPSurveyFactory.GenerateJPSurveyPoints(sol);

            game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(sol.Guid);
            return sol;
        }



        #region Create Sol

        /// <summary>
        /// Creates our own solar system.
        /// This probibly needs to be Json! (since we're getting atmo stuff)
        /// Adds sol to game.StarSystems.
        /// </summary>
        public StarSystem CreateSol(Game game)
        {
            // WIP Function...
            StarSystem sol = new StarSystem();
            sol.Initialize(game, "Sol", -1);
            Entity sun = _starFactory.CreateStar(sol, UniversalConstants.Units.SolarMassInKG, UniversalConstants.Units.SolarRadiusInAu, 4.6E9, "G", 5778, 1, SpectralType.G, "Sol");

            // Planets and their moons
            Entity mercury = SolEntities.Mercury(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, mercury);

            Entity venus = SolEntities.Venus(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, venus);

            Entity earth = SolEntities.Earth(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.HomeworldMineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, earth);
            #region Earth Moon
            //Entity luna = SolEntities.Luna(game, sol, sun, earth, GalaxyGen.Settings.J2000, new SensorProfileDB());
            var luna = SystemBodyFromJsonFactory.Create(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB(), "Data/basemod/bodies/luna.json");
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, luna);
            #endregion

            //Entity mars = SolEntities.Mars(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            var mars = SystemBodyFromJsonFactory.Create(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB(), "Data/basemod/bodies/mars.json");
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, mars);

            Entity jupiter = SolEntities.Jupiter(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, jupiter);
            #region Jupiter Moons
            Entity io = SolEntities.Io(game, sol, sun, jupiter, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, io);

            Entity europa = SolEntities.Europa(game, sol, sun, jupiter, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, europa);

            Entity ganymede = SolEntities.Ganymede(game, sol, sun, jupiter, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, ganymede);

            Entity callisto = SolEntities.Callisto(game, sol, sun, jupiter, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, callisto);

            Entity amalthea = SolEntities.Amalthea(game, sol, sun, jupiter, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, amalthea);

            Entity himalia = SolEntities.Himalia(game, sol, sun, jupiter, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, himalia);

            Entity elara = SolEntities.Elara(game, sol, sun, jupiter, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, elara);

            Entity pasiphae = SolEntities.Pasiphae(game, sol, sun, jupiter, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, pasiphae);

            Entity sinope = SolEntities.Sinope(game, sol, sun, jupiter, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, sinope);

            Entity lysithea = SolEntities.Lysithea(game, sol, sun, jupiter, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, lysithea);
            #endregion

            Entity saturn = SolEntities.Saturn(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, saturn);
            #region Saturn Moons
            Entity mimas = SolEntities.Mimas(game, sol, sun, saturn, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, mimas);

            Entity enceladus = SolEntities.Enceladus(game, sol, sun, saturn, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, enceladus);

            Entity tethys = SolEntities.Tethys(game, sol, sun, saturn, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, tethys);

            Entity dione = SolEntities.Dione(game, sol, sun, saturn, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, dione);

            Entity rhea = SolEntities.Rhea(game, sol, sun, saturn, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, rhea);

            Entity titan = SolEntities.Titan(game, sol, sun, saturn, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, titan);
            #endregion

            Entity uranus = SolEntities.Uranus(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, uranus);

            Entity neptune = SolEntities.Neptune(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, neptune);

            Entity pluto = SolEntities.Pluto(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, pluto);

            Entity haumea = SolEntities.Haumea(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, haumea);

            Entity makemake = SolEntities.Makemake(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, makemake);

            Entity eris = SolEntities.Eris(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, eris);

            Entity ceres = SolEntities.Ceres(game, sol, sun, GalaxyGen.Settings.J2000, new SensorProfileDB());
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, ceres);

            // Comets
            //Entity halleysComet = SolEntities.HalleysComet(game, sol, sun, new System.DateTime(1994, 2, 17), new SensorProfileDB());
            var halleysComet = SystemBodyFromJsonFactory.Create(game, sol, sun, new System.DateTime(1994, 2, 17), new SensorProfileDB(), "Data/basemod/bodies/halleyscomet.json");
            _systemBodyFactory.MineralGeneration(game.StartingGameData.Minerals.Values.ToList(), sol, halleysComet);

            // Clean up cached RNG:
            JPSurveyFactory.GenerateJPSurveyPoints(sol);

            // Go through all the created entities and set them to be neutral
            foreach(var entity in sol.GetAllEntites())
            {
                entity.FactionOwnerID = Game.NeutralFactionId;
            }

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
            StarSystem system = new StarSystem();
            system.Initialize(game, "Eccentricity test", -1);

            Entity sun = _starFactory.CreateStar(system, UniversalConstants.Units.SolarMassInKG, UniversalConstants.Units.SolarRadiusInAu, 4.6E9, "G", 5778, 1, SpectralType.G, "_ecc");

            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();


            double planetSemiMajorAxisAU = 0.387098;
            double planetEccentricity = 0.205630;
            double planetEclipticInclination = 0;
            double planetLoAN = 48.33167;
            double planetAoP = 77.45645;
            double planetMeanAnomaly = 252.25084;


            for (int i = 0; i < 16; i++)
            {
                NameDB planetNameDB = new NameDB("planet"+i);

                SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true };
                MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(3.3022E23, Distance.KmToAU(2439.7));
                PositionDB planetPositionDB = new PositionDB(system.Guid);
                planetEccentricity = i / 16.0;
                OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, GalaxyGen.Settings.J2000);
                planetPositionDB.AbsolutePosition = planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime);
                Entity planet = Entity.Create();
                system.AddEntity(planet, new List<BaseDataBlob> { planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB });
            }
            game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(system.Guid);
            return system;
        }

        /// <summary>
        /// Creates an test system with planets of varying longitude of periapsis.
        /// Adds to game.StarSystem.
        /// </summary>
        public StarSystem CreateLongitudeTest(Game game) {
            StarSystem system = new StarSystem();
            system.Initialize(game, "Longitude test", -1);

            Entity sun = _starFactory.CreateStar(system, UniversalConstants.Units.SolarMassInKG, UniversalConstants.Units.SolarRadiusInAu, 4.6E9, "G", 5778, 1, SpectralType.G, "_lop");

            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            double planetSemiMajorAxisAU = 0.387098;
            double planetEccentricity = 0.9;// 0.205630;
            double planetEclipticInclination = 0;
            double planetLoAN = 48.33167;
            double planetAoP = 77.45645;
            double planetMeanAnomaly = 252.25084;


            for (int i = 0; i < 13; i++)
            {
                NameDB planetNameDB = new NameDB("planet"+i);
                SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true };
                MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(3.3022E23, Distance.KmToAU(2439.7));
                PositionDB planetPositionDB = new PositionDB(system.Guid);
                planetAoP = i * 15;
                OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, GalaxyGen.Settings.J2000);
                planetPositionDB.AbsolutePosition = planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime);
                Entity planet = Entity.Create();
                system.AddEntity(planet, new List<BaseDataBlob> { planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB });
            }
            game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(system.Guid);
            return system;
        }

        #endregion

    }
}
