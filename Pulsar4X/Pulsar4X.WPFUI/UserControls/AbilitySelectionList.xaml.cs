using System.Collections.Generic;
using System.Windows.Controls;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel;

namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for AbilitySelectionList.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class AbilitySelectionList : UserControl
    {
        private List<TechSD> _techList;
        private ComponentAbilityDesignVM _designAbility;
        public event ValueChangedEventHandler ValueChanged; 

        public AbilitySelectionList()
        {
            InitializeComponent();
        }

        public void GuiListSetup(ComponentAbilityDesignVM designAbility)
        {
            _designAbility = designAbility;
            _techList = designAbility.TechList;

            NameLabel.Content = designAbility.Name;
            SelectionComboBox.ItemsSource = _techList;
            SelectionComboBox.DisplayMemberPath = "Name";

            SelectionComboBox.SelectedIndex = 0;
            
        }

        private void SelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ValueChanged != null)
            {
                _designAbility.OnValueChanged(GuiHint.GuiTechSelectionList, SelectionComboBox.SelectedIndex);
                ValueChanged.Invoke(GuiHint.GuiTechSelectionList, SelectionComboBox.SelectedIndex);
            }
        }
    }
}
