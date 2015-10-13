using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{  
    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct ComponentSD
    {
        public string Name;
        public string Description;
        public Guid ID;

        public string SizeFormula;
        public string HTKFormula;
        public string CrewReqFormula;
        public JDictionary<Guid,string> MineralCostFormula;
        public string ResearchCostFormula;
        public string CreditCostFormula;
        public string BuildPointCostFormula;
        //if it can be fitted to a ship as a ship component, on a planet as an installation, can be cargo etc.
        public JDictionary<ComponentMountType, bool> MountType; 

        public List<ComponentAbilitySD> ComponentAbilitySDs;
    }

    [StaticDataAttribute(false)]
    public struct ComponentAbilitySD
    {
        public string Name;
        public string Description;
        public GuiHint GuiHint; //if AbilityFormula uses AbilityArgs(), this should be none!

        //used if guihint is GuiSelectionList
        public JDictionary<Guid, string> GuidDictionary;

        //used if GuiHint is GuiMinMax
        public string MaxFormula;
        public string MinFormula;

        //if guihint is selection list or minmax, this should point to a default value. 
        public string AbilityFormula;

        public string AbilityDataBlobType;
    }
}
