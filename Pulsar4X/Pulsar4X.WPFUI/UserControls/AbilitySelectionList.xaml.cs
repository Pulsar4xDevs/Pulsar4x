using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for AbilitySelectionList.xaml
    /// </summary>
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
