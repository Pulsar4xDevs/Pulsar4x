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
            List<AbilityPropertiesData> abilityProperties = new List<AbilityPropertiesData>();
            abilityProperties.Add(new AbilityPropertiesData("Name", componentAbility.Name));
            abilityProperties.Add(new AbilityPropertiesData("Ability", componentAbility.Ability));
            abilityProperties.Add(new AbilityPropertiesData("Ability Amount", componentAbility.AbilityAmount));
            abilityProperties.Add(new AbilityPropertiesData("Description", componentAbility.Description));
            abilityProperties.Add(new AbilityPropertiesData("CrewAmount", componentAbility.CrewAmount));
            abilityProperties.Add(new AbilityPropertiesData("SizeAmount", componentAbility.WeightAmount));
            abilityProperties.Add(new AbilityPropertiesData("Affects Ability", componentAbility.AffectsAbility));
            abilityProperties.Add(new AbilityPropertiesData("Affected Amount", componentAbility.AffectedAmount));
            abilityProperties.Add(new AbilityPropertiesData("Tech Requrements", componentAbility.TechRequiremets));
            listBox_AbilityProperties.DataSource = abilityProperties;
        }

        private class AbilityPropertiesData
        {
            public string Displayname { get; set; }
            public object ValueObject { get; set; }

            internal AbilityPropertiesData(string name, object item)
            {
                Displayname = name;
                ValueObject = item;
            }

            public override string ToString()
            {
                return Displayname;
            }
        }

        private void SetCurrentAbilityProperty(AbilityPropertiesData propety)
        {
            switch (propety.Displayname)
            {
                case "Name":
                case "Description":
                {
                    currentAbilityString((string)propety.ValueObject);
                    break;
                }
                case "Ability":
                case"Affects Ability":
                {
                    currentAbilityAbilityItem((AbilityType)propety.ValueObject);
                    break;
                }
                case "Ability Amount":
                case "CrewAmount":
                case "SizeAmount":
                case "Affected Amount":
                {
                    currentAbilitysListItems((List<float>)propety.ValueObject);
                    break;
                }


            }
        }


        private void currentAbilityString(string stringitem)
        {
            panel_AbiltyProperty.Controls.Clear();
            
            TextBox stringentry = new TextBox();
            panel_AbiltyProperty.Controls.Add(stringentry);
            stringentry.Text = stringitem;
            stringentry.Anchor = (AnchorStyles.Left | AnchorStyles.Right); //wtf is this syntax, I've not seen it before.
        }

        private void currentAbilityAbilityItem(AbilityType abiltyitem)
        {
            panel_AbiltyProperty.Controls.Clear();
            ComboBox abilitysComboBox = new ComboBox();
            BindingList<AbilityType> abilitys = new BindingList<AbilityType>(Enum.GetValues(typeof(AbilityType)).Cast<AbilityType>().ToList());          
            abilitysComboBox.DataSource = abilitys;
            panel_AbiltyProperty.Controls.Add(abilitysComboBox);
            abilitysComboBox.SelectedIndex = abilitys.IndexOf(abiltyitem);
            abilitysComboBox.Anchor = (AnchorStyles.Left | AnchorStyles.Right);
        }

        private void currentAbilitysListItems(List<float> listitem)
        {
            panel_AbiltyProperty.Controls.Clear();
            ListBox abilityListBox = new ListBox();
            BindingList<float> floatitems = new BindingList<float>(listitem);
            abilityListBox.DataSource = floatitems;
            panel_AbiltyProperty.Controls.Add(abilityListBox);

            abilityListBox.Dock = DockStyle.Fill;
            
        }

        private void currentAbilityTechItems(List<Guid> techitems)
        {

        }

        private DataGridViewCell[] dataGridViewCells_FromList(List<float> list)
        {
            List<DataGridViewCell> cellList = new List<DataGridViewCell>();
            foreach (float ability in list)
            {
                DataGridViewCell cell = new DataGridViewTextBoxCell();
                cell.Value = ability.ToString();
                cellList.Add(cell);
            }
            return cellList.ToArray();
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

        private void listBox_AbilityProperties_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetCurrentAbilityProperty((AbilityPropertiesData)listBox_AbilityProperties.SelectedItem);
        }
    }
}
