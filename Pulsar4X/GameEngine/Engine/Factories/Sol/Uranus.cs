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
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.BarToAtm(1000f);         // https://nssdc.gsfc.nasa.gov/planetary/factsheet/uranusfact.html#:~:text=Surface%20Pressure%3A%20%3E%3E1000%20bars,)%20%2D%2082.5%25%20(3.3%25)%3B
            var atmoGasses = new Dictionary<string, float>
            {
                { game.GetGasBySymbol("H2").UniqueID,  0.83f * pressureAtm },
                { game.GetGasBySymbol("He").UniqueID,  0.15f * pressureAtm },
                { game.GetGasBySymbol("CH4").UniqueID, 0.023f * pressureAtm },
                { game.GetGasBySymbol("HD").UniqueID,  0.00009f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -197.2f, atmoGasses);

            Entity planet = Entity.Create();
            sol.AddEntity(planet, new List<BaseDataBlob> { planetNameDB, sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetOrbitDB, planetAtmosphereDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
