using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;
using Pulsar4X.Stargen;
using Newtonsoft.Json;
using log4net.Config;
using log4net;
using Pulsar4X.Entities.Components;
using System.Runtime.InteropServices;

namespace Pulsar4X.UI.Handlers
{
    public class Components
    {
        public static readonly ILog logger = LogManager.GetLogger(typeof(Components));

        Panels.Component_Design m_oComponentDesignPanel;

        /// <summary>
        /// The view model for components.
        /// </summary>
        public ComponentsViewModel VM { get; set; }


        /// <summary>
        /// Currently selected faction.
        /// </summary>
        private Faction _CurrnetFaction;
        public Faction CurrentFaction
        {
            get { return _CurrnetFaction; }
            set
            {
                _CurrnetFaction = value;

                if (_CurrnetFaction == null)
                {
                    return;
                }

                _CurrnetComponent = ComponentsViewModel.Components.ActiveMFC;
            }
        }

        /// <summary>
        /// The Currently selected tech project.
        /// </summary>
        private ComponentsViewModel.Components _CurrnetComponent;
        public ComponentsViewModel.Components CurrentComponent
        {
            get { return _CurrnetComponent; }
            set
            {
                _CurrnetComponent = value;

                if (_CurrnetComponent < 0 || _CurrnetComponent >= ComponentsViewModel.Components.Count)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// These are the componentdefTNs for each research project.
        /// </summary>
        private ActiveSensorDefTN ActiveSensorProject;
        private PassiveSensorDefTN PassiveSensorProject;
        private BeamFireControlDefTN BeamFCProject;
        private ReactorDefTN ReactorProject;
        private ShieldDefTN ShieldProject;
        private EngineDefTN EngineProject;
        private BeamDefTN BeamProject;

        private IntPtr eventMask;

        [DllImport("user32", CharSet = CharSet.Auto)]
        private extern static IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

        public Components()
        {
            eventMask = IntPtr.Zero;

            m_oComponentDesignPanel = new Panels.Component_Design();

            VM = new ComponentsViewModel();

            /// <summary>
            /// Bind factions to the empire selection combo box.
            /// </summary>
            m_oComponentDesignPanel.FactionComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oComponentDesignPanel.FactionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oComponentDesignPanel.FactionComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => _CurrnetFaction = VM.CurrentFaction;
            _CurrnetFaction = VM.CurrentFaction;
            m_oComponentDesignPanel.FactionComboBox.SelectedIndexChanged += (s, args) => m_oComponentDesignPanel.FactionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oComponentDesignPanel.FactionComboBox.SelectedIndexChanged += new EventHandler(FactionComboBox_SelectedIndexChanged);


            /// <summary>
            /// Load the tech names. Commented out code commented out because it seizes control of the control away from the user for reasons I don't understand.
            /// </summary>
            m_oComponentDesignPanel.ResearchComboBox.Bind(c => c.DataSource, VM, d => d.RPTechs);
            //m_oComponentDesignPanel.ResearchComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentComponent, DataSourceUpdateMode.OnPropertyChanged);
            //m_oComponentDesignPanel.ResearchComboBox.DisplayMember = "Name";
            VM.ComponentChanged += (s, args) => _CurrnetComponent = VM.CurrentComponent;
            _CurrnetComponent = VM.CurrentComponent;
            //m_oComponentDesignPanel.ResearchComboBox.SelectedIndexChanged += (s, args) => m_oComponentDesignPanel.ResearchComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oComponentDesignPanel.ResearchComboBox.SelectedIndexChanged += new EventHandler(ResearchComboBox_SelectedIndexChanged);

            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndexChanged   += new EventHandler(TechComboBox_SelectedIndexChanged);
            m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndexChanged   += new EventHandler(TechComboBox_SelectedIndexChanged);
            m_oComponentDesignPanel.TechComboBoxThree.SelectedIndexChanged += new EventHandler(TechComboBox_SelectedIndexChanged);
            m_oComponentDesignPanel.TechComboBoxFour.SelectedIndexChanged  += new EventHandler(TechComboBox_SelectedIndexChanged);
            m_oComponentDesignPanel.TechComboBoxFive.SelectedIndexChanged  += new EventHandler(TechComboBox_SelectedIndexChanged);
            m_oComponentDesignPanel.TechComboBoxSix.SelectedIndexChanged   += new EventHandler(TechComboBox_SelectedIndexChanged);
            m_oComponentDesignPanel.TechComboBoxSeven.SelectedIndexChanged += new EventHandler(TechComboBox_SelectedIndexChanged);

            m_oComponentDesignPanel.CloseButton.Click += new EventHandler(CloseButton_Click);

            m_oComponentDesignPanel.SizeTonsCheckBox.Click += new EventHandler(SizeTonsCheckBox_Click);

            m_oComponentDesignPanel.InstantButton.Click += new EventHandler(InstantButton_Click);

            //m_oComponentDesignPanel.ResizeBegin +=new EventHandler(ResizeBegin);
            //m_oComponentDesignPanel.ResizeEnd += new EventHandler(ResizeEnd);

            ActiveSensorProject = null;
            PassiveSensorProject = null;
            BeamFCProject = null;
            ReactorProject = null;
            ShieldProject = null;
            EngineProject = null;
            BeamProject = null;
        }

        /// <summary>
        /// Opens as a popup the RP creation page.
        /// </summary>
        public void Popup()
        {
            m_oComponentDesignPanel.ShowDialog();
        }

        /// <summary>
        /// Space master on, bring in the instant button.
        /// </summary>
        public void SMOn()
        {
            m_oComponentDesignPanel.InstantButton.Visible = true;
            m_oComponentDesignPanel.InstantButton.Enabled = true;
        }

        /// <summary>
        /// Space master off, get rid of the instant button.
        /// </summary>
        public void SMOff()
        {
            m_oComponentDesignPanel.InstantButton.Visible = false;
            m_oComponentDesignPanel.InstantButton.Enabled = false;
        }

        /*
         
        private const int WM_SETREDRAW      = 0x000B;
        private const int WM_USER           = 0x400;
        private const int EM_GETEVENTMASK   = (WM_USER + 59);
        private const int EM_SETEVENTMASK   = (WM_USER + 69);
         * 
         * m_oDetailsPanel.SelectedActiveComboBox.SelectedIndex = -1;
            m_oDetailsPanel.SelectedActiveComboBox.Items.Clear();

         */
        /// <summary>
        /// If a user drags the window somewhere, this fires on the start of that. I want to improve performance with these, but it doesn't work without wrecking the display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeBegin(object sender, EventArgs e)
        {
            // Stop redrawing:
            //SendMessage(m_oComponentDesignPanel.Handle, 0x000B, 0, IntPtr.Zero);
            // Stop sending of events:
            //eventMask = SendMessage(m_oComponentDesignPanel.Handle, 0x459, 0, IntPtr.Zero);

        }

        /// <summary>
        /// When the user is done moving the window start drawing and events again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeEnd(object sender, EventArgs e)
        {
            // turn on events
            //SendMessage(m_oComponentDesignPanel.Handle, 0x469, 0, eventMask);

            // turn on redrawing
            //SendMessage(m_oComponentDesignPanel.Handle, 0x000B, 1, IntPtr.Zero);

            //m_oComponentDesignPanel.Invalidate();
            //m_oComponentDesignPanel.Refresh();
        }

        /// <summary>
        /// On faction change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FactionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _CurrnetComponent = ComponentsViewModel.Components.ActiveMFC;
            m_oComponentDesignPanel.ResearchComboBox.SelectedIndex = 0;

            BuildBackgroundTech();
        }

        /// <summary>
        /// On Selected index changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResearchComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            BuildBackgroundTech();
        }

        /// <summary>
        /// On any of the tech combo boxes being changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TechComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            BuildSystemParameters();
        }

        /// <summary>
        /// Closes the dialogbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_Click(object sender, EventArgs e)
        {
            m_oComponentDesignPanel.Close();
        }

        /// <summary>
        /// Sets display of tonnage from HS to Tons if clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SizeTonsCheckBox_Click(object sender, EventArgs e)
        {
            BuildSystemParameters();
        }

