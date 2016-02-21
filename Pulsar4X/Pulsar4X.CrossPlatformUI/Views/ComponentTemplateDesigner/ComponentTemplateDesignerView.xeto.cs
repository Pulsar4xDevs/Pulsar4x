using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class ComponentTemplateDesignerView : Panel
    {
        protected ComboBox ComponentsComBox { get; set; }
        protected StackLayout MineralCostFormulaStackLayout { get; set; }
        protected StackLayout AbilityTemplates { get; set; }
        protected StackLayout MountTypes { get; set; }
        protected Button Save { get; set; }
        protected Button Export { get; set; }
        private ComponentTemplateVM _viewModel;
        public ComponentTemplateDesignerView()
        {
            XamlReader.Load(this);
        }

        public ComponentTemplateDesignerView(ComponentTemplateVM viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
            Save.Click += Save_Click;
            Export.Click += Export_Click;
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

        private void Export_Click(object sender, System.EventArgs e)
        {
            _viewModel.SaveToFile();
        }

        private void Save_Click(object sender, System.EventArgs e)
        {
            _viewModel.CreateSD();
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
            AbilityTemplates.SuspendLayout();
            AbilityTemplates.Items.Clear();
            foreach (var item in _viewModel.ComponentAbilitySDs)
            {
                AbilityTemplates.Items.Add(new ComponentAbilityTemplateDesignerView(item));
            }
            //padding to fix a bug with eto scrollable not scrolling down far enough. 
            //can be removed when the next version of eto.forms comes out as of this writing we're using 2.2 (it's fixed in the dev version of eto.forms)
            AbilityTemplates.Items.Add(new Label());
            AbilityTemplates.Items.Add(new Label());

            AbilityTemplates.ResumeLayout();
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
