using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Pulsar4X;
using Pulsar4X.UI;
using Pulsar4X.UI.GLUtilities;
using OpenTK;
using Pulsar4X.Entities;
using log4net.Config;
using log4net;

namespace Pulsar4X.UI.SceenGraph
{
    /// <summary>
    /// Root Node of a Sceen Graph
    /// </summary>
    public class Sceen
    {
        /// <summary>
        /// TG Logger:
        /// </summary>
        public static readonly ILog logger = LogManager.GetLogger(typeof(Sceen));

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

        private BindingList<MapMarker> m_lMapMarkers = new BindingList<MapMarker>();

        /// <summary>
        /// Gets a list of the Map Markers in the sceen.
        /// </summary>
        public BindingList<MapMarker> MapMarkers
        {
            get
            {
                return m_lMapMarkers;
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

        /// <summary>   Gets or sets a value indicating whether the measure mode is active. </summary>
        public bool MeasureMode { get; set; }

        /// <summary> Used to draw a measurement on the screen. </summary>
        private MeasurementElement m_oMeasurementElement;


        /// <summary>
        /// need a link to our parent for map marker updates
        /// </summary>
        public Pulsar4X.UI.Handlers.SystemMap ParentSystemMap { get; set; }

        /// <summary>
        /// Where is DefaultEffect coming from anyway? need this for contact element creation.
        /// </summary>
        public GLEffect SceenDefaultEffect { get; set; }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Sceen(Pulsar4X.UI.Handlers.SystemMap ParentSM)
        {
            MeasureMode = false;
            m_v3ViewOffset = Vector3.Zero;

            ParentSystemMap = ParentSM;
        }

        public Sceen(StarSystem a_oStarSystem, GLEffect a_oDefaultEffect, Pulsar4X.UI.Handlers.SystemMap ParentSM)
        {
            // set member vars:
            m_v3ViewOffset = Vector3.Zero;
            MeasureMode = false;

            ParentSystemMap = ParentSM;
            SceenDefaultEffect = a_oDefaultEffect;

            // Set Sceen Vars:
            m_oSceenEntity = a_oStarSystem;
            SceenID = a_oStarSystem.Id;
            
            // Create measurement element:
            m_oMeasurementElement = new MeasurementElement();
            m_oMeasurementElement.PrimaryPrimitive = new GLLine(a_oDefaultEffect, Vector3.Zero, new Vector2(1.0f, 1.0f), Color.Yellow, UIConstants.Textures.DEFAULT_TEXTURE);
            m_oMeasurementElement.AddPrimitive(m_oMeasurementElement.PrimaryPrimitive);
            m_oMeasurementElement.Lable = new GLUtilities.GLFont(a_oDefaultEffect, Vector3.Zero, UIConstants.DEFAULT_TEXT_SIZE, Color.Yellow, UIConstants.Textures.DEFAULT_GLFONT2, "");

            // Creat Working Vars:
            //double dKMperAUdevby10 = (Pulsar4X.Constants.Units.KM_PER_AU / 10); // we scale everthing down by 10 to avoid float buffer overflows.
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
                    //float fAngle = 0.0f; // rnd.Next(0, 360);
                    //fAngle = MathHelper.DegreesToRadians(fAngle);
                   // double x, y;
                    Pulsar4X.Lib.OrbitTable.Instance.UpdatePosition(oStar, 0);
                    v3StarPos.X = (float)(oStar.XSystem); //(float)(Math.Cos(fAngle) * oStar.SemiMajorAxis * dKMperAUdevby10);
                    v3StarPos.Y = (float)(oStar.YSystem);    //(float)(Math.Sin(fAngle) * oStar.SemiMajorAxis * dKMperAUdevby10);
                    MaxOrbitDistTest(ref dMaxOrbitDist, oStar.SemiMajorAxis);
                    oCurrStar = new StarElement(oStar, false);

                    // create orbit circle
                    GLUtilities.GLCircle oStarOrbitCirc = new GLUtilities.GLCircle(a_oDefaultEffect,
                        Vector3.Zero,                                                                      // base around parent star pos.
                        oStar, //(float)(oStar.SemiMajorAxis * dKMperAUdevby10) / 2,
                        Pulsar4X.Constants.StarColor.LookupColor(oStar.Class),
                        UIConstants.Textures.DEFAULT_TEXTURE);
                    oCurrStar.AddPrimitive(oStarOrbitCirc);
                }
                

                fStarSize = (float)(oStar.Radius * 2.0 * (Constants.Units.SOLAR_RADIUS_IN_AU));

                GLUtilities.GLQuad oStarQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                                                                        v3StarPos,
                                                                        new Vector2(fStarSize, fStarSize),
                                                                        Pulsar4X.Constants.StarColor.LookupColor(oStar.Class),
                                                                        UIConstants.Textures.DEFAULT_PLANET_ICON);
                // create name lable:
                GLUtilities.GLFont oNameLable = new GLUtilities.GLFont(a_oDefaultEffect,
                    new Vector3((float)(v3StarPos.X), (float)(v3StarPos.Y - (oStar.Radius / Constants.Units.KM_PER_AU)), 0),
                    UIConstants.DEFAULT_TEXT_SIZE, Color.White, UIConstants.Textures.DEFAULT_GLFONT, oStar.Name);

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
                        oCurrStar.SmallestOrbit = (float)(oPlanet.SemiMajorAxis * 2);
                    }

