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
    public partial class TechRequirementsUC : UserControl
    {
        private BindingList<DataHolder> _allTechs  = new BindingList<DataHolder>();


        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<DataHolder> RequredTechs
        {
            get { return listBox_requredTechs.Items.Cast<DataHolder>().ToList(); }
            set { listBox_requredTechs.DataSource = value; }
        }

        public TechRequirementsUC()
        {
            InitializeComponent();
            UpdateTechlist();          
                 
            Data.TechData.ListChanged += UpdateTechlist;
            listBox_allTechs.DataSource = _allTechs;
        }

        public List<Guid> GetData
        {
            get
            {
                List<Guid> list = new List<Guid>();
                foreach (var item in RequredTechs)
                {
                    list.Add(item.Guid);
                }
                return list;
            }
        }

        private void UpdateTechlist()
        {
            _allTechs = new BindingList<DataHolder>(Data.TechData.GetDataHolders().ToList());
        }

        private void listBox_requredTechs_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            listBox_requredTechs.Items.Remove(listBox_requredTechs.SelectedItem);
        }

        private void listBox_allTechs_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            object selectedItem = listBox_allTechs.SelectedItem;            
            if (selectedItem != null && !listBox_requredTechs.Items.Contains(selectedItem))
                listBox_requredTechs.Items.Add(selectedItem);
        }
    }
}
