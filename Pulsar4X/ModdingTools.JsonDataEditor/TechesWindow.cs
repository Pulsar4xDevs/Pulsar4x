using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Pulsar4X.ECSLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModdingTools.JsonDataEditor
{
    public partial class TechesWindow : UserControl
    {
        private bool _isLoaded = false;
        private bool _updating = false;
        private string _fileName;
        private Guid _selectedItemGuid;

        //stolen from StaticDataManager
        private static JsonSerializer serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        private JDictionary<Guid, TechSD> _allTechs = new JDictionary<Guid, TechSD>();
        private Dictionary<Guid, TechDataHolder> _allDataHolders = new Dictionary<Guid, TechDataHolder>();
        private List<ResearchCategories> _categories;

        public TechesWindow()
        {
            InitializeComponent();

            _categories = new List<ResearchCategories>
            {
                ResearchCategories.BiologyGenetics, 
                ResearchCategories.ConstructionProduction, 
                ResearchCategories.DefensiveSystems, 
                ResearchCategories.EnergyWeapons, 
                ResearchCategories.LogisticsGroundCombat, 
                ResearchCategories.MissilesKineticWeapons, 
                ResearchCategories.PowerAndPropulsion, 
                ResearchCategories.SensorsAndFireControl, 
                ResearchCategories.FromStaticData00, 
                ResearchCategories.FromStaticData01, 
                ResearchCategories.FromStaticData02, 
                ResearchCategories.FromStaticData03, 
                ResearchCategories.FromStaticData04, 
                ResearchCategories.FromStaticData05, 
                ResearchCategories.FromStaticData06, 
                ResearchCategories.FromStaticData07, 
                ResearchCategories.FromStaticData08, 
                ResearchCategories.FromStaticData09,
            };
            foreach(ResearchCategories cat in _categories)
                categoryComboBox.Items.Add(cat);

            UpdateSelectedItem();
        }

        private void UpdateSearch()
        {
            //if(!_isLoaded)
            //    return;
            string searchPattern = searchBox.Text;

            availibleTechs.BeginUpdate();
            availibleTechs.Items.Clear();

            foreach (TechDataHolder tech in _allDataHolders.Values)
            {
                if(string.IsNullOrWhiteSpace(searchPattern) || tech.Name.ToLower().Contains(searchPattern.ToLower()))
                    availibleTechs.Items.Add(tech);
            }

            availibleTechs.EndUpdate();
        }

        private void GatherAndUpdateData()
        {
            if(_selectedItemGuid == Guid.Empty)
                return;

            if(_updating)
                return;

            TechSD newTechSD = _allTechs[_selectedItemGuid];

            string newName = nameTextBox.Text;
            if(!string.IsNullOrWhiteSpace(newName))
            {
                newTechSD.Name = newName;
                nameTextBox.BackColor = Color.White;
            }
            else
                nameTextBox.BackColor = Color.Red;

            string newDesc = descTextBox.Text;
            if(!string.IsNullOrWhiteSpace(newDesc))
                newTechSD.Description = newDesc;

            newTechSD.Category = (ResearchCategories)categoryComboBox.SelectedItem;

            int newCost;
            if(Int32.TryParse(costTextBox.Text, out newCost) && newCost > 0)
            {
                newTechSD.Cost = newCost;
                costTextBox.BackColor = Color.White;
            }
            else
                costTextBox.BackColor = Color.Red;

            List<TechDataHolder> requirements = requirementsListBox.Items.Cast<TechDataHolder>().ToList();
            newTechSD.Requirements = requirements.ConvertAll(entry => entry.Guid);

            _allTechs[_selectedItemGuid] = newTechSD;
            _allDataHolders[_selectedItemGuid].Name = newTechSD.Name;

            UpdateSearch();
        }

        private void UpdateSelectedItem()
        {
            _updating = true;

            TechSD techSD;
            if(_selectedItemGuid == Guid.Empty)
                techSD = new TechSD {Name = "Name", Description = "Description", Category = ResearchCategories.BiologyGenetics, Id = Guid.Empty, Cost = 1000, Requirements = new List<Guid>()};
            else
                techSD = _allTechs[_selectedItemGuid];

            guidDataLabel.Text = techSD.Id.ToString();
            nameTextBox.Text = techSD.Name;
            descTextBox.Text = techSD.Description;
            categoryComboBox.SelectedItem = techSD.Category;
            costTextBox.Text = techSD.Cost.ToString();

            requirementsListBox.BeginUpdate();
            requirementsListBox.Items.Clear();
            foreach(Guid requirementGuid in techSD.Requirements)
            {
                requirementsListBox.Items.Add(_allDataHolders[requirementGuid]);
            }
            requirementsListBox.EndUpdate();

            _updating = false;
        }

        private void TrySave(bool noButton = false)
        {
            if(_isLoaded)
            {

                DialogResult result = MessageBox.Show("Currently loaded file is " + _fileName + "\nDo you want to save file?", "Savefile", (noButton) ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.OKCancel);
                if(result == DialogResult.Cancel)
                    return;

                if(result == DialogResult.No)
                {
                    _isLoaded = false;
                    _allTechs = new JDictionary<Guid, TechSD>();
                    _allDataHolders = new Dictionary<Guid, TechDataHolder>();

                }
                else if(result == DialogResult.Yes || result == DialogResult.OK)
                {
                    if(SaveFile())
                        MessageBox.Show("Saved");
                }
            }
        }

        private bool SaveFile()
        {
            if(string.IsNullOrWhiteSpace(_fileName))
                return false;

            DataExportContainer exportContainer = new DataExportContainer {Type = "Techs", Data = _allTechs};

            using(StreamWriter streamWriter = new StreamWriter(_fileName))
            using(JsonWriter writer = new JsonTextWriter(streamWriter))
            {
                serializer.Serialize(writer, exportContainer);
            }
            return true;
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("There is some troubles with reading TechnologyData.json even in unit tests.\nI'm sure it would be fixed in short time. Be patient.");
            //return;

            if(_isLoaded)
                TrySave(true);

            if(fileDialog.ShowDialog() == DialogResult.OK)
            {
                _fileName = fileDialog.FileName;

                if(!_fileName.EndsWith(".json"))
                    return;

                JObject jobj;

                using(StreamReader streamReader = new StreamReader(_fileName))
                using(JsonReader reader = new JsonTextReader(streamReader))
                {
                    jobj = (JObject)serializer.Deserialize(reader);
                }
              
                if (jobj["Type"].ToObject<string>(serializer) != "Techs")
                {
                    MessageBox.Show("Invalid file type");
                    return;
                }

                JDictionary<Guid, TechSD> dict = jobj["Data"].ToObject < JDictionary<Guid, TechSD>>(serializer);

                _allTechs.Clear();
                _allDataHolders.Clear();

                foreach(TechSD techSD in dict.Values)
                {
                    _allTechs[techSD.Id] = techSD;
                    _allDataHolders[techSD.Id] = new TechDataHolder(techSD.Name, techSD.Id);
                }

                _isLoaded = true;

                UpdateSearch();
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            TrySave();
        }

        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            UpdateSearch();
        }

        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            GatherAndUpdateData();
        }

        private void descTextBox_TextChanged(object sender, EventArgs e)
        {
            GatherAndUpdateData();
        }

        private void categoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            GatherAndUpdateData();
        }

        private void costTextBox_TextChanged(object sender, EventArgs e)
        {
            GatherAndUpdateData();
        }

        private void requirementsListBox_SelectedValueChanged(object sender, EventArgs e)
        {
            GatherAndUpdateData();
        }

        private void addRequirementButton_Click(object sender, EventArgs e)
        {
            if(availibleTechs.SelectedItem == null)
                return;
            if(requirementsListBox.Items.Cast<TechDataHolder>().Any(dataHolder => dataHolder.Guid == ((TechDataHolder)availibleTechs.SelectedItem).Guid))
                return;
            requirementsListBox.Items.Add(availibleTechs.SelectedItem);
        }

        private void removeRequirementButton_Click(object sender, EventArgs e)
        {
            if(requirementsListBox.SelectedItem == null)
                return;
            requirementsListBox.Items.RemoveAt(requirementsListBox.SelectedIndex);
        }

        private void newTechButton_Click(object sender, EventArgs e)
        {
            TechSD newTechSD = new TechSD() {Name = "New Tech", Description = "Description Here", Id = Guid.NewGuid(), Cost = 1000, Requirements = new List<Guid>()};
            TechDataHolder newTechDataHolder = new TechDataHolder(newTechSD.Name, newTechSD.Id);
            _allTechs[newTechSD.Id] = newTechSD;
            _allDataHolders[newTechSD.Id] = newTechDataHolder;

            UpdateSearch();
        }

        private void selectTechButton_Click(object sender, EventArgs e)
        {
            if(availibleTechs.SelectedItem == null)
                return;
            _selectedItemGuid = ((TechDataHolder)availibleTechs.SelectedItem).Guid;
            UpdateSelectedItem();
        }

        private void removeTechButton_Click(object sender, EventArgs e)
        {
            if(availibleTechs.SelectedItem == null)
                return;
            TechDataHolder holder = (TechDataHolder)availibleTechs.SelectedItem;
            if(_selectedItemGuid == holder.Guid)
                _selectedItemGuid = Guid.Empty;
            _allDataHolders.Remove(holder.Guid);
            _allTechs.Remove(holder.Guid);

            UpdateSearch();
            UpdateSelectedItem();
        }
    }

    public class TechDataHolder
    {
        public string Name;
        public Guid Guid;

        public TechDataHolder(string name, Guid guid)
        {
            Name = name;
            Guid = guid;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
