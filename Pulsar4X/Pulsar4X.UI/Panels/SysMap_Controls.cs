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
    public partial class SysMap_Controls : DockContent
    {
        #region Properties

        /// <summary>   Gets the system selection combo box. </summary>
        public ComboBox SystemSelectionComboBox
        {
            get
            {
                return m_oSystemSelectionComboBox;
            }
        }

        /// <summary>   Gets the pan up button. </summary>
        public Button PanUpButton
        {
            get
            {
                return m_oPanUpButton;
            }
        }

        /// <summary>   Gets the pan down button. </summary>
        public Button PanDownButton
        {
            get
            {
                return m_oPanDownButton;
            }
        }

        /// <summary>   Gets the pan Left button. </summary>
        public Button PanLeftButton
        {
            get
            {
                return m_oPanLeftButton;
            }
        }

        /// <summary>   Gets the pan Right button. </summary>
        public Button PanRightButton
        {
            get
            {
                return m_oPanRightButton;
            }
        }

        /// <summary>   Gets the Zoom in button. </summary>
        public Button ZoomInButton
        {
            get
            {
                return m_oZoomInButton;
            }
        }

        /// <summary>   Gets the Zoom out button. </summary>
        public Button ZoomOutButton
        {
            get
            {
                return m_oZoomOutButton;
            }
        }

        /// <summary>   Gets the reset view button. </summary>
        public Button ResetViewButton
        {
            get
            {
                return m_oResetViewButton;
            }
        }

        /// <summary>
        /// Scale in KM lable
        /// </summary>
        public Label ScaleKMLable
        {
            get
            {
                return m_oScaleKMLabel;
            }
        }

        /// <summary>
        /// Scale in AU lable
        /// </summary>
        public Label ScaleAULable
        {
            get
            {
                return m_oScaleAULabel;
            }
        }

        #region Waypoint Tab
        /// <summary>
        /// Create a new map marker button.
        /// </summary>
        public Button CreateMapMarkerButton
        {
            get
            {
                return m_oCreateMapMarkerButton;
            }
        }

        /// <summary>
        /// Delete the selected map marker button.
        /// </summary>
        public Button DeleteMapMarkerButton
        {
            get
            {
                return m_oDeleteMapMarkerButton;
            }
        }

        /// <summary>
        /// Lists the map markers.
        /// </summary>
        public ListBox MapMarkersListBox
        {
            get
            {
                return m_oMapMarkersListBox;
            }
        }
        #endregion

        #region Sensors Tab
#warning Only the show actives and show passives checkboxes are implemented for the sensor tab
        /// <summary>
        /// Attempt to increment time by specified increment.
        /// </summary>
        public Button PlayerIncrementButton
        {
            get { return m_oPlayerIncrementButton; }
        }

        /// <summary>
        /// Show thermal detection ranges for currently emitted thermal signatures?
        /// </summary>
        public CheckBox ThermalSignatureDetectionCheckBox
        {
            get { return m_oThermalSignatureDetectionCheckBox; }
        }

        /// <summary>
        /// Show Passive sensors?
        /// </summary>
        public CheckBox ShowPassiveSensorsCheckBox
        {
            get { return m_oShowPassiveSensorsCheckBox; }
        }

        /// <summary>
        /// Show Active Sensors?
        /// </summary>
        public CheckBox ShowActiveSensorsCheckBox
        {
            get { return m_oShowActiveSensorsCheckBox; }
        }

        /// <summary>
        /// What signature should the display show passive detection for?
        /// </summary>
        public TextBox DetectSignatureTextBox
        {
            get { return m_oDetectSignatureTextBox; }
        }

        /// <summary>
        /// What thermal signature detection strength should be used for currently emitted thermal signatures(ships with engines).
        /// </summary>
        public TextBox ThermalSignatureTextBox
        {
            get { return m_oThermalSignatureTextBox; }
        }

        /// <summary>
        /// What time increment should Pulsar attempt to run?
        /// </summary>
        public TextBox UserIncrementTextBox
        {
            get { return m_oUserIncrementTextBox; }
        }
        #endregion

        private ToolTip m_oToolTip;

        #endregion

        public SysMap_Controls()
        {
            InitializeComponent();

            m_oToolTip = new ToolTip();

            this.AutoHidePortion = 0.2f;
            this.HideOnClose = true;
            this.Text = "System Map";
            this.TabText = "System Map";
            this.ToolTipText = "System Map Controls";

            //setup tool tips:
            m_oToolTip.SetToolTip(m_oPanDownButton, "Pans Down (S)");
            m_oToolTip.SetToolTip(m_oPanLeftButton, "Pans Left (A)");
            m_oToolTip.SetToolTip(m_oPanRightButton, "Pans Right (D)");
            m_oToolTip.SetToolTip(m_oPanUpButton, "Pans Up (W)");
            m_oToolTip.SetToolTip(m_oResetViewButton, "Resets the System view (R)");
            m_oToolTip.SetToolTip(m_oScaleAULabel, "The width of the View Port in AU");
            m_oToolTip.SetToolTip(m_oScaleKMLabel, "The width of the View Port in Km");
            m_oToolTip.SetToolTip(m_oSystemSelectionComboBox, "Select the system you wish to view");
            m_oToolTip.SetToolTip(m_oZoomInButton, "Zooms in (E)");
            m_oToolTip.SetToolTip(m_oZoomOutButton, "Zooms out (Q)");
            m_oToolTip.SetToolTip(m_oCreateMapMarkerButton, "Create a new map marker on the map on next left click.");
            m_oToolTip.SetToolTip(m_oDeleteMapMarkerButton, "Deletes the selected map marker");
        }
    }
}
