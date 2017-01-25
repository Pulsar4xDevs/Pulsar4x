using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;
using System.Threading.Tasks;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class NewGame : Command
    {
        private readonly GameVM _gameVM;

        public NewGame(GameVM gameVM)
        {
            ID = "newgame";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.NewGame.ico");
            MenuText = "New Game";
            ToolBarText = "New Game";
            Shortcut = Keys.F11;
            _gameVM = gameVM;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);

            if (_gameVM.HasGame)
            {
                // Check if we want to save the current game.
                DialogResult result = MessageBox.Show("Would you like to save the current game?", "Save game?", MessageBoxButtons.YesNoCancel, MessageBoxType.Question, MessageBoxDefaultButton.Yes);
                if (result == DialogResult.Yes)
                {
                    var saveGame = new SaveGame(_gameVM);
                    saveGame.Execute();
                } else if(result == DialogResult.Cancel)
                {
                    return;
                }
            }

            var newGameDialog = new Views.NewGame(_gameVM);
            newGameDialog.ShowModal(Application.Instance.MainForm);
        }
    }
}
