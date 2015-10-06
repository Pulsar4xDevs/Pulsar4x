using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    internal static class ShipMovementProcessor
    {
        public static void Initialize()
        {
        }

        /// <summary>
        /// Sets a ships position.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="systems"></param>
        /// <param name="deltaSeconds"></param>
        public static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            foreach (var system in systems)
            {
                foreach (Entity shipEntity in system.SystemManager.GetAllEntitiesWithDataBlob<PropulsionDB>())
                {
                    //TODO: do we need to check if the ship has an orbitDB?
                    shipEntity.GetDataBlob<PositionDB>().Position += shipEntity.GetDataBlob<PropulsionDB>().CurrentSpeed * deltaSeconds;
                    //TODO: use fuel.
                }
            }
        }


        /// <summary>
        /// recalculates a shipsMaxSpeed.
        /// </summary>
        /// <param name="ship"></param>
        public static void CalcMaxSpeed(Entity ship)
        {
            int totalEnginePower = 0;

            List<Entity> engineEntities = ship.GetDataBlob<ShipInfoDB>().ComponentList.Where(item => item.HasDataBlob<EnginePowerDB>()).ToList();
            foreach (var engine in engineEntities)
            {
                //todo check if it's damaged
                totalEnginePower += engine.GetDataBlob<EnginePowerDB>().EnginePower;
            }

            //Note: TN aurora uses the TCS for max speed calcs. 
            ship.GetDataBlob<PropulsionDB>().MaximumSpeed = (int)(totalEnginePower / ship.GetDataBlob<ShipInfoDB>().Tonnage) * 20;
        }
    }
}