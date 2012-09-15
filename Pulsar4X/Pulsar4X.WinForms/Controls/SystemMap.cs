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
            SystemSelectComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentStarSystem);
            SystemSelectComboBox.DisplayMember = "Name";

            this.Bind(c => c.CurrentStarSystem, VM, d => d.CurrentStarSystem);

            SystemSelectComboBox.SelectedIndexChanged += (s, args) => SystemSelectComboBox.DataBindings["SelectedItem"].WriteValue();
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
            // We only want to refresh a system when we have a valid GLCanvas:
            if (m_GLCanvas == null)
            {
                return;
            }
            else if (m_GLCanvas.m_bLoaded == false)
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
            Sceen oNewSceen = new Sceen();
            oNewSceen.SceenID = m_oCurrnetSystem.Id;
            m_lSystemSceens.Add(oNewSceen);
            // set sceen to current:
            SetCurrentSceen(oNewSceen);

            // Creat Working Vars:
            double dKMperAUdevby10 = (Pulsar4X.Constants.Units.KM_PER_AU / 10); // we scale everthing down by 10 to avoid float buffer overflows.
            int iStarCounter = 0;                                               // Keeps track of the number of stars.
            int iPlanetCounter = 0;                                             // Keeps track of the number of planets around the current star
            int iMoonCounter = 0;                                               // Keeps track of the number of moons around the current planet.
            double dMaxOrbitDist = 0;                                           // used for fit to zoom.
            Vector3 v3StarPos = new Vector3(0, 0, 0);                           // used for storing the psoition of the current star in the system
            float fStarSize = 0.0f;                                             // Size of a star
            double dPlanetOrbitRadius = 0;                                      // used for holding the orbit in Km for a planet.
            Vector3 v3PlanetPos = new Vector3(0, 0, 0);                         // Used to store the planet Pos.
            float fPlanetSize = 0;                                              // used to hold the planets size.
            double dMoonOrbitRadius = 0;                                        // used for holding the orbit in Km for a Moon.
            float fMoonSize = 0;                                                // used to hold the Moons size.

            // start creating star branches in the sceen graph:
            SceenElement oRootStar = new StarElement();
            SceenElement oCurrStar = oRootStar;
            foreach (Pulsar4X.Entities.Star oStar in m_oCurrnetSystem.Stars)
            {
                if (iStarCounter > 0)
                {
                    // then we have a secondary, etc star give random position around its orbit!
                    Random rnd = new Random();
                    float fAngle = rnd.Next(0, 360);
                    fAngle = MathHelper.DegreesToRadians(fAngle);
                    v3StarPos.X = (float)(Math.Cos(fAngle) * oStar.OrbitalRadius * dKMperAUdevby10);
                    v3StarPos.Y = (float)(Math.Sin(fAngle) * oStar.OrbitalRadius * dKMperAUdevby10);
                    MaxOrbitDistTest(ref dMaxOrbitDist, oStar.OrbitalRadius * dKMperAUdevby10);
                    oCurrStar = new StarElement();

                    // create orbit circle:

                }
                iStarCounter++;

                fStarSize = (float)oStar.Radius * 2 * 69550; // i.e. radius of sun / 10.

                GLUtilities.GLQuad oStarQuad = new GLUtilities.GLQuad(m_GLCanvas.DefaultShader,
                                                                        v3StarPos,
                                                                        new Vector2(fStarSize, fStarSize),
                                                                        Color.FromArgb(255, 255, 255, 0),    // yellow!
                                                                        UIConstants.Textures.DEFAULT_PLANET_ICON);
                // create texture from name:
                GLUtilities.GLFont test = new GLUtilities.GLFont(m_GLCanvas.DefaultShader,
                    new Vector3((float)(v3StarPos.X), (float)(v3StarPos.Y - (oStar.Radius * 69550)) - 16, 0)
                    , new Vector2(16, 16), Color.White, UIConstants.Textures.DEFAULT_GLFONT);
                test.Text = "testaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                Vector2 v2NameSize;
                uint uiNameTex = Helpers.ResourceManager.Instance.GenStringTexture(oStar.Name, out v2NameSize);
                GLUtilities.GLQuad oNameQuad = new GLUtilities.GLQuad(m_GLCanvas.DefaultShader,
                                                                      new Vector3((float)(v3StarPos.X), (float)(v3StarPos.Y - (oStar.Radius * 69550)) - v2NameSize.Y, v3StarPos.Z),
                                                                      v2NameSize,
                                                                      Color.White);
                oNameQuad.TextureID = uiNameTex;

                // create orbit circle
                if (iStarCounter > 0)
                {
                    GLUtilities.GLCircle oStarOrbitCirc = new GLUtilities.GLCircle(m_GLCanvas.DefaultShader,
                        Vector3.Zero,                                                                      // base around parent star pos.
                        (float)(oStar.OrbitalRadius * dKMperAUdevby10) / 2,
                        Color.FromArgb(255, 255, 255, 0),  // yellow.
                        UIConstants.Textures.DEFAULT_TEXTURE);
                    oCurrStar.AddPrimitive(oStarOrbitCirc);
                }
                oCurrStar.AddPrimitive(oStarQuad); // Add star icon to the Sceen element.
                oCurrStar.AddPrimitive(oNameQuad);
                //oCurrStar.Lable = test;
                oCurrStar.PrimaryPrimitive = oStarQuad;
                oCurrStar.EntityID = oStar.Id;
                oCurrStar.RealSize = new Vector2(fStarSize, fStarSize);
                oNewSceen.AddElement(oCurrStar);

                // now go though and add each planet to render list.
                foreach (Pulsar4X.Entities.Planet oPlanet in oStar.Planets)
                {
                    SceenElement oPlanetElement = new PlanetElement();
                    oPlanetElement.EntityID = oPlanet.Id;

                    if (iPlanetCounter == 0)
                    {
                        oCurrStar.SmallestOrbit = (float)(oPlanet.SemiMajorAxis * Pulsar4X.Constants.Units.KM_PER_AU * 2);
                    }

                    dPlanetOrbitRadius = oPlanet.SemiMajorAxis * dKMperAUdevby10;
                    v3PlanetPos = new Vector3((float)dPlanetOrbitRadius, 0, 0) + v3StarPos; // offset Pos by parent star pos
                    fPlanetSize = (float)oPlanet.Radius * 2 / 10;
                    MaxOrbitDistTest(ref dMaxOrbitDist, dPlanetOrbitRadius);
                    
                    GLUtilities.GLQuad oPlanetQuad = new GLUtilities.GLQuad(m_GLCanvas.DefaultShader,
                        v3PlanetPos,                                    
                        new Vector2(fPlanetSize, fPlanetSize),
                        Color.FromArgb(255, 0, 255, 0),  // lime green
                        UIConstants.Textures.DEFAULT_PLANET_ICON);
                    GLUtilities.GLCircle oPlanetOrbitCirc = new GLUtilities.GLCircle(m_GLCanvas.DefaultShader,
                        v3StarPos,                                                                      // base around parent star pos.
                        (float)dPlanetOrbitRadius / 2,
                        Color.FromArgb(255, 0, 255, 0),  // lime green
                        UIConstants.Textures.DEFAULT_TEXTURE);

                    oPlanetElement.AddPrimitive(oPlanetQuad);
                    oPlanetElement.AddPrimitive(oPlanetOrbitCirc);
                    oPlanetElement.PrimaryPrimitive = oPlanetQuad;
                    oPlanetElement.RealSize = new Vector2(fPlanetSize, fPlanetSize);
                    oCurrStar.AddChildElement(oPlanetElement);

                    iPlanetCounter++;

                    // now again for the moons:
                    foreach (Pulsar4X.Entities.Planet oMoon in oPlanet.Moons)
                    {
                        SceenElement oMoonElement = new PlanetElement();
                        oMoonElement.EntityID = oMoon.Id;

                        if (iMoonCounter == 0)
                        {
                            oPlanetElement.SmallestOrbit = (float)(oMoon.SemiMajorAxis * dKMperAUdevby10);
                        }

                        dMoonOrbitRadius = oMoon.SemiMajorAxis * dKMperAUdevby10;
                        fMoonSize = (float)oMoon.Radius * 2 / 10;

                        GLUtilities.GLQuad oMoonQuad = new GLUtilities.GLQuad(m_GLCanvas.DefaultShader,
                            new Vector3((float)dMoonOrbitRadius, 0, 0) + v3PlanetPos,                                    // offset Pos by parent planet pos
                            new Vector2(fMoonSize, fMoonSize),
                            Color.FromArgb(255, 0, 205, 0),  // lime green
                            UIConstants.Textures.DEFAULT_PLANET_ICON);
                        GLUtilities.GLCircle oMoonOrbitCirc = new GLUtilities.GLCircle(m_GLCanvas.DefaultShader,
                            v3PlanetPos,                                                                      // base around parent planet pos.
                            (float)dMoonOrbitRadius / 2,
                            Color.FromArgb(255, 0, 205, 0),  // lime green
                            UIConstants.Textures.DEFAULT_TEXTURE);

                        oMoonElement.AddPrimitive(oMoonQuad);
                        oMoonElement.AddPrimitive(oMoonOrbitCirc);
                        oMoonElement.PrimaryPrimitive = oMoonQuad;
                        oMoonElement.RealSize = new Vector2(fMoonSize, fMoonSize);
                        oPlanetElement.AddChildElement(oMoonElement);

                        iMoonCounter++;
                    }
                    iMoonCounter = 0;
                }
                iPlanetCounter = 0;
            }

            FitZoom(dMaxOrbitDist);
            oNewSceen.DefaultZoomScaler = m_oCurrentSceen.ZoomSclaer;

            oNewSceen.Refresh(); // force refresh.
            // Change Cursor Back to default.
            Cursor.Current = Cursors.Default;
        }

        private void SystemSelectComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
           // RefreshStarSystem(); // we only need to do this as the System is change automagicly by the data binding. not needed?????
        }

        private void UpdateScaleLabels()
        {
            double dKmscale = this.Size.Width / m_GLCanvas.ZoomFactor * 10;  // times by 10 to make scale the same as actual scale usid in drawing the systems.
            float dAUScale = (float)(dKmscale / Pulsar4X.Constants.Units.KM_PER_AU);
            KmScaleLabel.Text = "Km = " + dKmscale.ToString();
            AUScaleLabel.Text = "AU = " + dAUScale.ToString();
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
        /// Small Function that will test a current Max orbit against a possible replacment, it will change the current Max orbit if the test is larger.
        /// </summary>
        /// <param name="a_dCurrent">Current Max Orbit</param>
        /// <param name="a_dTest">Orbit to test against</param>
        private void MaxOrbitDistTest(ref double a_dCurrent, double a_dTest)
        {
            if (a_dCurrent < a_dTest)
            {
                a_dCurrent = a_dTest;
            }
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
            Point oCursorPosition = m_GLCanvas.PointToClient(Cursor.Position);

            // Convert to be world coords:
            Vector3 v3CurPosWorldCorrds = new Vector3((m_GLCanvas.Size.Width / 2) - oCursorPosition.X, (m_GLCanvas.Size.Height / 2) - oCursorPosition.Y, 0);
            v3CurPosWorldCorrds = v3CurPosWorldCorrds / m_GLCanvas.ZoomFactor;

            Guid oEntity = m_oCurrentSceen.GetElementAtCoords(v3CurPosWorldCorrds);
        }

        private void ResetViewButton_Click(object sender, EventArgs e)
        {
            m_GLCanvas.CenterOnZero();
            m_GLCanvas.ZoomFactor = m_oCurrentSceen.DefaultZoomScaler;
            m_oCurrentSceen.Refresh();
        }

        private void ZoomInButton_Click(object sender, EventArgs e)
        {
            m_GLCanvas.IncreaseZoomScaler();
            UpdateScaleLabels();
        }

        private void ZoomOutButton_Click(object sender, EventArgs e)
        {
            m_GLCanvas.DecreaseZoomScaler();
            UpdateScaleLabels();
        }

        private void PanRightButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(-10.0f, 0, 0);
            m_GLCanvas.Pan(ref v3PanAmount);
        }

        private void PanLeftButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(10.0f, 0, 0);
            m_GLCanvas.Pan(ref v3PanAmount);
        }

        private void PanUpButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(0, -10.0f, 0);
            m_GLCanvas.Pan(ref v3PanAmount);
        }

        private void PanDownButton_Click(object sender, EventArgs e)
        {
            Vector3 v3PanAmount = new Vector3(0, 10.0f, 0);
            m_GLCanvas.Pan(ref v3PanAmount);
        }

        


    }
}







