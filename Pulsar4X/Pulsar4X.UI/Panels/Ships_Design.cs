using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

//I want Armor Status, Damage Control, Combat settings

namespace Pulsar4X.UI.Panels
{
    public partial class Ships_Design : DockContent
    {
        #region Properties

        /// <summary>
        /// For design details text.
        /// </summary>
        public RichTextBox DesignRichTextBox
        {
            get
            {
                return m_oDesignRichTextBox;
            }
        }

        int m_iRange;

        public int Range
        {
            get
            {
                return m_iRange;
            }
        }

        float m_fSpeed;

        public float Speed
        {
            get
            {
                return m_fSpeed;
            }
        }

        #endregion

        public Ships_Design()
        {
            InitializeComponent();

            this.AutoHidePortion = 0.2f;
            this.HideOnClose = true;
            this.Text = "Individual Unit Details";
            this.TabText = "Individual Unit Details";
            this.ToolTipText = "Individual ship information display.";

            m_oDesignRichTextBox.Multiline = true;
            m_oDesignRichTextBox.ScrollBars = RichTextBoxScrollBars.Both;
            m_oDesignRichTextBox.Enabled = true;
            m_oDesignRichTextBox.ReadOnly = true;

            // set default range and Speed:
            m_iRange = 10000;
            m_oRange10000RadioButton.Checked = true;
            m_fSpeed = 1000.0f;
            m_oSpeed1000RadioButton.Checked = true;

            // events to make sure only one speed and/or range is seleted:
            m_oRangeCustomRadioButton.CheckedChanged += new EventHandler(m_oRangeCustomRadioButton_CheckedChanged);
            m_oRange10000RadioButton.CheckedChanged += new EventHandler(m_oRange10000RadioButton_CheckedChanged);
            m_oRange1000000RadioButton.CheckedChanged += new EventHandler(m_oRange1000000RadioButton_CheckedChanged);
            m_oRange200000RadioButton.CheckedChanged += new EventHandler(m_oRange200000RadioButton_CheckedChanged);
            m_oRange20000RadioButton.CheckedChanged += new EventHandler(m_oRange20000RadioButton_CheckedChanged);
            m_oRange30000RadioButton.CheckedChanged += new EventHandler(m_oRange30000RadioButton_CheckedChanged);
            m_oRange500000RadioButton.CheckedChanged += new EventHandler(m_oRange500000RadioButton_CheckedChanged);
            m_oRange50000RadioButton.CheckedChanged += new EventHandler(m_oRange50000RadioButton_CheckedChanged);
            m_oRangeCustomTextBox.TextChanged += new EventHandler(m_oRangeCustomTextBox_TextChanged);
        }

        void m_oRangeCustomTextBox_TextChanged(object sender, EventArgs e)
        {
            int temp = 1000;
            if (int.TryParse(m_oRangeCustomTextBox.Text, out temp))
            {
                m_iRange = temp;
                // we have a valid input, change the checked box too:
                m_oRange50000RadioButton.Checked = false;
                m_oRange500000RadioButton.Checked = false;
                m_oRange30000RadioButton.Checked = false;
                m_oRange20000RadioButton.Checked = false;
                m_oRange200000RadioButton.Checked = false;
                m_oRange1000000RadioButton.Checked = false;
                m_oRange10000RadioButton.Checked = false;
                m_oRangeCustomRadioButton.Checked = true;
            }
            else
            {
                // give valid number:
                m_iRange = 10000;
                m_oRange10000RadioButton.Checked = true;
                m_oRange50000RadioButton.Checked = false;
                m_oRange500000RadioButton.Checked = false;
                m_oRange30000RadioButton.Checked = false;
                m_oRange20000RadioButton.Checked = false;
                m_oRange200000RadioButton.Checked = false;
                m_oRange1000000RadioButton.Checked = false;
                m_oRangeCustomRadioButton.Checked = false;
            }
        }

