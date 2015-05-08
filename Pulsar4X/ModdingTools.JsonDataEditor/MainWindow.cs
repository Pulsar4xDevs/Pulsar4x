using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModdingTools.JsonDataEditor
{
    public partial class MainWindow : Form
    {
        private const int BorderWidth = 20;
        private const int BorderHeight = 40;

        public MainWindow()
        {
            InitializeComponent();
            UserControl techs = new TechnologiesWindow();
            Controls.Add(techs);
            Width = techs.Width + BorderWidth;
            Height = techs.Height + BorderHeight;
        }
    }
}
