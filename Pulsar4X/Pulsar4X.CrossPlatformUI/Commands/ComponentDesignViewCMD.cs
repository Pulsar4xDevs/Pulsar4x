using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class ComponentDesignViewCMD : Command
    {
        private GameVM GameData;
        public ComponentDesignViewCMD(GameVM GameVM)
        {
            ID = "ComponentDesignViewCMD";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "Component Design View";
            ToolBarText = "Component Design View";
            //Shortcut = Keys.F5;
            GameData = GameVM;
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);

            ComponentDesignVM designVM = ComponentDesignVM.Create(GameData);
            Application.Instance.MainForm.Content = new Views.ComponentDesignView(designVM);
        }
    }
}

