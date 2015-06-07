using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor
{
    public partial class ComponentsWindow : UserControl
    {
        BindingList<DataHolder> AllComponents { get; set; }
        ComponentSD CurrentComponent { get; set; }

        public ComponentsWindow()
        {
            InitializeComponent();
            //UpdateInstallationlist();
            //Data.InstallationData.ListChanged += UpdateInstallationlist;
            listBox_allComponents.DataSource = AllComponents;
            CurrentComponent = new ComponentSD();
        }


        private void SetCurrentComponent(ComponentSD componentSD)
        {
            CurrentComponent = componentSD;
            
            DataHolder dh = Data.InstallationData.GetDataHolder(CurrentComponent.ID, false);
            if (dh == null)
            {
                string file = Data.InstallationData.GetLoadedFiles()[0];
                dh = new DataHolder("", file, new Guid());
            }
            genericDataUC1.Item = dh;
            genericDataUC1.Description = CurrentComponent.Description;
   
        }




        /// <summary>
        /// creates newSD
        /// </summary>
        /// <param name="guid">guid: current or new</param>
        /// <returns></returns>
        private ComponentSD staticData(Guid guid)
        {
            ComponentSD newSD = new ComponentSD
            {
                ID = Guid.NewGuid(),
                Name = genericDataUC1.GetName,
                Description = genericDataUC1.Description,
                //ComponentAbilitySDs = new List<ComponentAbilitySD>( listBox_Abilities.Items);
            };
            return newSD;
        }


        private void button_mainMenu_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.LoadingWindow);
        }

        private void button_clearSelection_Click(object sender, EventArgs e)
        {
            ComponentSD newEmptySD = new ComponentSD
            {
                ID = new Guid(),
                Name = "",
                Description = "",

            };
            SetCurrentComponent(newEmptySD);
        }

        private void button_saveNew_Click(object sender, EventArgs e)
        {
            SetCurrentComponent(staticData(Guid.NewGuid()));
            Data.InstallationData.Update(CurrentComponent);

        }

        private void button_updateExsisting_Click(object sender, EventArgs e)
        {
            CurrentComponent = staticData(CurrentComponent.ID);
            Data.InstallationData.Update(CurrentComponent);
        }
    }
}
