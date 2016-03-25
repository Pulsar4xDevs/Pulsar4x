using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class ComponentTemplateAbilityPropertiesView : Panel
    {

        protected GroupBox GrpBx { get; set; }
        protected ComboBox GuiHint { get; set; }
        protected StackLayout GuiHintControls { get; set; }

        protected TextBox NameTBx { get; set; }
        protected TextBox DescriptionTBx { get; set; }
        protected TextBox AbilityFormulaTBx { get; set; }

        protected ContextMenu ContextMenuItem = new ContextMenu();
        private List<ButtonMenuItem> ContextMenuButtons = new List<ButtonMenuItem>();

        private ComponentAbilityTemplateVM _viewModel;

        public ComponentTemplateAbilityPropertiesView()
        {
            XamlReader.Load(this);
            GuiHint.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string> m) => m.DisplayList);
            GuiHint.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string> m) => m.SelectedIndex);

            NameTBx.GotFocus += (sender, e) => ((ComponentTemplateDesignerBaseVM)DataContext).SubControlInFocus = FocusedControl.NameControl;
            DescriptionTBx.GotFocus += (sender, e) => ((ComponentTemplateDesignerBaseVM)DataContext).SubControlInFocus = FocusedControl.DescriptionControl;
            AbilityFormulaTBx.GotFocus += (sender, e) => ((ComponentTemplateDesignerBaseVM)DataContext).SubControlInFocus = FocusedControl.AbilityFormulaControl;


            GrpBx.MouseDown += (sender, e) => { if (e.Buttons == MouseButtons.Alternate) ContextMenuItem.Show(GrpBx); };
        }

        public ComponentTemplateAbilityPropertiesView(ComponentAbilityTemplateVM viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;

            _viewModel.SelectedGuiHint.PropertyChanged += GuiHint_SelectedIndexChanged;
            GuiHint_SelectedIndexChanged(this, null);

            ButtonMenuItem btnAdd = new ButtonMenuItem();
            btnAdd.Command = _viewModel.AddToEditCommand;
            btnAdd.Text = "Add To Edit Field";
            ButtonMenuItem btnDel = new ButtonMenuItem();
            btnDel.Text = "Delete This Ability Item";
            btnDel.Command = _viewModel.DeleteCommand;

            ContextMenuButtons.Add(btnAdd);
            ContextMenuButtons.Add(btnDel);
            ContextMenuItem = new ContextMenu(ContextMenuButtons);

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

            TableLayout tl1 = new TableLayout(2, 3);

            Label lblMin = new Label();
            lblMin.Text = "MinFormula";
            TextBox minFormula = new TextBox();
            minFormula.ToolTip = "Formula Field";
            minFormula.TextBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.MinFormula);
            minFormula.GotFocus += (sender, e) => ((ComponentTemplateDesignerBaseVM)DataContext).SubControlInFocus = FocusedControl.MinControl;            
            minFormula.DataContext = _viewModel;
            
            Label lblMax = new Label();
            lblMax.Text = "MaxFormula:";
            TextBox maxFormula = new TextBox();
            maxFormula.ToolTip = "Formula Field";
            maxFormula.TextBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.MaxFormula);
            maxFormula.GotFocus += (sender, e) => ((ComponentTemplateDesignerBaseVM)DataContext).SubControlInFocus = FocusedControl.MaxControl;
            maxFormula.DataContext = _viewModel;

            Label lblStep = new Label();
            lblStep.Text = "StepFormula:";
            TextBox stepFormula = new TextBox();
            stepFormula.ToolTip = "Formula Field";
            stepFormula.TextBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.StepFormula);
            stepFormula.GotFocus += (sender, e) => ((ComponentTemplateDesignerBaseVM)DataContext).SubControlInFocus = FocusedControl.StepControl;
            stepFormula.DataContext = _viewModel;

            tl1.Add(lblMin, 0, 0);
            tl1.Add(minFormula, 1, 0);
            tl1.Add(lblMax, 0, 1);
            tl1.Add(maxFormula, 1, 1);
            tl1.Add(lblStep, 0, 2);
            tl1.Add(stepFormula, 1, 2);
            GuiHintControls.Items.Add(tl1);
            
            
        }

        private void GuiTechSelectionList()
        {
            GuiHintControls.Items.Clear();
            
            TechList techList = new TechList();
            techList.DataContext = _viewModel.GuidDict;
            GuiHintControls.Items.Add(techList);

        }

        private void GuiHintTextDisplay()
        {
            //this just uses the exsisting Abilityformula
            GuiHintControls.Items.Clear();
            //Label label = new Label();
            //label.Text = "Displays AbilityFormula";
            //GuiHintControls.Items.Add(label);
        }

        private void GuiHintNone()
        {
            GuiHintControls.Items.Clear();
            //Label label = new Label();
            //label.Text = "Does Not display anything, however, \r\nAbilityFormula still works 'under the hood' \r\nOr use in conjunction with Datablob type and args";
            ComboBox dataBlobSelection = new ComboBox();
            dataBlobSelection.BindDataContext(c => c.DataStore, (ComponentAbilityTemplateVM m) => m.AbilityDataBlobTypeSelection);
            dataBlobSelection.SelectedValueBinding.BindDataContext((ComponentAbilityTemplateVM m) => m.AbilityDataBlobType);
            dataBlobSelection.DataContext = _viewModel;

            ItemDictionaryView idict = new ItemDictionaryView();
            idict.SetViewmodel(_viewModel);

            //GuiHintControls.Items.Add(label);
            GuiHintControls.Items.Add(dataBlobSelection);
            GuiHintControls.Items.Add(idict);
        }
    }
}
