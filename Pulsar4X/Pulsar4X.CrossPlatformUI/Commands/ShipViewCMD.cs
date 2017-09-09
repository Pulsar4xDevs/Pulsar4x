using Eto.Forms;
using Pulsar4X.ECSLib;
using System;
using System.ComponentModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    sealed class ShipViewCMD : Command
    {
        private readonly GameVM _gameVM;
        public ShipViewCMD(GameVM gameVM)
        {
            ID = "ShipViewCMD";
            //Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ShipView.ico");
            MenuText = "Ship View";
            ToolBarText = "Ship View";
            //            Shortcut = Keys.F3;
            _gameVM = gameVM;
            _gameVM.PropertyChanged += _gameVM_PropertyChanged;
            Enabled = _gameVM.HasGame;
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

            ShipOrderVM orderVM = ShipOrderVM.Create(_gameVM);
            Views.MainWindow mw = (Views.MainWindow)Application.Instance.MainForm.Content;
            mw.AddOrSelectTabPanel("Ship Order View", new Views.ShipView(orderVM));
//            mw.AddOrSelectTabPanel("Ship Orders View", new Views.ShipOrderView(designVM));
        }
    }
}
