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
        public static void Mine(Entity factionEntity, Entity colonyEntity)
        {
            float factionMineingAbility = factionEntity.GetDataBlob<FactionAbilitiesDB>().BaseMiningBonus;
            float sectorGovenerAbility = 1.0f; //todo these guys dont exsist yet
            float planetGovenerAbility = 1.0f; //todo these guys dont exsist yet
            float totalBonusMultiplier = factionMineingAbility * sectorGovenerAbility * planetGovenerAbility;

            Entity planetEntity = colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity;
            SystemBodyDB planetDB = planetEntity.GetDataBlob<SystemBodyDB>();
            JDictionary<Guid, int> colonyMineralStockpile = colonyEntity.GetDataBlob<ColonyInfoDB>().MineralStockpile;
            int installationMineingAbility = TotalAbilityofType(InstallationAbilityType.Mine, colonyEntity.GetDataBlob<InstallationsDB>());
            JDictionary<Guid, MineralDepositInfo> planetRawMinerals = planetDB.Minerals;

            foreach (KeyValuePair<Guid, MineralDepositInfo> depositKeyValuePair in planetRawMinerals)
            {
                Guid mineralGuid = depositKeyValuePair.Key;
                int amountOnPlanet = depositKeyValuePair.Value.Amount;
                double accessibility = depositKeyValuePair.Value.Accessibility;
                double abilitiestoMine = installationMineingAbility * totalBonusMultiplier * accessibility;
                int amounttomine = (int)Math.Min(abilitiestoMine, amountOnPlanet);
                colonyMineralStockpile[mineralGuid] += amounttomine;
                MineralDepositInfo mineralDeposit = depositKeyValuePair.Value;
                mineralDeposit.Amount -= amounttomine;
                double accecability = Math.Pow((float)mineralDeposit.Amount / mineralDeposit.HalfOrigionalAmount, 3) * mineralDeposit.Accessibility;
                mineralDeposit.Accessibility = GMath.Clamp(accecability, 0.1, mineralDeposit.Accessibility);
            }
            
        }


        /// <summary>
        /// an attempt at a more generic constructionProcessor.
        /// </summary>
        /// <param name="ablityPointsThisColony"></param>
        /// <param name="jobList"></param>
        /// <param name="rawMaterials"></param>
        /// <param name="stockpileOut"></param>
        public static void ConstructionJobs(double ablityPointsThisColony, ref List<ConstructionJob> jobList, ref JDictionary<Guid,int> rawMaterials, ref JDictionary<Guid,double> stockpileOut)
        {
            List<ConstructionJob> newJobList = new List<ConstructionJob>();

            foreach (ConstructionJob job in jobList)
            {
                double pointsToUseThisJob = Math.Min(job.BuildPointsRemaining, (ablityPointsThisColony * job.PriorityPercent.Percent));
                double pointsUsedThisJob = 0;
                //the points per requred resources.
                double pointsPerResourcees = (double)job.BuildPointsRemaining / job.RawMaterialsRemaining.Values.Sum();
                foreach (var resourcePair in new Dictionary<Guid,int>(job.RawMaterialsRemaining))
                {
                    Guid resourceGuid = resourcePair.Key;

                    double pointsPerThisResource = (double)job.BuildPointsRemaining / resourcePair.Value;

                    //maximum rawMaterials needed or availible whichever is less
                    int maxResource = Math.Min(resourcePair.Value, rawMaterials[resourceGuid]);
                    
                    //maximum buildpoints I can use for this resource
                    //should I be using pointsPerResources or pointsPerThisResource?
                    double maxPoint = pointsPerResourcees * maxResource; 

                    double usedPoints = Math.Min(maxPoint, pointsToUseThisJob); 

                    //this little bit here needs a small rework, we're loosing accuracy due to int.
                    //we need to adjust the points used to the usedResource Int.
                    int usedResource = (int)(usedPoints / pointsPerResourcees);
                    
                    
                    
                    job.RawMaterialsRemaining[resourceGuid] -= usedResource; //needs to be an int
                    rawMaterials[resourceGuid] -= usedResource; //needs to be an int
                    pointsUsedThisJob += usedPoints;
                    pointsToUseThisJob -= usedPoints;
                                                            
                }
                ablityPointsThisColony -= pointsUsedThisJob;

                double percentPerItem = (double)job.BuildPointsPerItem / 100; 
                double percentthisjob = pointsUsedThisJob / 100; 
                double itemsCreated = percentPerItem * percentthisjob;
                double itemsLeft = job.ItemsRemaining - itemsCreated;
                stockpileOut[job.Type] += job.ItemsRemaining - itemsLeft;

                if (itemsLeft > 0)
                {
                    //recreate constructionJob because it's a struct.
                    ConstructionJob newJob = new ConstructionJob 
                    {
                        Type = job.Type, 
                        ItemsRemaining = (float)itemsLeft, 
                        PriorityPercent = job.PriorityPercent, 
                        RawMaterialsRemaining = job.RawMaterialsRemaining, //check this one. mutability?                    
                        BuildPointsRemaining = job.BuildPointsRemaining - (int)Math.Ceiling(pointsUsedThisJob),
                        BuildPointsPerItem = job.BuildPointsPerItem
                    };
                    newJobList.Add(newJob); //then add it to the new list
                }

            }
            jobList = newJobList; //old list gets replaced with new
        }

        /// <summary>
        /// Can this be made more generic, and reused for 
        /// ordnance and fighter production too?
        /// 
        /// </summary>
        /// <param name="factionEntity"></param>
        /// <param name="colonyEntity"></param>
        public static void Construct(Entity factionEntity, Entity colonyEntity)
        {
            float factionConstructionAbility = factionEntity.GetDataBlob<FactionAbilitiesDB>().BaseConstructionBonus;
            float sectorGovenerAbility = 1.0f; //todo these guys dont exsist yet
            float planetGovenerAbility = 1.0f; //todo these guys dont exsist yet
            float totalBonusMultiplier = factionConstructionAbility * sectorGovenerAbility * planetGovenerAbility;

            InstallationsDB colonyInstallations = colonyEntity.GetDataBlob<InstallationsDB>();
            List<ConstructionJob> constructionJobs = colonyInstallations.InstallationJobs;
            List<ConstructionJob> constructionJobs_newList = new List<ConstructionJob>();

            JDictionary<Guid, int> resourceStockpile = colonyEntity.GetDataBlob<ColonyInfoDB>().MineralStockpile;

            float constructionPoints = TotalAbilityofType(InstallationAbilityType.InstallationConstruction, colonyEntity.GetDataBlob<InstallationsDB>());
            constructionPoints *= totalBonusMultiplier;
            
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