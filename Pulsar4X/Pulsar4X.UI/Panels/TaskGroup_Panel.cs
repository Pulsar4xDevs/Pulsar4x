using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using OpenTK;

/// <summary>
/// Program and MainForm reference UIController
/// Helpers.UIController references SystemMap as member.
/// Handler.SystemMap references Panels.SysMap_Controls and Panels.SysMap_Viewport as members.
/// </summary>

namespace Pulsar4X.UI.Panels
{
    public partial class TaskGroup_Panel : DockContent
    {
        public TaskGroup_Panel()
        {
            InitializeComponent();
        }
    }
}
