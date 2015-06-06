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
            this.mainMenuButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.genericDataUC1 = new ModdingTools.JsonDataEditor.GenericDataUC();
            this.mineralsCostsUC1 = new ModdingTools.JsonDataEditor.MineralsCostsUC();
            this.techRequirementsUC1 = new ModdingTools.JsonDataEditor.TechRequirementsUC();
            this.listBox_AllInstalations = new System.Windows.Forms.ListBox();
            this.installationUC1 = new ModdingTools.JsonDataEditor.UserControls.InstallationUC();
            this.abilitiesListUC1 = new ModdingTools.JsonDataEditor.AbilitiesListUC();
            this.button_clearSelection = new System.Windows.Forms.Button();
            this.button_saveNew = new System.Windows.Forms.Button();
            this.button_updateExsisting = new System.Windows.Forms.Button();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenuButton
            // 
            this.mainMenuButton.Location = new System.Drawing.Point(3, 53);
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
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 47.58221F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52.41779F));
            this.tableLayoutPanel1.Controls.Add(this.genericDataUC1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.mineralsCostsUC1, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.techRequirementsUC1, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.listBox_AllInstalations, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.installationUC1, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.abilitiesListUC1, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(692, 460);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // genericDataUC1
            // 
            this.genericDataUC1.Description = "";
            this.genericDataUC1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.genericDataUC1.Location = new System.Drawing.Point(178, 3);
            this.genericDataUC1.MinimumSize = new System.Drawing.Size(240, 0);
            this.genericDataUC1.Name = "genericDataUC1";
            this.genericDataUC1.Size = new System.Drawing.Size(240, 147);
            this.genericDataUC1.TabIndex = 1;
            // 
            // mineralsCostsUC1
            // 
            this.mineralsCostsUC1.Location = new System.Drawing.Point(424, 3);
            //this.mineralsCostsUC1.MineralCosts = null;
            this.mineralsCostsUC1.Name = "mineralsCostsUC1";
            this.mineralsCostsUC1.Size = new System.Drawing.Size(263, 133);
            this.mineralsCostsUC1.TabIndex = 3;
            // 
            // techRequirementsUC1
            // 
            this.techRequirementsUC1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.techRequirementsUC1.Location = new System.Drawing.Point(424, 156);
            this.techRequirementsUC1.Name = "techRequirementsUC1";
            this.techRequirementsUC1.Size = new System.Drawing.Size(265, 147);
            this.techRequirementsUC1.TabIndex = 2;
            // 
            // listBox_AllInstalations
            // 
            this.listBox_AllInstalations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_AllInstalations.FormattingEnabled = true;
            this.listBox_AllInstalations.Location = new System.Drawing.Point(3, 3);
            this.listBox_AllInstalations.Name = "listBox_AllInstalations";
            this.tableLayoutPanel1.SetRowSpan(this.listBox_AllInstalations, 2);
            this.listBox_AllInstalations.Size = new System.Drawing.Size(169, 300);
            this.listBox_AllInstalations.TabIndex = 4;
            this.listBox_AllInstalations.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listBox_AllInstalations_MouseDoubleClick);
            // 
            // installationUC1
            // 
            this.installationUC1.Location = new System.Drawing.Point(178, 156);
            this.installationUC1.Name = "installationUC1";
            this.installationUC1.Size = new System.Drawing.Size(150, 104);
            this.installationUC1.TabIndex = 5;
            // 
            // abilitiesListUC1
            // 
            //this.abilitiesListUC1.AbilityAmount = null;
            this.abilitiesListUC1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.abilitiesListUC1.Location = new System.Drawing.Point(178, 309);
            this.abilitiesListUC1.Name = "abilitiesListUC1";
            this.abilitiesListUC1.Size = new System.Drawing.Size(240, 148);
            this.abilitiesListUC1.TabIndex = 6;
            // 
            // button_clearSelection
            // 
            this.button_clearSelection.Location = new System.Drawing.Point(3, 3);
            this.button_clearSelection.Name = "button_clearSelection";
            this.button_clearSelection.Size = new System.Drawing.Size(78, 23);
            this.button_clearSelection.TabIndex = 7;
            this.button_clearSelection.Text = "Clear Selected";
            this.button_clearSelection.UseVisualStyleBackColor = true;
            this.button_clearSelection.Click += new System.EventHandler(this.button_clearSelection_Click);
            // 
            // button_saveNew
            // 
            this.button_saveNew.Location = new System.Drawing.Point(87, 3);
            this.button_saveNew.Name = "button_saveNew";
            this.button_saveNew.Size = new System.Drawing.Size(75, 23);
            this.button_saveNew.TabIndex = 8;
            this.button_saveNew.Text = "Save New";
            this.button_saveNew.UseVisualStyleBackColor = true;
            this.button_saveNew.Click += new System.EventHandler(this.button_saveNew_Click);
            // 
            // button_updateExsisting
            // 
            this.button_updateExsisting.Location = new System.Drawing.Point(87, 53);
            this.button_updateExsisting.Name = "button_updateExsisting";
            this.button_updateExsisting.Size = new System.Drawing.Size(75, 23);
            this.button_updateExsisting.TabIndex = 9;
            this.button_updateExsisting.Text = "Update Exsisting";
            this.button_updateExsisting.UseVisualStyleBackColor = true;
            this.button_updateExsisting.Click += new System.EventHandler(this.button_updateExsisting_Click);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.button_updateExsisting, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.mainMenuButton, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.button_saveNew, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.button_clearSelection, 0, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 309);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(169, 100);
            this.tableLayoutPanel2.TabIndex = 10;
            // 
            // InstallationsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "InstallationsWindow";
            this.Size = new System.Drawing.Size(692, 460);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button mainMenuButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private GenericDataUC genericDataUC1;
        private TechRequirementsUC techRequirementsUC1;
        private MineralsCostsUC mineralsCostsUC1;
        private System.Windows.Forms.ListBox listBox_AllInstalations;
        private UserControls.InstallationUC installationUC1;
        private AbilitiesListUC abilitiesListUC1;
        private System.Windows.Forms.Button button_clearSelection;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button button_updateExsisting;
        private System.Windows.Forms.Button button_saveNew;
    }
}
