using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;
using System.ComponentModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    sealed class ColonyView : Command
    {
        private readonly GameVM _gameVM;
        public ColonyView(GameVM gameVM)
        {
            ID = "ColonyView";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "Colony View";
            ToolBarText = "Colony View";
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
            Views.MainWindow mw = (Views.MainWindow)Application.Instance.MainForm.Content;
            mw.AddOrSelectTabPanel("Colony View", new Views.ColonyScreenView(_gameVM));
        }
    }
}
