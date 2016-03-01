using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class MineralFormulaView : Panel
    {
        private MineralFormulaVM _viewModel;
        protected ComboBox SelectionComboBox { get; set; }
        protected TextBox FormulaTBx { get; set; }
        public MineralFormulaView()
        {
            XamlReader.Load(this);

            SelectionComboBox.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string, string> m) => m.DisplayList);
            SelectionComboBox.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string, string> m) => m.SelectedIndex);

            FormulaTBx.GotFocus += (sender, e) => ((ComponentTemplateDesignerBaseVM)DataContext).SubControlInFocus = FocusedControl.AbilityFormulaControl;

        }
        public MineralFormulaView(MineralFormulaVM viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            SelectionComboBox.DataContext = viewModel.Minerals;

            SelectionComboBox.SelectedIndexChanged += _viewModel.OnSelectionChange;
        }
    }
}
