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
            float factionMineingAbility = factionEntity.GetDataBlob<FactionAbilitiesDB>().BaseMiningBonus;
            float sectorGovenerAbility = 1.0f; //todo these guys dont exsist yet
            float planetGovenerAbility = 1.0f; //todo these guys dont exsist yet
            float totalBonusMultiplier = factionMineingAbility * sectorGovenerAbility * planetGovenerAbility;

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
                    double abilitiestoMine = installationMineingAbility * totalBonusMultiplier * accessibility;
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
            float factionConstructionAbility = factionEntity.GetDataBlob<FactionAbilitiesDB>().BaseConstructionBonus;
            float sectorGovenerAbility = 1.0f; //todo these guys dont exsist yet
            float planetGovenerAbility = 1.0f; //todo these guys dont exsist yet
            float totalBonusMultiplier = factionConstructionAbility * sectorGovenerAbility * planetGovenerAbility;
            foreach (Entity colonyEntity in factionEntity.GetDataBlob<FactionDB>().Colonies)
            {
                InstallationsDB colonyInstallations = colonyEntity.GetDataBlob<InstallationsDB>();
                List<ConstructionJob> constructionJobs = colonyInstallations.InstallationJobs;
                JDictionary<Guid, int> resourceStockpile = colonyEntity.GetDataBlob<ColonyInfoDB>().MineralStockpile;

                float constructionPoints = TotalAbilityofType(InstallationAbilityType.InstallationConstruction, colonyEntity.GetDataBlob<InstallationsDB>());
                constructionPoints *= totalBonusMultiplier;
                List<ConstructionJob> constructionJobs_newList = new List<ConstructionJob>();
                foreach (ConstructionJob job in constructionJobs)
                {

                    var type = StaticDataManager.StaticDataStore.Installations[job.Type];
                    float constructiononthisjob = constructionPoints * job.PriorityPercent.Percent;
                    float pointsUsed = Math.Min(job.ItemsRemaining, constructiononthisjob);

                    int pointsPerThisInstallation = type.BuildPoints;
                    float percentBuilt = pointsUsed / pointsPerThisInstallation;

                    colonyInstallations.Installations[type] += percentBuilt;
                    constructionPoints -= pointsUsed;

                    if (job.ItemsRemaining > 0) //if there's still itemsremaining...
                    {
                        //+becuase it's a struct, we have to re-create it.
                        ConstructionJob newStruct = new ConstructionJob 
                        {
                            ItemsRemaining = job.ItemsRemaining - percentBuilt, 
                            PriorityPercent = job.PriorityPercent, 
                            Type = job.Type
                        };
                        constructionJobs_newList.Add(newStruct);//+then add it to the new list;
                    }
                    colonyInstallations.InstallationJobs = constructionJobs_newList; //+and finaly, replace the list.
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