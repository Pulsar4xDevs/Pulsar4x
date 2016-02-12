using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System.Collections.Generic;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ComponentTemplateDesignerView : Panel
    {
        protected StackLayout MineralCostFormulaStackLayout { get; set; }
        protected StackLayout AbilityTemplates { get; set; }
        protected StackLayout MountTypes { get; set; }
        private ComponentTemplateVM _viewModel;
        public ComponentTemplateDesignerView()
        {
            XamlReader.Load(this);
        }

        public ComponentTemplateDesignerView(ComponentTemplateVM viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;

            foreach (var item in viewModel.ComponentAbilitySDs)
            {
                AbilityTemplates.Items.Add(new ComponentAbilityTemplateDesignerView(item));
            }
            foreach (var item in viewModel.MineralCostFormula)
            {
                MineralCostFormulaStackLayout.Items.Add(new MineralFormulaView(item));
            }
            viewModel.MineralCostFormula.CollectionChanged += MineralCostFormula_CollectionChanged;

            foreach (var item in _viewModel.MountType)
            {
                KeyValuePair<ECSLib.ComponentMountType, bool?> kvp = item;
                CheckBox chkbx = new CheckBox();
                chkbx.Text = item.Key.ToString();
                chkbx.CheckedBinding.BindDataContext((DictionaryVM<ECSLib.ComponentMountType, bool?> x) => x.GetValue(_viewModel.MountType.GetIndex(item)));
                chkbx.DataContext = _viewModel.MountType;
                //chkbx.Checked = item.Value;
                MountTypes.Items.Add(chkbx);
            }
        }

        private void MineralCostFormula_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MineralCostFormulaStackLayout.Items.Clear();
            foreach (var item in _viewModel.MineralCostFormula)
            {
                MineralCostFormulaStackLayout.Items.Add(new MineralFormulaView(item));
            }
        }
    }
}
