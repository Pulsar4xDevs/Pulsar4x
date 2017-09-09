using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ECSLib;
using System;
using System.ComponentModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    sealed class SystemInfoCMD : Command
    {
        private readonly GameVM _gameVM;
        public SystemInfoCMD(GameVM gameVM)
        {
            ID = "SysInfoCMD";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "SystemInfo";
            ToolBarText = "SystemInfo";
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

            SystemInfoVM sysinfoVM = new SystemInfoVM(_gameVM);
            Views.MainWindow mw = (Views.MainWindow)Application.Instance.MainForm.Content;
            var view = new Views.SystemInfoView();
            view.DataContext = sysinfoVM;
            mw.AddOrSelectTabPanel("System Info", view);
        }
    }
}
