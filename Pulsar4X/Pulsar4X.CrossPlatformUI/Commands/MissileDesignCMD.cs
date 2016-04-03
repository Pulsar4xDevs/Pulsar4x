using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;
using System.ComponentModel;


namespace Pulsar4X.CrossPlatformUI.Commands
{
    
    sealed class MissileDesignCMD : Command
    {
        private readonly GameVM _gameVM;

        public MissileDesignCMD(GameVM gameVM)
        {
            ID = "MissileDesignCMD";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "Missile  Design";
            ToolBarText = "Missile  Design";
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

            MissileDesignVM designVM = new MissileDesignVM(_gameVM);
            Views.MainWindow mw = (Views.MainWindow)Application.Instance.MainForm.Content;
            mw.AddOrSelectTabPanel("Missile Design", new Views.MissileDesignView(designVM));
        }
    }
}
