using System.Windows.Controls;
using Pulsar4X.ViewModel;
using Xceed.Wpf.Toolkit;

namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for RefinaryJobUC.xaml
    /// </summary>
    public partial class RefinaryJobUC : UserControl
    {
        private RefinaryJobVM _refinaryJobVM;
        public RefinaryJobUC()
        {
            InitializeComponent();
        }

        public void Initialize(RefinaryJobVM refinaryJob)
        {
            _refinaryJobVM = refinaryJob;
            DataContext = _refinaryJobVM;
        }

        private void ButtonSpinner_Spin(object sender, SpinEventArgs e)
        {
            if (e.Direction == SpinDirection.Increase)
                _refinaryJobVM.IncreasePriority();
            else if (e.Direction == SpinDirection.Decrease)
                _refinaryJobVM.DecresePriorty();
        }
    }
}
