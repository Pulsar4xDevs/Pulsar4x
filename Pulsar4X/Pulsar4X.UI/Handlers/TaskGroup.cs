using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;
using Pulsar4X.Stargen;
using log4net.Config;
using log4net;
using Pulsar4X.UI.GLUtilities;
using Pulsar4X.UI.SceenGraph;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Pulsar4X.UI.Handlers
{
    public class TaskGroup
    {
        public enum StratCells
        {
            Name,
            ShipClass,
            Fuel,
            Ammo,
            Shields,
            Thermal,
            MaintSup,
            MaintClock,
            Crew,
            Morale,
            Grade,
            Training,
            Count

        }

        /// <summary>
        /// TG Logger:
        /// </summary>
        public static readonly ILog logger = LogManager.GetLogger(typeof(TaskGroup));

        /// <summary>
        /// Panel for taskgroup related stuff. Opengl shouldn't be used here I don't think, but I'm not sure. Included everything from SystemMap.cs anyway.
        /// </summary>
        Panels.TaskGroup_Panel m_oTaskGroupPanel;

        /// <summary>
        /// Misspelling intentional to keep this in line with systemMap's misspelling.
        /// </summary>
        private Pulsar4X.Entities.TaskGroupTN m_oCurrnetTaskGroup;
        public Pulsar4X.Entities.TaskGroupTN CurrentTaskGroup
        {
            get { return m_oCurrnetTaskGroup; }
            set
            {
                if (value != m_oCurrnetTaskGroup)
                {
                    m_oCurrnetTaskGroup = value;
                    RefreshTGPanel();
                }
            }
        }

        /// <summary>
        /// Current faction
        /// </summary>
        private Pulsar4X.Entities.Faction m_oCurrnetFaction;
        public Pulsar4X.Entities.Faction CurrentFaction
        {
            get { return m_oCurrnetFaction; }
            set
            {
                if (value != m_oCurrnetFaction)
                {
                    m_oCurrnetFaction = value;
                    RefreshTGPanel();
                }
            }

        }

        /// <summary>
        /// The view model this handler uses.
        /// </summary>
        public ViewModels.TaskGroupViewModel VM { get; set; }


        /// <summary>
        /// Constructor for this handler.
        /// </summary>
        public TaskGroup()
        {
            m_oTaskGroupPanel = new Panels.TaskGroup_Panel();

            /// <summary>
            /// setup viewmodel:
            /// Bind TG Selection Combo Box.
            /// </summary>
            VM = new ViewModels.TaskGroupViewModel();


            /// <summary>
            /// Set up the faction bindings. FactionSelectionComboBox is in the TaskGroup_Panel.designer.cs file.
            /// </summary>
            m_oTaskGroupPanel.FactionSelectionComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oTaskGroupPanel.FactionSelectionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oTaskGroupPanel.FactionSelectionComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => CurrentFaction = VM.CurrentFaction;
            CurrentFaction = VM.CurrentFaction;
            m_oTaskGroupPanel.FactionSelectionComboBox.SelectedIndexChanged += (s, args) => m_oTaskGroupPanel.FactionSelectionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oTaskGroupPanel.FactionSelectionComboBox.SelectedIndexChanged += new EventHandler(FactionSelectComboBox_SelectedIndexChanged);


            /// <summary>
            /// Bind the TaskGroup to the appropriate combo box.
            m_oTaskGroupPanel.TaskGroupSelectionComboBox.Bind(c => c.DataSource, VM, d => d.TaskGroups);
            m_oTaskGroupPanel.TaskGroupSelectionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentTaskGroup, DataSourceUpdateMode.OnPropertyChanged);
            m_oTaskGroupPanel.TaskGroupSelectionComboBox.DisplayMember = "Name";
            VM.TaskGroupChanged += (s, args) => CurrentTaskGroup = VM.CurrentTaskGroup;
            CurrentTaskGroup = VM.CurrentTaskGroup;
            m_oTaskGroupPanel.TaskGroupSelectionComboBox.SelectedIndexChanged += (s, args) => m_oTaskGroupPanel.TaskGroupSelectionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oTaskGroupPanel.TaskGroupSelectionComboBox.SelectedIndexChanged += new EventHandler(TaskGroupSelectComboBox_SelectedIndexChanged);

            /// <summary>
            /// Setup TG Data Grid:
            /// </summary>
            m_oTaskGroupPanel.TaskGroupDataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            m_oTaskGroupPanel.TaskGroupDataGrid.RowHeadersVisible = false;
            m_oTaskGroupPanel.TaskGroupDataGrid.AutoGenerateColumns = false;
            SetupShipDataGrid();
            RefreshShipCells();
        }

        /// <summary>
        /// Handle Faction Changes here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FactionSelectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshShipCells();
        }

        /// <summary>
        /// Handle TaskGroup Changes here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskGroupSelectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshShipCells();
        }

        /// <summary>
        /// Shows all the System Map Panels.
        /// </summary>
        /// <param name="a_oDockPanel"> The target Docking Panel. </param>
        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            ShowViewPortPanel(a_oDockPanel);
        }

        /// <summary>
        /// Shows the View Port Panel.
        /// </summary>
        /// <param name="a_oDockPanel"> The target Docking Panel. </param>
        public void ShowViewPortPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oTaskGroupPanel.Show(a_oDockPanel, DockState.Document);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }


        #region PrivateMethods

        /// <summary>
        /// Creates the ship information section. There is also a tactical data condition where this info is swapped out with other info, that is not yet handled.
        /// </summary>
        private void SetupShipDataGrid()
        {
            try
            {
                /// <summary>
                /// Add columns:
                /// </summary>
                Padding newPadding = new Padding(2, 0, 2, 0);
                AddColumn("Ship Name", newPadding);
                AddColumn("Class", newPadding);
                AddColumn("Fuel", newPadding);
                AddColumn("Ammo", newPadding);
                AddColumn("Shields", newPadding);
                AddColumn("Thermal Sig.", newPadding);
                AddColumn("Maint Supplies", newPadding);
                AddColumn("Maint Clock", newPadding);
                AddColumn("Crew (mths)", newPadding);
                AddColumn("Morale", newPadding);
                AddColumn("Grade Bonus", newPadding);
                AddColumn("TF Training", newPadding);
            }
            catch
            {
                logger.Error("Something whent wrong Creating Columns for Taskgroup summary screen...");
            }
        }

        private void RefreshShipCells()
        {
            try
            {
                m_oTaskGroupPanel.TaskGroupLocationTextBox.Text = CurrentTaskGroup.Contact.CurrentSystem.Name;
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows.Clear();
                /// <summary>
                /// Add Rows:
                /// </summary>
                for (int loop = 0; loop < m_oCurrnetTaskGroup.Ships.Count; loop++)
                {
                    using (DataGridViewRow row = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        row.Height = 18;
                        m_oTaskGroupPanel.TaskGroupDataGrid.Rows.Add(row);

                        PopulateRow(loop);
                    }
                }
            }
            catch
            {
                logger.Error("Something whent wrong Creating Columns for Taskgroup summary screen...");
            }
        }

        /// <summary>
        /// Just a space saver here to avoid copy pasting a lot.
        /// </summary>
        /// <param name="Header">Text of column header.</param>
        /// <param name="newPadding">Padding in use, not sure what this is or why its necessary. Cargo culting it is.</param>
        private void AddColumn(String Header, Padding newPadding)
        {
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = Header;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.DefaultCellStyle.Padding = newPadding;
                if (col != null)
                {
                    m_oTaskGroupPanel.TaskGroupDataGrid.Columns.Add(col);
                }
            }
        }

        /// <summary>
        /// Fills in the rows of the display.
        /// </summary>
        /// <param name="row">ship index and also row.</param>
        private void PopulateRow(int row)
        {
            try
            {
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Name].Value = m_oCurrnetTaskGroup.Ships[row].Name;
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.ShipClass].Value = m_oCurrnetTaskGroup.Ships[row].ShipClass.Name;

                float fuelPercent = (m_oCurrnetTaskGroup.Ships[row].CurrentFuel / m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalFuelCapacity) * 100.0f;
                fuelPercent = (float)Math.Floor(fuelPercent);
            
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Fuel].Value = fuelPercent.ToString() + "%";

                String Ammo;
                if (m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalMagazineCapacity == 0)
                {
                    Ammo = "N/A";
                }
                else
                {
                    float AmmoPercent = (m_oCurrnetTaskGroup.Ships[row].CurrentMagazineCapacity / m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalMagazineCapacity) * 100.0f;
                    Ammo = AmmoPercent.ToString();
                }
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Ammo].Value = Ammo;

                String Shield;
                if (m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalShieldPool == 0.0f)
                {
                    Shield = "N/A";
                }
                else
                {
                    Shield = m_oCurrnetTaskGroup.Ships[row].CurrentShieldPool.ToString() + "/" + m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalShieldPool.ToString();
                }
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Shields].Value = Shield;

                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Thermal].Value = m_oCurrnetTaskGroup.Ships[row].CurrentThermalSignature.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.MaintSup].Value = m_oCurrnetTaskGroup.Ships[row].CurrentMSP.ToString() + "/" + m_oCurrnetTaskGroup.Ships[row].CurrentMSPCapacity.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.MaintClock].Value = m_oCurrnetTaskGroup.Ships[row].MaintenanceClock.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Crew].Value = m_oCurrnetTaskGroup.Ships[row].CurrentDeployment.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Morale].Value = m_oCurrnetTaskGroup.Ships[row].Morale.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Grade].Value = m_oCurrnetTaskGroup.Ships[row].ShipGrade.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Training].Value = m_oCurrnetTaskGroup.Ships[row].TFTraining.ToString();
            }
            catch
            {
                logger.Error("Something whent wrong Refreshing Cells for Taskgroup Ship summary screen...");
            }
        }

        /// <summary>
        /// Refresh the TG page.
        /// </summary>
        private void RefreshTGPanel()
        {
            RefreshShipCells();
        }
        #endregion
    }
}
