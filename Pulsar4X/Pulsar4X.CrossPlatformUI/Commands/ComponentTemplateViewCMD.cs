using Eto.Drawing;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class ComponentTemplateViewCMD : Command
        {
            private GameVM GameData;
            public ComponentTemplateViewCMD(GameVM GameVM)
        {
                ID = "ComponentTemplateViewCMD";
                Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
                MenuText = "Component Template Design View";
                ToolBarText = "Component Template Design View";
                //Shortcut = Keys.F5;
                GameData = GameVM;
            }

            protected override void OnExecuted(EventArgs e)
            {
                base.OnExecuted(e);

                ComponentTemplateVM designVM = new ComponentTemplateVM(GameData);//ComponentTemplateVM.Create(GameData);
                Application.Instance.MainForm.Content = new Views.ComponentTemplateDesignerView(designVM);
            }
        }
    }
