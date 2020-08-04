using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Venus
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Venus</remarks>
        public static Entity Venus(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true, Albedo = 0.77f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(4.8676E24, Distance.KmToAU(6051.8));
            NameDB planetNameDB = new NameDB("Venus");

            double planetSemiMajorAxisAU = 0.72333199;
            double planetEccentricity = 0.00677323;
            double planetEclipticInclination = 0;
            double planetLoAN = 76.68069;
            double planetAoP = 131.53298;
            double planetMeanAnomaly = 181.97973;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            planetBodyDB.Tectonics = TectonicActivity.EarthLike;
            PositionDB planetPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(planetOrbitDB, StaticRefLib.CurrentDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.BarToAtm(92f);
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>
            {
                { game.StaticData.GetAtmosGasBySymbol("CO2"), 0.965f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("N2"), 0.035f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("SO2"), 0.00015f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("Ar"), 0.00007f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("H2O"), 0.00002f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("CO"), 0.000017f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("He"), 0.000012f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("Ne"), 0.000007f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, 464f, atmoGasses); 

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
