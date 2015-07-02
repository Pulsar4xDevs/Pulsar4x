using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Animation;

namespace ModdingTools.JsonDataEditor
{
    public partial class TechRequirementsUC : UserControl
    {
        private BindingList<DataHolder> _allTechs  = new BindingList<DataHolder>();
        private BindingList<DataHolder> _requredTechs = new BindingList<DataHolder>();
            
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<DataHolder> RequredTechs
        {
            get { return _requredTechs.ToList(); }
            set
            {
                _requredTechs = new BindingList<DataHolder>(value);
                listBox_requredTechs.DataSource = _requredTechs;
            }
        }

        public bool AllowDuplicates { get; set; }

        public TechRequirementsUC()
        {
            InitializeComponent();
            UpdateTechlist();

            AllowDuplicates = false;
            //Data.TechData.ListChanged += UpdateTechlist;
            listBox_allTechs.DataSource = _allTechs;
            listBox_requredTechs.DataSource = _requredTechs;
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
            _allTechs = new BindingList<DataHolder>(Data.TechData.Values.ToList());
            listBox_allTechs.DataSource = _allTechs;
        }

        private void listBox_requredTechs_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            listBox_requredTechs.Items.Remove(_requredTechs.Remove((DataHolder)listBox_requredTechs.SelectedItem));
        }

        private void listBox_allTechs_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            object selectedItem = listBox_allTechs.SelectedItem;            
            if (selectedItem != null)
                if (!AllowDuplicates && listBox_requredTechs.Items.Contains(selectedItem))
                {
                    //do nothing.
                }
                else
                {
                    _requredTechs.Add((DataHolder)selectedItem);
                }
           
        }
    }
}
