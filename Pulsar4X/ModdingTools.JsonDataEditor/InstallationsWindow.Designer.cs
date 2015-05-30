namespace ModdingTools.JsonDataEditor
{
    partial class InstallationsWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallationsWindow));
            this.mainMenuButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.genericDataUC1 = new ModdingTools.JsonDataEditor.GenericDataUC();
            this.techRequirementsUC1 = new ModdingTools.JsonDataEditor.TechRequirementsUC();
            this.mineralsCostsUC1 = new ModdingTools.JsonDataEditor.MineralsCostsUC();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenuButton
            // 
            this.mainMenuButton.Location = new System.Drawing.Point(3, 420);
            this.mainMenuButton.Name = "mainMenuButton";
            this.mainMenuButton.Size = new System.Drawing.Size(77, 23);
            this.mainMenuButton.TabIndex = 0;
            this.mainMenuButton.Text = "Main Menu";
            this.mainMenuButton.UseVisualStyleBackColor = true;
            this.mainMenuButton.Click += new System.EventHandler(this.mainMenuButton_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 86F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 47.66537F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52.33463F));
            this.tableLayoutPanel1.Controls.Add(this.genericDataUC1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.mainMenuButton, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.mineralsCostsUC1, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.techRequirementsUC1, 2, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 42.21636F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 57.78364F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(600, 460);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // genericDataUC1
            // 
            this.genericDataUC1.Location = new System.Drawing.Point(89, 3);
            this.genericDataUC1.MinimumSize = new System.Drawing.Size(240, 0);
            this.genericDataUC1.Name = "genericDataUC1";
            this.genericDataUC1.Size = new System.Drawing.Size(240, 147);
            this.genericDataUC1.TabIndex = 1;
            // 
            // techRequirementsUC1
            // 
            this.techRequirementsUC1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.techRequirementsUC1.Location = new System.Drawing.Point(334, 179);
            this.techRequirementsUC1.Name = "techRequirementsUC1";
            this.techRequirementsUC1.Size = new System.Drawing.Size(263, 235);
            this.techRequirementsUC1.TabIndex = 2;
            // 
            // mineralsCostsUC1
            // 
            this.mineralsCostsUC1.AllMineralSds = null;
            this.mineralsCostsUC1.Location = new System.Drawing.Point(334, 3);
            this.mineralsCostsUC1.MineralsCosts = ((System.Collections.Generic.Dictionary<ModdingTools.JsonDataEditor.DataHolder, int>)(resources.GetObject("mineralsCostsUC1.MineralsCosts")));
            this.mineralsCostsUC1.Name = "mineralsCostsUC1";
            this.mineralsCostsUC1.Size = new System.Drawing.Size(263, 170);
            this.mineralsCostsUC1.TabIndex = 3;
            // 
            // InstallationsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "InstallationsWindow";
            this.Size = new System.Drawing.Size(600, 460);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button mainMenuButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private GenericDataUC genericDataUC1;
        private TechRequirementsUC techRequirementsUC1;
        private MineralsCostsUC mineralsCostsUC1;
    }
}
