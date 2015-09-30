using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModels
{


    public class ComponentDesignVM
    {
        public ComponentDesignDB DesignDB { get; private set; }
        private readonly StaticDataStore _staticData;

        //public event ValueChangedEventHandler ValueChanged;

        public List<ComponentAbilityDesignVM> AbilityList { get; private set; } 

        public ComponentDesignVM(ComponentDesignDB design, StaticDataStore staticData)
        {
            DesignDB = design;
            _staticData = staticData;
            AbilityList = new List<ComponentAbilityDesignVM>();
            foreach (var componentAbility in design.ComponentDesignAbilities)
            {
                AbilityList.Add(new ComponentAbilityDesignVM(componentAbility, _staticData));
            }
            
         }

        public string StatsText
        {
            get
            {
                string text = DesignDB.Name + Environment.NewLine;
                text += "Size: " + DesignDB.SizeValue + Environment.NewLine;
                text += "HTK: " + DesignDB.HTKValue + Environment.NewLine;
                text += "Crew: " + DesignDB.CrewReqValue + Environment.NewLine;
                text += "ResearchCost: " + DesignDB.ResearchCostValue + Environment.NewLine;
                foreach (var kvp in DesignDB.MineralCostValues)
                {
                    string mineralName = _staticData.Minerals.Find(item => item.ID == kvp.Key).Name;
                    text += mineralName + ": " + kvp.Value + Environment.NewLine;
                }
                text += "Credit Cost: " + DesignDB.CreditCostValue + Environment.NewLine;
                return text;
            }
        }

        public string AbilityStatsText
        {
            get
            {
                string text = "Ability Stats:" + Environment.NewLine;

                foreach (var abilty in AbilityList)
                {
                    text += abilty.AbilityStat;
                }
                return text;
            }
        }
    }


    public class ComponentAbilityDesignVM
    {
        private ComponentDesignAbilityDB _designAbility;
        private StaticDataStore _staticData;
        
        
        public event ValueChangedEventHandler ValueChanged;

        public List<TechSD> TechList { get; private set; }
        public string Name { get { return _designAbility.Name; } }
        public string Description { get { return _designAbility.Description; } }

        public double MaxValue { get { return _designAbility.MaxValue; } }
        public double MinValue { get { return _designAbility.MinValue; } }
        public double Value { get { return _designAbility.Value; } }

        public GuiHint GuiHint { get { return _designAbility.GuiHint; }}
        

        public ComponentAbilityDesignVM(ComponentDesignAbilityDB designAbility, StaticDataStore staticData)
        {
            _designAbility = designAbility;
            _staticData = staticData;

            switch (designAbility.GuiHint)
            {
                case GuiHint.GuiTechSelectionList:
                    TechList = new List<TechSD>();
                    foreach (var kvp in designAbility.GuidDictionary)
                    {
                        TechList.Add(_staticData.Techs[kvp.Key]);
                    }
                    break;
                case GuiHint.GuiSelectionMaxMin:
                    {
                        designAbility.SetMax();
                        designAbility.SetMin();
                        designAbility.SetValue();
                    }
                    break;
            }

        }

        public void OnValueChanged(GuiHint controlType, double value)
        {
            if(controlType == GuiHint.GuiSelectionMaxMin)
                _designAbility.SetValueFromInput(value);
            else if (controlType == GuiHint.GuiTechSelectionList)
                _designAbility.SetValueFromGuidList(TechList[(int)value].ID);

            //if (ValueChanged != null) //bubble it up to ComponentDesignVM?
            //{
            //    ValueChanged.Invoke(controlType, value);
            //}
        }

        public string AbilityStat {
            get
            {
                string text = null;
                if (_designAbility.GuiHint == GuiHint.GuiTextDisplay)
                {
                    text += _designAbility.Name + ": ";
                    text += _designAbility.Value + Environment.NewLine;
                }
                return text;
            }
        }
    }

    public delegate void ValueChangedEventHandler(GuiHint controlType, double value);
}
