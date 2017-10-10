using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ECSLib;
using System;
using System.Threading.Tasks;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class ClientConnectCMD : Command
    {
        private GameVM _gameVM;
        public ClientConnectCMD(GameVM gameVM)
        {
            ID = "Connect";
            //Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.Connect.ico");
            MenuText = "Connect";
            ToolBarText = "Connect";
            //Shortcut = Keys.F5;
            _gameVM = gameVM;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);

            if (_gameVM.HasGame)
            {
                // Check to see if we want to save current game.
                DialogResult result = MessageBox.Show("Would you like to save the current game?", MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes);
                if (result == DialogResult.Yes)
                {
                    var saveGame = new SaveGame(_gameVM);
                    saveGame.Execute();
                }
            }

            //Views.ClientConnection clientConnect = new Views.ClientConnection();
            var vm = new ClientConnectionVM();
            var clientConnect = new Views.ClientConnectionDialog();
            clientConnect.DataContext = vm;
            clientConnect.ShowModal(Application.Instance.MainForm);

        }
    }
}
