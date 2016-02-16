using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class MineralFormulaView : Panel
    {
        private MineralFormulaVM _viewModel;
        protected ComboBox SelectionComboBox { get; set; }
        public MineralFormulaView()
        {
            XamlReader.Load(this);
        }
        public MineralFormulaView(MineralFormulaVM viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            SelectionComboBox.DataContext = viewModel.Minerals;
            SelectionComboBox.BindDataContext(c => c.DataStore, (DictionaryVM<Guid,string> m) => m.DisplayList);
            SelectionComboBox.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid,string> m) => m.SelectedIndex);
            SelectionComboBox.SelectedIndexChanged += _viewModel.OnSelectionChange;
        }

        //private void SelectionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    _viewModel.SetSelectedMineral(SelectionComboBox.SelectedIndex);
        //}
    }
}
