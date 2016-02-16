using Pulsar4X.ViewModel;
using System.Windows.Controls;

namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for RefinedMatsStockpileView.xaml
    /// </summary>
    public partial class RefinedMatsStockpileView : UserControl
    {
        private RefinedMatsStockpileVM _refinedMatsStockpileVM;

        public RefinedMatsStockpileView()
        {
            InitializeComponent();
        }

        public void Setup(RefinedMatsStockpileVM refinedMatsStockpileVM)
        {
            _refinedMatsStockpileVM = refinedMatsStockpileVM;
            DataContext = _refinedMatsStockpileVM;
        }
    }
}
