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
        public ComboBox FactionComboBox
        {
            get
            {
                return m_oFactionComboBox;
            }
        }

        /// <summary>
        /// For selecting the current Class
        /// </summary>
        public ComboBox ClassComboBox
        {
            get
            {
                return m_oClassComboBox;
            }
        }

        /// <summary>
        /// For selecting the Current Class Type
        /// </summary>
        public ComboBox TypeComboBox
        {
            get
            {
                return m_oTypeComboBox;
            }
        }

        /// <summary>
        /// For selecting the current Hull Type
        /// </summary>
        public ComboBox HullComboBox
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
            get { return m_oTankerCheckBox.Checked; }
            set { m_oTankerCheckBox.Checked = value; }
        }
        public CheckBox TankerCheckBox
        {
            get { return m_oTankerCheckBox; }
        }
        public bool Collier
        {
            get{ return m_oCollierCheckBox.Checked; }
            set{ m_oCollierCheckBox.Checked = value; }
        }
        public CheckBox CollierCheckBox
        {
            get { return m_oCollierCheckBox; }
        }
        public bool Conscript
        {
            get { return m_oConscriptCheckBox.Checked; }
            set { m_oConscriptCheckBox.Checked = value; }
        }

        public bool SupplyShip
        {
            get { return m_oSupplyShipCheckBox.Checked; }
            set { m_oSupplyShipCheckBox.Checked = value; }
        }
        public CheckBox SupplyShipCheckBox
        {
            get { return m_oSupplyShipCheckBox; }
        }

        public CheckBox SizeInTonsCheckBox
        {
            get { return m_oSizeinTonsCheckBox; }
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

        /// <summary>
        /// Display for Ship information.
        /// </summary>
        private DataGridView m_oComponentDataGrid;
        public DataGridView ComponentDataGrid
        {
            get { return m_oComponentDataGrid; }
        }

        /// <summary>
        /// Display for selecting ordnance loadouts on the ordnance/fighters page.
        /// </summary>
        private DataGridView m_oMissileDataGrid;
        public DataGridView MissileDataGrid
        {
            get { return m_oMissileDataGrid; }
        }

        /// <summary>
        /// Preferred strikegroup selection display.
        /// </summary>
        private DataGridView m_oFighterDataGrid;
        public DataGridView FighterDataGrid
        {
            get { return m_oFighterDataGrid; }
        }

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

            m_oComponentDataGrid = new DataGridView();
            m_oComponentDataGrid.Dock = DockStyle.Top;
            m_oComponentDataGrid.SetBounds(3, 3, 745, 350);
            m_oComponentDataGrid.AllowUserToAddRows = false;
            m_oComponentDataGrid.AllowUserToDeleteRows = false;
            m_oComponentDataGrid.AllowUserToOrderColumns = false;
            m_oComponentDataGrid.AllowUserToResizeColumns = false;
            m_oComponentDataGrid.AllowUserToResizeRows = false;
            m_oComponentDataGrid.MultiSelect = false;
            m_oComponentDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            m_oComponentDataGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            m_oComponentDataGrid.ReadOnly = true;
            m_oComponentDataGrid.Enabled = true;
            this.m_oAvailCompGroupBox.Controls.Add(m_oComponentDataGrid);

            m_oMissileDataGrid = new DataGridView();
            m_oMissileDataGrid.Dock = DockStyle.Top;
            m_oMissileDataGrid.SetBounds(3, 3, 745, 350);
            m_oMissileDataGrid.AllowUserToAddRows = false;
            m_oMissileDataGrid.AllowUserToDeleteRows = false;
            m_oMissileDataGrid.AllowUserToOrderColumns = false;
            m_oMissileDataGrid.AllowUserToResizeColumns = false;
            m_oMissileDataGrid.AllowUserToResizeRows = false;
            m_oMissileDataGrid.MultiSelect = false;
            m_oMissileDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            m_oMissileDataGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            m_oMissileDataGrid.RowHeadersVisible = false;
            m_oMissileDataGrid.ReadOnly = true;
            m_oMissileDataGrid.Enabled = true;
            this.m_oMissileGroupBox.Controls.Add(m_oMissileDataGrid);

            m_oFighterDataGrid = new DataGridView();
            m_oFighterDataGrid.Dock = DockStyle.Top;
            m_oFighterDataGrid.SetBounds(3, 3, 745, 350);
            m_oFighterDataGrid.AllowUserToAddRows = false;
            m_oFighterDataGrid.AllowUserToDeleteRows = false;
            m_oFighterDataGrid.AllowUserToOrderColumns = false;
            m_oFighterDataGrid.AllowUserToResizeColumns = false;
            m_oFighterDataGrid.AllowUserToResizeRows = false;
            m_oFighterDataGrid.MultiSelect = false;
            m_oFighterDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            m_oFighterDataGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            m_oFighterDataGrid.RowHeadersVisible = false;
            m_oFighterDataGrid.ReadOnly = true;
            m_oFighterDataGrid.Enabled = true;
            this.m_oStrikeGroupBox.Controls.Add(m_oFighterDataGrid);
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
