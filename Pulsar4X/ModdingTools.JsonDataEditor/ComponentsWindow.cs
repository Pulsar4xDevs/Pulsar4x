using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Forms.VisualStyles;
using ModdingTools.JsonDataEditor.UserControls;
using Newtonsoft.Json.Serialization;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor
{
    public partial class ComponentsWindow : UserControl
    {
        private BindingList<DataHolder> _allComponents = new BindingList<DataHolder>();
        private ComponentSD _currentComponent = new ComponentSD();
 
        private BindingList<ComponentAbilityWrapper> _selectedComponentAbilityWrappers = new BindingList<ComponentAbilityWrapper>();
        private int _currentAbilityIndex = 0;
        private ComponentAbilityWrapper CurrentAbility { get { return _selectedComponentAbilityWrappers[_currentAbilityIndex]; } }
        public ComponentsWindow()
        {
            InitializeComponent();
            UpdateComponentslist();

            listBox_allComponents.DataSource = _allComponents;
            listBox_allAbilities.DataSource = Enum.GetValues(typeof(AbilityType)).Cast<AbilityType>();
            listBox_Abilities.DataSource = _selectedComponentAbilityWrappers;
            listBox_Abilities.DisplayMember = "Name";
            
            itemGridUC1.RowChanged += (OnRowChanged);
        }


        private void SetCurrentComponent(ComponentSD componentSD)
        {
            _currentComponent = componentSD;
            
            DataHolder dh;
            if (Data.ComponentData.ContainsKey(_currentComponent.ID))
                dh = Data.ComponentData[_currentComponent.ID];
            else
                dh = new DataHolder(componentSD);

            genericDataUC1.Item = dh;
            genericDataUC1.Description = _currentComponent.Description;
 
            foreach (ComponentAbilitySD abilitySD in _currentComponent.ComponentAbilitySDs)
            {
  
                _selectedComponentAbilityWrappers.Add(new ComponentAbilityWrapper(abilitySD));
            }            
        }


        private void SetCurrentAbility(int index)
        {
            if (index > -1)
            {
                _currentAbilityIndex = index;
                SetupItemGrid(_selectedComponentAbilityWrappers[index].AbilityStaticData());
            }
        }

        /// <summary>
        /// itemgrid stuff. gets cell data and updates the _currentAbility ComponentAbilityWrapper type. 
        /// is invoked when the row is changed in the itemgridUC. 
        /// </summary>
        /// <param name="rowNum"></param>
        /// <param name="e"></param>
        private void OnRowChanged(object rowNum, EventArgs e)
        {
            int row = (int)rowNum;
            ItemGridHeaderCell header = (ItemGridHeaderCell)itemGridUC1.GetCellItem(row,0);
            
            PropertyInfo pinfo = header.RowData; 
            Type propertyType = pinfo.PropertyType; 


            if (typeof(IList).IsAssignableFrom(propertyType))
            {
                //Type listObjectType = itemGridUC1.RowData(row)[0].GetType();
                IList list = (IList)Activator.CreateInstance(propertyType);
                foreach (var item in itemGridUC1.GetRowData(row))
                {
                    list.Add(item);
                }
                pinfo.SetValue(CurrentAbility, list);                
            }
            else 
            { 
                pinfo.SetValue(CurrentAbility, itemGridUC1.GetRowData(row)[0]); //if is not a list,
            }

        }



        /// <summary>
        /// This sets up the itemGrid control by 
        /// Creating the headers
        /// Creating the footers
        /// Selecting the correct cell type
        /// Creating the cells and adding the data
        /// Adding them to the itemGrid control.
        /// </summary>
        /// <param name="abilitySD"></param>
        private void SetupItemGrid(ComponentAbilitySD abilitySD)
        {
            itemGridUC1.Clear();

            Type t = _selectedComponentAbilityWrappers[_currentAbilityIndex].GetType();
            PropertyInfo pinfo = t.GetProperty("Name");
            ItemGridHeaderCell rowHeader = null;
            ItemGridFooterCell rowFooter = null;
            List<ItemGridDataCell> dataCells = new List<ItemGridDataCell>();     

            rowHeader = new ItemGridHeaderCell("Name", pinfo);
            dataCells = new List<ItemGridDataCell>();
            rowFooter = new ItemGridFooterCell(new ItemGridCell_String(null));
            ItemGridCell_String nameCell = new ItemGridCell_String(null);
            if (!String.IsNullOrEmpty(abilitySD.Name))
                dataCells.Add( new ItemGridCell_String(abilitySD.Name));
            itemGridUC1.AddRow(rowHeader, dataCells, rowFooter);


            rowHeader = new ItemGridHeaderCell("Description", t.GetProperty("Description"));
            dataCells = new List<ItemGridDataCell>();
            rowFooter = new ItemGridFooterCell(new ItemGridCell_String(null));
            ItemGridCell_String descCell = new ItemGridCell_String(null);
            if (!String.IsNullOrEmpty(abilitySD.Description))
               dataCells.Add( new ItemGridCell_String(abilitySD.Description));
            itemGridUC1.AddRow(rowHeader, dataCells, rowFooter);


            rowHeader = new ItemGridHeaderCell("Ability", t.GetProperty("Ability"));
            dataCells = new List<ItemGridDataCell>();
            rowFooter = new ItemGridFooterCell(new ItemGridCell_AbilityType(null));            
            if (abilitySD.Ability != null)
                dataCells.Add(new ItemGridCell_AbilityType(abilitySD.Ability));
            itemGridUC1.AddRow(rowHeader, dataCells, rowFooter);


            rowHeader = new ItemGridHeaderCell("AbilityAmount", t.GetProperty("AbilityAmount"));
            dataCells = new List<ItemGridDataCell>();
            rowFooter = new ItemGridFooterCell(new ItemGridCell_FloatType(0));
            if (!abilitySD.AbilityAmount.IsNullOrEmpty())
            {
                foreach (float ammount in abilitySD.AbilityAmount)
                {                 
                    dataCells.Add(new ItemGridCell_FloatType(ammount));
                }
            }
            itemGridUC1.AddRow(rowHeader, dataCells, rowFooter);


            rowHeader = new ItemGridHeaderCell("CrewAmount", t.GetProperty("CrewAmount"));
            dataCells = new List<ItemGridDataCell>();
            rowFooter = new ItemGridFooterCell(new ItemGridCell_FloatType(0));
            if (!abilitySD.CrewAmount.IsNullOrEmpty())
            foreach (float crew in abilitySD.CrewAmount)
            {
              
                dataCells.Add(new ItemGridCell_FloatType(crew));
            }
            itemGridUC1.AddRow(rowHeader, dataCells, rowFooter);


            rowHeader = new ItemGridHeaderCell("WeightAmount", t.GetProperty("WeightAmount"));
            dataCells = new List<ItemGridDataCell>();
            rowFooter = new ItemGridFooterCell(new ItemGridCell_FloatType(0));
            if (!abilitySD.WeightAmount.IsNullOrEmpty())
            {
                foreach (float weight in abilitySD.WeightAmount)
                {
                    dataCells.Add(new ItemGridCell_FloatType(weight));
                }
            }
            itemGridUC1.AddRow(rowHeader, dataCells, rowFooter);


            rowHeader = new ItemGridHeaderCell("AffectsAbility", t.GetProperty("AffectsAbility"));
            dataCells = new List<ItemGridDataCell>();
            rowFooter = new ItemGridFooterCell(new ItemGridCell_AbilityType(null));     
            if (abilitySD.AffectsAbility != null)
                dataCells.Add(new ItemGridCell_AbilityType(abilitySD.AffectsAbility));
            itemGridUC1.AddRow(rowHeader, dataCells, rowFooter);


            rowHeader = new ItemGridHeaderCell("AffectedAmount", t.GetProperty("AffectedAmount"));
            dataCells = new List<ItemGridDataCell>();
            rowFooter = new ItemGridFooterCell(new ItemGridCell_FloatType(0));
            if (!abilitySD.AffectedAmount.IsNullOrEmpty())
            {
                foreach (float affect in abilitySD.AffectedAmount)
                {
                    dataCells.Add(new ItemGridCell_FloatType(affect));
                }
            }
            itemGridUC1.AddRow(rowHeader, dataCells, rowFooter);


            rowHeader = new ItemGridHeaderCell("TechRequirements", t.GetProperty("TechRequirements"));
            dataCells = new List<ItemGridDataCell>();
            rowFooter = new ItemGridFooterCell(new ItemGridCell_TechStaticDataType(null, Data.GetllistoftTechSds()));
            if (!abilitySD.TechRequirements.IsNullOrEmpty())
            {
                foreach (Guid techGuid in abilitySD.TechRequirements)
                {
                    dataCells.Add(new ItemGridCell_TechStaticDataType(techGuid, Data.GetllistoftTechSds()));
                }
            }
            itemGridUC1.AddRow(rowHeader, dataCells, rowFooter);

            //rowHeader = new ItemGridHeaderCell("MineralCosts", t.GetProperty("MineralCosts"));
            //dataCells = new List<ItemGridDataCell>();
            //rowFooter = new ItemGridFooterCell(new ItemGridCell_MineralDictionary(null, Data.GetListofMineralSds()));
            //itemGridUC1.AddRow(rowHeader, dataCells, rowFooter);

            itemGridUC1.HardRedraw();
        }


        private void UpdateComponentslist()
        {
            _allComponents = new BindingList<DataHolder>(Data.ComponentData.Values.ToList());

        }

        /// <summary>
        /// creates new ComponentSD
        /// </summary>
        /// <param name="guid">guid: current or new</param>
        /// <returns></returns>
        private ComponentSD ComponentStaticData(Guid guid)
        {
            List<ComponentAbilitySD> abilityList = new List<ComponentAbilitySD>();
            foreach (var ability in _selectedComponentAbilityWrappers)
            {
                abilityList.Add(ability.AbilityStaticData());
            }
            ComponentSD newSD = new ComponentSD
            {
                ID =guid,
                Name = genericDataUC1.GetName,
                Description = genericDataUC1.Description,
                ComponentAbilitySDs = new List<ComponentAbilitySD>(abilityList)
            };
            return newSD;
        }


        /// <summary>
        /// this is a wrapper class for the ComponentAbilitySD
        /// can be gotten rid of when ComponentAbilitySD is changed from a struct to a class. 
        /// </summary>
        public class ComponentAbilityWrapper
        {
            public string Name { get; set; }
            public string Description { get; set; }

            public AbilityType Ability { get; set; }
            public List<float> AbilityAmount { get; set; }
            public List<float> CrewAmount { get; set; }
            public List<float> WeightAmount { get; set; }
            public AbilityType AffectsAbility { get; set; }
            public List<float> AffectedAmount { get; set; }
            public List<Guid> TechRequirements { get; set; }
            public Dictionary<Guid, float> MineralCosts { get; set; }  

            public ComponentAbilityWrapper(ComponentAbilitySD _abilitySD)
            {
                Name = _abilitySD.Name;
                Description = _abilitySD.Description;
                Ability = _abilitySD.Ability;
                AbilityAmount = _abilitySD.AbilityAmount;
                CrewAmount = _abilitySD.CrewAmount;
                WeightAmount = _abilitySD.WeightAmount;
                AffectsAbility = _abilitySD.AffectsAbility;
                AffectedAmount = _abilitySD.AffectedAmount;
                TechRequirements = _abilitySD.TechRequirements;
            }

            public ComponentAbilitySD AbilityStaticData()
            {
                ComponentAbilitySD newSD = new ComponentAbilitySD
                {
                    Name = Name,
                    Description = Description,

                    Ability = Ability,
                    AbilityAmount = AbilityAmount,
                    CrewAmount = CrewAmount,
                    WeightAmount = WeightAmount,
                    AffectsAbility = AffectsAbility,
                    AffectedAmount = AffectedAmount,
                    TechRequirements = TechRequirements
                };
                return newSD;
            }
        }

        private void listBox_AllComponents_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            _selectedComponentAbilityWrappers.Clear();
            DataHolder selectedItem = (DataHolder)listBox_allComponents.SelectedItem;
            SetCurrentComponent(Data.ComponentData[selectedItem.Guid].StaticData);
        }

        private void button_mainMenu_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.LoadingWindow);
        }

        private void button_clearSelection_Click(object sender, EventArgs e)
        {
            _selectedComponentAbilityWrappers.Clear();
            ComponentAbilitySD newemptyabilitySD = new ComponentAbilitySD
            {
                
            };


            ComponentSD newEmptySD = new ComponentSD
            {
                ID = new Guid(),
                Name = "",
                Description = "",
                ComponentAbilitySDs = new List<ComponentAbilitySD>(),
            };
            SetCurrentComponent(newEmptySD);
        }

        private void button_saveNew_Click(object sender, EventArgs e)
        {
      
            SetCurrentComponent(ComponentStaticData(Guid.NewGuid()));
            Data.SaveToDataStore(_currentComponent);
            UpdateComponentslist();
        }

        private void button_updateExisting_Click(object sender, EventArgs e)
        {
            _currentComponent = ComponentStaticData(_currentComponent.ID);
            Data.SaveToDataStore(_currentComponent);
        }

        private void listBox_Abilities_DoubleClick(object sender, EventArgs e)
        {

            SetCurrentAbility(listBox_Abilities.SelectedIndex);
        }

        private void listBox_allAbilities_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //create a list of AbilityTypes from the abilitys this component has so I can check if it's already in the list or not. 
            List<AbilityType> abilityTypeslistList = new List<AbilityType>();
            foreach (var abilityWrapper in _selectedComponentAbilityWrappers)
            {
                ComponentAbilitySD abilitySD = abilityWrapper.AbilityStaticData();
                abilityTypeslistList.Add(abilitySD.Ability);
            }

            if (!abilityTypeslistList.Contains((AbilityType)listBox_allAbilities.SelectedItem)) //check if it's in the list
            {
                ComponentAbilitySD abilitySD = new ComponentAbilitySD();
                abilitySD.Name = listBox_allAbilities.SelectedItem.ToString();
                abilitySD.Ability = (AbilityType)listBox_allAbilities.SelectedItem;
                abilitySD.AbilityAmount = new List<float>();

                abilitySD.AffectsAbility = AbilityType.Nothing;
                abilitySD.AffectedAmount = new List<float>();
                
                abilitySD.CrewAmount = new List<float>();
                abilitySD.Description = "";
                abilitySD.TechRequirements = new List<Guid>();
                abilitySD.WeightAmount = new List<float>();

                _selectedComponentAbilityWrappers.Add(new ComponentAbilityWrapper(abilitySD));

            }
            //UpdateAbilityAmounts();
        }


        private void listBox_Abilities_MouseClick(object sender, MouseEventArgs e)
        {
            int clickedItemIndex = listBox_Abilities.IndexFromPoint(e.X, e.Y);
            if (e.Button == MouseButtons.Right)
            {
                if (listBox_Abilities.SelectedIndex == clickedItemIndex) //only remove if clicked on the selected item
                    _selectedComponentAbilityWrappers.Remove((ComponentAbilityWrapper)listBox_Abilities.SelectedItem);
            }
            else if (e.Button == MouseButtons.Left)
                SetCurrentAbility(listBox_Abilities.SelectedIndex);

        }
    }

}
