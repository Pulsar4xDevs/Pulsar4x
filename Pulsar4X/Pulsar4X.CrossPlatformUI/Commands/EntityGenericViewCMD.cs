using Eto.Forms;
using Pulsar4X.ECSLib;
using System;
using System.ComponentModel;

namespace Pulsar4X.CrossPlatformUI.Commands
    {
        sealed class EntityGenericViewCMD:Command
        {
            private readonly GameVM _gameVM;
            public EntityGenericViewCMD(GameVM gameVM)
            {
                ID = "EntityViewCMD";
                //Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ShipView.ico");
                MenuText = "Entity View";
                ToolBarText = "Entity View";
                //            Shortcut = Keys.F3;
                _gameVM = gameVM;
                _gameVM.PropertyChanged += _gameVM_PropertyChanged;
                Enabled = _gameVM.HasGame;
            }

            private void _gameVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if(e.PropertyName == "HasGame")
                {
                    Enabled = _gameVM.HasGame;
                }
            }

            protected override void OnExecuted(EventArgs e)
            {
                base.OnExecuted(e);

                EntityVM entityVM = new EntityVM(_gameVM);
                Views.MainWindow mw = (Views.MainWindow)Application.Instance.MainForm.Content;
                mw.AddOrSelectTabPanel("Entity View", new Views.EntityGenericView());                
            }
        }
    }