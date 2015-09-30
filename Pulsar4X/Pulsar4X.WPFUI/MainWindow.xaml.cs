using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Pulsar4X.ECSLib;
using Pulsar4X.WPFUI.Properties;
using Pulsar4X.ViewModels;
using Xceed.Wpf.AvalonDock.Layout;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private double _customAdvSimValue = 5;
        delegate void ProgressUpdate(double progress);

        private readonly CancellationToken _pulseCancellationToken;

        [UsedImplicitly]
        public string CustomAdvSimValue
        {
            get
            {
                return _customAdvSimValue.ToString(CultureInfo.CurrentCulture);
            }
            set
            {
                if (!double.TryParse(value, out _customAdvSimValue))
                {
                    throw new ApplicationException("Custom Sim Advancement Value must be a number");
                }
            }
        }    

        public MainWindow()
        {
            InitializeComponent();
            TBTB_AdvSim_Cust.DataContext = this;
            MenuItem_Fullscreen.IsChecked = WindowState == WindowState.Maximized;
            MenuItem_Boarderless.IsChecked = WindowStyle == WindowStyle.None;

            _pulseCancellationToken = new CancellationToken();

            App.Current.PropertyChanged += AppOnPropertyChanged;
            // Get the initial state of the game from the app. (This fires the PropertyChanged event we just hooked into.
            App.Current.Game = App.Current.Game;
        }

        /// <summary>
        /// PropertyChanged handler for the App class.
        /// 
        /// Currently handles changes to the current game.
        /// </summary>
        private void AppOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            switch (propertyChangedEventArgs.PropertyName)
            {
                case "Game":
                    bool isEnabled = App.Current.Game != null;
                    TBT_Toolbar.IsEnabled = isEnabled;
                    MI_SaveGame.IsEnabled = isEnabled;
                    break;
            }
        }

        private async void NewGame_Click(object sender, RoutedEventArgs e)
        {
            // Todo: New Game window to set the game parameters.
            //try
            //{
                Status_TextBlock.Text = "Creating new game...";
                App.Current.Game = await Task.Run(() => Game.NewGame("Test Game", new DateTime(2050, 1, 1), 10, new Progress<double>(OnProgressUpdate)));
                //add sol for shits and giggles and testing
                StarSystemFactory starfac = new StarSystemFactory(App.Current.Game);
                starfac.CreateSol(App.Current.Game);
         
                starfac.CreateEccTest(App.Current.Game);
                starfac.CreateLongitudeTest(App.Current.Game);
                //
                MessageBox.Show(this, "Game Created.", "Result");
                App.Current.GameVM = new GameVM(App.Current.Game);
                Status_TextBlock.Text = "Game Created.";
                Status_ProgressBar.Value = 0;

            //}
            //catch (Exception exception)
            //{
            //    DisplayException("creating a new game", exception);
            //}

            //e.Handled = true;
        }

        private async void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "json Save File|*.json";
            if(fileDialog.ShowDialog() == true)
            {
                string pathToFile = fileDialog.FileName;
                try
                {
                    Status_TextBlock.Text = "Game Loading...";
                    App.Current.Game = await Task.Run(() => SaveGame.Load(pathToFile, new Progress<double>(OnProgressUpdate)));
                    MessageBox.Show(this, "Game Loaded.", "Result");
                    Status_TextBlock.Text = "Game Loaded.";
                    Status_ProgressBar.Value = 0;
                }
                catch (Exception exception)
                {
                    DisplayException("loading the game", exception);
                }
            }
            e.Handled = true;
        }

        private async void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "json Save File|*.json";
            if(fileDialog.ShowDialog() == true)
            {
                string pathToFile = fileDialog.FileName;
                try
                {
                    Status_TextBlock.Text = "Game Saving...";
                    await Task.Run(() => SaveGame.Save(App.Current.Game, pathToFile, new Progress<double>(OnProgressUpdate)));
                    //await Task.Run(() => SaveGame.Save(CurrentGame, pathToFile, true)); // Compressed
                    MessageBox.Show(this, "Game Saved.", "Result");
                    Status_TextBlock.Text = "Game Saved.";
                    Status_ProgressBar.Value = 0;
                }
                catch (Exception exception)
                {
                    DisplayException("saving the game", exception);
                }

            }
            e.Handled = true;
        }

        /// <summary>
        /// OnProgressUpdate eventhandler for the Progress class.
        /// Called from the task thread, this call must be marshalled to the UI thread.
        /// </summary>
        private void OnProgressUpdate(double progress)
        {
            // The Dispatcher contains the UI thread. Make sure we are on the UI thread.
            if (Thread.CurrentThread != Dispatcher.Thread)
            {
                Dispatcher.BeginInvoke(new ProgressUpdate(OnProgressUpdate), progress);
                return;
            }

            Status_ProgressBar.Value = progress * 100;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            e.Handled = true;
        }

        private void Boarderless_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItem_Boarderless.IsChecked)
            {
                // TODO: Stop using Properties.Settings.Default.
                // It places files in %appdata% folder, leaving orphaned files on uninstallation.
                MenuItem_Boarderless.IsChecked = false;
                Settings.Default.WindowStyle = 1;
                WindowStyle = WindowStyle.SingleBorderWindow;
                Settings.Default.Save();
            }
            else
            {
                MenuItem_Boarderless.IsChecked = true;
                Settings.Default.WindowStyle = 0;
                WindowStyle = WindowStyle.None;
                //Minimizing, then maximizing gets the taskbar out of our way.
                WindowState = WindowState.Minimized;
                WindowState = (WindowState)Settings.Default.WindowState;
                Settings.Default.Save();
            }
            e.Handled = true;
        }
        private void Fullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItem_Fullscreen.IsChecked)
            {
                MenuItem_Fullscreen.IsChecked = false;
                Settings.Default.WindowStyle = 0;
                WindowState = WindowState.Normal;
                Settings.Default.Save();
            }
            else
            {
                MenuItem_Fullscreen.IsChecked = true;
                Settings.Default.WindowState = 2;
                WindowState = WindowState.Maximized;
                Settings.Default.Save();
            }
            e.Handled = true;
        }

        private void NewWindow_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = new MainWindow();
            window.Show();
            e.Handled = true;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            About aboutWindow = new About();
            aboutWindow.Show();
            e.Handled = true;
        }

        #region Testing Menu Functions
        private void CurrentTest_Click(object sender, RoutedEventArgs e)
        {
            OrbitProcessorTests test = new OrbitProcessorTests();
            test.Init();
            MessageBox.Show("Ready for snapshot");
            test.OrbitStressTest();
            MessageBox.Show("Test Completed.");
        }
        #endregion

        /// <summary>
        /// Handles all clicks for the TaskBars.
        /// </summary>
        private void TBB_Click(object sender, RoutedEventArgs e)
        {
            UserControl control;
            switch (((Button)e.Source).Name)
            {
                case "TBB_SystemMap":
                    control = new SystemMap();
                    break;
                case "TBB_SystemInfo":
                    control = new SystemInfo();
                    break;
                case "TBB_GalaticMap":
                    control = new GalaticMap();
                    break;
                case "TBB_EventLog":
                    control = new EventLog();
                    break;
                case "TBB_Ships":
                    control = new Ships();
                    break;
                case "TBB_TaskGroups":
                    control = new TaskGroups();
                    break;
                case "TBB_TaskForces":
                    control = new TaskForces();
                    break;
                case "TBB_Combat":
                    control = new Combat();
                    break;
                case "TBB_Populations":
                    control = new Populations();
                    break;
                case "TBB_Leaders":
                    control = new Leaders();
                    break;
                case "TBB_ShipDesign":
                    control = new ShipDesign();
                    break;
                case "TBB_ComponentDesign":
                    control = new ComponentDesign();
                    break;
                case "TBB_Intelligence":
                    control = new Intelligence();
                    break;
                case "TBB_Races":
                    control = new Races();
                    break;
                case "TBB_DebugLog":
                    control = new DebugLog();
                    break;
                default:
                    return;
            }
            LayoutDocument doc = new LayoutDocument();
            string title = ((ITabControl)control).Title;
            doc.Title = title;
            doc.ToolTip = title;
            doc.Content = control;
            LayoutPane.Children.Add(doc);

            e.Handled = true;
        }

        #region AdvSim Menu Functions
        /// <summary>
        /// Handles all button clicks from AdvSim toolbar.
        /// </summary>
        private async void TBB_AdvSim_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan pulseLength = GetPulseLength(e.Source as Button);

            if (pulseLength == TimeSpan.Zero)
            {
                return;
            }

            var pulseProgress = new Progress<double>(UpdatePulseProgress);

            int secondsPulsed;

            //try
            //{
                secondsPulsed = await Task.Run(() => App.Current.Game.AdvanceTime((int)pulseLength.TotalSeconds, _pulseCancellationToken, pulseProgress));
                App.Current.GameVM.Refresh();
            //}
            //catch (Exception exception)
            //{
            //    DisplayException("executing a pulse", exception);
            //}
            //e.Handled = true;
        }

        private void DisplayException(string activity, Exception exception)
        {
            MessageBox.Show("Exception thrown while " + activity + ":\n\n" +
                    exception.GetType() + "\n" +
                    exception.Message +
                    "\n\nThrown in function:\n" +
                    exception.TargetSite +
                    "\n\nStack Trace:\n" +
                    exception.StackTrace,
                    "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private TimeSpan GetPulseLength(Button button)
        {
            if (button == null)
            {
                return GetCustomPulseLength();
            }
            switch (button.Name)
            {
                case "TBB_AdvSim_5Sec":
                    return TimeSpan.FromSeconds(5);
                case "TBB_AdvSim_30Sec":
                    return TimeSpan.FromSeconds(30);
                case "TBB_AdvSim_2Min":
                    return TimeSpan.FromMinutes(2);
                case "TBB_AdvSim_5Min":
                    return TimeSpan.FromMinutes(5);
                case "TBB_AdvSim_1Hour":
                    return TimeSpan.FromHours(1);
                case "TBB_AdvSim_3Hour":
                    return TimeSpan.FromHours(3);
                case "TBB_AdvSim_8Hour":
                    return TimeSpan.FromHours(8);
                case "TBB_AdvSim_1Day":
                    return TimeSpan.FromDays(1);
                case "TBB_AdvSim_5Day":
                    return TimeSpan.FromDays(5);
                case "TBB_AdvSim_30Day":
                    return TimeSpan.FromDays(30);
                case "TBB_AdvSim_Cust":
                    return GetCustomPulseLength();
                default:
                    throw new ArgumentException("Invalid AdvSim Button");
            }
        }

        private void UpdatePulseProgress(double progressPercent)
        {
            // Do some UI stuff with Progress percent
        }

        /// <summary>
        /// Parses the pulse length from the custom pulse length selecter.
        /// </summary>
        /// <returns></returns>
        private TimeSpan GetCustomPulseLength()
        {
            TimeSpan pulseLength = TimeSpan.Zero;
            try
            {
                switch (TBCB_AdvSim_Cust.SelectedIndex)
                {
                    case -1:
                        break;
                    case 0:
                        pulseLength = TimeSpan.FromSeconds(_customAdvSimValue);
                        break;
                    case 1:
                        pulseLength = TimeSpan.FromMinutes(_customAdvSimValue);
                        break;
                    case 2:
                        pulseLength = TimeSpan.FromHours(_customAdvSimValue);
                        break;
                    case 3:
                        pulseLength = TimeSpan.FromDays(_customAdvSimValue);
                        break;
                    case 4:
                        pulseLength = TimeSpan.FromDays(_customAdvSimValue * 30);
                        break;
                    case 5:
                        pulseLength = TimeSpan.FromDays(_customAdvSimValue * 365);
                        break;
                }
            }
            catch (OverflowException)
            {
                pulseLength = TimeSpan.MaxValue;
            }
            return pulseLength;
        }

        /// <summary>
        /// Behavior to make pressing enter or return while focused on the
        /// custom AdvSim input execute the time input.
        /// </summary>
        private void TBTB_AdvSim_Cust_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                TBB_AdvSim_Click(TBTB_AdvSim_Cust, new RoutedEventArgs(Button.ClickEvent, this));
            }
        }

        /// <summary>
        /// Prevents non-numeric input to the AdvSim custom input.
        /// </summary>
        private void TBTB_AdvSim_Cust_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]"); //regex that matches Non-numeric input.
            // Setting handled to true prevents the event from reaching the textbox.
            // This prevents the textbox from updating.
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Highlights the AdvSim custom input on first click.
        /// </summary>
        private void TBTB_AdvSim_Cust_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox tbSender = (TextBox)sender;
            if (!tbSender.IsFocused)
            {
                tbSender.Focus();
                tbSender.SelectAll();
                e.Handled = true;
            }
        }
    #endregion
    }
}
