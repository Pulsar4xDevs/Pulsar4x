using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class ComponentTemplateMainPropertiesView : Panel
    {

        protected StackLayout MineralCostFormulaStackLayout { get; set; }

        protected StackLayout MountTypes { get; set; }
        //private ComponentTemplateMainPropertiesVM _viewModel;
        
        protected TextBox NameTBx { get; set; }
        protected TextBox DescriptionTBx { get; set; }
        protected TextBox SizeFormulaTBx { get; set; }
        protected TextBox VolumeFormulaTBx { get; set; }
        protected TextBox HTKTBx { get; set; }
        protected TextBox CrewReqTBx { get; set; }
        protected TextBox ResearchCostTBx { get; set; }
        protected TextBox BuildPointTBx { get; set; }
        protected TextBox CreditCostTBx { get; set; }



        public ComponentTemplateMainPropertiesView()
        {
            XamlReader.Load(this);

            NameTBx.GotFocus += (sender, e) => ((ComponentTemplateMainPropertiesVM)DataContext).SubControlInFocus = FocusedControl.NameControl;
            DescriptionTBx.GotFocus += (sender, e) => ((ComponentTemplateMainPropertiesVM)DataContext).SubControlInFocus = FocusedControl.DescriptionControl;
            SizeFormulaTBx.GotFocus += (sender, e) => ((ComponentTemplateMainPropertiesVM)DataContext).SubControlInFocus = FocusedControl.MassControl;
            VolumeFormulaTBx.GotFocus += (sender, e) => ((ComponentTemplateMainPropertiesVM)DataContext).SubControlInFocus = FocusedControl.VolumeControl;
            HTKTBx.GotFocus += (sender, e) => ((ComponentTemplateDesignerBaseVM)DataContext).SubControlInFocus = FocusedControl.HTKControl;
            CrewReqTBx.GotFocus += (sender, e) => ((ComponentTemplateMainPropertiesVM)DataContext).SubControlInFocus = FocusedControl.CrewReqControl;
            ResearchCostTBx.GotFocus += (sender, e) => ((ComponentTemplateMainPropertiesVM)DataContext).SubControlInFocus = FocusedControl.ResearchCostControl;
            BuildPointTBx.GotFocus += (sender, e) => ((ComponentTemplateMainPropertiesVM)DataContext).SubControlInFocus = FocusedControl.BPCostControl;
            CreditCostTBx.GotFocus += (sender, e) => ((ComponentTemplateMainPropertiesVM)DataContext).SubControlInFocus = FocusedControl.CreditCostControl;

            DataContextChanged += ComponentTemplateMainPropertiesView_DataContextChanged;
        }

        private void ComponentTemplateMainPropertiesView_DataContextChanged(object sender, System.EventArgs e)
        {
            if (DataContext is ComponentTemplateMainPropertiesVM)
            {
                ComponentTemplateMainPropertiesVM dc = (ComponentTemplateMainPropertiesVM)DataContext;
                SetViewModel(dc);
            }
        }

        public void SetViewModel(ComponentTemplateMainPropertiesVM viewModel) 
        {
            //_viewModel = viewModel;
            //DataContext = viewModel;
            //FormulaEditorView.SetViewModel(_viewModel.FormulaEditor);
            MineralCostFormulaStackLayout.SuspendLayout();
            MineralCostFormulaStackLayout.Items.Clear();
            foreach (var item in viewModel.MineralCostFormula)
            {
                MineralCostFormulaStackLayout.Items.Add(new MineralFormulaView(item));
            }
            viewModel.MineralCostFormula.CollectionChanged += MineralCostFormula_CollectionChanged;
            MineralCostFormulaStackLayout.ResumeLayout();

            MountTypes.SuspendLayout();
            MountTypes.Items.Clear();
            foreach (var item in viewModel.MountType)
            {

                ECSLib.ComponentMountType key = item.Key;
                CheckBox chkbx = new CheckBox();
                chkbx.Text = key.ToString();
                //chkbx.CheckedBinding.BindDataContext((DictionaryVM<ECSLib.ComponentMountType, bool?, bool?> x) => x[kvp.Key], (m, val) => m[kvp.Key] = val);
                chkbx.CheckedBinding.BindDataContext((ObservableDictionary<ECSLib.ComponentMountType, bool?> x) => x[key], (m, val) => m[key] = val);
                chkbx.DataContext = viewModel.MountType;
                MountTypes.Items.Add(chkbx);
            }
            MountTypes.ResumeLayout();
            viewModel.MountType.PropertyChanged += MountType_PropertyChanged;

        }        

        private void MountType_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            foreach (var item in MountTypes.Items)
            {
                item.Control.UpdateBindings(BindingUpdateMode.Destination);
            }
        }

        private void MineralCostFormula_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ComponentTemplateMainPropertiesVM viewModel = (ComponentTemplateMainPropertiesVM)DataContext;
            MineralCostFormulaStackLayout.Items.Clear();
            foreach (var item in viewModel.MineralCostFormula)
            {
                MineralCostFormulaStackLayout.Items.Add(new MineralFormulaView(item));
            }
        }        
    }
}
