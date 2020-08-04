using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Pluto
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Pluto</remarks>
        public static Entity Pluto(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.DwarfPlanet, SupportsPopulations = true, Albedo = 0.49f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(1.303E22, Distance.KmToAU(1188.3));
            NameDB planetNameDB = new NameDB("Pluto");

            double planetSemiMajorAxisAU = 39.482;
            double planetEccentricity = 0.2488;
            double planetEclipticInclination = 0;   // 1.85
            double planetLoAN = 110.299;
            double planetAoP = 113.834;
            double planetMeanAnomaly = 14.53;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            PositionDB planetPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(planetOrbitDB, StaticRefLib.CurrentDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.PaToAtm(1f);
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>
            {
                { game.StaticData.GetAtmosGasBySymbol("N2"), 0.99f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("CH4"), 0.005f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("CO"), 0.0025f * pressureAtm },
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -229, atmoGasses); 

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
