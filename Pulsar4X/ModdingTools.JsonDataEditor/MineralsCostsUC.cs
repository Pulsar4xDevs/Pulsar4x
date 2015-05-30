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
    public partial class MineralsCostsUC : UserControl
    {
        public List<string> AllMineralSds { get; set; } 
        public MineralsCostsUC()
        {
            InitializeComponent();
            Data.MineralData.ListChanged += UpdateMineralList;
        }

        public MineralsCostsUC(List<string> mineralData)
        {
            AllMineralSds = mineralData;
            listBox_MineralsAll.DataSource = AllMineralSds;
        }

        private void UpdateMineralList()
        {
            listBox_MineralsAll.DataSource = AllMineralSds;
        }

        private void listBox_MineralsAll_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
