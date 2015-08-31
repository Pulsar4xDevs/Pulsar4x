using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    
    internal static class EnginePowerProcessor
    {
        public delegate void StatRecalc(Entity ship);


        public static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            
        }

        /// <summary>
        /// This need not run every tick, that would be wastefull. 
        /// How are we going to call this (and other simular processors) on component damage?
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
            //TODO: NutonionAurora will use Mass.(will have to figure out what math makes sense for the whole calc)
            ship.GetDataBlob<PropulsionDB>().MaximumSpeed = (totalEnginePower / ship.GetDataBlob<SensorProfileDB>().TotalCrossSection) * 1000;
            
        }
    }
}