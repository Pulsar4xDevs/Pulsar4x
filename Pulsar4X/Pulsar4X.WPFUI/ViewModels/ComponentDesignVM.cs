using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using Pulsar4X.ECSLib;
using Pulsar4X.WPFUI.UserControls;

namespace Pulsar4X.WPFUI.ViewModels
{
    class ComponentDesignVM
    {
        public ComponentDesignDB DesignDB { get; private set; }
        private FactionTechDB _factionTech;
        private StaticDataStore _staticData;

        public Control SizeControl;

        public List<ComponentAbilityDesignVM> AbilityList { get; private set; } 

        public ComponentDesignVM(ComponentDesignDB design, FactionTechDB factionTech, StaticDataStore staticData)
        {
            DesignDB = design;
            _factionTech = factionTech;
            _staticData = staticData;
            AbilityList = new List<ComponentAbilityDesignVM>();
            foreach (var componentAbility in design.ComponentDesignAbilities)
            {
                AbilityList.Add(new ComponentAbilityDesignVM(componentAbility, _factionTech, _staticData));
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
        private FactionTechDB _factionTech;
        private StaticDataStore _staticData;

        public event ValueChangedEventHandler ValueChanged;

        public ComponentAbilityDesignVM(ComponentDesignAbilityDB designAbility, FactionTechDB factionTech, StaticDataStore staticData)
        {
            _designAbility = designAbility;
            _factionTech = factionTech;
            _staticData = staticData;
            switch (designAbility.GuiHint)
            {
                case GuiHint.GuiTechSelectionList:
                    GuiListSetup();
                    break;
                case GuiHint.GuiSelectionMaxMin:
                    GuiSliderSetup();
                    break;
                //case GuiHint.GuiTextDisplay:
                //    GuiTextSetup();
                //    break;
            }
        }

        private void GuiListSetup()
        {
            ComboBox guiComboBox = new ComboBox();
            List<TechSD> techList = new List<TechSD>();
            foreach (var kvp in _designAbility.GuidDictionary)
            {
                techList.Add(_staticData.Techs[kvp.Key]);
            }

            guiComboBox.ItemsSource = techList;
            guiComboBox.DisplayMemberPath = "Name";
            GuiControl = guiComboBox;
        }

        private void GuiSliderSetup()
        {
            MinMaxSlider guiSliderControl = new MinMaxSlider();
            guiSliderControl.Name.Content = _designAbility.Name;
            guiSliderControl.ToolTip = _designAbility.Description;
            _designAbility.SetMax();
            guiSliderControl.Maximum = _designAbility.MaxValue;
            _designAbility.SetMin();
            guiSliderControl.Minimum = _designAbility.MinValue;
            GuiControl = guiSliderControl;
            guiSliderControl.ValueChanged += OnValueChanged;
        }

        private void GuiTextSetup()
        {
            TextBox guiTextBlock = new TextBox();
            guiTextBlock.IsReadOnly = true;
            GuiControl = guiTextBlock;
        }

        private void OnValueChanged(double value)
        {
            _designAbility.SetValueFromInput(value);

            if (ValueChanged != null)
            {
                ValueChanged.Invoke(value);
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
