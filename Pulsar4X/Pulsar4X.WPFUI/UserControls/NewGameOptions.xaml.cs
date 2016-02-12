using Pulsar4X.ViewModel;
using System.Windows;

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
