using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.ECSLib.Industry;


namespace Pulsar4X.ECSLib
{
    [Flags]
    public enum GuiHint
    {
        None = 1,
        GuiDisplayBool = 2,
        GuiTechSelectionList = 4,
        GuiSelectionMaxMin = 8,
        GuiTextDisplay = 16,
        GuiEnumSelectionList = 32,
        GuiOrdnanceSelectionList = 64,
        GuiTextSelectionFormula = 128,
    }

    public static class ComponentParseCheck
    {
        public static bool IsParseable(ComponentTemplateSD componentSD, out List<(string formula,string error)> errors)
        {
            errors = new List<(string formula,string error)>();
            var factionTech = new FactionTechDB();
            ComponentDesigner designer;
            try
            {
                designer = new ComponentDesigner(componentSD, factionTech);
                
                List<ChainedExpression> allExpressions = new List<ChainedExpression>()
                {
                    designer.MassFormula,
                    designer.VolumeFormula,
                    designer.CrewFormula,
                    designer.HTKFormula,
                    designer.ResearchCostFormula,
                    designer.BuildCostFormula,
                    designer.CreditCostFormula
                };
                allExpressions.AddRange(designer.ResourceCostFormulas.Values);
                
                //allExpressions.AddRange(designer.MineralCostFormulas.Values);
                //allExpressions.AddRange(designer.MaterialCostFormulas.Values);
                //allExpressions.AddRange(designer.ComponentCostFormulas.Values);
                foreach (var value in designer.ComponentDesignAttributes.Values)
                {
                    allExpressions.Add(value.Formula);
                    if(value.MaxValueFormula != null)
                        allExpressions.Add(value.MaxValueFormula);
                    if(value.MinValueFormula != null)
                        allExpressions.Add(value.MinValueFormula);
                    if(value.StepValueFormula != null)
                        allExpressions.Add(value.StepValueFormula);
                }

                foreach (var expression in allExpressions)
                {
                    if (expression == null)
                    {
                        errors.Add(("Null Value", "Unexpected Null Value for Formula"));
                    }

                    else if (expression.HasErrors())
                    {
                        errors.Add((expression.RawExpressionString, expression.Error()));
                    }
                }
            }
            catch (Exception e)
            {
                string errorMessage = "Malformed ComponentTemplate, this error happened before the specific formula could be found \r\n " + e.Message;
                errors.Add(("", errorMessage));
            }
            return (errors.Count == 0);
        }
    }

    public class ComponentDesign : ICargoable, IConstrucableDesign
    {
        public ConstructableGuiHints GuiHints { get; set; } 
        public Guid ID { get; internal set; }
        public string Name { get; internal set; } //player defined name. ie "5t 2kn Thruster".
        
        public Guid CargoTypeID { get; internal set; }
        public long MassPerUnit { get; internal set; }

        public double VolumePerUnit { get; internal set; }

        public double Density { get; internal set; }

        public long ResearchCostValue;
        public Guid TechID;
        public string TypeName; //ie the name in staticData. ie "Newtonion Thruster".
        public string Description;
        //public int Volume_m3 = 1;
        public int HTK;
        public int CrewReq;
        public long IndustryPointCosts { get; set; }
        public Guid IndustryTypeID { get; set; }


        public int CreditCost;
        
        //public int ResearchCostValue;
        public Dictionary<Guid, long> ResourceCosts { get; internal set; } = new Dictionary<Guid, long>();

        public ComponentMountType ComponentMountType;
        //public List<ComponentDesignAtbData> ComponentDesignAttributes;
        public Dictionary<Type, IComponentDesignAttribute> AttributesByType = new Dictionary<Type, IComponentDesignAttribute>();

