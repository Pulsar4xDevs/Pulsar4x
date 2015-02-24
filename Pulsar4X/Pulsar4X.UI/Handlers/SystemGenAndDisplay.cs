using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;
using Newtonsoft.Json;

namespace Pulsar4X.UI.Handlers
{
    public class SystemGenAndDisplay
    {
        /// <summary>
        /// Panel that contains the Star and SystemBody Data grids.
        /// </summary>
        Panels.SGaD_DataPanel m_oDataPanel;

        /// <summary>
        /// Panel Containing all the f9 screen controls:
        /// </summary>
        Panels.SGaD_Controls m_oControlsPanel;

        public StarSystemViewModel VM { get; set; }

        private int m_iNumberOfNewSystemsGened = 0;

        public SystemGenAndDisplay()
        {
            // Create Panels:
            m_oDataPanel = new Panels.SGaD_DataPanel();
            m_oControlsPanel = new Panels.SGaD_Controls();

            // setup view model:
            VM = new StarSystemViewModel();

            // bind System Selection combo box:
            m_oControlsPanel.SystemSelectionComboBox.DataSource = VM.StarSystems;
            m_oControlsPanel.SystemSelectionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentStarSystem, DataSourceUpdateMode.OnPropertyChanged);
            m_oControlsPanel.SystemSelectionComboBox.DisplayMember = "Name";

            m_oControlsPanel.SystemSelectionComboBox.SelectedIndexChanged += (s, args) => m_oControlsPanel.SystemSelectionComboBox.DataBindings["SelectedItem"].WriteValue();

            // bind text boxes:
            m_oControlsPanel.AgeTextBox.Bind(c => c.Text, VM, d => d.CurrentStarSystemAge);
            m_oControlsPanel.SeedTextBox.Bind(c => c.Text, VM, d => d.Seed);

            // Setup the stars Grid
            m_oDataPanel.StarDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            m_oDataPanel.StarDataGrid.RowHeadersVisible = false;
            m_oDataPanel.StarDataGrid.AutoGenerateColumns = false;
            m_oDataPanel.StarDataGrid.Bind(c => c.AllowUserToAddRows, VM, d => d.isSM);
            m_oDataPanel.StarDataGrid.Bind(c => c.AllowUserToDeleteRows, VM, d => d.isSM);
            m_oDataPanel.StarDataGrid.Bind(c => c.ReadOnly, VM, d => d.isNotSM);

            AddColumnsToStarDataGrid();

            m_oDataPanel.StarDataGrid.DataSource = VM.StarsSource;
            m_oDataPanel.StarDataGrid.SelectionChanged += new EventHandler(StarsDataGrid_SelectionChanged);

            // Setup the SystemBody Data Grid
            m_oDataPanel.PlanetsDataGrid.AutoGenerateColumns = false;
            m_oDataPanel.PlanetsDataGrid.RowHeadersVisible = false;
            m_oDataPanel.PlanetsDataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            m_oDataPanel.PlanetsDataGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            m_oDataPanel.PlanetsDataGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            m_oDataPanel.PlanetsDataGrid.Bind(c => c.AllowUserToAddRows, VM, d => d.isSM);
            m_oDataPanel.PlanetsDataGrid.Bind(c => c.AllowUserToDeleteRows, VM, d => d.isSM);
            m_oDataPanel.PlanetsDataGrid.Bind(c => c.ReadOnly, VM, d => d.isNotSM);
            AddColumnsToPlanetDataGrid();

            m_oDataPanel.PlanetsDataGrid.DataSource = VM.PlanetSource;
            m_oDataPanel.PlanetsDataGrid.SelectionChanged += new EventHandler(PlanetsDataGrid_SelectionChanged);
            m_oDataPanel.PlanetsDataGrid.CellDoubleClick += new DataGridViewCellEventHandler(PlanetDataGrid_CellDoubleClick);

