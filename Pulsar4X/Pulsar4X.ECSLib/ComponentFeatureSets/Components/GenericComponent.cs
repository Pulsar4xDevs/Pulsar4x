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
            design.RawName = component.Name;
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

            design.ComponentDesignAttributes = new List<ComponentDesignAttribute>();
            foreach (var abilitySD in component.ComponentAbilitySDs)
            {
                ComponentDesignAttribute designAttribute = new ComponentDesignAttribute(design);

                designAttribute.Name = abilitySD.Name;
                designAttribute.Description = abilitySD.Description;
                designAttribute.GuiHint = abilitySD.GuiHint;

                if(abilitySD.AbilityFormula !=  null)
                    designAttribute.Formula = new ChainedExpression(abilitySD.AbilityFormula, designAttribute, factionTech, staticData);

                if (abilitySD.GuidDictionary != null )
                {
                    designAttribute.GuidDictionary = new Dictionary<object, ChainedExpression>();
                    if (designAttribute.GuiHint == GuiHint.GuiTechSelectionList)
                    {
                        foreach (var kvp in abilitySD.GuidDictionary)
                        {
                            if (factionTech.ResearchedTechs.ContainsKey(Guid.Parse(kvp.Key.ToString())))
                            {
                                TechSD techSD = staticData.Techs[Guid.Parse(kvp.Key.ToString())];
                                designAttribute.GuidDictionary.Add(kvp.Key, new ChainedExpression(ResearchProcessor.DataFormula(factionTech, techSD).ToString(), designAttribute, factionTech, staticData));                      
                            }
                        }
                    }
                    else
                    {
                        foreach (var kvp in abilitySD.GuidDictionary)
                        {
                            designAttribute.GuidDictionary.Add(kvp.Key, new ChainedExpression(kvp.Value, designAttribute, factionTech, staticData));
                        }
                    }
                }
                if (designAttribute.GuiHint == GuiHint.GuiSelectionMaxMin)
                {
                    designAttribute.MaxValueFormula = new ChainedExpression(abilitySD.MaxFormula, designAttribute, factionTech, staticData);
                    designAttribute.MinValueFormula = new ChainedExpression(abilitySD.MinFormula, designAttribute, factionTech, staticData);
                    designAttribute.StepValueFormula = new ChainedExpression(abilitySD.StepFormula, designAttribute, factionTech, staticData);
                }
                if (abilitySD.AbilityDataBlobType != null)
                {
                    designAttribute.DataBlobType = Type.GetType(abilitySD.AbilityDataBlobType);        
                }
                
                design.ComponentDesignAttributes.Add(designAttribute);
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
            Entity component = new Entity(globalEntityManager, factionEntity);
            
            TechSD tech = new TechSD();
            tech.ID = Guid.NewGuid();
            tech.Name = componentDesign.Name + " Design Research";
            tech.Description = "Research into building " + componentDesign.Name;
            tech.MaxLevel = 1;
            tech.CostFormula = componentDesign.ResearchCostValue.ToString();

            factionTech.ResearchableTechs.Add(tech, 0);
            NameDB nameDB = new NameDB(componentDesign.RawName);
            nameDB.SetName(factionEntity.Guid, componentDesign.Name);
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
            //note: MassVolumeDB stores mass in kg and volume in km^3, however we use kg and m^3 in the json data. 
            component.SetDataBlob(MassVolumeDB.NewFromMassAndVolume(componentDesign.MassValue, componentDesign.VolumeValue * 1e-9));
            foreach (var designAttribute in componentDesign.ComponentDesignAttributes)
            {
                if (designAttribute.DataBlobType != null)
                {
                    if (designAttribute.DataBlobArgs == null)
                        designAttribute.SetValue();  //force recalc.
                                 
                    object[] constructorArgs = designAttribute.DataBlobArgs;
                    dynamic datablob = (BaseDataBlob)Activator.CreateInstance(designAttribute.DataBlobType, constructorArgs);
                    component.SetDataBlob(datablob);
                    if(datablob is IComponentDesignAttribute)
                        componentInfo.DesignAttributes.Add(datablob);
                }
            }

            faction.InternalComponentDesigns.Add(component.Guid,component);
            return component;
        }        
    }
}