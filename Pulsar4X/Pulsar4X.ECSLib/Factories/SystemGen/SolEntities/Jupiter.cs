using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Jupiter
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Jupiter</remarks>
        public static Entity Jupiter(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.GasGiant, SupportsPopulations = false, Albedo = 0.503f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(1.8982E27, Distance.KmToAU(71492));
            NameDB planetNameDB = new NameDB("Jupiter");

            double planetSemiMajorAxisAU = 5.2044;
            double planetEccentricity = 0.0489;
            double planetEclipticInclination = 0;   //1.303;
            double planetLoAN = 100.464;
            double planetAoP = 273.867;
            double planetMeanAnomaly = 20.020;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            PositionDB planetPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(planetOrbitDB, StaticRefLib.CurrentDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.KPaToAtm(200f);
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>
            {
                { game.StaticData.GetAtmosGasBySymbol("H2"), 0.89f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("He"), 0.10f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("CH4"), 0.003f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("NH3"), 0.00026f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("HD"), 0.000028f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("C2H6"), 0.000006f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("H2O"), 0.000004f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -108, atmoGasses);

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }


        /// <summary>
        /// Creates Juipiter's 1st moon Io
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Io_(moon)</remarks>
        public static Entity Io(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true, Albedo = 0.63f };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(8.931938E22, Distance.KmToAU(1821.6));
            NameDB moonNameDB = new NameDB("Io");
            double moonSemiMajorAxisAU = Distance.KmToAU(421700);
            double moonEccentricity = 0.0041;
            double moonEclipticInclination = 0; // 2.213

            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression. 
            double moonLoAN = 123;
            double moonAoP = 123;
            double moonMeanAnomaly = 123;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(moonOrbitDB, StaticRefLib.CurrentDateTime) + planetPositionDB.AbsolutePosition_AU, sol.Guid, sun);

            Entity moon = new Entity(sol, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }


        /// <summary>
        /// Creates Juipiter's 2nd moon Europa
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Europa_(moon)</remarks>
        public static Entity Europa(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true, Albedo = 0.67f };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(4.799844E22, Distance.KmToAU(1560.8));
            NameDB moonNameDB = new NameDB("Europa");
            double moonSemiMajorAxisAU = Distance.KmToAU(670900);
            double moonEccentricity = 0.009;
            double moonEclipticInclination = 0; // 1.791

            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression. 
            double moonLoAN = 123;
            double moonAoP = 123;
            double moonMeanAnomaly = 123;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(moonOrbitDB, StaticRefLib.CurrentDateTime) + planetPositionDB.AbsolutePosition_AU, sol.Guid, sun);

            Entity moon = new Entity(sol, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }


        /// <summary>
        /// Creates Juipiter's 3rd moon Ganymede
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Ganymede_(moon)</remarks>
        public static Entity Ganymede(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true, Albedo = 0.43f };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(1.4819E23, Distance.KmToAU(2634.1));
            NameDB moonNameDB = new NameDB("Ganymede");
            double moonSemiMajorAxisAU = Distance.KmToAU(1070400);
            double moonEccentricity = 0.0013;
            double moonEclipticInclination = 0; // 2.214

            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression. 
            double moonLoAN = 123;
            double moonAoP = 123;
            double moonMeanAnomaly = 123;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(moonOrbitDB, StaticRefLib.CurrentDateTime) + planetPositionDB.AbsolutePosition_AU, sol.Guid, sun);

            Entity moon = new Entity(sol, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }


        /// <summary>
        /// Creates Juipiter's 4th moon Callisto
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Callisto_(moon)</remarks>
        public static Entity Callisto(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true, Albedo = 0.22f };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(1.075938E23, Distance.KmToAU(2410.3));
            NameDB moonNameDB = new NameDB("Callisto");
            double moonSemiMajorAxisAU = Distance.KmToAU(1882700);
            double moonEccentricity = 0.0074;
            double moonEclipticInclination = 0; // 2.017

            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression. 
            double moonLoAN = 123;
            double moonAoP = 123;
            double moonMeanAnomaly = 123;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(moonOrbitDB, StaticRefLib.CurrentDateTime) + planetPositionDB.AbsolutePosition_AU, sol.Guid, sun);

            Entity moon = new Entity(sol, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }
    }
}
