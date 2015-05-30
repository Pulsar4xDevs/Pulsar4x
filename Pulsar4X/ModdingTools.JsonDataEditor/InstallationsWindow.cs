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

        public InstallationsWindow()
        {
            InitializeComponent();
            Data.MineralData.LoadedFilesListChanged += setLists;
        }

        public void setLists()
        {
            List<string>minerals = new List<string>();
            foreach (var mineralSD in Data.MineralData.GetDataHolders())
            {
                minerals.Add(mineralSD.Name);
            }
            this.mineralsCostsUC1.AllMineralSds = minerals;
        }

        private void mainMenuButton_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.LoadingWindow);
        }
    }
}
