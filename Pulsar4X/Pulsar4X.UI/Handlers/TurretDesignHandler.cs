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

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif 

using Pulsar4X.Entities.Components;

namespace Pulsar4X.UI.Handlers
{
    public class TurretDesignHandler
    {

#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(TurretDesignHandler));
#endif

        /// <summary>
        /// Panel for this turret designer
        /// </summary>
        Panels.TurretDesign m_oTurretDesignPanel { get; set; }

        /// <summary>
        /// view model for the turret designer
        /// </summary>
        public TurretDesignViewModel VM { get; set; }

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

                /// <summary>
                /// I don't need all these returns, but am putting them in anyways just in case I do something here later. 
                /// And as an aside, the reason for all these summarys everywhere is that they look nice.
                /// </summary>
                if (_CurrnetFaction == null)
                {
                    return;
                }

                if (_CurrnetFaction.ComponentList.TurretableBeamDef.Count != 0)
                    _CurrnetBeam = _CurrnetFaction.ComponentList.TurretableBeamDef[0];
            }
        }

        /// <summary>
        /// Currently selected Beam
        /// </summary>
        private BeamDefTN _CurrnetBeam;
        public BeamDefTN CurrentBeam
        {
            get { return _CurrnetBeam; }
            set
            {
                _CurrnetBeam = value;
                if (_CurrnetBeam == null)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Turret barrel multiplier.
        /// </summary>
        private int Multiplier { get; set; }

        /// <summary>
        /// Desired Turret Tracking
        /// </summary>
        private int TurretProjTracking { get; set; }

        /// <summary>
        /// desired armour thickness
        /// </summary>
        private int TurretProjArmour { get; set; }

        /// <summary>
        /// The turret project that this design handler will use.
        /// </summary>
        private TurretDefTN TurretProject { get; set; }


        /// <summary>
        /// Constructor
        /// </summary>
        public TurretDesignHandler()
        {
            m_oTurretDesignPanel = new Panels.TurretDesign();

            VM = new ViewModels.TurretDesignViewModel();

            /// <summary>
            /// Bind factions to the empire selection combo box.
            /// </summary>
            m_oTurretDesignPanel.EmpireComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oTurretDesignPanel.EmpireComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oTurretDesignPanel.EmpireComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => _CurrnetFaction = VM.CurrentFaction;
            _CurrnetFaction = VM.CurrentFaction;
            m_oTurretDesignPanel.EmpireComboBox.SelectedIndexChanged += (s, args) => m_oTurretDesignPanel.EmpireComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oTurretDesignPanel.EmpireComboBox.SelectedIndexChanged += new EventHandler(EmpireComboBox_SelectedIndexChanged);

            /// <summary>
            /// Binding missile engines to the appropriate combo box.
            /// </summary>
            m_oTurretDesignPanel.BeamComboBox.Bind(c => c.DataSource, VM, d => d.TurretableList);
            m_oTurretDesignPanel.BeamComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentBeam, DataSourceUpdateMode.OnPropertyChanged);
            m_oTurretDesignPanel.BeamComboBox.DisplayMember = "Name";
            VM.BeamChanged += (s, args) => _CurrnetBeam = VM.CurrentBeam;
            _CurrnetBeam = VM.CurrentBeam;
            m_oTurretDesignPanel.BeamComboBox.SelectedIndexChanged += (s, args) => m_oTurretDesignPanel.BeamComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oTurretDesignPanel.BeamComboBox.SelectedIndexChanged += new EventHandler(BeamComboBox_SelectedIndexChanged);


            m_oTurretDesignPanel.SingleRadioButton.CheckedChanged += new EventHandler(MultRadioButton_CheckedChanged);
            m_oTurretDesignPanel.TwinRadioButton.CheckedChanged += new EventHandler(MultRadioButton_CheckedChanged);
            m_oTurretDesignPanel.TripleRadioButton.CheckedChanged += new EventHandler(MultRadioButton_CheckedChanged);
            m_oTurretDesignPanel.QuadRadioButton.CheckedChanged += new EventHandler(MultRadioButton_CheckedChanged);

            m_oTurretDesignPanel.TrackSpeedTextBox.TextChanged += new EventHandler(TrackSpeedTextBox_TextChanged);
            m_oTurretDesignPanel.TurretArmourTextBox.TextChanged += new EventHandler(TurretArmourTextBox_TextChanged);

            Multiplier = 1;
            TurretProjTracking = 10000;
            TurretProjArmour = 0;

            TurretProject = null;
        }

        #region Public methods
        /// <summary>
        /// Opens as a popup the turret design page
        /// </summary>
        public void Popup()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            //BuildTurretDesignPage();
            m_oTurretDesignPanel.ShowDialog();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        /// <summary>
        /// Space master on, bring in the instant button.
        /// </summary>
        public void SMOn()
        {
            m_oTurretDesignPanel.InstantButton.Visible = true;
            m_oTurretDesignPanel.InstantButton.Enabled = true;
        }

        /// <summary>
        /// Space master off, get rid of the instant button.
        /// </summary>
        public void SMOff()
        {
            m_oTurretDesignPanel.InstantButton.Visible = false;
            m_oTurretDesignPanel.InstantButton.Enabled = false;
        }
        #endregion

        #region private methods
        /// <summary>
        /// When a new empire/faction is selected this will be run
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EmpireComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {


            BuildTurretDesignPage();
        }

        /// <summary>
        /// if a new turretable beam is selected this will be run.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BeamComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            BuildTurretDesignPage();
        }

        /// <summary>
        /// If a new turret barrel multiplier is selected this is run.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MultRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oTurretDesignPanel.SingleRadioButton.Checked == true)
                Multiplier = 1;
            else if (m_oTurretDesignPanel.TwinRadioButton.Checked == true)
                Multiplier = 2;
            else if (m_oTurretDesignPanel.TripleRadioButton.Checked == true)
                Multiplier = 3;
            else if (m_oTurretDesignPanel.QuadRadioButton.Checked == true)
                Multiplier = 4;

            BuildTurretDesignPage();
        }

        /// <summary>
        /// if a new tracking speed is entered the turret design page should be reprinted to reflect this.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackSpeedTextBox_TextChanged(object sender, EventArgs e)
        {
            int TurretTrack;
            bool result = Int32.TryParse(m_oTurretDesignPanel.TrackSpeedTextBox.Text, out TurretTrack);

            if (result)
            {
                TurretProjTracking = TurretTrack;
            }
            BuildTurretDesignPage();
        }

        /// <summary>
        /// if a new tracking speed is entered the turret design page should be reprinted to reflect this.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurretArmourTextBox_TextChanged(object sender, EventArgs e)
        {
            int DesiredArmour;
            bool result = Int32.TryParse(m_oTurretDesignPanel.TurretArmourTextBox.Text, out DesiredArmour);

            if (result)
            {
                TurretProjArmour = DesiredArmour;
            }
            BuildTurretDesignPage();
        }

        /// <summary>
        /// overall display function
        /// </summary>
        private void BuildTurretDesignPage()
        {
            #warning two occurences of magic number tech checking here.
            int TrackTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.TurretTracking];

            if(TrackTech > 11)
                TrackTech = 11;

            int ArmourTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.ArmourProtection];

            if(ArmourTech > 12)
                ArmourTech = 12;

            if(_CurrnetBeam != null)
                TurretProject = new TurretDefTN("---Working Title---", _CurrnetBeam, Multiplier, TurretProjTracking, TrackTech, TurretProjArmour, ArmourTech); 

            BuildFactionInfo();
            BuildBeamInfo();
            BuildTurretInfo();
            BuildSystemParamters();
        }

        /// <summary>
        /// display only the faction tech information.
        /// </summary>
        private void BuildFactionInfo()
        {
            #warning two occurences of magic number tech checking here.
            int TrackTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.TurretTracking];

            if(TrackTech > 11)
                TrackTech = 11;

            String Entry = String.Format("Turret Tracking Speed(10% Gear) {0} km/s",Constants.BFCTN.BeamFireControlTracking[TrackTech]);
            m_oTurretDesignPanel.TurretTrackTextBox.Text = Entry;

            int FCTrackTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.BeamFireControlTracking];
            if(FCTrackTech > 11)
                FCTrackTech = 11;

            Entry = String.Format("Fire Control Speed Rating {0} km/s",Constants.BFCTN.BeamFireControlTracking[FCTrackTech]);
            m_oTurretDesignPanel.FireControlTrackTextBox.Text = Entry;
        }

        /// <summary>
        /// Display only beam information
        /// </summary>
        private void BuildBeamInfo()
        {
            if (_CurrnetBeam != null)
            {
                m_oTurretDesignPanel.BeamCostTextBox.Text = String.Format("{0}",_CurrnetBeam.cost);
                m_oTurretDesignPanel.BeamSizeTextBox.Text = String.Format("{0}", _CurrnetBeam.size);
                m_oTurretDesignPanel.TotalCostTextBox.Text = String.Format("{0}", (_CurrnetBeam.cost * Multiplier));
                m_oTurretDesignPanel.TotalSizeTextBox.Text = String.Format("{0}", (_CurrnetBeam.size * Multiplier));
            }
        }

        /// <summary>
        /// build the armour/turret information related to size, cost, gear percentage.
        /// </summary>
        private void BuildTurretInfo()
        {
            float GearSize = (float)Multiplier * _CurrnetBeam.size * TurretProject.gearPercent;

            m_oTurretDesignPanel.GearPercentTextBox.Text = (TurretProject.gearPercent * 100.0f).ToString();
            m_oTurretDesignPanel.GearSizeTextBox.Text = GearSize.ToString();

            m_oTurretDesignPanel.ArmourCostTextBox.Text = TurretProject.armourCost.ToString();
            m_oTurretDesignPanel.ArmourSizeTextBox.Text = TurretProject.armourSize.ToString();

        }

        /// <summary>
        /// build the System parameter text box and name.
        /// </summary>
        private void BuildSystemParamters()
        {
            switch (TurretProject.multiplier)
            {
                case 1: m_oTurretDesignPanel.TurretNameTextBox.Text = String.Format("Single {0} Turret", TurretProject.baseBeamWeapon.Name);
                    break;
                case 2: m_oTurretDesignPanel.TurretNameTextBox.Text = String.Format("Twin {0} Turret", TurretProject.baseBeamWeapon.Name);
                    break;
                case 3: m_oTurretDesignPanel.TurretNameTextBox.Text = String.Format("Triple {0} Turret", TurretProject.baseBeamWeapon.Name);
                    break;
                case 4: m_oTurretDesignPanel.TurretNameTextBox.Text = String.Format("Quad {0} Turret", TurretProject.baseBeamWeapon.Name);
                    break;
            }

            m_oTurretDesignPanel.TurretParametersTextBox.Clear();

            float ROF = (float)Math.Ceiling(TurretProject.powerRequirement / (TurretProject.baseBeamWeapon.weaponCapacitor * TurretProject.multiplier)) * 5.0f;
            if (ROF < 5)
                ROF = 5;
            String Entry = String.Format("Damage Output {0}x{1}      Rate of Fire: {2} seconds     Range Modifier: {3}\n", TurretProject.baseBeamWeapon.damage[0],TurretProject.multiplier,
                                         ROF.ToString(), (TurretProject.baseBeamWeapon.damage.Count - 1));
            m_oTurretDesignPanel.TurretParametersTextBox.AppendText(Entry);

            String FormattedRange = TurretProject.baseBeamWeapon.range.ToString("#,###0");
            String Range = String.Format("Range {0} km", FormattedRange);
            float SpacePerWeapon = TurretProject.size / TurretProject.multiplier;

            Entry = String.Format("Max {0}    Turret Size: {1:N2}    SPW: {2:N2}    Turret HTK: {3}\n", Range ,TurretProject.size, SpacePerWeapon, TurretProject.htk );
            m_oTurretDesignPanel.TurretParametersTextBox.AppendText(Entry);

            Entry = String.Format("Power Requirement: {0}    Power Recharge per 5 Secs: {1}\n", TurretProject.powerRequirement, (TurretProject.baseBeamWeapon.weaponCapacitor * TurretProject.multiplier) );
            m_oTurretDesignPanel.TurretParametersTextBox.AppendText(Entry);

            Entry = String.Format("Cost: {0}    Crew: {1}\n", TurretProject.cost, TurretProject.crew );
            m_oTurretDesignPanel.TurretParametersTextBox.AppendText(Entry);

            Entry = String.Format("Maximum Tracking Speed: {0} km/s\n", TurretProject.tracking);
            m_oTurretDesignPanel.TurretParametersTextBox.AppendText(Entry);

            Entry = String.Format("Materials Required: Not Yet Implemented\n");
            m_oTurretDesignPanel.TurretParametersTextBox.AppendText(Entry);

            Entry = String.Format("\nDevelopment Cost for Project: {0} RP\n", (TurretProject.cost * 4));
            m_oTurretDesignPanel.TurretParametersTextBox.AppendText(Entry);
        }

        #endregion
    }
}
