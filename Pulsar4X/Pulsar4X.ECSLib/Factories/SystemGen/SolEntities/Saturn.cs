using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Saturn
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Saturn</remarks>
        public static Entity Saturn(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.GasGiant, SupportsPopulations = false, Albedo = 0.342f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(5.6834E14, Distance.KmToAU(60268));
            NameDB planetNameDB = new NameDB("Saturn");

            double planetSemiMajorAxisAU = 9.5826;
            double planetEccentricity = 0.0565;
            double planetEclipticInclination = 0;   //2.485;
            double planetLoAN = 113.665;
            double planetAoP = 339.392;
            double planetMeanAnomaly = 317.020;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition_AU(StaticRefLib.CurrentDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.KPaToAtm(140f);
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>
            {
                { game.StaticData.GetAtmosGasBySymbol("H2"),  0.963f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("He"),  0.0325f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("CH4"), 0.0045f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("NH3"), 0.000125f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("HD"),  0.000110f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("C2H6"),0.000007f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -139, atmoGasses);

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
