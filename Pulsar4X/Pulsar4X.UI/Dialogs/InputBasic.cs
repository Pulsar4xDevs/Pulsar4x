using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pulsar4X.UI.Dialogs
{
    /// <summary>
    /// A very basic input dialog.
    /// </summary>
    public partial class InputBasic : Form
    {
        public InputBasic()
        {
            InitializeComponent();
            SetButtonEvents();
        }

        public InputBasic(string a_szTitle, string a_szDescription, string a_szDefaultInput)
        {
            InitializeComponent();
            SetButtonEvents();

            this.Text = a_szTitle;
            m_oDescription.Text = a_szDescription;
            m_oInputTextBox.Text = a_szDefaultInput;
        }

        private void SetButtonEvents()
        {
            m_oCancelButton.Click += new EventHandler(m_oCancelButton_Click);
            m_oOkButton.Click += new EventHandler(m_oOkButton_Click);
        }

        void m_oOkButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Hide();
        }

        void m_oCancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Hide();
        }

        public string InputStr
        {
            get
            {
                return m_oInputTextBox.Text;
            }
            set
            {
                m_oInputTextBox.Text = value;
            }
        }

        public int InputInt
        {
            get
            {
                int iReturn;
                int.TryParse(m_oInputTextBox.Text, out iReturn);
                return iReturn;
            }
        }

        public TextBox InputTextBox
        {
            get
            {
                return m_oInputTextBox;
            }
        }

        public string Description
        {
            get
            {
                return m_oDescription.Text;
            }
            set
            {
                m_oDescription.Text = value;
            }
        }

        public string Title
        {
            get
            {
                return this.Text;
            }
            set
            {
                this.Text = value;
            }
        }
    }
}
