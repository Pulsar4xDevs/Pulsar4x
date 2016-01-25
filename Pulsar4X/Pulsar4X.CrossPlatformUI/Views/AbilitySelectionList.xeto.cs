using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class AbilitySelectionList : Panel
    {
        protected ComboBox AbilitySelection { get; set; }
        protected Label AbilityName { get; set; }

        private ComponentAbilityDesignVM _designAbility;
        public event ValueChangedEventHandler ValueChanged; 

        public AbilitySelectionList()
        {
            XamlReader.Load(this);
        }

        public AbilitySelectionList(ComponentAbilityDesignVM designAbility)
            : this()
        {
            _designAbility = designAbility;
            DataContext = _designAbility;
            AbilitySelection.DataStore = _designAbility.TechList.Cast<object>();
            AbilitySelection.SelectedIndex = 0;
            AbilitySelection.SelectedKeyChanged += SelectionComboBox_SelectionChanged;
        }

        //public void GuiListSetup(ComponentAbilityDesignVM designAbility)
        //{
        //    _designAbility = designAbility;

        //    //AbilitySelection.ItemTextBinding = "Name";

            

        //}

        private void SelectionComboBox_SelectionChanged(object sender, EventArgs e)
        {
            if (ValueChanged != null)
            {
                _designAbility.OnValueChanged(GuiHint.GuiTechSelectionList, AbilitySelection.SelectedIndex);
                ValueChanged.Invoke(GuiHint.GuiTechSelectionList, AbilitySelection.SelectedIndex);
            }
        }
    }
}
