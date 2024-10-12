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
        /// Creates Haumea
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Haumea</remarks>
        public static Entity Haumea(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.DwarfPlanet, SupportsPopulations = true };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(4.006E21, Distance.KmToAU(798));
            NameDB planetNameDB = new NameDB("Haumea");

            double planetSemiMajorAxisAU = 43.182;
            double planetEccentricity = 0.19489;
            double planetEclipticInclination = 0;   // 28.214
            double planetLoAN = 122.163;
            double planetAoP = 238.778;
            double planetMeanAnomaly = 217.774;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sun);

            var geoSurveyable = new GeoSurveyableDB()
            {
                PointsRequired = 575
            };

            Entity planet = Entity.Create();
            sol.AddEntity(planet, new List<BaseDataBlob> { planetNameDB, sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetOrbitDB, geoSurveyable });
            SensorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
