using System;
using System.Collections.Generic;
using System.Linq;


namespace Pulsar4X.ECSLib
{

    public static class GenericComponentFactory
    {
        
        /// <summary>
        /// takes a ComponentSD staticData and turns it into a DesignDB
        /// </summary>
        /// <param name="component">the componentSD</param>
        /// <param name="factionTech">this factions TechDB</param>
        /// <param name="staticData">the game staticData</param>
        /// <returns></returns>
        public static ComponentDesign StaticToDesign(ComponentTemplateSD component, FactionTechDB factionTech, StaticDataStore staticData)
        {
            ComponentDesign design = new ComponentDesign();

            design.Name = component.Name;
            design.Description = component.Description;

            design.MassFormula = new ChainedExpression(component.MassFormula, design, factionTech, staticData);
            design.VolumeFormula = new ChainedExpression(component.VolumeFormula, design, factionTech, staticData);
            design.CrewFormula = new ChainedExpression(component.CrewReqFormula, design, factionTech, staticData);
            design.HTKFormula = new ChainedExpression(component.HTKFormula, design, factionTech, staticData);
            design.ResearchCostFormula = new ChainedExpression(component.ResearchCostFormula, design, factionTech, staticData);
            design.BuildCostFormula = new ChainedExpression(component.BuildPointCostFormula, design, factionTech, staticData);
            design.MineralCostFormulas = new Dictionary<Guid, ChainedExpression>();
            design.CreditCostFormula = new ChainedExpression(component.CreditCostFormula, design, factionTech, staticData);
            design.ComponentMountType = component.MountType;
            design.ConstructionType = component.ConstructionType;
            design.CargoTypeID = component.CargoTypeID;

            foreach (var kvp in component.MineralCostFormula)
            {
                design.MineralCostFormulas.Add(kvp.Key, new ChainedExpression(kvp.Value, design, factionTech, staticData));
            }

            design.ComponentDesignAbilities = new List<ComponentDesignAbility>();
            foreach (var abilitySD in component.ComponentAbilitySDs)
            {
                ComponentDesignAbility designAbility = new ComponentDesignAbility(design);

                designAbility.Name = abilitySD.Name;
                designAbility.Description = abilitySD.Description;
                designAbility.GuiHint = abilitySD.GuiHint;

                if(abilitySD.AbilityFormula !=  null)
                    designAbility.Formula = new ChainedExpression(abilitySD.AbilityFormula, designAbility, factionTech, staticData);

                if (abilitySD.GuidDictionary != null )
                {
                    designAbility.GuidDictionary = new Dictionary<object, ChainedExpression>();
                    if (designAbility.GuiHint == GuiHint.GuiTechSelectionList)
                    {
                        foreach (var kvp in abilitySD.GuidDictionary)
                        {
                            if (factionTech.ResearchedTechs.ContainsKey(Guid.Parse(kvp.Key.ToString())))
                            {
                                TechSD techSD = staticData.Techs[Guid.Parse(kvp.Key.ToString())];
                                designAbility.GuidDictionary.Add(kvp.Key, new ChainedExpression(TechProcessor.DataFormula(factionTech, techSD).ToString(), designAbility, factionTech, staticData));                      
                            }
                        }
                    }
                    else
                    {
                        foreach (var kvp in abilitySD.GuidDictionary)
                        {
                            designAbility.GuidDictionary.Add(kvp.Key, new ChainedExpression(kvp.Value, designAbility, factionTech, staticData));
                        }
                    }
                }
                if (designAbility.GuiHint == GuiHint.GuiSelectionMaxMin)
                {
                    designAbility.MaxValueFormula = new ChainedExpression(abilitySD.MaxFormula, designAbility, factionTech, staticData);
                    designAbility.MinValueFormula = new ChainedExpression(abilitySD.MinFormula, designAbility, factionTech, staticData);
                    designAbility.StepValueFormula = new ChainedExpression(abilitySD.StepFormula, designAbility, factionTech, staticData);
                }
                if (abilitySD.AbilityDataBlobType != null)
                {
                    designAbility.DataBlobType = Type.GetType(abilitySD.AbilityDataBlobType);        
                }
                
                design.ComponentDesignAbilities.Add(designAbility);
            }

            design.MassFormula.Evaluate();
            design.SetCrew();
            design.SetHTK();
            design.SetResearchCost();
            design.SetBuildCost();
            design.SetMineralCosts();

            return design;
        }

