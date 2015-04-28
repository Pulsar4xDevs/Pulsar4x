using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    internal static class InstallationProcessor
    {
        /// <summary>
        /// run every ??? 
        /// extracts minerals from planet surface by mineing ability;
        /// </summary>
        /// <param name="factionEntity"></param>
        internal static void Mine(Entity factionEntity)
        {
            float factionMineingAbility = 1.0f;//factionEntity.GetDataBlob<FactionAbilitiesDB>(); todo when this is in
            foreach (Entity colonyEntity in factionEntity.GetDataBlob<FactionDB>().Colonies)
            {
                Entity planet = colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity;
                JDictionary<Guid, int> mineralStockpile = colonyEntity.GetDataBlob<ColonyInfoDB>().MineralStockpile;
                int installationMineingAbility = TotalAbilityofType(InstallationAbilityType.Mine, colonyEntity.GetDataBlob<InstallationsDB>());
                SystemBodyDB systemBody = planet.GetDataBlob<SystemBodyDB>();
                JDictionary<Guid, MineralDepositInfo> minerals = systemBody.Minerals;
                foreach (KeyValuePair<Guid, MineralDepositInfo> kvp in minerals)
                {
                    Guid mineralGuid = kvp.Key;
                    int amountOnPlanet = kvp.Value.Amount;
                    double accessibility = kvp.Value.Accessibility;
                    double abilitiestoMine = installationMineingAbility * factionMineingAbility * accessibility;
                    int amounttomine = (int)Math.Min(abilitiestoMine, amountOnPlanet);
                    mineralStockpile[mineralGuid] += amounttomine;
                    MineralDepositInfo mineralDeposit = kvp.Value;
                    mineralDeposit.Amount -= amounttomine;
                    double accecability = Math.Pow(mineralDeposit.Amount / mineralDeposit.HalfOrigionalAmount, 2) * mineralDeposit.Accessibility;
                    mineralDeposit.Accessibility = Math.Max(accecability, 0.1);
                }
            }
        }

        internal static void Construct(Entity factionEntity)
        {
            float factionConstructionAbility = 1.0f; //factionEntity.GetDataBlob<FactionAbilitiesDB>(); todo when this is in
            foreach (Entity colonyEntity in factionEntity.GetDataBlob<FactionDB>().Colonies)
            {
                int installationConstructionAbility = TotalAbilityofType(InstallationAbilityType.InstallationConstruction, colonyEntity.GetDataBlob<InstallationsDB>());

            }
        }

        private static int TotalAbilityofType(InstallationAbilityType type, InstallationsDB installationsDB)
        {            
            int totalAbilityValue = 0;
            foreach (KeyValuePair<InstallationSD, float> kvp in installationsDB.Installations.Where(item => item.Key.BaseAbilityAmounts.ContainsKey(type)))
            {
                totalAbilityValue += kvp.Key.BaseAbilityAmounts[type] * (int)kvp.Value; //the decimal is an incomplete instalation, so ignore it. 
            }
            return totalAbilityValue;           
        }
    }
}