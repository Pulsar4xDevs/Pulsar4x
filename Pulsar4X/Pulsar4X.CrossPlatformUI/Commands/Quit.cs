using Eto.Drawing;
using Eto.Forms;
using System;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class Quit : Command
    {
        public Quit()
        {
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
            Application.Instance.Quit();
        }
    }
}
