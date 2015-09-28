using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Pulsar4X.ECSLib;
using Pulsar4X.WPFUI.UserControls;

namespace Pulsar4X.WPFUI.ViewModels
{
    class ComponentDesignVM
    {
        public ComponentDesignDB DesignDB { get; private set; }
        private readonly StaticDataStore _staticData;

        public Control SizeControl;

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
        public Control GuiControl;
        private ComponentDesignAbilityDB _designAbility;
        private StaticDataStore _staticData;
        private List<TechSD> _techList;
        public event ValueChangedEventHandler ValueChanged;

        public ComponentAbilityDesignVM(ComponentDesignAbilityDB designAbility, StaticDataStore staticData)
        {
            _designAbility = designAbility;
            _staticData = staticData;
            switch (designAbility.GuiHint)
            {
                case GuiHint.GuiTechSelectionList:
                    GuiListSetup();
                    break;
                case GuiHint.GuiSelectionMaxMin:
                    GuiSliderSetup();
                    break;
            }
        }

        private void GuiListSetup()
        {
            AbilitySelectionList abilitySelection = new AbilitySelectionList();
            _techList =  new List<TechSD>();
            foreach (var kvp in _designAbility.GuidDictionary)
            {
                _techList.Add(_staticData.Techs[kvp.Key]);
            }
            abilitySelection.NameLabel.Content = _designAbility.Name;
            abilitySelection.SelectionComboBox.ItemsSource = _techList;
            abilitySelection.SelectionComboBox.DisplayMemberPath = "Name";
            abilitySelection.ValueChanged += OnValueChanged;
            abilitySelection.SelectionComboBox.SelectedIndex = 0;
            GuiControl = abilitySelection;
        }

        private void GuiSliderSetup()
        {
            MinMaxSlider guiSliderControl = new MinMaxSlider
            {
                NameLabel = {Content = _designAbility.Name}, ToolTip = _designAbility.Description
            };
            _designAbility.SetMax();
            guiSliderControl.Maximum = _designAbility.MaxValue;
            _designAbility.SetMin();
            guiSliderControl.Minimum = _designAbility.MinValue;
            guiSliderControl.ValueChanged += OnValueChanged;
            guiSliderControl.Value = _designAbility.Value;
            GuiControl = guiSliderControl;
            
        }

        private void OnValueChanged(object sender, double value)
        {
            if(sender is MinMaxSlider)
                _designAbility.SetValueFromInput(value);
            else if (sender is AbilitySelectionList)
                _designAbility.SetValueFromGuidList(_techList[(int)value].ID);

            if (ValueChanged != null)
            {
                ValueChanged.Invoke(sender, value);
            }
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
}
