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

/// <summary>
/// To Do: when multi faction system mapping is done, be sure to update waypoint creation further down.
/// Advance time,system select box, and refreshStarsystem() handle taskgroup dot creation and update. To Do: add past position line. Change from primitive to circle.
/// </summary>

namespace Pulsar4X.UI.Handlers
{
    public class SystemMap
    {
        #region Properties and Member Vars

        // System Map Logger:
        public static readonly ILog logger = LogManager.GetLogger(typeof(SystemMap));

        /// <summary>
        /// Panel that contains the System map view port (i.e. openGL canvas).
        /// </summary>
        Panels.SysMap_ViewPort m_oViewPortPanel;

        /// <summary>
        /// Panel that contains the System Map Controls.
        /// </summary>
        Panels.SysMap_Controls m_oControlsPanel;

        /// <summary>
        /// The GL Canvas, created here and inserted into the viewport panel.
        /// </summary>
        GLCanvas m_oGLCanvas;
        public GLCanvas oGLCanvas
        {
            get { return m_oGLCanvas; }
        }

        /// <summary>
        /// Keeps tract of the start location when calculation Panning.
        /// </summary>
        Vector3 m_v3PanStartLocation;

        /// <summary>
        /// Keeps tract of the start location when calculation a measurement.
        /// </summary>
        Vector3 m_v3MeasurementStartLocation;

        /// <summary> The currnet star system </summary>
        private Pulsar4X.Entities.StarSystem m_oCurrnetSystem;

        /// <summary>
        /// Gets or sets the currently viewed star system.
        /// </summary>
        public Entities.StarSystem CurrentStarSystem
        {
            get
            {
                return m_oCurrnetSystem;
            }
            set
            {
                if (value != m_oCurrnetSystem)
                {
                    m_oCurrnetSystem = value;
                    RefreshStarSystem();
                }
            }
        }

        // List of sceens created for display.
        private List<Sceen> m_lSystemSceens = new List<Sceen>();
        public List<Sceen> SystemSceens
        {
            get { return m_lSystemSceens; }
        }

        private Sceen m_oCurrentSceen;

        public GLStarSystemViewModel VM { get; set; }

        private bool m_bCreateMapMarkerOnNextClick = false;

        /// <summary>
        /// And here is how I'll be passing the DateTimeModifier correctly.
        /// </summary>
        public Pulsar4X.UI.Forms.MainForm MainFormReference { get; set; }

        #endregion

        public SystemMap()
        {
            m_oViewPortPanel = new Panels.SysMap_ViewPort();
            m_oControlsPanel = new Panels.SysMap_Controls();

            /// <summary>
            /// Add time button handlers to ViewPortPanel.
            /// </summary>
            m_oViewPortPanel.AdvanceTime5S.Click += new EventHandler(AdvanceTime5S_Click);
            m_oViewPortPanel.AdvanceTime30S.Click += new EventHandler(AdvanceTime30S_Click);
            m_oViewPortPanel.AdvanceTime2M.Click += new EventHandler(AdvanceTime2M_Click);
            m_oViewPortPanel.AdvanceTime5M.Click += new EventHandler(AdvanceTime5M_Click);
            m_oViewPortPanel.AdvanceTime20M.Click += new EventHandler(AdvanceTime20M_Click);
            m_oViewPortPanel.AdvanceTime1H.Click += new EventHandler(AdvanceTime1H_Click);
            m_oViewPortPanel.AdvanceTime3H.Click += new EventHandler(AdvanceTime3H_Click);
            m_oViewPortPanel.AdvanceTime8H.Click += new EventHandler(AdvanceTime8H_Click);
            m_oViewPortPanel.AdvanceTime1D.Click += new EventHandler(AdvanceTime1D_Click);
            m_oViewPortPanel.AdvanceTime5D.Click += new EventHandler(AdvanceTime5D_Click);
            m_oViewPortPanel.AdvanceTime30D.Click += new EventHandler(AdvanceTime30D_Click);
            m_oViewPortPanel.StartSim.Click += new EventHandler(StartSim_Click);

            // setup GL Canvas and insert it into the ViewPort:
            m_oGLCanvas = new GLCanvas();
            m_oGLCanvas.Dock = DockStyle.Fill;
            m_oViewPortPanel.Controls.Add(m_oGLCanvas);

            // setup viewmodel:
            VM = new GLStarSystemViewModel();

            // Bind System Selection Combo Box.
            m_oControlsPanel.SystemSelectionComboBox.Bind(c => c.DataSource, VM, d => d.StarSystems);
            m_oControlsPanel.SystemSelectionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentStarSystem, DataSourceUpdateMode.OnPropertyChanged);
            m_oControlsPanel.SystemSelectionComboBox.DisplayMember = "Name";
            VM.StarSystemChanged += (s, args) => CurrentStarSystem = VM.CurrentStarSystem;
            m_oCurrnetSystem = VM.CurrentStarSystem;
            m_oControlsPanel.SystemSelectionComboBox.SelectedIndexChanged += (s, args) => m_oControlsPanel.SystemSelectionComboBox.DataBindings["SelectedItem"].WriteValue();

