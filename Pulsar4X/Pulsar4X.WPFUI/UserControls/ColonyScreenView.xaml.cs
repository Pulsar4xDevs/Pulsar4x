using Pulsar4X.ViewModel;
using System.Windows.Controls;



namespace Pulsar4X.WPFUI.UserControls
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
            DataContext = _colonyScreenVM;
            PlanetMineralDeposit.Setup(_colonyScreenVM.PlanetMineralDepositVM);
            RawMinStockpile.Setup(_colonyScreenVM.RawMineralStockpileVM);
            RefinedMatsStockpile.Setup(_colonyScreenVM.RefinedMatsStockpileVM);
            FacDataGrid.ItemsSource = _colonyScreenVM.Facilities;
            PopDataGrid.ItemsSource = _colonyScreenVM.Species;
            RefinaryAbility.DataContext = _colonyScreenVM.RefinaryAbilityVM;
            ConstructionAbility.DataContext = _colonyScreenVM.ConstructionAbilityVM;
            ColonyResearchView1.DataContext = _colonyScreenVM.ColonyResearchVM;

        }

        public string Title { get; set; }

        private void ColonySelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetColony((ColonyScreenVM)ColonySelection.SelectedItem);
        }
    }
}
