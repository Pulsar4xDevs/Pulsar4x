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
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {      
            InitializeComponent();
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            /* Stuff to replace */
            new Game();
            Entity playerFaction = Game.Instance.GlobalManager.GetFirstEntityWithDataBlob<FactionDB>();
            if(playerFaction == null)
                playerFaction = ECSLib.Factories.FactionFactory.CreateFaction(Game.Instance.GlobalManager, "playerFaction");
            Game.Instance.EngineComms.AddFaction(playerFaction);
            /* Stuff to replace */

            UI_Comms.Instance = new UI_Comms(Game.Instance.EngineComms, playerFaction);
            StatusBarText.Text = "Status: Engine fired";
        }

        private void LoadGame_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveGame_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(UI_Comms.MainLoopThread != null)
                UI_Comms.MainLoopThread.Abort();
        }
    }
}
