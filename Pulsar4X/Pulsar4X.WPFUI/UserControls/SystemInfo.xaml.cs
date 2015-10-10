using System.Windows.Controls;
using Pulsar4X.ViewModel;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for SystemView.xaml
    /// </summary>
    public partial class SystemInfo : ITabControl
    {
        public string Title { get; set; }
        public SystemInfo()
        {
            InitializeComponent();
            Title = "System Information";
            SystemSelection.ItemsSource = App.Current.GameVM.StarSystems;
        }

        private void SystemSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SystemVM systemVM = (SystemVM)SystemSelection.SelectedItem;
            SystemView_DataGrid.ItemsSource = App.Current.GameVM.GetSystem(systemVM.ID).PlanetList;
        }
    }
}
