using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using ModdingTools.JsonDataEditor.UserControls;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor
{
    public partial class ComponentsWindow : UserControl
    {
        BindingList<DataHolder> _allComponents = new BindingList<DataHolder>();
        private ComponentSD _currentComponent = new ComponentSD();
 
        private BindingList<DataHolder> _selectedComponentAbilities = new BindingList<DataHolder>();
       

        public ComponentsWindow()
        {
            InitializeComponent();
            UpdateComponentslist();
            //Data.InstallationData.ListChanged += UpdateComponentslist;
            listBox_allComponents.DataSource = _allComponents;
            listBox_allAbilities.DataSource = Enum.GetValues(typeof(AbilityType)).Cast<AbilityType>();
            listBox_Abilities.DataSource = _selectedComponentAbilities;
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
            _selectedComponentAbilities.Clear();
            foreach (ComponentAbilitySD abilitySD in _currentComponent.ComponentAbilitySDs)
            {
                _selectedComponentAbilities.Add(new DataHolder(abilitySD));
            }
            
        }

        private void SetCurrentAbility(DataHolder componentAbilityDH)
        {            
            propertyGrid_PropertyEditor.SelectedObject = new AbilitiesDisplayer(componentAbilityDH);
            SetupItemGrid(componentAbilityDH);
        }

        private void SetupItemGrid(DataHolder componentAbilityDH)
        {
            ComponentAbilitySD abilitySD = componentAbilityDH.StaticData;
            ItemGridCell_String nameCell = new ItemGridCell_String(abilitySD.Name);
            itemGridUC1.AddRow(new List<ItemGridCell>() { nameCell });

            ItemGridCell_String descCell = new ItemGridCell_String(abilitySD.Description);
            itemGridUC1.AddRow(new List<ItemGridCell>() { descCell });

            ItemGridCell_AbilityType abilityCell = new ItemGridCell_AbilityType(abilitySD.Ability);
            itemGridUC1.AddRow(new List<ItemGridCell>() { abilityCell });

            List<ItemGridCell> abilityAmountCells = new List<ItemGridCell>();
            foreach (float ammount in abilitySD.AbilityAmount)
            {
                ItemGridCell_FloatType abilityAmountCell = new ItemGridCell_FloatType(ammount);
                abilityAmountCells.Add(abilityAmountCell);
            }
            itemGridUC1.AddRow(abilityAmountCells);

            List<ItemGridCell> crewAmountCells = new List<ItemGridCell>();
            if (abilitySD.CrewAmount != null)
            foreach (float crew in abilitySD.CrewAmount)
            {
                ItemGridCell_FloatType cell = new ItemGridCell_FloatType(crew);
                crewAmountCells.Add(cell);
            }
            else
            {
                crewAmountCells.Add(new ItemGridCell_FloatType(0));
            }
            itemGridUC1.AddRow(crewAmountCells);


            List<ItemGridCell> weightAmountCells = new List<ItemGridCell>();
            if (abilitySD.WeightAmount != null)
            {
                foreach (float weight in abilitySD.WeightAmount)
                {
                    ItemGridCell_FloatType cell = new ItemGridCell_FloatType(weight);
                    weightAmountCells.Add(cell);
                }
            }
            else
            {
                weightAmountCells.Add(new ItemGridCell_FloatType(0));
            }
            itemGridUC1.AddRow(weightAmountCells);


            ItemGridCell_AbilityType abilityAffectedCell = new ItemGridCell_AbilityType(abilitySD.AffectsAbility);
            itemGridUC1.AddRow(new List<ItemGridCell>() { abilityAffectedCell });

            List<ItemGridCell> affectedAmountCells = new List<ItemGridCell>();
            foreach (float affect in abilitySD.AffectedAmount)
            {
                ItemGridCell_FloatType cell = new ItemGridCell_FloatType(affect);
                affectedAmountCells.Add(cell);
            }
            itemGridUC1.AddRow(affectedAmountCells);
        }

        /// <summary>
        /// sets how the propertygrid displays ComponentAbilitySD Data
        /// </summary>
        public class AbilitiesDisplayer
        {
            private DataHolder _abilityDH;
            private ComponentAbilitySD _abilitySD { 
                get { return _abilityDH.StaticData; } 
                set { _abilityDH.StaticData = value; } }
            
            private string _name;
            private string _description;

            private AbilityType _ability;
            private List<float> _abilityAmount;
            private List<float> _crewAmount;
            private List<float> _weightAmount;
            private AbilityType _affectsAbility;
            private List<float> _affectedAmount;
            private List<Guid> _techRequirements;


            public AbilitiesDisplayer(DataHolder abilityDH)
            {
                _abilityDH = abilityDH;
                _name = _abilitySD.Name;
                _description = _abilitySD.Description;
                _ability = _abilitySD.Ability;
                _abilityAmount = _abilitySD.AbilityAmount;
                _crewAmount = _abilitySD.CrewAmount;
                _weightAmount = _abilitySD.WeightAmount;
                _affectsAbility = _abilitySD.AffectsAbility;
                _affectedAmount = _abilitySD.AffectedAmount;
                _techRequirements = _abilitySD.TechRequirements;

            }

            [DisplayName("Name")]
            [Description("This is the displayed name for this ability")]
            [Category("Name")]
            [Editor(typeof(String), typeof(string))]
            public String Name {
                get { return _abilitySD.Name; }
                set
                {
                    _name = value; 
                    UpdateAbilityStaticData();
                }
            }

            [DisplayName("Description")]
            [Description("This is the displayed Description for this ability")]
            [Category("Description")]
            [Editor(typeof(String), typeof(string))]
            public String Description
            {
                get { return _abilitySD.Description; }
                set
                {
                    _description = value;
                    UpdateAbilityStaticData();
                }
            }

            [DisplayName("Ability Type")]
            [Description("This is the Ability Type")]
            [Category("AbilityType")]
            [Editor(typeof(AbilityType), typeof(AbilityType))]
            public AbilityType AbilityType
            {
                get { return _abilitySD.Ability; }
                set
                {
                    _ability = value;
                    UpdateAbilityStaticData();
                }
            }

            [DisplayName("Ability Amount")]
            [Description("This is the selectable Ability Amounts")]
            [Category("AbilityAmount")]
            [Editor(typeof(List<float>), typeof(List<float>))]
            public List<float> AbilityAmounts
            {
                get { return _abilitySD.AbilityAmount; }
                set
                {
                    _abilityAmount = value;
                    UpdateAbilityStaticData();
                }
            }

            [DisplayName("Crew Amount")]
            [Description("This is the Crew Amounts")]
            [Category("CrewAmount")]
            [Editor(typeof(List<float>), typeof(List<float>))]
            public List<float> CrewAmounts
            {
                get { return _abilitySD.CrewAmount; }
                set
                {
                    _crewAmount = value;
                    UpdateAbilityStaticData();
                }
            }

            [DisplayName("Size Amount")]
            [Description("This is the size/weight Amounts")]
            [Category("Size")]
            [Editor(typeof(List<float>), typeof(List<float>))]
            public List<float> SizeAmounts
            {
                get { return _abilitySD.WeightAmount; }
                set
                {
                    _weightAmount = value;
                    UpdateAbilityStaticData();
                }
            }

            [DisplayName("Affected Ability Type")]
            [Description("This is the Affected Ability Type")]
            [Category("Affected AbilityType")]
            [Editor(typeof(AbilityType), typeof(AbilityType))]
            public AbilityType AffectedType
            {
                get { return _abilitySD.AffectsAbility; }
                set
                {
                    _affectsAbility = value;
                    UpdateAbilityStaticData();
                }
            }

            [DisplayName("Affected Amount")]
            [Description("This is the affected Ability Amounts")]
            [Category("Affect amount")]
            [Editor(typeof(List<float>), typeof(List<float>))]
            public List<float> AffectedAmount
            {
                get { return _abilitySD.AffectedAmount; }
                set
                {
                    _affectedAmount = value;
                    UpdateAbilityStaticData();
                }
            }

            [DisplayName("Tech Requrements")]
            [Description("This is the Required Techs for the selected Levels")]
            [Category("Tech Requrements")]
            [Editor(typeof(TechListEditor), typeof(UITypeEditor))]
            public List<DataHolder> TechReqs
            {
                get
                {
                    List<DataHolder> techlist = new List<DataHolder>();
                    if (_abilitySD.TechRequirements != null)                       
                        foreach (Guid? guid in _abilitySD.TechRequirements)
                        {
                            if(guid != null)
                                techlist.Add(Data.TechData[(Guid)guid]);
                        }
                    return techlist;
                }
                set
                {

                    _techRequirements = Data.GetGuidList(value);
                    UpdateAbilityStaticData();
                }
            }

            private void UpdateAbilityStaticData()
            {
                ComponentAbilitySD newSD = new ComponentAbilitySD
                {
                    Name = _name,
                    Description = _description,

                    Ability = _ability,
                    AbilityAmount = _abilityAmount,
                    CrewAmount = _crewAmount,
                    WeightAmount = _weightAmount,
                    AffectsAbility = _affectsAbility,
                    AffectedAmount = _affectedAmount,
                    TechRequirements = _techRequirements
                };
                _abilitySD = newSD;

            }
        }

        
        
        class TechListEditor : UITypeEditor
        {
            public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
            {
                return UITypeEditorEditStyle.Modal;
            }
            public override object EditValue(ITypeDescriptorContext context, System.IServiceProvider provider, object value)
            {
                IWindowsFormsEditorService svc = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
                List<DataHolder> dataHolders = value as List<DataHolder>;
                if (svc != null && dataHolders != null)
                {            
                    using (RequiredTechsForm form = new RequiredTechsForm())
                    {
                        form.ValueList = dataHolders;
                        if (svc.ShowDialog(form) == DialogResult.OK)
                        {
                            dataHolders = form.ValueList; // update object
                        }
                    }
                }
                return dataHolders; // can also replace the wrapper object here
            }
        }

        class RequiredTechsForm : Form
        {
            private TechRequirementsUC techucUc;
            private Button okButton;
            public RequiredTechsForm()
            {
                techucUc = new TechRequirementsUC();
                techucUc.AllowDuplicates = true;
                techucUc.Dock = DockStyle.Fill;
                Controls.Add(techucUc);
                
                okButton = new Button();
                okButton.Text = "OK";
                okButton.Dock = DockStyle.Bottom;
                okButton.DialogResult = DialogResult.OK;
                Controls.Add(okButton);
                
            }
            public List<DataHolder> ValueList
            {
                get { return techucUc.RequredTechs; }
                set { techucUc.RequredTechs = value; }
            }
        }

        private void UpdateComponentslist()
        {
            _allComponents = new BindingList<DataHolder>(Data.ComponentData.Values.ToList());

        }



        /// <summary>
        /// creates newSD
        /// </summary>
        /// <param name="guid">guid: current or new</param>
        /// <returns></returns>
        private ComponentSD StaticData(Guid guid)
        {
            List<ComponentAbilitySD> abilityList = new List<ComponentAbilitySD>();
            foreach (var abilityDH in _selectedComponentAbilities)
            {
                abilityList.Add(abilityDH.StaticData);
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
            SetCurrentComponent(StaticData(Guid.NewGuid()));
            Data.SaveToDataStore(_currentComponent);
        }

        private void button_updateExisting_Click(object sender, EventArgs e)
        {
            _currentComponent = StaticData(_currentComponent.ID);
            Data.SaveToDataStore(_currentComponent);
        }

        private void listBox_Abilities_DoubleClick(object sender, EventArgs e)
        {
            DataHolder selectedItem = (DataHolder)listBox_Abilities.SelectedItem;
            StaticData(_currentComponent.ID); //recreate the SD so any changes are updated.
            
            SetCurrentAbility(selectedItem);
        }

        private void listBox_allAbilities_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //create a list of AbilityTypes from the abilitys this component has so I can check if it's already in the list or not. 
            List<AbilityType> abilityTypeslistList = new List<AbilityType>();
            foreach (var abilityDH in _selectedComponentAbilities)
            {
                ComponentAbilitySD abilitySD = abilityDH.StaticData;
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

                _selectedComponentAbilities.Add(new DataHolder(abilitySD));

            }
            //UpdateAbilityAmounts();
        }
    }
}
