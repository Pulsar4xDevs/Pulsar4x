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
        public List<DataHolder> AllMineralSds { get; set; }
        public Dictionary<DataHolder, int> MineralsCosts { get; set; } 
        public MineralsCostsUC()
        {
            InitializeComponent();
            MineralsCosts = new Dictionary<DataHolder, int>();
            dataGridView_MineralCosts.DataSource = MineralsCosts.ToArray();
            Data.MineralData.ListChanged += UpdateMineralList;
        }

        public MineralsCostsUC(List<DataHolder> mineralData)
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

        private void listBox_MineralsAll_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!MineralsCosts.ContainsKey((DataHolder)listBox_MineralsAll.SelectedItem))
                MineralsCosts.Add((DataHolder)listBox_MineralsAll.SelectedItem, 0);
            dataGridView_MineralCosts.DataSource = MineralsCosts.ToArray();
        }
    }
}
