namespace Pulsar4X.UI.Dialogs
{
    partial class AboutBox
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
            this.OKButton = new System.Windows.Forms.Button();
            this.m_oPulsarLogoPicBox = new System.Windows.Forms.PictureBox();
            this.AboutTextBox = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.m_oPulsarLogoPicBox)).BeginInit();
            this.SuspendLayout();
            // 
            // OKButton
            // 
            this.OKButton.Location = new System.Drawing.Point(497, 327);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 2;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // m_oPulsarLogoPicBox
            // 
            this.m_oPulsarLogoPicBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.m_oPulsarLogoPicBox.ImageLocation = "";
            this.m_oPulsarLogoPicBox.Location = new System.Drawing.Point(13, 13);
            this.m_oPulsarLogoPicBox.Name = "m_oPulsarLogoPicBox";
            this.m_oPulsarLogoPicBox.Size = new System.Drawing.Size(249, 337);
            this.m_oPulsarLogoPicBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.m_oPulsarLogoPicBox.TabIndex = 0;
            this.m_oPulsarLogoPicBox.TabStop = false;
            // 
            // AboutTextBox
            // 
            this.AboutTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.AboutTextBox.Cursor = System.Windows.Forms.Cursors.Default;
            this.AboutTextBox.Location = new System.Drawing.Point(268, 13);
            this.AboutTextBox.Name = "AboutTextBox";
            this.AboutTextBox.ReadOnly = true;
            this.AboutTextBox.ShortcutsEnabled = false;
            this.AboutTextBox.Size = new System.Drawing.Size(304, 308);
            this.AboutTextBox.TabIndex = 1;
            this.AboutTextBox.TabStop = false;
            this.AboutTextBox.Text = "";
            // 
            // AboutBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 362);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.AboutTextBox);
            this.Controls.Add(this.m_oPulsarLogoPicBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "AboutBox";
            this.Text = "AboutBox";
            this.Load += new System.EventHandler(this.AboutBox_Load);
            ((System.ComponentModel.ISupportInitialize)(this.m_oPulsarLogoPicBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.PictureBox m_oPulsarLogoPicBox;
        private System.Windows.Forms.RichTextBox AboutTextBox;
    }
}