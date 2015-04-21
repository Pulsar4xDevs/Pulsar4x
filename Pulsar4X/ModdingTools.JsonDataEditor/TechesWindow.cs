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
        private string _fileName;
        private Guid _selectedItemGuid;

        //stolen from StaticDataManager
        private static JsonSerializer serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        private JDictionary<Guid, TechSD> _allTechs = new JDictionary<Guid, TechSD>();
        private Dictionary<Guid, TechDataHolder> _allDataHolders = new Dictionary<Guid, TechDataHolder>();

        public TechesWindow()
        {
            InitializeComponent();
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
                if(string.IsNullOrWhiteSpace(searchPattern) || tech.Name.Contains(searchPattern))
                    availibleTechs.Items.Add(tech);
            }

            availibleTechs.EndUpdate();
        }

        private void GatherAndUpdateData()
        {
            if(_selectedItemGuid == Guid.Empty)
                return;
            TechSD newTechSD = _allTechs[_selectedItemGuid];

            string newName = nameTextBox.Text;
            if(!string.IsNullOrWhiteSpace(newName))
                newTechSD.Name = newName;

            string newDesc = descTextBox.Text;
            if(!string.IsNullOrWhiteSpace(newDesc))
                newTechSD.Description = newDesc;

            //Category here

            int newCost = (int)Math.Round(costUpDown.Value);
            if(newCost > 0)
                newTechSD.Cost = newCost;

            List<TechDataHolder> requirements = requirementsListBox.Items.Cast<TechDataHolder>().ToList();
            newTechSD.Requirements = requirements.ConvertAll(entry => entry.Guid);

            _allTechs[_selectedItemGuid] = newTechSD;
            _allDataHolders[_selectedItemGuid].Name = newTechSD.Name;

            UpdateSearch();
        }

        private void UpdateSelectedItem()
        {
            if(_selectedItemGuid == Guid.Empty)
                return;
            TechSD techSD = _allTechs[_selectedItemGuid];

            nameTextBox.Text = techSD.Name;
            descTextBox.Text = techSD.Description;
            //Category
            costUpDown.Value = techSD.Cost;

            requirementsListBox.BeginUpdate();
            requirementsListBox.Items.Clear();
            foreach(Guid requirementGuid in techSD.Requirements)
            {
                requirementsListBox.Items.Add(_allDataHolders[requirementGuid]);
            }
            requirementsListBox.EndUpdate();
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("There is some troubles with reading TechnologyData.json even in unit tests.\nI'm sure it would be fixed in short time. Be patient.");
            return;

            if(_isLoaded)
                return;
            if(fileDialog.ShowDialog() == DialogResult.OK)
            {
                _fileName = fileDialog.FileName;

                if(!_fileName.EndsWith(".json"))
                    return;

                JObject jobj;

                StreamReader streamReader = new StreamReader(_fileName);
                using(JsonReader reader = new JsonTextReader(streamReader))
                {
                    jobj = (JObject)serializer.Deserialize(reader);
                }

                //if(container.Type != "Techs")
                //{
                //    MessageBox.Show("Invalid file type");
                //    return;
                //}

                //JDictionary<Guid, TechSD> techs = (JDictionary<Guid, TechSD>)container.Data;
                JToken jdata = jobj["Data"];
                JDictionary<Guid, TechSD> dict = jdata.ToObject<JDictionary<Guid, TechSD>>();

                foreach(TechSD techSD in dict.Values)
                    _allDataHolders.Add(techSD.Id, new TechDataHolder(techSD.Name, techSD.Id));

                _isLoaded = true;

                UpdateSearch();
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
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

        private void costUpDown_ValueChanged(object sender, EventArgs e)
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
