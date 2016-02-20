using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System;
using System.Linq;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
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
            GuiHint_SelectedIndexChanged(this, null);
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

            TableLayout tl1 = new TableLayout(2, 2);

            Label lblMin = new Label();
            lblMin.Text = "MinFormula";
            TextBox minFormula = new TextBox();
            minFormula.TextBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.MinFormula);
            minFormula.DataContext = _viewModel;

            Label lblMax = new Label();
            lblMax.Text = "MaxFormula:";
            TextBox maxFormula = new TextBox();
            maxFormula.TextBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.MaxFormula);
            maxFormula.DataContext = _viewModel;
            tl1.Add(lblMin, 0, 0);
            tl1.Add(minFormula, 1, 0);
            tl1.Add(lblMax, 0, 1);
            tl1.Add(maxFormula, 1, 1);
            GuiHintControls.Items.Add(tl1);
            //GuiHintControls.Items.Add(minFormula);
            //GuiHintControls.Items.Add(maxFormula);
        }

        private void GuiTechSelectionList()
        {
            GuiHintControls.Items.Clear();
            TextBox tb = new TextBox();
            
            TechList techList = new TechList();
            techList.DataContext = _viewModel.GuidDict;
            //techList.BindDataContext(c => c.DataStore, (ComponentAbilityTemplateVM n) => n.AbilityDataBlobTypeSelection.DisplayList);
            //techList.SelectedValueBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.AbilityDataBlobType);

            GuiHintControls.Items.Add(techList);

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
            label.Text = "Does Not display anything, however, \r\nAbilityFormula still works 'under the hood'";
            GuiHintControls.Items.Add(label);

        }
    }
}
