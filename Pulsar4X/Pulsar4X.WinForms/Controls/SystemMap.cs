using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.Controls;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using log4net.Config;
using log4net;
using Pulsar4X.WinForms.ViewModels;
using Pulsar4X.WinForms.Controls.SceenGraph;
using Pulsar4X.Entities;

namespace Pulsar4X.WinForms.Controls
{
    public partial class SystemMap : UserControl
    {
        public static readonly ILog logger = LogManager.GetLogger(typeof(SystemMap));

        private Pulsar4X.Entities.StarSystem m_oCurrnetSystem;

        public GLStarSystemViewModel VM { get; set; }

        /// <summary> The gl canvas </summary>
        GLCanvas m_GLCanvas;

        List<Sceen> m_lSystemSceens = new List<Sceen>();

        Sceen m_oCurrentSceen;

        public SystemMap()
        {
            InitializeComponent();
            this.MouseWheel += new MouseEventHandler(SystemMap_MouseWheel);

            VM = new GLStarSystemViewModel();

            SystemSelectComboBox.Bind(c => c.DataSource, VM, d => d.StarSystems);
            SystemSelectComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentStarSystem, DataSourceUpdateMode.OnPropertyChanged);
            SystemSelectComboBox.DisplayMember = "Name";

            // This doesn't work under linux or mac
            //this.Bind(c => c.CurrentStarSystem, VM, d => d.CurrentStarSystem);

            VM.StarSystemChanged += (s, args) => CurrentStarSystem = VM.CurrentStarSystem;
            CurrentStarSystem = VM.CurrentStarSystem;

            SystemSelectComboBox.SelectedIndexChanged += (s, args) => SystemSelectComboBox.DataBindings["SelectedItem"].WriteValue();
        }

        private void SystemMap_Load(object sender, EventArgs e)
        {
            try
            {
                m_GLCanvas = OpenTKUtilities.Instance.CreateGLCanvas(); //new GLCanvas30();
                m_GLCanvas.Size = this.Size;
                //this.Dock = DockStyle.Fill;
                //m_GLCanvas.Dock = DockStyle.Fill;
                m_GLCanvas.InputHandler += InputProcessor;
                this.Controls.Add(m_GLCanvas);
                //m_GLCanvas.Parent = this;
                RefreshStarSystem();
            }
            catch (System.NotSupportedException ex)
            {
                logger.Fatal("Error Occured when trying to Load a GLCanvas.", ex);
            }
            UpdateScaleLabels();
        }

