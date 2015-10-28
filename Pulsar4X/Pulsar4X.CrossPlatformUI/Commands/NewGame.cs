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
    class NewGame : Command
    {
        private NewGameOptionsVM NewGameModel;
        public NewGame(NewGameOptionsVM model)
        {
            ID = "newgame";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.NewGame.ico");
            MenuText = "New Game";
            ToolBarText = "New Game";
            Shortcut = Keys.F11;
            NewGameModel = model;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);
            var newgame = new Views.NewGame(NewGameModel);
            newgame.ShowModal(Application.Instance.MainForm);
        }
    }
}
