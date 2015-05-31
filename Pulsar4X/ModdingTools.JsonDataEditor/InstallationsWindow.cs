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
        public InstallationsWindow()
        {
            InitializeComponent();
            UpdateInstallationlist();            
            Data.InstallationData.ListChanged += UpdateInstallationlist;
            listBox_AllInstalations.DataSource = AllInstallations;
        }

        private void UpdateInstallationlist()
        {
            AllInstallations = new BindingList<DataHolder>(Data.InstallationData.GetDataHolders().ToList());
        }

        private void mainMenuButton_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.LoadingWindow);
        }

        private void listBox_AllInstalations_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            genericDataUC1.Item((DataHolder)listBox_AllInstalations.SelectedItem);
        }
    }
}
