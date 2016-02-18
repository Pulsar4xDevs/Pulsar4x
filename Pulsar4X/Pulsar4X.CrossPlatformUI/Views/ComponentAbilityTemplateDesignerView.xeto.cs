using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System;
using System.Linq;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ComponentAbilityTemplateDesignerView : Panel
    {
        protected ComboBox GuiHint { get; set; }
        protected StackLayout GuiHintControls { get; set; }

        private ComponentAbilityTemplateVM _viewModel;

        public ComponentAbilityTemplateDesignerView()
        {
            XamlReader.Load(this);
        }

        public ComponentAbilityTemplateDesignerView(ComponentAbilityTemplateVM viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            GuiHint.DataContext = viewModel.SelectedGuiHint;
            GuiHint.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string, string> m) => m.DisplayList);
            GuiHint.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string, string> m) => m.SelectedIndex);
            _viewModel.SelectedGuiHint.PropertyChanged += GuiHint_SelectedIndexChanged;
        }

        private void GuiHint_SelectedIndexChanged(object sender, EventArgs e)
        {

            switch (_viewModel.SelectedGuiHint.GetKey())
            {
                case ECSLib.GuiHint.GuiSelectionMaxMin:
                    GuiHintMinMax();
                    break;
                case ECSLib.GuiHint.GuiTechSelectionList:
                    GuiTechSelectionList();
                    break;
                case ECSLib.GuiHint.GuiTextDisplay:
                    GuiHintTextDisplay();
                    break;
                case ECSLib.GuiHint.None:
                    GuiHintNone();
                    break;

            }
        }

        private void GuiHintMinMax()
        {
            GuiHintControls.Items.Clear();
            TextBox minFormula = new TextBox();
            minFormula.TextBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.MinFormula);
            minFormula.DataContext = _viewModel;
            TextBox maxFormula = new TextBox();
            maxFormula.TextBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.MaxFormula);
            maxFormula.DataContext = _viewModel;
            GuiHintControls.Items.Add(minFormula);
            GuiHintControls.Items.Add(maxFormula);
        }

        private void GuiTechSelectionList()
        {
            GuiHintControls.Items.Clear();
            TextBox tb = new TextBox();
            
            ComboBox comboBox = new ComboBox();
            comboBox.BindDataContext(c => c.DataStore, (ComponentAbilityTemplateVM n) => n.AbilityDataBlobTypeSelection.DisplayList);
            comboBox.SelectedValueBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.AbilityDataBlobType);

            GuiHintControls.Items.Add(comboBox);

        }

        private void GuiHintTextDisplay()
        {
            //this just uses the exsisting Abilityformula
            GuiHintControls.Items.Clear();
            Label label = new Label();
            label.Text = "Displays AbilityFormula";
            GuiHintControls.Items.Add(label);
        }

        private void GuiHintNone()
        {
            GuiHintControls.Items.Clear();
            Label label = new Label();
            label.Text = "Does Not display anything, however AbilityFormula still works 'under the hood'";
            GuiHintControls.Items.Add(label);

        }
    }
}
