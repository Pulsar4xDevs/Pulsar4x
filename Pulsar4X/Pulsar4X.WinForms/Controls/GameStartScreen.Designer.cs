namespace Pulsar4X.WinForms.Controls
{
    partial class GameStartScreen
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
            this.NewGameButton = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.labelTagLine = new System.Windows.Forms.Label();
            this.LoadGameButton = new System.Windows.Forms.Button();
            this.OptionsButton = new System.Windows.Forms.Button();
            this.ExitButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // NewGameButton
            // 
            this.NewGameButton.Location = new System.Drawing.Point(385, 225);
            this.NewGameButton.Name = "NewGameButton";
            this.NewGameButton.Size = new System.Drawing.Size(75, 23);
            this.NewGameButton.TabIndex = 0;
            this.NewGameButton.Text = "New Game";
            this.NewGameButton.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(78, 38);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(748, 129);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // labelTagLine
            // 
            this.labelTagLine.AutoSize = true;
            this.labelTagLine.Location = new System.Drawing.Point(354, 170);
            this.labelTagLine.Name = "labelTagLine";
            this.labelTagLine.Size = new System.Drawing.Size(140, 13);
            this.labelTagLine.TabIndex = 2;
            this.labelTagLine.Text = "A Fanmade Clone of Aurora!";
            // 
            // LoadGameButton
            // 
            this.LoadGameButton.Location = new System.Drawing.Point(385, 254);
            this.LoadGameButton.Name = "LoadGameButton";
            this.LoadGameButton.Size = new System.Drawing.Size(75, 23);
            this.LoadGameButton.TabIndex = 3;
            this.LoadGameButton.Text = "Load Game";
            this.LoadGameButton.UseVisualStyleBackColor = true;
            // 
            // OptionsButton
            // 
            this.OptionsButton.Location = new System.Drawing.Point(385, 283);
            this.OptionsButton.Name = "OptionsButton";
            this.OptionsButton.Size = new System.Drawing.Size(75, 23);
            this.OptionsButton.TabIndex = 4;
            this.OptionsButton.Text = "Options";
            this.OptionsButton.UseVisualStyleBackColor = true;
            // 
            // ExitButton
            // 
            this.ExitButton.Location = new System.Drawing.Point(385, 312);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 5;
            this.ExitButton.Text = "Exit";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // GameStartScreen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.OptionsButton);
            this.Controls.Add(this.LoadGameButton);
            this.Controls.Add(this.labelTagLine);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.NewGameButton);
            this.Name = "GameStartScreen";
            this.Size = new System.Drawing.Size(911, 531);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button NewGameButton;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label labelTagLine;
        private System.Windows.Forms.Button LoadGameButton;
        private System.Windows.Forms.Button OptionsButton;
        private System.Windows.Forms.Button ExitButton;
    }
}
