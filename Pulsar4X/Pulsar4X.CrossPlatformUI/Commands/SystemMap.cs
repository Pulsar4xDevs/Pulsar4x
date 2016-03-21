using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.CrossPlatformUI.Views;
using Pulsar4X.ViewModel;
using System;
using System.ComponentModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    sealed class SystemMap : Command
    {
        private readonly GameVM _gameVM;

        public SystemMap(GameVM gameVM)
        {
            _gameVM = gameVM;
            ID = "SystemMap";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.SystemMap.ico");
            MenuText = "System Map";
            ToolBarText = "System Map";
            Shortcut = Keys.F3;
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
            MainWindow mw = (MainWindow)Application.Instance.MainForm.Content;
            mw.AddOrSelectTabPanel("System Map",new SystemView(_gameVM.StarSystemViewModel));
        }
    }
}
