using System.Windows.Controls;
using Pulsar4X.ViewModel;
namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for RawMineralStockpile.xaml
    /// </summary>
    public partial class RawMineralStockpile : UserControl
    {
        private RawMineralStockpileVM _rawMineralStockpileVM;

        public RawMineralStockpile()
        {
            InitializeComponent();
        }

        public void Setup(RawMineralStockpileVM rawMineralStockpileVM)
        {
            _rawMineralStockpileVM = rawMineralStockpileVM;
            DataContext = _rawMineralStockpileVM;
        }
    }
}
