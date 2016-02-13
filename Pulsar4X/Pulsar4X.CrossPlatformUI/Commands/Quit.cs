using Eto.Drawing;
using Eto.Forms;
using System;
using System.Threading.Tasks;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class Quit : Command
    {
        private readonly GameVM _gameVM;

        public Quit(GameVM gameVM)
        {
            _gameVM = gameVM;
            ID = "quit";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.NewGame.ico");
            MenuText = "&Quit";
            ToolBarText = "Quit";
            ToolTip = "Close the application";
            Shortcut = Keys.Q | Application.Instance.CommonModifier;
        }

        protected override async void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);

            await Exit();
        }

        public async Task Exit()
        {
            if (_gameVM.HasGame)
            {
                // Check to see if we want to save current game.
                DialogResult result = MessageBox.Show("Would you like to save the current game?", MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes);
                if (result == DialogResult.Yes)
                {
                    var saveGame = new SaveGame(_gameVM);
                    await saveGame.Save();
                }
            }
            Application.Instance.Quit();
        }
    }
}
