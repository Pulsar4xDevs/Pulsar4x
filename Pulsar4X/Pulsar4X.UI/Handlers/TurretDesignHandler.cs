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

            Multiplier = 1;
        }

        #region Public methods
        /// <summary>
        /// Opens as a popup the missile design page
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
        /// overall display function
        /// </summary>
        private void BuildTurretDesignPage()
        {
            BuildFactionInfo();
            BuildBeamInfo();
            BuildTurretInfo();
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

        private void BuildTurretInfo()
        {
            int TurretTrack;
            bool result = Int32.TryParse(m_oTurretDesignPanel.TrackSpeedTextBox.Text, out TurretTrack);

            if (result)
            {
                int TrackTech = _CurrnetFaction.FactionTechLevel[(int)Faction.FactionTechnology.TurretTracking];

                if (TrackTech > 11)
                    TrackTech = 11;

                float TrackFactor = (float)TurretTrack / (float)Constants.BFCTN.BeamFireControlTracking[TrackTech];
                float GearSize = _CurrnetBeam.size;
                float GearPer = 0.1f;
                switch (Multiplier)
                {
                    case 1:
                        GearSize = GearSize * GearPer;
                        break;
                    case 2:
                        GearPer = 0.095f;
                        GearSize = GearSize * GearPer * 2;
                        break;
                    case 3:
                        GearPer = 0.0925f;
                        GearSize = GearSize * GearPer * 3;
                        break;
                    case 4:
                        GearPer = 0.09f;
                        GearSize = GearSize * GearPer * 4;
                        break;
                }
                String Entry = String.Format("{0}", (GearSize * TrackFactor));
                m_oTurretDesignPanel.GearSizeTextBox.Text = Entry;

                float TotalSize = (float)Multiplier * _CurrnetBeam.size;

                m_oTurretDesignPanel.GearPercentTextBox.Text = GearPer.ToString();
            }

            int ArmourThick;
            result = Int32.TryParse(m_oTurretDesignPanel.TurretArmourTextBox.Text, out ArmourThick);

            if (result && ArmourThick != 0)
            {

            }
        }

        #endregion
    }
}
