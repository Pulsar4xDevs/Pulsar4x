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
        TechWindow,
        InstallationsWindow,
        ComponentsWindow
    }
    public partial class MainWindow : Form
    {
        private LoadWindow _loadWindow;
        private TechnologiesWindow _technologiesWindow;
        private InstallationsWindow _installationsWindow;
        private ComponentsWindow _componentsWindow;

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
            switch (mode)
            {
                case WindowModes.LoadingWindow:
                    if (_loadWindow == null)
                        _loadWindow = new LoadWindow();
                    Width = _loadWindow.Width + BorderWidth;
                    Height = _loadWindow.Height + BorderHeight;
                    Controls.Add(_loadWindow);
                    break;
                case WindowModes.TechWindow:
                    if(_technologiesWindow == null)
                        _technologiesWindow = new TechnologiesWindow();
                    Width = _technologiesWindow.Width + BorderWidth;
                    Height = _technologiesWindow.Height + BorderHeight;
                    Controls.Add(_technologiesWindow);
                    break;
                case WindowModes.InstallationsWindow:
                    if (_installationsWindow == null)
                        _installationsWindow = new InstallationsWindow();
                    Width = _installationsWindow.Width + BorderWidth;
                    Height = _installationsWindow.Height + BorderHeight;
                    Controls.Add(_installationsWindow);
                    break;
                case WindowModes.ComponentsWindow:
                    if (_componentsWindow == null)
                        _componentsWindow = new ComponentsWindow();
                    Width = _componentsWindow.Width + BorderWidth;
                    Height = _componentsWindow.Height + BorderHeight;
                    _componentsWindow.Dock = DockStyle.Fill;
                    Controls.Add(_componentsWindow);
                    break;
            }
        }
    }
}
