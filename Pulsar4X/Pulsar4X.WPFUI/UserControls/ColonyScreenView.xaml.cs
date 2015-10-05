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
using Pulsar4X.ViewModels;
using Pulsar4X.WPFUI;


namespace Pulsar4X.ViewModels.UserControls
{
    /// <summary>
    /// Interaction logic for ColonyScreenView.xaml
    /// </summary>
    public partial class ColonyScreenView : ITabControl
    {
        private ColonyScreenVM _colonyScreenVM;

        public ColonyScreenView()
        {
            InitializeComponent();
            Title = "Colony";
            ColonySelection.ItemsSource = App.Current.GameVM.ColonyScreens;
            DataContext = _colonyScreenVM;
        }

        public void SetColony(ColonyScreenVM colonyScreenVM)
        {
            Title = colonyScreenVM.ColonyName;
            _colonyScreenVM = colonyScreenVM;
            
        }

        public string Title { get; set; }

        private void ColonySelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetColony((ColonyScreenVM)ColonySelection.SelectedItem);
        }
    }
}
