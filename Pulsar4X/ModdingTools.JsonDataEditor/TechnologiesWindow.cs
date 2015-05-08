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
    public partial class TechnologiesWindow : UserControl
    {
        private bool _isLoaded = false;
        private bool _updating = false;
        private string _fileName;
        private Guid _selectedItemGuid;


        public TechnologiesWindow()
        {
            InitializeComponent();

            foreach(ResearchCategories cat in Enum.GetValues(typeof(ResearchCategories)))
                categoryComboBox.Items.Add(cat);

            Data.TechListChanged += UpdateSearch;
            Data.TechLoadedFilesListChanged += UpdateFileList;

            UpdateSelectedItem();
        }

        private void UpdateFileList()
        {
            selectedFileComboBox.Items.Clear();
            foreach(string str in Data.GetLoadedFiles())
            {
                selectedFileComboBox.Items.Add(str);
            }
        }

        private void UpdateSearch()
        {
            string searchPattern = searchBox.Text;

            availibleTechs.BeginUpdate();
            availibleTechs.Items.Clear();

            foreach (TechDataHolder tech in Data.GetTechDataHolders())
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

            TechSD newTechSD = Data.GetTech(_selectedItemGuid);

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

            Data.UpdateTech(newTechSD);
        }

        private void UpdateSelectedItem()
        {
            _updating = true;

            TechSD techSD;
            if(_selectedItemGuid == Guid.Empty)
                techSD = new TechSD {Name = "Name", Description = "Description", Category = ResearchCategories.BiologyGenetics, Id = Guid.Empty, Cost = 1000, Requirements = new List<Guid>()};
            else
                techSD = Data.GetTech(_selectedItemGuid);

            guidDataLabel.Text = techSD.Id.ToString();
            nameTextBox.Text = techSD.Name;
            descTextBox.Text = techSD.Description;
            categoryComboBox.SelectedItem = techSD.Category;
            costTextBox.Text = techSD.Cost.ToString();

            requirementsListBox.BeginUpdate();
            requirementsListBox.Items.Clear();
            foreach(Guid requirementGuid in techSD.Requirements)
            {
                requirementsListBox.Items.Add(Data.GetTechDataHolder(requirementGuid));
            }
            requirementsListBox.EndUpdate();

            _updating = false;
        }

        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            UpdateSearch(); //Still here because we don't change tech list but hiding some of them
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
            Data.UpdateTech(newTechSD);
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
            Data.RemoveTech(holder.Guid);

            UpdateSelectedItem();
        }

        private void selectedFileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string filePath = (string)selectedFileComboBox.SelectedItem;
            Data.SetSelectedTechFile(filePath);
        }
    }
}
