using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ComponentDesignAbility
    {
        public string Name;
        public string Description;

        public GuiHint GuiHint;
        public Type DataBlobType;
        //public BaseDataBlob DataBlob;
        internal ComponentDesign ParentComponent; 
        public ComponentDesignAbility(ComponentDesign parentComponent)
        {
            ParentComponent = parentComponent;
        }

        public Dictionary<object, ChainedExpression> GuidDictionary;

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

        public double StepValue;
        internal ChainedExpression StepValueFormula { get; set; }
        public void SetStep()
        {
            StepValueFormula.Evaluate();
            StepValue = StepValueFormula.DResult;
        }

        internal object[] DataBlobArgs { get; set; }
    }
}