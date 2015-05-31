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
    public partial class AbilitiesListUC : UserControl
    {
        private Dictionary<AbilityType, int> AbilityAmounts { get; set; } 
        public AbilitiesListUC()
        {
            InitializeComponent();
            AbilityAmounts = new Dictionary<AbilityType, int>();
            dataGridView_addedAbilities.DataSource = AbilityAmounts.ToArray();
            listBox_allAbilities.DataSource = Enum.GetValues(typeof(AbilityType)).Cast<AbilityType>();           
        }

        private void listBox_allAbilities_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!AbilityAmounts.ContainsKey((AbilityType)listBox_allAbilities.SelectedItem))
            {
                AbilityAmounts.Add((AbilityType)listBox_allAbilities.SelectedItem, 0);         
            }
            dataGridView_addedAbilities.DataSource = AbilityAmounts.ToArray();
        }

        private void dataGridView_addedAbilities_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                dataGridView_addedAbilities.CurrentCell.ReadOnly = false;
                dataGridView_addedAbilities.BeginEdit(true);
            }
        }
    }
}
