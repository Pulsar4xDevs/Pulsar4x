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
        public GuiHint SizeGuiHint;

        public string HTKFormula;
        public GuiHint HTKGuiHint;

        public string CrewReqFormula;
        public GuiHint CrewReqGuiHint;

        public JDictionary<Guid,string> MineralCostFormula;
        public GuiHint MineralCostGuiHint;

        public string ResearchCostFormula;
        public GuiHint ResearchCostGuiHint;

        public string CreditCostFormula;
        public GuiHint CreditCostGuiHint;

        //if it can be fitted to a ship as a ship component, on a planet as an installation, can be cargo etc.
        public JDictionary<ComponentMountType, bool> MountType; 

        public List<ComponentAbilitySD> ComponentAbilitySDs;
    }

    [StaticDataAttribute(false)]
    public struct ComponentAbilitySD
    {
        public string Name;
        public string Description;
        public GuiHint GuiHint;

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
