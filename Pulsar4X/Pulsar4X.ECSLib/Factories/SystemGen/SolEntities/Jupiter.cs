using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Jupiter
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Jupiter</remarks>
        public static Entity Jupiter(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.GasGiant, SupportsPopulations = false, Albedo = 0.503f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(1.8982E27, Distance.KmToAU(71492));
            NameDB planetNameDB = new NameDB("Jupiter");

            double planetSemiMajorAxisAU = 5.2044;
            double planetEccentricity = 0.0489;
            double planetEclipticInclination = 0;   //1.303;
            double planetLoAN = 100.464;
            double planetAoP = 273.867;
            double planetMeanAnomaly = 20.020;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            PositionDB planetPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(planetOrbitDB, StaticRefLib.CurrentDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.KPaToAtm(200f);
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>
            {
                { game.StaticData.GetAtmosGasBySymbol("H2"), 0.89f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("He"), 0.10f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("CH4"), 0.003f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("NH3"), 0.00026f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("HD"), 0.000028f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("C2H6"), 0.000006f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("H2O"), 0.000004f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -108, atmoGasses);

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
