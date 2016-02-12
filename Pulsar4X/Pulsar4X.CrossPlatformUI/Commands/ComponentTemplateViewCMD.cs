using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Commands
{
    class ComponentTemplateViewCMD : Command
        {
            private GameVM GameData;
            public ComponentTemplateViewCMD()
            {
                ID = "ComponentTemplateViewCMD";
                Image = Icon.FromResource("Pulsar4X.CrossPlatformUI.Resources.Icons.ColonyView.ico");
                MenuText = "Component Template Design View";
                ToolBarText = "Component Template Design View";
                //Shortcut = Keys.F5;
                //GameData = GameVM;
            }

            protected override void OnExecuted(EventArgs e)
            {
                base.OnExecuted(e);

                ComponentTemplateVM designVM = new ComponentTemplateVM();//ComponentTemplateVM.Create(GameData);
                Application.Instance.MainForm.Content = new Views.ComponentTemplateDesignerView(designVM);
            }
        }
    }
