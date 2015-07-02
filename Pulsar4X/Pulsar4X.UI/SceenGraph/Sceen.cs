#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif
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
using Pulsar4X.Entities.Components;
using Pulsar4X.Helpers;


namespace Pulsar4X.UI.SceenGraph
{
    /// <summary>
    /// Root Node of a Sceen Graph
    /// </summary>
    public class Sceen
    {
        /// <summary>
        /// Sceen Logger:
        /// </summary>
#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(Sceen));
#endif

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
        /// Should Active Sensors be shown in the display.
        /// </summary>
        public bool ShowActives { get; set; }

        /// <summary>
        /// Should Passive Sensors be shown in the display.
        /// </summary>
        public bool ShowPassives { get; set; }

        /// <summary>
        /// What signature strength should the display show current passives as searching for. The size of the sensor bubble indicates the extent to which this signature strength can be detected.
        /// </summary>
        public int ShowPassiveSignatureRange { get; set; }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Sceen(Pulsar4X.UI.Handlers.SystemMap ParentSM)
        {
            MeasureMode = false;
            m_v3ViewOffset = Vector3.Zero;

            ParentSystemMap = ParentSM;

            ShowActives = true;
            ShowPassives = true;
            ShowPassiveSignatureRange = (int)Constants.SensorTN.DefaultPassiveSignature;
        }

        public Sceen(StarSystem a_oStarSystem, GLEffect a_oDefaultEffect, Pulsar4X.UI.Handlers.SystemMap ParentSM)
        {
            // set member vars:
            m_v3ViewOffset = Vector3.Zero;
            MeasureMode = false;

            ParentSystemMap = ParentSM;
            SceenDefaultEffect = a_oDefaultEffect;

            /// <summary>
            /// These have to be initialized before the contactElements are created as contactElement uses these.
            /// </summary>
            ShowActives = true;
            ShowPassives = true;
            ShowPassiveSignatureRange = (int)Constants.SensorTN.DefaultPassiveSignature;

            // Set Sceen Vars:
            m_oSceenEntity = a_oStarSystem;
            SceenID = a_oStarSystem.Id;

            // Create measurement element:
            m_oMeasurementElement = new MeasurementElement();
            m_oMeasurementElement.PrimaryPrimitive = new GLLine(a_oDefaultEffect, Vector3.Zero, new Vector2(1.0f, 1.0f), Color.Yellow, UIConstants.Textures.DEFAULT_TEXTURE);
            m_oMeasurementElement.AddPrimitive(m_oMeasurementElement.PrimaryPrimitive);
            m_oMeasurementElement.Lable = new GLUtilities.GLFont(a_oDefaultEffect, Vector3.Zero, UIConstants.DEFAULT_TEXT_SIZE, Color.Yellow, UIConstants.Textures.DEFAULT_GLFONT2, "");

            // Creat Working Vars:
            //double dKMperAUdevby10 = (Pulsar4X.Constants.Units.KmPerAu / 10); // we scale everthing down by 10 to avoid float buffer overflows.
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
                    oRootStar = new StarElement(oStar, a_oDefaultEffect, Vector3.Zero, Pulsar4X.Constants.StarColor.LookupColor(oStar), true);
                    oCurrStar = oRootStar;
                }
                else
                {
                    Random rnd = new Random();
                    v3StarPos.X = (float)(oStar.Position.X);
                    v3StarPos.Y = (float)(oStar.Position.Y);    
                    MaxOrbitDistTest(ref dMaxOrbitDist, oStar.Orbit.SemiMajorAxis);
                    oCurrStar = new StarElement(oStar, a_oDefaultEffect, v3StarPos, Pulsar4X.Constants.StarColor.LookupColor(oStar), false);
                }


                fStarSize = (float)(oStar.Radius * 2.0 * (Constants.Units.SolarRadiusInAu));

                GLUtilities.GLQuad oStarQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                                                                        v3StarPos,
                                                                        new Vector2(fStarSize, fStarSize),
                                                                        Pulsar4X.Constants.StarColor.LookupColor(oStar),
                                                                        UIConstants.Textures.DEFAULT_PLANET_ICON);
                // create name lable:
                GLUtilities.GLFont oNameLable = new GLUtilities.GLFont(a_oDefaultEffect,
                    new Vector3((float)(v3StarPos.X), (float)(v3StarPos.Y - (oStar.Radius / Constants.Units.KmPerAu)), 0),
                    UIConstants.DEFAULT_TEXT_SIZE, Color.White, UIConstants.Textures.DEFAULT_GLFONT2, oStar.Name);

