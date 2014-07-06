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
    public partial class Ships_ShipList : DockContent
    {
        #region Properties

        /// <summary>
        /// List of ships owned by the selected faction.
        /// </summary>
        public ListBox ShipsListBox
        {
            get
            {
                return m_oShipsListBox;
            }
        }

        /// <summary>
        /// Used to select the current faction.
        /// </summary>
        public ComboBox FactionSelectionComboBox
        {
            get
            {
                return m_oFactionComboBox;
            }
        }

        /// <summary>
        /// If true then we only show militart Ships.
        /// </summary>
        public bool FilterMilitaryOnly
        {
            get
            {
                return m_oFilterMilitaryCheckBox.Checked;
            }
            set
            {
                m_oFilterMilitaryCheckBox.Checked = value;
                m_oFilterCivilianCheckBox.Checked = !value;
            }
        }

        /// <summary>
        /// If true we only show civilian ships.
        /// </summary>
        public bool FilterCivilianOnly
        {
            get
            {
                return m_oFilterCivilianCheckBox.Checked;
            }
            set
            {
                m_oFilterMilitaryCheckBox.Checked = !value;
                m_oFilterCivilianCheckBox.Checked = value;
            }
        }

        /// <summary>
        /// if true we filter out all fighters.
        /// </summary>
        public bool FilterNoFighters
        {
            get
            {
                return m_oFilterNoFightersCheckBox.Checked;
            }
            set
            {
                m_oFilterNoFightersCheckBox.Checked = value;
            }
        }

        /// <summary>
        /// If true then we are sorting by size.
        /// </summary>
        public bool SortBySize
        {
            get
            {
                return m_oSortSizeRadioButton.Checked;
            }
        }

        /// <summary>
        /// if true then we are shrting by hull.
        /// </summary>
        public bool SortByHull
        {
            get
            {
                return m_oSortHullRadioButton.Checked;
            }
        }

        /// <summary>
        /// If true then we are sorting by alpha.
        /// </summary>
        public bool SortByAlpha
        {
            get
            {
                return m_oSortAlphaRadioButton.Checked;
            }
        }

        #endregion


        public Ships_ShipList()
        {
            InitializeComponent();

            this.HideOnClose = true;
            this.Text = "Ship Design";
            this.TabText = "Ship Design";
            this.ToolTipText = "Ship Design Details";

            // set default sort to Size:
            m_oSortSizeRadioButton.Checked = true;

            // the folowing events make sure that if Military only is checked, Civilian Only will be unchecked, etc.
            m_oFilterMilitaryCheckBox.CheckedChanged += new EventHandler(m_oFilterMilitaryCheckBox_CheckedChanged);
            m_oFilterCivilianCheckBox.CheckedChanged += new EventHandler(m_oFilterCivilianCheckBox_CheckedChanged);

            // the following events make sure that only one sort type can be selected at any one time:
            m_oSortSizeRadioButton.CheckedChanged += new EventHandler(m_oSortSizeRadioButton_CheckedChanged);
            m_oSortHullRadioButton.CheckedChanged += new EventHandler(m_oSortHullRadioButton_CheckedChanged);
            m_oSortAlphaRadioButton.CheckedChanged += new EventHandler(m_oSortAlphaRadioButton_CheckedChanged);
        }


        #region Events

        void m_oSortAlphaRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oSortAlphaRadioButton.Checked == true)
            {
                m_oSortHullRadioButton.Checked = false;
                m_oSortSizeRadioButton.Checked = false;
            }
        }

        void m_oSortHullRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oSortHullRadioButton.Checked == true)
            {
                m_oSortAlphaRadioButton.Checked = false;
                m_oSortSizeRadioButton.Checked = false;
            }
        }

        void m_oSortSizeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oSortSizeRadioButton.Checked == true)
            {
                m_oSortAlphaRadioButton.Checked = false;
                m_oSortHullRadioButton.Checked = false;
            }
        }

        void m_oFilterCivilianCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oFilterCivilianCheckBox.Checked == true)
            {
                m_oFilterMilitaryCheckBox.Checked = false;
            }
        }

        void m_oFilterMilitaryCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oFilterMilitaryCheckBox.Checked == true)
            {
                m_oFilterCivilianCheckBox.Checked = false;
            }
        }

        #endregion
    }
}