        public Connections Connections = 0;
        public float AspectRatio = 1f;
        public DamageResist DamageResistance;
        
        
        public void OnConstructionComplete(Entity industryEntity, VolumeStorageDB storage, Guid productionLine, IndustryJob batchJob, IConstrucableDesign designInfo)
        {
            var colonyConstruction = industryEntity.GetDataBlob<IndustryAbilityDB>();
            batchJob.NumberCompleted++;
            batchJob.ResourcesRequired = designInfo.ResourceCosts;

            batchJob.ProductionPointsLeft = designInfo.IndustryPointCosts;


            if (batchJob.InstallOn != null)
            {
               ComponentInstance specificComponent = new ComponentInstance((ComponentDesign)designInfo);
               if (batchJob.InstallOn == industryEntity || StorageSpaceProcessor.HasEntity(storage, batchJob.InstallOn.GetDataBlob<CargoAbleTypeDB>()))
               {
                   EntityManipulation.AddComponentToEntity(batchJob.InstallOn, specificComponent);
                   ReCalcProcessor.ReCalcAbilities(batchJob.InstallOn);
               }
            }
            else
            {
                storage.AddCargoByUnit((ComponentDesign)designInfo, 1);
               //StorageSpaceProcessor.AddCargo(storage, (ComponentDesign)designInfo, 1);
            }

            if (batchJob.NumberCompleted == batchJob.NumberOrdered)
            {
               colonyConstruction.ProductionLines[productionLine].Jobs.Remove(batchJob);
               if (batchJob.Auto)
               {
                   colonyConstruction.ProductionLines[productionLine].Jobs.Add(batchJob);
               }
            }
        }
     
        public bool HasAttribute<T>()
            where T : IComponentDesignAttribute
        {
            return AttributesByType.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Will throw an exception if it doesn't have the type of attribute.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetAttribute<T>() 
            where T : IComponentDesignAttribute
        {
            return (T)AttributesByType[typeof(T)];
        }
        
        public bool TryGetAttribute<T>(out T attribute)
            where T : IComponentDesignAttribute
        {
            if (HasAttribute<T>())
            {
                attribute = (T)AttributesByType[typeof(T)];
                return true;
            }
            attribute = default(T);
            return false;
        }
    }
    
    /// <summary>
    /// This object is what the UI manipulates to create a player design
    /// the ComponentDesignData is then set.
    /// </summary>
    public class ComponentDesigner
    {
        ComponentDesign _design = new ComponentDesign();
        
        public ComponentDesigner(ComponentTemplateSD componentSD, FactionTechDB factionTech)
        {
            var staticData = StaticRefLib.StaticData;
            TypeName = componentSD.Name;
            Name = componentSD.Name;
            
            _design.ID = Guid.NewGuid();
            MassFormula = new ChainedExpression(componentSD.MassFormula, this, factionTech, staticData);
            VolumeFormula = new ChainedExpression(componentSD.VolumeFormula, this, factionTech, staticData);
            CrewFormula = new ChainedExpression(componentSD.CrewReqFormula, this, factionTech, staticData);
            HTKFormula = new ChainedExpression(componentSD.HTKFormula, this, factionTech, staticData);
            ResearchCostFormula = new ChainedExpression(componentSD.ResearchCostFormula, this, factionTech, staticData);
            BuildCostFormula = new ChainedExpression(componentSD.BuildPointCostFormula, this, factionTech, staticData);
            CreditCostFormula = new ChainedExpression(componentSD.CreditCostFormula, this, factionTech, staticData);
            ComponentMountType = componentSD.MountType;
            IndustryType = componentSD.IndustryTypeID;
            CargoTypeID = componentSD.CargoTypeID;
            _design.CargoTypeID = componentSD.CargoTypeID;
            if (componentSD.MountType.HasFlag(ComponentMountType.PlanetInstallation))
                _design.GuiHints = ConstructableGuiHints.CanBeInstalled;
            if(!string.IsNullOrEmpty(componentSD.DescriptionFormula))
                DescriptionFormula = new ChainedExpression(componentSD.DescriptionFormula, this, factionTech, staticData);

            Dictionary<Guid, ChainedExpression> resourceCostForulas = new Dictionary<Guid, ChainedExpression>();

            foreach (var kvp in componentSD.ResourceCostFormula)
            {
                if(staticData.CargoGoods.GetAny(kvp.Key) != null)
                    resourceCostForulas.Add(kvp.Key, new ChainedExpression(kvp.Value, this, factionTech));
                else //TODO: log don't crash.
                    throw new Exception("GUID object {" + kvp.Key + "} not found in resourceCosting for " + this.TypeName + " This object needs to be either a mineral, material or component defined in the Data folder");
            }

            ResourceCostFormulas = resourceCostForulas;
            
            foreach (ComponentTemplateAttributeSD attrbSD in componentSD.ComponentAtbSDs)
            {
                ComponentDesignAttribute designAttribute = new ComponentDesignAttribute(this, attrbSD, factionTech);
                ComponentDesignAttributes.Add(designAttribute.Name, designAttribute);
                ComponentDesignAttributeList.Add(designAttribute);
            }

            EvalAll();
        }


