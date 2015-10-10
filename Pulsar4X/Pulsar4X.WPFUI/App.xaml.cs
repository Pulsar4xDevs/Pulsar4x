using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Pulsar4X.ViewModel;
//using Pulsar4X.ECSLib;
using Pulsar4X.WPFUI;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : INotifyPropertyChanged
    {
        public new static App Current { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Register an event for all TextBoxes.
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));
            base.OnStartup(e);
        }

        /// <summary>
        /// Event handler for TextBoxes getting focus from the keyboard tab.
        /// Causes the textbox to highlight its text when tabbed into.
        /// </summary>
        private static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }


        internal GameVM GameVM { get; set; }

        internal FactionVM Faction
        {
            get { return _faction; }
            set {
                _faction = value; 
                OnPropertyChanged(); }
        }
        private FactionVM _faction;

        App()
        {
            Current = this;
            GameVM = new GameVM();
            if (string.IsNullOrEmpty(WPFUI.Properties.Settings.Default.ClientGuid))
            {
                WPFUI.Properties.Settings.Default.ClientGuid = Guid.NewGuid().ToString();
            }
            Guid clientGuid;
            if (!Guid.TryParse(WPFUI.Properties.Settings.Default.ClientGuid, out clientGuid))
            {
                clientGuid = Guid.NewGuid();
                WPFUI.Properties.Settings.Default.ClientGuid = clientGuid.ToString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
