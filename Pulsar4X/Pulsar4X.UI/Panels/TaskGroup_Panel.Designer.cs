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
    partial class TaskGroup_Panel : DockContent
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>   
        /// Gets the TG selection combo box. 
        /// </summary>
        public ComboBox TaskGroupSelectionComboBox
        {
            get
            {
                return m_oTaskGroupName;
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.m_oTaskGroupName = new System.Windows.Forms.ComboBox();
            this.m_oShipsBox = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // m_oTaskGroupName
            // 
            this.m_oTaskGroupName.FormattingEnabled = true;
            this.m_oTaskGroupName.Location = new System.Drawing.Point(12, 12);
            this.m_oTaskGroupName.Name = "m_oTaskGroupName";
            this.m_oTaskGroupName.Size = new System.Drawing.Size(171, 21);
            this.m_oTaskGroupName.TabIndex = 1;
            // 
            // m_oShipsBox
            // 
            this.m_oShipsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oShipsBox.Location = new System.Drawing.Point(252, 12);
            this.m_oShipsBox.Name = "m_oShipsBox";
            this.m_oShipsBox.Size = new System.Drawing.Size(921, 299);
            this.m_oShipsBox.TabIndex = 2;
            this.m_oShipsBox.TabStop = false;
            this.m_oShipsBox.Text = "Ships in TaskGroup - Double-Click to open Ship window";
            // 
            // TaskGroup_Panel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1185, 652);
            this.Controls.Add(this.m_oShipsBox);
            this.Controls.Add(this.m_oTaskGroupName);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "TaskGroup_Panel";
            this.Text = "Task Groups";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox m_oTaskGroupName;
        private GroupBox m_oShipsBox;
    }
}