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
    public partial class InstallationsWindow : UserControl
    {
        BindingList<DataHolder> AllInstallations { get; set; }
        InstallationSD CurrentInstallation { get; set; }

        //InstallationSD SelectedInstallation
        //{
        //    get { return installationUC1.StaticData; }
        //    set
        //    {
        //        CurrentInstallation = value; 
        //        SetCurrentInstalation();
        //    }
        //}
        public InstallationsWindow()
        {
            InitializeComponent();
            UpdateInstallationlist();            
            Data.InstallationData.ListChanged += UpdateInstallationlist;
            listBox_AllInstalations.DataSource = AllInstallations;
            CurrentInstallation = new InstallationSD();
        }

        private void SetCurrentInstallation(InstallationSD installationSD)
        {
            CurrentInstallation = installationSD;
            installationUC1.StaticData = CurrentInstallation;
            //DataHolder dh;
            //if (!Data.InstallationData.TryGetDataHolder(CurrentInstallation.ID, out dh))
            //{
            //    dh = new DataHolder("", "", new Guid());
            //}
            DataHolder dh = Data.InstallationData.GetDataHolderOrNull(CurrentInstallation.ID);
            if (dh == null)
            {
                dh = new DataHolder("", "", new Guid());
            }
            genericDataUC1.Item = dh;
            genericDataUC1.Description = CurrentInstallation.Description;
            abilitiesListUC1.AbilityAmount = CurrentInstallation.BaseAbilityAmounts;
            techRequirementsUC1.RequredTechs = Data.TechData.GetDataHolders(CurrentInstallation.TechRequirements).ToList();
            mineralsCostsUC1.MineralCosts = MineralCostsDictionary(CurrentInstallation.ResourceCosts);
        }

        private void UpdateInstallationlist()
        {
            AllInstallations = new BindingList<DataHolder>(Data.InstallationData.GetDataHolders().ToList());
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

        private void mainMenuButton_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.LoadingWindow);
        }

        private void listBox_AllInstalations_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DataHolder selectedItem = (DataHolder)listBox_AllInstalations.SelectedItem;
            SetCurrentInstallation(Data.InstallationData.Get(selectedItem.Guid));
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

        /// <summary>
        /// creates newSD
        /// </summary>
        /// <param name="guid">guid: current or new</param>
        /// <returns></returns>
        private InstallationSD staticData(Guid guid)
        {
            InstallationSD newSD = new InstallationSD
            {
                ID = Guid.NewGuid(),
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

        private void button_saveNew_Click(object sender, EventArgs e)
        {
            SetCurrentInstallation(staticData(Guid.NewGuid()));
            Data.InstallationData.Update(CurrentInstallation);
             
        }

        private void button_updateExsisting_Click(object sender, EventArgs e)
        {
            CurrentInstallation = staticData(CurrentInstallation.ID);
            Data.InstallationData.Update(CurrentInstallation);
        }
    }
}