            // register event handlers:\
            m_oGLCanvas.KeyDown += new KeyEventHandler(OnKeyDown);
            m_oGLCanvas.MouseDown += new MouseEventHandler(OnMouseDown);
            m_oGLCanvas.MouseMove += new MouseEventHandler(OnMouseMove);
            m_oGLCanvas.MouseUp += new MouseEventHandler(OnMouseUp);
            m_oGLCanvas.MouseHover += new EventHandler(OnMouseHover);
            m_oGLCanvas.MouseWheel += new MouseEventHandler(OnMouseWheel);
            m_oGLCanvas.Click += new EventHandler(m_oGLCanvas_Click);

            m_oViewPortPanel.SizeChanged += new EventHandler(ViewPort_SizeChanged);

            m_oControlsPanel.PanUpButton.Click += new EventHandler(PanUpButton_Click);
            m_oControlsPanel.PanDownButton.Click += new EventHandler(PanDownButton_Click);
            m_oControlsPanel.PanLeftButton.Click += new EventHandler(PanLeftButton_Click);
            m_oControlsPanel.PanRightButton.Click += new EventHandler(PanRightButton_Click);
            m_oControlsPanel.ZoomInButton.Click += new EventHandler(ZoomInButton_Click);
            m_oControlsPanel.ZoomOutButton.Click += new EventHandler(ZoomOutButton_Click);
            m_oControlsPanel.ResetViewButton.Click += new EventHandler(ResetViewButton_Click);
            m_oControlsPanel.CreateMapMarkerButton.Click += new EventHandler(CreateMapMarkerButton_Click);
            m_oControlsPanel.DeleteMapMarkerButton.Click += new EventHandler(DeleteMapMarkerButton_Click);
            m_oControlsPanel.SystemSelectionComboBox.SelectedIndexChanged += new EventHandler(SystemSelectComboBox_SelectedIndexChanged);
        }

        #region EventHandlers

        void m_oGLCanvas_Click(object sender, EventArgs e)
        {
            if (m_bCreateMapMarkerOnNextClick == true)
            {
                // safty check:
                if (m_oCurrentSceen != null)
                {
                    Point oCursorPosition = m_oGLCanvas.PointToClient(Cursor.Position);
                    // Convert to be world coords:
                    Vector3 v3CurPosWorldCorrds = new Vector3(oCursorPosition.X - (m_oGLCanvas.Size.Width / 2), oCursorPosition.Y - (m_oGLCanvas.Size.Height / 2), 0);
                    v3CurPosWorldCorrds = v3CurPosWorldCorrds / m_oGLCanvas.ZoomFactor;
                    v3CurPosWorldCorrds.Y = -v3CurPosWorldCorrds.Y;

                    // add screen offset:
                    v3CurPosWorldCorrds = v3CurPosWorldCorrds - (m_oCurrentSceen.ViewOffset / m_oCurrentSceen.ZoomSclaer);

                    m_oCurrentSceen.AddMapMarker(v3CurPosWorldCorrds, m_oGLCanvas.DefaultEffect);

                    /// <summary>
                    /// Create waypoint on the back end to correspond to the front end display.
                    /// </summary>
                    m_oCurrnetSystem.AddWaypoint(m_oCurrentSceen.MapMarkers.Last().Lable.Text,v3CurPosWorldCorrds.X, v3CurPosWorldCorrds.Y,0);

                    m_bCreateMapMarkerOnNextClick = false;

                    m_oControlsPanel.MapMarkersListBox.Refresh();
                }
            }
        }