        public void SetAttributes()
        {
            EvalAll();
            foreach (var designAttribute in ComponentDesignAttributes.Values)
            {
                if (designAttribute.AttributeType != null && designAttribute.IsEnabled)
                {
                    //if (designAttribute.AtbConstrArgs == null)
                    designAttribute.SetValue();  //force recalc.
                                 
                    object[] constructorArgs = designAttribute.AtbConstrArgs;
                    try
                    {
                        dynamic attrbute = (IComponentDesignAttribute)Activator.CreateInstance(designAttribute.AttributeType, constructorArgs);
                        _design.AttributesByType[attrbute.GetType()] = attrbute;
                    }
                    catch (MissingMethodException e)
                    {
                        string argTypes = "";
                        int i = 0;
                        foreach (var arg in constructorArgs)
                        {
                            argTypes += arg.GetType() + ": " + constructorArgs[i].ToString() + ",\n";
                            
                            i++;
                        }
                        
                        string exstr = "The Attribute: " + designAttribute.AttributeType + " in "+ _design.Name + " was found, but the arguments did not match any constructors.\nThe given arguments are:\n"
                                       + argTypes 
                                       + "The full exception is as follows:\n" + e;
                        throw new Exception(exstr);
                    }
                }
            }
        }

        /// <summary>
        /// "Set" and returns the designdata
        /// this also sets up a research item for the design,
        /// and adds it to the factions designs.
        /// </summary>
        /// <returns></returns>
        public ComponentDesign CreateDesign(Entity factionEntity)
        {
 
            FactionInfoDB faction = factionEntity.GetDataBlob<FactionInfoDB>();
            
            //set up the research
            FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            TechSD tech = new TechSD();
            tech.ID = Guid.NewGuid();
            tech.Name = _design.Name + " Design Research";
            tech.Description = "Research into building " + _design.Name;
            tech.MaxLevel = 1;
            tech.CostFormula = _design.ResearchCostValue.ToString();


            _design.TechID = tech.ID;
            factionTech.MakeResearchable(tech); //add it to researchable techs 
            
            SetAttributes();

            faction.InternalComponentDesigns[_design.ID] = _design;
            faction.IndustryDesigns[_design.ID] = _design;
            return _design;
            
        }


        public void EvalAll()
        {
            SetMass();
            SetVolume();
            SetCreditCost();
            SetCrew();
            SetHTK();
            SetResearchCost();
            SetBuildCost();
            SetResourceCosts();

        }

        public string TypeName
        {
            get { return _design.TypeName; }
            set { _design.TypeName = value; }
        }

        public string Name 
        {             
            get { return _design.Name; }
            set { _design.Name = value; } 
        }


        internal ChainedExpression DescriptionFormula { get; set; }
        
        public string Description
        {
            get
            {
                if (DescriptionFormula == null)
                    return "";
                else
                    return DescriptionFormula.StrResult;
            }
        }

        

        public long MassValue
        {
            get { return _design.MassPerUnit; }
        }
        internal ChainedExpression MassFormula { get; set; }
        public void SetMass()
        {
            MassFormula.Evaluate();
            _design.MassPerUnit = MassFormula.LongResult;
            _design.Density = _design.MassPerUnit / _design.VolumePerUnit;
        }

        public double VolumeM3Value { get { return _design.VolumePerUnit; } }//TODO: check units are @SI UNITS kg/m^3
        internal ChainedExpression VolumeFormula { get; set; }
        public void SetVolume()
        {
            VolumeFormula.Evaluate();
            _design.VolumePerUnit = VolumeFormula.LongResult;
            _design.Density = _design.MassPerUnit / _design.VolumePerUnit;
        }


        public int HTKValue { get { return _design.HTK; } }
        internal ChainedExpression HTKFormula { get; set; }
        public void SetHTK()
        {
            HTKFormula.Evaluate();
            _design.HTK = HTKFormula.IntResult;
        }

        public int CrewReqValue { get { return _design.CrewReq ; } }
        internal ChainedExpression CrewFormula { get; set; }
        public void SetCrew()
        {
            CrewFormula.Evaluate();
            _design.CrewReq = CrewFormula.IntResult;
        }

