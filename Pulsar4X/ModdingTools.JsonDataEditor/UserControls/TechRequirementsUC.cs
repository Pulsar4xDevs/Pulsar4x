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
        private BindingList<DataHolder> AllTechs { get; set; }

        public TechRequirementsUC()
        {
            InitializeComponent();
            UpdateTechlist();          
            //listBox_allTechs.DataSource = Data.TechData.GetDataHolders().ToList();
            Data.TechData.ListChanged += UpdateTechlist;
            listBox_allTechs.DataSource = AllTechs;
        }

        private void UpdateTechlist()
        {
            AllTechs = new BindingList<DataHolder>(Data.TechData.GetDataHolders().ToList());
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
