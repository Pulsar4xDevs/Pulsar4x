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

            m_oComponentDesignPanel.CloseButton.Click += new EventHandler(CloseButton_Click);

            m_oComponentDesignPanel.ResizeBegin +=new EventHandler(ResizeBegin);
            m_oComponentDesignPanel.ResizeEnd += new EventHandler(ResizeEnd);
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
        /// Closes the dialogbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_Click(object sender, EventArgs e)
        {
            m_oComponentDesignPanel.Close();
        }


        /// <summary>
        /// Builds the display for the background technology group box
        /// </summary>
        private void BuildBackgroundTech()
        {
            if (m_oComponentDesignPanel.ResearchComboBox.SelectedIndex != -1)
            {
                switch ((ComponentsViewModel.Components)m_oComponentDesignPanel.ResearchComboBox.SelectedIndex)
                {
                    #region Active Sensors / MFC
                    case ComponentsViewModel.Components.ActiveMFC:

                        SetTechVisible(6);

                        SetLabels("Active Sensor Strength","EM Sensor Sensitivity","Total Sensor Size","Minimum Resolution","Hardening","Active Sensor Type","");

                        m_oComponentDesignPanel.NotesLabel.Text = "Sensor Range = Active Strength x Size x SQRT(Resolution) x EM Sensitivity x 10,000 km\nMissile Fire Controls multiply this by 3 in exchange for only being able to guide missiles, while losing the ability to perform active scanning.";

                        
                    break;
                    #endregion

                    #region Beam Fire Control
                    case ComponentsViewModel.Components.BFC:

                        SetTechVisible(7);

                        SetLabels("Beam Fire Control Distance Rating","Fire Control Speed Rating","Fire Control Size vs Range","Fire Control Size vs Tracking Speed",
                                  "Hardening","Platform Type","Ship Type Limitations");

                        m_oComponentDesignPanel.NotesLabel.Text = "Note that the minimum range for combat is assumed to be 10,000 km. All to hit chances will be calculated using that as a minimum range so fire controls with a lower range than 10,000 km will be ineffective.";

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

                    break;
                    #endregion

                    #region Engines
                    case ComponentsViewModel.Components.Engine:

                        SetTechVisible(6);

                        SetLabels("Engine Technology", "Power / Efficiency Modifers", "Fuel Consumption", "Thermal Reduction", "Engine Size", "Hyper Drive Efficiency", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Fuel Modifier = Power Modifer ^ 2.5(so a 2x Power Modifier will have a 5.66x fuel consumption)";

                    break;
                    #endregion

                    #region Gauss Cannon
                    case ComponentsViewModel.Components.Gauss:

                        SetTechVisible(3);

                        SetLabels("Gauss Cannon Rate of Fire", "Gauss Cannon Velocity", "Gauss Cannon Size vs Accuracy", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "With enough research Gauss cannons become the fastest firing weapon available, which in addition to being turretable makes them ideal for point defense. They may also trade size for accuracy making smaller craft able to mount them.";
                    
                    break;
                    #endregion

                    #region High Power Microwave
                    case ComponentsViewModel.Components.Microwave:

                        SetTechVisible(3);

                        SetLabels("Microwave Focal Size", "Microwave Focusing", "Capacitor Recharge Rate", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Microwaves do damage somewhat differently from other weapons. They only do 1 point of damage(3 vs shields), but pass through armor, and only hit components in the electronic Damage Allocation Table(EDAC). " + 
                                                                  "Likewise hardening of electronic components does not convey additional hit to kill, but reduces on a percentage basis the chance of destruction vs electronic damage. " +
                                                                  "Microwaves have 1/2 the range of a similarly sized and focused laser.";

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

                    break;
                    #endregion

                    #region Plasma Carronades
                    case ComponentsViewModel.Components.Plasma:

                        SetTechVisible(2);

                        SetLabels("Carronade Calibre", "Capacitor Recharge Rate", "", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Plasma Carronades are cheaper than lasers of similar size, do not have any focusing tech associated with them, have a different armour damage pattern, and may not be turreted.";

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

                    break;
                    #endregion

                    #region Railguns
                    case ComponentsViewModel.Components.Rail:

                        SetTechVisible(3);

                        SetLabels("Railgun Type", "Railgun Velocity", "Capacitor Recharge Rate", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Railguns are initially the fastest firing weapon available. They out damage similar lasers but have less range, less penetration, and may not be turreted.";

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

                    break;
                    #endregion

                    #region Thermal Sensors
                    case ComponentsViewModel.Components.Thermal:

                        SetTechVisible(3);

                        SetLabels("Thermal Sensor Sensitivity", "Total Sensor Size", "Hardening", "", "", "", "");

                        m_oComponentDesignPanel.NotesLabel.Text = "Rating is Thermal Sensor Sensitivity x Size. This is the distance in Millions of Km that a signature of 1000 may be detected. smaller or larger signatures are detected by (Signature/1000) * Rating. " +
                                                                  "All ships always emit a thermal signature, even if it is tiny.";


                    break;
                    #endregion
                }
            }
        }

        /// <summary>
        /// This is a helper function that should reduce the clutter in background tech. visibility for each of the seven labels and combo boxes needs to be controlled and set.
        /// This is done here.
        /// </summary>
        /// <param name="count">1 to 7 indicates which tech combo boxes should be visible.</param>
        private void SetTechVisible(int count)
        {
            SendMessage(m_oComponentDesignPanel.Handle, 0x000B, 0, IntPtr.Zero);

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

            SendMessage(m_oComponentDesignPanel.Handle, 0x000B, 1, IntPtr.Zero);

            m_oComponentDesignPanel.Invalidate();
            m_oComponentDesignPanel.Refresh();
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
