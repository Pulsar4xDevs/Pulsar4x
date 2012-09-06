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
using Pulsar4X.ViewModels;

namespace Pulsar4X.WinForms.Controls
{
    public partial class SystemMap : UserControl
    {

        public static readonly ILog logger = LogManager.GetLogger(typeof(SystemMap));

        private Pulsar4X.Entities.StarSystem m_oCurrnetSystem;

        public GLStarSystemViewModel VM { get; set; }

        /// <summary> The gl canvas </summary>
        GLCanvas m_GLCanvas;
        
        public SystemMap()
        {
            InitializeComponent();
            this.MouseWheel += new MouseEventHandler(SystemMap_MouseWheel);

            VM = new GLStarSystemViewModel();

            SystemSelectComboBox.Bind(c => c.DataSource, VM, d => d.StarSystems);
            SystemSelectComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentStarSystem);
            SystemSelectComboBox.DisplayMember = "Name";

            this.Bind(c => c.CurrentStarSystem, VM, d => d.CurrentStarSystem);
        }

        private void SystemMap_Load(object sender, EventArgs e)
        {
            try
            {
                m_GLCanvas = OpenTKUtilities.Instance.CreateGLCanvas(); //new GLCanvas30();
                m_GLCanvas.Size = this.Size;
                //this.Dock = DockStyle.Fill;
                m_GLCanvas.Dock = DockStyle.Fill;
                this.Controls.Add(m_GLCanvas);
                RefreshStarSystem();
            }
            catch (System.NotSupportedException ex)
            {
                logger.Fatal("Error Occured when trying to Load a GLCanvas.", ex);
            }
            UpdateScaleLabels();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //m_GLCanvas.TestFunc(int.Parse(textBox1.Text));
            FPSLabel.Text = m_GLCanvas.FPS.ToString();
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
            if (m_GLCanvas == null)
            {
                return;
            }
            else if (m_GLCanvas.m_bLoaded == false)
            {
                return;
            }

            // test code only, just to see how bad the scale issue is.
            // for star color later: http://www.vendian.org/mncharity/dir3/starcolor/UnstableURLs/starcolors.html
            // or this http://www.vendian.org/mncharity/dir3/blackbody/UnstableURLs/tool_pl.txt 
            // and this http://www.vendian.org/mncharity/dir3/blackbody/UnstableURLs/bbr_color.html
            // or this: http://www.vendian.org/mncharity/dir3/starcolor/
            m_GLCanvas.RenderList.Clear(); // clear the render list!!

            // add star to centre of the map.
            int iCounter = 0;
            foreach (Pulsar4X.Entities.Star oStar in m_oCurrnetSystem.Stars)
            {
                Vector3 v3StarPos = new Vector3(0, 0, 0);
                if (iCounter > 0)
                {
                    // then we have a secondary, etc star give random position.
                    Random rnd = new Random();
                    v3StarPos.X = (float)(rnd.Next(10, 20) * Pulsar4X.Constants.Units.KM_PER_AU);
                    v3StarPos.Y = (float)(rnd.Next(10, 20) * Pulsar4X.Constants.Units.KM_PER_AU);
                }

                float fSize = (float)oStar.EcoSphereRadius * 2 * 695500; // i.e. radois of sun.

                GLUtilities.GLQuad oStarQuad = new GLUtilities.GLQuad(m_GLCanvas.DefaultShader,
                                                                        v3StarPos,
                                                                        new Vector2(fSize, fSize), 
                                                                        Color.FromArgb(255, 255, 255, 0),    // yellow!
                                                                        "./Resources/Textures/DefaultIcon.png");
                m_GLCanvas.AddToRenderList(oStarQuad);

                // now go though and add each planet to render list.

                foreach (Pulsar4X.Entities.Planet oPlanet in oStar.Planets)
                {
                    double fOrbitRadius = oPlanet.SemiMajorAxis * Pulsar4X.Constants.Units.KM_PER_AU / 10;
                    float fPlanetSize = (float)oPlanet.Radius * 2;
                    if (fPlanetSize * m_GLCanvas.ZoomFactor < 16)
                    {
                        // if we are too small, make us bigger for drawing!!
                        fPlanetSize = fPlanetSize * 1000;

                    }

                    GLUtilities.GLQuad oPlanetQuad = new GLUtilities.GLQuad(m_GLCanvas.DefaultShader,
                        new Vector3((float)fOrbitRadius, 0, 0) + v3StarPos,                                    // offset Pos by parent star pos
                        new Vector2(fPlanetSize, fPlanetSize),
                        Color.FromArgb(255, 50, 205, 50),  // lime green
                        "./Resources/Textures/DefaultIcon.png");
                    GLUtilities.GLCircle oPlanetOrbitCirc = new GLUtilities.GLCircle(m_GLCanvas.DefaultShader,
                        v3StarPos,                                                                      // base around parent star pos.
                        (float)fOrbitRadius, 
                        Color.FromArgb(255, 50, 205, 50),  // lime green
                        "./Resources/Textures/DefaultTexture.png");

                    m_GLCanvas.AddToRenderList(oPlanetQuad);
                    m_GLCanvas.AddToRenderList(oPlanetOrbitCirc);
                }
                // just do primary for now:
                //break;
                iCounter++;
            }

            


        }

        private void SystemSelectComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
           // RefreshStarSystem(); // we only need to do this as the System is change automagicly by the data binding. not needed?????
        }

        private void UpdateScaleLabels()
        {
            double dKmscale = this.Size.Width / m_GLCanvas.ZoomFactor;
            float dAUScale = (float)(dKmscale / Pulsar4X.Constants.Units.KM_PER_AU);
            KmScaleLabel.Text = "Km = " + dKmscale.ToString();
            AUScaleLabel.Text = "AU = " + dAUScale.ToString();
        }
        
    }
}
