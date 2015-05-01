using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
        double _customAdvSimValue;
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
        }

        private void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "json Save File|*.json";
            if(fileDialog.ShowDialog() == true)
            {
                string pathToFile = fileDialog.FileName;
                UIComms.Instance.SendMessage(new Message(Message.MessageType.Load, pathToFile));
            }
        }

        private void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "json Save File|*.json";
            if(fileDialog.ShowDialog() == true)
            {
                string pathToFile = fileDialog.FileName;
                UIComms.Instance.SendMessage(new Message(Message.MessageType.Save, pathToFile));
            }
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
            var aboutWindow = new About();
            aboutWindow.Show();
            e.Handled = true;
        }

        #region Testing Menu Functions
        private void CurrentTest_Click(object sender, RoutedEventArgs e)
        {
        }
        #endregion

        private void TBB_Click(object sender, RoutedEventArgs e)
        {
            UserControl control;
            switch (((Button)e.Source).Name)
            {
                case "TBB_SystemMap":
                    control = new SystemMap();
                    break;
                case "TBB_SystemView":
                    control = new SystemView();
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

        #region AdvSim functions
        private void TBB_AdvSim_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan PulseLength = TimeSpan.Zero;
            if (e.Source is Button)
            {
                switch ((e.Source as Button).Name)
                {
                    case "TBB_AdvSim_5Sec":
                        PulseLength = TimeSpan.FromSeconds(5);
                        break;
                    case "TBB_AdvSim_30Sec":
                        PulseLength = TimeSpan.FromSeconds(30);
                        break;
                    case "TBB_AdvSim_2Min":
                        PulseLength = TimeSpan.FromMinutes(2);
                        break;
                    case "TBB_AdvSim_5Min":
                        PulseLength = TimeSpan.FromMinutes(5);
                        break;
                    case "TBB_AdvSim_1Hour":
                        PulseLength = TimeSpan.FromHours(1);
                        break;
                    case "TBB_AdvSim_3Hour":
                        PulseLength = TimeSpan.FromHours(3);
                        break;
                    case "TBB_AdvSim_8Hour":
                        PulseLength = TimeSpan.FromHours(8);
                        break;
                    case "TBB_AdvSim_1Day":
                        PulseLength = TimeSpan.FromDays(1);
                        break;
                    case "TBB_AdvSim_5Day":
                        PulseLength = TimeSpan.FromDays(5);
                        break;
                    case "TBB_AdvSim_30Day":
                        PulseLength = TimeSpan.FromDays(30);
                        break;
                    case "TBB_AdvSim_Cust":
                        PulseLength = GetCustomPulseLength();
                        break;
                }
            }
            else
            {
                PulseLength = GetCustomPulseLength();
            }
            if (PulseLength != TimeSpan.Zero)
            {
                // Message the lib to pulse.
                e.Handled = true;
            }
        }

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

        private void TBTB_AdvSim_Cust_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                TBB_AdvSim_Click(TBTB_AdvSim_Cust, new RoutedEventArgs(Button.ClickEvent, this));
            }
        }
    #endregion
    }
}
