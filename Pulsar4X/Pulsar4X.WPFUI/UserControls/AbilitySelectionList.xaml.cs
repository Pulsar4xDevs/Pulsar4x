using System.Windows.Controls;

namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for AbilitySelectionList.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class AbilitySelectionList : UserControl
    {
        public event ValueChangedEventHandler ValueChanged; 

        public AbilitySelectionList()
        {
            InitializeComponent();
        }

        private void SelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ValueChanged != null)
            {
                ValueChanged.Invoke(this, SelectionComboBox.SelectedIndex);
            }
        }
    }
}
