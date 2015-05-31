namespace ModdingTools.JsonDataEditor
{
    partial class MineralsCostsUC
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.listBox_MineralsAll = new System.Windows.Forms.ListBox();
            this.dataGridView_MineralCosts = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_MineralCosts)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.listBox_MineralsAll, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.dataGridView_MineralCosts, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(191, 150);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 3);
            this.label1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Mineral Costs:";
            // 
            // listBox_MineralsAll
            // 
            this.listBox_MineralsAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_MineralsAll.FormattingEnabled = true;
            this.listBox_MineralsAll.Location = new System.Drawing.Point(98, 3);
            this.listBox_MineralsAll.Name = "listBox_MineralsAll";
            this.tableLayoutPanel1.SetRowSpan(this.listBox_MineralsAll, 2);
            this.listBox_MineralsAll.Size = new System.Drawing.Size(90, 144);
            this.listBox_MineralsAll.TabIndex = 2;
            this.listBox_MineralsAll.SelectedIndexChanged += new System.EventHandler(this.listBox_MineralsAll_SelectedIndexChanged);
            this.listBox_MineralsAll.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listBox_MineralsAll_MouseDoubleClick);
            // 
            // dataGridView_MineralCosts
            // 
            this.dataGridView_MineralCosts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_MineralCosts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView_MineralCosts.Location = new System.Drawing.Point(3, 23);
            this.dataGridView_MineralCosts.Name = "dataGridView_MineralCosts";
            this.dataGridView_MineralCosts.RowHeadersVisible = false;
            this.dataGridView_MineralCosts.Size = new System.Drawing.Size(89, 124);
            this.dataGridView_MineralCosts.TabIndex = 0;
            this.dataGridView_MineralCosts.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_MineralCosts_CellMouseClick);
            // 
            // MineralsCostsUC
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "MineralsCostsUC";
            this.Size = new System.Drawing.Size(191, 150);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_MineralCosts)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.DataGridView dataGridView_MineralCosts;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.ListBox listBox_MineralsAll;
    }
}
