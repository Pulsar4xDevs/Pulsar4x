using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using OpenTK;

namespace Pulsar4X.UI.Panels
{
    partial class MissileDesign : DockContent
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
            this.m_oEmpireGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oCompSizeBox = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.groupBox9 = new System.Windows.Forms.GroupBox();
            this.groupBox10 = new System.Windows.Forms.GroupBox();
            this.groupBox11 = new System.Windows.Forms.GroupBox();
            this.groupBox12 = new System.Windows.Forms.GroupBox();
            this.m_oEmpireComboBox = new System.Windows.Forms.ComboBox();
            this.m_oEmpireGroupBox.SuspendLayout();
            this.m_oCompSizeBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oEmpireGroupBox
            // 
            this.m_oEmpireGroupBox.Controls.Add(this.m_oEmpireComboBox);
            this.m_oEmpireGroupBox.Location = new System.Drawing.Point(12, 12);
            this.m_oEmpireGroupBox.Name = "m_oEmpireGroupBox";
            this.m_oEmpireGroupBox.Size = new System.Drawing.Size(277, 50);
            this.m_oEmpireGroupBox.TabIndex = 0;
            this.m_oEmpireGroupBox.TabStop = false;
            this.m_oEmpireGroupBox.Text = "Empire";
            // 
            // m_oCompSizeBox
            // 
            this.m_oCompSizeBox.Controls.Add(this.groupBox9);
            this.m_oCompSizeBox.Controls.Add(this.groupBox10);
            this.m_oCompSizeBox.Controls.Add(this.groupBox11);
            this.m_oCompSizeBox.Controls.Add(this.groupBox12);
            this.m_oCompSizeBox.Location = new System.Drawing.Point(12, 68);
            this.m_oCompSizeBox.Name = "m_oCompSizeBox";
            this.m_oCompSizeBox.Size = new System.Drawing.Size(277, 491);
            this.m_oCompSizeBox.TabIndex = 1;
            this.m_oCompSizeBox.TabStop = false;
            this.m_oCompSizeBox.Text = "Enter Component Sizes";
            // 
            // groupBox3
            // 
            this.groupBox3.Location = new System.Drawing.Point(12, 572);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(277, 140);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "groupBox3";
            // 
            // groupBox4
            // 
            this.groupBox4.Location = new System.Drawing.Point(295, 12);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(319, 50);
            this.groupBox4.TabIndex = 2;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "groupBox4";
            // 
            // groupBox5
            // 
            this.groupBox5.Location = new System.Drawing.Point(295, 68);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(319, 109);
            this.groupBox5.TabIndex = 2;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "groupBox5";
            // 
            // groupBox6
            // 
            this.groupBox6.Location = new System.Drawing.Point(295, 183);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(319, 209);
            this.groupBox6.TabIndex = 2;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "groupBox6";
            // 
            // groupBox7
            // 
            this.groupBox7.Location = new System.Drawing.Point(295, 398);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(668, 314);
            this.groupBox7.TabIndex = 2;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "groupBox7";
            // 
            // groupBox8
            // 
            this.groupBox8.Location = new System.Drawing.Point(620, 68);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(355, 324);
            this.groupBox8.TabIndex = 2;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "groupBox8";
            // 
            // groupBox9
            // 
            this.groupBox9.Location = new System.Drawing.Point(8, 56);
            this.groupBox9.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Size = new System.Drawing.Size(261, 127);
            this.groupBox9.TabIndex = 2;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "groupBox9";
            // 
            // groupBox10
            // 
            this.groupBox10.Location = new System.Drawing.Point(8, 193);
            this.groupBox10.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox10.Name = "groupBox10";
            this.groupBox10.Size = new System.Drawing.Size(261, 127);
            this.groupBox10.TabIndex = 2;
            this.groupBox10.TabStop = false;
            this.groupBox10.Text = "groupBox10";
            // 
            // groupBox11
            // 
            this.groupBox11.Location = new System.Drawing.Point(8, 330);
            this.groupBox11.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox11.Name = "groupBox11";
            this.groupBox11.Size = new System.Drawing.Size(261, 100);
            this.groupBox11.TabIndex = 2;
            this.groupBox11.TabStop = false;
            this.groupBox11.Text = "groupBox11";
            // 
            // groupBox12
            // 
            this.groupBox12.Location = new System.Drawing.Point(8, 433);
            this.groupBox12.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox12.Name = "groupBox12";
            this.groupBox12.Size = new System.Drawing.Size(261, 50);
            this.groupBox12.TabIndex = 2;
            this.groupBox12.TabStop = false;
            this.groupBox12.Text = "groupBox12";
            // 
            // m_oEmpireComboBox
            // 
            this.m_oEmpireComboBox.FormattingEnabled = true;
            this.m_oEmpireComboBox.Location = new System.Drawing.Point(13, 18);
            this.m_oEmpireComboBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oEmpireComboBox.Name = "m_oEmpireComboBox";
            this.m_oEmpireComboBox.Size = new System.Drawing.Size(251, 21);
            this.m_oEmpireComboBox.TabIndex = 0;
            // 
            // MissileDesign
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(975, 759);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox7);
            this.Controls.Add(this.groupBox8);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.m_oCompSizeBox);
            this.Controls.Add(this.m_oEmpireGroupBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "MissileDesign";
            this.Text = "MissileDesign";
            this.m_oEmpireGroupBox.ResumeLayout(false);
            this.m_oCompSizeBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private GroupBox m_oEmpireGroupBox;
        private GroupBox m_oCompSizeBox;
        private GroupBox groupBox9;
        private GroupBox groupBox10;
        private GroupBox groupBox11;
        private GroupBox groupBox12;
        private GroupBox groupBox3;
        private GroupBox groupBox4;
        private GroupBox groupBox5;
        private GroupBox groupBox6;
        private GroupBox groupBox7;
        private GroupBox groupBox8;
        private ComboBox m_oEmpireComboBox;
    }
}