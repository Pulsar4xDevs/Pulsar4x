namespace Pulsar4X.UI.Panels
{
    partial class Eco_Populations
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.FactionGroupBox = new System.Windows.Forms.GroupBox();
            this.PopulatedSystemGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oFactionSelectionComboBox = new System.Windows.Forms.ComboBox();
            this.m_oPopulationsTreeView = new System.Windows.Forms.TreeView();
            this.FactionGroupBox.SuspendLayout();
            this.PopulatedSystemGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // FactionGroupBox
            // 
            this.FactionGroupBox.Controls.Add(this.m_oFactionSelectionComboBox);
            this.FactionGroupBox.Location = new System.Drawing.Point(2, 4);
            this.FactionGroupBox.Name = "FactionGroupBox";
            this.FactionGroupBox.Size = new System.Drawing.Size(200, 51);
            this.FactionGroupBox.TabIndex = 0;
            this.FactionGroupBox.TabStop = false;
            this.FactionGroupBox.Text = "Faction";
            // 
            // PopulatedSystemGroupBox
            // 
            this.PopulatedSystemGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PopulatedSystemGroupBox.AutoSize = true;
            this.PopulatedSystemGroupBox.Controls.Add(this.m_oPopulationsTreeView);
            this.PopulatedSystemGroupBox.Location = new System.Drawing.Point(2, 61);
            this.PopulatedSystemGroupBox.Name = "PopulatedSystemGroupBox";
            this.PopulatedSystemGroupBox.Size = new System.Drawing.Size(200, 376);
            this.PopulatedSystemGroupBox.TabIndex = 1;
            this.PopulatedSystemGroupBox.TabStop = false;
            this.PopulatedSystemGroupBox.Text = "Populated Systems";
            // 
            // m_oFactionSelectionComboBox
            // 
            this.m_oFactionSelectionComboBox.FormattingEnabled = true;
            this.m_oFactionSelectionComboBox.Location = new System.Drawing.Point(11, 20);
            this.m_oFactionSelectionComboBox.Name = "m_oFactionSelectionComboBox";
            this.m_oFactionSelectionComboBox.Size = new System.Drawing.Size(183, 21);
            this.m_oFactionSelectionComboBox.TabIndex = 0;
            // 
            // m_oPopulationsTreeView
            // 
            this.m_oPopulationsTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_oPopulationsTreeView.Location = new System.Drawing.Point(3, 16);
            this.m_oPopulationsTreeView.Name = "m_oPopulationsTreeView";
            this.m_oPopulationsTreeView.Size = new System.Drawing.Size(194, 357);
            this.m_oPopulationsTreeView.TabIndex = 0;
            // 
            // Eco_Populations
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(208, 449);
            this.Controls.Add(this.PopulatedSystemGroupBox);
            this.Controls.Add(this.FactionGroupBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Eco_Populations";
            this.FactionGroupBox.ResumeLayout(false);
            this.PopulatedSystemGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox FactionGroupBox;
        private System.Windows.Forms.ComboBox m_oFactionSelectionComboBox;
        private System.Windows.Forms.GroupBox PopulatedSystemGroupBox;
        private System.Windows.Forms.TreeView m_oPopulationsTreeView;
    }
}
