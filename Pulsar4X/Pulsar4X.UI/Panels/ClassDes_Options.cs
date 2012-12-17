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
    public partial class ClassDes_Options : DockContent
    {
        #region Properties

        /// <summary>
        /// For selecting the current Faction
        /// </summary>
        public ComboBox Faction
        {
            get
            {
                return m_oFactionComboBox;
            }
        }

        /// <summary>
        /// For selecting the current Class
        /// </summary>
        public ComboBox Class
        {
            get
            {
                return m_oClassComboBox;
            }
        }

        /// <summary>
        /// For selecting the Current Class Type
        /// </summary>
        public ComboBox Type
        {
            get
            {
                return m_oTypeComboBox;
            }
        }

        /// <summary>
        /// For selecting the current Hull Type
        /// </summary>
        public ComboBox Hull
        {
            get
            {
                return m_oHullComboBox;
            }
        }

        public bool ShowCivilianDesigns
        {
            get
            {
                return m_oShowCivilianDesignsCheckBox.Checked;
            }
            set
            {
                m_oShowCivilianDesignsCheckBox.Checked = value;
            }
        }

        public CheckBox ShowCivilianDesignsCheckBox
        {
            get
            {
                return m_oShowCivilianDesignsCheckBox;
            }
        }

        public bool NoThemeName
        {
            get
            {
                return m_oNoThemeCheckBox.Checked;
            }
            set
            {
                m_oNoThemeCheckBox.Checked = value;
            }
        }

        public CheckBox NoThemeNameCheckBox
        {
            get
            {
                return m_oNoThemeCheckBox;
            }
        }

        public bool HideObsolete
        {
            get
            {
                return m_oHideObsoleteCheckBox.Checked;
            }
            set
            {
                m_oHideObsoleteCheckBox.Checked = value;
            }
        }

        public CheckBox HideObsoleteCheckBox
        {
            get
            {
                return m_oHideObsoleteCheckBox;
            }
        }

        public bool Tanker
        {
            get
            {
                return m_oTankerCheckBox.Checked;
            }
            set
            {
                m_oTankerCheckBox.Checked = value;
            }
        }

        public bool Collier
        {
            get
            {
                return m_oCollierCheckBox.Checked;
            }
            set
            {
                m_oCollierCheckBox.Checked = value;
            }
        }

        public bool Conscript
        {
            get
            {
                return m_oConscriptCheckBox.Checked;
            }
            set
            {
                m_oConscriptCheckBox.Checked = value;
            }
        }

        public bool SupplyShip
        {
            get
            {
                return m_oSupplyShipCheckBox.Checked;
            }
            set
            {
                m_oSupplyShipCheckBox.Checked = value;
            }
        }

        public CheckBox SizeInTonsCheckBox
        {
            get
            {
                return m_oSizeinTonsCheckBox;
            }
        }

        public bool Obsolete
        {
            get
            {
                return m_oObsoleteCheckBox.Checked;
            }
            set
            {
                m_oObsoleteCheckBox.Checked = value;
            }
        }

        public CheckBox KeepExcessQuarters
        {
            get
            {
                return m_oKeepExcessQCheckBox;
            }
        }

        public Button RenameButton
        {
            get
            {
                return m_oRenameButton;
            }
        }

        public Button AutoRenameButton
        {
            get
            {
                return m_oAutoRenameButton;
            }
        }

        public Button ReNumberButton
        {
            get
            {
                return m_oReNumberButton;
            }
        }

        public Button RandomNameButton
        {
            get
            {
                return m_oRandomNameButton;
            }
        }

        public Button NewButton
        {
            get
            {
                return m_oNewButton;
            }
        }

        public Button DeleteButton
        {
            get
            {
                return m_oDeleteButton;
            }
        }

        public Button NPRClassButton
        {
            get
            {
                return NPRClassButton;
            }
        }

        public Button DesignTechButton
        {
            get
            {
                return m_oDesignTechButton;
            }
        }

        public Button NewArmorButton
        {
            get
            {
                return m_oNewArmorButton;
            }
        }

        public Button NewHullButton
        {
            get
            {
                return m_oNewHullButton;
            }
        }

        public Button LockDesignButton
        {
            get
            {
                return m_oLockDesignButton;
            }
        }

        public Button CopyDesignButton
        {
            get
            {
                return m_oCopyDesignButton;
            }
        }

        public Button RefreshTechButton
        {
            get
            {
                return m_oRefreshTechButton;
            }
        }

        public Button ObsoleteCompButton
        {
            get
            {
                return m_oObsoleteCompButton;
            }
        }

        public Button FleetAssignButton
        {
            get
            {
                return m_oFleetAssignBbutton;
            }
        }

        public Button ViewTechButton
        {
            get
            {
                return m_oViewTechButton;
            }
        }

        public Button TextFileButton
        {
            get
            {
                return m_oTextFileButton;
            }
        }

        public Button SMModeButton
        {
            get
            {
                return m_oSMModeButton;
            }
        }

        #endregion


        public ClassDes_Options()
        {
            InitializeComponent();

            this.AutoHidePortion = 0.2f;
            this.HideOnClose = true;
            this.Text = "Class Design Options";
            this.TabText = "Class Design Options";
            this.ToolTipText = "Class Design Options";

            // events to make sure that only one sort button is active at a time.
            m_oSortAlphaRadioButton.CheckedChanged += new EventHandler(m_oSortAlphaRadioButton_CheckedChanged);
            m_oSortCostRadioButton.CheckedChanged += new EventHandler(m_oSortCostRadioButton_CheckedChanged);
            m_oSortHullRadioButton.CheckedChanged += new EventHandler(m_oSortHullRadioButton_CheckedChanged);
            m_oSortSizeRadioButton.CheckedChanged += new EventHandler(m_oSortSizeRadioButton_CheckedChanged);
        }

        #region Events

        void m_oSortSizeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oSortSizeRadioButton.Checked == true)
            {
                m_oSortCostRadioButton.Checked = false;
                m_oSortHullRadioButton.Checked = false;
                m_oSortAlphaRadioButton.Checked = false;
            }
        }

        void m_oSortHullRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oSortHullRadioButton.Checked == true)
            {
                m_oSortCostRadioButton.Checked = false;
                m_oSortSizeRadioButton.Checked = false;
                m_oSortAlphaRadioButton.Checked = false;
            }
        }

        void m_oSortCostRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oSortCostRadioButton.Checked == true)
            {
                m_oSortHullRadioButton.Checked = false;
                m_oSortSizeRadioButton.Checked = false;
                m_oSortAlphaRadioButton.Checked = false;
            }
        }

        void m_oSortAlphaRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oSortAlphaRadioButton.Checked == true)
            {
                m_oSortHullRadioButton.Checked = false;
                m_oSortSizeRadioButton.Checked = false;
                m_oSortCostRadioButton.Checked = false;
            }
        }

        #endregion
    }
}
