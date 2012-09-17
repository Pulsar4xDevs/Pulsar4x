using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Pulsar4X;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.GLUtilities;
using OpenTK;
using Pulsar4X.Entities;

namespace Pulsar4X.WinForms.Controls.SceenGraph
{
    /// <summary>
    /// Root Node of a Sceen Graph
    /// </summary>
    public class Sceen
    {
        /// <summary>
        /// List of all top level Sprites That Make up the Sceen.
        /// </summary>
        private List<SceenElement> m_lElements = new List<SceenElement>();

        /// <summary>
        /// Gets a list of the Elements that make up the sceen.
        /// </summary>
        public List<SceenElement> Elements
        {
            get
            {
                return m_lElements;
            }
        }

        /// <summary> 
        /// The zoom scaler, make this smaller to zoom out, larger to zoom in.
        /// </summary>
        private float m_fZoomScaler = UIConstants.ZOOM_DEFAULT_SCALLER;

        /// <summary>
        /// Gets or Sets the ZoomScaler for the Sceen.
        /// </summary>
        public float ZoomSclaer
        {
            get
            {
                return m_fZoomScaler;
            }
            set
            {
                m_fZoomScaler = value;
            }
        }

        /// <summary>
        /// The Default Zoom Scaler for this Sceen.
        /// </summary>
        private float m_fDefaultZoomScaler = UIConstants.ZOOM_DEFAULT_SCALLER;

        /// <summary>
        /// Gets or sets the Default Zoom FDor this Sceen.
        /// </summary>
        public float DefaultZoomScaler
        {
            get
            {
                return m_fDefaultZoomScaler;
            }
            set
            {
                m_fDefaultZoomScaler = value;
            }
        }

        /// <summary> 
        /// The view offset, i.e. how much the view should be offset from 0, 0 
        /// </summary>
        private Vector3 m_v3ViewOffset = Vector3.Zero;

        public Vector3 ViewOffset
        {
            get
            {
                return m_v3ViewOffset;
            }
            set
            {
                m_v3ViewOffset = value;
            }
        }

        /// <summary>
        /// The Size of the Sceen. Most likly to be the diamater of the larges orbit.
        /// </summary>
        private Vector2d m_v2SceenSize = Vector2d.Zero;

        public Vector2d SceenSize
        {
            get
            {
                return m_v2SceenSize;
            }
        }

        /// <summary>
        /// The Sceen ID, this could be a system ID for example.
        /// </summary>
        public Guid SceenID { get; set; }

        /// <summary>
        /// The Entity this Sceen Repesents, e.g A StarSystem.
        /// </summary>
        private GameEntity m_oSceenEntity;

        /// <summary>
        /// Get The Entity This System Repesents.
        /// </summary>
        public GameEntity SceenEntity 
        {
            get
            {
                return m_oSceenEntity;
            }
        }


        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Sceen()
        {
        }

