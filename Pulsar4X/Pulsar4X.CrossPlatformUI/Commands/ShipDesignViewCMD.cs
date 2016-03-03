using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;
using System.ComponentModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    sealed class ShipDesignViewCMD : Command
    {
        private readonly GameVM _gameVM;
        public ShipDesignViewCMD(GameVM gameVM)
        {
            ID = "ShipDesignViewCMD";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "Ship Design View";
            ToolBarText = "Ship Design View";
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

            ShipDesignVM designVM = ShipDesignVM.Create(_gameVM);
            Views.MainWindow mw = (Views.MainWindow)Application.Instance.MainForm.Content;
            mw.AddTabPanel("Ship Design", new Views.ShipDesignView(designVM));
        }
    }
}
