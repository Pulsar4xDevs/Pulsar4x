using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor
{
    public partial class AbilitiesListUC : UserControl
    {
        private Dictionary<AbilityType, int> _abilityAmounts = new Dictionary<AbilityType, int>();

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<AbilityType,int> AbilityAmount
        {
            get { return _abilityAmounts; }
            set
            {
                _abilityAmounts = value;
                if (_abilityAmounts != null)
                    UpdateAbilityAmounts();
            }
        }
        public AbilitiesListUC()
        {
            InitializeComponent();
                        
            listBox_allAbilities.DataSource = Enum.GetValues(typeof(AbilityType)).Cast<AbilityType>();

            dataGridView_addedAbilities.Columns.Add("Key", "Ability Type");
            dataGridView_addedAbilities.Columns.Add("Values", "Amount");
            UpdateAbilityAmounts();
        }

        private void UpdateAbilityAmounts()
        {
            dataGridView_addedAbilities.DataSource = null;
            dataGridView_addedAbilities.Rows.Clear();

            foreach (KeyValuePair<AbilityType, int> item in _abilityAmounts)
            {
                dataGridView_addedAbilities.Rows.Add(item.Key, item.Value);
            }
        }

        public JDictionary<AbilityType, int> GetData {get { return new JDictionary<AbilityType, int>(_abilityAmounts); } }

        private void listBox_allAbilities_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!_abilityAmounts.ContainsKey((AbilityType)listBox_allAbilities.SelectedItem))
            {
                _abilityAmounts.Add((AbilityType)listBox_allAbilities.SelectedItem, 0);         
            }
            UpdateAbilityAmounts();
        }

        private void dataGridView_addedAbilities_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                dataGridView_addedAbilities.CurrentCell.ReadOnly = false;
                dataGridView_addedAbilities.BeginEdit(true);
            }
        }

        private void dataGridView_addedAbilities_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            AbilityType key = (AbilityType)dataGridView_addedAbilities.Rows[e.RowIndex].Cells[0].Value;
            DataGridViewCell cell = dataGridView_addedAbilities.Rows[e.RowIndex].Cells[1];
            object cellValue = dataGridView_addedAbilities.Rows[e.RowIndex].Cells[1].Value;
            if (cellValue is int)
            {
                _abilityAmounts[key] = (int)cellValue;
            }
            else if (cellValue is string)
            {
                int amount;
                if (Int32.TryParse((string)cell.Value, out amount))
                    _abilityAmounts[key] = amount;
            }
            else
                cell.Value = _abilityAmounts[key];
        }
    }
}
