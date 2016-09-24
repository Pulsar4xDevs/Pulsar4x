using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct ComponentTemplateSD
    {
        public string Name;
        public string Description;
        public Guid ID;

        public string MassFormula;
        public string VolumeFormula;
        public string HTKFormula;       
        public string CrewReqFormula;
        public Dictionary<Guid,string> MineralCostFormula;
        public string ResearchCostFormula;
        public string CreditCostFormula;
        public string BuildPointCostFormula;
        //if it can be fitted to a ship as a ship component, on a planet as an installation, can be cargo etc.
        public ComponentMountType MountType;
        public ConstructionType ConstructionType;
        public Guid CargoTypeID; //cargo TypeID from CargoTypeSD
        public List<ComponentTemplateAbilitySD> ComponentAbilitySDs;
    }

    [StaticDataAttribute(false)]
    public struct ComponentTemplateAbilitySD
    {
        public string Name;
        public string Description;
        public GuiHint GuiHint; //if AbilityFormula uses AbilityArgs(), this should be none!

        //used if guihint is GuiSelectionList
        public Dictionary<object, string> GuidDictionary;

        //used if GuiHint is GuiMinMax
        public string MaxFormula;
        public string MinFormula;
        public string StepFormula; 

        //if guihint is selection list or minmax, this should point to a default value. 
        public string AbilityFormula;

        public string AbilityDataBlobType;
    }
}