        public long ResearchCostValue { get { return _design.ResearchCostValue; } }
        internal ChainedExpression ResearchCostFormula { get; set; }
        public void SetResearchCost()
        {
            ResearchCostFormula.Evaluate();
            _design.ResearchCostValue = ResearchCostFormula.LongResult;
        }

        public long IndustryPointCostsValue { get { return _design.IndustryPointCosts; } }
        internal ChainedExpression BuildCostFormula { get; set; }
        public void SetBuildCost()
        {
            BuildCostFormula.Evaluate();
            _design.IndustryPointCosts = BuildCostFormula.LongResult;
        }

        public Dictionary<Guid, long> ResourceCostValues => _design.ResourceCosts;
        internal Dictionary<Guid, ChainedExpression> ResourceCostFormulas { get; set; }
        public void SetResourceCosts()
        {
            Dictionary<Guid, long> dict = new Dictionary<Guid, long>();
            foreach (var kvp in ResourceCostFormulas)
            {
                kvp.Value.Evaluate();
                dict.Add(kvp.Key, kvp.Value.LongResult);  
            }
            _design.ResourceCosts = dict;
        }
        
        /*
        public Dictionary<Guid, int> MineralCostValues => _design.MineralCosts;
        internal Dictionary<Guid, ChainedExpression> MineralCostFormulas { get; set; }
        public void SetMineralCosts()
        {
            Dictionary<Guid,int> dict = new Dictionary<Guid, int>();
            foreach (var kvp in MineralCostFormulas)
            {
                kvp.Value.Evaluate();
                dict.Add(kvp.Key, kvp.Value.IntResult);  
            }
            _design.MineralCosts = dict;
        }
        
        public Dictionary<Guid, int> MaterialCostValues => _design.MaterialCosts;
        internal Dictionary<Guid, ChainedExpression> MaterialCostFormulas { get; set; }
        public void SetMaterialCosts()
        {
            Dictionary<Guid,int> dict = new Dictionary<Guid, int>();
            foreach (var kvp in MaterialCostFormulas)
            {
                kvp.Value.Evaluate();
                dict.Add(kvp.Key, kvp.Value.IntResult);  
            }
            _design.MaterialCosts = dict;
        }
        
        public Dictionary<Guid, int> ComponentCostValues => _design.ComponentCosts;
        internal Dictionary<Guid, ChainedExpression> ComponentCostFormulas { get; set; }
        public void SetComponentCosts()
        {
            Dictionary<Guid,int> dict = new Dictionary<Guid, int>();
            foreach (var kvp in ComponentCostFormulas)
            {
                kvp.Value.Evaluate();
                dict.Add(kvp.Key, kvp.Value.IntResult);  
            }
            _design.ComponentCosts = dict;
        }
        */
        
        public int CreditCostValue => _design.CreditCost;
        internal ChainedExpression CreditCostFormula { get; set; }
        public void SetCreditCost()
        {
            CreditCostFormula.Evaluate();
            _design.CreditCost = CreditCostFormula.IntResult;
        }

        public ComponentMountType ComponentMountType
        {
            get { return _design.ComponentMountType;} 
            internal set { _design.ComponentMountType = value; } }
        public Guid IndustryType
        {
            get { return _design.IndustryTypeID; }
            internal set { _design.IndustryTypeID = value; }
        }
        public Guid CargoTypeID 
        {             
            get { return _design.CargoTypeID; }
            internal set { _design.CargoTypeID = value; } 
        }
        
        [Obsolete]//don't use this, TODO: get rid of this once json data is rewritten to use names instead of indexes
        internal List<ComponentDesignAttribute> ComponentDesignAttributeList = new List<ComponentDesignAttribute>();
        
        public Dictionary<string, ComponentDesignAttribute> ComponentDesignAttributes = new Dictionary<string, ComponentDesignAttribute>();
        public Dictionary<Type, IComponentDesignAttribute> Attributes
        {
            get { return _design.AttributesByType; }
        }

        public T GetAttribute<T>() 
            where T : IComponentDesignAttribute
        {
            return (T)_design.AttributesByType[typeof(T)];
        }
        
        public bool TryGetAttribute<T>(out T attribute)
            where T : IComponentDesignAttribute
        {
            if (Attributes.ContainsKey(typeof(T)))
            {
                attribute = (T)_design.AttributesByType[typeof(T)];
                return true;
            }
            attribute = default(T);
            return false;
        }
    }
}
