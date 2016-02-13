using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class NewGame : Command
    {
        private Views.NewGame NewGameDialog;
        private GameVM _gameVM;

        public NewGame(GameVM Game)
        {
            ID = "newgame";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.NewGame.ico");
            MenuText = "New Game";
            ToolBarText = "New Game";
            Shortcut = Keys.F11;
            _gameVM = Game;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);
            NewGameDialog = new Views.NewGame(_gameVM);
            NewGameDialog.ShowModal(Application.Instance.MainForm);
        }
    }
}