        void m_oRange50000RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oRange50000RadioButton.Checked == true)
            {
                m_oRange500000RadioButton.Checked = false;
                m_oRange30000RadioButton.Checked = false;
                m_oRange20000RadioButton.Checked = false;
                m_oRange200000RadioButton.Checked = false;
                m_oRange1000000RadioButton.Checked = false;
                m_oRange10000RadioButton.Checked = false;
                m_oRangeCustomRadioButton.Checked = false;
                m_iRange = 50000;
            }
        }

        void m_oRange500000RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oRange500000RadioButton.Checked == true)
            {
                m_oRange50000RadioButton.Checked = false;
                m_oRange30000RadioButton.Checked = false;
                m_oRange20000RadioButton.Checked = false;
                m_oRange200000RadioButton.Checked = false;
                m_oRange1000000RadioButton.Checked = false;
                m_oRange10000RadioButton.Checked = false;
                m_oRangeCustomRadioButton.Checked = false;
                m_iRange = 500000;
            }
        }

        void m_oRange30000RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oRange30000RadioButton.Checked == true)
            {
                m_oRange50000RadioButton.Checked = false;
                m_oRange500000RadioButton.Checked = false;
                m_oRange20000RadioButton.Checked = false;
                m_oRange200000RadioButton.Checked = false;
                m_oRange1000000RadioButton.Checked = false;
                m_oRange10000RadioButton.Checked = false;
                m_oRangeCustomRadioButton.Checked = false;
                m_iRange = 3000;
            }
        }

        void m_oRange20000RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oRange20000RadioButton.Checked == true)
            {
                m_oRange50000RadioButton.Checked = false;
                m_oRange500000RadioButton.Checked = false;
                m_oRange30000RadioButton.Checked = false;
                m_oRange200000RadioButton.Checked = false;
                m_oRange1000000RadioButton.Checked = false;
                m_oRange10000RadioButton.Checked = false;
                m_oRangeCustomRadioButton.Checked = false;
                m_iRange = 20000;
            }
        }

        void m_oRange200000RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oRange200000RadioButton.Checked == true)
            {
                m_oRange50000RadioButton.Checked = false;
                m_oRange500000RadioButton.Checked = false;
                m_oRange30000RadioButton.Checked = false;
                m_oRange20000RadioButton.Checked = false;
                m_oRange1000000RadioButton.Checked = false;
                m_oRange10000RadioButton.Checked = false;
                m_oRangeCustomRadioButton.Checked = false;
                m_iRange = 200000;
            }
        }

        void m_oRange1000000RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oRange1000000RadioButton.Checked == true)
            {
                m_oRange50000RadioButton.Checked = false;
                m_oRange500000RadioButton.Checked = false;
                m_oRange30000RadioButton.Checked = false;
                m_oRange20000RadioButton.Checked = false;
                m_oRange200000RadioButton.Checked = false;
                m_oRange10000RadioButton.Checked = false;
                m_oRangeCustomRadioButton.Checked = false;
                m_iRange = 1000000;
            }
        }

        void m_oRange10000RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oRange10000RadioButton.Checked == true)
            {
                m_oRange50000RadioButton.Checked = false;
                m_oRange500000RadioButton.Checked = false;
                m_oRange30000RadioButton.Checked = false;
                m_oRange20000RadioButton.Checked = false;
                m_oRange200000RadioButton.Checked = false;
                m_oRange1000000RadioButton.Checked = false;
                m_oRangeCustomRadioButton.Checked = false;
                m_iRange = 10000;
            }
        }

        void m_oRangeCustomRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oRangeCustomRadioButton.Checked == true)
            {
                m_oRange50000RadioButton.Checked = false;
                m_oRange500000RadioButton.Checked = false;
                m_oRange30000RadioButton.Checked = false;
                m_oRange20000RadioButton.Checked = false;
                m_oRange200000RadioButton.Checked = false;
                m_oRange1000000RadioButton.Checked = false;
                m_oRange10000RadioButton.Checked = false;
                int temp = 1000;
                if (int.TryParse(m_oRangeCustomTextBox.Text, out temp))
                {
                    m_iRange = temp;
                }
                else
                {
                    m_iRange = 10000;
                }
            }
        }
    }
}
