using Eto.Drawing;
using Eto.Forms;
using System;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

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

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);
            Application.Instance.MainForm.Close();
        }
    }
}
