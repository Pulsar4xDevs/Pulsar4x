using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor
{
    public partial class InstallationsWindow : UserControl
    {
        private BindingList<DataHolder> _allInstallations = new BindingList<DataHolder>();
        private InstallationSD _currentInstallation;

        public InstallationsWindow()
        {
            InitializeComponent();
            UpdateInstallationlist();            
            Data.InstallationData.ListChanged += UpdateInstallationlist;
            listBox_AllInstalations.DataSource = _allInstallations;
            _currentInstallation = new InstallationSD();
        }

        private void SetCurrentInstallation(InstallationSD installationSD)
        {
            _currentInstallation = installationSD;
            installationUC1.StaticData = _currentInstallation;

            DataHolder dh = Data.InstallationData.GetDataHolder(_currentInstallation.ID, false);
            if (dh == null)
            {
                string file = Data.InstallationData.GetLoadedFiles()[0];
                dh = new DataHolder(installationSD, file);
            }
            genericDataUC1.Item = dh;
            genericDataUC1.Description = _currentInstallation.Description;
            abilitiesListUC1.AbilityAmount = _currentInstallation.BaseAbilityAmounts;
            techRequirementsUC1.RequredTechs = Data.TechData.GetDataHolders(_currentInstallation.TechRequirements).ToList();
            mineralsCostsUC1.MineralCosts = MineralCostsDictionary(_currentInstallation.ResourceCosts);
        }

        private void UpdateInstallationlist()
        {
            _allInstallations = new BindingList<DataHolder>(Data.InstallationData.GetDataHolders().ToList());
            listBox_AllInstalations.DataSource = _allInstallations;
        }

        private Dictionary<DataHolder, int> MineralCostsDictionary(Dictionary<Guid, int> guidDictionary  )
        {
            Dictionary<DataHolder, int> dataHandlerDictionary = new Dictionary<DataHolder, int>();
            foreach (var kvp in guidDictionary)
            {
                DataHolder mineral = Data.MineralData.GetDataHolder(kvp.Key);
                dataHandlerDictionary.Add(mineral, kvp.Value);
            }
            return dataHandlerDictionary;
        }

        /// <summary>
        /// creates newSD
        /// </summary>
        /// <param name="guid">guid: current or new</param>
        /// <returns></returns>
        private InstallationSD StaticData(Guid guid)
        {
            InstallationSD newSD = new InstallationSD
            {
                ID = guid,
                Name = genericDataUC1.GetName,
                Description = genericDataUC1.Description,
                PopulationRequired = installationUC1.GetPopReqirement,
                CargoSize = installationUC1.GetCargoSize,
                BuildPoints = installationUC1.GetBuildPoints,
                WealthCost = installationUC1.GetWealthCost,
                BaseAbilityAmounts = abilitiesListUC1.GetData,
                TechRequirements = techRequirementsUC1.GetData,
                ResourceCosts = mineralsCostsUC1.GetData
            };
            return newSD;
        }

        private void listBox_AllInstalations_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DataHolder selectedItem = (DataHolder)listBox_AllInstalations.SelectedItem;
            SetCurrentInstallation(Data.InstallationData.Get(selectedItem.Guid));
        }

        private void mainMenuButton_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.LoadingWindow);
        }

        private void button_clearSelection_Click(object sender, EventArgs e)
        {
            InstallationSD newEmptySD = new InstallationSD 
            {
                ID = new Guid(),
                Name = "",
                Description = "",
                PopulationRequired = 0,
                CargoSize = 0,
                BuildPoints = 0,
                WealthCost = 0,
                BaseAbilityAmounts = new JDictionary<AbilityType,int>(),
                TechRequirements = new List<Guid>(),
                ResourceCosts = new JDictionary<Guid,int>()
            };
            SetCurrentInstallation(newEmptySD);
        }

        private void button_saveNew_Click(object sender, EventArgs e)
        {
            SetCurrentInstallation(StaticData(Guid.NewGuid()));
            Data.InstallationData.Update(_currentInstallation);
             
        }

        private void button_updateExsisting_Click(object sender, EventArgs e)
        {
            _currentInstallation = StaticData(_currentInstallation.ID);
            Data.InstallationData.Update(_currentInstallation);
        }
    }
}
