using System;
using System.Collections.Generic;
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
            Program.PulsarMain(); // todo: replace
            UIComms.Instance.OnStatusUpdate += (status) => { StatusBarText.Text = status; };
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UIComms.Instance.HaltEngine();
        }
    }
}
