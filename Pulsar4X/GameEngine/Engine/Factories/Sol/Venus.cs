using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Blueprints;
using Pulsar4X.Extensions;
using System.Linq;

namespace Pulsar4X.Engine.Sol
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
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.BarToAtm(92f);
            var atmoGasses = new Dictionary<string, float>
            {
                { game.GetGasBySymbol("CO2").UniqueID, 0.965f * pressureAtm },
                { game.GetGasBySymbol("N2").UniqueID, 0.035f * pressureAtm },
                { game.GetGasBySymbol("SO2").UniqueID, 0.00015f * pressureAtm },
                { game.GetGasBySymbol("Ar").UniqueID, 0.00007f * pressureAtm },
                { game.GetGasBySymbol("H2O").UniqueID, 0.00002f * pressureAtm },
                { game.GetGasBySymbol("CO").UniqueID, 0.000017f * pressureAtm },
                { game.GetGasBySymbol("He").UniqueID, 0.000012f * pressureAtm },
                { game.GetGasBySymbol("Ne").UniqueID, 0.000007f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, 464f, atmoGasses);

            Entity planet = Entity.Create();
            sol.AddEntity(planet, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
