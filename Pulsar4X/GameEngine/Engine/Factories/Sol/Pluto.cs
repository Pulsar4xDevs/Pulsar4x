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
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.PaToAtm(1f);
            var atmoGasses = new Dictionary<string, float>
            {
                { game.GetGasBySymbol("N2").UniqueID, 0.99f * pressureAtm },
                { game.GetGasBySymbol("CH4").UniqueID, 0.005f * pressureAtm },
                { game.GetGasBySymbol("CO").UniqueID, 0.0025f * pressureAtm },
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -229, atmoGasses);

            var geoSurveyable = new GeoSurveyableDB()
            {
                PointsRequired = 575
            };

            Entity planet = Entity.Create();
            sol.AddEntity(planet, new List<BaseDataBlob> { planetNameDB, sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetOrbitDB, planetAtmosphereDB, geoSurveyable });
            SensorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
