using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.ECSLib
{
    //This processor takes an entity and recalcs for each of the datablobs in that entity.
    //as an ability is added to the game, it's recalc processor should be linked here.
    internal static class ReCalcProcessor
    {
        [ThreadStatic]
        private static Entity CurrentEntity;
        internal static Dictionary<Type, Delegate> TypeProcessorMap = new Dictionary<Type, Delegate>
            {
                { typeof(ShipInfoDB), new Action<ShipInfoDB>(processor => {ShipAndColonyInfoProcessor.ReCalculateShipTonnaageAndHTK(CurrentEntity); }) },
                { typeof(MiningDB), new Action<MiningDB>(processor => { MineResourcesProcessor.CalcMaxRate(CurrentEntity);}) },
                { typeof(ColonyLifeSupportDB), new Action<ColonyLifeSupportDB>(processor => {PopulationProcessor.ReCalcMaxPopulation(CurrentEntity); }) },
                { typeof(CargoStorageDB), new Action<CargoStorageDB>(processor => {StorageSpaceProcessor.ReCalcCapacity(CurrentEntity); }) },

            };

        

        internal static void ReCalcAbilities(Entity entity)
        {
             
            //lock (CurrentEntity) 
            //{
                CurrentEntity = entity;
                foreach (var datablob in entity.DataBlobs)
                {
                    var t = datablob.GetType();
                    if (TypeProcessorMap.ContainsKey(t))
                        TypeProcessorMap[t].DynamicInvoke(datablob); // invoke appropriate delegate  
                }                
            //}
        }
    }



}