                    dPlanetOrbitRadius = oPlanet.SemiMajorAxis;
                    Pulsar4X.Lib.OrbitTable.Instance.UpdatePosition(oPlanet, 0);
                    v3PlanetPos = new Vector3((float)(oPlanet.XSystem), (float)(oPlanet.YSystem), 0) + v3StarPos; // offset Pos by parent star pos
                    fPlanetSize = (float)((oPlanet.Radius * 2.0) / Constants.Units.KM_PER_AU);
                    MaxOrbitDistTest(ref dMaxOrbitDist, dPlanetOrbitRadius);

                    GLUtilities.GLQuad oPlanetQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                        v3PlanetPos,
                        new Vector2(fPlanetSize, fPlanetSize),
                        Color.FromArgb(255, 0, 255, 0),  // lime green
                        UIConstants.Textures.DEFAULT_PLANET_ICON);
                    GLUtilities.GLCircle oPlanetOrbitCirc = new GLUtilities.GLCircle(a_oDefaultEffect,
                        v3StarPos,                                                                      // base around parent star pos.
                        oPlanet, //(float)dPlanetOrbitRadius / 2,
                        Color.FromArgb(255, 0, 205, 0),  // lime green
                        UIConstants.Textures.DEFAULT_TEXTURE);
                    // create name lable:
                    GLUtilities.GLFont oPlanetNameLable = new GLUtilities.GLFont(a_oDefaultEffect,
                        new Vector3((float)(v3PlanetPos.X), (float)(v3PlanetPos.Y - (oPlanet.Radius / Constants.Units.KM_PER_AU)), 0),
                        UIConstants.DEFAULT_TEXT_SIZE, Color.AntiqueWhite, UIConstants.Textures.DEFAULT_GLFONT, oPlanet.Name);

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
                            oPlanetElement.SmallestOrbit = (float)(oMoon.SemiMajorAxis);
                        }

                        dMoonOrbitRadius = oMoon.SemiMajorAxis;
                        Pulsar4X.Lib.OrbitTable.Instance.UpdatePosition(oMoon, 0);
                        fMoonSize = (float)((oMoon.Radius * 2.0) / Constants.Units.KM_PER_AU);
                        v3MoonPos = new Vector3((float)(oMoon.XSystem), (float)(oMoon.YSystem), 0) + v3PlanetPos;

                        GLUtilities.GLQuad oMoonQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                            v3MoonPos,                                    // offset Pos by parent planet pos
                            new Vector2(fMoonSize, fMoonSize),
                            Color.FromArgb(255, 0, 205, 0),  // lime green
                            UIConstants.Textures.DEFAULT_PLANET_ICON);
                        GLUtilities.GLCircle oMoonOrbitCirc = new GLUtilities.GLCircle(a_oDefaultEffect,
                            v3PlanetPos,                                                                      // base around parent planet pos.
                            oMoon, //(float)dMoonOrbitRadius / 2,
                            Color.FromArgb(255, 0, 205, 0),  // lime green
                            UIConstants.Textures.DEFAULT_TEXTURE);
                        GLUtilities.GLFont oMoonNameLable = new GLUtilities.GLFont(a_oDefaultEffect,
                        new Vector3((float)(v3MoonPos.X), (float)(v3MoonPos.Y - (oMoon.Radius / Constants.Units.KM_PER_AU)), 0),
                        UIConstants.DEFAULT_TEXT_SIZE, Color.AntiqueWhite, UIConstants.Textures.DEFAULT_GLFONT, oMoon.Name);

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

            // create any system contacts:
            foreach (Pulsar4X.Entities.SystemContact oContact in a_oStarSystem.SystemContactList)
            {
                SceenElement oContactElement;
                Vector3 v3ContactPos;
                GLUtilities.GLFont oNameLable;
                GLUtilities.GLQuad oContactQuad;

                switch(oContact.SSEntity)
                {
                    case StarSystemEntityType.TaskGroup:
                        oContactElement = new ContactElement(a_oDefaultEffect, oContact);
                        oContactElement.EntityID = oContact.Id;

                        v3ContactPos = new Vector3((float)oContact.TaskGroup.Contact.XSystem, (float)oContact.TaskGroup.Contact.YSystem, 0.0f);

                        oContactQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                                                                        v3ContactPos,
                                                                        new Vector2(0.0001f, 0.0001f),                   // what size is a task groug anyway???
                                                                        oContact.faction.FactionColor,                   
                                                                        UIConstants.Textures.DEFAULT_TASKGROUP_ICON);

                        oNameLable = new GLUtilities.GLFont(a_oDefaultEffect, v3ContactPos,
                        UIConstants.DEFAULT_TEXT_SIZE, oContact.faction.FactionColor, UIConstants.Textures.DEFAULT_GLFONT2, oContact.TaskGroup.Name);

                        oContactElement.Lable = oNameLable;
                        oContactElement.PrimaryPrimitive = oContactQuad;
                        oContactElement.AddPrimitive(oContactQuad);
                        oContactElement.RealSize = new Vector2(0.0001f, 0.0001f);
                        this.AddElement(oContactElement);
                        (oContactElement as ContactElement).ParentSceen = this;
                    break;
                    case StarSystemEntityType.Missile:
                        oContactElement = new ContactElement(a_oDefaultEffect, oContact);
                        oContactElement.EntityID = oContact.Id;

                        v3ContactPos = new Vector3((float)oContact.MissileGroup.contact.XSystem, (float)oContact.MissileGroup.contact.YSystem, 0.0f);

                        oContactQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                                                                    v3ContactPos,
                                                                    new Vector2(0.0001f, 0.0001f),                   // what size is a missile?
                                                                    oContact.faction.FactionColor,
                                                                    UIConstants.Textures.DEFAULT_TASKGROUP_ICON);

                        oNameLable = new GLUtilities.GLFont(a_oDefaultEffect, v3ContactPos,
                        UIConstants.DEFAULT_TEXT_SIZE, oContact.faction.FactionColor, UIConstants.Textures.DEFAULT_GLFONT2, oContact.MissileGroup.Name);

                        oContactElement.Lable = oNameLable;
                        oContactElement.PrimaryPrimitive = oContactQuad;
                        oContactElement.AddPrimitive(oContactQuad);
                        oContactElement.RealSize = new Vector2(0.0001f, 0.0001f);
                        this.AddElement(oContactElement);
                        (oContactElement as ContactElement).ParentSceen = this;
                    break;                     
                }

                oContact.ContactElementCreated = SystemContact.CEState.Created;
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

            foreach (SceenElement oElement in m_lMapMarkers)
            {
                oElement.Render();
            }

            if (MeasureMode == true)
            {
                m_oMeasurementElement.Render();
            }
        }

        /// <summary>
        /// Refresh the Sceen
        /// </summary>
        public void Refresh()
        {

            /// <summary>
            /// Kludge to create new contact elements to draw, or delete them as needed.
            /// </summary>
            StarSystem Sys = m_oSceenEntity as StarSystem;
            foreach (SystemContact oContact in Sys.SystemContactList)
            {
                if (oContact.ContactElementCreated == SystemContact.CEState.NotCreated)
                {
                    AddContactElement(SceenDefaultEffect, oContact);
                }
                else if (oContact.ContactElementCreated == SystemContact.CEState.Delete)
                {
                    RemoveContactElement(SceenDefaultEffect, oContact);
                }
            }

            foreach (SceenElement oElement in m_lElements)
            {
                oElement.Refresh(m_fZoomScaler);
            }

            foreach (SceenElement oElement in m_lMapMarkers)
            {
                oElement.Refresh(m_fZoomScaler);
            }

            m_oMeasurementElement.Refresh(m_fZoomScaler);
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

        public void SetMeasurementStartPos(Vector3 a_v3Pos)
        {
            m_oMeasurementElement.PrimaryPrimitive.Position = a_v3Pos;
            m_oMeasurementElement.Lable.Position = a_v3Pos;
        }

        public void SetMeasurementEndPos(Vector3 a_v3Pos, string a_szMeasure)
        {
            GLLine temp = m_oMeasurementElement.PrimaryPrimitive as GLLine;
            if (temp != null)
            {
                temp.PosEnd = a_v3Pos;
            }
            m_oMeasurementElement.Lable.Text = a_szMeasure;
        }

        public void AddMapMarker(Vector3 a_v3Pos, GLEffect a_oDefaultEffect)
        {
            MapMarker oMapMarker = new MapMarker();

            GLUtilities.GLQuad oMarkerQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                                                                        a_v3Pos,
                                                                        new Vector2(0.0001f, 0.0001f),
                                                                        Color.Tan,
                                                                        UIConstants.Textures.DEFAULT_PLANET_ICON);
            // create name lable:
            int m_Count = m_lMapMarkers.Count + 1;
            string name = "WP" + m_Count.ToString();
            GLUtilities.GLFont oNameLable = new GLUtilities.GLFont(a_oDefaultEffect, a_v3Pos,
                UIConstants.DEFAULT_TEXT_SIZE, Color.Tan, UIConstants.Textures.DEFAULT_GLFONT, name);

            oMapMarker.AddPrimitive(oMarkerQuad);
            oMapMarker.PrimaryPrimitive = oMarkerQuad;
            oMapMarker.Lable = oNameLable;

            oMapMarker.ParentSceen = this;

            m_lMapMarkers.Add(oMapMarker);

            Refresh();
        }

        /// <summary>
        /// creates a new post sceen creation contact element.
        /// </summary>
        /// <param name="a_oDefaultEffect">default effect, I don't know what these are really.</param>
        /// <param name="oContact">The system contact to be created.</param>
        public void AddContactElement(GLEffect a_oDefaultEffect, SystemContact oContact)
        {
            SceenElement oContactElement;
            Vector3 v3ContactPos;
            GLUtilities.GLFont oNameLable;
            GLUtilities.GLQuad oContactQuad;

            switch (oContact.SSEntity)
            {
                case StarSystemEntityType.TaskGroup:
                    oContactElement = new ContactElement(a_oDefaultEffect, oContact);
                    oContactElement.EntityID = oContact.Id;

                    v3ContactPos = new Vector3((float)oContact.TaskGroup.Contact.XSystem, (float)oContact.TaskGroup.Contact.YSystem, 0.0f);

                    oContactQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                                                                    v3ContactPos,
                                                                    new Vector2(0.0001f, 0.0001f),                   // what size is a task groug anyway???
                                                                    oContact.faction.FactionColor,
                                                                    UIConstants.Textures.DEFAULT_TASKGROUP_ICON);

                    oNameLable = new GLUtilities.GLFont(a_oDefaultEffect, v3ContactPos,
                    UIConstants.DEFAULT_TEXT_SIZE, oContact.faction.FactionColor, UIConstants.Textures.DEFAULT_GLFONT2, oContact.TaskGroup.Name);

                    oContactElement.Lable = oNameLable;
                    oContactElement.PrimaryPrimitive = oContactQuad;
                    oContactElement.AddPrimitive(oContactQuad);
                    oContactElement.RealSize = new Vector2(0.0001f, 0.0001f);
                    this.AddElement(oContactElement);
                    (oContactElement as ContactElement).ParentSceen = this;
                    break;
                case StarSystemEntityType.Missile:
                    oContactElement = new ContactElement(a_oDefaultEffect, oContact);
                    oContactElement.EntityID = oContact.Id;

                    v3ContactPos = new Vector3((float)oContact.MissileGroup.contact.XSystem, (float)oContact.MissileGroup.contact.YSystem, 0.0f);

                    oContactQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                                                                v3ContactPos,
                                                                new Vector2(0.0001f, 0.0001f),                   // what size is a missile?
                                                                oContact.faction.FactionColor,
                                                                UIConstants.Textures.DEFAULT_TASKGROUP_ICON);

                    oNameLable = new GLUtilities.GLFont(a_oDefaultEffect, v3ContactPos,
                    UIConstants.DEFAULT_TEXT_SIZE, oContact.faction.FactionColor, UIConstants.Textures.DEFAULT_GLFONT2, oContact.MissileGroup.Name);

                    oContactElement.Lable = oNameLable;
                    oContactElement.PrimaryPrimitive = oContactQuad;
                    oContactElement.AddPrimitive(oContactQuad);
                    oContactElement.RealSize = new Vector2(0.0001f, 0.0001f);
                    this.AddElement(oContactElement);
                    (oContactElement as ContactElement).ParentSceen = this;
                    break;
            }

            oContact.ContactElementCreated = SystemContact.CEState.Created;
        }

        /// <summary>
        /// Removes a contact element from the display. This is controlled by SystemContact.ContactElementCreated.
        /// </summary>
        /// <param name="a_oDefaultEffect"></param>
        /// <param name="oContact"></param>
        public void RemoveContactElement(GLEffect a_oDefaultEffect, SystemContact oContact)
        {
            foreach (SceenElement Ele in m_lElements)
            {
                /// <summary>
                /// Have to use Guid to identify elements and get rid of the one we no longer want.
                /// </summary>
                if (Ele.EntityID == oContact.Id)
                {
                    m_lElements.Remove(Ele);
                    oContact.ContactElementCreated = SystemContact.CEState.NotCreated;
                    break;
                }
            }
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
