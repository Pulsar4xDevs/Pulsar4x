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
            float sectorGovenerAbility = 1.025f; //these guys dont exsist yet
            float planetGovenerAbility = 1.05f; //these guys dont exsist yet
            float totalBonusMultiplier = factionConstructionAbility * sectorGovenerAbility * planetGovenerAbility;
            foreach (Entity colonyEntity in factionEntity.GetDataBlob<FactionDB>().Colonies)
            {
                var colonyInstallations = colonyEntity.GetDataBlob<InstallationsDB>();
                var constructionJobs = colonyEntity.GetDataBlob<InstallationsDB>().InstallationConstructionJobs;
                float constructionPoints = TotalAbilityofType(InstallationAbilityType.InstallationConstruction, colonyEntity.GetDataBlob<InstallationsDB>());
                constructionPoints *= totalBonusMultiplier;
                List<InstallationSD> constructionDone = new List<InstallationSD>();
                foreach (var kvp in constructionJobs)
                {
                    PercentValue<float> value = kvp.Value;
                    float constructiononthisjob = constructionPoints * value.Percent;
                    float pointsUsed = Math.Min(value.Value, constructiononthisjob);

                    int pointsPerThisInstallation = kvp.Key.BuildPoints;
                    float percent_built = pointsUsed / pointsPerThisInstallation;
                    value.Value -= percent_built;
                    colonyInstallations.Installations[kvp.Key] += percent_built;
                    constructionPoints -= pointsUsed;

                    if (value.Value <= 0)
                        constructionDone.Add(kvp.Key);

                }
                foreach (var job in constructionDone)
                {
                    constructionJobs.Remove(job);
                }
            }
        }

        internal static void ConstructionPriority(Entity colonyEntity, Message neworder)
        {
            //idk...
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