using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    internal static class ShipDamageProcessor
    {
        public static void Initialize()
        {
        }

        public static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {

        }

        public static void OnTakingDamage(Entity ship)
        {
            foreach (var componentEntity in ship.GetDataBlob<ShipInfoDB>().ComponentList)
            {
                ComponentInfoDB componentInfo = componentEntity.GetDataBlob<ComponentInfoDB>();
                componentInfo.StatRecalcDelegate.DynamicInvoke(ship);
            }
            
        }

    }
}