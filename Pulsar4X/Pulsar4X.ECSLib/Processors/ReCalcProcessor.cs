using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    //This processor takes an entity and recalcs for each of the datablobs in that entity.
    //as an ability is added to the game, it's recalc processor should be linked here.
    internal static class ReCalcProcessor
    {      
        internal static void ReCalcAbilities(Entity entity)
        {
            var typeProcessorMap = new Dictionary<Type, Delegate>
            {
                { typeof(ColonyMinesDB), new Action<ColonyMinesDB>(processor => { MineProcessor.CalcMaxRate(entity);}) },
                { typeof(PropulsionDB), new Action<PropulsionDB>(processor => { ShipMovementProcessor.CalcMaxSpeed(entity); }) },
                { typeof(ColonyRefiningDB), new Action<ColonyRefiningDB>(processor => { RefiningProcessor.ReCalcRefiningRate(entity); }) },
                { typeof(ColonyConstructionDB), new Action<ColonyConstructionDB>(processor => { ConstructionProcessor.ReCalcConstructionRate(entity); }) },
            };            
            
            
            foreach (var datablob in entity.DataBlobs)
            {
                var t = datablob.GetType();
                if(typeProcessorMap.ContainsKey(t))
                    typeProcessorMap[t].DynamicInvoke(datablob); // invoke appropriate delegate  
            }
        }
    }
}