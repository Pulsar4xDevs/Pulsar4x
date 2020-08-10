using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public static class StarSystemExtensions
    {
        public static int GetNumberOfStars(this StarSystem system)
        {
            return system.GetAllEntitiesWithDataBlob<StarInfoDB>().Count();
        }

        public static int GetNumberOfBodiesOfType(this StarSystem system, BodyType type)
        {
            return system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>().Where(x => x.GetDataBlob<SystemBodyInfoDB>().BodyType == type).Count();
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

    }
}