                oCurrStar.AddPrimitive(oStarQuad); // Add star icon to the Sceen element.
                oCurrStar.Lable = oNameLable;
                oCurrStar.PrimaryPrimitive = oStarQuad;
                oCurrStar.RealSize = new Vector2(fStarSize, fStarSize);
                this.AddElement(oCurrStar);

                // now go though and add each planet to render list.
                foreach (Pulsar4X.Entities.SystemBody oPlanet in oStar.Planets)
                {
                    SceenElement oPlanetElement = new PlanetElement(a_oDefaultEffect, v3StarPos, oPlanet, Color.FromArgb(255, 0, 205, 0));
                    oPlanetElement.EntityID = oPlanet.Id;

                    if (iPlanetCounter == 0)
                    {
                        oCurrStar.SmallestOrbit = (float)(oPlanet.Orbit.SemiMajorAxis * 2);
                    }
                    dPlanetOrbitRadius = oPlanet.Orbit.SemiMajorAxis;

                    fPlanetSize = (float)((oPlanet.Radius * 2.0) / Constants.Units.KmPerAu);
                    MaxOrbitDistTest(ref dMaxOrbitDist, dPlanetOrbitRadius);

                    GLUtilities.GLQuad oPlanetQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                        v3PlanetPos,
                        new Vector2(fPlanetSize, fPlanetSize),
                        Color.FromArgb(255, 0, 255, 0),  // lime green
                        UIConstants.Textures.DEFAULT_PLANET_ICON);

                    // create name lable:
                    GLUtilities.GLFont oPlanetNameLable = new GLUtilities.GLFont(a_oDefaultEffect,
                        new Vector3((float)(v3PlanetPos.X), (float)(v3PlanetPos.Y - (oPlanet.Radius / Constants.Units.KmPerAu)), 0),
                        UIConstants.DEFAULT_TEXT_SIZE, Color.AntiqueWhite, UIConstants.Textures.DEFAULT_GLFONT2, oPlanet.Name);

                    oPlanetElement.AddPrimitive(oPlanetQuad);

                    oPlanetElement.Lable = oPlanetNameLable;
                    oPlanetElement.PrimaryPrimitive = oPlanetQuad;
                    oPlanetElement.RealSize = new Vector2(fPlanetSize, fPlanetSize);
                    oCurrStar.AddChildElement(oPlanetElement);

                    iPlanetCounter++;

                    // now again for the moons:
                    foreach (Pulsar4X.Entities.SystemBody oMoon in oPlanet.Moons)
                    {
                        SceenElement oMoonElement = new PlanetElement(a_oDefaultEffect, v3PlanetPos, oMoon, Color.FromArgb(255, 0, 205, 0));
                        oMoonElement.EntityID = oMoon.Id;

                        if (iMoonCounter == 0)
                        {
                            oPlanetElement.SmallestOrbit = (float)(oMoon.Orbit.SemiMajorAxis);
                        }

                        dMoonOrbitRadius = oMoon.Orbit.SemiMajorAxis;
                        fMoonSize = (float)((oMoon.Radius * 2.0) / Constants.Units.KmPerAu);
                        v3MoonPos = new Vector3((float)(oMoon.Position.X), (float)(oMoon.Position.Y), 0);
                        oMoon.Position.X = oMoon.Position.X + v3PlanetPos.X;
                        oMoon.Position.Y = oMoon.Position.Y + v3PlanetPos.Y;

                        GLUtilities.GLQuad oMoonQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                            v3MoonPos,                                    // offset Pos by parent planet pos
                            new Vector2(fMoonSize, fMoonSize),
                            Color.FromArgb(255, 0, 205, 0),  // lime green
                            UIConstants.Textures.DEFAULT_PLANET_ICON);

                        GLUtilities.GLFont oMoonNameLable = new GLUtilities.GLFont(a_oDefaultEffect,
                        new Vector3((float)(v3MoonPos.X), (float)(v3MoonPos.Y - (oMoon.Radius / Constants.Units.KmPerAu)), 0),
                        UIConstants.DEFAULT_TEXT_SIZE, Color.AntiqueWhite, UIConstants.Textures.DEFAULT_GLFONT2, oMoon.Name);

                        oMoonElement.AddPrimitive(oMoonQuad);

