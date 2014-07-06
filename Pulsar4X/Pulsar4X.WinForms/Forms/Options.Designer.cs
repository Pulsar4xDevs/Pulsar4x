namespace Pulsar4X.WinForms.Forms
{
    partial class Options
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Options));
            this.TabControl = new System.Windows.Forms.TabControl();
            this.GeneralTabPage = new System.Windows.Forms.TabPage();
            this.GraphicsTabPage = new System.Windows.Forms.TabPage();
            this.OpenGLVerLabel = new System.Windows.Forms.Label();
            this.ForecOpenGLVerCheckBox = new System.Windows.Forms.CheckBox();
            this.OpenGLVerComboBox = new System.Windows.Forms.ComboBox();
            this.InfoToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.GLFontLabel = new System.Windows.Forms.Label();
            this.GLFontComboBox = new System.Windows.Forms.ComboBox();
            this.TabControl.SuspendLayout();
            this.GraphicsTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // TabControl
            // 
            this.TabControl.Controls.Add(this.GeneralTabPage);
            this.TabControl.Controls.Add(this.GraphicsTabPage);
            this.TabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TabControl.Location = new System.Drawing.Point(0, 0);
            this.TabControl.Name = "TabControl";
            this.TabControl.SelectedIndex = 0;
            this.TabControl.Size = new System.Drawing.Size(584, 362);
            this.TabControl.TabIndex = 0;
            // 
            // GeneralTabPage
            // 
            this.GeneralTabPage.Location = new System.Drawing.Point(4, 22);
            this.GeneralTabPage.Name = "GeneralTabPage";
            this.GeneralTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.GeneralTabPage.Size = new System.Drawing.Size(576, 336);
            this.GeneralTabPage.TabIndex = 0;
            this.GeneralTabPage.Text = "General";
            this.GeneralTabPage.UseVisualStyleBackColor = true;
            // 
            // GraphicsTabPage
            // 
            this.GraphicsTabPage.Controls.Add(this.GLFontComboBox);
            this.GraphicsTabPage.Controls.Add(this.GLFontLabel);
            this.GraphicsTabPage.Controls.Add(this.OpenGLVerLabel);
            this.GraphicsTabPage.Controls.Add(this.ForecOpenGLVerCheckBox);
            this.GraphicsTabPage.Controls.Add(this.OpenGLVerComboBox);
            this.GraphicsTabPage.Location = new System.Drawing.Point(4, 22);
            this.GraphicsTabPage.Name = "GraphicsTabPage";
            this.GraphicsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.GraphicsTabPage.Size = new System.Drawing.Size(576, 336);
            this.GraphicsTabPage.TabIndex = 1;
            this.GraphicsTabPage.Text = "Graphics";
            this.GraphicsTabPage.UseVisualStyleBackColor = true;
            // 
            // OpenGLVerLabel
            // 
            this.OpenGLVerLabel.AutoSize = true;
            this.OpenGLVerLabel.Enabled = false;
            this.OpenGLVerLabel.Location = new System.Drawing.Point(6, 30);
            this.OpenGLVerLabel.Name = "OpenGLVerLabel";
            this.OpenGLVerLabel.Size = new System.Drawing.Size(88, 13);
            this.OpenGLVerLabel.TabIndex = 2;
            this.OpenGLVerLabel.Text = "OpenGL Version:";
            // 
            // ForecOpenGLVerCheckBox
            // 
            this.ForecOpenGLVerCheckBox.AutoSize = true;
            this.ForecOpenGLVerCheckBox.Location = new System.Drawing.Point(9, 7);
            this.ForecOpenGLVerCheckBox.Name = "ForecOpenGLVerCheckBox";
            this.ForecOpenGLVerCheckBox.Size = new System.Drawing.Size(134, 17);
            this.ForecOpenGLVerCheckBox.TabIndex = 0;
            this.ForecOpenGLVerCheckBox.Text = "Force OpenGL Version";
            this.InfoToolTip.SetToolTip(this.ForecOpenGLVerCheckBox, resources.GetString("ForecOpenGLVerCheckBox.ToolTip"));
            this.ForecOpenGLVerCheckBox.UseVisualStyleBackColor = true;
            this.ForecOpenGLVerCheckBox.CheckedChanged += new System.EventHandler(this.ForecOpenGLVerCheckBox_CheckedChanged);
            // 
            // OpenGLVerComboBox
            // 
            this.OpenGLVerComboBox.Enabled = false;
            this.OpenGLVerComboBox.FormattingEnabled = true;
            this.OpenGLVerComboBox.Items.AddRange(new object[] {
            "2.0",
            "2.1",
            "3.0",
            "3.1",
            "3.2",
            "3.3",
            "4.0",
            "4.1",
            "4.2",
            "4.3"});
            this.OpenGLVerComboBox.Location = new System.Drawing.Point(103, 27);
            this.OpenGLVerComboBox.Name = "OpenGLVerComboBox";
            this.OpenGLVerComboBox.Size = new System.Drawing.Size(40, 21);
            this.OpenGLVerComboBox.TabIndex = 1;
            this.OpenGLVerComboBox.Text = "2.0";
            this.InfoToolTip.SetToolTip(this.OpenGLVerComboBox, "Select OpenGL version to use.");
            this.OpenGLVerComboBox.SelectedIndexChanged += new System.EventHandler(this.OpenGLVerComboBox_SelectedIndexChanged);
            // 
            // GLFontLabel
            // 
            this.GLFontLabel.AutoSize = true;
            this.GLFontLabel.Location = new System.Drawing.Point(9, 58);
            this.GLFontLabel.Name = "GLFontLabel";
            this.GLFontLabel.Size = new System.Drawing.Size(71, 13);
            this.GLFontLabel.TabIndex = 3;
            this.GLFontLabel.Text = "OpenGL Font";
            // 
            // GLFontComboBox
            // 
            this.GLFontComboBox.FormattingEnabled = true;
            this.GLFontComboBox.Items.AddRange(new object[] {
            "DejaVu Sans Mono",
            "Droid Sans Mono",
            "Microsoft Sans Serif"});
            this.GLFontComboBox.Location = new System.Drawing.Point(12, 75);
            this.GLFontComboBox.Name = "GLFontComboBox";
            this.GLFontComboBox.Size = new System.Drawing.Size(121, 21);
            this.GLFontComboBox.TabIndex = 4;
            this.GLFontComboBox.Text = "DejaVu Sans Mono";
            this.GLFontComboBox.SelectedIndexChanged += new System.EventHandler(this.GLFontComboBox_SelectedIndexChanged);
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 362);
            this.Controls.Add(this.TabControl);
            this.Name = "Options";
            this.Text = "Options";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Options_FormClosing);
            this.TabControl.ResumeLayout(false);
            this.GraphicsTabPage.ResumeLayout(false);
            this.GraphicsTabPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl TabControl;
        private System.Windows.Forms.TabPage GeneralTabPage;
        private System.Windows.Forms.TabPage GraphicsTabPage;
        private System.Windows.Forms.Label OpenGLVerLabel;
        private System.Windows.Forms.CheckBox ForecOpenGLVerCheckBox;
        private System.Windows.Forms.ComboBox OpenGLVerComboBox;
        private System.Windows.Forms.ToolTip InfoToolTip;
        private System.Windows.Forms.Label GLFontLabel;
        private System.Windows.Forms.ComboBox GLFontComboBox;
    }
}