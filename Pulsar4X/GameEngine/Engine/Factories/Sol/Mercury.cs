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
        /// Creates Mercury
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Mercury_(planet)</remarks>
        public static Entity Mercury(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true, Albedo = 0.068f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(3.3022E23, Distance.KmToAU(2439.7));
            NameDB planetNameDB = new NameDB("Mercury");

            double planetSemiMajorAxisAU = 0.387098;
            double planetEccentricity = 0.205630;
            double planetEclipticInclination = 0;
            double planetLoAN = 48.33167;
            double planetAoP = 77.45645;
            double planetMeanAnomaly = 252.25084;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            planetBodyDB.Tectonics = TectonicActivity.EarthLike;
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.ID, sun);

            var geoSurveyable = new GeoSurveyableDB()
            {
                PointsRequired = 575
            };

            Entity planet = Entity.Create();
            sol.AddEntity(planet, new List<BaseDataBlob> { planetNameDB, sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetOrbitDB, geoSurveyable, new ColonizeableDB() });
            SensorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