        /// <summary>
        /// Creates Entity and blobs.
        /// </summary>
        /// <param name="globalEntityManager"></param>
        /// <param name="componentDesign"></param>
        /// <param name="factionTech"></param>
        /// <returns></returns>
        public static Entity DesignToDesignEntity(Game game, Entity factionEntity, ComponentDesign componentDesign)
        {
            EntityManager globalEntityManager = game.GlobalManager;
            StaticDataStore staticData = game.StaticData;
            FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            FactionInfoDB faction = factionEntity.GetDataBlob<FactionInfoDB>();
            //TODO probilby do checking to see if valid here?
            Entity component = new Entity(globalEntityManager);
            
            TechSD tech = new TechSD();
            tech.ID = Guid.NewGuid();
            tech.Name = componentDesign.Name + " Design Research";
            tech.Description = "Research into building " + componentDesign.Name;
            tech.MaxLevel = 1;
            tech.CostFormula = componentDesign.ResearchCostValue.ToString();

            factionTech.ResearchableTechs.Add(tech, 0);
            NameDB nameDB = new NameDB(componentDesign.Name);

            Dictionary<Guid, int> mineralCosts = new Dictionary<Guid, int>();
            Dictionary<Guid, int> materalCosts = new Dictionary<Guid, int>();
            Dictionary<Guid, int> componentCosts = new Dictionary<Guid, int>();

            foreach (var kvp in componentDesign.MineralCostValues)
            {

                if (staticData.ProcessedMaterials.ContainsKey(kvp.Key))
                {
                    materalCosts.Add(kvp.Key, kvp.Value);
                }
                else if (staticData.ComponentTemplates.ContainsKey(kvp.Key))
                {
                    componentCosts.Add(kvp.Key, kvp.Value);
                }
                else if (staticData.Minerals.ContainsKey(kvp.Key))
                {
                    mineralCosts.Add(kvp.Key, kvp.Value);
                }
                else
                    throw new Exception("GUID object {" + kvp.Key + "} not found in materialCosting for " + componentDesign.Name + " This object needs to be either a mineral, material or component defined in the Data folder");
            }

            ComponentInfoDB componentInfo = new ComponentInfoDB(component.Guid, componentDesign.MassValue, componentDesign.HTKValue, componentDesign.BuildCostValue , mineralCosts,materalCosts,componentCosts , tech.ID, componentDesign.CrewReqValue);
            componentInfo.ComponentMountType = componentDesign.ComponentMountType;
            componentInfo.ConstructionType = componentDesign.ConstructionType;
            CargoAbleTypeDB cargoType = new CargoAbleTypeDB(componentDesign.CargoTypeID);

            component.SetDataBlob(componentInfo);
            component.SetDataBlob(nameDB);
            component.SetDataBlob(cargoType);
            component.SetDataBlob(MassVolumeDB.NewFromMassAndVolume(componentDesign.MassValue, componentDesign.VolumeValue));
            foreach (var designAbility in componentDesign.ComponentDesignAbilities)
            {
                if (designAbility.DataBlobType != null)
                {
                    if (designAbility.DataBlobArgs == null)
                        designAbility.SetValue();  //force recalc.
                                 
                    object[] constructorArgs = designAbility.DataBlobArgs;
                    dynamic datablob = (BaseDataBlob)Activator.CreateInstance(designAbility.DataBlobType, constructorArgs);
                    component.SetDataBlob(datablob);
                }
            }

            faction.InternalComponentDesigns.Add(component.Guid,component);
            return component;
        }        
    }
}