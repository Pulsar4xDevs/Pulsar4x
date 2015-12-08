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
    class SystemMap : Command
    {
        private GameVM GameData;
        public SystemMap(GameVM GameVM)
        {
            ID = "SystemMap";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.SystemMap.ico");
            MenuText = "System Map";
            ToolBarText = "System Map";
            //Shortcut = Keys.F5;
            GameData = GameVM;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);
            Application.Instance.MainForm.Content = new Views.SystemView(GameData);
        }
    }
}
