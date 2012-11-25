using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Pulsar4X.UI.Panels
{
    public partial class SGaD_Controls : DockContent
    {

        #region Properties

        /// <summary>
        /// Combo Box used to select the current star system.
        /// </summary>
        public ComboBox SystemSelectionComboBox
        {
            get
            {
                return m_oSystemSelectionComboBox;
            }
        }

        /// <summary>
        /// Text Box used to print the system age
        /// </summary>
        public TextBox AgeTextBox
        {
            get
            {
                return m_oAgeTextBox;
            }
        }

        /// <summary>
        /// Text box that hols the system type.
        /// </summary>
        public TextBox TypeTextBox
        {
            get
            {
                return m_oSystemTypeTextBox;
            }
        }

        /// <summary>
        /// Text box that holds the date disscovered.
        /// </summary>
        public TextBox DiscoveredTextBox
        {
            get
            {
                return m_oDisscoveredDateTextBox;
            }
        }

        /// <summary>
        /// Button for generating a whole set of systems.
        /// </summary>
        public Button GenGalaxyButton
        {
            get
            {
                return m_oGenGalaxyButton;
            }
        }

        /// <summary>
        /// Button for generating a single systems.
        /// </summary>
        public Button GenSystemButton
        {
            get
            {
                return m_oGenSystemButton;
            }
        }

        /// <summary>
        /// Button for deleting a systems.
        /// </summary>
        public Button DeleteSystemButton
        {
            get
            {
                return m_oDeleteSystemButton;
            }
        }

        /// <summary>
        /// Button for autoimatically renaming a star system based on race templates.
        /// </summary>
        public Button AutoRenameButton
        {
            get
            {
                return m_oAutoRenameButton;
            }
        }

        /// <summary>
        /// Button for adding a colony to the selected system body.
        /// </summary>
        public Button AddColonyButton
        {
            get
            {
                return m_oAddColonyButton;
            }
        }

        /// <summary>
        /// Button for Exporting the selected system or systems.
        /// </summary>
        public Button ExportButton
        {
            get
            {
                return m_oExportButton;
            }
        }

        private ToolTip m_oToolTip;

        #endregion

        public SGaD_Controls()
        {
            InitializeComponent();

            m_oToolTip = new ToolTip();
            m_oToolTip.ShowAlways = true;

            // Define some default values for the textboxes.
            m_oAgeTextBox.Enabled = false;
            m_oAgeTextBox.Text = "0.0";
            m_oSystemTypeTextBox.Enabled = false;
            m_oSystemTypeTextBox.Text = "Single Star";
            m_oDisscoveredDateTextBox.Enabled = false;
            m_oDisscoveredDateTextBox.Text = "1st Jan 2025";

            // Set SM controls disabled by default:
            OnSMDisable();

            // Setup tool tips:
            m_oToolTip.SetToolTip(m_oAgeTextBox, "System Age in Years");
            m_oToolTip.SetToolTip(m_oAddColonyButton, "Adds a colony to the selected system body");
            m_oToolTip.SetToolTip(m_oAutoRenameButton, "Automatically re-names all bodies the system based on race templates");
            m_oToolTip.SetToolTip(m_oDeleteSystemButton, "Deletes the current system");
            m_oToolTip.SetToolTip(m_oDisscoveredDateTextBox, "The date you first explored this system");
            m_oToolTip.SetToolTip(m_oExportButton, "Exports the current galaxy to a Json file");
            m_oToolTip.SetToolTip(m_oGenGalaxyButton, "Allows the generation of multipul systems as a batch job");
            m_oToolTip.SetToolTip(m_oGenSystemButton, "Generates a single system");
            m_oToolTip.SetToolTip(m_oSystemSelectionComboBox, "Select the system you wish to view");
            m_oToolTip.SetToolTip(m_oSystemTypeTextBox, "The type of system, e.g Binary Star System");
        }

        #region PublicMethods

        /// <summary>
        /// When SM mode is enabled call this function to enable SM controls.
        /// </summary>
        public void OnSMEnable()
        {
            m_oGenGalaxyButton.Enabled = true;
            m_oGenSystemButton.Enabled = true;
            m_oDeleteSystemButton.Enabled = true;
        }

        /// <summary>
        /// When SM mode is disabled call this function to disable SM controls.
        /// </summary>
        public void OnSMDisable()
        {
            m_oGenGalaxyButton.Enabled = false;
            m_oGenSystemButton.Enabled = false;
            m_oDeleteSystemButton.Enabled = false;
        }

        #endregion
    }
}
