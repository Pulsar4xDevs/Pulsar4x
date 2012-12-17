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
    public partial class ClassDes_DesignAndInfo : DockContent
    {
        #region Properties

        private SplitContainer m_oOuterSplitContainer;
        private SplitContainer m_oInerSplitContainer;

        private DataGridView m_oComponentsDataGridView;

        public DataGridView ComponentsDataGridView
        {
            get
            {
                return m_oComponentsDataGridView;
            }
        }

        private RichTextBox m_oSummaryRichTextBox;

        public RichTextBox SummaryRichTextBox
        {
            get
            {
                return m_oSummaryRichTextBox;
            }
        }

        private RichTextBox m_oDesignErrorsRichTextBox;

        private RichTextBox DesignErrorsRichTextBox
        {
            get
            {
                return m_oDesignErrorsRichTextBox;
            }
        }

        #endregion

        public ClassDes_DesignAndInfo()
        {
            InitializeComponent();

            this.HideOnClose = true;
            this.Text = "Design and Info";
            this.TabText = "Design and Info";
            this.ToolTipText = "Class Design and Information";

            // create controls:
            m_oOuterSplitContainer = new SplitContainer();
            m_oInerSplitContainer = new SplitContainer();
            m_oComponentsDataGridView = new DataGridView();
            m_oSummaryRichTextBox = new RichTextBox();
            m_oDesignErrorsRichTextBox = new RichTextBox();

            // layout controls:
            m_oOuterSplitContainer.Orientation = Orientation.Horizontal;
            m_oOuterSplitContainer.Dock = DockStyle.Fill;
            m_oOuterSplitContainer.SplitterDistance = 500;
            this.Controls.Add(m_oOuterSplitContainer);

            m_oInerSplitContainer.Orientation = Orientation.Vertical;
            m_oInerSplitContainer.Dock = DockStyle.Fill;
            m_oInerSplitContainer.SplitterDistance = 480;
            m_oOuterSplitContainer.Panel2.Controls.Add(m_oInerSplitContainer); // add iner split container to the bottom of the outer!

            m_oComponentsDataGridView.Dock = DockStyle.Fill;
            m_oOuterSplitContainer.Panel1.Controls.Add(m_oComponentsDataGridView);

            m_oSummaryRichTextBox.Dock = DockStyle.Fill;
            m_oSummaryRichTextBox.Text = "Design Summary";
            m_oSummaryRichTextBox.ReadOnly = true;
            m_oInerSplitContainer.Panel1.Controls.Add(m_oSummaryRichTextBox);

            m_oDesignErrorsRichTextBox.Dock = DockStyle.Fill;
            m_oDesignErrorsRichTextBox.Text = "Design Errors";
            m_oDesignErrorsRichTextBox.ReadOnly = true;
            m_oInerSplitContainer.Panel2.Controls.Add(m_oDesignErrorsRichTextBox);
        }

        public void SetHorzSplitterDistance(int a_iDist)
        {
            m_oInerSplitContainer.SplitterDistance = a_iDist;
        }

        public void SetVertSplitterDistance(int a_iDist)
        {
            m_oOuterSplitContainer.SplitterDistance = a_iDist;
        }
    }
}
