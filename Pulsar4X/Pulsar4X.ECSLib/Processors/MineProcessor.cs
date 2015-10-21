using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    internal static class MineProcessor
    {





        internal static void MineResources(Entity colonyEntity, int econTicks)
        {
            JDictionary<Guid, int> mineRates = colonyEntity.GetDataBlob<ColonyMinesDB>().MineingRate;
            JDictionary<Guid,MineralDepositInfo> planetMinerals = colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<SystemBodyDB>().Minerals;
            JDictionary<Guid, int> colonyMineralStockpile = colonyEntity.GetDataBlob<ColonyInfoDB>().MineralStockpile;
            float mineBonuses = 1;//colonyEntity.GetDataBlob<ColonyBonusesDB>().GetBonus(AbilityType.Mine);
            foreach (var kvp in mineRates)
            {                
                double accessability = planetMinerals[kvp.Key].Accessibility;
                double actualRate = kvp.Value * mineBonuses * accessability * econTicks;
                int mineralsMined = (int)Math.Min(actualRate, planetMinerals[kvp.Key].Amount);

                colonyMineralStockpile.SafeValueAdd<Guid>(kvp.Key, mineralsMined);
                MineralDepositInfo mineralDeposit = planetMinerals[kvp.Key];
                int newAmount = mineralDeposit.Amount -= mineralsMined;
                
                accessability = Math.Pow((float)mineralDeposit.Amount / mineralDeposit.HalfOriginalAmount, 3) * mineralDeposit.Accessibility;
                double newAccess = GMath.Clamp(accessability, 0.1, mineralDeposit.Accessibility);

                mineralDeposit.Amount = newAmount;
                mineralDeposit.Accessibility = newAccess;

                //planetMinerals[kvp.Key] = mineralDeposit;
            }
        }

        /// <summary>
        /// Called by the ReCalcProcessor.
        /// </summary>
        /// <param name="colonyEntity"></param>
        internal static void CalcMaxRate(Entity colonyEntity)
        {
            Dictionary<Entity, int> installations = colonyEntity.GetDataBlob<ColonyInfoDB>().Installations;
            Dictionary<Entity, int> mines = installations.Where(kvp => kvp.Key.HasDataBlob<MineResourcesDB>()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            JDictionary<Guid,int> rates = new JDictionary<Guid, int>();
            
            foreach (var mineTypeKvp in mines)
            {
                foreach (var kvp in mineTypeKvp.Key.GetDataBlob<MineResourcesDB>().ResourcesPerEconTick)
                {
                    rates.SafeValueAdd(kvp.Key,kvp.Value * mineTypeKvp.Value);
                }                
            }
            colonyEntity.GetDataBlob<ColonyMinesDB>().MineingRate = rates;
        }
    }
}