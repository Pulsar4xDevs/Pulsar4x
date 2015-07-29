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
        private DataHolder _item;
        
        /// <summary>
        /// consider handling this differently. maybe Dataholder
        /// should hold a description? except not all static data has it. 
        /// </summary>
        public string Description 
        {
            get { return textBox_Description.Text;}
            set { textBox_Description.Text = value; }
        }
        public GenericDataUC()
        {
            InitializeComponent();           
        }

        public Guid GetGuid {
            get { return _item.Guid; }
        }
        public string GetName
        {
            get { return textBox_Name.Text; }
        }
        public string GetDescription
        {
            get { return textBox_Description.Text; }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataHolder Item
        {
            get{return _item;}
            set
            {
                _item = value;
                label_Guid.Text = _item.Guid.ToString();
                textBox_Name.Text = _item.Name;
                //textBox_Description.Text = _Item.?????????
            }
        }

        private void textBox_Name_TextChanged(object sender, EventArgs e)
        {
   
        }

    }
}
