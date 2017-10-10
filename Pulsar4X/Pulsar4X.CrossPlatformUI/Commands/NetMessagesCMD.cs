using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ECSLib;
using System;
using System.ComponentModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class NetMessagesCMD : Command
    {
        private readonly GameVM _gameVM;
        public NetMessagesCMD(GameVM gameVM)
        {
            ID = "NetMessageCMD";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "Net Messages View";
            ToolBarText = "Net Messages View";
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

            var vm = _gameVM;      
            Views.MainWindow mw = (Views.MainWindow)Application.Instance.MainForm.Content;
            Views.NetMessages.NetMessagesView messageView = new Views.NetMessages.NetMessagesView();
            messageView.DataContext = vm;
            mw.AddOrSelectTabPanel("Net Messages", messageView);

        }
    }
}
