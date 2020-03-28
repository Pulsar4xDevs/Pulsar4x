using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.ECSLib
{
    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct ComponentTemplateSD
    {
        public string Name;
        public string DescriptionFormula;
        public Guid ID;

        public string MassFormula;
        public string VolumeFormula;
        public string HTKFormula;       
        public string CrewReqFormula;
        public Dictionary<Guid,string> ResourceCostFormula; //mins, mats and components can also be included here.
        public string ResearchCostFormula;
        public string CreditCostFormula;
        public string BuildPointCostFormula;
        //if it can be fitted to a ship as a ship component, on a planet as an installation, can be cargo etc.
        public ComponentMountType MountType;
        public Guid IndustryTypeID;
        public Guid CargoTypeID; //cargo TypeID from CargoTypeSD
        public List<ComponentTemplateAttributeSD> ComponentAtbSDs;
    }

    [StaticDataAttribute(false)]
    public struct ComponentTemplateAttributeSD
    {
        public string Name;
        public string DescriptionFormula;
        public string Unit;
        public GuiHint GuiHint; //if AttributeFormula uses AbilityArgs(), this should be none!

        public string GuiIsEnabledFormula; //if this attribute should be displayed/enabled
        //used if guihint is GuiSelectionList
        public Dictionary<object, string> GuidDictionary;

        public string EnumTypeName;
        //public string[] EnumItems;
        //public string[] EnumTechReq;
        //public string[] EnumFormula;
        
        //used if GuiHint is GuiMinMax
        public string MaxFormula;
        public string MinFormula;
        public string StepFormula;
        

        //if guihint is selection list or minmax, this should point to a default value. 
        public string AttributeFormula;

        public string AttributeType;
    }
}