        private void SystemMap_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // See here for MSDN Ref: http://msdn.microsoft.com/en-us/library/system.windows.forms.control.mousewheel(v=vs.71).aspx
            if (e.Delta <= -120)
            {
                // then we have scrolled down, so zoom out!!
                m_GLCanvas.DecreaseZoomScaler();
                UpdateScaleLabels();                
            }
            else if (e.Delta >= 120)
            {
                // the we have scrolled up, so zoom in.
                m_GLCanvas.IncreaseZoomScaler();
                UpdateScaleLabels();
            }
        }

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

        /// <summary>
        /// Refreshes and redraws the StarSystem
        /// </summary>
        public void RefreshStarSystem()
        {
            // We only want to refresh a system when we have a valid GLCanvas:
            if (m_GLCanvas == null)
            {
                return;
            }
            else if (m_GLCanvas.Loaded == false)
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
        /// Creats a new sceen for the currently selected star system.
        /// </summary>
        private void CreateNewSystemSceen()
        {
            // Change cursor to wait
            Cursor.Current = Cursors.WaitCursor;

            // create new sceen root node:
            Sceen oNewSceen = new Sceen(m_oCurrnetSystem, m_GLCanvas.DefaultShader);
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
            if (m_GLCanvas != null)
            {
                double dKmscale = this.Size.Width / m_GLCanvas.ZoomFactor * 10;  // times by 10 to make scale the same as actual scale usid in drawing the systems.
                float dAUScale = (float)(dKmscale / Pulsar4X.Constants.Units.KM_PER_AU);
                KmScaleLabel.Text = "Km = " + dKmscale.ToString();
                AUScaleLabel.Text = "AU = " + dAUScale.ToString();

                // Add FPS too for now:
                FPSLabel.Text = m_GLCanvas.FPS.ToString();
            }
        }

        /// <summary>
        /// This will fit the current zoom level to the system, based on the "Max Orbit" provided.
        /// </summary>
        /// <param name="a_dMaxOrbit">The Highest orbiut in the system.</param>
        private void FitZoom(double a_dMaxOrbit)
        {
            double dZoomFactor = ((this.Size.Width / m_GLCanvas.ZoomFactor) / (a_dMaxOrbit * 2));
            m_GLCanvas.ZoomFactor = (float)dZoomFactor;
            m_oCurrentSceen.ZoomSclaer = (float)dZoomFactor;
        }

        /// <summary>
        /// Sets the Current Sceen
        /// </summary>
        /// <param name="a_oSceen">The New Current Sceen</param>
        private void SetCurrentSceen(Sceen a_oSceen)
        {
            m_oCurrentSceen = a_oSceen;
            m_GLCanvas.SceenToRender = a_oSceen;
        }

        private void SystemMap_MouseHover(object sender, EventArgs e)
        {
            // get mouse position in control coords:
            //Point oCursorPosition = m_GLCanvas.PointToClient(Cursor.Position);

            // Convert to be world coords:
            //Vector3 v3CurPosWorldCorrds = new Vector3((m_GLCanvas.Size.Width / 2) - oCursorPosition.X, (m_GLCanvas.Size.Height / 2) - oCursorPosition.Y, 0);
            //v3CurPosWorldCorrds = v3CurPosWorldCorrds / m_GLCanvas.ZoomFactor;

            //Guid oEntity = m_oCurrentSceen.GetElementAtCoords(v3CurPosWorldCorrds);
        }

        private void ResetViewButton_Click(object sender, EventArgs e)
        {
            m_GLCanvas.CenterOnZero();
            m_GLCanvas.ZoomFactor = m_oCurrentSceen.DefaultZoomScaler;
            m_oCurrentSceen.Refresh();
            m_GLCanvas.Focus();
        }

        private void ZoomInButton_Click(object sender, EventArgs e)
        {
            m_GLCanvas.IncreaseZoomScaler();
            UpdateScaleLabels();
            m_GLCanvas.Focus();
        }

        private void ZoomOutButton_Click(object sender, EventArgs e)
        {
            m_GLCanvas.DecreaseZoomScaler();
            UpdateScaleLabels();
            m_GLCanvas.Focus();
        }

        private void PanRightButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(-UIConstants.DEFAULT_PAN_AMOUNT, 0, 0);
            m_GLCanvas.Pan(ref v3PanAmount);
            m_GLCanvas.Focus();
        }

        private void PanLeftButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(UIConstants.DEFAULT_PAN_AMOUNT, 0, 0);
            m_GLCanvas.Pan(ref v3PanAmount);
            m_GLCanvas.Focus();
        }

        private void PanUpButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(0, -UIConstants.DEFAULT_PAN_AMOUNT, 0);
            m_GLCanvas.Pan(ref v3PanAmount);
            m_GLCanvas.Focus();
        }

        private void PanDownButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(0, UIConstants.DEFAULT_PAN_AMOUNT, 0);
            m_GLCanvas.Pan(ref v3PanAmount);
            m_GLCanvas.Focus();
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
                            m_GLCanvas.Pan(ref v3PanAmount);
                            break;
                        }
                    case Keys.S:
                        {
                            Vector3 v3PanAmount = new Vector3(0, UIConstants.DEFAULT_PAN_AMOUNT, 0);
                            m_GLCanvas.Pan(ref v3PanAmount);
                            break;
                        }
                    case Keys.A:
                        {
                            Vector3 v3PanAmount = new Vector3(UIConstants.DEFAULT_PAN_AMOUNT, 0, 0);
                            m_GLCanvas.Pan(ref v3PanAmount);
                            break;
                        }
                    case Keys.D:
                        {
                            Vector3 v3PanAmount = new Vector3(-UIConstants.DEFAULT_PAN_AMOUNT, 0, 0);
                            m_GLCanvas.Pan(ref v3PanAmount);
                            break;
                        }
                    case Keys.Add:
                    case Keys.E:
                        {
                            m_GLCanvas.IncreaseZoomScaler();
                            UpdateScaleLabels();
                            break;
                        }
                    case Keys.Subtract:
                    case Keys.Q:
                        {
                            m_GLCanvas.DecreaseZoomScaler();
                            UpdateScaleLabels();
                            break;
                        }
                    case Keys.R:
                        {
                            m_GLCanvas.CenterOnZero();
                            m_GLCanvas.ZoomFactor = m_oCurrentSceen.DefaultZoomScaler;
                            m_oCurrentSceen.Refresh();
                            break;
                        }
                }
            }

            if (m != null)
            {

            }
        }

        private void SystemMap_SizeChanged(object sender, EventArgs e)
        {
            // update Scale lables for new screen size:
            UpdateScaleLabels();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            InputProcessor(e, null);
        }
    }
}







//// test code only, just to see how bad the scale issue is.
//// for star color later: http://www.vendian.org/mncharity/dir3/starcolor/UnstableURLs/starcolors.html
//// or this http://www.vendian.org/mncharity/dir3/blackbody/UnstableURLs/tool_pl.txt 
//// and this http://www.vendian.org/mncharity/dir3/blackbody/UnstableURLs/bbr_color.html
//// or this: http://www.vendian.org/mncharity/dir3/starcolor
//// For right now to work around float percision and overflow issues 1 am scaling everthing down by a factor of 10.