        public Sceen(StarSystem a_oStarSystem, GLShader a_oDefaultShader)
        {
            // Set Sceen Vars:
            m_oSceenEntity = a_oStarSystem;
            SceenID = a_oStarSystem.Id;

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
            Vector3 v3MoonPos = Vector3.Zero;                                   // Used to store the Moons Position.

            // start creating star branches in the sceen graph:
            SceenElement oRootStar;
            SceenElement oCurrStar;
            foreach (Pulsar4X.Entities.Star oStar in a_oStarSystem.Stars)
            {

                if (iStarCounter <= 0)
                {
                    // then we have a secondary, etc star give random position around its orbit!
                    oRootStar = new StarElement(oStar, true);
                    oCurrStar = oRootStar;
                }
                else
                {

                    Random rnd = new Random();
                    float fAngle = rnd.Next(0, 360);
                    fAngle = MathHelper.DegreesToRadians(fAngle);
                    v3StarPos.X = (float)(Math.Cos(fAngle) * oStar.SemiMajorAxis * dKMperAUdevby10);
                    v3StarPos.Y = (float)(Math.Sin(fAngle) * oStar.SemiMajorAxis * dKMperAUdevby10);
                    MaxOrbitDistTest(ref dMaxOrbitDist, oStar.SemiMajorAxis * dKMperAUdevby10);
                    oCurrStar = new StarElement(oStar, false);

                    // create orbit circle
                    GLUtilities.GLCircle oStarOrbitCirc = new GLUtilities.GLCircle(a_oDefaultShader,
                        Vector3.Zero,                                                                      // base around parent star pos.
                        (float)(oStar.SemiMajorAxis * dKMperAUdevby10) / 2,
                        Color.FromArgb(255, 255, 255, 0),  // yellow.
                        UIConstants.Textures.DEFAULT_TEXTURE);
                    oCurrStar.AddPrimitive(oStarOrbitCirc);
                }
                

                fStarSize = (float)(oStar.Radius * 2.0 * (Constants.Units.SOLAR_RADIUS_IN_KM / 10)); // i.e. radius of sun / 10.

                GLUtilities.GLQuad oStarQuad = new GLUtilities.GLQuad(a_oDefaultShader,
                                                                        v3StarPos,
                                                                        new Vector2(fStarSize, fStarSize),
                                                                        Color.FromArgb(255, 255, 255, 0),    // yellow!
                                                                        UIConstants.Textures.DEFAULT_PLANET_ICON);
                // create name lable:
                GLUtilities.GLFont oNameLable = new GLUtilities.GLFont(a_oDefaultShader,
                    new Vector3((float)(v3StarPos.X), (float)(v3StarPos.Y - (oStar.Radius * 69550)) - 280, 0),
                    new Vector2(11, 14), Color.White, UIConstants.Textures.DEFAULT_GLFONT, oStar.Name);

                oCurrStar.AddPrimitive(oStarQuad); // Add star icon to the Sceen element.
                oCurrStar.Lable = oNameLable;
                oCurrStar.PrimaryPrimitive = oStarQuad;
                oCurrStar.RealSize = new Vector2(fStarSize, fStarSize);
                this.AddElement(oCurrStar);

                // now go though and add each planet to render list.
                foreach (Pulsar4X.Entities.Planet oPlanet in oStar.Planets)
                {
                    SceenElement oPlanetElement = new PlanetElement(oPlanet);
                    oPlanetElement.EntityID = oPlanet.Id;

                    if (iPlanetCounter == 0)
                    {
                        oCurrStar.SmallestOrbit = (float)(oPlanet.SemiMajorAxis * Pulsar4X.Constants.Units.KM_PER_AU * 2);
                    }

                    dPlanetOrbitRadius = oPlanet.SemiMajorAxis * dKMperAUdevby10;
                    v3PlanetPos = new Vector3((float)dPlanetOrbitRadius, 0, 0) + v3StarPos; // offset Pos by parent star pos
                    fPlanetSize = (float)oPlanet.Radius * 2 / 10;
                    MaxOrbitDistTest(ref dMaxOrbitDist, dPlanetOrbitRadius);

                    GLUtilities.GLQuad oPlanetQuad = new GLUtilities.GLQuad(a_oDefaultShader,
                        v3PlanetPos,
                        new Vector2(fPlanetSize, fPlanetSize),
                        Color.FromArgb(255, 0, 255, 0),  // lime green
                        UIConstants.Textures.DEFAULT_PLANET_ICON);
                    GLUtilities.GLCircle oPlanetOrbitCirc = new GLUtilities.GLCircle(a_oDefaultShader,
                        v3StarPos,                                                                      // base around parent star pos.
                        (float)dPlanetOrbitRadius / 2,
                        Color.FromArgb(255, 0, 255, 0),  // lime green
                        UIConstants.Textures.DEFAULT_TEXTURE);
                    // create name lable:
                    GLUtilities.GLFont oPlanetNameLable = new GLUtilities.GLFont(a_oDefaultShader,
                        new Vector3((float)(v3PlanetPos.X), (float)(v3PlanetPos.Y - (oPlanet.Radius)) - 280, 0),
                        new Vector2(11, 14), Color.Green, UIConstants.Textures.DEFAULT_GLFONT, oPlanet.Name);

                    oPlanetElement.AddPrimitive(oPlanetQuad);
                    oPlanetElement.AddPrimitive(oPlanetOrbitCirc);
                    oPlanetElement.Lable = oPlanetNameLable;
                    oPlanetElement.PrimaryPrimitive = oPlanetQuad;
                    oPlanetElement.RealSize = new Vector2(fPlanetSize, fPlanetSize);
                    oCurrStar.AddChildElement(oPlanetElement);

                    iPlanetCounter++;

                    // now again for the moons:
                    foreach (Pulsar4X.Entities.Planet oMoon in oPlanet.Moons)
                    {
                        SceenElement oMoonElement = new PlanetElement(oMoon);
                        oMoonElement.EntityID = oMoon.Id;

                        if (iMoonCounter == 0)
                        {
                            oPlanetElement.SmallestOrbit = (float)(oMoon.SemiMajorAxis * dKMperAUdevby10);
                        }

                        dMoonOrbitRadius = oMoon.SemiMajorAxis * dKMperAUdevby10;
                        fMoonSize = (float)oMoon.Radius * 2 / 10;
                        v3MoonPos = new Vector3((float)dMoonOrbitRadius, 0, 0) + v3PlanetPos;

                        GLUtilities.GLQuad oMoonQuad = new GLUtilities.GLQuad(a_oDefaultShader,
                            v3MoonPos,                                    // offset Pos by parent planet pos
                            new Vector2(fMoonSize, fMoonSize),
                            Color.FromArgb(255, 0, 205, 0),  // lime green
                            UIConstants.Textures.DEFAULT_PLANET_ICON);
                        GLUtilities.GLCircle oMoonOrbitCirc = new GLUtilities.GLCircle(a_oDefaultShader,
                            v3PlanetPos,                                                                      // base around parent planet pos.
                            (float)dMoonOrbitRadius / 2,
                            Color.FromArgb(255, 0, 205, 0),  // lime green
                            UIConstants.Textures.DEFAULT_TEXTURE);
                        GLUtilities.GLFont oMoonNameLable = new GLUtilities.GLFont(a_oDefaultShader,
                        new Vector3((float)(v3MoonPos.X), (float)(v3MoonPos.Y - (oMoon.Radius)) - 280, 0),
                        new Vector2(11, 14), Color.LightGreen, UIConstants.Textures.DEFAULT_GLFONT, oMoon.Name);

                        oMoonElement.AddPrimitive(oMoonQuad);
                        oMoonElement.AddPrimitive(oMoonOrbitCirc);
                        oMoonElement.Lable = oMoonNameLable;
                        oMoonElement.PrimaryPrimitive = oMoonQuad;
                        oMoonElement.RealSize = new Vector2(fMoonSize, fMoonSize);
                        oPlanetElement.AddChildElement(oMoonElement);

                        iMoonCounter++;
                    }
                    iMoonCounter = 0;
                }
                iPlanetCounter = 0;
                iStarCounter++;
            }

            // Set Sceen Size basd on Max Orbit:
            m_v2SceenSize = new Vector2d(dMaxOrbitDist * 2, dMaxOrbitDist * 2);
        }

        /// <summary>
        /// Render the Sceen.
        /// </summary>
        public void Render()
        {
            foreach (SceenElement oElement in m_lElements)
            {
                oElement.Render();
            }
        }

        /// <summary>
        /// Refresh the Sceen
        /// </summary>
        public void Refresh()
        {
            foreach (SceenElement oElement in m_lElements)
            {
                oElement.Refresh(m_fZoomScaler);
            }
        }

        /// <summary>
        /// Add an element to the sceen
        /// </summary>
        /// <param name="a_oElement"> the Element to add.</param>
        public void AddElement(SceenElement a_oElement)
        {
            m_lElements.Add(a_oElement);
        }

        public Guid GetElementAtCoords(Vector3 a_v3Coords)
        {
            Guid oElementID = Guid.Empty;
            foreach (SceenElement oElement in m_lElements)
            {
                oElementID = oElement.GetSelected(a_v3Coords);

                if (oElementID != Guid.Empty)
                {
                    // we have found something, retur its ID:
                    return oElementID;
                }
            }

            return oElementID;
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
    }
}
