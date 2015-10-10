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
using Pulsar4X.ViewModel;
using Pulsar4X.WPFUI;

namespace Pulsar4X.WPFUI.UserControls
{
    /// <summary>
    /// Interaction logic for NewGameOptions.xaml
    /// </summary>
    public partial class NewGameOptions : ITabControl
    {
        public string Title { get; set; }
        private NewGameOptionsVM _newGameOptions;

        public NewGameOptions()
        {
            Title = "New Game";
            InitializeComponent();
        }

        public NewGameOptions(NewGameOptionsVM vm)
        {
            Title = "New Game";
            InitializeComponent();
            _newGameOptions = vm;
            DataContext = _newGameOptions;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _newGameOptions.CreateGame();
            
        }
    }
}
