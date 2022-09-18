using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Eris
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Eris_(dwarf_planet)</remarks>
        public static Entity Eris(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.DwarfPlanet, SupportsPopulations = true, Albedo = 0.96f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(1.66E22, Distance.KmToAU(1163));
            NameDB planetNameDB = new NameDB("Eris");

            double planetSemiMajorAxisAU = 67.864;
            double planetEccentricity = 0.43607;
            double planetEclipticInclination = 0;   // 44.040
            double planetLoAN = 35.951;
            double planetAoP = 151.639;
            double planetMeanAnomaly = 205.989;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(StaticRefLib.CurrentDateTime), sol.Guid, sun);

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
