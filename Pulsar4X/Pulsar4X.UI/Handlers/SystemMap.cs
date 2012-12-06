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
    public class SystemMap
    {
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

        List<Sceen> m_lSystemSceens = new List<Sceen>();

        private Sceen m_oCurrentSceen;

        public GLStarSystemViewModel VM { get; set; }


        public SystemMap()
        {
            m_oViewPortPanel = new Panels.SysMap_ViewPort();
            m_oControlsPanel = new Panels.SysMap_Controls();

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

            // register event handlers:
            m_oGLCanvas.InputHandler += InputProcessor;
            m_oGLCanvas.KeyDown += new KeyEventHandler(OnKeyDown);
            m_oGLCanvas.MouseDown += new MouseEventHandler(OnMouseDown);
            m_oGLCanvas.MouseMove += new MouseEventHandler(OnMouseMove);
            m_oGLCanvas.MouseUp += new MouseEventHandler(OnMouseUp);
            m_oGLCanvas.MouseHover += new EventHandler(OnMouseHover);
            m_oGLCanvas.MouseWheel += new MouseEventHandler(OnMouseWheel);

            m_oControlsPanel.PanUpButton.Click += new EventHandler(PanUpButton_Click);
            m_oControlsPanel.PanDownButton.Click += new EventHandler(PanDownButton_Click);
            m_oControlsPanel.PanLeftButton.Click += new EventHandler(PanLeftButton_Click);
            m_oControlsPanel.PanRightButton.Click += new EventHandler(PanRightButton_Click);
            m_oControlsPanel.ZoomInButton.Click += new EventHandler(ZoomInButton_Click);
            m_oControlsPanel.ZoomOutButton.Click += new EventHandler(ZoomOutButton_Click);
            m_oControlsPanel.ResetViewButton.Click += new EventHandler(ResetViewButton_Click);
            m_oViewPortPanel.SizeChanged += new EventHandler(ViewPort_SizeChanged);
        }

        #region EventHandlers

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
            else if (e.Button == MouseButtons.Left && m_oCurrentSceen.MeasureMode == true)
            {
                Point oCursorPosition = m_oGLCanvas.PointToClient(Cursor.Position);
                // Convert to be world coords:
                Vector3 v3CurPosWorldCorrds = new Vector3(oCursorPosition.X - (m_oGLCanvas.Size.Width / 2), oCursorPosition.Y - (m_oGLCanvas.Size.Height / 2), 0);
                v3CurPosWorldCorrds = v3CurPosWorldCorrds / m_oGLCanvas.ZoomFactor;
                v3CurPosWorldCorrds.Y = -v3CurPosWorldCorrds.Y;

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
            else if (e.Button.Equals(System.Windows.Forms.MouseButtons.Left))
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

        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            ShowViewPortPanel(a_oDockPanel);
            ShowControlsPanel(a_oDockPanel);

            // refresh the current star system:
            RefreshStarSystem();
            UpdateScaleLabels();
        }

        public void ShowViewPortPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oViewPortPanel.Show(a_oDockPanel, DockState.Document);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ActivatePortPanel()
        {
            if (!m_oViewPortPanel.IsActivated)
            {
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
                m_oViewPortPanel.Activate();
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
            }
        }

        public void ShowControlsPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oControlsPanel.Show(a_oDockPanel, DockState.DockLeft);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

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

        public void InputProcessor(KeyEventArgs k, MouseEventArgs m)
        {
            if (k != null)
            {
                switch (k.KeyCode)
                {
                    case Keys.W:
                        {
                            Vector3 v3PanAmount = new Vector3(0, -UIConstants.DEFAULT_PAN_AMOUNT, 0);
                            m_oGLCanvas.Pan(ref v3PanAmount);
                            break;
                        }
                    case Keys.S:
                        {
                            Vector3 v3PanAmount = new Vector3(0, UIConstants.DEFAULT_PAN_AMOUNT, 0);
                            m_oGLCanvas.Pan(ref v3PanAmount);
                            break;
                        }
                    case Keys.A:
                        {
                            Vector3 v3PanAmount = new Vector3(UIConstants.DEFAULT_PAN_AMOUNT, 0, 0);
                            m_oGLCanvas.Pan(ref v3PanAmount);
                            break;
                        }
                    case Keys.D:
                        {
                            Vector3 v3PanAmount = new Vector3(-UIConstants.DEFAULT_PAN_AMOUNT, 0, 0);
                            m_oGLCanvas.Pan(ref v3PanAmount);
                            break;
                        }
                    case Keys.Add:
                    case Keys.E:
                        {
                            m_oGLCanvas.IncreaseZoomScaler();
                            UpdateScaleLabels();
                            break;
                        }
                    case Keys.Subtract:
                    case Keys.Q:
                        {
                            m_oGLCanvas.DecreaseZoomScaler();
                            UpdateScaleLabels();
                            break;
                        }
                    case Keys.R:
                        {
                            m_oGLCanvas.CenterOnZero();
                            m_oGLCanvas.ZoomFactor = m_oCurrentSceen.DefaultZoomScaler;
                            m_oCurrentSceen.Refresh();
                            break;
                        }
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
            Sceen oNewSceen = new Sceen(m_oCurrnetSystem, m_oGLCanvas.DefaultEffect);
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


        private void UpdateScaleLabels()
        {
            if (m_oGLCanvas != null)
            {
                double dKmscale = (m_oGLCanvas.Size.Width / m_oGLCanvas.ZoomFactor) * Constants.Units.KM_PER_AU;  // times by 10 to make scale the same as actual scale usid in drawing the systems.
                float dAUScale = (float)(m_oGLCanvas.Size.Width / m_oGLCanvas.ZoomFactor);
                m_oControlsPanel.ScaleKMLable.Text = "Km = " + dKmscale.ToString();
                m_oControlsPanel.ScaleAULable.Text = "AU = " + dAUScale.ToString();

                // Add FPS too for now:
                //FPSLabel.Text = m_oGLCanvas.FPS.ToString();
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
