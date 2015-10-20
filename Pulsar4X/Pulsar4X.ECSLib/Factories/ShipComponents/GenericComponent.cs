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
        public static ComponentDesignDB StaticToDesign(ComponentSD component, FactionTechDB factionTech, StaticDataStore staticData)
        {
            ComponentDesignDB design = new ComponentDesignDB();

            design.Name = component.Name;
            design.Description = component.Description;

            design.SizeFormula = new ChainedExpression(component.SizeFormula, design, factionTech, staticData);
            design.CrewFormula = new ChainedExpression(component.CrewReqFormula, design, factionTech, staticData);
            design.HTKFormula = new ChainedExpression(component.HTKFormula, design, factionTech, staticData);
            design.ResearchCostFormula = new ChainedExpression(component.ResearchCostFormula, design, factionTech, staticData);
            design.BuildCostFormula = new ChainedExpression(component.BuildPointCostFormula, design, factionTech, staticData);
            design.MineralCostFormulas = new Dictionary<Guid, ChainedExpression>();
            design.CreditCostFormula = new ChainedExpression(component.CreditCostFormula, design, factionTech, staticData);



            foreach (var kvp in component.MineralCostFormula)
            {
                design.MineralCostFormulas.Add(kvp.Key, new ChainedExpression(kvp.Value, design, factionTech, staticData));
            }

            design.ComponentDesignAbilities = new List<ComponentDesignAbilityDB>();
            foreach (var abilitySD in component.ComponentAbilitySDs)
            {
                ComponentDesignAbilityDB designAbility = new ComponentDesignAbilityDB(design);

                designAbility.Name = abilitySD.Name;
                designAbility.Description = abilitySD.Description;
                designAbility.GuiHint = abilitySD.GuiHint;

                if(abilitySD.AbilityFormula !=  null)
                    designAbility.Formula = new ChainedExpression(abilitySD.AbilityFormula, designAbility, factionTech, staticData);

                if (abilitySD.GuidDictionary != null)
                {
                    designAbility.GuidDictionary = new Dictionary<Guid, ChainedExpression>();
                    if (designAbility.GuiHint == GuiHint.GuiTechSelectionList)
                    {
                        foreach (var kvp in abilitySD.GuidDictionary)
                        {
                            if (factionTech.ResearchedTechs.ContainsKey(kvp.Key))
                            {
                                TechSD techSD = staticData.Techs[kvp.Key];
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
                }
                if (abilitySD.AbilityDataBlobType != null)
                {
                    designAbility.DataBlobType = Type.GetType(abilitySD.AbilityDataBlobType);        
                }
                
                design.ComponentDesignAbilities.Add(designAbility);
            }

            design.SizeFormula.Evaluate();
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
        public static Entity DesignToEntity(Game game, Entity factionEntity, ComponentDesignDB componentDesign)
        {
            EntityManager globalEntityManager = game.GlobalManager;
            StaticDataStore staticData = game.StaticData;
            FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            FactionInfoDB factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
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

            JDictionary<Guid, int> mineralCosts = new JDictionary<Guid, int>();
            JDictionary<Guid, int> materalCosts = new JDictionary<Guid, int>();
            JDictionary<Guid, int> componentCosts = new JDictionary<Guid, int>();

            foreach (var kvp in componentDesign.MineralCostValues)
            {

                if (staticData.RefinedMaterials.ContainsKey(kvp.Key))
                {
                    materalCosts.Add(kvp.Key, kvp.Value);
                }
                else if (staticData.Components.ContainsKey(kvp.Key))
                {
                    componentCosts.Add(kvp.Key, kvp.Value);
                }
                else if (staticData.Minerals.Any(item => item.ID == kvp.Key))
                {
                    mineralCosts.Add(kvp.Key, kvp.Value);
                }
                else
                    throw new Exception("GUID object {" + kvp.Key + "} not found in materialCosting for " + componentDesign.Name + " This object needs to be either a mineral, material or component defined in the Data folder");
            }

            ComponentInfoDB componentInfo = new ComponentInfoDB(component.Guid, componentDesign.SizeValue, componentDesign.HTKValue, componentDesign.BuildCostValue , mineralCosts,materalCosts,componentCosts , tech.ID, componentDesign.CrewReqValue);
            
            component.SetDataBlob(componentInfo);
            component.SetDataBlob(nameDB);
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

            factionInfo.ComponentDesigns.Add(component.Guid,component);
            return component;
        }        
    }
}