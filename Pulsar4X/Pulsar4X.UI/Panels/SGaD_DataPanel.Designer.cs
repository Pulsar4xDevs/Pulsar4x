namespace Pulsar4X.UI.Panels
{
    partial class SGaD_DataPanel
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.m_oStarDataGridView = new System.Windows.Forms.DataGridView();
            this.m_oPlanetsDataGridView = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_oStarDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_oPlanetsDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.m_oStarDataGridView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.m_oPlanetsDataGridView);
            this.splitContainer1.Size = new System.Drawing.Size(284, 262);
            this.splitContainer1.SplitterDistance = 94;
            this.splitContainer1.TabIndex = 0;
            // 
            // m_oStarDataGridView
            // 
            this.m_oStarDataGridView.AllowUserToOrderColumns = true;
            this.m_oStarDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.m_oStarDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_oStarDataGridView.Location = new System.Drawing.Point(0, 0);
            this.m_oStarDataGridView.Name = "m_oStarDataGridView";
            this.m_oStarDataGridView.Size = new System.Drawing.Size(284, 94);
            this.m_oStarDataGridView.TabIndex = 0;
            // 
            // m_oPlanetsDataGridView
            // 
            this.m_oPlanetsDataGridView.AllowUserToOrderColumns = true;
            this.m_oPlanetsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.m_oPlanetsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_oPlanetsDataGridView.Location = new System.Drawing.Point(0, 0);
            this.m_oPlanetsDataGridView.Name = "m_oPlanetsDataGridView";
            this.m_oPlanetsDataGridView.Size = new System.Drawing.Size(284, 164);
            this.m_oPlanetsDataGridView.TabIndex = 0;
            // 
            // SGaD_DataPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.splitContainer1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HideOnClose = true;
            this.Name = "SGaD_DataPanel";
            this.TabText = "System Gen Data View";
            this.Text = "System Gen Data View";
            this.ToolTipText = "System Generation Data View";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.m_oStarDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_oPlanetsDataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView m_oStarDataGridView;
        private System.Windows.Forms.DataGridView m_oPlanetsDataGridView;
    }
}
