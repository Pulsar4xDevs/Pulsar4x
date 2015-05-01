using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public static class InstallationProcessor
    {

        /// <summary>
        /// should be called when new facilitys are added, 
        /// facilies are enabled or disabled, 
        /// or if population changes significantly.
        /// Or maybe just check at the beginning of every econ tick.
        /// </summary>
        /// <param name="colonyEntity"></param>
        public static void Employment(Entity colonyEntity)
        {
            var employablePopulationlist = colonyEntity.GetDataBlob<ColonyInfoDB>().Population.Values;
            long employable = (long)(employablePopulationlist.Sum() * 1000000); //because it's in millions I think...maybe we should change.
            InstallationsDB installationsDB = colonyEntity.GetDataBlob<InstallationsDB>();
            //int totalReq = 0;
            JDictionary<Guid,int> workingInstallations  = new JDictionary<Guid, int>(StaticDataManager.StaticDataStore.Installations.Keys.ToDictionary(key => key, val => 0));
            foreach (var type in installationsDB.EmploymentList)
            {
                //totalReq += type.Key.PopulationRequired * (int)type.Value;
                var fac = StaticDataManager.StaticDataStore.Installations[type.Type];
                if (type.Enabled && employable >= fac.PopulationRequired)
                {
                    employable -= fac.PopulationRequired;
                    workingInstallations[type.Type] += 1;
                }
            }
            installationsDB.WorkingInstallations = workingInstallations;
        }

        /// <summary>
        /// run every econ tic 
        /// extracts minerals from planet surface by mineing ability;
        /// </summary>
        /// <param name="factionEntity"></param>
        public static void Mine(Entity factionEntity)
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
                    double accecability = Math.Pow((float)mineralDeposit.Amount / mineralDeposit.HalfOrigionalAmount, 3) * mineralDeposit.Accessibility;
                    mineralDeposit.Accessibility = GMath.Clamp(accecability, 0.1, mineralDeposit.Accessibility);
                }
            }
        }


        /// <summary>
        /// Can this be made more generic, and reused for 
        /// ordnance and fighter production too?
        /// 
        /// </summary>
        /// <param name="factionEntity"></param>
        public static void Construct(Entity factionEntity)
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

                    //this confusing block of code below checks if there's a fully constructed installation
                    //if so, it adds it to the employment list, which is used to check pop requrements etc. 
                    int fullColInstallations = (int)colonyInstallations.Installations[type];
                    colonyInstallations.Installations[type] += percentBuilt;
                    if ((int)colonyInstallations.Installations[type] > fullColInstallations)
                    {
                        colonyInstallations.EmploymentList.Add(new InstallationEmployment{Enabled = true,Type = type.ID});
                    }
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

        public static void ConstructionPriority(Entity colonyEntity, Message neworder)
        {
            //idk... needs to be something in the message about whether it's Construction, Ordnance or Fighers...  
            //I think if it's a valid list we can just chuck it straight in.
            try
            {
                colonyEntity.GetDataBlob<InstallationsDB>().InstallationJobs = (List<ConstructionJob>)neworder.Data;                
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        private static int TotalAbilityofType(InstallationAbilityType type, InstallationsDB installationsDB)
        {            
            int totalAbilityValue = 0;
            foreach (KeyValuePair<Guid, int> kvp in installationsDB.WorkingInstallations)//.Where(item => item.Key.BaseAbilityAmounts.ContainsKey(type)))
            {
                InstallationSD facility = StaticDataManager.StaticDataStore.Installations[kvp.Key];
                if(facility.BaseAbilityAmounts.ContainsKey(type))
                    totalAbilityValue += facility.BaseAbilityAmounts[type] * kvp.Value;  
            }
            return totalAbilityValue;           
        }
    }
}