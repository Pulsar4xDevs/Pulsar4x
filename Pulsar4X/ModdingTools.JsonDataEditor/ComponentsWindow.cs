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
        private int _currentAbility = 0;

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
            _currentAbility = index;
            SetupItemGrid(_selectedComponentAbilityWrappers[index].AbilityStaticData());
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
            ItemGridCell_HeaderType header = itemGridUC1.GetCellItem(row,0) as ItemGridCell_HeaderType;
            
            //Type dataType = header.RowData.GetType();
            //List<object> datalist = header.RowData;
            
            PropertyInfo pinfo = header.RowData; 
            Type t1 = pinfo.PropertyType; //this does not valadate as a list. 
            dynamic value = pinfo.GetValue(_currentAbility, null);
            Type t3 = value.GetType();

            if (value is IList)
            {
                var listType = t3.GetTypeInfo().GenericTypeArguments[0];
                if (listType == typeof(IConvertible))
                {     
                    pinfo.SetValue(_currentAbility, Extensions.ConvertList(itemGridUC1.RowData(row), value));
                }
                else if (listType == typeof(Guid)) 
                {
                    List<Guid> newlist = new List<Guid>();
                    foreach (Guid item in itemGridUC1.RowData(row))
                    {
                        newlist.Add(item);
                    }
                    pinfo.SetValue(_currentAbility, newlist);
                }
                else
                {
                    throw new Exception("Unknown type that does not implement IConvertable, make boilerplate as per the Guid example");
                }
                
            }
              else 
                pinfo.SetValue(_currentAbility, itemGridUC1.RowData(row)[0]);
            

            //header.RowData = itemGridUC1.RowData(row);
            ComponentAbilitySD abilitySD = _selectedComponentAbilityWrappers[_currentAbility].AbilityStaticData();
        }

        /// <summary>
        /// this sets up the itemGrid controll by selecting the correct cell type
        /// creating the cells
        /// creating the headders
        /// adding them to the itemGrid control.
        /// </summary>
        /// <param name="componentAbilityDH"></param>
        private void SetupItemGrid(ComponentAbilitySD abilitySD)
        {
            itemGridUC1.Clear();
            //ComponentAbilitySD abilitySD = componentAbilityDH.StaticData;

            Type t = _currentAbility.GetType();
            ItemGridCell_HeaderType rowHeader = new ItemGridCell_HeaderType("Name", t.GetProperty("Name"));
            
            ItemGridCell_String nameCell = new ItemGridCell_String(null);
            if (!String.IsNullOrEmpty(abilitySD.Name))
                nameCell = new ItemGridCell_String(abilitySD.Name);
            itemGridUC1.AddRow(rowHeader, new List<ItemGridCell>{nameCell });


            rowHeader = new ItemGridCell_HeaderType("Description", t.GetProperty("Description"));
            ItemGridCell_String descCell = new ItemGridCell_String(null);
            if (!String.IsNullOrEmpty(abilitySD.Description))
                descCell = new ItemGridCell_String(abilitySD.Description);
            itemGridUC1.AddRow(rowHeader, new List<ItemGridCell>{descCell });


            rowHeader = new ItemGridCell_HeaderType("Ability", t.GetProperty("Ability"));
            ItemGridCell_AbilityType abilityCell = new ItemGridCell_AbilityType(null);
            if (abilitySD.Ability != null)
                abilityCell = new ItemGridCell_AbilityType(abilitySD.Ability);
            itemGridUC1.AddRow(rowHeader, new List<ItemGridCell>{abilityCell });

            rowHeader = new ItemGridCell_HeaderType("AbilityAmount", t.GetProperty("AbilityAmount"));
            List<ItemGridCell> abilityAmountCells = new List<ItemGridCell>();
            if (!abilitySD.AbilityAmount.IsNullOrEmpty())
            {
                foreach (float ammount in abilitySD.AbilityAmount)
                {
                    ItemGridCell_FloatType abilityAmountCell = new ItemGridCell_FloatType(ammount);
                    abilityAmountCells.Add(abilityAmountCell);
                }
            }
            else
            {
                abilityAmountCells.Add(new ItemGridCell_EmptyCellType(new ItemGridCell_FloatType(0)));
            }
            itemGridUC1.AddRow(rowHeader, abilityAmountCells);

            rowHeader = new ItemGridCell_HeaderType("CrewAmount", t.GetProperty("CrewAmount"));
            List<ItemGridCell> crewAmountCells = new List<ItemGridCell>();
            if (!abilitySD.CrewAmount.IsNullOrEmpty())
            foreach (float crew in abilitySD.CrewAmount)
            {
                ItemGridCell_FloatType cell = new ItemGridCell_FloatType(crew);
                crewAmountCells.Add(cell);
            }
            else
            {
                crewAmountCells.Add(new ItemGridCell_EmptyCellType(new ItemGridCell_FloatType(0)));
            }
            itemGridUC1.AddRow(rowHeader, crewAmountCells);

            rowHeader = new ItemGridCell_HeaderType("WeightAmount", t.GetProperty("WeightAmount"));
            List<ItemGridCell> weightAmountCells = new List<ItemGridCell>();
            if (!abilitySD.WeightAmount.IsNullOrEmpty())
            {
                foreach (float weight in abilitySD.WeightAmount)
                {
                    ItemGridCell_FloatType cell = new ItemGridCell_FloatType(weight);
                    weightAmountCells.Add(cell);
                }
            }
            else
            {
                weightAmountCells.Add(new ItemGridCell_EmptyCellType(new ItemGridCell_FloatType(0)));
            }
            itemGridUC1.AddRow(rowHeader, weightAmountCells);


            rowHeader = new ItemGridCell_HeaderType("AffectsAbility", t.GetProperty("AffectsAbility"));
            ItemGridCell_AbilityType abilityAffectedCell = new ItemGridCell_AbilityType(null);
            if (abilitySD.AffectsAbility != null)
                abilityAffectedCell = new ItemGridCell_AbilityType(abilitySD.AffectsAbility);
            itemGridUC1.AddRow(rowHeader, new List<ItemGridCell> {abilityAffectedCell });


            rowHeader = new ItemGridCell_HeaderType("AffectedAmount", t.GetProperty("AffectedAmount"));
            List<ItemGridCell> affectedAmountCells = new List<ItemGridCell>{rowHeader};
            if (!abilitySD.AffectedAmount.IsNullOrEmpty())
            {
                foreach (float affect in abilitySD.AffectedAmount)
                {
                    ItemGridCell_FloatType cell = new ItemGridCell_FloatType(affect);
                    affectedAmountCells.Add(cell);
                }
            }
            else
            {
                affectedAmountCells.Add(new ItemGridCell_EmptyCellType(new ItemGridCell_FloatType(0)));
            }
            itemGridUC1.AddRow(rowHeader, affectedAmountCells);


            rowHeader = new ItemGridCell_HeaderType("TechRequirements", t.GetProperty("TechRequirements"));
            List<ItemGridCell> techRequrementCells = new List<ItemGridCell>{rowHeader};
            if (!abilitySD.TechRequirements.IsNullOrEmpty())
            {
                foreach (Guid techGuid in abilitySD.TechRequirements)
                {
                    ItemGridCell_TechStaticDataType cell = new ItemGridCell_TechStaticDataType(techGuid, Data.GetllistoftTechSds());
                    techRequrementCells.Add(cell);
                }
            }
            else
            {
                techRequrementCells.Add(new ItemGridCell_EmptyCellType(new ItemGridCell_TechStaticDataType(null, Data.GetllistoftTechSds())));
            }
            itemGridUC1.AddRow(rowHeader, techRequrementCells);
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
            DataHolder selectedItem = (DataHolder)listBox_allComponents.SelectedItem;
            SetCurrentComponent(Data.ComponentData[selectedItem.Guid].StaticData);
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
            SetCurrentComponent(ComponentStaticData(Guid.NewGuid()));
            Data.SaveToDataStore(_currentComponent);
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
    }

}
