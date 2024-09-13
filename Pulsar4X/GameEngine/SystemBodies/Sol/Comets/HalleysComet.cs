using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using System.Text;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Sol
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Halleys Comet
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Halley%27s_Comet</remarks>
        public static Entity HalleysComet(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB cometBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Comet, SupportsPopulations = false, Albedo = 0.04f };
            MassVolumeDB cometMVDB = MassVolumeDB.NewFromMassAndRadius_AU(2.2E14, Distance.KmToAU(11));
            NameDB cometNameDB = new NameDB("Halleys Comet");

            double cometSemiMajAxis = 17.834;
            double cometEccentricity = 0.96714;
            double cometInclination = 180;
            double cometLoAN = 58.42;
            double cometLoP = 111.33;
            double cometMeanAnomaly = 38.38;//°

            OrbitDB cometOrbitDB = OrbitDB.FromAsteroidFormat(sun, sunMVDB.MassDry, cometMVDB.MassDry, cometSemiMajAxis, cometEccentricity, cometInclination, cometLoAN, cometLoP, cometMeanAnomaly, epoch);
            cometBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, cometOrbitDB);
            PositionDB cometPositionDB = new PositionDB(cometOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.ID, sun);

            var geoSurveyable = new GeoSurveyableDB()
            {
                PointsRequired = 575
            };

            Entity comet = Entity.Create();
            sol.AddEntity(comet, new List<BaseDataBlob> { cometNameDB, sensorProfile, cometPositionDB, cometBodyDB, cometMVDB, cometOrbitDB, geoSurveyable });
            return comet;
        }
    }
}
