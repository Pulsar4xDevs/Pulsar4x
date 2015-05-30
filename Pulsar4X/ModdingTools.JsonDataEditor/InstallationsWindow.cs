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
            mineralsCostsUC1.listBox_MineralsAll.Items.Clear();
            foreach (DataHolder mineral in Data.MineralData.GetDataHolders())
            {
                mineralsCostsUC1.listBox_MineralsAll.Items.Add(mineral);
            }            
        }

        private void mainMenuButton_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.LoadingWindow);
        }
    }
}
