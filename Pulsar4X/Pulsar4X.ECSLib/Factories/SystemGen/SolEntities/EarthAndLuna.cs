using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        public static Entity Earth(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true, Albedo = 0.306f };
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
            PositionDB planetPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(planetOrbitDB, StaticRefLib.CurrentDateTime), sol.Guid, sun);

            var pressureAtm = 1.0f;
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>
            {
                { game.StaticData.GetAtmosGasBySymbol("N2"), 0.78f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("O2"), 0.12f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("H2O"), 0.01f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, true, 71, 1f, 1f, 57.2f, atmoGasses); 

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }


        public static Entity Luna(Game game, StarSystem sol, Entity sun, Entity parentPlanet, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB planetMVDB = parentPlanet.GetDataBlob<MassVolumeDB>();
            OrbitDB planetOrbit = parentPlanet.GetDataBlob<OrbitDB>();
            PositionDB planetPositionDB = parentPlanet.GetDataBlob<PositionDB>();

            SystemBodyInfoDB moonBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Moon, SupportsPopulations = true };
            MassVolumeDB moonMVDB = MassVolumeDB.NewFromMassAndRadius_AU(5.9726E24, Distance.KmToAU(6378.1));
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
            PositionDB moonPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(moonOrbitDB, StaticRefLib.CurrentDateTime) + planetPositionDB.AbsolutePosition_AU, sol.Guid, parentPlanet);

            Entity moon = new Entity(sol, new List<BaseDataBlob> { sensorProfile, moonPositionDB, moonBodyDB, moonMVDB, moonNameDB, moonOrbitDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, moonBodyDB, moonMVDB);
            return moon;
        }
    }
}
