using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Ceres
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Ceres_(dwarf_planet)</remarks>
        public static Entity Ceres(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.DwarfPlanet, SupportsPopulations = true };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(9.3835E20, Distance.KmToAU(469.73));
            NameDB planetNameDB = new NameDB("Ceres");

            double planetSemiMajorAxisAU = 2.7691651545;
            double planetEccentricity = 0.07600902910;
            double planetEclipticInclination = 0;   // 10.59406704
            double planetLoAN = 80.3055316;
            double planetAoP = 73.5976941;
            double planetMeanAnomaly = 77.37209589;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            PositionDB planetPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(planetOrbitDB, StaticRefLib.CurrentDateTime), sol.Guid, sun);

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