        /// <summary>
        /// Instant Adds the current tech project as a component to the faction component list, well, instantly.
        /// Clicking Instant on an undefined component will break this.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstantButton_Click(object sender, EventArgs e)
        {
            #region InstantButton Switch
            switch ((ComponentsViewModel.Components)m_oComponentDesignPanel.ResearchComboBox.SelectedIndex)
            {
                #region Active Sensors / MFC
                case ComponentsViewModel.Components.ActiveMFC:

                    if(ActiveSensorProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        ActiveSensorProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;

                    if (m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex == 0)
                        _CurrnetFaction.ComponentList.ActiveSensorDef.Add(ActiveSensorProject);
                    else
                        _CurrnetFaction.ComponentList.MissileFireControlDef.Add(ActiveSensorProject);
                break;
                #endregion

                #region Beam Fire Control
                case ComponentsViewModel.Components.BFC:

                    if (BeamFCProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        BeamFCProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.BeamFireControlDef.Add(BeamFCProject);

                break;
                #endregion

                #region CIWS
                case ComponentsViewModel.Components.CIWS:
                break;
                #endregion

                #region Cloak
                case ComponentsViewModel.Components.Cloak:
                break;
                #endregion

                #region EM
                case ComponentsViewModel.Components.EM:

                    if (PassiveSensorProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        PassiveSensorProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.PassiveSensorDef.Add(PassiveSensorProject);

                break;
                #endregion

                #region Engines
                case ComponentsViewModel.Components.Engine:

                if (EngineProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                    EngineProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                _CurrnetFaction.ComponentList.Engines.Add(EngineProject);
                break;
                #endregion

                #region Gauss Cannon
                case ComponentsViewModel.Components.Gauss:

                    if (BeamProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        BeamProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.BeamWeaponDef.Add(BeamProject);

                break;
                #endregion

                #region High Power Microwave
                case ComponentsViewModel.Components.Microwave:

                    if (BeamProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        BeamProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.BeamWeaponDef.Add(BeamProject);

                break;
                #endregion

                #region Jump Engines
                case ComponentsViewModel.Components.Jump:
                break;
                #endregion

                #region Lasers
                case ComponentsViewModel.Components.Laser:

                    if (BeamProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        BeamProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.BeamWeaponDef.Add(BeamProject);

                break;
                #endregion

                #region Magazines
                case ComponentsViewModel.Components.Magazine:
                break;
                #endregion

                #region Meson Cannons
                case ComponentsViewModel.Components.Meson:

                    if (BeamProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        BeamProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.BeamWeaponDef.Add(BeamProject);

                break;
                #endregion

                #region Missile Engines
                case ComponentsViewModel.Components.MissileEngine:
                break;
                #endregion

                #region Missile Launcher
                case ComponentsViewModel.Components.MissileLauncher:
                break;
                #endregion

                #region New Species
                case ComponentsViewModel.Components.NewSpecies:
                break;
                #endregion

                #region Particle beams
                case ComponentsViewModel.Components.Particle:

                    if (BeamProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        BeamProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.BeamWeaponDef.Add(BeamProject);

                break;
                #endregion

                #region Plasma Carronades
                case ComponentsViewModel.Components.Plasma:

                    if (BeamProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        BeamProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.BeamWeaponDef.Add(BeamProject);
                break;
                #endregion

                #region Plasma Torpedos
                case ComponentsViewModel.Components.PlasmaTorp:
                break;
                #endregion

                #region Power Plants
                case ComponentsViewModel.Components.Reactor:

                    if (ReactorProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        ReactorProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.ReactorDef.Add(ReactorProject);

                break;
                #endregion

                #region Railguns
                case ComponentsViewModel.Components.Rail:
                    if (BeamProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        BeamProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.BeamWeaponDef.Add(BeamProject);
                break;
                #endregion

                #region Absorption Shields
                case ComponentsViewModel.Components.ShieldAbs:
                break;
                #endregion

                #region Standard Shields
                case ComponentsViewModel.Components.ShieldStd:

                    if (ShieldProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        ShieldProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.ShieldDef.Add(ShieldProject);

                break;
                #endregion

                #region Thermal Sensors
                case ComponentsViewModel.Components.Thermal:

                    if (PassiveSensorProject.Name != m_oComponentDesignPanel.TechNameTextBox.Text)
                        PassiveSensorProject.Name = m_oComponentDesignPanel.TechNameTextBox.Text;
                    _CurrnetFaction.ComponentList.PassiveSensorDef.Add(PassiveSensorProject);

                break;
                #endregion
            }
            #endregion

            _CurrnetFaction.ComponentList.TotalComponents = _CurrnetFaction.ComponentList.TotalComponents + 1;
        }


        /// <summary>
        /// Builds the display for the background technology group box. Sanity checking is done here as the all techs function sets every tech level to 100 rather than its respective max.
        /// </summary>
        private void BuildBackgroundTech()
        {
            String Entry = "N/A";
            int TechLevel = -1;

            if (m_oComponentDesignPanel.ResearchComboBox.SelectedIndex != -1)
            {

                #region BuildBackgroundTech Switch
                switch ((ComponentsViewModel.Components)m_oComponentDesignPanel.ResearchComboBox.SelectedIndex)
                {
                    #region Active Sensors / MFC
                    case ComponentsViewModel.Components.ActiveMFC:

                        SetTechVisible(6);

                        SetLabels("Active Sensor Strength","EM Sensor Sensitivity","Total Sensor Size","Minimum Resolution","Hardening","Active Sensor Type","");

                        m_oComponentDesignPanel.NotesLabel.Text = "Sensor Range = Active Strength x Size x SQRT(Resolution) x EM Sensitivity x 10,000 km\nMissile Fire Controls multiply this by 3 in exchange for only being able to guide missiles, while losing the ability to perform active scanning.";


                        /// <summary>
                        /// Active Sensor tech listing
                        /// </summary>
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ActiveSensorStrength];
                        if (TechLevel >= 0)
                        {
                            if(TechLevel > 11)
                                TechLevel = 11;

                            for (int loop = TechLevel; loop >= 0; loop--)
                            {
                                Entry = String.Format("Active Sensor Strength {0}", Constants.SensorTN.ActiveStrength[loop]);
                                m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                            }

                            if (m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                                m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;
                        }

                        /// <summary>
                        /// EM sensitivity tech listing.
                        /// </summary>
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EMSensorSensitivity];
                        if (TechLevel >= 0)
                        {
                            if(TechLevel > 11)
                                TechLevel = 11;

                            for (int loop = TechLevel; loop >= 0; loop--)
                            {
                                Entry = String.Format("EM Sensor Sensitivity {0}", Constants.SensorTN.PassiveStrength[loop]);
                                m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                            }

                            m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;
                        }

                        /// <summary>
                        /// Overall size from 0.1 to 50
                        /// </summary>
                        for (int loop = 1; loop < 10; loop++)
                        {
                            Entry = String.Format("0.{0}", loop);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        for (int loop = 0; loop < 10; loop += 2)
                        {
                            Entry = "N/A";
                            if (loop == 0)
                                Entry = "1";
                            else
                                Entry = String.Format("1.{0}", loop);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        for (int loop = 2; loop < 5; loop++)
                        {
                            for (int loop2 = 0; loop2 < 100; loop2 += 25)
                            {
                                Entry = "N/A";
                                if (loop2 == 0)
                                    Entry = String.Format("{0}", loop);
                                else
                                    Entry = String.Format("{0}.{1}", loop, loop2);

                                m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                            }
                        }

                        for (int loop = 5; loop < 51; loop++)
                        {
                            Entry = String.Format("{0}", loop);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 9;

                        /// <summary>
                        /// Resolution for sensor listing.
                        /// </summary>
                        for (int loop = 1; loop <= 20; loop++)
                        {
                            Entry = String.Format("{0} tons ({1} HS)", (loop * 50), loop);
                            m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        }

                        for (int loop = 25; loop <= 200; loop+=5)
                        {
                            Entry = String.Format("{0} tons ({1} HS)", (loop * 50), loop);
                            m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        }

                        for (int loop = 220; loop <= 500; loop += 20)
                        {
                            Entry = String.Format("{0} tons ({1} HS)", (loop * 50), loop);
                            m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex = 35;

                        /// <summary>
                        /// Electronic hardening tech listing.
                        /// </summary>
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.Hardening];
                        if (TechLevel >= 0)
                        {
                            if (TechLevel > 8)
                                TechLevel = 8;

                            for (int loop = 0; loop <= TechLevel; loop++)
                            {
                                Entry = String.Format("Electronic Hardening Level {0}", loop);
                                m_oComponentDesignPanel.TechComboBoxFive.Items.Add(Entry);
                            }

                            m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex = 0;
                        }

                        m_oComponentDesignPanel.TechComboBoxSix.Items.Add("Search Sensor");
                        m_oComponentDesignPanel.TechComboBoxSix.Items.Add("Missile Fire Control");
                        m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex = 0;

                        
                    break;
                    #endregion

                    #region Beam Fire Control
                    case ComponentsViewModel.Components.BFC:

                        SetTechVisible(7);

                        SetLabels("Beam Fire Control Distance Rating","Fire Control Speed Rating","Fire Control Size vs Range","Fire Control Size vs Tracking Speed",
                                  "Hardening","Platform Type","Ship Type Limitations");

                        m_oComponentDesignPanel.NotesLabel.Text = "Note that the minimum range for combat is assumed to be 10,000 km. All to hit chances will be calculated using that as a minimum range so fire controls with a lower range than 10,000 km will be ineffective.";

                        /// <summary>
                        /// Beam Fire Control Range Section.
                        /// </summary>
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.BeamFireControlRange];

                        if (TechLevel >= 0)
                        {
                            if (TechLevel > 11)
                                TechLevel = 11;

                            for (int loop = TechLevel; loop >= 0; loop--)
                            {
                                Entry = String.Format("Beam Fire Control Range {0},000 km", Constants.BFCTN.BeamFireControlRange[loop]);
                                m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                            }

                            if (m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                                m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;
                        }

                        /// <summary>
                        /// Beam Fire Control Tracking Section.
                        /// </summary>
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.BeamFireControlTracking];

                        if (TechLevel >= 0)
                        {
                            if (TechLevel > 11)
                                TechLevel = 11;

                            for (int loop = TechLevel; loop >= 0; loop--)
                            {
                                int Tracking = Constants.BFCTN.BeamFireControlTracking[loop];
                                String TrackForm;
                                if (Tracking >= 10000)
                                    TrackForm = Tracking.ToString("#,##0");
                                else
                                    TrackForm = Tracking.ToString();
                                Entry = String.Format("Beam Fire Control Range {0} km", TrackForm);
                                m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                            }

                            if (m_oComponentDesignPanel.TechComboBoxTwo.Items.Count != 0)
                                m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;
                        }

                        /// <summary>
                        /// Size vs Range Component:
                        /// </summary
                        Entry = "Normal Size Normal Range";
                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        Entry = "Fire Control 1.5x Size 1.5x Range";
                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        Entry = "Fire Control 2x Size 2x Range";
                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        Entry = "Fire Control 3x Size 3x Range";
                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        Entry = "Fire Control 50% Size 50% Range";
                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        Entry = "Fire Control 4x Size 4x Range";
                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        Entry = "Fire Control 34% Size 34% Range";
                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        Entry = "Fire Control 25% Size 25% Range";
                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;

                        /// <summary>
                        /// Size vs Tracking Component:
                        /// </summary>
                        Entry = "Normal Size Normal Speed";
                        m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        Entry = "Fire Control 1.25x Size 1.25x Tracking Speed";
                        m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        Entry = "Fire Control 50% Size 50% Tracking Speed";
                        m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        Entry = "Fire Control 1.5x Size 1.5x Tracking Speed";
                        m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        Entry = "Fire Control 2x Size 2x Tracking Speed";
                        m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        Entry = "Fire Control 3x Size 3x Tracking Speed";
                        m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        Entry = "Fire Control 4x Size 4x Tracking Speed";
                        m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex = 0;

                        /// <summary>
                        /// Electronic hardening tech listing.
                        /// </summary>
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.Hardening];
                        if (TechLevel >= 0)
                        {
                            if (TechLevel > 8)
                                TechLevel = 8;

                            for (int loop = 0; loop <= TechLevel; loop++)
                            {
                                Entry = String.Format("Electronic Hardening Level {0}", loop);
                                m_oComponentDesignPanel.TechComboBoxFive.Items.Add(Entry);
                            }

                            m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex = 0;
                        }

                        /// <summary>
                        /// Is this a PDC FC or a ship FC?
                        /// </summary>
                        Entry = "Ship-based System";
                        m_oComponentDesignPanel.TechComboBoxSix.Items.Add(Entry);
                        Entry = "PDC-based System";
                        m_oComponentDesignPanel.TechComboBoxSix.Items.Add(Entry);
                        m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex = 0;

                        /// <summary>
                        /// Is this a fighter BFC?
                        /// </summary>
                        Entry = "No Restrictions";
                        m_oComponentDesignPanel.TechComboBoxSeven.Items.Add(Entry);
                        Entry = "Fighter Only";
                        m_oComponentDesignPanel.TechComboBoxSeven.Items.Add(Entry);
                        m_oComponentDesignPanel.TechComboBoxSeven.SelectedIndex = 0;
                    break;
                    #endregion

                    #region CIWS
                    case ComponentsViewModel.Components.CIWS:

                        SetTechVisible(6);

                        SetLabels("Gauss Cannon Rate of Fire", "Beam Fire Control Distance Rating", "Fire Control Speed Rating", "Active Sensor Strength", "Turret Rotation Gear", "ECCM", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Close in Weapons Systems are self contained and do not need support from other components to fully function. They also do not flag a vessel as military, but are only capable of protecting the ship that they are on.";


                    break;
                    #endregion

                    #region Cloak
                    case ComponentsViewModel.Components.Cloak:

                        SetTechVisible(3);

                        SetLabels("Cloaking Efficiency", "Cloaking Sensor Reduction", "Minimum Cloak Size", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Cloaks are always on, and only reduce the Total Cross Section of the vessel that they are on in addition to being quite large.";

                    break;
                    #endregion

                    #region EM
                    case ComponentsViewModel.Components.EM:

                        SetTechVisible(3);

                        SetLabels("EM Sensor Sensitivity", "Total Sensor Size", "Hardening", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Rating is EM Sensor Sensitivity x Size. This is the distance in Millions of Km that a signature of 1000 may be detected. smaller or larger signatures are detected by (Signature/1000) * Rating. " +
                                                                  "Not all ships will emit an EM signature, but those that do tend to be very large.";

                        /// <summary>
                        /// EM sensitivity tech listing.
                        /// </summary>
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EMSensorSensitivity];
                        if (TechLevel >= 0)
                        {
                            if (TechLevel > 11)
                                TechLevel = 11;

                            for (int loop = TechLevel; loop >= 0; loop--)
                            {
                                Entry = String.Format("EM Sensor Sensitivity {0}", Constants.SensorTN.PassiveStrength[loop]);
                                m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                            }

                            
                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;
                        }

                        /// <summary>
                        /// Overall size from 0.1 to 50
                        /// </summary>
                        for (int loop = 1; loop < 10; loop++)
                        {
                            Entry = String.Format("0.{0}", loop);
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        for (int loop = 0; loop < 10; loop += 2)
                        {
                            Entry = "N/A";
                            if (loop == 0)
                                Entry = "1";
                            else
                                Entry = String.Format("1.{0}", loop);
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        for (int loop = 2; loop < 5; loop++)
                        {
                            for (int loop2 = 0; loop2 < 100; loop2 += 25)
                            {
                                Entry = "N/A";
                                if (loop2 == 0)
                                    Entry = String.Format("{0}", loop);
                                else
                                    Entry = String.Format("{0}.{1}", loop, loop2);

                                m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                            }
                        }

                        for (int loop = 5; loop < 51; loop++)
                        {
                            Entry = String.Format("{0}", loop);
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 9;

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.Hardening];
                        if (TechLevel >= 0)
                        {
                            if (TechLevel > 8)
                                TechLevel = 8;

                            for (int loop = 0; loop <= TechLevel; loop++)
                            {
                                Entry = String.Format("Electronic Hardening Level {0}", loop);
                                m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                            }

                            m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;
                        }

                    break;
                    #endregion

                    #region Engines
                    case ComponentsViewModel.Components.Engine:

                        SetTechVisible(6);

                        SetLabels("Engine Technology", "Power / Efficiency Modifers", "Fuel Consumption", "Thermal Reduction", "Engine Size", "Hyper Drive Efficiency", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Fuel Modifier = Power Modifer ^ 2.5(so a 2x Power Modifier will have a 5.66x fuel consumption)";

                        #region Engine Base

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EngineBaseTech];
                        if(TechLevel > 12)
                            TechLevel = 12;

                        for (int loop = TechLevel; loop >= 1; loop--)
                        {
                            switch (loop)
                            {
                                case 1 :
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Nuclear Thermal Engine Technology");
                                break;
                                case 2:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Nuclear Pulse Engine Technology");
                                break;
                                case 3:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Ion Drive Technology");
                                break;
                                case 4:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Magneto-Plasma Drive Technology");
                                break;
                                case 5:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Internal Confinement Fusion Drive Technology");
                                break;
                                case 6:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Magnetic Confinement Fusion Drive Technology");
                                break;
                                case 7:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Inertial Confinement Fusion Drive Technology");
                                break;
                                case 8:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Solid-Core Antimatter Drive Technology");
                                break;
                                case 9:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Gas-Core Antimatter Drive Technology");
                                break;
                                case 10:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Plasma-Core Antimatter Drive Technology");
                                break;
                                case 11:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Beam-Core Antimatter Drive Technology");
                                break;
                                case 12:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Photonic Drive Technology");
                                break;

                            }
                            
                        }

                        m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Conventional Engine Technology");
                        m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;

                        #endregion

                        #region engine power mod

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MinEnginePowerMod];

                        if (TechLevel > 5)
                            TechLevel = 5;

                        int MinPower = 50;
                        switch (TechLevel)
                        {
                            case 0:
                                MinPower = 40;
                                break;
                            case 1:
                                MinPower = 30;
                                break;
                            case 2:
                                MinPower = 25;
                                break;
                            case 3:
                                MinPower = 20;
                                break;
                            case 4:
                                MinPower = 15;
                                break;
                            case 5:
                                MinPower = 10;
                                break;
                        }
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MaxEnginePowerMod];

                        if (TechLevel > 5)
                            TechLevel = 5;

                        int MaxPower = 100;
                        switch (TechLevel)
                        {
                            case 0:
                                MaxPower = 125;
                                break;
                            case 1:
                                MaxPower = 150;
                                break;
                            case 2:
                                MaxPower = 175;
                                break;
                            case 3:
                                MaxPower = 200;
                                break;
                            case 4:
                                MaxPower = 250;
                                break;
                            case 5:
                                MaxPower = 300;
                                break;
                        }

                        int Count = 0;
                        for (int loop = MinPower; loop <= MaxPower; loop += 5)
                        {
                            float EP = (float)loop/100.0f;
                            float EPH = (float)Math.Pow(EP,2.5f);

                            Entry = String.Format("Engine Power x{0:N2} Fuel Consumption Per EPH x{1:N2}", EP, EPH);
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);

                            if(loop == 100)
                                m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = Count;

                            Count++;
                        }


                        #endregion

                        #region Fuel Consumption

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.FuelConsumption];

                        if (TechLevel > 12)
                            TechLevel = 12;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            Entry = String.Format("Fuel Consumption: {0} Litres per Engine Power Hour", Constants.EngineTN.FuelConsumption[loop]);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        /// <summary>
                        /// Should atleast be 1 level of fuel con even for conventional starts.
                        /// </summary>
                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;

                        #endregion

                        #region Thermal Reduction

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ThermalReduction];

                        if (TechLevel > 12)
                            TechLevel = 12;

                        for (int loop = 0; loop <= TechLevel; loop++)
                        {
                            Entry = String.Format("Thermal Reduction: Signature {0}% Normal", (Constants.EngineTN.ThermalReduction[loop] * 100.0f));
                            m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        }
                        m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex = 0;


                        #endregion

                        #region Size
                        for (int loop = 1; loop <= 50; loop++)
                        {
                            Entry = String.Format("{0} HS. Fuel Consumption -{0}%", loop);
                            m_oComponentDesignPanel.TechComboBoxFive.Items.Add(Entry);
                        }
                        m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex = 4;
                        #endregion

                        #region HyperDrive

                        m_oComponentDesignPanel.TechComboBoxSix.Items.Add("No Hyper Drive Capability");

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.HyperdriveSizeMod];

                        if (TechLevel > 10)
                            TechLevel = 10;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            Entry = String.Format("Hyper Drive Size Multiplier {0:N2}", Constants.EngineTN.HyperDriveSize[loop]);
                            m_oComponentDesignPanel.TechComboBoxSix.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex = 0;

                        #endregion

                    break;
                    #endregion

                    #region Gauss Cannon
                    case ComponentsViewModel.Components.Gauss:

                        SetTechVisible(3);

                        SetLabels("Gauss Cannon Rate of Fire", "Gauss Cannon Velocity", "Gauss Cannon Size vs Accuracy", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "With enough research Gauss cannons become the fastest firing weapon available, which in addition to being turretable makes them ideal for point defense. They may also trade size for accuracy making smaller craft able to mount them.";

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.GaussCannonROF];

                        if (TechLevel > 6)
                            TechLevel = 6;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            Entry = String.Format("Gauss Cannon Rate of Fire {0}", Constants.BeamWeaponTN.GaussShots[loop]);
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.GaussCannonVelocity];

                        if (TechLevel > 5)
                            TechLevel = 5;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            Entry = String.Format("Gauss Cannon Launch Velocity {0}", (loop + 1));
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxTwo.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;


                        for (int loop = 0; loop < 10; loop++)
                        {
                            Entry = String.Format("Gauss Cannon Size vs Accuracy {0}HS and {1}%", Constants.BeamWeaponTN.GaussSize[loop], (Constants.BeamWeaponTN.GaussAccuracy[loop]*100.0f));
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;


                    break;
                    #endregion

                    #region High Power Microwave
                    case ComponentsViewModel.Components.Microwave:

                        SetTechVisible(3);

                        SetLabels("Microwave Focal Size", "Microwave Focusing", "Capacitor Recharge Rate", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Microwaves do damage somewhat differently from other weapons. They only do 1 point of damage(3 vs shields), but pass through armor, and only hit components in the electronic Damage Allocation Table(EDAC). " + 
                                                                  "Likewise hardening of electronic components does not convey additional hit to kill, but reduces on a percentage basis the chance of destruction vs electronic damage. " +
                                                                  "Microwaves have 1/2 the range of a similarly sized and focused laser.";

                        int MicrowaveTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MicrowaveFocal];
                        int MicroFocusTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MicrowaveFocusing];

                        if (MicrowaveTech > 11)
                            MicrowaveTech = 11;
                        if (MicroFocusTech > 11)
                            MicroFocusTech = 11;

                        for (int loop = MicrowaveTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("{0}cm Microwave Focal Size", Constants.BeamWeaponTN.SizeClass[loop]);
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;

                        for (int loop = MicroFocusTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("Microwave Focusing Technology {0}", (loop+1));
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxTwo.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;


                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                        if (TechLevel > 11)
                            TechLevel = 11;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            Entry = String.Format("Capacitor Recharge Rate {0}", Constants.BeamWeaponTN.Capacitor[loop]);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;

                    break;
                    #endregion

                    #region Jump Engines
                    case ComponentsViewModel.Components.Jump:

                        SetTechVisible(5);

                        SetLabels("Jump Efficiency", "Max Jump Squadron Size", "Max Squadron Jump Radius", "Size", "Jump Drive Type", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Jump rating is equal to ship size, not jump engine rating. at 15KT ship with a 40K jump engine has a jump rating of 15K, not 40K." +
                                                                  " Commercial ships may use military jump engine tenders, but military vessels may not use commercial jump engine tenders." +
                                                                  " Self jump only applies to squadron transits, standard transits are uneffected.";

                    break;
                    #endregion

                    #region Lasers
                    case ComponentsViewModel.Components.Laser:

                        SetTechVisible(5);

                        SetLabels("Laser Focal Size", "Laser Wavelength", "Capacitor Recharge Rate", "Reduced Size Lasers", "Energy Weapon Mount", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Damage dropoff due to range is controlled by wavelength, higher wavelengths forestall damage dropoff in addition to raising weapon range. Lasers have the outright highest armor penetration pattern of all weapons.";


                        int AdvLaserTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.AdvancedLaserFocal];
                        int LaserTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.LaserFocal];
                        int WaveTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.LaserWavelength];

                        if (AdvLaserTech > 11)
                            AdvLaserTech = 11;

                        if (LaserTech > 11)
                            LaserTech = 11;

                        if (WaveTech > 11)
                            WaveTech = 11;

                        for (int loop = AdvLaserTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("{0}cm Advanced Laser Focal Size", Constants.BeamWeaponTN.SizeClass[loop]);
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        for (int loop = LaserTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("{0}cm Laser Focal Size", Constants.BeamWeaponTN.SizeClass[loop]);
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;

                        for (int loop = WaveTech; loop >= 0; loop--)
                        {
                            //Infared, Visible Light, Near Ultraviolet, Ultraviolet, Far Ultraviolet, Soft X-Ray(30), X-Ray(60), Far X-Ray(125), Extreme X-Ray(250), Near Gamma Ray(500), Gamma Ray(1M), Far Gamma Ray(2M)
        
                            switch (loop)
                            {
                                case 0:
                                    Entry = "Infared Laser";
                                    break;
                                case 1:
                                    Entry = "Visible Light Laser";
                                    break;
                                case 2:
                                    Entry = "Near Ultraviolet Laser";
                                    break;
                                case 3:
                                    Entry = "Ultraviolet Laser";
                                    break;
                                case 4:
                                    Entry = "Far Ultraviolet Laser";
                                    break;
                                case 5:
                                    Entry = "Soft X-Ray Laser";
                                    break;
                                case 6:
                                    Entry = "X-Ray Laser";
                                    break;
                                case 7:
                                    Entry = "Far X-Ray Laser";
                                    break;
                                case 8:
                                    Entry = "Extreme X-Ray Laser";
                                    break;
                                case 9:
                                    Entry = "Near Gamma Ray Laser";
                                    break;
                                case 10:
                                    Entry = "Gamma Ray Laser";
                                    break;
                                case 11:
                                    Entry = "Far Gamma Ray Laser";
                                    break;
                            }
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }
                        if (m_oComponentDesignPanel.TechComboBoxTwo.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;


                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                        if (TechLevel > 11)
                            TechLevel = 11;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            Entry = String.Format("Capacitor Recharge Rate {0}", Constants.BeamWeaponTN.Capacitor[loop]);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;

                        TechLevel  = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ReducedSizeLasers];
                        if (TechLevel > 1)
                            TechLevel = 1;

                        m_oComponentDesignPanel.TechComboBoxFour.Items.Add("Standard Laser Size and Recharge Rate");

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            switch (loop)
                            {
                                case 1:
                                    Entry = "Reduced-Size Laser 0.75 Size / 4x Recharge";
                                    break;
                                case 0:
                                    Entry = "Reduced-Size Laser 0.5 Size / 20x Recharge";
                                    break;
                            }

                            m_oComponentDesignPanel.TechComboBoxFour.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex = 0;

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.SpinalMount];
                        if (TechLevel > 1)
                            TechLevel = 1;

                        m_oComponentDesignPanel.TechComboBoxFive.Items.Add("Standard Mount");

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            switch (loop)
                            {
                                case 1:
                                    Entry = "Spinal Mount";
                                    break;
                                case 0:
                                    Entry = "Advanced Spinal Mount";
                                    break;
                            }

                            m_oComponentDesignPanel.TechComboBoxFive.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex = 0;

                    break;
                    #endregion

                    #region Magazines
                    case ComponentsViewModel.Components.Magazine:

                        SetTechVisible(5);

                        SetLabels("Magazine Feed System Efficiency", "Magazine Ejection System", "Armour", "Magazine Size", "HTK", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "HTK consumes internal magazine space as though it were armour being applied to the inside of the component.";

                    break;
                    #endregion

                    #region Meson Cannons
                    case ComponentsViewModel.Components.Meson:

                        SetTechVisible(3);

                        SetLabels("Meson Focal Size", "Meson Focusing", "Capacitor Recharge Rate", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Mesons pass through shields and armour to strike components directly, but only do a damage of 1, so larger and harder to kill components may survive. As with microwaves Mesons have half the range of a similar laser.";


                        int MesonTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MesonFocal];
                        int MesonFocusTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MesonFocusing];

                        if (MesonTech > 11)
                            MesonTech = 11;
                        if (MesonFocusTech > 11)
                            MesonFocusTech = 11;

                        for (int loop = MesonTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("{0}cm Meson Focal Size", Constants.BeamWeaponTN.SizeClass[loop]);
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;

                        for (int loop = MesonFocusTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("Meson Focusing Technology {0}", (loop+1));
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxTwo.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;


                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                        if (TechLevel > 11)
                            TechLevel = 11;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            Entry = String.Format("Capacitor Recharge Rate {0}", Constants.BeamWeaponTN.Capacitor[loop]);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;

                    break;
                    #endregion

                    #region Missile Engines
                    case ComponentsViewModel.Components.MissileEngine:

                        SetTechVisible(4);

                        SetLabels("Engine Technology", "Power / Efficiency Modifier", "Fuel Consumption", "Missile Engine Size", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Missiles are solid fueled. They have 2x max power modification and 5x max fuel consumption compared to ship based engines.";

                    break;
                    #endregion

                    #region Missile Launcher
                    case ComponentsViewModel.Components.MissileLauncher:

                        SetTechVisible(4);

                        SetLabels("Missile Launcher Size", "Missile Launcher Reload Rate", "Platform Type", "Reduced Size Launchers", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Missile launchers may load ordnance of any size less than or equal to their own size before size reductions.";

                    break;
                    #endregion

                    #region New Species
                    case ComponentsViewModel.Components.NewSpecies:

                        SetTechVisible(5);

                        SetLabels("Species Name", "Temperature Range", "Base Temperature", "Base Oxygen Level", "Base Gravity", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "New species may expand range of worlds that your empire can colonize.";

                    break;
                    #endregion

                    #region Particle beams
                    case ComponentsViewModel.Components.Particle:

                        SetTechVisible(3);

                        SetLabels("Particle Beam Strength", "Particle Beam Range", "Capacitor Recharge Rate", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Particle beams have slightly shorter range, are somewhat larger, and cannot be turreted compared to lasers, but never suffer damage drop off.";

                        int AdvPartTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.AdvancedParticleBeamStrength];
                        int PartTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ParticleBeamStrength];
                        int PartRangeTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ParticleBeamRange];

                        if (AdvPartTech > 10)
                            AdvPartTech = 10;

                        if (PartTech > 10)
                            PartTech = 10;

                        if (PartRangeTech > 11)
                            PartRangeTech = 11;

                        for (int loop = AdvPartTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("Advanced Particle Beam Strength {0}", Constants.BeamWeaponTN.AdvancedParticleDamage[loop]);
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        for (int loop = PartTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("Particle Beam Strength {0}", Constants.BeamWeaponTN.ParticleDamage[loop]);
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;

                        for (int loop = PartRangeTech; loop >= 0; loop--)
                        {
                            float range = (float)Constants.BeamWeaponTN.ParticleRange[loop] * 10000.0f;
                            String FormattedRange = range.ToString("#,###0");
                            Entry = String.Format("Particle Beam Range {0}km", FormattedRange);
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }
                        if (m_oComponentDesignPanel.TechComboBoxTwo.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;


                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                        if (TechLevel > 11)
                            TechLevel = 11;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            Entry = String.Format("Capacitor Recharge Rate {0}", Constants.BeamWeaponTN.Capacitor[loop]);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;
                    break;
                    #endregion

                    #region Plasma Carronades
                    case ComponentsViewModel.Components.Plasma:

                        SetTechVisible(2);

                        SetLabels("Carronade Calibre", "Capacitor Recharge Rate", "", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Plasma Carronades are cheaper than lasers of similar size, do not have any focusing tech associated with them, have a different armour damage pattern, and may not be turreted.";

                        int AdvPlasmaTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.AdvancedPlasmaCarronadeCalibre];
                        int PlasmaTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.PlasmaCarronadeCalibre];

                        if (AdvPlasmaTech > 9)
                            AdvPlasmaTech = 9;
                        if (PlasmaTech > 9)
                            PlasmaTech = 9;

                        for (int loop = AdvPlasmaTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("{0}cm Advanced Carronade", Constants.BeamWeaponTN.SizeClass[loop + 2]);
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        for (int loop = PlasmaTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("{0}cm Carronade", Constants.BeamWeaponTN.SizeClass[loop + 2]);
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;


                        int CapTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                        if (CapTech > 11)
                            CapTech = 11;

                        for (int loop = CapTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("Capacitor Recharge Rate {0}", Constants.BeamWeaponTN.Capacitor[loop]);
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;

                    break;
                    #endregion

                    #region Plasma Torpedos
                    case ComponentsViewModel.Components.PlasmaTorp:

                        SetTechVisible(4);

                        SetLabels("Plasma Torpedo Warhead Strength", "Plasma Torpedo Speed", "Plasma Torpedo Integrity", "Plasma Torpedo Recharge Rate", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "No Notes";

                    break;
                    #endregion

                    #region Power Plants
                    case ComponentsViewModel.Components.Reactor:

                        SetTechVisible(3);

                        SetLabels("Power Plant Technology", "Power vs Efficiency", "Size", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "All beam weapons need power from power plants in order to function.";

                        /// <summary>
                        /// Base Reactor Tech.These strings should ideally be in the database as well
                        /// </summary>

                        int RT = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ReactorBaseTech];

                        if (RT > 11)
                            RT = 11;

                        #region Reactor loop and switch. These are hard coded, move to DB?
                        for (int loop = RT; loop >= 0; loop--)
                        {
                            switch(loop)
                            {
                                case 11:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Vacuum Energy Power Plant Technology");
                                break;
                                case 10:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Beam-Core Antimatter Reactor Technology");
                                break;
                                case 9:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Plasma-Core Antimatter Reactor Technology");
                                break;
                                case 8:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Gas-Core Antimatter Reactor Technology");
                                break;
                                case 7:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Solid-Core Antimatter Reactor Technology");
                                break;
                                case 6:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Inertial Confinement Fusion Reactor Technology");
                                break;
                                case 5:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Magnetic Confinement Fusion Reactor Technology");
                                break;
                                case 4:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Tokamak Fusion Reactor Technology");
                                break;
                                case 3:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Stellarator Fusion Reactor Technology");
                                break;
                                case 2:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Gas Cooled Fast Reactor Technology");
                                break;
                                case 1:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Pebble Bed Reactor Technology");
                                break;
                                case 0 :
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Pressurised Water Reactor Technology");
                                break;
                            }
                        }
                        if (m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;
                        #endregion

                        #region Reactor Power Boost, again hard coded.

                        int RP = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ReactorPowerBoost];

                        if(RP > 8)
                            RP = 8;

                        m_oComponentDesignPanel.TechComboBoxTwo.Items.Add("No Reactor Power Boost");

                        for (int loop = 0; loop < RP; loop++)
                        {
                            switch (loop)
                            {
                                case 0 :
                                    m_oComponentDesignPanel.TechComboBoxTwo.Items.Add("Reactor Power Boost 5%, Explosion 7%");
                                break;
                                case 1:
                                    m_oComponentDesignPanel.TechComboBoxTwo.Items.Add("Reactor Power Boost 10%, Explosion 10%");
                                break;
                                case 2:
                                    m_oComponentDesignPanel.TechComboBoxTwo.Items.Add("Reactor Power Boost 15%, Explosion 12%");
                                break;
                                case 3:
                                    m_oComponentDesignPanel.TechComboBoxTwo.Items.Add("Reactor Power Boost 20%, Explosion 16%");
                                break;
                                case 4:
                                    m_oComponentDesignPanel.TechComboBoxTwo.Items.Add("Reactor Power Boost 25%, Explosion 20%");
                                break;
                                case 5:
                                    m_oComponentDesignPanel.TechComboBoxTwo.Items.Add("Reactor Power Boost 30%, Explosion 25%");
                                break;
                                case 6:
                                    m_oComponentDesignPanel.TechComboBoxTwo.Items.Add("Reactor Power Boost 40%, Explosion 30%");
                                break;
                                case 7:
                                    m_oComponentDesignPanel.TechComboBoxTwo.Items.Add("Reactor Power Boost 50%, Explosion 35%");
                                break;
                            }
                        }

                        m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;

                        #endregion

                        #region Reactor Size. I wish that size were standardized across all components.

                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add("0.1");
                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add("0.2");
                        m_oComponentDesignPanel.TechComboBoxThree.Items.Add("0.5");

                        for (int loop = 1; loop <= 30; loop++)
                        {
                            Entry = String.Format("{0}", loop);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;
                    #endregion

                    break;
                    #endregion

                    #region Railguns
                    case ComponentsViewModel.Components.Rail:

                        SetTechVisible(3);

                        SetLabels("Railgun Type", "Railgun Velocity", "Capacitor Recharge Rate", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Railguns are initially the fastest firing weapon available. They out damage similar lasers but have less range, less penetration, and may not be turreted.";

                        int AdvRailTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.AdvancedRailgun];
                        int RailTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.Railgun];

                        if (AdvRailTech > 9)
                            AdvRailTech = 9;
                        if (RailTech > 9)
                            RailTech = 9;

                        /// <summary>
                        /// So rail guns have their own special class size, 45cm, that no other beam weapon uses(except perhaps spinal lasers).
                        /// Here is the kludge for that.
                        /// </summary>
                        for (int loop = AdvRailTech; loop >= 0; loop--)
                        {
                            switch (loop)
                            {
                                case 9:
                                    Entry = String.Format("{0}cm Advanced Railgun", "50");
                                    break;
                                case 8:
                                    Entry = String.Format("{0}cm Advanced Railgun", "45");
                                    break;
                                default:
                                    Entry = String.Format("{0}cm Advanced Railgun", Constants.BeamWeaponTN.SizeClass[loop]);
                                    break;
                            }
                            
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        for (int loop = RailTech; loop >= 0; loop--)
                        {
                            switch (loop)
                            {
                                case 9:
                                    Entry = String.Format("{0}cm Railgun", "50");
                                    break;
                                case 8:
                                    Entry = String.Format("{0}cm Railgun", "45");
                                    break;
                                default:
                                    Entry = String.Format("{0}cm Railgun", Constants.BeamWeaponTN.SizeClass[loop]);
                                    break;
                            }
                            m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;

                        int RailVelTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.RailgunVelocity];
                        if (RailVelTech > 8)
                            RailVelTech = 8;

                        for (int loop = RailVelTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("Railgun Launch Velocity {0}", (loop + 1));
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        if (m_oComponentDesignPanel.TechComboBoxTwo.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;


                        CapTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                        if (CapTech > 11)
                            CapTech = 11;

                        for (int loop = CapTech; loop >= 0; loop--)
                        {
                            Entry = String.Format("Capacitor Recharge Rate {0}", Constants.BeamWeaponTN.Capacitor[loop]);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;

                    break;
                    #endregion

                    #region Absorption Shields
                    case ComponentsViewModel.Components.ShieldAbs:

                        SetTechVisible(4);

                        SetLabels("Absorption Shield Strength", "Absorption Shield Radiation", "Fuel Efficiency", "Size", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "No Notes";

                    break;
                    #endregion

                    #region Standard Shields
                    case ComponentsViewModel.Components.ShieldStd:

                        SetTechVisible(3);

                        SetLabels("Shield Type", "Shield Regeneration Rate", "Fuel Consumption", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Armour offers better protection for cost and size, but shields can recharge, and will block electronic damage as well as preventing shock damage. Mesons will still pass through shields as with armour, but mesons have a chance to hit a shield emitter, while they have no such chance to hit armour.";

                        #region Shield Strength, as with other entries, this is hard coded and should be moved to the DB somehow at some point.
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.StandardShieldStrength];

                        if (TechLevel > 11)
                            TechLevel = 11;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            switch(loop)
                            {
                                case 0:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Alpha Shields");
                                    break;
                                case 1:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Beta Shields");
                                    break;
                                case 2:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Gamma Shields");
                                    break;
                                case 3:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Delta Shields");
                                    break;
                                case 4:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Epsilon Shields");
                                    break;
                                case 5:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Theta Shields");
                                    break;
                                case 6:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Xi Shields");
                                    break;
                                case 7:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Omicron Shields");
                                    break;
                                case 8:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Sigma Shields");
                                    break;
                                case 9:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Tau Shields");
                                    break;
                                case 10:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Psi Shields");
                                    break;
                                case 11:
                                    m_oComponentDesignPanel.TechComboBoxOne.Items.Add("Omega Shields");
                                    break;
                            }
                        }
                        if(m_oComponentDesignPanel.TechComboBoxOne.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;
                        #endregion


                        #region Shield Regeneration
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.StandardShieldRegen];

                        if (TechLevel > 11)
                            TechLevel = 11;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            Entry = String.Format("Shield Regeneration Rate {0}", Constants.ShieldTN.ShieldBase[loop]);
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }
                        if (m_oComponentDesignPanel.TechComboBoxTwo.Items.Count != 0)
                            m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 0;
                        #endregion

                        #region Fuel Consumption

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.FuelConsumption];

                        if (TechLevel > 12)
                            TechLevel = 12;

                        for (int loop = TechLevel; loop >= 0; loop--)
                        {
                            Entry = String.Format("Fuel Consumption: {0} Litres per Hour", Constants.EngineTN.FuelConsumption[loop]);
                            m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                        }

                        /// <summary>
                        /// Should atleast be 1 level of fuel con even for conventional starts.
                        /// </summary>
                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;

                        #endregion

                    break;
                    #endregion

                    #region Thermal Sensors
                    case ComponentsViewModel.Components.Thermal:

                        SetTechVisible(3);

                        SetLabels("Thermal Sensor Sensitivity", "Total Sensor Size", "Hardening", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Rating is Thermal Sensor Sensitivity x Size. This is the distance in Millions of Km that a signature of 1000 may be detected. smaller or larger signatures are detected by (Signature/1000) * Rating. " +
                                                                  "All ships always emit a thermal signature, even if it is tiny.";

                        /// <summary>
                        /// EM sensitivity tech listing.
                        /// </summary>
                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ThermalSensorSensitivity];
                        if (TechLevel >= 0)
                        {
                            if (TechLevel > 11)
                                TechLevel = 11;

                            for (int loop = TechLevel; loop >= 0; loop--)
                            {
                                Entry = String.Format("Thermal Sensor Sensitivity {0}", Constants.SensorTN.PassiveStrength[loop]);
                                m_oComponentDesignPanel.TechComboBoxOne.Items.Add(Entry);
                            }

                            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = 0;
                        }

                        /// <summary>
                        /// Overall size from 0.1 to 50
                        /// </summary>
                        for (int loop = 1; loop < 10; loop++)
                        {
                            Entry = String.Format("0.{0}", loop);
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        for (int loop = 0; loop < 10; loop += 2)
                        {
                            Entry = "N/A";
                            if (loop == 0)
                                Entry = "1";
                            else
                                Entry = String.Format("1.{0}", loop);
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        for (int loop = 2; loop < 5; loop++)
                        {
                            for (int loop2 = 0; loop2 < 100; loop2 += 25)
                            {
                                Entry = "N/A";
                                if (loop2 == 0)
                                    Entry = String.Format("{0}", loop);
                                else
                                    Entry = String.Format("{0}.{1}", loop, loop2);

                                m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                            }
                        }

                        for (int loop = 5; loop < 51; loop++)
                        {
                            Entry = String.Format("{0}", loop);
                            m_oComponentDesignPanel.TechComboBoxTwo.Items.Add(Entry);
                        }

                        m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = 9;

                        TechLevel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.Hardening];
                        if (TechLevel >= 0)
                        {
                            if (TechLevel > 8)
                                TechLevel = 8;

                            for (int loop = 0; loop <= TechLevel; loop++)
                            {
                                Entry = String.Format("Electronic Hardening Level {0}", loop);
                                m_oComponentDesignPanel.TechComboBoxThree.Items.Add(Entry);
                            }

                            m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = 0;
                        }
                    break;
                    #endregion
                }
                #endregion

            }
        }

        /// <summary>
        /// Update the display/Name for the proposed component.
        /// </summary>
        private void BuildSystemParameters()
        {
            String Entry = "N/A";
            float Size = 0.0f;
            float Hard = 1.0f;
            float Boost = 1.0f;
            int FactTech = -1;

            m_oComponentDesignPanel.TechNameTextBox.Clear();
            m_oComponentDesignPanel.ParametersTextBox.Clear();

            #region BuildSystemParamters Switch
            switch ((ComponentsViewModel.Components)m_oComponentDesignPanel.ResearchComboBox.SelectedIndex)
            {
                #region Active Sensors / MFC
                case ComponentsViewModel.Components.ActiveMFC:

                    /// <summary>
                    /// Sanity check.
                    /// </summary>
                    if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex != -1 &&
                        m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex != -1)
                    {


                        #region Size
                        /// <summary>
                        /// Pull size out of this mess.
                        /// </summary>
                        if(m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex < 9)
                        {
                            Size = (float)(m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex+1) / 10.0f;
                        }
                        else if( m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex >= 9 && m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex <= 13)
                        {
                            Size = 1.0f + (float)((m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex-9)*2) / 10.0f;
                        }
                        else if( m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex >= 14 && m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex <= 25)
                        {
                            Size = 2.0f + (float)(((m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex-14) * 25) / 100.0f);
                        }
                        else if (m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex > 25)
                        {
                            Size = (float)(m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex - 21);
                        }
                        #endregion

                        #region Resolution
                        ushort Resolution = 501;
                        if (m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex < 20)
                        {
                            Resolution = (ushort)(m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex + 1);
                        }
                        else if (m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex >= 20 && m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex <= 55)
                        {
                            Resolution = (ushort)(20 + ((m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex - 19) * 5));
                        }
                        else if (m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex > 55)
                        {
                            Resolution = (ushort)(200 + ((m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex - 55) * 20));
                        }
                        #endregion

                        #region Hardening
                        /// <summary>
                        /// Get chance of destruction due to electronic damage.
                        /// </summary>
                        switch (m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex)
                        {
                            case 0:
                            Hard = 1.0f;
                            break;
                            case 1:
                            Hard = 0.7f;
                            break;
                            case 2:
                            Hard = 0.5f;
                            break;
                            case 3:
                            Hard = 0.4f;
                            break;
                            case 4:
                            Hard = 0.3f;
                            break;
                            case 5:
                            Hard = 0.25f;
                            break;
                            case 6:
                            Hard = 0.2f;
                            break;
                            case 7:
                            Hard = 0.15f;
                            break;
                            case 8:
                            Hard = 0.1f;
                            break;
                            default:
                            Hard = 1.0f;
                            break;
                        }
                        #endregion

                        bool isMFC = false;

                        if (m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex == 0)
                        {
                            Entry = "Active Search Sensor MR";
                            isMFC = false;
                        }
                        else
                        {
                            Entry = "Missile Fire Control FC";
                            isMFC = true;
                        }

                        FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ActiveSensorStrength];

                        /// <summary>
                        /// More sanity checking as give all does not do this. I might not want to hard code all of this however.
                        /// </summary>
                        if (FactTech > 11)
                            FactTech = 11;

                        int AS = FactTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;

                        FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EMSensorSensitivity];

                        if (FactTech > 11)
                            FactTech = 11;

                        int EM = FactTech - m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex;

                        ActiveSensorProject = new ActiveSensorDefTN(Entry,Size, Constants.SensorTN.ActiveStrength[AS],
                                                                               Constants.SensorTN.PassiveStrength[EM],
                                                                               Resolution, isMFC,
                                                                               Hard, (byte)(m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex+1));

                        int mkm = (int)Math.Floor((float)ActiveSensorProject.maxRange / 100.0f);

                        Entry = String.Format("{0}{1}-R{2}", Entry, mkm, Resolution);

                        if (Hard != 1.0f)
                        {
                            Entry = String.Format("{0} ({1}%)", Entry, (Hard*100.0f));
                        }

                        ActiveSensorProject.Name = Entry;

                        m_oComponentDesignPanel.TechNameTextBox.Text = ActiveSensorProject.Name;

                        Entry = String.Format("Active Sensor Strength: {0} Sensitivity Modifier: {1}%\n", (ActiveSensorProject.activeStrength * ActiveSensorProject.size), ((float)ActiveSensorProject.eMRecv * 10.0f));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        if (m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                            Entry = String.Format("Sensor Size: {0} Tons  Sensor HTK: {1}\n", (ActiveSensorProject.size * 50.0f), ActiveSensorProject.htk);
                        else
                            Entry = String.Format("Sensor Size: {0} HS  Sensor HTK: {1}\n", ActiveSensorProject.size, ActiveSensorProject.htk);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);


                        int RangeFormat = (ActiveSensorProject.maxRange*10);
                        string FormattedRange = RangeFormat.ToString("#,##0");



                        Entry = String.Format("Resolution: {0}    Maximum Range vs {1} ton object (or larger): {2},000 km\n", ActiveSensorProject.resolution, (ActiveSensorProject.resolution * 50), FormattedRange);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        if (Resolution == 1)
                        {
                            RangeFormat = ActiveSensorProject.GetActiveDetectionRange(0,0) * 10;
                            if (RangeFormat < 100)
                            {
                                int Range1 = (int)((float)ActiveSensorProject.maxRange * 10000.0f * (float)Math.Pow((0.33 / (float)Resolution), 2.0f));
                                FormattedRange = Range1.ToString("#,##0");
                                Entry = String.Format("Range vs Size 6 Missile (or smaller): {0} km\n", FormattedRange);
                                m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                                int Range2 = (int)((float)ActiveSensorProject.maxRange * 10000.0f * (float)Math.Pow((0.40 / (float)Resolution), 2.0f));
                                FormattedRange = Range2.ToString("#,##0");
                                Entry = String.Format("Range vs Size 8 Missile: {0} km\n", FormattedRange);
                                m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                                int Range3 = (int)((float)ActiveSensorProject.maxRange * 10000.0f * (float)Math.Pow((0.60 / (float)Resolution), 2.0f));
                                FormattedRange = Range3.ToString("#,##0");
                                Entry = String.Format("Range vs Size 12 Missile: {0} km\n", FormattedRange);
                                m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                            }
                            else
                            {

                                FormattedRange = RangeFormat.ToString("#,##0");
                                Entry = String.Format("Range vs Size 6 Missile (or smaller): {0},000 km\n", FormattedRange);
                                m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                                RangeFormat = ActiveSensorProject.GetActiveDetectionRange(0, 2) * 10;
                                FormattedRange = RangeFormat.ToString("#,##0");
                                Entry = String.Format("Range vs Size 8 Missile: {0},000 km\n", FormattedRange);
                                m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                                RangeFormat = ActiveSensorProject.GetActiveDetectionRange(0, 4) * 10;
                                FormattedRange = RangeFormat.ToString("#,##0");
                                Entry = String.Format("Range vs Size 12 Missile: {0},000 km\n", FormattedRange);
                                m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                            }
                        }
                        else
                        {
                            RangeFormat = ActiveSensorProject.GetActiveDetectionRange(19,-1) *10;
                            FormattedRange = RangeFormat.ToString("#,##0");
                            Entry = String.Format("Range vs 1000 ton object: {0},000 km\n",FormattedRange);
                            m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                            RangeFormat = ActiveSensorProject.GetActiveDetectionRange(4, -1) * 10;
                            FormattedRange = RangeFormat.ToString("#,##0");
                            Entry = String.Format("Range vs 250 ton object: {0},000 km\n",FormattedRange);
                            m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                        }

                        Entry = String.Format("Chance of destruction by electronic damage: {0}%\n", (Hard * 100.0f));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Cost: {0}    Crew: {1}\n",ActiveSensorProject.cost, ActiveSensorProject.crew);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Materials Required: Not Yet Implemented\n\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Development Cost for Project: {0}RP\n", ActiveSensorProject.cost * 10);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    }

                break;
                #endregion

                #region Beam Fire Control
                case ComponentsViewModel.Components.BFC:
                    /// <summary>
                    /// Sanity check.
                    /// </summary>
                if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                    m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex != -1 &&
                    m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex != -1 &&
                    m_oComponentDesignPanel.TechComboBoxSeven.SelectedIndex != -1)
                {

                    #region Get Stats
                    FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.BeamFireControlRange];

                    if (FactTech > 11)
                        FactTech = 11;

                    int BR = FactTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;

                    FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.BeamFireControlTracking];

                    if (FactTech > 11)
                        FactTech = 11;

                    int BT = FactTech - m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex;

                    float modR = 1.0f;

                    switch (m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex)
                    {
                        case 0 :
                            modR = 1.0f;
                            break;
                        case 1 :
                            modR = 1.5f;
                            break;
                        case 2 :
                            modR = 2.0f;
                            break;
                        case 3 :
                            modR = 3.0f;
                            break;
                        case 4:
                            modR = 0.5f;
                            break;
                        case 5:
                            modR = 4.0f;
                            break;
                        case 6:
                            modR = 0.34f;
                            break;
                        case 7:
                            modR = 0.25f;
                            break;
                        default :
                            modR = 1.0f;
                            break;
                    }

                    float modT = 1.0f;

                    switch (m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex)
                    {
                        case 0:
                            modT = 1.0f;
                            break;
                        case 1:
                            modT = 1.25f;
                            break;
                        case 2:
                            modT = 0.5f;
                            break;
                        case 3:
                            modT = 1.5f;
                            break;
                        case 4:
                            modT = 2.0f;
                            break;
                        case 5:
                            modT = 3.0f;
                            break;
                        case 6:
                            modT = 4.0f;
                            break;
                        default :
                            modT = 1.0f;
                            break;
                    }

                    switch (m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex)
                    {
                        case 0:
                            Hard = 1.0f;
                            break;
                        case 1:
                            Hard = 0.7f;
                            break;
                        case 2:
                            Hard = 0.5f;
                            break;
                        case 3:
                            Hard = 0.4f;
                            break;
                        case 4:
                            Hard = 0.3f;
                            break;
                        case 5:
                            Hard = 0.25f;
                            break;
                        case 6:
                            Hard = 0.2f;
                            break;
                        case 7:
                            Hard = 0.15f;
                            break;
                        case 8:
                            Hard = 0.1f;
                            break;
                        default:
                            Hard = 1.0f;
                            break;
                    }

                    bool isPDC = false;
                    if (m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex == 0)
                        isPDC = false;
                    else
                        isPDC = true;

                    bool isFighter = false;
                    if (m_oComponentDesignPanel.TechComboBoxSeven.SelectedIndex == 0)
                        isFighter = false;
                    else
                        isFighter = true;

                    #endregion


                    BeamFCProject = new BeamFireControlDefTN("Fire Control S", BR, BT, 
                                                             modR, modT, isPDC, isFighter, Hard, (byte)(m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex + 1));

                    Entry = "N/A";
                    if(BeamFCProject.size >= 10.0f)
                        Entry = String.Format("{0}{1} {2}-{3}", BeamFCProject.Name,BeamFCProject.size,(BeamFCProject.range/1000.0f),BeamFCProject.tracking);
                    else
                        Entry = String.Format("{0}0{1} {2}-{3}", BeamFCProject.Name, BeamFCProject.size, (BeamFCProject.range / 1000.0f), BeamFCProject.tracking);

                    if (Hard != 1.0f)
                    {
                        Entry = String.Format("{0} H{1}", Entry, (Hard * 100.0f));
                    }

                    if (isPDC == true)
                    {
                        Entry = String.Format("PDC {0}", Entry);
                    }
                    else if (isFighter == true && isPDC == false)
                    {
                        Entry = String.Format("{0} (FTR)", Entry);
                    }

                    m_oComponentDesignPanel.TechNameTextBox.Text = Entry;
                    BeamFCProject.Name = Entry;

                    String FormattedRange = BeamFCProject.range.ToString("#,##0");
                    Entry = String.Format("50% Accuracy at Range: {0} km     Tracking Speed: {1} km/s\n",FormattedRange,BeamFCProject.tracking);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    if(m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                        Entry = String.Format("Size: {0} Tons    HTK: {1}    Cost: {2}    Crew: {3}\n", (BeamFCProject.size  * 50.0f), BeamFCProject.htk, BeamFCProject.cost, BeamFCProject.crew);
                    else
                        Entry = String.Format("Size: {0} HS    HTK: {1}    Cost: {2}    Crew: {3}\n", BeamFCProject.size, BeamFCProject.htk, BeamFCProject.cost, BeamFCProject.crew);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Chance of destruction by electronic damage: {0}%\n", (Hard * 100.0f));
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Materials Required: Not Yet Implemented\n\n");
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Development Cost for Project: {0}RP\n", BeamFCProject.cost * 10);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                }
                break;
                #endregion

                #region CIWS
                case ComponentsViewModel.Components.CIWS:
                break;
                #endregion

                #region Cloak
                case ComponentsViewModel.Components.Cloak:
                break;
                #endregion

                #region EM
                case ComponentsViewModel.Components.EM:

                    if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1)
                    {
                        #region Size
                        /// <summary>
                        /// Pull size out of this mess.
                        /// </summary>
                        if (m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex < 9)
                        {
                            Size = (float)(m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex + 1) / 10.0f;
                        }
                        else if (m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex >= 9 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex <= 13)
                        {
                            Size = 1.0f + (float)((m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex - 9) * 2) / 10.0f;
                        }
                        else if (m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex >= 14 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex <= 25)
                        {
                            Size = 2.0f + (float)(((m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex - 14) * 25) / 100.0f);
                        }
                        else if (m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex > 25)
                        {
                            Size = (float)(m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex - 21);
                        }
                        #endregion

                        #region Hardening
                        /// <summary>
                        /// Get chance of destruction due to electronic damage.
                        /// </summary>
                        switch (m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex)
                        {
                            case 0:
                                Hard = 1.0f;
                            break;
                            case 1:
                                Hard = 0.7f;
                            break;
                            case 2:
                                Hard = 0.5f;
                            break;
                            case 3:
                                Hard = 0.4f;
                            break;
                            case 4:
                                Hard = 0.3f;
                            break;
                            case 5:
                                Hard = 0.25f;
                            break;
                            case 6:
                                Hard = 0.2f;
                            break;
                            case 7:
                                Hard = 0.15f;
                            break;
                            case 8:
                                Hard = 0.1f;
                            break;
                        }
                        #endregion

                        FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EMSensorSensitivity];

                        if (FactTech > 11)
                            FactTech = 11;

                        int EM = FactTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;

                        Entry = "EM Detection Sensor EM";
                        PassiveSensorProject = new PassiveSensorDefTN(Entry, Size, Constants.SensorTN.PassiveStrength[EM], PassiveSensorType.EM, Hard, 
                                                                     (byte)(m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex + 1));

                        if(Hard == 1.0f)
                            Entry = String.Format("{0}{1}-{2}", Entry, Size, PassiveSensorProject.rating);
                        else
                            Entry = String.Format("{0}{1}-{2}({3}%)", Entry, Size, PassiveSensorProject.rating, (Hard * 100.0f));

                        m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                        Entry = String.Format("EM Sensitivity: {0}\n", PassiveSensorProject.rating);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        if(m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                            Entry = String.Format("Sensor Size: {0} Tons    Sensor HTK: {1}\n",(PassiveSensorProject.size*50.0f),PassiveSensorProject.htk);
                        else
                            Entry = String.Format("Sensor Size: {0} HS    Sensor HTK: {1}\n", PassiveSensorProject.size, PassiveSensorProject.htk);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Chance of destruction by electronic damage: {0}%\n", (Hard * 100.0f));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Cost: {0}    Crew: {1}\n", PassiveSensorProject.cost, PassiveSensorProject.crew);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Materials Required: Not Yet Implemented\n\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Development Cost for Project: {0}RP", (PassiveSensorProject.cost * 10));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);


                    }

                    

                break;
                #endregion

                #region Engines
                case ComponentsViewModel.Components.Engine:

                    /// <summary>
                    /// Sanity check.
                    /// </summary>
                if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                    m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex != -1 &&
                    m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex != -1)
                {

                    #region Engine Name

                    FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.EngineBaseTech];

                    if (FactTech > 12)
                        FactTech = 12;

                    int EngineBase = FactTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;


                    switch (EngineBase)
                    {
                        case 0:
                            Entry = "Conventional Engine";
                            break;
                        case 1:
                            Entry = "Nuclear Thermal Engine";
                            break;
                        case 2:
                            Entry = "Nuclear Pulse Engine";
                            break;
                        case 3:
                            Entry = "Ion Drive";
                            break;
                        case 4:
                            Entry = "Magneto-Plasma Drive";
                            break;
                        case 5:
                            Entry = "Internal Fusion Drive";
                            break;
                        case 6:
                            Entry = "Magnetic Fusion Drive";
                            break;
                        case 7:
                            Entry = "Inertial Fusion Drive";
                            break;
                        case 8:
                            Entry = "Solid-Core Antimatter Drive";
                            break;
                        case 9:
                            Entry = "Gas-Core Antimatter Drive";
                            break;
                        case 10:
                            Entry = "Plasma-Core Antimatter Drive";
                            break;
                        case 11:
                            Entry = "Beam-Core Antimatter Drive";
                            break;
                        case 12:
                            Entry = "Photonic Drive";
                            break;
                    }

                    #endregion

                    #region Power Mod
                    int MinPowerTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MinEnginePowerMod];
                    int MaxPowerTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MaxEnginePowerMod];

                    if (MinPowerTech > 5)
                        MinPowerTech = 5;
                    if (MaxPowerTech > 5)
                        MaxPowerTech = 5;

                    int minPower = 50;
                    switch (MinPowerTech)
                    {
                        case 0:
                            minPower = 40;
                            break;
                        case 1:
                            minPower = 30;
                            break;
                        case 2:
                            minPower = 25;
                            break;
                        case 3:
                            minPower = 20;
                            break;
                        case 4:
                            minPower = 15;
                            break;
                        case 5:
                            minPower = 10;
                            break;
                    }

                    float Power = (float)(minPower + (5 * m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex)) / 100.0f;

                    #endregion

                    #region Fuel Consumption

                    FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.FuelConsumption];

                    if (FactTech > 12)
                        FactTech = 12;

                    int FC = FactTech - m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex;

                    #endregion

                    #region Thermal Reduction

                    int TR = m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex;

                    #endregion

                    #region HyperDrive

                    float HD = -1.0f;
                    if(m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex != 0)
                    {
                        FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.HyperdriveSizeMod];

                        if(FactTech > 10)
                            FactTech = 10;

                        HD = Constants.EngineTN.HyperDriveSize[(FactTech - (m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex-1))];
                    }

                    #endregion

                    EngineProject = new EngineDefTN(Entry, Constants.EngineTN.EngineBase[EngineBase], Power, Constants.EngineTN.FuelConsumption[FC],
                                                    Constants.EngineTN.ThermalReduction[TR], (byte)(TR + 1), (m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex + 1), HD);

                    Entry = String.Format("{0} EP {1}", EngineProject.enginePower, Entry);


                    if (EngineProject.isMilitary == false)
                    {
                        Entry = String.Format("Commercial {0}", Entry);
                    }
                    else
                    {
                        Entry = String.Format("Military {0}", Entry);
                    }

                    m_oComponentDesignPanel.TechNameTextBox.Text = Entry;
                    EngineProject.Name = Entry;

                    Entry = String.Format("Engine Power: {0}     Fuel Use Per Hour: {1:N2} Litres\n",EngineProject.enginePower,EngineProject.fuelUsePerHour);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Fuel Consumption per Engine Power Hour: {0:N3} Litres\n",(EngineProject.fuelUsePerHour / EngineProject.enginePower));
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    if (m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                        Entry = String.Format("Engine Size: {0} Tons    Engine HTK: {1}\n", (EngineProject.size*50.0f), EngineProject.htk);
                    else
                        Entry = String.Format("Engine Size: {0} HS    Engine HTK: {1}\n",EngineProject.size, EngineProject.htk);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Thermal Signature: {0}     Exp Chance: {1}\n",EngineProject.thermalSignature,EngineProject.expRisk);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Cost: {0}    Crew: {1}\n",EngineProject.cost,EngineProject.crew);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Materials Required: Not Yet Implemented\n");
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    if (EngineProject.isMilitary == true)
                    {
                        Entry = String.Format("Military Engine\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                    }
                    if (EngineProject.hyperDriveMod != -1.0f)
                    {
                        Entry = String.Format("Hyper Drive\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                    }

                    Entry = String.Format("\nDevelopment Cost for Project: {0}RP\n", (EngineProject.cost * 10));
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                }
                break;
                #endregion

                #region Gauss Cannon
                case ComponentsViewModel.Components.Gauss:
                    /// <summary>
                    /// Sanity check.
                    /// </summary>
                    if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1)
                    {
                        int GaussROF = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.GaussCannonROF];
                        int GaussVel = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.GaussCannonVelocity];

                        if (GaussROF > 6)
                            GaussROF = 6;

                        if (GaussVel > 5)
                            GaussVel = 5;

                        int GR = GaussROF - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;

                        int GV = GaussVel - m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex;

                        int GA = m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex;

                        Entry = String.Format("Gauss Cannon R{0}-{1}",(GV+1),(Constants.BeamWeaponTN.GaussAccuracy[GA] * 100.0f));
                        BeamProject = new BeamDefTN(Entry, ComponentTypeTN.Gauss, (byte)GA, (byte)GV, (byte)GR, 1.0f);

                        m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                        Entry = String.Format("Damage Output 1     Rate of Fire: {0} shots every 5 seconds     Range Modifier: {1}\n",BeamProject.shotCount,(BeamProject.weaponRangeTech+1));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        String FormattedRange = BeamProject.range.ToString("#,###0");
                        if(m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                            Entry = String.Format("Max Range {0} km     Size: {1} Tons    HTK: {2}\n",FormattedRange,(BeamProject.size*50.0f),BeamProject.htk);
                        else
                            Entry = String.Format("Max Range {0} km     Size: {1} HS    HTK: {2}\n", FormattedRange, BeamProject.size, BeamProject.htk);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Cost: {0}    Crew: {1}\n", BeamProject.cost, BeamProject.crew);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                        Entry = String.Format("Materials Required: Not Yet Implemented\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        if (BeamProject.size != 6.0f)
                        {
                            Entry = String.Format("This weapon has a penalty to accuracy. Chance to hit is multiplied by {0}\n",Constants.BeamWeaponTN.GaussAccuracy[GA]);
                            m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                        }
                        Entry = String.Format("\nDevelopment Cost for Project: {0}RP\n",(BeamProject.cost*50));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    }
                break;
                #endregion

                #region High Power Microwave
                case ComponentsViewModel.Components.Microwave:
                /// <summary>
                /// Sanity check.
                /// </summary>
                if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                    m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1)
                {
                    int MicroTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MicrowaveFocal];
                    int MicroFocusTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MicrowaveFocusing];
                    int CapTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                    if (MicroTech > 11)
                        MicroTech = 11;
                    if (MicroFocusTech > 11)
                        MicroFocusTech = 11;
                    if (CapTech > 11)
                        CapTech = 11;

                    int Cal = MicroTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;
                    ComponentTypeTN BeamType = ComponentTypeTN.Meson;
                    int Cap = CapTech - m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex;
                    int Foc = MicroFocusTech - m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex;

                    BeamProject = new BeamDefTN("Microwave", BeamType, (byte)Cal, (byte)Foc, (byte)Cap, 1.0f);

                    float RangeAdjust = BeamProject.range / 10000.0f;

                    Entry = String.Format("R{0}/C{1} High Power Microwave", RangeAdjust, BeamProject.weaponCapacitor);
                    BeamProject.Name = Entry;

                    m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                    String FormattedRange = BeamProject.range.ToString("#,###0");
                    Entry = String.Format("Max Range {0} km     Rate of Fire: {1} seconds     Focus Modifier: {2}\n", FormattedRange, BeamProject.rof, (BeamProject.weaponRangeTech + 1));
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    if (m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                        Entry = String.Format("HPM Size: {0} Tons    HTK: {1}\n", (BeamProject.size * 50.0f), BeamProject.htk);
                    else
                        Entry = String.Format("HPM Size: {0} HS    HTK: {1}\n", BeamProject.size, BeamProject.htk);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Power Requirement: {0}    Power Recharge per 5 Secs: {1}\n", BeamProject.powerRequirement, BeamProject.weaponCapacitor);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Cost: {0}    Crew: {1}\n", BeamProject.cost, BeamProject.crew);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Materials Required: Not Yet Implemented\n");
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("\nDevelopment Cost for Project: {0}RP\n", (BeamProject.cost * 50));
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                }
                break;
                #endregion

                #region Jump Engines
                case ComponentsViewModel.Components.Jump:
                break;
                #endregion

                #region Lasers
                case ComponentsViewModel.Components.Laser:
                /// <summary>
                /// Sanity check.
                /// </summary>
                if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                    m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex != -1 &&
                    m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex != -1)
                {
                    int AdvLaserTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.AdvancedLaserFocal];
                    int LaserTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.LaserFocal];
                    int WaveTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.LaserWavelength];
                    int CapTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];
                    int ReduceTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ReducedSizeLasers];
                    int SpinalTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.SpinalMount];

                    if (AdvLaserTech > 11)
                        AdvLaserTech = 11;
                    if (LaserTech > 11)
                        LaserTech = 11;
                    if (WaveTech > 11)
                        WaveTech = 11;
                    if (CapTech > 11)
                        CapTech = 11;
                    if (ReduceTech > 1)
                        ReduceTech = 1;
                    if (SpinalTech > 1)
                        SpinalTech = 1;

                    int Cal = -1;
                    ComponentTypeTN BeamType = ComponentTypeTN.TypeCount;
                    int Cap = CapTech - m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex;
                    int Vel = WaveTech - m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex;
                    int Reduce = (ReduceTech - m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex + 1);
                    int Spinal = (SpinalTech - m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex  + 1);
                    float red = 1.0f;
                    BeamDefTN.MountType MType = BeamDefTN.MountType.Standard;

                    if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex > AdvLaserTech)
                    {
                        Cal = LaserTech - (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex - AdvLaserTech - 1);
                        BeamType = ComponentTypeTN.Laser;

                        float Capacitor = Constants.BeamWeaponTN.Capacitor[Cap];
                        switch (Reduce)
                        {
                            case 1:
                                Capacitor = Capacitor / 4.0f;
                                red = 0.75f;
                                break;
                            case 0:
                                Capacitor = Capacitor / 20.0f;
                                red = 0.5f;
                                break;
                        }

                        String CapString = "N/A";
                        if (red == 1.0f)
                            CapString = String.Format("{0}", Capacitor);
                        else
                            CapString = String.Format("{0:N1}", Capacitor);


                        String Calibre = Constants.BeamWeaponTN.SizeClass[Cal].ToString();
                        switch (Spinal)
                        {
                            case 1:
                                MType = BeamDefTN.MountType.Spinal;
                                Calibre = Math.Round(Constants.BeamWeaponTN.SizeClass[Cal] * 1.25f).ToString();
                                break;
                            case 0:
                                MType = BeamDefTN.MountType.AdvancedSpinal;
                                Calibre = Math.Round(Constants.BeamWeaponTN.SizeClass[Cal] * 1.5f).ToString();
                                break;
                        }


                        Entry = String.Format("{0}cm C{1} {2}", Calibre , CapString , m_oComponentDesignPanel.TechComboBoxTwo.SelectedItem);
                    

                    }
                    else
                    {
                        Cal = AdvLaserTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;
                        BeamType = ComponentTypeTN.AdvLaser;

                        float Capacitor = Constants.BeamWeaponTN.Capacitor[Cap];
                        switch (Reduce)
                        {
                            case 1:
                                Capacitor = Capacitor / 4.0f;
                                red = 0.75f;
                                break;
                            case 0:
                                Capacitor = Capacitor / 20.0f;
                                red = 0.5f;
                                break;
                        }

                        String CapString = "N/A";
                        if(red == 1.0f)
                            CapString = String.Format("{0}", Capacitor);
                        else
                            CapString = String.Format("{0:N1}", Capacitor);


                        String Calibre = Constants.BeamWeaponTN.SizeClass[Cal].ToString();
                        switch (Spinal)
                        {
                            case 1:
                                MType = BeamDefTN.MountType.Spinal;
                                Calibre = Math.Round(Constants.BeamWeaponTN.SizeClass[Cal] * 1.25f).ToString();
                                break;
                            case 0:
                                MType = BeamDefTN.MountType.AdvancedSpinal;
                                Calibre = Math.Round(Constants.BeamWeaponTN.SizeClass[Cal] * 1.5f).ToString();
                                break;
                        }


                        Entry = String.Format("{0}cm C{1} Advanced {2}", Calibre, CapString, m_oComponentDesignPanel.TechComboBoxTwo.SelectedItem);
                    }
                    m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                    BeamProject = new BeamDefTN(Entry, BeamType, (byte)Cal, (byte)Vel, (byte)Cap, red, MType);

                    Entry = String.Format("Damage Output {0}     Rate of Fire: {1} seconds     Range Modifier: {2}\n", BeamProject.damage[0], BeamProject.rof, (Vel+1));
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    String FormattedRange = BeamProject.range.ToString("#,###0");

                    if (m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                        Entry = String.Format("Max Range {0} km     Laser Size: {1} Tons    Laser HTK: {2}\n", FormattedRange, (BeamProject.size * 50.0f), BeamProject.htk);
                    else
                        Entry = String.Format("Max Range {0} km     Laser Size: {1} HS    Laser HTK: {2}\n", FormattedRange, BeamProject.size, BeamProject.htk);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Power Requirement: {0}    Power Recharge per 5 Secs: {1}\n", BeamProject.powerRequirement, BeamProject.weaponCapacitor);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Cost: {0}    Crew: {1}\n", BeamProject.cost, BeamProject.crew);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    if (Spinal != 2)
                    {
                        m_oComponentDesignPanel.ParametersTextBox.AppendText("Spinal Weapon Only\n");
                    }

                    Entry = String.Format("Materials Required: Not Yet Implemented\n");
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("\nDevelopment Cost for Project: {0}RP\n", (BeamProject.cost * 50));
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                }
                break;
                #endregion

                #region Magazines
                case ComponentsViewModel.Components.Magazine:
                break;
                #endregion

                #region Meson Cannons
                case ComponentsViewModel.Components.Meson:
                    /// <summary>
                    /// Sanity check.
                    /// </summary>
                if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                    m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1)
                {
                        int MesonTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MesonFocal];
                        int MesonFocusTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.MesonFocusing];
                        int CapTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                        if (MesonTech > 11)
                             MesonTech = 11;
                        if (MesonFocusTech > 11)
                            MesonFocusTech = 11;
                        if (CapTech > 11)
                            CapTech = 11;

                        int Cal = MesonTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;
                        ComponentTypeTN BeamType = ComponentTypeTN.Meson;
                        int Cap = CapTech - m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex;
                        int Foc = MesonFocusTech - m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex;

                        BeamProject = new BeamDefTN("Meson",BeamType,(byte)Cal,(byte)Foc,(byte)Cap,1.0f);

                        float RangeAdjust = BeamProject.range / 10000.0f;

                        Entry = String.Format("R{0}/C{1} Meson Cannon", RangeAdjust, BeamProject.weaponCapacitor);
                        BeamProject.Name = Entry;

                        m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                        String FormattedRange = BeamProject.range.ToString("#,###0");
                        Entry = String.Format("Max Range {0} km     Rate of Fire: {1} seconds     Focus Modifier: {2}\n",FormattedRange,BeamProject.rof,(BeamProject.weaponRangeTech+1));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        if (m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                            Entry = String.Format("Meson Cannon Size: {0} Tons    Meson Cannon HTK: {1}\n", (BeamProject.size*50.0f), BeamProject.htk);
                        else
                            Entry = String.Format("Meson Cannon Size: {0} HS    Meson Cannon HTK: {1}\n",BeamProject.size,BeamProject.htk);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Power Requirement: {0}    Power Recharge per 5 Secs: {1}\n",BeamProject.powerRequirement,BeamProject.weaponCapacitor);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Cost: {0}    Crew: {1}\n",BeamProject.cost, BeamProject.crew);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Materials Required: Not Yet Implemented\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("\nDevelopment Cost for Project: {0}RP\n",(BeamProject.cost * 50));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                }
                break;
                #endregion

                #region Missile Engines
                case ComponentsViewModel.Components.MissileEngine:
                break;
                #endregion

                #region Missile Launcher
                case ComponentsViewModel.Components.MissileLauncher:
                break;
                #endregion

                #region New Species
                case ComponentsViewModel.Components.NewSpecies:
                break;
                #endregion

                #region Particle beams
                case ComponentsViewModel.Components.Particle:
                    /// <summary>
                    /// Sanity check.
                    /// </summary>
                    if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1)
                    {
                        int AdvPartTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.AdvancedParticleBeamStrength];
                        int PartTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ParticleBeamStrength];
                        int PartRangeTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ParticleBeamRange];
                        int CapTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                        if (AdvPartTech > 10)
                            AdvPartTech = 10;
                        if (PartTech > 10)
                            PartTech = 10;
                        if (PartRangeTech > 11)
                            PartRangeTech = 11;
                        if (CapTech > 11)
                            CapTech = 11;

                        int Cal = -1;
                        ComponentTypeTN BeamType = ComponentTypeTN.TypeCount;
                        int Range = PartRangeTech - m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex;
                        int Cap = CapTech - m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex;

                        if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex > AdvPartTech)
                        {
                            BeamType = ComponentTypeTN.Particle;
                            Cal = PartTech - (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex - AdvPartTech - 1);

                            float RangeAdjust = (float)Constants.BeamWeaponTN.ParticleRange[Cal] / 10.0f;
                            Entry = String.Format("Particle Beam-{0} C{1}/R{2}",Constants.BeamWeaponTN.ParticleDamage[Cal],Constants.BeamWeaponTN.Capacitor[Cap],RangeAdjust);
                        }
                        else
                        {
                            BeamType = ComponentTypeTN.AdvParticle;
                            Cal = AdvPartTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;

                            float RangeAdjust = (float)Constants.BeamWeaponTN.ParticleRange[Cal] / 10.0f;
                            Entry = String.Format("Advanced Particle Beam-{0} C{1}/R{2}", Constants.BeamWeaponTN.ParticleDamage[Cal], Constants.BeamWeaponTN.Capacitor[Cap], RangeAdjust);
                        }

                        BeamProject = new BeamDefTN(Entry, BeamType, (byte)Cal, (byte)Range, (byte)Cap, 1.0f);

                        m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                        String FormattedRange = BeamProject.range.ToString("#,###0");

                        Entry = String.Format("Beam Strength {0}     Rate of Fire: {1} seconds     Maximum Range: {2} km\n", BeamProject.damage[0], BeamProject.rof, FormattedRange);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        if (m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                            Entry = String.Format("Launcher Size: {0} Tons    Launcher HTK: {1}\n", (BeamProject.size * 50.0f), BeamProject.htk);
                        else
                            Entry = String.Format("Launcher Size: {0} HS    Launcher HTK: {1}\n", BeamProject.size, BeamProject.htk);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Power Requirement: {0}    Power Recharge per 5 Secs: {1}\n", BeamProject.powerRequirement, BeamProject.weaponCapacitor);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Cost: {0}    Crew: {1}\n", BeamProject.cost, BeamProject.crew);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Materials Required: Not Yet Implemented\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("\nDevelopment Cost for Project: {0}RP\n", (BeamProject.cost * 50));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                    }
                break;
                #endregion

                #region Plasma Carronades
                case ComponentsViewModel.Components.Plasma:

                    /// <summary>
                    /// Sanity check.
                    /// </summary>
                    if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1)
                    {

                        int AdvPlasmaTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.AdvancedPlasmaCarronadeCalibre];
                        int PlasmaTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.PlasmaCarronadeCalibre];
                        int CapTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                        if (AdvPlasmaTech > 9)
                            AdvPlasmaTech = 9;
                        if (PlasmaTech > 9)
                            PlasmaTech = 9;
                        if (CapTech > 11)
                            CapTech = 11;

                        int Cal = -1;
                        ComponentTypeTN BeamType = ComponentTypeTN.TypeCount;
                        int Cap = CapTech - m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex;

                        if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex > AdvPlasmaTech)
                        {
                            Cal = PlasmaTech - (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex - AdvPlasmaTech - 1);
                            BeamType = ComponentTypeTN.Plasma;

                            Entry = String.Format("{0}cm C{1} Carronade",Constants.BeamWeaponTN.SizeClass[Cal+2],Constants.BeamWeaponTN.Capacitor[Cap]);
                        }
                        else
                        {
                            Cal = AdvPlasmaTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;
                            BeamType = ComponentTypeTN.AdvPlasma;

                            Entry = String.Format("{0}cm C{1} Advanced Carronade", Constants.BeamWeaponTN.SizeClass[Cal + 2], Constants.BeamWeaponTN.Capacitor[Cap]);
                        }


                        BeamProject = new BeamDefTN(Entry, BeamType, (byte)(Cal + 2), 1, (byte)Cap, 1.0f);

                        m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                        Entry = String.Format("Damage Output {0}     Rate of Fire: {1} seconds\n",BeamProject.damage[0],BeamProject.rof);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        String FormattedRange = BeamProject.range.ToString("#,###0");

                        if(m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                            Entry = String.Format("Max Range {0} km     Carronade Size: {1} Tons    Carronade HTK: {2}\n", FormattedRange, (BeamProject.size * 50.0f), BeamProject.htk);
                        else
                            Entry = String.Format("Max Range {0} km     Carronade Size: {1} HS    Carronade HTK: {2}\n", FormattedRange, BeamProject.size, BeamProject.htk);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Power Requirement: {0}    Power Recharge per 5 Secs: {1}\n",BeamProject.powerRequirement,BeamProject.weaponCapacitor);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Cost: {0}    Crew: {1}\n",BeamProject.cost, BeamProject.crew);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Materials Required: Not Yet Implemented\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("\nDevelopment Cost for Project: {0}RP\n",(BeamProject.cost*50));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                    }


                break;
                #endregion

                #region Plasma Torpedos
                case ComponentsViewModel.Components.PlasmaTorp:
                break;
                #endregion

                #region Power Plants
                case ComponentsViewModel.Components.Reactor:

                    /// <summary>
                    /// Sanity check.
                    /// </summary>
                    if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1)
                    {

                        #region Get boost
                        switch (m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex)
                        {
                            case 0:
                                Boost = 1.0f;
                            break;
                            case 1:
                                Boost = 1.05f;
                            break;
                            case 2:
                                Boost = 1.10f;
                            break;
                            case 3:
                                Boost = 1.15f;
                            break;
                            case 4:
                                Boost = 1.2f;
                            break;
                            case 5:
                                Boost = 1.25f;
                            break;
                            case 6:
                                Boost = 1.3f;
                            break;
                            case 7:
                                Boost = 1.4f;
                            break;
                            case 8:
                                Boost = 1.5f;
                            break;
                        }
                        #endregion

                        #region Get size
                        switch (m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex)
                        {
                            case 0:
                                Size = 0.1f;
                            break;
                            case 1:
                                Size = 0.2f;
                            break;
                            case 2:
                                Size = 0.5f;
                            break;
                            default:
                                Size = (float)(m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex - 2);
                            break;
                        }
                        #endregion

                        #region Set Name. Hard coded again, DB here as well.
                        switch (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex)
                        {
                            case 0:
                                Entry = "Vacuum Energy Power Plant Technology";
                            break;
                            case 1:
                                Entry = "Beam-Core Antimatter Reactor Technology";
                            break;
                            case 2:
                                Entry = "Plasma-Core Antimatter Reactor Technology";
                            break;
                            case 3:
                                Entry = "Gas-Core Antimatter Reactor Technology";
                            break;
                            case 4:
                                Entry = "Solid-Core Antimatter Reactor Technology";
                            break;
                            case 5:
                                Entry = "Inertial Confinement Fusion Reactor Technology";
                            break;
                            case 6:
                                Entry = "Magnetic Confinement Fusion Reactor Technology";
                            break;
                            case 7:
                                Entry = "Tokamak Fusion Reactor Technology";
                            break;
                            case 8:
                                Entry = "Stellarator Fusion Reactor Technology";
                            break;
                            case 9:
                                Entry = "Gas Cooled Fast Reactor Technology";
                            break;
                            case 10:
                                Entry = "Pebble Bed Reactor Technology";
                            break;
                            case 11:
                                Entry = "Pressurised Water Reactor Technology";
                            break;
                        }
                        #endregion

                        if (Boost == 1.0f)
                            Entry = String.Format("{0} PB-1", Entry);
                        else
                            Entry = String.Format("{0} PB-{1}", Entry, Boost);

                        ReactorProject = new ReactorDefTN(Entry, (byte)(11 - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex), Size, Boost);

                        m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                        Entry = String.Format("Power Output: {0}     Explosion Chance: {1}\n",ReactorProject.powerGen, ReactorProject.expRisk);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        if(m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                            Entry = String.Format("Reactor Size: {0} Tons    Reactor HTK: {1}\n",(ReactorProject.size*50.0f),ReactorProject.htk);
                        else
                            Entry = String.Format("Reactor Size: {0} HS    Reactor HTK: {1}\n", ReactorProject.size, ReactorProject.htk);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Cost: {0}    Crew: {1}\n",ReactorProject.cost, ReactorProject.crew);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Materials Required: Not Yet Implemented\n\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Development Cost for Project: {0}RP\n",(ReactorProject.cost) * 10);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                    }

                break;
                #endregion

                #region Railguns
                case ComponentsViewModel.Components.Rail:
                    /// <summary>
                    /// Sanity check.
                    /// </summary>
                    if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1)
                    {
                        int AdvRailTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.AdvancedRailgun];
                        int RailTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.Railgun];
                        int RailVelTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.RailgunVelocity];
                        int CapTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.CapacitorChargeRate];

                        if (AdvRailTech > 9)
                            AdvRailTech = 9;
                        if (RailTech > 9)
                            RailTech = 9;
                        if (RailVelTech > 9)
                            RailVelTech = 9;
                        if (CapTech > 11)
                            CapTech = 11;

                        int Cal = -1;
                        ComponentTypeTN BeamType = ComponentTypeTN.TypeCount;
                        int Cap = CapTech - m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex;
                        int Vel = RailVelTech - m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex;

                        if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex > AdvRailTech)
                        {
                            Cal = RailTech - (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex - AdvRailTech - 1);
                            BeamType = ComponentTypeTN.Rail;


                            /// <summary>
                            /// This is the counterpart to the earlier kludge of allowing 45cm rail guns:
                            /// </summary>
                            switch (Cal)
                            {
                                case 9:
                                    Entry = String.Format("{0}cm Railgun V{1}/C{2}", "50", Vel, Constants.BeamWeaponTN.Capacitor[Cap]);
                                    break;
                                case 8:
                                    Entry = String.Format("{0}cm Railgun V{1}/C{2}", "45", Vel, Constants.BeamWeaponTN.Capacitor[Cap]);
                                    break;
                                default:
                                    Entry = String.Format("{0}cm Railgun V{1}/C{2}", Constants.BeamWeaponTN.SizeClass[Cal], Vel, Constants.BeamWeaponTN.Capacitor[Cap]);
                                    break;
                            }
                        }
                        else
                        {
                            Cal = AdvRailTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;
                            BeamType = ComponentTypeTN.AdvRail;

                            switch (Cal)
                            {
                                case 9:
                                    Entry = String.Format("{0}cm Advanced Railgun V{1}/C{2}", "50", Vel, Constants.BeamWeaponTN.Capacitor[Cap]);
                                    break;
                                case 8:
                                    Entry = String.Format("{0}cm Advanced Railgun V{1}/C{2}", "45", Vel, Constants.BeamWeaponTN.Capacitor[Cap]);
                                    break;
                                default:
                                    Entry = String.Format("{0}cm Advanced Railgun V{1}/C{2}", Constants.BeamWeaponTN.SizeClass[Cal], Vel, Constants.BeamWeaponTN.Capacitor[Cap]);
                                    break;
                            }
                        }

                        m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                        BeamProject = new BeamDefTN(Entry, BeamType, (byte)Cal, (byte)Vel, (byte)Cap, 1.0f);

                        Entry = String.Format("Damage Per Shot ({0}): {1}     Rate of Fire: {2} seconds     Range Modifier: {3}\n", BeamProject.shotCount, BeamProject.damage[0], BeamProject.rof, Vel);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        String FormattedRange = BeamProject.range.ToString("#,###0");

                        if(m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                            Entry = String.Format("Max Range {0} km     Railgun Size: {1} Tons    Railgun HTK: {2}\n", FormattedRange, (BeamProject.size*50.0f), BeamProject.htk);
                        else
                            Entry = String.Format("Max Range {0} km     Railgun Size: {1} HS    Railgun HTK: {2}\n", FormattedRange, BeamProject.size, BeamProject.htk);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Power Requirement: {0}    Power Recharge per 5 Secs: {1}\n", BeamProject.powerRequirement, BeamProject.weaponCapacitor);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Cost: {0}    Crew: {1}\n", BeamProject.cost, BeamProject.crew);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Materials Required: Not Yet Implemented\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("\nDevelopment Cost for Project: {0}RP\n", (BeamProject.cost*50));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                    }
                break;
                #endregion

                #region Absorption Shields
                case ComponentsViewModel.Components.ShieldAbs:
                break;
                #endregion

                #region Standard Shields
                case ComponentsViewModel.Components.ShieldStd:

                    /// <summary>
                    /// Sanity check.
                    /// </summary>
                    if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                        m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1)
                    {

                        FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.StandardShieldStrength];

                        if (FactTech > 11)
                            FactTech = 11;

                        int SS = FactTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;

                        FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.StandardShieldRegen];

                        if (FactTech > 11)
                            FactTech = 11;

                        int SR = FactTech - m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex;

                        FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.FuelConsumption];

                        if (FactTech > 12)
                            FactTech = 12;

                        int FC = FactTech - m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex;

                        #region name
                        switch (SS)
                        {
                            case 0:
                                Entry = "Alpha";
                            break;
                            case 1:
                                Entry = "Beta";
                            break;
                            case 2:
                                Entry = "Gamma";
                            break;
                            case 3:
                                Entry = "Delta";
                            break;
                            case 4:
                                Entry = "Epsilon";
                            break;
                            case 5:
                                Entry = "Theta";
                            break;
                            case 6:
                                Entry = "Xi";
                            break;
                            case 7:
                                Entry = "Omicron";
                            break;
                            case 8:
                                Entry = "Sigma";
                            break;
                            case 9:
                                Entry = "Tau";
                            break;
                            case 10:
                                Entry = "Psi";
                            break;
                            case 11:
                                Entry = "Omega";
                            break;
                        }
                        #endregion

                        ShieldProject = new ShieldDefTN(Entry, SS, SR, Constants.EngineTN.FuelConsumption[FC], 1.0f, ComponentTypeTN.Shield);

                        float RechargeTime = (ShieldProject.shieldPool / ShieldProject.shieldGen) * 300.0f;

                        Entry = String.Format("{0} R{1}/{2} Shields",Entry,RechargeTime,(ShieldProject.fuelCostPerHour * 24.0f));

                        ShieldProject.Name = Entry;

                        m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                        Entry = String.Format("Shield Strength: {0}\n", ShieldProject.shieldPool);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Recharge Rate: {0}    Recharge Time: {1} seconds\n",ShieldProject.shieldGen,RechargeTime);
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Cost: {0}    Crew: {1}     Daily Fuel Cost while Active: {2} Litres\n",ShieldProject.cost, ShieldProject.crew, (ShieldProject.fuelCostPerHour * 24.0f));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Materials Required: Not Yet Implemented\n\n");
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                        Entry = String.Format("Development Cost for Project: {0}RP\n", (ShieldProject.cost*100));
                        m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);
                    }

                break;
                #endregion

                #region Thermal Sensors
                case ComponentsViewModel.Components.Thermal:
                if (m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex != -1 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex != -1 &&
                    m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex != -1)
                {
                    #region Size
                    /// <summary>
                    /// Pull size out of this mess.
                    /// </summary>
                    if (m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex < 9)
                    {
                        Size = (float)(m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex + 1) / 10.0f;
                    }
                    else if (m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex >= 9 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex <= 13)
                    {
                        Size = 1.0f + (float)((m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex - 9) * 2) / 10.0f;
                    }
                    else if (m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex >= 14 && m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex <= 25)
                    {
                        Size = 2.0f + (float)(((m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex - 14) * 25) / 100.0f);
                    }
                    else if (m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex > 25)
                    {
                        Size = (float)(m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex - 21);
                    }
                    #endregion

                    #region Hardening
                    /// <summary>
                    /// Get chance of destruction due to electronic damage.
                    /// </summary>
                    switch (m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex)
                    {
                        case 0:
                            Hard = 1.0f;
                            break;
                        case 1:
                            Hard = 0.7f;
                            break;
                        case 2:
                            Hard = 0.5f;
                            break;
                        case 3:
                            Hard = 0.4f;
                            break;
                        case 4:
                            Hard = 0.3f;
                            break;
                        case 5:
                            Hard = 0.25f;
                            break;
                        case 6:
                            Hard = 0.2f;
                            break;
                        case 7:
                            Hard = 0.15f;
                            break;
                        case 8:
                            Hard = 0.1f;
                            break;
                    }
                    #endregion

                    FactTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ThermalSensorSensitivity];

                    if (FactTech > 11)
                        FactTech = 11;

                    int TH = FactTech - m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex;

                    Entry = "Thermal Sensor TH";
                    PassiveSensorProject = new PassiveSensorDefTN(Entry, Size, Constants.SensorTN.PassiveStrength[TH], PassiveSensorType.Thermal, Hard,
                                                                 (byte)(m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex + 1));

                    if (Hard == 1.0f)
                        Entry = String.Format("{0}{1}-{2}", Entry, Size, PassiveSensorProject.rating);
                    else
                        Entry = String.Format("{0}{1}-{2}({3}%)", Entry, Size, PassiveSensorProject.rating, (Hard * 100.0f));

                    m_oComponentDesignPanel.TechNameTextBox.Text = Entry;

                    Entry = String.Format("Thermal Sensor Sensitivity: {0}\n", PassiveSensorProject.rating);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    if (m_oComponentDesignPanel.SizeTonsCheckBox.Checked == true)
                        Entry = String.Format("Sensor Size: {0} Tons    Sensor HTK: {1}\n", (PassiveSensorProject.size * 50.0f), PassiveSensorProject.htk);
                    else
                        Entry = String.Format("Sensor Size: {0} HS    Sensor HTK: {1}\n", PassiveSensorProject.size, PassiveSensorProject.htk);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Chance of destruction by electronic damage: {0}%\n", (Hard * 100.0f));
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Cost: {0}    Crew: {1}\n", PassiveSensorProject.cost, PassiveSensorProject.crew);
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Materials Required: Not Yet Implemented\n\n");
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);

                    Entry = String.Format("Development Cost for Project: {0}RP", (PassiveSensorProject.cost * 10));
                    m_oComponentDesignPanel.ParametersTextBox.AppendText(Entry);


                }
                break;
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// This is a helper function that should reduce the clutter in background tech. visibility for each of the seven labels and combo boxes needs to be controlled and set.
        /// This is done here. No I couldn't figure out how to make an array of tech combo boxes, why do you ask?
        /// </summary>
        /// <param name="count">1 to 7 indicates which tech combo boxes should be visible.</param>
        private void SetTechVisible(int count)
        {

            #region Switch visibility settings
            switch (count)
            {
                case 1 :
                    m_oComponentDesignPanel.TechComboBoxOne.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxTwo.Visible   = false;
                    m_oComponentDesignPanel.TechComboBoxThree.Visible = false;
                    m_oComponentDesignPanel.TechComboBoxFour.Visible  = false;
                    m_oComponentDesignPanel.TechComboBoxFive.Visible  = false;
                    m_oComponentDesignPanel.TechComboBoxSix.Visible   = false;
                    m_oComponentDesignPanel.TechComboBoxSeven.Visible = false;

                    m_oComponentDesignPanel.TechLabelOne.Visible   = true;
                    m_oComponentDesignPanel.TechLabelTwo.Visible   = false;
                    m_oComponentDesignPanel.TechLabelThree.Visible = false;
                    m_oComponentDesignPanel.TechLabelFour.Visible  = false;
                    m_oComponentDesignPanel.TechLabelFive.Visible  = false;
                    m_oComponentDesignPanel.TechLabelSix.Visible   = false;
                    m_oComponentDesignPanel.TechLabelSeven.Visible = false;
                break;
                case 2 :
                    m_oComponentDesignPanel.TechComboBoxOne.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxTwo.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxThree.Visible = false;
                    m_oComponentDesignPanel.TechComboBoxFour.Visible  = false;
                    m_oComponentDesignPanel.TechComboBoxFive.Visible  = false;
                    m_oComponentDesignPanel.TechComboBoxSix.Visible   = false;
                    m_oComponentDesignPanel.TechComboBoxSeven.Visible = false;

                    m_oComponentDesignPanel.TechLabelOne.Visible   = true;
                    m_oComponentDesignPanel.TechLabelTwo.Visible   = true;
                    m_oComponentDesignPanel.TechLabelThree.Visible = false;
                    m_oComponentDesignPanel.TechLabelFour.Visible  = false;
                    m_oComponentDesignPanel.TechLabelFive.Visible  = false;
                    m_oComponentDesignPanel.TechLabelSix.Visible   = false;
                    m_oComponentDesignPanel.TechLabelSeven.Visible = false;
                break;
                case 3 :
                    m_oComponentDesignPanel.TechComboBoxOne.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxTwo.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxThree.Visible = true;
                    m_oComponentDesignPanel.TechComboBoxFour.Visible  = false;
                    m_oComponentDesignPanel.TechComboBoxFive.Visible  = false;
                    m_oComponentDesignPanel.TechComboBoxSix.Visible   = false;
                    m_oComponentDesignPanel.TechComboBoxSeven.Visible = false;

                    m_oComponentDesignPanel.TechLabelOne.Visible   = true;
                    m_oComponentDesignPanel.TechLabelTwo.Visible   = true;
                    m_oComponentDesignPanel.TechLabelThree.Visible = true;
                    m_oComponentDesignPanel.TechLabelFour.Visible  = false;
                    m_oComponentDesignPanel.TechLabelFive.Visible  = false;
                    m_oComponentDesignPanel.TechLabelSix.Visible   = false;
                    m_oComponentDesignPanel.TechLabelSeven.Visible = false;
                break;
                case 4 :
                    m_oComponentDesignPanel.TechComboBoxOne.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxTwo.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxThree.Visible = true;
                    m_oComponentDesignPanel.TechComboBoxFour.Visible  = true;
                    m_oComponentDesignPanel.TechComboBoxFive.Visible  = false;
                    m_oComponentDesignPanel.TechComboBoxSix.Visible   = false;
                    m_oComponentDesignPanel.TechComboBoxSeven.Visible = false;

                    m_oComponentDesignPanel.TechLabelOne.Visible   = true;
                    m_oComponentDesignPanel.TechLabelTwo.Visible   = true;
                    m_oComponentDesignPanel.TechLabelThree.Visible = true;
                    m_oComponentDesignPanel.TechLabelFour.Visible  = true;
                    m_oComponentDesignPanel.TechLabelFive.Visible  = false;
                    m_oComponentDesignPanel.TechLabelSix.Visible   = false;
                    m_oComponentDesignPanel.TechLabelSeven.Visible = false;
                break;
                case 5 :
                    m_oComponentDesignPanel.TechComboBoxOne.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxTwo.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxThree.Visible = true;
                    m_oComponentDesignPanel.TechComboBoxFour.Visible  = true;
                    m_oComponentDesignPanel.TechComboBoxFive.Visible  = true;
                    m_oComponentDesignPanel.TechComboBoxSix.Visible   = false;
                    m_oComponentDesignPanel.TechComboBoxSeven.Visible = false;

                    m_oComponentDesignPanel.TechLabelOne.Visible   = true;
                    m_oComponentDesignPanel.TechLabelTwo.Visible   = true;
                    m_oComponentDesignPanel.TechLabelThree.Visible = true;
                    m_oComponentDesignPanel.TechLabelFour.Visible  = true;
                    m_oComponentDesignPanel.TechLabelFive.Visible  = true;
                    m_oComponentDesignPanel.TechLabelSix.Visible   = false;
                    m_oComponentDesignPanel.TechLabelSeven.Visible = false;
                break;
                case 6 :
                    m_oComponentDesignPanel.TechComboBoxOne.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxTwo.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxThree.Visible = true;
                    m_oComponentDesignPanel.TechComboBoxFour.Visible  = true;
                    m_oComponentDesignPanel.TechComboBoxFive.Visible  = true;
                    m_oComponentDesignPanel.TechComboBoxSix.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxSeven.Visible = false;

                    m_oComponentDesignPanel.TechLabelOne.Visible   = true;
                    m_oComponentDesignPanel.TechLabelTwo.Visible   = true;
                    m_oComponentDesignPanel.TechLabelThree.Visible = true;
                    m_oComponentDesignPanel.TechLabelFour.Visible  = true;
                    m_oComponentDesignPanel.TechLabelFive.Visible  = true;
                    m_oComponentDesignPanel.TechLabelSix.Visible   = true;
                    m_oComponentDesignPanel.TechLabelSeven.Visible = false;
                break;
                case 7 :
                    m_oComponentDesignPanel.TechComboBoxOne.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxTwo.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxThree.Visible = true;
                    m_oComponentDesignPanel.TechComboBoxFour.Visible  = true;
                    m_oComponentDesignPanel.TechComboBoxFive.Visible  = true;
                    m_oComponentDesignPanel.TechComboBoxSix.Visible   = true;
                    m_oComponentDesignPanel.TechComboBoxSeven.Visible = true;

                    m_oComponentDesignPanel.TechLabelOne.Visible   = true;
                    m_oComponentDesignPanel.TechLabelTwo.Visible   = true;
                    m_oComponentDesignPanel.TechLabelThree.Visible = true;
                    m_oComponentDesignPanel.TechLabelFour.Visible  = true;
                    m_oComponentDesignPanel.TechLabelFive.Visible  = true;
                    m_oComponentDesignPanel.TechLabelSix.Visible   = true;
                    m_oComponentDesignPanel.TechLabelSeven.Visible = true;
                break;

            }
            #endregion

            m_oComponentDesignPanel.TechComboBoxOne.SelectedIndex = -1;
            m_oComponentDesignPanel.TechComboBoxOne.Items.Clear();
            m_oComponentDesignPanel.TechComboBoxTwo.SelectedIndex = -1;
            m_oComponentDesignPanel.TechComboBoxTwo.Items.Clear();
            m_oComponentDesignPanel.TechComboBoxThree.SelectedIndex = -1;
            m_oComponentDesignPanel.TechComboBoxThree.Items.Clear();
            m_oComponentDesignPanel.TechComboBoxFour.SelectedIndex = -1;
            m_oComponentDesignPanel.TechComboBoxFour.Items.Clear();
            m_oComponentDesignPanel.TechComboBoxFive.SelectedIndex = -1;
            m_oComponentDesignPanel.TechComboBoxFive.Items.Clear();
            m_oComponentDesignPanel.TechComboBoxSix.SelectedIndex = -1;
            m_oComponentDesignPanel.TechComboBoxSix.Items.Clear();
            m_oComponentDesignPanel.TechComboBoxSeven.SelectedIndex = -1;
            m_oComponentDesignPanel.TechComboBoxSeven.Items.Clear();

            m_oComponentDesignPanel.TechLabelOne.Text   = "";
            m_oComponentDesignPanel.TechLabelTwo.Text   = "";
            m_oComponentDesignPanel.TechLabelThree.Text = "";
            m_oComponentDesignPanel.TechLabelFour.Text  = "";
            m_oComponentDesignPanel.TechLabelFive.Text  = "";
            m_oComponentDesignPanel.TechLabelSix.Text   = "";
            m_oComponentDesignPanel.TechLabelSeven.Text = "";

            m_oComponentDesignPanel.NotesLabel.Text = "";
        }

        /// <summary>
        /// Another helper to set each text label, One through seven go into tech labels one through seven
        /// </summary>
        /// <param name="One"></param>
        /// <param name="Two"></param>
        /// <param name="Three"></param>
        /// <param name="Four"></param>
        /// <param name="Five"></param>
        /// <param name="Six"></param>
        /// <param name="Seven"></param>
        private void SetLabels(String One, String Two, String Three, String Four, String Five, String Six, String Seven)
        {
            m_oComponentDesignPanel.TechLabelOne.Text   = One;
            m_oComponentDesignPanel.TechLabelTwo.Text   = Two;
            m_oComponentDesignPanel.TechLabelThree.Text = Three;
            m_oComponentDesignPanel.TechLabelFour.Text  = Four;
            m_oComponentDesignPanel.TechLabelFive.Text  = Five;
            m_oComponentDesignPanel.TechLabelSix.Text   = Six;
            m_oComponentDesignPanel.TechLabelSeven.Text = Seven;
        }
    }
}