//// test code only, just to see how bad the scale issue is.
//// for star color later: http://www.vendian.org/mncharity/dir3/starcolor/UnstableURLs/starcolors.html
//// or this http://www.vendian.org/mncharity/dir3/blackbody/UnstableURLs/tool_pl.txt 
//// and this http://www.vendian.org/mncharity/dir3/blackbody/UnstableURLs/bbr_color.html
//// or this: http://www.vendian.org/mncharity/dir3/starcolor
//// For right now to work around float percision and overflow issues 1 am scaling everthing down by a factor of 10.
//m_GLCanvas.RenderList.Clear(); // clear the render list!!

//// add star to centre of the map.
//int iCounter = 0;
//double dMaxOrbitDist = 0; // used for fit to zoom.
//foreach (Pulsar4X.Entities.Star oStar in m_oCurrnetSystem.Stars)
//{
//    // reset zoom factor:
//    // we need to do this to stop the problem of "big Planets" when changing systems with a non 1.0 zoom factor.
//    m_GLCanvas.ZoomFactor = 1.0f;

//    double dKMperAUdevby10 = (Pulsar4X.Constants.Units.KM_PER_AU / 10);

//    Vector3 v3StarPos = new Vector3(0, 0, 0);
//    if (iCounter > 0)
//    {
//        // then we have a secondary, etc star give random position around its orbit!
//        Random rnd = new Random();
//        float fAngle = rnd.Next(0, 360);
//        fAngle = MathHelper.DegreesToRadians(fAngle);
//        v3StarPos.X = (float)(Math.Cos(fAngle) * oStar.OrbitalRadius * dKMperAUdevby10);
//        v3StarPos.Y = (float)(Math.Sin(fAngle) * oStar.OrbitalRadius * dKMperAUdevby10);
//        MaxOrbitDistTest(ref dMaxOrbitDist, oStar.OrbitalRadius * dKMperAUdevby10);
//    }

