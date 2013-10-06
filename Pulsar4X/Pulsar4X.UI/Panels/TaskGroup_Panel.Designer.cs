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

        public ComboBox FactionSelectionComboBox
        {
            get
            {
                return m_oFactionName;
            }
        }

        public TextBox TaskGroupLocationTextBox
        {
            get
            {
                return m_oTGLocation;
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
            this.m_oGeneralTGDetails = new System.Windows.Forms.GroupBox();
            this.m_oTaskForceName = new System.Windows.Forms.ComboBox();
            this.m_oTFLabel = new System.Windows.Forms.Label();
            this.m_oLocationLabel = new System.Windows.Forms.Label();
            this.m_oTGLocation = new System.Windows.Forms.TextBox();
            this.m_oFactionName = new System.Windows.Forms.ComboBox();
            this.m_oFactionLabel = new System.Windows.Forms.Label();
            this.m_oTaskGroupLabel = new System.Windows.Forms.Label();
            this.m_oGeneralTGDetails.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oTaskGroupName
            // 
            this.m_oTaskGroupName.FormattingEnabled = true;
            this.m_oTaskGroupName.Location = new System.Drawing.Point(54, 52);
            this.m_oTaskGroupName.Name = "m_oTaskGroupName";
            this.m_oTaskGroupName.Size = new System.Drawing.Size(171, 21);
            this.m_oTaskGroupName.TabIndex = 1;
            // 
            // m_oShipsBox
            // 
            this.m_oShipsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oShipsBox.Location = new System.Drawing.Point(380, 12);
            this.m_oShipsBox.MaximumSize = new System.Drawing.Size(760, 300);
            this.m_oShipsBox.MinimumSize = new System.Drawing.Size(760, 300);
            this.m_oShipsBox.Name = "m_oShipsBox";
            this.m_oShipsBox.Size = new System.Drawing.Size(760, 300);
            this.m_oShipsBox.TabIndex = 2;
            this.m_oShipsBox.TabStop = false;
            this.m_oShipsBox.Text = "Ships in TaskGroup - Double-Click to open Ship window";
            // 
            // m_oGeneralTGDetails
            // 
            this.m_oGeneralTGDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oGeneralTGDetails.Controls.Add(this.m_oTaskForceName);
            this.m_oGeneralTGDetails.Controls.Add(this.m_oTFLabel);
            this.m_oGeneralTGDetails.Controls.Add(this.m_oLocationLabel);
            this.m_oGeneralTGDetails.Controls.Add(this.m_oTGLocation);
            this.m_oGeneralTGDetails.Controls.Add(this.m_oFactionName);
            this.m_oGeneralTGDetails.Controls.Add(this.m_oFactionLabel);
            this.m_oGeneralTGDetails.Controls.Add(this.m_oTaskGroupLabel);
            this.m_oGeneralTGDetails.Controls.Add(this.m_oTaskGroupName);
            this.m_oGeneralTGDetails.Location = new System.Drawing.Point(12, 12);
            this.m_oGeneralTGDetails.MaximumSize = new System.Drawing.Size(240, 150);
            this.m_oGeneralTGDetails.MinimumSize = new System.Drawing.Size(240, 150);
            this.m_oGeneralTGDetails.Name = "m_oGeneralTGDetails";
            this.m_oGeneralTGDetails.Size = new System.Drawing.Size(240, 150);
            this.m_oGeneralTGDetails.TabIndex = 3;
            this.m_oGeneralTGDetails.TabStop = false;
            this.m_oGeneralTGDetails.Text = "Details and Special Orders";
            // 
            // m_oTaskForceName
            // 
            this.m_oTaskForceName.FormattingEnabled = true;
            this.m_oTaskForceName.Location = new System.Drawing.Point(54, 111);
            this.m_oTaskForceName.Name = "m_oTaskForceName";
            this.m_oTaskForceName.Size = new System.Drawing.Size(171, 21);
            this.m_oTaskForceName.TabIndex = 28;
            // 
            // m_oTFLabel
            // 
            this.m_oTFLabel.AutoSize = true;
            this.m_oTFLabel.Location = new System.Drawing.Point(10, 106);
            this.m_oTFLabel.MaximumSize = new System.Drawing.Size(35, 26);
            this.m_oTFLabel.MinimumSize = new System.Drawing.Size(35, 26);
            this.m_oTFLabel.Name = "m_oTFLabel";
            this.m_oTFLabel.Size = new System.Drawing.Size(35, 26);
            this.m_oTFLabel.TabIndex = 27;
            this.m_oTFLabel.Text = "Task  Force";
            // 
            // m_oLocationLabel
            // 
            this.m_oLocationLabel.AutoSize = true;
            this.m_oLocationLabel.Location = new System.Drawing.Point(6, 82);
            this.m_oLocationLabel.Name = "m_oLocationLabel";
            this.m_oLocationLabel.Size = new System.Drawing.Size(48, 13);
            this.m_oLocationLabel.TabIndex = 26;
            this.m_oLocationLabel.Text = "Location";
            // 
            // m_oTGLocation
            // 
            this.m_oTGLocation.Enabled = false;
            this.m_oTGLocation.Location = new System.Drawing.Point(54, 79);
            this.m_oTGLocation.Name = "m_oTGLocation";
            this.m_oTGLocation.Size = new System.Drawing.Size(171, 20);
            this.m_oTGLocation.TabIndex = 25;
            // 
            // m_oFactionName
            // 
            this.m_oFactionName.FormattingEnabled = true;
            this.m_oFactionName.Location = new System.Drawing.Point(54, 25);
            this.m_oFactionName.Name = "m_oFactionName";
            this.m_oFactionName.Size = new System.Drawing.Size(171, 21);
            this.m_oFactionName.TabIndex = 24;
            // 
            // m_oFactionLabel
            // 
            this.m_oFactionLabel.AutoSize = true;
            this.m_oFactionLabel.Location = new System.Drawing.Point(6, 28);
            this.m_oFactionLabel.Name = "m_oFactionLabel";
            this.m_oFactionLabel.Size = new System.Drawing.Size(39, 13);
            this.m_oFactionLabel.TabIndex = 23;
            this.m_oFactionLabel.Text = "Empire";
            // 
            // m_oTaskGroupLabel
            // 
            this.m_oTaskGroupLabel.AutoSize = true;
            this.m_oTaskGroupLabel.Location = new System.Drawing.Point(6, 55);
            this.m_oTaskGroupLabel.Name = "m_oTaskGroupLabel";
            this.m_oTaskGroupLabel.Size = new System.Drawing.Size(35, 13);
            this.m_oTaskGroupLabel.TabIndex = 22;
            this.m_oTaskGroupLabel.Text = "Name";
            // 
            // TaskGroup_Panel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1163, 558);
            this.Controls.Add(this.m_oGeneralTGDetails);
            this.Controls.Add(this.m_oShipsBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "TaskGroup_Panel";
            this.Text = "Task Groups";
            this.m_oGeneralTGDetails.ResumeLayout(false);
            this.m_oGeneralTGDetails.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox m_oTaskGroupName;
        private GroupBox m_oShipsBox;
        private GroupBox m_oGeneralTGDetails;
        private Label m_oTaskGroupLabel;
        private ComboBox m_oFactionName;
        private Label m_oFactionLabel;
        private Label m_oLocationLabel;
        private TextBox m_oTGLocation;
        private ComboBox m_oTaskForceName;
        private Label m_oTFLabel;
    }
}