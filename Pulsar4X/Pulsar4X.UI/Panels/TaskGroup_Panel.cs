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
        /// <summary>
        /// Display for Ship information.
        /// </summary>
        private DataGridView m_oTaskGroupDataGrid;
        public DataGridView TaskGroupDataGrid
        {
            get { return m_oTaskGroupDataGrid; }
        }

        public TaskGroup_Panel()
        {
            InitializeComponent();

            this.HideOnClose = true;
            this.Text = "Task Groups";
            this.TabText = "Task Groups";
            this.ToolTipText = "Task group order, information, organization";

            m_oTaskGroupDataGrid = new DataGridView();
            m_oTaskGroupDataGrid.Dock = DockStyle.Fill;
            m_oTaskGroupDataGrid.AllowUserToAddRows = false;
            m_oTaskGroupDataGrid.AllowUserToDeleteRows = false;
            m_oTaskGroupDataGrid.AllowUserToOrderColumns = false;
            m_oTaskGroupDataGrid.AllowUserToResizeColumns = false;
            m_oTaskGroupDataGrid.AllowUserToResizeRows = false;
            m_oTaskGroupDataGrid.ReadOnly = true;
            m_oTaskGroupDataGrid.Enabled = true;
            this.m_oShipsBox.Controls.Add(m_oTaskGroupDataGrid);
        }
    }
}