            // Setup Event handlers for Controls panel buttons:
            m_oControlsPanel.GenSystemButton.Click += new EventHandler(GenSystemButton_Click);
            m_oControlsPanel.GenGalaxyButton.Click += new EventHandler(GenGalaxyButton_Click);
            m_oControlsPanel.DeleteSystemButton.Click += new EventHandler(DeleteSystemButton_Click);
            m_oControlsPanel.AutoRenameButton.Click += new EventHandler(AutoRenameButton_Click);
            m_oControlsPanel.AddColonyButton.Click += new EventHandler(AddColonyButton_Click);
            m_oControlsPanel.ExportButton.Click += new EventHandler(ExportButton_Click);
        }

        #region EventHandlers

        void ExportButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog oSaveFileDialog = new SaveFileDialog();
            oSaveFileDialog.DefaultExt = "json";
            oSaveFileDialog.Filter = "JSON Files (*.json)|*.json";
            oSaveFileDialog.Title = "Export Systems";

            oSaveFileDialog.ShowDialog();

            var serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            serializer.NullValueHandling = NullValueHandling.Include;
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.All;

            using (StreamWriter sw = new StreamWriter(oSaveFileDialog.FileName))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, VM.StarSystems);
            }
        }

        void AddColonyButton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void AutoRenameButton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void DeleteSystemButton_Click(object sender, EventArgs e)
        {
            ///< @todo Need better cleanup /refresh...
            GameState.Instance.StarSystems.Remove(VM.CurrentStarSystem);
        }

        void GenGalaxyButton_Click(object sender, EventArgs e)
        {
            Dialogs.GenGalaxyDialog diagGenGalaxy = new Dialogs.GenGalaxyDialog();
            diagGenGalaxy.ShowDialog();
        }

        void GenSystemButton_Click(object sender, EventArgs e)
        {
            Dialogs.InputBasic InpuDialog = new Dialogs.InputBasic("Enter Seed", "Enter System Seed", "-1");
            InpuDialog.ShowDialog();
            if (InpuDialog.DialogResult == DialogResult.OK)
            {
                m_iNumberOfNewSystemsGened++;
                SystemGen.CreateSystem("Gened System " + m_iNumberOfNewSystemsGened.ToString(), InpuDialog.InputInt);
            }
            // cleanup input box:
            InpuDialog.Close();
        }

        void StarsDataGrid_SelectionChanged(object sender, EventArgs e)
        {
            var sel = m_oDataPanel.StarDataGrid.SelectedRows;
            if (sel.Count > 0)
            {
                VM.CurrentStar = (Star)sel[0].DataBoundItem;
                m_oDataPanel.PlanetsDataGrid.DataSource = VM.PlanetSource;
            }
        }

        void PlanetsDataGrid_SelectionChanged(object sender, EventArgs e)
        {
            var sel = m_oDataPanel.PlanetsDataGrid.SelectedRows;
            if (sel.Count > 0)
            {
                VM.CurrentPlanet = (SystemBody)sel[0].DataBoundItem;
            }
        }

        private void PlanetDataGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 6)
            {
                // double clicked on sma, check if inside habitable zone.
                double sma = (double)m_oDataPanel.PlanetsDataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                if (sma < VM.CurrentStar.MaxHabitableRadius && sma > VM.CurrentStar.MinHabitableRadius)
                {
                    // we are in the habitable zone:
                    m_oDataPanel.PlanetsDataGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.Aqua;
                }
                else
                {
                    m_oDataPanel.PlanetsDataGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.LightSalmon;
                }
            }
        }


        #endregion

        #region PublicMethods

        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            ShowDataPanel(a_oDockPanel);
            ShowControlsPanel(a_oDockPanel);
        }

        public void ShowDataPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oDataPanel.Show(a_oDockPanel, DockState.Document);
            m_oDataPanel.SetSplitterDistance(110);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ActivateDataPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oDataPanel.Activate();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ShowControlsPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oControlsPanel.Show(a_oDockPanel, DockState.DockLeft);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ActivateControlsPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oControlsPanel.Activate();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void SMOn()
        {
            m_oControlsPanel.OnSMEnable();
        }

        public void SMOff()
        {
            m_oControlsPanel.OnSMDisable();
        }

        #endregion

        #region PrivateMethods

        private void AddColumnsToStarDataGrid()
        {
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Name";
                col.HeaderText = "Name";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Class";
                col.HeaderText = "Class";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "RadiusinKM";
                col.HeaderText = "Radius";
                col.DefaultCellStyle.Format = "N0";
                col.ToolTipText = "Radius in Km";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Orbit_MassRelativeToSol";
                col.HeaderText = "Mass";
                col.DefaultCellStyle.Format = "N2";
                col.ToolTipText = "Mass in Solar Masses";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Luminosity";
                col.HeaderText = "Luminosity";
                col.DefaultCellStyle.Format = "N2";
                col.ToolTipText = "Luminosity in Solar Luminosity";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Temperature";
                col.HeaderText = "Temperature";
                col.DefaultCellStyle.Format = "N0";
                col.ToolTipText = "Mass in Degrees C";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "EcoSphereRadius";
                col.HeaderText = "Habitable Zone";
                col.DefaultCellStyle.Format = "N2";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Orbit_SemiMajorAxis";
                col.HeaderText = "Orbital Dist";
                col.DefaultCellStyle.Format = "N2";
                col.ToolTipText = "Semi Major Axis in AU";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
        }

        private void AddColumnsToPlanetDataGrid()
        {
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Name";
                col.HeaderText = "Name";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Type";
                col.HeaderText = "Type";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "ColonyCost";
                col.HeaderText = "Colony Cost";
                col.DefaultCellStyle.Format = "N2";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Atmosphere_SurfaceTemperature";
                col.HeaderText = "Temperature";
                col.DefaultCellStyle.Format = "N1";
                col.ToolTipText = "Average Surface Temperature in Degrees C";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "SurfaceGravity";
                col.HeaderText = "Gravity";
                col.DefaultCellStyle.Format = "N2";
                col.ToolTipText = "Gravity in m/s^2";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Atmosphere_InPercent";
                col.HeaderText = "Atmosphere Composition";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Orbit_SemiMajorAxis";
                col.HeaderText = "Orbit Dist (AU)";
                col.DefaultCellStyle.Format = "N4";
                col.ToolTipText = "Semi Major Axis in AU";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Atmosphere_Pressure";
                col.HeaderText = "Pressure";
                col.DefaultCellStyle.Format = "N2";
                col.ToolTipText = "Atmospheric Pressure in Earth Atmospheres (atm) at the Surface";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "RadiusinKM";
                col.HeaderText = "Radius";
                col.DefaultCellStyle.Format = "N0";
                col.ToolTipText = "Average Radius in Km";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Atmosphere_HydrosphereExtent";
                col.HeaderText = "Hydrosphere Extent";
                col.DefaultCellStyle.Format = "N1";
                col.ToolTipText = "Hydrosphere Cover - Liquid (%)";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Orbit_OrbitalPeriod";
                col.HeaderText = "Year";
                col.DefaultCellStyle.Format = "g";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "AxialTilt";
                col.HeaderText = "Axial Tilt";
                col.DefaultCellStyle.Format = "N0";
                col.ToolTipText = "Axial Tilt in degrees";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Orbit_MassRelativeToEarth";
                col.HeaderText = "Mass";
                col.DefaultCellStyle.Format = "N6";
                col.ToolTipText = "Mass relative to the Earth";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Density";
                col.HeaderText = "Density";
                col.DefaultCellStyle.Format = "N2";
                col.ToolTipText = "Density in g/cm^2";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Atmosphere_Albedo";
                col.HeaderText = "Albedo";
                col.DefaultCellStyle.Format = "P0";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Tectonics";
                col.HeaderText = "Tectonics";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
        }

        #endregion

    }
}
