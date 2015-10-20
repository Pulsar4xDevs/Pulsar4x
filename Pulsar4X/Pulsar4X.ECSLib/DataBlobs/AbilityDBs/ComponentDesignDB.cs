using System;
using System.Collections.Generic;


namespace Pulsar4X.ECSLib
{
    public enum GuiHint
    {
        GuiTechSelectionList,
        GuiSelectionMaxMin,
        GuiTextDisplay,
        None
    }
    public class ComponentDesignDB
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public int SizeValue { get { return SizeFormula.IntResult; } }
        internal ChainedExpression SizeFormula { get; set; }
        public void SetSize()
        {
            SizeFormula.Evaluate();
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

        public JDictionary<Guid, int> MineralCostValues {
            get
            {
                JDictionary<Guid,int> dict = new JDictionary<Guid, int>();
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

        public Dictionary<ComponentMountType, bool> ComponentMountType { get; internal set; }

        public List<ComponentDesignAbilityDB> ComponentDesignAbilities;

    }

    public class ComponentDesignAbilityDB
    {
        public string Name;
        public string Description;

        public GuiHint GuiHint;
        public Type DataBlobType;
        //public BaseDataBlob DataBlob;
        internal ComponentDesignDB ParentComponent; 
        public ComponentDesignAbilityDB(ComponentDesignDB parentComponent)
        {
            ParentComponent = parentComponent;
        }

        public Dictionary<Guid, ChainedExpression> GuidDictionary;

        public void SetValueFromGuidList(Guid techguid)
        {
            Formula.ReplaceExpression("TechData('" + techguid + "')");
        }

        internal ChainedExpression Formula { get; set; }
        public void SetValue()
        {
            Formula.Evaluate();
        }

        public void SetValueFromInput(double input)
        {
            SetMin();
            SetMax();
            if (input < MinValue)
                input = MinValue;
            else if (input > MaxValue)
                input = MaxValue;
            Formula.ReplaceExpression(input.ToString()); //prevents it being reset to the default value on Evaluate;
            Formula.Evaluate();//force dependants to recalc.
        }

        public double Value { get { return Formula.DResult; } }

        public double MinValue;
        internal ChainedExpression MinValueFormula { get; set; }
        public void SetMin()
        {
            MinValueFormula.Evaluate();
            MinValue = MinValueFormula.DResult;
        }
        public double MaxValue;
        internal ChainedExpression MaxValueFormula { get; set; }
        public void SetMax()
        {
            MaxValueFormula.Evaluate();
            MaxValue = MaxValueFormula.DResult;
        }

        internal object[] DataBlobArgs { get; set; }
    }

}