                        oMoonElement.Lable = oMoonNameLable;
                        oMoonElement.PrimaryPrimitive = oMoonQuad;
                        oMoonElement.RealSize = new Vector2(fMoonSize, fMoonSize);
                        oPlanetElement.AddChildElement(oMoonElement);

                        iMoonCounter++;
                    }
                    iMoonCounter = 0;
                }
                iPlanetCounter = 0;
                foreach (Pulsar4X.Entities.JumpPoint oJumpPoint in a_oStarSystem.JumpPoints)
                {
                    CreateJumpPoint(oCurrStar, oJumpPoint);
                }

                iStarCounter++;
            }

            foreach (Pulsar4X.Entities.SystemContact systemContact in a_oStarSystem.SystemContactList)
            {
                AddContactElement(SceenDefaultEffect, systemContact);
            }

            a_oStarSystem.JumpPoints.ListChanged += JumpPoints_ListChanged;
            a_oStarSystem.SystemContactList.ListChanging += SystemContactList_ListChanging;

            // Set Sceen Size basd on Max Orbit:
            m_v2SceenSize = new Vector2d(dMaxOrbitDist * 2, dMaxOrbitDist * 2);
        }

        private void SystemContactList_ListChanging(object sender, ListChangingEventArgs e)
        {
            VerboseBindingList<SystemContact> list = sender as VerboseBindingList<SystemContact>;

            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    AddContactElement(SceenDefaultEffect, e.ChangingObject as SystemContact);
                    break;
                case ListChangedType.ItemDeleted:
                    RemoveContactElement(SceenDefaultEffect, e.ChangingObject as SystemContact);
                    break;
            }
        }

        private void JumpPoints_ListChanged(object sender, ListChangedEventArgs e)
        {
            BindingList<JumpPoint> list = sender as BindingList<JumpPoint>;
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                JumpPoint jp = list[e.NewIndex];

                foreach (SceenElement element in Elements)
                {
                    if (element.SceenEntity as Star == jp.Parent)
                    {
                        CreateJumpPoint(element, jp);
                        break;
                    }
                }
            }
        }

        private void CreateJumpPoint(SceenElement parent, JumpPoint oJumpPoint)
        {
            SceenElement oJumpPointElement = new JumpPointElement(oJumpPoint);
            oJumpPointElement.EntityID = oJumpPoint.Id;

            Vector3 v3JPPos = new Vector3((float)oJumpPoint.Position.X, (float)oJumpPoint.Position.Y, 0.0f);

            GLQuad oJPQuad = new GLUtilities.GLQuad(SceenDefaultEffect,
                                                            v3JPPos,
                                                            new Vector2(0.0001f, 0.0001f),                   // what size is a jump point anyway???
                                                            Color.Cyan,
                                                            UIConstants.Textures.DEFAULT_JUMPPOINT_ICON);

            GLUtilities.GLFont oNameLable = new GLUtilities.GLFont(SceenDefaultEffect, v3JPPos,
            UIConstants.DEFAULT_TEXT_SIZE, Color.Cyan, UIConstants.Textures.DEFAULT_GLFONT2, oJumpPoint.Name);

            oJumpPointElement.Lable = oNameLable;
            oJumpPointElement.PrimaryPrimitive = oJPQuad;
            oJumpPointElement.AddPrimitive(oJPQuad);
            oJumpPointElement.RealSize = new Vector2(0.0001f, 0.0001f);
            parent.AddChildElement(oJumpPointElement);
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
                    TaskGroupTN TaskGroup = oContact.Entity as TaskGroupTN;
                    oContactElement = new ContactElement(a_oDefaultEffect, oContact);
                    oContactElement.EntityID = oContact.Id;

                    v3ContactPos = new Vector3((float)TaskGroup.Contact.Position.X, (float)TaskGroup.Contact.Position.Y, 0.0f);

                    oContactQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                                                                    v3ContactPos,
                                                                    new Vector2(0.0001f, 0.0001f),                   // what size is a task groug anyway???
                                                                    oContact.faction.FactionColor,
                                                                    UIConstants.Textures.DEFAULT_TASKGROUP_ICON);

                    oNameLable = new GLUtilities.GLFont(a_oDefaultEffect, v3ContactPos,
                    UIConstants.DEFAULT_TEXT_SIZE, oContact.faction.FactionColor, UIConstants.Textures.DEFAULT_GLFONT2, TaskGroup.Name);

                    oContactElement.Lable = oNameLable;
                    oContactElement.Lable.Size = UIConstants.DEFAULT_TEXT_SIZE / m_fZoomScaler; //Initial taskgroup names weren't being scaled properly for whatever reason.
                    oContactElement.PrimaryPrimitive = oContactQuad;
                    oContactElement.AddPrimitive(oContactQuad);
                    oContactElement.RealSize = new Vector2(0.0001f, 0.0001f);
                    this.AddElement(oContactElement);
                    (oContactElement as ContactElement).ParentSceen = this;
                    break;
                case StarSystemEntityType.Missile:
                    OrdnanceGroupTN MissileGroup = oContact.Entity as OrdnanceGroupTN;
                    oContactElement = new ContactElement(a_oDefaultEffect, oContact);
                    oContactElement.EntityID = oContact.Id;

                    v3ContactPos = new Vector3((float)MissileGroup.contact.Position.X, (float)MissileGroup.contact.Position.Y, 0.0f);

                    oContactQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                                                                v3ContactPos,
                                                                new Vector2(0.0001f, 0.0001f),                   // what size is a missile?
                                                                oContact.faction.FactionColor,
                                                                UIConstants.Textures.DEFAULT_TASKGROUP_ICON);

                    oNameLable = new GLUtilities.GLFont(a_oDefaultEffect, v3ContactPos,
                    UIConstants.DEFAULT_TEXT_SIZE, oContact.faction.FactionColor, UIConstants.Textures.DEFAULT_GLFONT2, MissileGroup.Name);

                    oContactElement.Lable = oNameLable;
                    oContactElement.Lable.Size = UIConstants.DEFAULT_TEXT_SIZE / m_fZoomScaler; //Same problem may exist with missile labels.
                    oContactElement.PrimaryPrimitive = oContactQuad;
                    oContactElement.AddPrimitive(oContactQuad);
                    oContactElement.RealSize = new Vector2(0.0001f, 0.0001f);
                    this.AddElement(oContactElement);
                    (oContactElement as ContactElement).ParentSceen = this;
                    break;
                case StarSystemEntityType.Population:
                    Population CurrentPopulation = oContact.Entity as Population;
                    oContactElement = new ContactElement(a_oDefaultEffect, oContact);
                    oContactElement.EntityID = oContact.Id;

                    v3ContactPos = new Vector3((float)CurrentPopulation.Contact.Position.X, (float)CurrentPopulation.Contact.Position.Y, 0.0f);

                    oContactQuad = new GLUtilities.GLQuad(a_oDefaultEffect,
                                                                v3ContactPos,
                                                                new Vector2(0.0001f, 0.0001f),                   // what size is a population?
                                                                oContact.faction.FactionColor,
                                                                UIConstants.Textures.DEFAULT_TASKGROUP_ICON);

                    oNameLable = new GLUtilities.GLFont(a_oDefaultEffect, v3ContactPos,
                    UIConstants.DEFAULT_TEXT_SIZE, oContact.faction.FactionColor, UIConstants.Textures.DEFAULT_GLFONT2, CurrentPopulation.Name);

                    oContactElement.Lable = oNameLable;
                    oContactElement.Lable.Size = UIConstants.DEFAULT_TEXT_SIZE / m_fZoomScaler; //Same problem may exist with population labels.
                    oContactElement.PrimaryPrimitive = oContactQuad;
                    oContactElement.AddPrimitive(oContactQuad);
                    oContactElement.RealSize = new Vector2(0.0001f, 0.0001f);
                    this.AddElement(oContactElement);
                    (oContactElement as ContactElement).ParentSceen = this;
                    break;
            }

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
                /// Update: as it turns out Id was not being declared anywhere.
                /// </summary>
                if (Ele.EntityID == oContact.Id)
                {
                    m_lElements.Remove(Ele);
                    break;
                }
            }

        }

        /// <summary>
        /// Public member for setting whether active sensors should be displayed.
        /// </summary>
        /// <param name="show">true or false</param>
        public void SetShowActives(bool show)
        {
            ShowActives = show;
            foreach (SceenElement oElement in m_lElements)
            {
                ContactElement cElement = oElement as ContactElement;
                if (cElement != null)
                    cElement.ForceSensorUpdate();
            }
        }

        /// <summary>
        /// Public member for setting whether passive sensors should be displayed.
        /// </summary>
        /// <param name="show">true or false</param>
        public void SetShowPassives(bool show)
        {
            ShowPassives = show;
            foreach (SceenElement oElement in m_lElements)
            {
                ContactElement cElement = oElement as ContactElement;
                if (cElement != null)
                    cElement.ForceSensorUpdate();
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
