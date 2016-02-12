using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class SaveGame : Command
    {
        private GameVM GameData;
        public SaveGame(GameVM GameVM)
        {
            ID = "savegame";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.NewGame.ico");
            MenuText = "Save Game";
            ToolBarText = "Save Game";
            Shortcut = Keys.F6;
            GameData = GameVM;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);
        }
    }
}
