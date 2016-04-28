using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    //This processor takes an entity and recalcs for each of the datablobs in that entity.
    //as an ability is added to the game, it's recalc processor should be linked here.
    internal static class ReCalcProcessor
    {
        private static Entity CurrentEntity { get; set; }
        internal static Dictionary<Type, Delegate> TypeProcessorMap = new Dictionary<Type, Delegate>
            {
                { typeof(ColonyMinesDB), new Action<ColonyMinesDB>(processor => { MineProcessor.CalcMaxRate(CurrentEntity);}) },
                { typeof(PropulsionDB), new Action<PropulsionDB>(processor => { ShipMovementProcessor.CalcMaxSpeed(CurrentEntity); }) },
                { typeof(ColonyRefiningDB), new Action<ColonyRefiningDB>(processor => { RefiningProcessor.ReCalcRefiningRate(CurrentEntity); }) },
                { typeof(ColonyConstructionDB), new Action<ColonyConstructionDB>(processor => { ConstructionProcessor.ReCalcConstructionRate(CurrentEntity); }) },
                { typeof(ShipInfoDB), new Action<ShipInfoDB>(processor => {ShipAndColonyInfoProcessor.ReCalculateShipTonnaageAndHTK(CurrentEntity); }) },
            };

        internal static void ReCalcAbilities(Entity entity)
        {
            CurrentEntity = entity;    
            lock (CurrentEntity) //I think this is needed to stop two threads running the same processor at the same time... right?
            {                                
                foreach (var datablob in entity.DataBlobs)
                {
                    var t = datablob.GetType();
                    if (TypeProcessorMap.ContainsKey(t))
                        TypeProcessorMap[t].DynamicInvoke(datablob); // invoke appropriate delegate  
                }                
            }

        }
    }
}