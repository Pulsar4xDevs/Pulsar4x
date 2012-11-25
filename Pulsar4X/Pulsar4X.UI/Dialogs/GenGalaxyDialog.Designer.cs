namespace Pulsar4X.UI.Dialogs
{
    partial class GenGalaxyDialog
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.GenProgressBar = new System.Windows.Forms.ProgressBar();
            this.GalaxyNameTextBox = new System.Windows.Forms.TextBox();
            this.GalaxyNameLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.NoOfSystemsTextBox = new System.Windows.Forms.TextBox();
            this.GenerateButton = new System.Windows.Forms.Button();
            this.CloseButton = new System.Windows.Forms.Button();
            this.Timelabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // GenProgressBar
            // 
            this.GenProgressBar.Location = new System.Drawing.Point(5, 95);
            this.GenProgressBar.Name = "GenProgressBar";
            this.GenProgressBar.Size = new System.Drawing.Size(238, 23);
            this.GenProgressBar.TabIndex = 0;
            // 
            // GalaxyNameTextBox
            // 
            this.GalaxyNameTextBox.Location = new System.Drawing.Point(78, 12);
            this.GalaxyNameTextBox.Name = "GalaxyNameTextBox";
            this.GalaxyNameTextBox.Size = new System.Drawing.Size(165, 20);
            this.GalaxyNameTextBox.TabIndex = 1;
            this.GalaxyNameTextBox.Text = "Milky Way";
            // 
            // GalaxyNameLabel
            // 
            this.GalaxyNameLabel.AutoSize = true;
            this.GalaxyNameLabel.Location = new System.Drawing.Point(2, 15);
            this.GalaxyNameLabel.Name = "GalaxyNameLabel";
            this.GalaxyNameLabel.Size = new System.Drawing.Size(73, 13);
            this.GalaxyNameLabel.TabIndex = 2;
            this.GalaxyNameLabel.Text = "Galaxy Name:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "No of Systems:";
            // 
            // NoOfSystemsTextBox
            // 
            this.NoOfSystemsTextBox.Location = new System.Drawing.Point(86, 36);
            this.NoOfSystemsTextBox.Name = "NoOfSystemsTextBox";
            this.NoOfSystemsTextBox.Size = new System.Drawing.Size(157, 20);
            this.NoOfSystemsTextBox.TabIndex = 4;
            this.NoOfSystemsTextBox.Text = "1000";
            // 
            // GenerateButton
            // 
            this.GenerateButton.Location = new System.Drawing.Point(23, 66);
            this.GenerateButton.Name = "GenerateButton";
            this.GenerateButton.Size = new System.Drawing.Size(75, 23);
            this.GenerateButton.TabIndex = 5;
            this.GenerateButton.Text = "Generate!";
            this.GenerateButton.UseVisualStyleBackColor = true;
            this.GenerateButton.Click += new System.EventHandler(this.GenerateButton_Click);
            // 
            // CloseButton
            // 
            this.CloseButton.Location = new System.Drawing.Point(154, 66);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 6;
            this.CloseButton.Text = "Close";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // Timelabel
            // 
            this.Timelabel.AutoSize = true;
            this.Timelabel.Location = new System.Drawing.Point(34, 102);
            this.Timelabel.Name = "Timelabel";
            this.Timelabel.Size = new System.Drawing.Size(0, 13);
            this.Timelabel.TabIndex = 7;
            // 
            // GenGalaxyDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(249, 127);
            this.Controls.Add(this.Timelabel);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.GenerateButton);
            this.Controls.Add(this.NoOfSystemsTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.GalaxyNameLabel);
            this.Controls.Add(this.GalaxyNameTextBox);
            this.Controls.Add(this.GenProgressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "GenGalaxyDialog";
            this.Text = "GenGalaxyDialog";
            this.Load += new System.EventHandler(this.GenGalaxyDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar GenProgressBar;
        private System.Windows.Forms.TextBox GalaxyNameTextBox;
        private System.Windows.Forms.Label GalaxyNameLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox NoOfSystemsTextBox;
        private System.Windows.Forms.Button GenerateButton;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Label Timelabel;
    }
}