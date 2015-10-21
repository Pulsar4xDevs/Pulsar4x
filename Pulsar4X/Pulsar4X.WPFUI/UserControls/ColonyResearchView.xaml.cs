using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Pulsar4X.ViewModel;

namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for ColonyResearchView.xaml
    /// </summary>
    public partial class ColonyResearchView : UserControl
    {
        private ColonyResearchVM _colonyResearchVM;
        public ObservableCollection<ScientistUC> ScientistControls { get; set; }
        public ColonyResearchView()
        {
            ScientistControls = new ObservableCollection<ScientistUC>();
            InitializeComponent();
        }

        public ColonyResearchView(ColonyResearchVM colonyResearchVM)
        {
            _colonyResearchVM = colonyResearchVM;
            DataContext = _colonyResearchVM;
            ScientistControls = new ObservableCollection<ScientistUC>();
            InitializeComponent();
            ScientistListBox.ItemsSource = ScientistControls;

            foreach (var sci in _colonyResearchVM.Scientists)
            {
                ScientistControls.Add(new ScientistUC(sci));
            }
            

            

        }
    }
}
