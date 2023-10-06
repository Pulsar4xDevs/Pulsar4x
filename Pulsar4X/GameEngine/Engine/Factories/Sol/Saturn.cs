using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Blueprints;
using System.Linq;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Sol
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Saturn
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Saturn</remarks>
        public static Entity Saturn(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.GasGiant, SupportsPopulations = false, Albedo = 0.342f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(5.6834E14, Distance.KmToAU(60268));
            NameDB planetNameDB = new NameDB("Saturn");

            double planetSemiMajorAxisAU = 9.5826;
            double planetEccentricity = 0.0565;
            double planetEclipticInclination = 0;   //2.485;
            double planetLoAN = 113.665;
            double planetAoP = 339.392;
            double planetMeanAnomaly = 317.020;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.KPaToAtm(140f);
            var atmoGasses = new Dictionary<string, float>
            {
                { game.GetGasBySymbol("H2").UniqueID,  0.963f * pressureAtm },
                { game.GetGasBySymbol("He").UniqueID,  0.0325f * pressureAtm },
                { game.GetGasBySymbol("CH4").UniqueID, 0.0045f * pressureAtm },
                { game.GetGasBySymbol("NH3").UniqueID, 0.000125f * pressureAtm },
                { game.GetGasBySymbol("HD").UniqueID,  0.000110f * pressureAtm },
                { game.GetGasBySymbol("C2H6").UniqueID,0.000007f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -139, atmoGasses);

            Entity planet = Entity.Create();
            sol.AddEntity(planet, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }

        /// <summary>
        /// Creates Saturn's 1st moon Mimas
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Mimas_(moon)</remarks>
        public static Entity Mimas(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true, Albedo = 0.962f };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(3.7493E19, Distance.KmToAU(198.2));
            NameDB moonNameDB = new NameDB("Mimas");
            double moonSemiMajorAxisAU = Distance.KmToAU(185539);
            double moonEccentricity = 0.0196;
            double moonEclipticInclination = 0; // 1.574

            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression.
            double moonLoAN = 123;
            double moonAoP = 123;
            double moonMeanAnomaly = 123;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(moonOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime) + planetPositionDB.AbsolutePosition, sol.Guid, parentPlanet);

            Entity moon = Entity.Create();
            sol.AddEntity(moon, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }

        /// <summary>
        /// Creates Saturn's 2nd moon Enceladus
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Enceladus_(moon)</remarks>
        public static Entity Enceladus(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true, Albedo = 0.81f };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(1.0822E20, Distance.KmToAU(252.1));
            NameDB moonNameDB = new NameDB("Enceladus");
            double moonSemiMajorAxisAU = Distance.KmToAU(237948);
            double moonEccentricity = 0.0047;
            double moonEclipticInclination = 0; // 0.009

            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression.
            double moonLoAN = 123;
            double moonAoP = 123;
            double moonMeanAnomaly = 123;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(moonOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime) + planetPositionDB.AbsolutePosition, sol.Guid, parentPlanet);

            Entity moon = Entity.Create();
            sol.AddEntity(moon, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }

        /// <summary>
        /// Creates Saturn's 3rd moon Tethys
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Tethys_(moon)</remarks>
        public static Entity Tethys(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true, Albedo = 0.80f };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(6.17449E20, Distance.KmToAU(531.1));
            NameDB moonNameDB = new NameDB("Tethys");
            double moonSemiMajorAxisAU = Distance.KmToAU(294619);
            double moonEccentricity = 0.0001;
            double moonEclipticInclination = 0; // 1.12

            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression.
            double moonLoAN = 123;
            double moonAoP = 123;
            double moonMeanAnomaly = 123;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(moonOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime) + planetPositionDB.AbsolutePosition, sol.Guid, parentPlanet);

            Entity moon = Entity.Create();
            sol.AddEntity(moon, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }

        /// <summary>
        /// Creates Saturn's 4th moon Dione
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Dione_(moon)</remarks>
        public static Entity Dione(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true, Albedo = 0.5f };     // Bond Albedo is a guess as only have Geometric Albedo value of 0.998 known
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(1.095452E21, Distance.KmToAU(561.4));
            NameDB moonNameDB = new NameDB("Dione");
            double moonSemiMajorAxisAU = Distance.KmToAU(377396);
            double moonEccentricity = 0.0022;
            double moonEclipticInclination = 0; // 0.019

            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression.
            double moonLoAN = 123;
            double moonAoP = 123;
            double moonMeanAnomaly = 123;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(moonOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime) + planetPositionDB.AbsolutePosition, sol.Guid, parentPlanet);

            Entity moon = Entity.Create();
            sol.AddEntity(moon, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }

        /// <summary>
        /// Creates Saturn's 5th moon Rhea
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Rhea_(moon)</remarks>
        public static Entity Rhea(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true, Albedo = 0.49f };     // Bond Albedo is a guess as only have Geometric Albedo value of 0.949 known
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(2.306518E21, Distance.KmToAU(763.8));
            NameDB moonNameDB = new NameDB("Rhea");
            double moonSemiMajorAxisAU = Distance.KmToAU(527108);
            double moonEccentricity = 0.0012583;
            double moonEclipticInclination = 0; // 0.345

            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression.
            double moonLoAN = 123;
            double moonAoP = 123;
            double moonMeanAnomaly = 123;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(moonOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime) + planetPositionDB.AbsolutePosition, sol.Guid, parentPlanet);

            Entity moon = Entity.Create();
            sol.AddEntity(moon, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }

        /// <summary>
        /// Creates Saturn's 6th moon Titan
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Titan_(moon)</remarks>
        public static Entity Titan(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true, Albedo = 0.22f };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(1.3452E23, Distance.KmToAU(2574.73));
            NameDB moonNameDB = new NameDB("Titan");
            double moonSemiMajorAxisAU = Distance.KmToAU(1221870);
            double moonEccentricity = 0.0288;
            double moonEclipticInclination = 0; // 0.34854

            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression.
            double moonLoAN = 123;
            double moonAoP = 123;
            double moonMeanAnomaly = 123;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(moonOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime) + planetPositionDB.AbsolutePosition, sol.Guid, parentPlanet);

            var pressureAtm = Pressure.KPaToAtm(146.7f);
            var atmoGasses = new Dictionary<string, float>
            {
                { game.GetGasBySymbol("N2").UniqueID, 0.97f * pressureAtm },
                { game.GetGasBySymbol("CH4").UniqueID, 0.028f * pressureAtm },
                { game.GetGasBySymbol("H2").UniqueID, 0.002f * pressureAtm }
            };
            AtmosphereDB moonAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -179.5f, atmoGasses);

            Entity moon = Entity.Create();
            sol.AddEntity(moon, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB, moonAtmosphereDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }
    }
}