        void DeleteMapMarkerButton_Click(object sender, EventArgs e)
        {
            MapMarker oMarker = m_oControlsPanel.MapMarkersListBox.SelectedItem as MapMarker;
            if (oMarker != null)
            {

                /// <summary>
                /// Delete the corresponding waypoint from the starsystem. Index is -1 due to me taking this value after the MMListBox removal.
                /// </summary>

                m_oCurrnetSystem.RemoveWaypoint(m_oCurrnetSystem.Waypoints[m_oControlsPanel.MapMarkersListBox.SelectedIndex]);
                
                m_oCurrentSceen.MapMarkers.Remove(oMarker);      
            }
        }

        void CreateMapMarkerButton_Click(object sender, EventArgs e)
        {
            m_bCreateMapMarkerOnNextClick = true;
        }


        #region Time Controls
        /// <summary>
        /// Function to advance time for all buttons.
        /// </summary>
        /// <param name="TickValue"></param>
        private void AdvanceTime(int TickValue)
        {
            int elapsed = GameState.SE.SubpulseHandler(GameState.Instance.Factions, GameState.RNG, TickValue);

            TimeSpan TS = new TimeSpan(0, 0, elapsed);
            GameState.Instance.GameDateTime = GameState.Instance.GameDateTime.Add(TS);

            int Seconds = GameState.Instance.GameDateTime.Second + (GameState.Instance.GameDateTime.Minute * 60) + (GameState.Instance.GameDateTime.Hour * 3600) +
                           (GameState.Instance.GameDateTime.DayOfYear * 86400) - 86400;

            GameState.Instance.YearTickValue = Seconds;

            /// <summary>
            /// Put the date time somewhere.
            /// </summary>
#warning don't really need this debug info
            m_oControlsPanel.TabText = "SystemMap.cs Kludge(239): " + GameState.Instance.GameDateTime.ToString() + " " + Seconds.ToString() + " " + GameState.SE.CurrentTick.ToString() + " " + GameState.SE.lastTick.ToString() ;

            MainFormReference.Text = "Pulsar4X - " + GameState.Instance.GameDateTime.ToString();

            m_oCurrentSceen.Refresh();
        }

        /// <summary>
        /// advances time by 5 seconds.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AdvanceTime5S_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 5 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.FiveSeconds);
            }
        }

        private void AdvanceTime30S_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 30 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.ThirtySeconds);
            }
        }

        private void AdvanceTime2M_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 120 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.TwoMinutes);
            }
        }

        private void AdvanceTime5M_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 300 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.FiveMinutes);
            }
        }

        private void AdvanceTime20M_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 1200 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.TwentyMinutes);
            }
        }

        private void AdvanceTime1H_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 3600 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.Hour);
            }
        }

        private void AdvanceTime3H_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 10800 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.ThreeHours);
            }
        }

        private void AdvanceTime8H_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 28800 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.EightHours);
            }
        }

        private void AdvanceTime1D_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 86400 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.Day);
            }
        }

        private void AdvanceTime5D_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 432000 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.FiveDays);
            }
        }
        private void AdvanceTime30D_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// Advance simtime by 2592000 seconds.
            /// </summary>
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.Month);
            }
        }

        #endregion

        /// <summary>
        /// Deprecated simulation code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
