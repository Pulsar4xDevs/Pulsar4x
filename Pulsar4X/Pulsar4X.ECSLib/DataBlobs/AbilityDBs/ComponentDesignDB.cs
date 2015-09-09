using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NCalc;

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

        internal ParsingProcessor Parser { get; private set; }

        public GuiHint SizeGuiHint { get; private set; }
        public int SizeValue { get; private set; }
        internal Expression SizeFormula { get; set; }
        public void SetSize(int size)
        {
            SetMinSize();
            SetMinSize();
            if (size < MinSizeValue)
                SizeValue = MinSizeValue;
            if (size > MaxSizeValue)
                SizeValue = MaxSizeValue;
            else
                SizeValue = size;
        }

        public int MaxSizeValue { get; private set; }
        internal Expression MaxSizeFormula { get; set; }
        public void SetMaxSize()
        {
            object result = MaxSizeFormula.Evaluate();
            if (result is int)
                MaxSizeValue = (int)result;
            else if (result is double)
                MaxSizeValue = (int)(double)result;
        }

        public int MinSizeValue { get; private set; }
        internal Expression MinSizeFormula { get; set; }
        public void SetMinSize()
        {
            object result = MinSizeFormula.Evaluate();
            if (result is int)
                MinSizeValue = (int)result;
            else if (result is double)
                MinSizeValue = (int)(double)result;
        }

        public GuiHint HTKGuiHint { get; private set; }
        public int HTKValue { get; private set; }
        internal Expression HTKFormula { get; set; }
        public void SetHTK()
        {
            object result = HTKFormula.Evaluate();
            if (result is int)
                HTKValue = (int)result;
            else if (result is double)
                HTKValue = (int)(double)result;
        }

        public GuiHint CrewReqGuiHint { get; private set; }
        public int CrewReqValue { get; private set; }
        internal Expression CrewFormula { get; set; }
        public void SetCrew()
        {
            object result = CrewFormula.Evaluate();
            if (result is int)
                CrewReqValue = (int)result;
            else if (result is double)
                CrewReqValue = (int)(double)result;
        }

        public GuiHint RessearchGuiHint { get; private set; }
        public int ResearchCostValue { get; private set; }
        internal Expression ResearchCostFormula { get; set; }
        public void SetResearchCost()
        {
            object result = ResearchCostFormula.Evaluate();
            if (result is int)
                ResearchCostValue = (int)result;
            else if (result is double)
                ResearchCostValue = (int)(double)result;
        }

        public GuiHint MineralCostGuiHint { get; private set; }
        public JDictionary<Guid, int> MineralCostValues { get; private set; }
        internal Dictionary<Guid, Expression> CostFormulas { get; set; }
        public void SetCosts()
        {
            MineralCostValues = new JDictionary<Guid, int>();
            foreach (var kvp in CostFormulas)
            {
                object value = kvp.Value.Evaluate();
                int intvalue = (int)(double)value;
                MineralCostValues.Add(kvp.Key, intvalue);
            }
        }


        public List<ComponentDesignAbilityDB> ComponentDesignAbilities;

        public ComponentDesignDB(StaticDataStore staticData, TechDB factionTech)
        {
            Parser = new ParsingProcessor(staticData, factionTech, this);
        }
    }

    public class ComponentDesignAbilityDB
    {
        public string Name;
        public string Description;

        public GuiHint GuiHint;
        public Type DataBlobType;
        //public BaseDataBlob DataBlob;


        public Dictionary<TechSD, double> SelectionDictionary;

        public void SetValueFromTechList(TechSD tech)
        {
            Value = SelectionDictionary[tech];
        }

        public Expression Formula { get; set; }
        public void SetValue()
        {
            object result = Formula.Evaluate();
            if (result is int)
                Value = (double)(int)result;
            else
                Value = (double)result;
        }

        public void SetValueFromInput(double input)
        {
            SetMin();
            SetMax();
            if (input < MinValue)
                Value = MinValue;
            if (input > MaxValue)
                Value = MaxValue;
            else
                Value = input;
            Formula = new Expression(Value.ToString()); //this will stop it re-evaluating to default if/when SetValue() is called.
        }

        public double Value;

        public double MinValue;
        public Expression MinValueFormula { get; set; }
        public void SetMin()
        {
            object result = MinValueFormula.Evaluate();
            if (result is int)
                MinValue = (double)(int)result;
            else
                MinValue = (double)result;


        }
        public double MaxValue;
        public Expression MaxValueFormula { get; set; }
        public void SetMax()
        {
            object result = MaxValueFormula.Evaluate();
            if (result is int)
                MaxValue = (double)(int)result;
            else
                MaxValue = (double)result;
        }

    }

}
