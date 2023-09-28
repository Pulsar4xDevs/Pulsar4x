using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Modding;

namespace Pulsar4X.Extensions
{
    public static class StarSystemExtensions
    {
        public static List<Entity> GetStarsSortedByDryMass(this StarSystem system)
        {
            List<Entity> stars = system.GetAllEntitiesWithDataBlob<StarInfoDB>();

            stars = stars.OrderByDescending(x => x.GetDataBlob<MassVolumeDB>().MassDry).ToList();

            return stars;
        }

        public static int GetNumberOfStars(this StarSystem system)
        {
            return system.GetAllEntitiesWithDataBlob<StarInfoDB>().Count();
        }

        public static int GetNumberOfBodies(this StarSystem system)
        {
            return system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>().Count();
        }

        public static int GetNumberOfBodiesOfType(this StarSystem system, BodyType type)
        {
            return system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>().Where(x => x.GetDataBlob<SystemBodyInfoDB>().BodyType == type).Count();
        }

        public static List<Entity> GetEntitiesOfAllBodiesOfType(this StarSystem system, BodyType type)
        {
            return system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>().Where(x => x.GetDataBlob<SystemBodyInfoDB>().BodyType == type).ToList();
        }

        public static int GetNumberOfAsteroids(this StarSystem system)
        {
            return system.GetNumberOfBodiesOfType(BodyType.Asteroid);
        }

        public static int GetNumberOfComets(this StarSystem system)
        {
            return system.GetNumberOfBodiesOfType(BodyType.Comet);
        }

        public static int GetNumberOfDwarfPlanets(this StarSystem system)
        {
            return system.GetNumberOfBodiesOfType(BodyType.DwarfPlanet);
        }

        public static int GetNumberOfGasDwarves(this StarSystem system)
        {
            return system.GetNumberOfBodiesOfType(BodyType.GasDwarf);
        }

        public static int GetNumberOfGasGiants(this StarSystem system)
        {
            return system.GetNumberOfBodiesOfType(BodyType.GasGiant);
        }

        public static int GetNumberOfIceGiants(this StarSystem system)
        {
            return system.GetNumberOfBodiesOfType(BodyType.IceGiant);
        }

        public static int GetNumberOfMoons(this StarSystem system)
        {
            return system.GetNumberOfBodiesOfType(BodyType.Moon);
        }

        public static int GetNumberOfTerrestrialPlanets(this StarSystem system)
        {
            return system.GetNumberOfBodiesOfType(BodyType.Terrestrial);
        }

        public static int GetNumberOfUnknownObjects(this StarSystem system)
        {
            return system.GetNumberOfBodiesOfType(BodyType.Unknown);
        }


        public static Dictionary<string, double> GetTotalSystemMinerals(this StarSystem system, ModDataStore dataStore)
        {
            var minerals = new Dictionary<int, double>();
            var bodies = system.GetAllEntitiesWithDataBlob<MineralsDB>().Select(x => x.GetDataBlob<MineralsDB>());
            foreach (var body in bodies)
            {
                foreach (var kvp in body.Minerals)
                {
                    if (!minerals.ContainsKey(kvp.Key))
                    {
                        minerals.Add(kvp.Key, kvp.Value.Amount * 1.0);
                    }
                    else
                    {
                        minerals[kvp.Key] += kvp.Value.Amount * 1.0;
                    }
                }
            }

            var mineralList = dataStore.Minerals.Values.ToList();
            var mineralsByName = minerals.ToDictionary(k => mineralList.First(m => m.ID == k.Key).Name, v => v.Value);
            return mineralsByName;
        }

    }
}