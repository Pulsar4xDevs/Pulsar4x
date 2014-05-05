using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pulsar4X.UI.Panels
{
    public partial class TurretDesign : Form
    {
        /// <summary>
        /// list of factions.
        /// </summary>
        public ComboBox EmpireComboBox
        {
            get { return m_oEmpireComboBox; }
        }

        /// <summary>
        /// List of turretable beams.
        /// </summary>
        public ComboBox BeamComboBox
        {
            get { return m_oBeamComboBox; }
        }

        /// <summary>
        /// Create a research project
        /// </summary>
        public Button CreateButton
        {
            get { return m_oTurretCreateButton; }
        }

        /// <summary>
        /// Instantly add designed turret to component list.
        /// </summary>
        public Button InstantButton
        {
            get { return m_oInstantButton; }
        }

        /// <summary>
        /// Closes this display
        /// </summary>
        public Button CloseTDButton
        {
            get { return m_oTurretCloseButton; }
        }

        /// <summary>
        /// Single turret selection
        /// </summary>
        public RadioButton SingleRadioButton
        {
            get { return m_oSingleTurretRadioButton; }
        }

        /// <summary>
        /// Double turret selection.
        /// </summary>
        public RadioButton TwinRadioButton
        {
            get { return m_oTwinTurretRadioButton; }
        }

        /// <summary>
        /// Triple turret selection
        /// </summary>
        public RadioButton TripleRadioButton
        {
            get { return m_oTripleTurretRadioButton; }
        }

        /// <summary>
        /// Quad turret selection.
        /// </summary>
        public RadioButton QuadRadioButton
        {
            get { return m_oQuadTurretRadioButton; }
        }

        /// <summary>
        /// Text box for parameters for designed turret.
        /// </summary>
        public RichTextBox TurretParametersTextBox
        {
            get { return m_oTurretParametersTextBox; }
        }

        /// <summary>
        /// Display empire's best turret tracking speed here.
        /// </summary>
        public TextBox TurretTrackTextBox
        {
            get { return m_oTurretTrackTextBox; }
        }

        /// <summary>
        /// display empire's fire control tracking tech here.
        /// </summary>
        public TextBox FireControlTrackTextBox
        {
            get { return m_oFireControlTrackTextBox; }
        }

        /// <summary>
        /// Display size of selected beam here
        /// </summary>
        public TextBox BeamSizeTextBox
        {
            get { return m_oBeamSizeTextBox; }
        }

        /// <summary>
        /// Display total size of all beams( beam size * multiplier)
        /// </summary>
        public TextBox TotalSizeTextBox
        {
            get { return m_oTotalSizeTextBox; }
        }

        /// <summary>
        /// Beam Cost text Box
        /// </summary>
        public TextBox BeamCostTextBox
        {
            get { return m_oBeamCostTextBox; }
        }

        /// <summary>
        /// Total Cost text Box
        /// </summary>
        public TextBox TotalCostTextBox
        {
            get { return m_oTotalCostTextBox; }
        }

        /// <summary>
        /// desired turret tracking speed entered by user.
        /// </summary>
        public TextBox TrackSpeedTextBox
        {
            get { return m_oTrackSpeedTextBox; }
        }

        /// <summary>
        /// Desired turret armour entered by user.
        /// </summary>
        public TextBox TurretArmourTextBox
        {
            get { return m_oTurretArmourTextBox; }
        }

        /// <summary>
        /// size of gears as a percentage of beam size
        /// </summary>
        public TextBox GearPercentTextBox
        {
            get { return m_oGearPercentTextBox; }
        }

        /// <summary>
        /// size of gears in HS
        /// </summary>
        public TextBox GearSizeTextBox
        {
            get { return m_oGearSizeTextBox; }
        }

        /// <summary>
        /// cost of Armour
        /// </summary>
        public TextBox ArmourCostTextBox
        {
            get { return m_oArmourCostTextBox; }
        }

        /// <summary>
        /// Size of Armour
        /// </summary>
        public TextBox ArmourSizeTextBox
        {
            get { return m_oArmourSizeTextBox; }
        }

        /// <summary>
        /// Name of this turret.
        /// </summary>
        public TextBox TurretNameTextBox
        {
            get { return m_oTurretNameTextBox; }
        }

        public TurretDesign()
        {
            InitializeComponent();
        }
    }
}
