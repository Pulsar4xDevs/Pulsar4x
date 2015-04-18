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
using Microsoft.Win32;
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
            if(UI_Comms.MainLoopThread != null && UI_Comms.MainLoopThread.IsAlive)
            {
                MessageBoxResult result = MessageBox.Show("Game is already started. Are you sure you want start a new game?", "New Game", MessageBoxButton.YesNo);
                if(result == MessageBoxResult.No)
                    return;
            }
            /* Stuff to replace */
            new Game();
            Entity playerFaction = Game.Instance.GlobalManager.GetFirstEntityWithDataBlob<FactionDB>();
            if(playerFaction == Entity.GetInvalidEntity())
                playerFaction = ECSLib.Factories.FactionFactory.CreateFaction(Game.Instance.GlobalManager, "playerFaction");
            Game.Instance.EngineComms.AddFaction(playerFaction);
            /* Stuff to replace */

            UI_Comms.Instance = new UI_Comms(Game.Instance.EngineComms, playerFaction);
            StatusBarText.Text = "Status: Engine fired"; //Should changing through Message StatusUpdate
        }

        private void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "json Save File|*.json";
            if(fileDialog.ShowDialog() == true)
            {
                string pathToFile = fileDialog.FileName;
                UI_Comms.Instance.SendMessage(new Message(Message.MessageType.Load, pathToFile));
                StatusBarText.Text = "Status: Loaded from " + pathToFile; //Should changing through Message StatusUpdate
            }
        }

        private void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "json Save File|*.json";
            if(fileDialog.ShowDialog() == true)
            {
                string pathToFile = fileDialog.FileName;
                UI_Comms.Instance.SendMessage(new Message(Message.MessageType.Save, pathToFile));
                StatusBarText.Text = "Status: Saved to " + pathToFile; //Should changing through Message StatusUpdate
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(UI_Comms.MainLoopThread != null)
                UI_Comms.MainLoopThread.Abort();
        }
    }
}
