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
    public enum WindowModes
    {
        LoadingWindow,
        TechWindow
    }
    public partial class MainWindow : Form
    {
        private LoadWindow _loadWindow;
        private TechnologiesWindow _technologiesWindow;

        private const int BorderWidth = 20;
        private const int BorderHeight = 40;

        public MainWindow()
        {
            Data.MainWindow = this;
            InitializeComponent();
            SetMode(WindowModes.LoadingWindow);
        }

        public void SetMode(WindowModes mode)
        {
            Controls.Clear();
            if(mode == WindowModes.LoadingWindow)
            {
                if (_loadWindow == null)
                    _loadWindow = new LoadWindow();
                Width = _loadWindow.Width + BorderWidth;
                Height = _loadWindow.Height + BorderHeight;
                Controls.Add(_loadWindow);
            }
            else if(mode == WindowModes.TechWindow)
            {
                if(_technologiesWindow == null)
                    _technologiesWindow = new TechnologiesWindow();
                Width = _technologiesWindow.Width + BorderWidth;
                Height = _technologiesWindow.Height + BorderHeight;
                Controls.Add(_technologiesWindow);
            }
        }
    }
}
