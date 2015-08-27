using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    internal static class ShipMovementProcessor
    {
        public static void Initialize()
        {
        }

        public static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            foreach (var system in systems)
            {
                foreach (Entity shipEntity in system.SystemManager.GetAllEntitiesWithDataBlob<PropulsionDB>())
                {
                    //TODO: do we need to check if the ship has an orbitDB?
                    shipEntity.GetDataBlob<PositionDB>().Position += shipEntity.GetDataBlob<PropulsionDB>().CurrentSpeed * deltaSeconds;
                }
            }
        }
    }
}