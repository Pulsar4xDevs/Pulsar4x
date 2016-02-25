using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;
using System.ComponentModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    sealed class ComponentTemplateViewCMD : Command
    {
        private readonly GameVM _gameVM;

        public ComponentTemplateViewCMD(GameVM gameVM)
        {
            ID = "ComponentTemplateViewCMD";
            Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
            MenuText = "Component Template Design View";
            ToolBarText = "Component Template Design View";
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

            ComponentTemplateVM designVM = new ComponentTemplateVM(_gameVM); //ComponentTemplateVM.Create(GameData);
            Application.Instance.MainForm.Content = new Views.ComponentTemplateDesigner.ComponentTemplateDesignerView(designVM);
        }
    }
}
