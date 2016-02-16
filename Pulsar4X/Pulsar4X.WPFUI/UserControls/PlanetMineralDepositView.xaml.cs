using Pulsar4X.ViewModel;
using System.Windows.Controls;

namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for PlanetMineralDepositView.xaml
    /// </summary>
    public partial class PlanetMineralDepositView : UserControl
    {
        private PlanetMineralDepositVM _planetMineralDepositVM;

        public PlanetMineralDepositView()
        {
            InitializeComponent();
        }

        public void Setup(PlanetMineralDepositVM planetMineralDepositVM)
        {
            _planetMineralDepositVM = planetMineralDepositVM;
            DataContext = _planetMineralDepositVM;
        }
    }
}
