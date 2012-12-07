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
    public partial class Eco_Populations : DockContent
    {
        #region Properties

        /// <summary>
        /// Used for selecting the active faction.
        /// </summary>
        public ComboBox FactionSelectionComboBox
        {
            get
            {
                return m_oFactionSelectionComboBox;
            }
        }

        /// <summary>
        /// Lists the Populated Systems for the selected faction.
        /// </summary>
        public ListBox PopulationsListBox
        {
            get
            {
                return m_oPopulationsListBox;
            }
        }

        #endregion

        public Eco_Populations()
        {
            InitializeComponent();

            this.AutoHidePortion = 0.2f;
            this.HideOnClose = true;
            this.Text = "Populations";
            this.TabText = "Populations";
            this.ToolTipText = "List of Populated Systems";
        }

    }
}
