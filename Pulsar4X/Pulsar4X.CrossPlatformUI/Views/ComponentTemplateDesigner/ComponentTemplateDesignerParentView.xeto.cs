using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class ComponentTemplateDesignerParentView : Panel
    {
        private ComponentTemplateParentVM _viewModel;
        protected ComboBox ComponentsComBox { get; set; }
        protected StackLayout AbilityTemplates { get; set; }
        protected ComponentTemplateMainPropertiesView MainPropertiesCtrl { get; set; }
        protected FormulaEditorView FormulaEditorCtrl { get; set; }
        protected Panel PaddingPnl { get; set; }
        public ComponentTemplateDesignerParentView()
        {
            XamlReader.Load(this);

            ComponentsComBox.BindDataContext(c => c.DataStore, (DictionaryVM<object, string, string> m) => m.DisplayList);
            ComponentsComBox.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string, string> m) => m.SelectedIndex);
        }
        public ComponentTemplateDesignerParentView(ComponentTemplateParentVM viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;

            foreach (var item in viewModel.ComponentAbilitySDs)
            {
                AbilityTemplates.Items.Add(new ComponentTemplateAbilityPropertiesView(item));
            }
            viewModel.ComponentAbilitySDs.CollectionChanged += ComponentAbilitySDs_CollectionChanged;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;

        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedComponent")
                MainPropertiesCtrl.SetViewModel(_viewModel.SelectedComponent);
        }

        private void ComponentAbilitySDs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AbilityTemplates.SuspendLayout();
            AbilityTemplates.Items.Clear();
            foreach (var item in _viewModel.ComponentAbilitySDs)
            {
                AbilityTemplates.Items.Add(new ComponentTemplateAbilityPropertiesView(item));
            }            
            //padding to fix a bug with eto scrollable not scrolling down far enough. 
            //can be removed when the next version of eto.forms comes out as of this writing we're using 2.2 (it's fixed in the dev version of eto.forms)
            PaddingPnl.Height = AbilityTemplates.Items.Count * 32;

            AbilityTemplates.ResumeLayout();
        }

    }
}
