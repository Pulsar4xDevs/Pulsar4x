using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
            ComponentsComBox.BindDataContext(c => c.DataStore, (DictionaryVM<object, string, string> m) => m.DisplayList);
            ComponentsComBox.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string, string> m) => m.SelectedIndex);

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
                //KeyValuePair<ECSLib.ComponentMountType, bool?> kvp = item;
                ECSLib.ComponentMountType key = item.Key;
                CheckBox chkbx = new CheckBox();
                chkbx.Text = key.ToString();
                //chkbx.CheckedBinding.BindDataContext((DictionaryVM<ECSLib.ComponentMountType, bool?, bool?> x) => x[kvp.Key], (m, val) => m[kvp.Key] = val);
                chkbx.CheckedBinding.BindDataContext((ObservableDictionary<ECSLib.ComponentMountType, bool?> x) => x[key], (m, val) => m[key] = val);
                chkbx.DataContext = _viewModel.MountType;
                MountTypes.Items.Add(chkbx);
            }
            _viewModel.MountType.PropertyChanged += MountType_PropertyChanged;
            //for (int i = 0; i < _viewModel.MountType.Count ; i++)
            //{
            //    //ItemPair<ECSLib.ComponentMountType, bool?> ipr = item;
            //    int idx = i;
            //    CheckBox chkbx = new CheckBox();
            //    chkbx.Text = _viewModel.MountType[idx].Item1.ToString();//ipr.Item1.ToString();
            //    chkbx.CheckedBinding.BindDataContext((ItemPair<ECSLib.ComponentMountType, bool?> x) => x.Item2, (m, val) => m.Item2 = val);
            //    chkbx.DataContext = _viewModel.MountType[idx];
            //    MountTypes.Items.Add(chkbx);
            //}
            //foreach (var item in _viewModel.MountType)
            //{
            //    ItemPair<ECSLib.ComponentMountType, bool?> ipr = item;
            //    CheckBox chkbx = new CheckBox();
            //    chkbx.Text = ipr.Item1.ToString();
            //    chkbx.CheckedBinding.BindDataContext((ItemPair<ECSLib.ComponentMountType, bool?> x) => x.Item2, (m, val) => m.Item2 = val);
            //    chkbx.DataContext = ipr; //_viewModel.MountType;
            //    MountTypes.Items.Add(chkbx);
            //}
        }

        private void MountType_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            foreach (var item in MountTypes.Items)
            {
                item.Control.UpdateBindings(BindingUpdateMode.Destination);
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
