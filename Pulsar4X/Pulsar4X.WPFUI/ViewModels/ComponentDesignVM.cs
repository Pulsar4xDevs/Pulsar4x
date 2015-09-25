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
        private ComponentDesignDB _design;
        private TechDB _factionTech;
        private StaticDataStore _staticData;

        public Control SizeControl;

        public ComponentDesignVM(ComponentDesignDB design, TechDB factionTech, StaticDataStore staticData)
        {
            _design = design;
            _factionTech = factionTech;
            _staticData = staticData;

         }

    }



    public class ComponentAbilityDesignVM
    {
        public Control GuiControl;
        private ComponentDesignAbilityDB _designAbility;
        private TechDB _factionTech;
        private StaticDataStore _staticData;
        public ComponentAbilityDesignVM(ComponentDesignAbilityDB designAbility, TechDB factionTech, StaticDataStore staticData)
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
                case GuiHint.GuiTextDisplay:
                    GuiTextSetup();
                    break;
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
        }

        private void GuiTextSetup()
        {
            TextBox guiTextBlock = new TextBox();
            guiTextBlock.IsReadOnly = true;
            GuiControl = guiTextBlock;
        }

    }
}
