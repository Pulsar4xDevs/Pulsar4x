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
        public BindingList<DataHolder> AllMineralSds { get; set; }
        public Dictionary<DataHolder, int> MineralsCosts { get; set; } 
        public MineralsCostsUC()
        {
            InitializeComponent();
            UpdateMineralList();
            MineralsCosts = new Dictionary<DataHolder, int>();
            dataGridView_MineralCosts.DataSource = MineralsCosts.ToArray();
            Data.MineralData.ListChanged += UpdateMineralList;
            listBox_MineralsAll.DataSource = AllMineralSds;
        }


        private void UpdateMineralList()
        {
            AllMineralSds = new BindingList<DataHolder>(Data.MineralData.GetDataHolders().ToList());
        }


        private void listBox_MineralsAll_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox_MineralsAll_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!MineralsCosts.ContainsKey((DataHolder)listBox_MineralsAll.SelectedItem))
            {
                MineralsCosts.Add((DataHolder)listBox_MineralsAll.SelectedItem, 0);
                //dataGridView_MineralCosts.CurrentCell = dataGridView_MineralCosts.CurrentRow.Cells[1];
                //dataGridView_MineralCosts.BeginEdit(true);
            }
            dataGridView_MineralCosts.DataSource = MineralsCosts.ToArray();
        }

        private void dataGridView_MineralCosts_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {            
                dataGridView_MineralCosts.CurrentCell.ReadOnly = false;
                dataGridView_MineralCosts.BeginEdit(true);
            }
        }
    }
}
