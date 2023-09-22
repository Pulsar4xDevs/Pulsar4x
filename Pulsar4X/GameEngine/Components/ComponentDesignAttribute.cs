using System;
using System.Collections.Generic;
using System.Globalization;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Components
{
    public class ComponentDesignAttribute
    {
        private readonly CultureInfo _toStringCulture = new CultureInfo("en-GB");

        private ComponentTemplateAttributeBlueprint _templateSD;
        public string Name { get { return _templateSD.Name; } }

        public string Unit { get { return _templateSD.Units; } }
        public GuiHint GuiHint { get { return _templateSD.GuiHint; } }
        public bool IsEnabled {
            get
            {
                if (IsEnabledFormula == null)
                    return true;
                IsEnabledFormula.Evaluate();
                return IsEnabledFormula.BoolResult;
            }
        }

        public Type AttributeType;

        public Type EnumType;
        public int ListSelection;
        //public BaseDataBlob DataBlob;
        internal ComponentDesigner ParentComponent;
        public ComponentDesignAttribute(ComponentDesigner parentComponent, ComponentTemplateAttributeBlueprint templateAtb, FactionTechDB factionTech)
        {
            ParentComponent = parentComponent;
            _templateSD = templateAtb;
            var staticData = StaticRefLib.StaticData;

            if (_templateSD.AttributeFormula != null)
            {
                Formula = new ChainedExpression(_templateSD.AttributeFormula, this, factionTech, staticData);
            }

            if (!string.IsNullOrEmpty(_templateSD.DescriptionFormula ))
            {
                DescriptionFormula = new ChainedExpression(_templateSD.DescriptionFormula, this, factionTech, staticData);
            }

            if (_templateSD.GuidDictionary != null )
            {
                GuidDictionary = new Dictionary<object, ChainedExpression>();
                if (GuiHint == GuiHint.GuiTechSelectionList)
                {
                    foreach (var kvp in _templateSD.GuidDictionary)
                    {
                        if (factionTech.ResearchedTechs.ContainsKey(Guid.Parse(kvp.Key.ToString())))
                        {
                            TechSD techSD = staticData.Techs[Guid.Parse(kvp.Key.ToString())];
                            GuidDictionary.Add(kvp.Key, new ChainedExpression(ResearchProcessor.DataFormula(factionTech, techSD).ToString(), this, factionTech, staticData));
                        }
                    }
                }
                else
                {
                    foreach (var kvp in _templateSD.GuidDictionary)
                    {
                        GuidDictionary.Add(kvp.Key, new ChainedExpression(kvp.Value, this, factionTech, staticData));
                    }
                }
            }
            if (GuiHint == GuiHint.GuiSelectionMaxMin || GuiHint == GuiHint.GuiSelectionMaxMinInt)
            {
                MaxValueFormula = new ChainedExpression(_templateSD.MaxFormula, this, factionTech, staticData);
                MinValueFormula = new ChainedExpression(_templateSD.MinFormula, this, factionTech, staticData);
                StepValueFormula = new ChainedExpression(_templateSD.StepFormula, this, factionTech, staticData);
            }
            if (_templateSD.AttributeType != null)
            {
                AttributeType = Type.GetType(_templateSD.AttributeType);
                if(AttributeType == null)
                    throw new Exception("Attribute Type Error. Attribute type not found: " + _templateSD.AttributeType + ". Try checking the namespace.");
            }

            if (GuiHint == GuiHint.GuiEnumSelectionList)
            {
                MaxValueFormula = new ChainedExpression(_templateSD.MaxFormula, this, factionTech, staticData);
                MinValueFormula = new ChainedExpression(_templateSD.MinFormula, this, factionTech, staticData);
                StepValueFormula = new ChainedExpression(_templateSD.StepFormula, this, factionTech, staticData);
                SetMax();
                SetMin();
                SetStep();
                EnumType = Type.GetType(_templateSD.EnumTypeName);
                if(EnumType == null)
                    throw new Exception("EnumTypeName not found: " + _templateSD.EnumTypeName);
                //don't allow a value less than 0
                if (MinValue < 0)
                    MinValue = 0;
                //Dont set a max value above the max length of the enum list.
                MaxValue = Math.Min(MaxValue , Enum.GetNames(EnumType).Length);

                ListSelection = (int)Value;
                //string[] names = Enum.GetNames(EnumType);
            }

            if (_templateSD.GuiIsEnabledFormula != null)
            {
                IsEnabledFormula = new ChainedExpression(_templateSD.GuiIsEnabledFormula, this, factionTech, staticData);
            }

            if (GuiHint == GuiHint.GuiOrdnanceSelectionList)
            {

            }
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

        internal ChainedExpression IsEnabledFormula { get; set; }
        public void RecalcIsEnabled()
        {
            IsEnabledFormula.Evaluate();
        }

        public Dictionary<object, ChainedExpression> GuidDictionary;

        public void SetValueFromDictionaryExpression(string key)
        {
            //Formula.ReplaceExpression(GuidDictionary[key].Result.ToString());
            Formula = GuidDictionary[key];
            ParentComponent.SetAttributes();
        }

        public void SetValueFromGuidList(Guid techguid)
        {
            Formula.ReplaceExpression("TechData('" + techguid + "')");
            ParentComponent.SetAttributes();
        }

        public void SetValueFromComponentList(Guid componentID)
        {
            Formula.ReplaceExpression("'" + componentID + "'");
            ParentComponent.SetAttributes();
        }

        public void SetValueFromGuid(Guid id)
        {
            Formula.ReplaceExpression("GuidString('" + id.ToString() + "')");
            Formula.Evaluate();
            ParentComponent.SetAttributes();
        }

        internal ChainedExpression Formula { get; set; }
        public void SetValue()
        {
            Formula.Evaluate();
            //ParentComponent.SetAttributes();
        }

        public void SetValueFromInput(double input)
        {
            Debug.Assert(GuiHint != GuiHint.GuiTextDisplay || GuiHint != GuiHint.None, Name + " is not an editable value");
            SetMin();
            SetMax();
            if (input < MinValue)
                input = MinValue;
            else if (input > MaxValue)
                input = MaxValue;
            Formula.ReplaceExpression(input.ToString(_toStringCulture));    //prevents it being reset to the default value on Evaluate;
            Formula.Evaluate();                                             //force dependants to recalc.
            ParentComponent.SetAttributes();
        }

        public double Value { get { return Formula.DResult; } }
        public string ValueString {get { return Formula.StrResult; } }
        public Guid ValueGuid { get { return Formula.GuidResult; } }

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

        internal object[] AtbConstrArgs { get; set; }
    }
}