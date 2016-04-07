using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class PlayerOptionsCMD : Command
    {
        private readonly GameVM _gameVM;

        public PlayerOptionsCMD(GameVM gameVM)
        {
            ID = "changeFaction";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.NewGame.ico");
            MenuText = "Change Faction";
            ToolBarText = "Change Faction";
            //Shortcut = Keys.F11;
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

            var viewmodel = new PlayerOptionsVM(_gameVM);
            var playerOptions = new Views.PlayerOptionsDialog();
            playerOptions.DataContext = viewmodel;
            playerOptions.ShowModal(Application.Instance.MainForm);
        }
    }
}
