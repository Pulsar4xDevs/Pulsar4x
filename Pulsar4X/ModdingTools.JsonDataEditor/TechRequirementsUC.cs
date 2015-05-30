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
        public BindingList<DataHolder> AllTechs { get; set; }

        public TechRequirementsUC()
        {
            InitializeComponent();
            updateTechlist();          
            //listBox_allTechs.DataSource = Data.TechData.GetDataHolders().ToList();
            Data.TechData.ListChanged += updateTechlist;
            listBox_allTechs.DataSource = AllTechs;
        }

        private void updateTechlist()
        {
            AllTechs = new BindingList<DataHolder>(Data.TechData.GetDataHolders().ToList());
        }
    }
}
