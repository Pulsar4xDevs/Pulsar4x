using System;
using System.Collections.Generic;


namespace Pulsar4X.ECSLib
{
    public enum GuiHint
    {
        None,
        GuiTechSelectionList,
        GuiSelectionMaxMin,
        GuiTextDisplay
        
    }
    public class ComponentDesign
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public int MassValue { get { return MassFormula.IntResult; } }
        internal ChainedExpression MassFormula { get; set; }
        public void SetMass()
        {
            MassFormula.Evaluate();
        }

        public int VolumeValue { get { return VolumeFormula.IntResult; } }
        internal ChainedExpression VolumeFormula { get; set; }
        public void SetVolume()
        {
            VolumeFormula.Evaluate();
        }


        public int HTKValue { get { return HTKFormula.IntResult; } }
        internal ChainedExpression HTKFormula { get; set; }
        public void SetHTK()
        {
            HTKFormula.Evaluate();
        }

        public int CrewReqValue { get { return CrewFormula.IntResult; } }
        internal ChainedExpression CrewFormula { get; set; }
        public void SetCrew()
        {
            CrewFormula.Evaluate();
        }

        public int ResearchCostValue { get { return ResearchCostFormula.IntResult; } }
        internal ChainedExpression ResearchCostFormula { get; set; }
        public void SetResearchCost()
        {
            ResearchCostFormula.Evaluate();
        }

        public int BuildCostValue { get { return BuildCostFormula.IntResult; } }
        internal ChainedExpression BuildCostFormula { get; set; }
        public void SetBuildCost()
        {
            BuildCostFormula.Evaluate();
        }

        public Dictionary<Guid, int> MineralCostValues {
            get
            {
                Dictionary<Guid,int> dict = new Dictionary<Guid, int>();
                foreach (var kvp in MineralCostFormulas)
                {
                    dict.Add(kvp.Key, kvp.Value.IntResult);  
                }
                return dict;
            }
        }
        internal Dictionary<Guid, ChainedExpression> MineralCostFormulas { get; set; }
        public void SetMineralCosts()
        {
            foreach (var expression in MineralCostFormulas.Values)
            {
                expression.Evaluate();
            }
        }

        public int CreditCostValue { get { return ResearchCostFormula.IntResult; } }
        internal ChainedExpression CreditCostFormula { get; set; }
        public void SetCreditCost()
        {
            CreditCostFormula.Evaluate();
        }

        public ComponentMountType ComponentMountType { get; internal set; }
        public ConstructionType ConstructionType { get; internal set; }
        public Guid CargoTypeID { get; internal set; }
        public List<ComponentDesignAbility> ComponentDesignAbilities;

    }
}
