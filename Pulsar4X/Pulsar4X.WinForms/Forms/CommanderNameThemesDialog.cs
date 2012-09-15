using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.Entities;
using Pulsar4X.ViewModels;

namespace Pulsar4X.Forms
{
    public partial class CommanderNameThemesDialog : Form
    {
        public CommanderNameThemesViewModel ViewModel { get; set; }
        public CommanderNameThemesDialog()
        {
            InitializeComponent();

            ViewModel = new CommanderNameThemesViewModel();

            //setup combo box
            cmbNameThemes.DataSource = ViewModel.NameThemes;
            cmbNameThemes.Bind(c => c.SelectedItem, ViewModel, d => d.CurrentTheme, DataSourceUpdateMode.OnPropertyChanged);
            cmbNameThemes.DisplayMember = "Name";

            cmbNameThemes.SelectedIndexChanged += (sender, args) =>
                                                      {
                                                          if (cmbNameThemes == null) return;
                                                          var dataBinding = cmbNameThemes.DataBindings["SelectedItem"];
                                                          if (dataBinding != null)
                                                              dataBinding.WriteValue();
                                                      };

            //setup name field
            txtNameOfTheme.Bind(c => c.Text, ViewModel, d => d.CurrentThemeName);

            //setup datagrid
            dgvNameEntries.AllowUserToAddRows = true;
            dgvNameEntries.AllowUserToDeleteRows = true;
            dgvNameEntries.AllowUserToOrderColumns = false;
            dgvNameEntries.AllowUserToResizeColumns = false;
            dgvNameEntries.ReadOnly = false;
            dgvNameEntries.AutoGenerateColumns = false;

            SetupDataGridViewColumns();

            dgvNameEntries.DataSource = ViewModel.NameEntryBindingSource;
        }

        private void SetupDataGridViewColumns()
        {
            using (var col = new DataGridViewTextBoxColumn())
            {
                col.DataPropertyName = "Name";
                col.HeaderText = "Name";
                dgvNameEntries.Columns.Add(col);
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            using (var col = new DataGridViewComboBoxColumn())
            {
                col.DataPropertyName = "NamePosition";
                col.HeaderText = "Name Position";
                col.DataSource = Enum.GetValues(typeof (NamePosition));
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgvNameEntries.Columns.Add(col);
            }
            using (var col = new DataGridViewCheckBoxColumn())
            {
                col.DataPropertyName = "IsFemale";
                col.HeaderText = "Is Female";
                col.FalseValue = false;
                col.TrueValue = true;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
                dgvNameEntries.Columns.Add(col);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var name = txtNameOfTheme.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Name required, cannot create un-named Theme.");
                return;
            }

            var theme = ViewModel.AddNewTheme(name);
            cmbNameThemes.SelectedItem = theme;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var name = txtNameOfTheme.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Name required, cannot save empty Name.");
                return;
            }

            ViewModel.SaveTheme();
        }
    }
}
