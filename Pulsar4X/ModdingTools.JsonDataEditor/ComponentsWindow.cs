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
            propertyGrid_PropertyEditor.SelectedObject =  new AbilitysDisplayer(componentAbility);
        }

        /// <summary>
        /// sets how the propertygrid displays TODO: setters should create a new SD.
        /// </summary>
        public class AbilitysDisplayer
        {
            private ComponentAbilitySD _abilitySD;
            public AbilitysDisplayer(ComponentAbilitySD abilitySD)
            {
                _abilitySD = abilitySD;
            }

            [DisplayName("Name")]
            [Description("This is the displayed name for this ability")]
            [Category("Name")]
            [Editor(typeof(String), typeof(string))]
            public String Name {
                get { return _abilitySD.Name; }
                set { _abilitySD.Name = value; }
            }

            [DisplayName("Description")]
            [Description("This is the displayed Description for this ability")]
            [Category("Description")]
            [Editor(typeof(String), typeof(string))]
            public String Description
            {
                get { return _abilitySD.Description; }
                set { _abilitySD.Description = value; }
            }

            [DisplayName("Ability Type")]
            [Description("This is the Ability Type")]
            [Category("AbilityType")]
            [Editor(typeof(AbilityType), typeof(AbilityType))]
            public AbilityType AbilityType
            {
                get { return _abilitySD.Ability; }
                set { _abilitySD.Ability = value; }
            }

            [DisplayName("Ability Amount")]
            [Description("This is the selectable Ability Amounts")]
            [Category("AbilityAmount")]
            [Editor(typeof(List<float>), typeof(List<float>))]
            public List<float> AbilityAmounts
            {
                get { return _abilitySD.AbilityAmount; }
                set { _abilitySD.AbilityAmount = value; }
            }

            [DisplayName("Crew Amount")]
            [Description("This is the Crew Amounts")]
            [Category("CrewAmount")]
            [Editor(typeof(List<float>), typeof(List<float>))]
            public List<float> CrewAmounts
            {
                get { return _abilitySD.CrewAmount; }
                set { _abilitySD.CrewAmount = value; }
            }

            [DisplayName("Size Amount")]
            [Description("This is the size/weight Amounts")]
            [Category("Size")]
            [Editor(typeof(List<float>), typeof(List<float>))]
            public List<float> SizeAmounts
            {
                get { return _abilitySD.WeightAmount; }
                set { _abilitySD.WeightAmount = value; }
            }

            [DisplayName("Affected Ability Type")]
            [Description("This is the Affected Ability Type")]
            [Category("Affected AbilityType")]
            [Editor(typeof(AbilityType), typeof(AbilityType))]
            public AbilityType AffectedType
            {
                get { return _abilitySD.AffectsAbility; }
                set { _abilitySD.AffectsAbility = value; }
            }

            [DisplayName("Affected Amount")]
            [Description("This is the affected Ability Amounts")]
            [Category("Affect amount")]
            [Editor(typeof(List<float>), typeof(List<float>))]
            public List<float> AffectedAmount
            {
                get { return _abilitySD.AffectedAmount; }
                set { _abilitySD.AffectedAmount = value; }
            }

            [DisplayName("Tech Requrements")]
            [Description("This is the Required Techs for the selected Levels")]
            [Category("Tech Requrements")]
            [Editor(typeof(TechListEditor), typeof(UITypeEditor))]
            public List<DataHolder> TechReqs
            {
                get { return Data.TechData.GetDataHolders(_abilitySD.TechRequiremets).ToList(); }
                set { _abilitySD.TechRequiremets = Data.TechData.GetGuids(value).ToList(); }
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
                    using (RequredTechsForm form = new RequredTechsForm())
                    {
                        form.ValueList = dataHolders;
                        if (svc.ShowDialog(form) == DialogResult.OK)
                        {
                            //foo.Guid = form.Value; // update object
                        }
                    }
                }
                return value; // can also replace the wrapper object here
            }
        }

        class RequredTechsForm : Form
        {
            private TechRequirementsUC techucUc;
            private Button okButton;
            public RequredTechsForm()
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
    }
}
