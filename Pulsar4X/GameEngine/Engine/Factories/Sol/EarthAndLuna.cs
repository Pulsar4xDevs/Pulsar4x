using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Blueprints;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Sol
{
    public static partial class SolEntities
    {
        public static Entity Earth(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB {
                BodyType = BodyType.Terrestrial,
                SupportsPopulations = true,
                Albedo = 0.306f,
                Gravity = 9.8,
                LengthOfDay = TimeSpan.FromHours(24),
                AxialTilt = 23.439f,
                MagneticField = 45, // 25 to 65 according to: https://en.wikipedia.org/wiki/Earth%27s_magnetic_field
                RadiationLevel = 0,
                AtmosphericDust = 0
            };

            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(5.9726E24, Distance.KmToAU(6378.1));
            NameDB planetNameDB = new NameDB("Earth");
            double planetSemiMajorAxisAU = 1.00000011;
            double planetEccentricity = 0.01671022;
            double planetEclipticInclination = 0;
            double planetLoAN = -11.26064;
            double planetAoP = 102.94719;
            double planetMeanAnomaly = 100.46435;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            planetBodyDB.Tectonics = TectonicActivity.EarthLike;
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, sun);

            var pressureAtm = 1.0f;
            var atmoGasses = new Dictionary<string, float>
            {
                { game.GetGasBySymbol("N2").UniqueID, 0.78f * pressureAtm },
                { game.GetGasBySymbol("O2").UniqueID, 0.12f * pressureAtm },
                { game.GetGasBySymbol("H2O").UniqueID, 0.01f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, true, 71, 1f, 1f, 57.2f, atmoGasses);

            Entity planet = Entity.Create();
            sol.AddEntity(planet, new List<BaseDataBlob> { planetNameDB, sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetOrbitDB, planetAtmosphereDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }


        public static Entity Luna(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(7.34767309E22, Distance.KmToAU(6378.1));
            NameDB moonNameDB = new NameDB("Luna");
            double moonSemiMajorAxisAU = Distance.KmToAU(0.3844E6);
            double moonEccentricity = 0.0549;
            double moonEclipticInclination = 0; // 5.1

            // Next three values are unimportant. Luna's LoAN and AoP regress/progress by one revolution every 18.6/8.85 years respectively.
            // Our orbit code it not advanced enough to deal with LoAN/AoP regression/progression.
            double moonLoAN = 125.08;
            double moonAoP = 318.0634;
            double moonMeanAnomaly = 115.3654;

            OrbitDB moonOrbitDB = OrbitDB.FromAsteroidFormat(parentPlanet, planetMVDB.MassDry, moonMVDB.MassDry, moonSemiMajorAxisAU, moonEccentricity, moonEclipticInclination, moonLoAN, moonAoP, moonMeanAnomaly, epoch);
            moonBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbit); //yes, using parent planet orbit here, since this is the DB it calculates the average distance from.
            PositionDB moonPositionDB = new PositionDB(moonOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, parentPlanet);

            Entity moon = Entity.Create();
            sol.AddEntity(moon, new List<BaseDataBlob> { moonNameDB, sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonOrbitDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }
    }
}
