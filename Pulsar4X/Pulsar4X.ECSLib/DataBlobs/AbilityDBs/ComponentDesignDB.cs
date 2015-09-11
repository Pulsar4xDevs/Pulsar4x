using System;
using System.Collections.Generic;


namespace Pulsar4X.ECSLib
{
    public enum GuiHint
    {
        GuiSelectionList,
        GuiSelectionMaxMin,
        GuiTextDisplay,
        None
    }
    public class ComponentDesignDB
    {
        public string Name;
        public string Description;

        //internal ParsingProcessor Parser { get; private set; }

        public GuiHint SizeGuiHint { get; internal set; }
        public int SizeValue { get { return SizeFormula.IntResult; } }
        internal ChainedExpression SizeFormula { get; set; }
        public void SetSize()
        {
            SizeFormula.Evaluate();
        }

        public GuiHint HTKGuiHint { get; internal set; }
        public int HTKValue { get { return HTKFormula.IntResult; } }
        internal ChainedExpression HTKFormula { get; set; }
        public void SetHTK()
        {
            HTKFormula.Evaluate();
        }

        public GuiHint CrewReqGuiHint { get; internal set; }
        public int CrewReqValue { get { return CrewFormula.IntResult; } }
        internal ChainedExpression CrewFormula { get; set; }
        public void SetCrew()
        {
            CrewFormula.Evaluate();
        }

        public GuiHint RessearchGuiHint { get; internal set; }
        public int ResearchCostValue { get { return ResearchCostFormula.IntResult; } }
        internal ChainedExpression ResearchCostFormula { get; set; }
        public void SetResearchCost()
        {
            ResearchCostFormula.Evaluate();
        }

        public GuiHint MineralCostGuiHint { get; internal set; }
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

        public Dictionary<TechSD, double> SelectionDictionary;

        public void SetValueFromTechList(TechSD tech)
        {
            Formula.ReplaceExpression("TechData('" + tech.ID + "')");
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
            object result = MinValueFormula.Result;
            if (result is int)
                MinValue = (double)(int)result;
            else
                MinValue = (double)result;
        }
        public double MaxValue;
        internal ChainedExpression MaxValueFormula { get; set; }
        public void SetMax()
        {
            MaxValueFormula.Evaluate();
            object result = MaxValueFormula.Result;
            if (result is int)
                MaxValue = (double)(int)result;
            else
                MaxValue = (double)result;
        }

        internal object[] DataBlobArgs { get; set; }
    }

}
