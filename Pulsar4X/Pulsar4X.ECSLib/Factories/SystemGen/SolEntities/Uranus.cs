using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Uranus
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Uranus</remarks>
        public static Entity Uranus(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.IceGiant, SupportsPopulations = true, Albedo = 0.300f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(8.6810E25, Distance.KmToAU(25559));
            NameDB planetNameDB = new NameDB("Uranus");

            double planetSemiMajorAxisAU = 19.2184;
            double planetEccentricity = 0.046381;
            double planetEclipticInclination = 0;   //0.773;
            double planetLoAN = 74.006;
            double planetAoP = 96.9998857;
            double planetMeanAnomaly = 142.238600;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            PositionDB planetPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(planetOrbitDB, StaticRefLib.CurrentDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.BarToAtm(1000f);         // https://nssdc.gsfc.nasa.gov/planetary/factsheet/uranusfact.html#:~:text=Surface%20Pressure%3A%20%3E%3E1000%20bars,)%20%2D%2082.5%25%20(3.3%25)%3B
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>
            {
                { game.StaticData.GetAtmosGasBySymbol("H2"),  0.83f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("He"),  0.15f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("CH4"), 0.023f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("HD"),  0.00009f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -197.2f, atmoGasses);

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
