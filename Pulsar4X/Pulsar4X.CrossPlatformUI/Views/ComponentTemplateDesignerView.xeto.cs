using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System.Collections.Generic;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ComponentTemplateDesignerView : Panel
    {
        protected ComboBox ComponentsComBox { get; set; }
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

            ComponentsComBox.DataContext = viewModel.Components;
            ComponentsComBox.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            ComponentsComBox.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            foreach (var item in viewModel.ComponentAbilitySDs)
            {
                AbilityTemplates.Items.Add(new ComponentAbilityTemplateDesignerView(item));
            }
            viewModel.ComponentAbilitySDs.CollectionChanged += ComponentAbilitySDs_CollectionChanged;
            foreach (var item in viewModel.MineralCostFormula)
            {
                MineralCostFormulaStackLayout.Items.Add(new MineralFormulaView(item));
            }
            viewModel.MineralCostFormula.CollectionChanged += MineralCostFormula_CollectionChanged;

            foreach (var item in _viewModel.MountType)
            {
                ECSLib.ComponentMountType key = item.Key;
                CheckBox chkbx = new CheckBox();
                chkbx.Text = key.ToString();
                chkbx.CheckedBinding.BindDataContext((DictionaryVM<ECSLib.ComponentMountType, bool?> x) => x[key] , (m, val) => m[key] = val);
                chkbx.DataContext = _viewModel.MountType;
                MountTypes.Items.Add(chkbx);
            }
        }

        private void ComponentAbilitySDs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AbilityTemplates.Items.Clear();
            foreach (var item in _viewModel.ComponentAbilitySDs)
            {
                AbilityTemplates.Items.Add(new ComponentAbilityTemplateDesignerView(item));
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
