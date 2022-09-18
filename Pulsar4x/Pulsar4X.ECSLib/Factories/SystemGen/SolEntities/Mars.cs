using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Mars
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Mars</remarks>
        public static Entity Mars(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true, Albedo = 0.25f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(0.64174E24, Distance.KmToAU(3396.2));
            NameDB planetNameDB = new NameDB("Mars");

            double planetSemiMajorAxisAU = Distance.KmToAU(227.92E6);
            double planetEccentricity = 0.0934;
            double planetEclipticInclination = 0;   // 1.85
            double planetLoAN = 49.57854;
            double planetAoP = 336.04084;
            double planetMeanAnomaly = 355.45332;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            planetBodyDB.Tectonics = TectonicActivity.EarthLike;
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(StaticRefLib.CurrentDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.KPaToAtm(0.87f);
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>
            {
                { game.StaticData.GetAtmosGasBySymbol("CO2"), 0.9597f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("Ar"), 0.0193f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("N2"), 0.0189f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("O2"), 0.00146f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("CO"), 0.000557f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("H2O"), 0.000210f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("NO"), 0.0001f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("Ne"), 0.0000025f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("Kr"), 0.0000003f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("Xe"), 0.0000001f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -63, atmoGasses); 

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
