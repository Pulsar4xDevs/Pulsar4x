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
        private BindingList<DataHolder> _allMinerals { get; set; }
        private Dictionary<DataHolder, int> _mineralsCosts = new Dictionary<DataHolder, int>();

        public Dictionary<DataHolder, int> MineralCosts
        {
            get { return _mineralsCosts; }
            set
            {
                _mineralsCosts = value;   
                if (_mineralsCosts != null)
                    dataGridView_MineralCosts.DataSource = _mineralsCosts.ToArray();
            }
        }
        public MineralsCostsUC()
        {
            InitializeComponent();
            UpdateMineralList();
            MineralCosts = new Dictionary<DataHolder, int>();
            
            Data.MineralData.ListChanged += UpdateMineralList;
            listBox_MineralsAll.DataSource = _allMinerals;
        }

        public Dictionary<Guid, int> GetData
        {
            get
            {
                Dictionary<Guid, int> dict = new Dictionary<Guid, int>();
                foreach (var kvp in _mineralsCosts)
                {
                    dict.Add(kvp.Key.Guid,kvp.Value);
                }
                return dict;
            }
        }


        private void UpdateMineralList()
        {
            _allMinerals = new BindingList<DataHolder>(Data.MineralData.GetDataHolders().ToList());
        }


        private void listBox_MineralsAll_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox_MineralsAll_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!_mineralsCosts.ContainsKey((DataHolder)listBox_MineralsAll.SelectedItem))
            {
                _mineralsCosts.Add((DataHolder)listBox_MineralsAll.SelectedItem, 0);
                //dataGridView_MineralCosts.CurrentCell = dataGridView_MineralCosts.CurrentRow.Cells[1];
                //dataGridView_MineralCosts.BeginEdit(true);
            }
            dataGridView_MineralCosts.DataSource = _mineralsCosts.ToArray();
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
