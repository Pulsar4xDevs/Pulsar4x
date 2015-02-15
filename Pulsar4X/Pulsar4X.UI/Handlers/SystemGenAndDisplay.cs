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
        /// Panel that contains the Star and Planet Data grids.
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

            // Setup the Planet Data Grid
            m_oDataPanel.PlanetsDataGrid.AutoGenerateColumns = false;
            m_oDataPanel.PlanetsDataGrid.RowHeadersVisible = false;
            m_oDataPanel.PlanetsDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            m_oDataPanel.PlanetsDataGrid.Bind(c => c.AllowUserToAddRows, VM, d => d.isSM);
            m_oDataPanel.PlanetsDataGrid.Bind(c => c.AllowUserToDeleteRows, VM, d => d.isSM);
            m_oDataPanel.PlanetsDataGrid.Bind(c => c.ReadOnly, VM, d => d.isNotSM);
            AddColumnsToPlanetDataGrid();

            m_oDataPanel.PlanetsDataGrid.DataSource = VM.PlanetSource;
            m_oDataPanel.PlanetsDataGrid.SelectionChanged += new EventHandler(PlanetsDataGrid_SelectionChanged);

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
                VM.CurrentPlanet = (Planet)sel[0].DataBoundItem;
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
                col.DataPropertyName = "Radius";
                col.HeaderText = "Radius";
                col.DefaultCellStyle.Format = "N4";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Mass";
                col.HeaderText = "Mass";
                col.DefaultCellStyle.Format = "N4";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Luminosity";
                col.HeaderText = "Luminosity";
                col.DefaultCellStyle.Format = "N4";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Temperature";
                col.HeaderText = "Temperature";
                col.DefaultCellStyle.Format = "N4";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "EcoSphereRadius";
                col.HeaderText = "Habitable Zone";
                col.DefaultCellStyle.Format = "N4";
                m_oDataPanel.StarDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "SemiMajorAxis";
                col.HeaderText = "Orbital Radius (AU)";
                col.DefaultCellStyle.Format = "N4";
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
                col.DataPropertyName = "PlanetTypeView";
                col.HeaderText = "Type";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "SurfaceTemperatureView";
                col.HeaderText = "Surface Temperature (K)";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "SurfaceGravityView";
                col.HeaderText = "Surface Gravity";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "MassOfGasInEarthMassesView";
                col.HeaderText = "Atmosphere (Earth Masses)";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "SemiMajorAxis";
                col.HeaderText = "Orbit Dist (AU)";
                col.DefaultCellStyle.Format = "N4";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "SurfacePressureView";
                col.HeaderText = "Surface Pressure (mb)";
                col.DefaultCellStyle.Format = "N4";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Radius";
                col.HeaderText = "Equitorial Radius (Km)";
                col.DefaultCellStyle.Format = "N1";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "HydrosphereCoverInPercent";
                col.HeaderText = "Hydrosphere Cover - Liquid (%)";
                col.DefaultCellStyle.Format = "N1";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "IceCoverInPercent";
                col.HeaderText = "Hydrosphere Cover - Solid (%)";
                col.DefaultCellStyle.Format = "N1";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "CloudCoverInPercent";
                col.HeaderText = "Cloud Cover (%)";
                col.DefaultCellStyle.Format = "N1";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "OrbitalPeriod";
                col.HeaderText = "Year (Earth Days)";
                col.DefaultCellStyle.Format = "N2";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "AxialTilt";
                col.HeaderText = "Axial Tilt";
                col.DefaultCellStyle.Format = "N0";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "MassInEarthMassesView";
                col.HeaderText = "Mass (Earth Masses)";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Density";
                col.HeaderText = "Density (g/cc)";
                col.DefaultCellStyle.Format = "N4";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "EscapeVelocity";
                col.HeaderText = "Escape Velocity (cm/s)";
                col.DefaultCellStyle.Format = "N2";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "IsInResonantRotation";
                col.HeaderText = "Tidal Lock";
                //col.DefaultCellStyle.Format .Format = "N4";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Albedo";
                col.HeaderText = "Albedo";
                col.DefaultCellStyle.Format = "N4";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                m_oDataPanel.PlanetsDataGrid.Columns.Add(col);
            }
        }

        #endregion

    }
}
