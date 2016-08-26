using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;
using System.ComponentModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class LogViewCMD : Command
    {
        private readonly GameVM _gameVM;
        public LogViewCMD(GameVM gameVM)
        {
            ID = "LogViewCMD";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "Event Log View";
            ToolBarText = "Event Log View";
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

            LogViewerVM vm = new LogViewerVM(_gameVM);
            Views.MainWindow mw = (Views.MainWindow)Application.Instance.MainForm.Content;
            Views.LogViewer logPannel = new Views.LogViewer();
            logPannel.DataContext = vm;
            mw.AddOrSelectTabPanel("Event Log", logPannel);

        }
    }
}
