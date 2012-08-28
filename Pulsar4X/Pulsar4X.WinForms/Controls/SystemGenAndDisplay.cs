using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.ViewModels;

namespace Pulsar4X.WinForms.Controls
{
    public partial class SystemGenAndDisplay : UserControl
    {
        public StarSystemViewModel VM { get; set; }
        public SystemGenAndDisplay()
        {
            InitializeComponent();

            VM = new StarSystemViewModel();

            NameListBox.DataSource = new BindingSource(VM.StarSystems, null);
            NameListBox.DataBindings.Add(new Binding("SelectedItem", VM.CurrentStarSystem, null));
            NameListBox.DisplayMember = "Name";

            AgetextBox.DataBindings.Add(new Binding("Text", VM.CurrentStarSystem.Stars[0].Age, null));

            StarsDataGridView.DataSource = VM.CurrentStarSystem.Stars;

            StarADataGridView.DataSource = new BindingSource(VM.CurrentStar.Planets, null);
        }

    }
}
