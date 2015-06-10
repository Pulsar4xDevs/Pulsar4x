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
 
        private BindingList<DataHolder> _selectedComponentAbilites = new BindingList<DataHolder>();

        public ComponentsWindow()
        {
            InitializeComponent();
            UpdateComponentslist();
            Data.InstallationData.ListChanged += UpdateComponentslist;
            listBox_allComponents.DataSource = AllComponents;
            CurrentComponent = new ComponentSD();

            listBox_Abilities.DataSource = _selectedComponentAbilites;
        }


        private void SetCurrentComponent(ComponentSD componentSD)
        {
            CurrentComponent = componentSD;
            
            DataHolder dh = Data.ComponentData.GetDataHolder(CurrentComponent.ID, false);
            if (dh == null)
            {
                string file = Data.ComponentData.GetLoadedFiles()[0];
                dh = new DataHolder(componentSD, file);
            }
            genericDataUC1.Item = dh;
            genericDataUC1.Description = CurrentComponent.Description;
            _selectedComponentAbilites.Clear();
            foreach (var abilitySD in CurrentComponent.ComponentAbilitySDs)
            {
                _selectedComponentAbilites.Add(new DataHolder(abilitySD));
            }
            
        }

        private void SetCurrentAbility(ComponentAbilitySD componentAbility)
        {
            //dataGridView_Abilitys.DataSource = componentAbility;
            dataGridView_Abilitys.Columns.Add("","");
            dataGridView_Abilitys.Rows.Add(componentAbility.Name);
            dataGridView_Abilitys.Rows.Add(componentAbility.Description);
            dataGridView_Abilitys.Rows.Add(componentAbility.Ability);
            dataGridView_Abilitys.Rows.Add(componentAbility.AbilityAmount);
        }

        private void UpdateComponentslist()
        {
            AllComponents = new BindingList<DataHolder>(Data.ComponentData.GetDataHolders().ToList());
        }



        /// <summary>
        /// creates newSD
        /// </summary>
        /// <param name="guid">guid: current or new</param>
        /// <returns></returns>
        private ComponentSD StaticData(Guid guid)
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

        private ComponentAbilitySD AbilityStaticData()
        {
            return new ComponentAbilitySD();
        }

        private void listBox_AllComponents_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DataHolder selectedItem = (DataHolder)listBox_allComponents.SelectedItem;
            SetCurrentComponent(Data.ComponentData.Get(selectedItem.Guid));
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
            SetCurrentComponent(StaticData(Guid.NewGuid()));
            Data.InstallationData.Update(CurrentComponent);

        }

        private void button_updateExsisting_Click(object sender, EventArgs e)
        {
            CurrentComponent = StaticData(CurrentComponent.ID);
            Data.InstallationData.Update(CurrentComponent);
        }

        private void listBox_Abilities_DoubleClick(object sender, EventArgs e)
        {
            DataHolder selectedItem = (DataHolder)listBox_Abilities.SelectedItem;
            ComponentAbilitySD selectedSD = selectedItem.StaticData;
            SetCurrentAbility(selectedSD);
        }
    }
}
