using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using Microsoft.Win32;
using Pulsar4X.ECSLib;
using Xceed.Wpf.AvalonDock.Layout;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        double _customAdvSimValue = 5;

        // ReSharper disable once MemberCanBePrivate.Global
        // Used by WPF during initialzation, hidden from ReSharper.
        public string CustomAdvSimValue
        {
            // ReSharper disable once UnusedMember.Local
            // Used by WPF during initialzation, hidden from ReSharper.
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

            // Temporary so I can test layout without messing with UI/Engine comms.
            TBT_Toolbar.IsEnabled = true;
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            if(UIComms.Instance.IsEngineFired())
            {
                MessageBoxResult result = MessageBox.Show("Game is already started. Are you sure you want start a new game?", "New Game", MessageBoxButton.YesNo);
                if(result == MessageBoxResult.No)
                    return;
                UIComms.Instance.HaltEngine();
            }
            UIComms.Instance.FireEngine();
            e.Handled = true;
        }

        private void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "json Save File|*.json";
            if(fileDialog.ShowDialog() == true)
            {
                string pathToFile = fileDialog.FileName;
                UIComms.Instance.SendMessage(new Message(MessageType.Load, pathToFile));
            }
            e.Handled = true;
        }

        private void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "json Save File|*.json";
            if(fileDialog.ShowDialog() == true)
            {
                string pathToFile = fileDialog.FileName;
                UIComms.Instance.SendMessage(new Message(MessageType.Save, pathToFile));
            }
            e.Handled = true;
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
                Properties.Settings.Default.WindowStyle = 1;
                WindowStyle = WindowStyle.SingleBorderWindow;
                Properties.Settings.Default.Save();
            }
            else
            {
                MenuItem_Boarderless.IsChecked = true;
                Properties.Settings.Default.WindowStyle = 0;
                WindowStyle = WindowStyle.None;
                //Minimizing, then maximizing gets the taskbar out of our way.
                WindowState = WindowState.Minimized;
                WindowState = (WindowState)Properties.Settings.Default.WindowState;
                Properties.Settings.Default.Save();
            }
            e.Handled = true;
        }
        private void Fullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItem_Fullscreen.IsChecked)
            {
                MenuItem_Fullscreen.IsChecked = false;
                Properties.Settings.Default.WindowStyle = 0;
                WindowState = WindowState.Normal;
                Properties.Settings.Default.Save();
            }
            else
            {
                MenuItem_Fullscreen.IsChecked = true;
                Properties.Settings.Default.WindowState = 2;
                WindowState = WindowState.Maximized;
                Properties.Settings.Default.Save();
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
        private void TBB_AdvSim_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan pulseLength = TimeSpan.Zero;
            Button button = e.Source as Button;
            if (button != null)
            {
                switch (button.Name)
                {
                    case "TBB_AdvSim_5Sec":
                        pulseLength = TimeSpan.FromSeconds(5);
                        break;
                    case "TBB_AdvSim_30Sec":
                        pulseLength = TimeSpan.FromSeconds(30);
                        break;
                    case "TBB_AdvSim_2Min":
                        pulseLength = TimeSpan.FromMinutes(2);
                        break;
                    case "TBB_AdvSim_5Min":
                        pulseLength = TimeSpan.FromMinutes(5);
                        break;
                    case "TBB_AdvSim_1Hour":
                        pulseLength = TimeSpan.FromHours(1);
                        break;
                    case "TBB_AdvSim_3Hour":
                        pulseLength = TimeSpan.FromHours(3);
                        break;
                    case "TBB_AdvSim_8Hour":
                        pulseLength = TimeSpan.FromHours(8);
                        break;
                    case "TBB_AdvSim_1Day":
                        pulseLength = TimeSpan.FromDays(1);
                        break;
                    case "TBB_AdvSim_5Day":
                        pulseLength = TimeSpan.FromDays(5);
                        break;
                    case "TBB_AdvSim_30Day":
                        pulseLength = TimeSpan.FromDays(30);
                        break;
                    case "TBB_AdvSim_Cust":
                        pulseLength = GetCustomPulseLength();
                        break;
                }
            }
            else
            {
                pulseLength = GetCustomPulseLength();
            }
            if (pulseLength != TimeSpan.Zero)
            {
                // Message the lib to pulse.
                e.Handled = true;
            }
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
