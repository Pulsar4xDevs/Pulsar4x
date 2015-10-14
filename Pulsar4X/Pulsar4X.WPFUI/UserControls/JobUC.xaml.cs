using System.Windows.Controls;
using Pulsar4X.ViewModel;
using Xceed.Wpf.Toolkit;

namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for RefinaryJobUC.xaml
    /// </summary>
    public partial class JobUC : UserControl
    {
        private JobVM _refinaryJobVM;
        public JobUC()
        {
            InitializeComponent();
        }

        public void Initialize(JobVM job)
        {
            _refinaryJobVM = job;
            DataContext = _refinaryJobVM;
        }

        private void ButtonSpinner_Spin(object sender, SpinEventArgs e)
        {
            //if (e.Direction == SpinDirection.Increase)
            //    _refinaryJobVM.IncreasePriority();
            //else if (e.Direction == SpinDirection.Decrease)
            //    _refinaryJobVM.DecresePriorty();
        }
    }
}
