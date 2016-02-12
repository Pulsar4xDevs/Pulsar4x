using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class ColonyView : Command
    {
        private GameVM GameData;
        public ColonyView(GameVM GameVM)
        {
            ID = "ColonyView";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "Colony View";
            ToolBarText = "Colony View";
            //Shortcut = Keys.F5;
            GameData = GameVM;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);
            Application.Instance.MainForm.Content = new Views.ColonyScreenView(GameData);
        }
    }
}
