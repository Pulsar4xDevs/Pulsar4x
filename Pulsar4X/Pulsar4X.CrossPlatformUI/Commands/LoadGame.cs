using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class LoadGame : Command
    {
        private GameVM GameData;
        public LoadGame(GameVM GameVM)
        {
            ID = "Loadgame";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.NewGame.ico");
            MenuText = "Load Game";
            ToolBarText = "Load Game";
            Shortcut = Keys.F5;
            GameData = GameVM;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);
        }
    }
}
