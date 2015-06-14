using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor
{
    public partial class MineralsCostsUC : UserControl
    {
        private BindingList<DataHolder> _allMinerals = new BindingList<DataHolder>();
        private Dictionary<DataHolder, int> _mineralsCosts = new Dictionary<DataHolder, int>();

            
            
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<DataHolder, int> MineralCosts
        {
            get { return _mineralsCosts; }
            set
            {
                _mineralsCosts = value;
                if (_mineralsCosts != null)
                {
                    UpdateMineralCosts();
                }
            }
        }
        public MineralsCostsUC()
        {
            InitializeComponent();
            Data.MineralData.ListChanged += UpdateMineralList;
            UpdateMineralList();

            dataGridView_MineralCosts.Columns.Add("Key", "Mineral");
            dataGridView_MineralCosts.Columns.Add("Values", "Amount");
            UpdateMineralCosts();
        }

        public JDictionary<Guid, int> GetData
        {
            get
            {
                JDictionary<Guid, int> dict = new JDictionary<Guid, int>();
                foreach (var kvp in _mineralsCosts)
                {
                    dict.Add(kvp.Key.Guid,kvp.Value);
                }
                return dict;
            }
        }

        private void UpdateMineralCosts()
        {
            dataGridView_MineralCosts.DataSource = null;
            dataGridView_MineralCosts.Rows.Clear();

            foreach (KeyValuePair<DataHolder, int> item in _mineralsCosts)
            {
                dataGridView_MineralCosts.Rows.Add(item.Key, item.Value);
            }
        }



        private void UpdateMineralList()
        {
            _allMinerals = new BindingList<DataHolder>(Data.MineralData.GetDataHolders().ToList());
            listBox_MineralsAll.DataSource = _allMinerals;
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
                //dataGridView_MineralCosts.CurrentCell.ReadOnly = false;
                //dataGridView_MineralCosts.BeginEdit(true);
            }
            UpdateMineralCosts();
        }

        private void dataGridView_MineralCosts_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {            
                dataGridView_MineralCosts.CurrentCell.ReadOnly = false;
                dataGridView_MineralCosts.BeginEdit(true);
            }
        }

        private void dataGridView_MineralCosts_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataHolder key = (DataHolder)dataGridView_MineralCosts.Rows[e.RowIndex].Cells[0].Value;
            DataGridViewCell cell = dataGridView_MineralCosts.Rows[e.RowIndex].Cells[1];
            object cellValue = dataGridView_MineralCosts.Rows[e.RowIndex].Cells[1].Value;
            if (cellValue is int)
            {
                _mineralsCosts[key] = (int)cellValue;
            }
            else if (cellValue is string)
            {
                int amount;
                if (Int32.TryParse((string)cell.Value, out amount))
                    _mineralsCosts[key] = amount;
            }
            else
                cell.Value = _mineralsCosts[key];
        }
    }
}