//    float fSize = (float)oStar.Radius * 2 * 69550; // i.e. radius of sun / 10.

//    GLUtilities.GLQuad oStarQuad = new GLUtilities.GLQuad(m_GLCanvas.DefaultShader,
//                                                            v3StarPos,
//                                                            new Vector2(fSize, fSize), 
//                                                            Color.FromArgb(255, 255, 255, 0),    // yellow!
//                                                            UIConstants.Textures.DEFAULT_PLANET_ICON);
//    m_GLCanvas.AddToRenderList(oStarQuad);

//    // now go though and add each planet to render list.

//    foreach (Pulsar4X.Entities.Planet oPlanet in oStar.Planets)
//    {
//        double fOrbitRadius = oPlanet.SemiMajorAxis * dKMperAUdevby10;
//        float fPlanetSize = (float)oPlanet.Radius * 2 / 10;
//        MaxOrbitDistTest(ref dMaxOrbitDist, fOrbitRadius);
//        //if (fPlanetSize * m_GLCanvas.ZoomFactor < 16)
//       // {
//            // if we are too small, make us bigger for drawing!!
//           // fPlanetSize = fPlanetSize * 1000;
//        //}

//        GLUtilities.GLQuad oPlanetQuad = new GLUtilities.GLQuad(m_GLCanvas.DefaultShader,
//            new Vector3((float)fOrbitRadius, 0, 0) + v3StarPos,                                    // offset Pos by parent star pos
//            new Vector2(fPlanetSize, fPlanetSize),
//            Color.FromArgb(255, 50, 205, 50),  // lime green
//            UIConstants.Textures.DEFAULT_PLANET_ICON);
//        GLUtilities.GLCircle oPlanetOrbitCirc = new GLUtilities.GLCircle(m_GLCanvas.DefaultShader,
//            v3StarPos,                                                                      // base around parent star pos.
//            (float)fOrbitRadius, 
//            Color.FromArgb(255, 50, 205, 50),  // lime green
//            UIConstants.Textures.DEFAULT_TEXTURE);

//        m_GLCanvas.AddToRenderList(oPlanetQuad);
//        m_GLCanvas.AddToRenderList(oPlanetOrbitCirc);
//    }
//    // just do primary for now:
//    //break;
//    iCounter++;
//}
