using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class ShipDesignViewCMD : Command
    {
        private GameVM GameData;
        public ShipDesignViewCMD(GameVM GameVM)
        {
            ID = "ShipDesignViewCMD";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "Ship Design View";
            ToolBarText = "Ship Design View";
            //Shortcut = Keys.F5;
            GameData = GameVM;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);

            ShipDesignVM designVM = ShipDesignVM.Create(GameData);
            Application.Instance.MainForm.Content = new Views.ShipDesignView(designVM);
        }
    }
}
