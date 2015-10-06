using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.AccessControl;

namespace Pulsar4X.ECSLib
{
    public static class InstallationProcessor
    {
        /*
        #region automaticEachTickStuff
        private const int _timeBetweenRuns = 68400; //one terran day.

        public static void Initialize()
        {
        }

        public static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            foreach (var system in systems)
            {
                system.EconLastTickRun += deltaSeconds;
                if (system.EconLastTickRun >= _timeBetweenRuns)
                {
                    foreach (Entity colonyEntity in system.SystemManager.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
                    {
                        PerEconTic(game.StaticData, colonyEntity);
                    }
                    system.EconLastTickRun -= _timeBetweenRuns;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="colonyEntity"></param>
        /// <param name="factionEntity"></param>
        public static void PerEconTic(StaticDataStore staticData, Entity colonyEntity)
        {
            //TODO this is broken, and old anyway (OwningEntity here returns the colonyEntity, ie the blobs owner not the faction entity)
            //Entity factionEntity = colonyEntity.GetDataBlob<ColonyInfoDB>().OwningEntity; 
            //FactionAbilitiesDB factionAbilities = factionEntity.GetDataBlob<FactionAbilitiesDB>();
            //FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            //Employment(staticData, colonyEntity); //check if installations still work
            //Mine(staticData, factionEntity, colonyEntity); //mine new materials.
            //Construction(staticData, factionEntity, colonyEntity); //construct, refine, etc.

            //DoResearch(staticData, colonyEntity, factionAbilities, factionTech);
        }

        /// <summary>
        /// should be called when new facilitys are added, 
        /// facilies are enabled or disabled, 
        /// or if population changes significantly.
        /// Or maybe just check at the beginning of every econ tick.
        /// </summary>
        /// <param name="staticData"></param>
        /// <param name="colonyEntity"></param>
        public static void Employment(StaticDataStore staticData, Entity colonyEntity)
        {
            var employablePopulationlist = colonyEntity.GetDataBlob<ColonyInfoDB>().Population.Values;
            long employable = employablePopulationlist.Sum();
            InstallationsDB installationsDB = colonyEntity.GetDataBlob<InstallationsDB>();
            //int totalReq = 0;
            JDictionary<Guid,int> workingInstallations  = new JDictionary<Guid, int>(staticData.Installations.Keys.ToDictionary(key => key, val => 0));
            foreach (var type in installationsDB.EmploymentList)
            {
                //totalReq += type.Key.PopulationRequired * (int)type.Value;
                var fac = staticData.Installations[type.Type];
                if (type.Enabled && employable >= fac.PopulationRequired)
                {
                    employable -= fac.PopulationRequired;
                    workingInstallations[type.Type] += 1;
                }
            }
            installationsDB.WorkingInstallations = workingInstallations;
        }

        /// <summary>
        /// run every econ tic DEFUNCT
        /// extracts minerals from planet surface by mineing ability;
        /// </summary>
        /// <param name="staticData"></param>
        /// <param name="factionEntity"></param>
        public static void Mine(StaticDataStore staticData, Entity factionEntity, Entity colonyEntity)
        {
            //int installationMineingAbility = InstallationAbilityofType(staticData, colonyEntity.GetDataBlob<InstallationsDB>(), AbilityType.Mine);
            //float factionMineingBonus = BonusesForType(factionEntity, colonyEntity, AbilityType.Mine);
            //float totalMineingAbility = installationMineingAbility * factionMineingBonus;
            //Entity planetEntity = colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity;
            //SystemBodyDB planetDB = planetEntity.GetDataBlob<SystemBodyDB>();
            //JDictionary<Guid, float> colonyMineralStockpile = colonyEntity.GetDataBlob<ColonyInfoDB>().MineralStockpile;
            
            //JDictionary<Guid, MineralDepositInfo> planetRawMinerals = planetDB.Minerals;

            //foreach (KeyValuePair<Guid, MineralDepositInfo> depositKeyValuePair in planetRawMinerals)
            //{
            //    Guid mineralGuid = depositKeyValuePair.Key;
            //    int amountOnPlanet = depositKeyValuePair.Value.Amount;
            //    double accessibility = depositKeyValuePair.Value.Accessibility;
            //    double abilitiestoMine = totalMineingAbility * accessibility;
            //    int amounttomine = (int)Math.Min(abilitiestoMine, amountOnPlanet);
            //    colonyMineralStockpile.SafeValueAdd<Guid>(mineralGuid, amounttomine);             
            //    MineralDepositInfo mineralDeposit = depositKeyValuePair.Value;
            //    mineralDeposit.Amount -= amounttomine;
            //    double accecability = Math.Pow((float)mineralDeposit.Amount / mineralDeposit.HalfOriginalAmount, 3) * mineralDeposit.Accessibility;
            //    mineralDeposit.Accessibility = GMath.Clamp(accecability, 0.1, mineralDeposit.Accessibility);
            //}
            
        }


        /// <summary>
        /// runs each of the constructionJob lists.
        /// </summary>
        /// <param name="staticData"></param>
        /// <param name="factionEntity"></param>
        /// <param name="colonyEntity"></param>
        public static void Construction(StaticDataStore staticData, Entity factionEntity, Entity colonyEntity)
        {
            ColonyInfoDB colonyInfo = colonyEntity.GetDataBlob<ColonyInfoDB>();
            InstallationsDB installations = colonyEntity.GetDataBlob<InstallationsDB>();
            
            

            //Refine stuff.
            var refinaryJobs = installations.RefineryJobs;
            float refinaryPoints = InstallationAbilityofType(staticData, installations, AbilityType.Refinery);
            refinaryPoints *= BonusesForType(factionEntity, colonyEntity, AbilityType.Refinery);

            GenericConstructionJobs(refinaryPoints, refinaryJobs, colonyInfo, colonyInfo.RefinedStockpile);


            //Build facilities.
            var facilityJobs = installations.InstallationJobs;
            float constructionPoints = InstallationAbilityofType(staticData, installations, AbilityType.GenericConstruction);
            constructionPoints *= BonusesForType(factionEntity, colonyEntity, AbilityType.GenericConstruction);
            var faciltiesList = new JDictionary<Guid, float>();

            GenericConstructionJobs(constructionPoints, facilityJobs, colonyInfo, faciltiesList);

            foreach (var facilityPair in faciltiesList)
            {
                //check how many complete installations we have by turning a float into an int;
                int fullColInstallations = (int)installations.Installations[facilityPair.Key]; 
                //add to the installations
                installations.Installations.SafeValueAdd<Guid>(facilityPair.Key, (float)facilityPair.Value);
                //compare how many complete we had then, vs now.
                if ((int)installations.Installations[facilityPair.Key] > fullColInstallations)
                {
                    installations.EmploymentList.Add(new InstallationEmployment 
                    {Enabled = true, Type = facilityPair.Key});
                }
            }

            //build components. this uses the same ability as installation construction tho...
            var componentJobs = installations.ComponentJobs;
           

            //Build Ordnance
            var ordnanceJobs = installations.OrdnanceJobs;
            float ordnancePoints = InstallationAbilityofType(staticData, installations, AbilityType.Refinery);
            ordnancePoints *= BonusesForType(factionEntity, colonyEntity, AbilityType.OrdnanceConstruction);

            GenericConstructionJobs(ordnancePoints, ordnanceJobs, colonyInfo, colonyInfo.OrdinanceStockpile);

            //Build Fighters
            var fighterJobs = installations.FigherJobs;
            float fighterPoints = InstallationAbilityofType(staticData, installations, AbilityType.Refinery);
            fighterPoints *= BonusesForType(factionEntity, colonyEntity, AbilityType.FighterConstruction);
            var fighterList = new JDictionary<Guid, float>();

            GenericConstructionJobs(fighterPoints, fighterJobs, colonyInfo, fighterList);
               
        }


        private static JDictionary<Guid,int> FindResource(ColonyInfoDB colony, Guid key)
        {
            JDictionary<Guid, int> resourceDictionary = null;
            if (colony.MineralStockpile.ContainsKey(key))
                resourceDictionary = colony.MineralStockpile;
            else if (colony.RefinedStockpile.ContainsKey(key))
                resourceDictionary = colony.RefinedStockpile;
            else if (colony.ComponentStockpile.ContainsKey(key))
                resourceDictionary = colony.ComponentStockpile;
            return resourceDictionary;
        }

        /// <summary>
        /// an attempt at a more generic constructionProcessor.
        /// should maybe be private.
        /// </summary>
        /// <param name="ablityPointsThisColony"></param>
        /// <param name="jobList"></param>
        /// <param name="rawMaterials"></param>
        /// <param name="colonyInfo"></param>
        /// <param name="stockpileOut"></param>
        public static void GenericConstructionJobs(double ablityPointsThisColony, List<ConstructionJob> jobList, ColonyInfoDB colonyInfo, JDictionary<Guid,int> stockpileOut)
        {
            List<ConstructionJob> newJobList = new List<ConstructionJob>();

            foreach (ConstructionJob job in jobList)
            {
                double pointsToUseThisJob = Math.Min(job.BuildPointsRemaining, (ablityPointsThisColony * job.PriorityPercent.Percent));
                double pointsUsedThisJob = 0;
                //the points per requred resources.
                double pointsPerResourcees = (double)job.BuildPointsRemaining / job.RawMaterialsRemaining.Values.Sum();
                foreach (var jobResourcePair in new Dictionary<Guid,int>(job.RawMaterialsRemaining))
                {
                    Guid resourceGuid = jobResourcePair.Key;
                    Dictionary<Guid, float> rawMaterials = FindResource(colonyInfo, resourceGuid);
                    if (rawMaterials == null)
                        break;
                    double pointsPerThisResource = (double)job.BuildPointsRemaining / jobResourcePair.Value;

                    //maximum rawMaterials needed or availible whichever is less
                    int maxResource = (int)Math.Min(jobResourcePair.Value, rawMaterials[resourceGuid]);
                    
                    //maximum buildpoints I can use for this resource
                    //should I be using pointsPerResources or pointsPerThisResource?
                    double maxPoint = pointsPerResourcees * maxResource;

                    maxPoint = Math.Min(maxPoint, pointsToUseThisJob); 

                    
                    int usedResource = (int)(maxPoint / pointsPerResourcees);
                    double usedPoints = pointsPerResourcees * usedResource;
                    
                    job.RawMaterialsRemaining[resourceGuid] -= usedResource; //needs to be an int
                    rawMaterials[resourceGuid] -= usedResource; //needs to be an int
                    pointsUsedThisJob += usedPoints;
                    pointsToUseThisJob -= usedPoints;
                                                            
                }
                ablityPointsThisColony -= pointsUsedThisJob;
                //pointsUsedThisJob = Math.Round(pointsUsedThisJob);
                

                double percentPerItem = (double)job.BuildPointsPerItem / 100; 
                double percentthisjob = pointsUsedThisJob / 100; 
                double itemsCreated = percentPerItem * percentthisjob;
                double itemsLeft = job.ItemsRemaining - itemsCreated;

                stockpileOut.SafeValueAdd<Guid>(job.Type, (float)(job.ItemsRemaining - itemsLeft));             
                
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
        /// adds research points to a scientists project.
        /// </summary>
        /// <param name="colonyEntity"></param>
        /// <param name="factionAbilities"></param>
        /// <param name="factionTechs"></param>
        public static void DoResearch(StaticDataStore staticData, Entity colonyEntity, FactionAbilitiesDB factionAbilities,  FactionTechDB factionTechs)
        {
            InstallationsDB installations = colonyEntity.GetDataBlob<InstallationsDB>();
            Dictionary<InstallationSD,int> labs = InstallationsWithAbility(staticData, installations, AbilityType.Research);
            int labsused = 0;

            foreach (var scientist in colonyEntity.GetDataBlob<ColonyInfoDB>().Scientists)
            {
                TechSD research = (TechSD)scientist.GetDataBlob<TeamsDB>().TeamTask;
                int numProjectLabs = scientist.GetDataBlob<TeamsDB>().TeamSize;
                float bonus = scientist.GetDataBlob<ScientistBonusDB>().Bonuses[research.Category];
                //bonus *= BonusesForType(factionEntity, colonyEntity, InstallationAbilityType.Research);
                
                int researchmax = TechProcessor.CostFormula(factionTechs, research);

                int researchPoints = 0;             
                foreach (var kvp in labs)
                { 
                    while (numProjectLabs > 0)
                    {
                        for(int i = 0; i < kvp.Value; i++)
                        {                         
                            researchPoints += kvp.Key.BaseAbilityAmounts[AbilityType.Research];
                            numProjectLabs --;
                        }
                    }
                }
                researchPoints = (int)(researchPoints * bonus);
                if (factionTechs.ResearchableTechs.ContainsKey(research))
                {
                    factionTechs.ResearchableTechs[research] += researchPoints;
                    if (factionTechs.ResearchableTechs[research] >= researchmax)
                    {
                        TechProcessor.ApplyTech(factionTechs, research); //apply effects from tech, and add it to researched techs
                        scientist.GetDataBlob<TeamsDB>().TeamTask = null; //team task is now nothing. 
                    }
                }
            }
        }

        #endregion

        #region InteractsWithPlayer

        /// <summary>
        /// for changeing the priority of the constructionJobs priorities.
        /// not sure how this should work...
        /// </summary>
        /// <param name="colonyEntity"></param>
        /// <param name="neworder"></param>
        public static void ConstructionPriority(Entity colonyEntity)
        {
            //idk... needs to be something in the message about whether it's Construction, Ordnance or Fighers...  
            //I think if it's a valid list we can just chuck it straight in.
            try
            {
               //colonyEntity.GetDataBlob<InstallationsDB>().InstallationJobs = (List<ConstructionJob>)neworder.Data;                
            }
            catch (Exception)
            {
                
                throw;
            }
        }
        
        #endregion

        #region PrivateMethods

       

        /// <summary>
        /// not sure if this should be a whole lot of if statements or if we can tidy it up somewhere else
        /// </summary>
        /// <param name="factioEntity"></param>
        /// <param name="colonyEntity"></param>
        /// <param name="ability"></param>
        /// <returns></returns>
        private static float BonusesForType(Entity factioEntity, Entity colonyEntity, AbilityType ability )
        {

            FactionAbilitiesDB factionAbilities = factioEntity.GetDataBlob<FactionAbilitiesDB>();

            float totalBonus = 1.0f;
            totalBonus += factionAbilities.AbilityBonuses[ability];
            totalBonus += 1.0f; // colonyAbilityes.AbilityBonus[ability]; 
            /*todo: hell why not just ask the colony to get all bonuses for [ability] and have it return all of them? 
             * Should it be in the processor and the processor look up each time, 
             * or should the faction hold the dictionary and the processor update the dictionary, 
             * either on something changeing, or at a designated tic.          
             *
            return totalBonus;
        }

        /// <summary>
        /// Returns the total InstallationAbilityPoints on an colony 
        /// Employment function should be run prior to this (ie should be run at the begining of econ tic)
        /// if any changes have been made to the numer of Installatioins, pop etc etc.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="installationsDB"></param>
        /// <returns></returns>
        private static int InstallationAbilityofType(StaticDataStore staticData, InstallationsDB installationsDB, AbilityType type)
        {            
            int totalAbilityValue = 0;
            foreach(var facility in InstallationsWithAbility(staticData, installationsDB, type))             
            {

                totalAbilityValue += facility.Key.BaseAbilityAmounts[type] * facility.Value;  
            }
            return totalAbilityValue;           
        }

        /// <summary>
        /// working installations that have ability
        /// Employment function should be run prior to this (ie should be run at the begining of econ tic)
        /// </summary>
        /// <param name="installations">colony installations</param>
        /// <param name="type">ability Type</param>
        /// <returns>a dictionary with key InstallationSD and the number of this type of installation</returns>
        private static Dictionary<InstallationSD,int> InstallationsWithAbility(StaticDataStore staticData, InstallationsDB installations, AbilityType type)
        {
            Dictionary<InstallationSD, int> facilities = new Dictionary<InstallationSD, int>();
            foreach (var kvp in installations.WorkingInstallations)
            {
                InstallationSD facility = staticData.Installations[kvp.Key];
                if (facility.BaseAbilityAmounts.ContainsKey(type))
                facilities.Add(facility,kvp.Value);
            }
            return facilities;
        }
        #endregion
         */
    }

}