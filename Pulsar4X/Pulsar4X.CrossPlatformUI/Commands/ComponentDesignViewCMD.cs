using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ECSLib;
using System;
using System.ComponentModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    sealed class ComponentDesignViewCMD : Command
    {
        private readonly GameVM _gameVM;
        public ComponentDesignViewCMD(GameVM gameVM)
        {
            ID = "ComponentDesignViewCMD";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "Component Design View";
            ToolBarText = "Component Design View";
            //Shortcut = Keys.F5;
            _gameVM = gameVM;
            Enabled = _gameVM.HasGame;
            _gameVM.PropertyChanged += _gameVM_PropertyChanged;
        }

        private void _gameVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HasGame")
            {
                Enabled = _gameVM.HasGame;
            }
        }

        protected override void OnExecuted(EventArgs e)
        {
            base.OnExecuted(e);

            ComponentDesignVM designVM = ComponentDesignVM.Create(_gameVM);
            Views.MainWindow mw = (Views.MainWindow)Application.Instance.MainForm.Content;
            mw.AddOrSelectTabPanel("Component Design", new Views.ComponentDesignView(designVM));
        }
    }
}