#warning this may be deprecated
        private void StartSim_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == false)
            {
                /// <summary>
                /// Start the simulation.
                /// </summary>

                for (int loop = GameState.SE.factionStart; loop < GameState.SE.factionCount; loop++)
                {
                    switch (loop)
                    {
                        case 0: GameState.Instance.Factions[loop].FactionColor = Color.Blue;
                            break;
                        case 1: GameState.Instance.Factions[loop].FactionColor = Color.Red;
                            break;
                        case 2: GameState.Instance.Factions[loop].FactionColor = Color.Green;
                            break;
                        case 3: GameState.Instance.Factions[loop].FactionColor = Color.Yellow;
                            break;
                        case 4: GameState.Instance.Factions[loop].FactionColor = Color.Brown;
                            break;
                        case 5: GameState.Instance.Factions[loop].FactionColor = Color.Purple;
                            break;
                        case 6: GameState.Instance.Factions[loop].FactionColor = Color.Orange;
                            break;
                        case 7: GameState.Instance.Factions[loop].FactionColor = Color.Pink;
                            break;
                        case 8: GameState.Instance.Factions[loop].FactionColor = Color.Gray;
                            break;
                        case 9: GameState.Instance.Factions[loop].FactionColor = Color.Black;
                            break;
                        case 10: GameState.Instance.Factions[loop].FactionColor = Color.White;
                            break;
                        case 11: GameState.Instance.Factions[loop].FactionColor = Color.Cyan;
                            break;
                        case 12: GameState.Instance.Factions[loop].FactionColor = Color.Aqua;
                            break;
                        case 13: GameState.Instance.Factions[loop].FactionColor = Color.Crimson;
                            break;
                        case 14: GameState.Instance.Factions[loop].FactionColor = Color.Khaki;
                            break;
                        case 15: GameState.Instance.Factions[loop].FactionColor = Color.Olive;
                            break;
                    }
                }

                GameState.SE.SimCreated = true;
            }
        }

        /// <summary>   Executes the mouse move action. i.e. Panning </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Vector3 v3PanEndLocation;
                v3PanEndLocation.X = e.Location.X;
                v3PanEndLocation.Y = e.Location.Y;
                v3PanEndLocation.Z = 0.0f;

                Vector3 v3PanAmount = (v3PanEndLocation - m_v3PanStartLocation);

                v3PanAmount.Y = -v3PanAmount.Y; // we flip Y to make the panning go in the right direction.
                m_oGLCanvas.Pan(ref v3PanAmount);

                m_v3PanStartLocation.X = e.Location.X;
                m_v3PanStartLocation.Y = e.Location.Y;
                m_v3PanStartLocation.Z = 0.0f;

                m_oGLCanvas.Invalidate();
            }
            else if (e.Button == MouseButtons.Left && m_oCurrentSceen.MeasureMode == true && Control.ModifierKeys == Keys.Control)
            {
                Point oCursorPosition = m_oGLCanvas.PointToClient(Cursor.Position);
                // Convert to be world coords:
                Vector3 v3CurPosWorldCorrds = new Vector3(oCursorPosition.X - (m_oGLCanvas.Size.Width / 2), oCursorPosition.Y - (m_oGLCanvas.Size.Height / 2), 0);
                v3CurPosWorldCorrds = v3CurPosWorldCorrds / m_oGLCanvas.ZoomFactor;
                v3CurPosWorldCorrds.Y = -v3CurPosWorldCorrds.Y;

                // add screen offset:
                v3CurPosWorldCorrds = v3CurPosWorldCorrds - (m_oCurrentSceen.ViewOffset / m_oCurrentSceen.ZoomSclaer);

                // calc the dist measured.
                v3CurPosWorldCorrds.X -= m_v3MeasurementStartLocation.X;
                v3CurPosWorldCorrds.Y -= m_v3MeasurementStartLocation.Y;
                float fLeng = v3CurPosWorldCorrds.Length;
                m_oCurrentSceen.SetMeasurementEndPos(v3CurPosWorldCorrds, fLeng.ToString() + "AU");
            }
        }


        /// <summary>   Executes the mouse down action. i.e. Start panning </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        public void OnMouseDown(object sender, MouseEventArgs e)
        {
            // An left mouse down, start pan.
            if (e.Button.Equals(System.Windows.Forms.MouseButtons.Right))
            {
                Cursor.Current = Cursors.NoMove2D;
                m_v3PanStartLocation.X = e.Location.X;
                m_v3PanStartLocation.Y = e.Location.Y;
                m_v3PanStartLocation.Z = 0.0f;
            }
            else if (e.Button.Equals(System.Windows.Forms.MouseButtons.Left) && Control.ModifierKeys == Keys.Control)
            {
                Point oCursorPosition = m_oGLCanvas.PointToClient(Cursor.Position);
                // Convert to be world coords:
                Vector3 v3CurPosWorldCorrds = new Vector3(oCursorPosition.X - (m_oGLCanvas.Size.Width / 2), oCursorPosition.Y - (m_oGLCanvas.Size.Height / 2), 0);
                v3CurPosWorldCorrds = v3CurPosWorldCorrds / m_oGLCanvas.ZoomFactor;
                v3CurPosWorldCorrds.Y = -v3CurPosWorldCorrds.Y;

                if (m_oCurrentSceen.MeasureMode == false)
                {
                    // on left button down, enable MesureMode
                    m_oCurrentSceen.MeasureMode = true;

                    // add screen offset:
                    v3CurPosWorldCorrds = v3CurPosWorldCorrds - (m_oCurrentSceen.ViewOffset / m_oCurrentSceen.ZoomSclaer);

                    // Set mesurement Start position:
                    m_oCurrentSceen.SetMeasurementStartPos(v3CurPosWorldCorrds);
                    m_v3MeasurementStartLocation = v3CurPosWorldCorrds;
                }
            }
            else if (e.Button.Equals(System.Windows.Forms.MouseButtons.Middle))
            {
                // on middle or mouse wheel button, centre!
                m_oGLCanvas.CenterOnZero();
            }

            m_oGLCanvas.Invalidate();
        }

        public void OnMouseUp(object sender, MouseEventArgs e)
        {
            // reset cursor:
            Cursor.Current = Cursors.Default;

            // cleamre measurement:
            m_oCurrentSceen.MeasureMode = false;

            m_oGLCanvas.Invalidate();
        }

        public void OnMouseHover(object sender, EventArgs e)
        {
            // get mouse position in control coords:
            Point oCursorPosition = m_oGLCanvas.PointToClient(Cursor.Position);

            // Convert to be world coords:
            Vector3 v3CurPosWorldCorrds = new Vector3((m_oGLCanvas.Size.Width / 2) - oCursorPosition.X, (m_oGLCanvas.Size.Height / 2) - oCursorPosition.Y, 0);
            v3CurPosWorldCorrds = v3CurPosWorldCorrds / m_oGLCanvas.ZoomFactor;

            // Guid oEntity = m_oCurrentSceen.GetElementAtCoords(v3CurPosWorldCorrds);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            InputProcessor(e, null);
            m_oGLCanvas.Invalidate();
        }

        private void SystemSelectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_oGLCanvas != null)
            {
                m_oGLCanvas.Focus();
                RefreshStarSystem();
                m_oGLCanvas.Invalidate();
                m_oControlsPanel.MapMarkersListBox.DataSource = m_oCurrentSceen.MapMarkers;
            }
        }

        private void OnMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // See here for MSDN Ref: http://msdn.microsoft.com/en-us/library/system.windows.forms.control.mousewheel(v=vs.71).aspx
            if (e.Delta <= -120)
            {
                // then we have scrolled down, so zoom out!!
                m_oGLCanvas.DecreaseZoomScaler();
                UpdateScaleLabels();
            }
            else if (e.Delta >= 120)
            {
                // the we have scrolled up, so zoom in.
                m_oGLCanvas.IncreaseZoomScaler();
                UpdateScaleLabels();
            }
        }

        private void ResetViewButton_Click(object sender, EventArgs e)
        {
            m_oGLCanvas.CenterOnZero();
            m_oGLCanvas.ZoomFactor = m_oCurrentSceen.DefaultZoomScaler;
            m_oCurrentSceen.Refresh();
            m_oGLCanvas.Focus();
            UpdateScaleLabels();
        }

        private void ZoomInButton_Click(object sender, EventArgs e)
        {
            m_oGLCanvas.IncreaseZoomScaler();
            UpdateScaleLabels();
            m_oGLCanvas.Focus();
        }

        private void ZoomOutButton_Click(object sender, EventArgs e)
        {
            m_oGLCanvas.DecreaseZoomScaler();
            UpdateScaleLabels();
            m_oGLCanvas.Focus();
        }

        private void PanRightButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(-UIConstants.DEFAULT_PAN_AMOUNT, 0, 0);
            m_oGLCanvas.Pan(ref v3PanAmount);
            m_oGLCanvas.Focus();
        }

        private void PanLeftButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(UIConstants.DEFAULT_PAN_AMOUNT, 0, 0);
            m_oGLCanvas.Pan(ref v3PanAmount);
            m_oGLCanvas.Focus();
        }

        private void PanUpButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(0, -UIConstants.DEFAULT_PAN_AMOUNT, 0);
            m_oGLCanvas.Pan(ref v3PanAmount);
            m_oGLCanvas.Focus();
        }

        private void PanDownButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(0, UIConstants.DEFAULT_PAN_AMOUNT, 0);
            m_oGLCanvas.Pan(ref v3PanAmount);
            m_oGLCanvas.Focus();
        }

        private void ViewPort_SizeChanged(object sender, EventArgs e)
        {
            UpdateScaleLabels();
        }

        #endregion

        #region PublicMethods

        /// <summary>
        /// Shows all the System Map Panels.
        /// </summary>
        /// <param name="a_oDockPanel"> The target Docking Panel. </param>
        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            ShowViewPortPanel(a_oDockPanel);
            ShowControlsPanel(a_oDockPanel);

            // refresh the current star system:
            RefreshStarSystem();
            UpdateScaleLabels();
        }

        /// <summary>
        /// Shows the View Port Panel.
        /// </summary>
        /// <param name="a_oDockPanel"> The target Docking Panel. </param>
        public void ShowViewPortPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oViewPortPanel.Show(a_oDockPanel, DockState.Document);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        /// <summary>
        /// Makes the View Port Panel Active.
        /// </summary>
        public void ActivatePortPanel()
        {
            if (!m_oViewPortPanel.IsActivated)
            {
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
                m_oViewPortPanel.Activate();
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
            }
        }

        /// <summary>
        /// Shows the Controls Panel.
        /// </summary>
        /// <param name="a_oDockPanel"> The target Docking Panel. </param>
        public void ShowControlsPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oControlsPanel.Show(a_oDockPanel, DockState.DockLeft);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        /// <summary>
        /// Makes the Controls Panel Active.
        /// </summary>
        public void ActivateControlsPanel()
        {
            if (!m_oControlsPanel.IsActivated)
            {
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
                m_oControlsPanel.Activate();
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
            }
        }

        /// <summary>
        /// Refreshes and redraws the StarSystem
        /// </summary>
        public void RefreshStarSystem()
        {
            // We only want to refresh a system when we have a valid GLCanvas:
            if (m_oGLCanvas == null)
            {
                return;
            }
            else if (m_oGLCanvas.Loaded == false)
            {
                return;
            }

            // Now we test to see if we have already loaded this system or if it the current one:
            if (m_oCurrentSceen == null)
            {
                // we have an invalid current system, creat a new one!!
                CreateNewSystemSceen();
                return; // we dont need to do anything else, CreateNewSystemSceen took care of it all for us.
            }
            else if (m_oCurrentSceen.SceenID != m_oCurrnetSystem.Id)
            {
                // check if we have created it previously:
                bool bSceenFound = false;
                foreach (Sceen oSceen in m_lSystemSceens)
                {
                    if (oSceen.SceenID == m_oCurrnetSystem.Id)
                    {
                        // switch our current sceen and break the loop:
                        SetCurrentSceen(oSceen);
                        bSceenFound = true;
                        break;
                    }
                }

                // check to see if we found it:
                if (bSceenFound == false)
                {
                    // we didn't, so crest it:
                    CreateNewSystemSceen();
                    return; // we dont need to do anything else, CreateNewSystemSceen took care of it all for us.
                }
            }

            // As we are refreshing the current system or one previosle turned into a sceen, we do the below:

            // Change cursor to wait
            Cursor.Current = Cursors.WaitCursor;

            // Do things like update orbits and taskgroups here.
            m_oCurrentSceen.Refresh();

            // Change Cursor Back to default.
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Processes Input for the system map.
        /// </summary>
        /// <param name="k"> Keyboard events, if none pass in null. </param>
        /// <param name="m"> Mouse events, if none pass in null. </param>
        public void InputProcessor(KeyEventArgs k, MouseEventArgs m)
        {
            if (k != null)
            {
                if (k.KeyCode == Keys.W)
                {
                    Vector3 v3PanAmount = new Vector3(0, -UIConstants.DEFAULT_PAN_AMOUNT, 0);
                    m_oGLCanvas.Pan(ref v3PanAmount);
                }
                if (k.KeyCode == Keys.S)
                {
                    Vector3 v3PanAmount = new Vector3(0, UIConstants.DEFAULT_PAN_AMOUNT, 0);
                    m_oGLCanvas.Pan(ref v3PanAmount);
                }
                if (k.KeyCode == Keys.A)
                {
                    Vector3 v3PanAmount = new Vector3(UIConstants.DEFAULT_PAN_AMOUNT, 0, 0);
                    m_oGLCanvas.Pan(ref v3PanAmount);
                }
                if (k.KeyCode == Keys.D)
                {
                    Vector3 v3PanAmount = new Vector3(-UIConstants.DEFAULT_PAN_AMOUNT, 0, 0);
                    m_oGLCanvas.Pan(ref v3PanAmount);
                }
                if (k.KeyCode == Keys.E)
                {
                    m_oGLCanvas.IncreaseZoomScaler();
                    UpdateScaleLabels();
                }
                if (k.KeyCode == Keys.Q)
                {
                    m_oGLCanvas.DecreaseZoomScaler();
                    UpdateScaleLabels();
                }
                if (k.KeyCode == Keys.R)
                {
                    m_oGLCanvas.CenterOnZero();
                    m_oGLCanvas.ZoomFactor = m_oCurrentSceen.DefaultZoomScaler;
                    m_oCurrentSceen.Refresh();
                }
            }

            if (m != null)
            {

            }
        }

        #endregion

        #region PrivateMethods

        /// <summary>
        /// Creats a new sceen for the currently selected star system.
        /// </summary>
        private void CreateNewSystemSceen()
        {
            // Change cursor to wait
            Cursor.Current = Cursors.WaitCursor;

            // create new sceen root node:
            Sceen oNewSceen = new Sceen(m_oCurrnetSystem, m_oGLCanvas.DefaultEffect, this);
            //oNewSceen.SceenID = m_oCurrnetSystem.Id;
            m_lSystemSceens.Add(oNewSceen);
            // set sceen to current:
            SetCurrentSceen(oNewSceen);

            FitZoom(oNewSceen.SceenSize.X / 1.8);
            oNewSceen.DefaultZoomScaler = m_oCurrentSceen.ZoomSclaer;

            oNewSceen.Refresh(); // force refresh.
            // Change Cursor Back to default.
            Cursor.Current = Cursors.Default;
        }


        /// <summary>
        /// Updates the Scale labels on the UI.
        /// </summary>
        private void UpdateScaleLabels()
        {
            if (m_oGLCanvas != null)
            {
                double dKmscale = (m_oGLCanvas.Size.Width / m_oGLCanvas.ZoomFactor) * Constants.Units.KM_PER_AU;
                float dAUScale = (float)(m_oGLCanvas.Size.Width / m_oGLCanvas.ZoomFactor);
                m_oControlsPanel.ScaleKMLable.Text = "Km = " + dKmscale.ToString();
                m_oControlsPanel.ScaleAULable.Text = "AU = " + dAUScale.ToString();
            }
        }

        /// <summary>
        /// This will fit the current zoom level to the system, based on the "Max Orbit" provided.
        /// </summary>
        /// <param name="a_dMaxOrbit">The Highest orbiut in the system.</param>
        private void FitZoom(double a_dMaxOrbit)
        {
            double dZoomFactor = ((m_oGLCanvas.Size.Width / m_oGLCanvas.ZoomFactor) / (a_dMaxOrbit * 2));
            m_oGLCanvas.ZoomFactor = (float)dZoomFactor;
            m_oCurrentSceen.ZoomSclaer = (float)dZoomFactor;
        }

        /// <summary>
        /// Sets the Current Sceen
        /// </summary>
        /// <param name="a_oSceen">The New Current Sceen</param>
        private void SetCurrentSceen(Sceen a_oSceen)
        {
            m_oCurrentSceen = a_oSceen;
            m_oGLCanvas.SceenToRender = a_oSceen;
        }

        #endregion

    }
}
