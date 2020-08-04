using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.ECSLib.Factories.SystemGen
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
            PositionDB planetPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(planetOrbitDB, StaticRefLib.CurrentDateTime), sol.Guid, sun);

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
