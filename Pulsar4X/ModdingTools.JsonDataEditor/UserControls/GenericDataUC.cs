using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModdingTools.JsonDataEditor
{
    public partial class GenericDataUC : UserControl
    {
        private DataHolder _Item { get; set; }

        public GenericDataUC()
        {
            InitializeComponent();           
        }

        public void Item(DataHolder item)
        {
            _Item = item;
            label_Guid.Text = _Item.Guid.ToString();
            textBox_Name.Text = _Item.Name;
            //textBox_Description.Text = _Item.?????????

        }

    }
}
