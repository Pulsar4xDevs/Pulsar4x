namespace Pulsar4X.UI.Panels
{
    partial class SysMap_Controls
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
            this.m_oSystemSelectionComboBox = new System.Windows.Forms.ComboBox();
            this.m_oViewControlsGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oZoomOutButton = new System.Windows.Forms.Button();
            this.m_oZoomInButton = new System.Windows.Forms.Button();
            this.m_oResetViewButton = new System.Windows.Forms.Button();
            this.m_oPanRightButton = new System.Windows.Forms.Button();
            this.m_oPanLeftButton = new System.Windows.Forms.Button();
            this.m_oPanDownButton = new System.Windows.Forms.Button();
            this.m_oPanUpButton = new System.Windows.Forms.Button();
            this.m_oScaleGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oScaleKMLabel = new System.Windows.Forms.Label();
            this.m_oScaleAULabel = new System.Windows.Forms.Label();
            this.m_oViewControlsGroupBox.SuspendLayout();
            this.m_oScaleGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oSystemSelectionComboBox
            // 
            this.m_oSystemSelectionComboBox.FormattingEnabled = true;
            this.m_oSystemSelectionComboBox.Location = new System.Drawing.Point(13, 13);
            this.m_oSystemSelectionComboBox.Name = "m_oSystemSelectionComboBox";
            this.m_oSystemSelectionComboBox.Size = new System.Drawing.Size(183, 21);
            this.m_oSystemSelectionComboBox.TabIndex = 0;
            // 
            // m_oViewControlsGroupBox
            // 
            this.m_oViewControlsGroupBox.Controls.Add(this.m_oZoomOutButton);
            this.m_oViewControlsGroupBox.Controls.Add(this.m_oZoomInButton);
            this.m_oViewControlsGroupBox.Controls.Add(this.m_oResetViewButton);
            this.m_oViewControlsGroupBox.Controls.Add(this.m_oPanRightButton);
            this.m_oViewControlsGroupBox.Controls.Add(this.m_oPanLeftButton);
            this.m_oViewControlsGroupBox.Controls.Add(this.m_oPanDownButton);
            this.m_oViewControlsGroupBox.Controls.Add(this.m_oPanUpButton);
            this.m_oViewControlsGroupBox.Location = new System.Drawing.Point(13, 41);
            this.m_oViewControlsGroupBox.Name = "m_oViewControlsGroupBox";
            this.m_oViewControlsGroupBox.Size = new System.Drawing.Size(183, 106);
            this.m_oViewControlsGroupBox.TabIndex = 1;
            this.m_oViewControlsGroupBox.TabStop = false;
            this.m_oViewControlsGroupBox.Text = "View Controls";
            // 
            // m_oZoomOutButton
            // 
            this.m_oZoomOutButton.Location = new System.Drawing.Point(102, 19);
            this.m_oZoomOutButton.Name = "m_oZoomOutButton";
            this.m_oZoomOutButton.Size = new System.Drawing.Size(23, 23);
            this.m_oZoomOutButton.TabIndex = 5;
            this.m_oZoomOutButton.Text = "-";
            this.m_oZoomOutButton.UseVisualStyleBackColor = true;
            // 
            // m_oZoomInButton
            // 
            this.m_oZoomInButton.Location = new System.Drawing.Point(154, 19);
            this.m_oZoomInButton.Name = "m_oZoomInButton";
            this.m_oZoomInButton.Size = new System.Drawing.Size(23, 23);
            this.m_oZoomInButton.TabIndex = 6;
            this.m_oZoomInButton.Text = "+";
            this.m_oZoomInButton.UseVisualStyleBackColor = true;
            // 
            // m_oResetViewButton
            // 
            this.m_oResetViewButton.Location = new System.Drawing.Point(102, 74);
            this.m_oResetViewButton.Name = "m_oResetViewButton";
            this.m_oResetViewButton.Size = new System.Drawing.Size(75, 23);
            this.m_oResetViewButton.TabIndex = 7;
            this.m_oResetViewButton.Text = "Reset";
            this.m_oResetViewButton.UseVisualStyleBackColor = true;
            // 
            // m_oPanRightButton
            // 
            this.m_oPanRightButton.Location = new System.Drawing.Point(58, 45);
            this.m_oPanRightButton.Name = "m_oPanRightButton";
            this.m_oPanRightButton.Size = new System.Drawing.Size(23, 23);
            this.m_oPanRightButton.TabIndex = 3;
            this.m_oPanRightButton.Text = ">";
            this.m_oPanRightButton.UseVisualStyleBackColor = true;
            // 
            // m_oPanLeftButton
            // 
            this.m_oPanLeftButton.Location = new System.Drawing.Point(6, 45);
            this.m_oPanLeftButton.Name = "m_oPanLeftButton";
            this.m_oPanLeftButton.Size = new System.Drawing.Size(23, 23);
            this.m_oPanLeftButton.TabIndex = 1;
            this.m_oPanLeftButton.Text = "<";
            this.m_oPanLeftButton.UseVisualStyleBackColor = true;
            // 
            // m_oPanDownButton
            // 
            this.m_oPanDownButton.Location = new System.Drawing.Point(31, 74);
            this.m_oPanDownButton.Name = "m_oPanDownButton";
            this.m_oPanDownButton.Size = new System.Drawing.Size(23, 23);
            this.m_oPanDownButton.TabIndex = 4;
            this.m_oPanDownButton.Text = "v";
            this.m_oPanDownButton.UseVisualStyleBackColor = true;
            // 
            // m_oPanUpButton
            // 
            this.m_oPanUpButton.Location = new System.Drawing.Point(31, 19);
            this.m_oPanUpButton.Name = "m_oPanUpButton";
            this.m_oPanUpButton.Size = new System.Drawing.Size(23, 23);
            this.m_oPanUpButton.TabIndex = 2;
            this.m_oPanUpButton.Text = "^";
            this.m_oPanUpButton.UseVisualStyleBackColor = true;
            // 
            // m_oScaleGroupBox
            // 
            this.m_oScaleGroupBox.Controls.Add(this.m_oScaleAULabel);
            this.m_oScaleGroupBox.Controls.Add(this.m_oScaleKMLabel);
            this.m_oScaleGroupBox.Location = new System.Drawing.Point(13, 153);
            this.m_oScaleGroupBox.Name = "m_oScaleGroupBox";
            this.m_oScaleGroupBox.Size = new System.Drawing.Size(183, 47);
            this.m_oScaleGroupBox.TabIndex = 2;
            this.m_oScaleGroupBox.TabStop = false;
            this.m_oScaleGroupBox.Text = "Scale";
            // 
            // m_oScaleKMLabel
            // 
            this.m_oScaleKMLabel.AutoSize = true;
            this.m_oScaleKMLabel.Location = new System.Drawing.Point(7, 16);
            this.m_oScaleKMLabel.Name = "m_oScaleKMLabel";
            this.m_oScaleKMLabel.Size = new System.Drawing.Size(22, 13);
            this.m_oScaleKMLabel.TabIndex = 0;
            this.m_oScaleKMLabel.Text = "Km";
            // 
            // m_oScaleAULabel
            // 
            this.m_oScaleAULabel.AutoSize = true;
            this.m_oScaleAULabel.Location = new System.Drawing.Point(7, 29);
            this.m_oScaleAULabel.Name = "m_oScaleAULabel";
            this.m_oScaleAULabel.Size = new System.Drawing.Size(22, 13);
            this.m_oScaleAULabel.TabIndex = 1;
            this.m_oScaleAULabel.Text = "AU";
            // 
            // SysMap_Controls
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(208, 562);
            this.Controls.Add(this.m_oScaleGroupBox);
            this.Controls.Add(this.m_oViewControlsGroupBox);
            this.Controls.Add(this.m_oSystemSelectionComboBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "SysMap_Controls";
            this.m_oViewControlsGroupBox.ResumeLayout(false);
            this.m_oScaleGroupBox.ResumeLayout(false);
            this.m_oScaleGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox m_oSystemSelectionComboBox;
        private System.Windows.Forms.GroupBox m_oViewControlsGroupBox;
        private System.Windows.Forms.GroupBox m_oScaleGroupBox;
        private System.Windows.Forms.Button m_oZoomOutButton;
        private System.Windows.Forms.Button m_oZoomInButton;
        private System.Windows.Forms.Button m_oResetViewButton;
        private System.Windows.Forms.Button m_oPanRightButton;
        private System.Windows.Forms.Button m_oPanLeftButton;
        private System.Windows.Forms.Button m_oPanDownButton;
        private System.Windows.Forms.Button m_oPanUpButton;
        private System.Windows.Forms.Label m_oScaleAULabel;
        private System.Windows.Forms.Label m_oScaleKMLabel;
    }
}
