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
        private bool _updating = false;
        private Guid _selectedItemGuid;

        public TechnologiesWindow()
        {
            InitializeComponent();

            foreach(ResearchCategories cat in Enum.GetValues(typeof(ResearchCategories)))
                categoryComboBox.Items.Add(cat);

            //Data.TechData.ListChanged += UpdateSearch;
            //Data.TechData.LoadedFilesListChanged += UpdateFileList;

            UpdateSearch();
            UpdateSelectedItem();
        }



        private void UpdateSearch()
        {
            string searchPattern = searchBox.Text;

            availibleTechs.BeginUpdate();
            availibleTechs.Items.Clear();

            foreach (DataHolder tech in Data.TechData.Values)
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

            TechSD newTechSD = Data.TechData[_selectedItemGuid].StaticData;

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
                newTechSD.CostFormula = newCost.ToString();
                costTextBox.BackColor = Color.White;
            }
            else
                costTextBox.BackColor = Color.Red;

            List<DataHolder> requirements = requirementsListBox.Items.Cast<DataHolder>().ToList();
            //newTechSD.Requirements = requirements.ConvertAll(entry => entry.Guid);

            Data.SaveToDataStore(newTechSD);
        }

        private void UpdateSelectedItem()
        {
            _updating = true;

            TechSD techSD;
            if(_selectedItemGuid == Guid.Empty)
                techSD = new TechSD {
                    Name = "Name", 
                    Description = "Description", 
                    ID = Guid.Empty, 
                    MaxLevel = 1,
                    DataFormula = "1",

                    Category = ResearchCategories.BiologyGenetics, 
                    Requirements = new JDictionary<Guid, int>(),
                    CostFormula = "1000", 
                };
            else
                techSD = Data.TechData[_selectedItemGuid].StaticData;

            guidDataLabel.Text = techSD.ID.ToString();
            nameTextBox.Text = techSD.Name;
            descTextBox.Text = techSD.Description;
            categoryComboBox.SelectedItem = techSD.Category;
            costTextBox.Text = techSD.CostFormula;

            requirementsListBox.BeginUpdate();
            requirementsListBox.Items.Clear();
            foreach(Guid requirementGuid in techSD.Requirements.Keys)
            {
                requirementsListBox.Items.Add(Data.TechData[requirementGuid]);
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
            if(requirementsListBox.Items.Cast<DataHolder>().Any(dataHolder => dataHolder.Guid == ((DataHolder)availibleTechs.SelectedItem).Guid))
                return;
            requirementsListBox.Items.Add(availibleTechs.SelectedItem);
            GatherAndUpdateData();
        }

        private void removeRequirementButton_Click(object sender, EventArgs e)
        {
            if(requirementsListBox.SelectedItem == null)
                return;
            requirementsListBox.Items.RemoveAt(requirementsListBox.SelectedIndex);
            GatherAndUpdateData();
        }

        private void newTechButton_Click(object sender, EventArgs e)
        {
            TechSD newTechSD = new TechSD() {
                Name = "New Tech", 
                Description = "Description Here", 
                ID = Guid.NewGuid(), 
                CostFormula = "", 
                Requirements = new JDictionary<Guid, int>()};
            Data.SaveToDataStore(newTechSD);
        }

        private void selectTechButton_Click(object sender, EventArgs e)
        {
            if(availibleTechs.SelectedItem == null)
                return;
            _selectedItemGuid = ((DataHolder)availibleTechs.SelectedItem).Guid;
            UpdateSelectedItem();
        }

        private void removeTechButton_Click(object sender, EventArgs e)
        {
            if(availibleTechs.SelectedItem == null)
                return;
            DataHolder holder = (DataHolder)availibleTechs.SelectedItem;
            if(_selectedItemGuid == holder.Guid)
                _selectedItemGuid = Guid.Empty;
            Data.TechData.Remove(holder.Guid);

            UpdateSelectedItem();
        }

        private void selectedFileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string filePath = (string)selectedFileComboBox.SelectedItem;
            //Data.TechData.SetSelectedFile(filePath);
        }

        private void mainMenuButton_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.LoadingWindow);
        }

        private void availibleTechs_MouseDoubleClick(object sender, EventArgs e)
        {
            if (availibleTechs.SelectedItem == null)
                return;
            _selectedItemGuid = ((DataHolder)availibleTechs.SelectedItem).Guid;
            UpdateSelectedItem();
        }

        private void costLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
