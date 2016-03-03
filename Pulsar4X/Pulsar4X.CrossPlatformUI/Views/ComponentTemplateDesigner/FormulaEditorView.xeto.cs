using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class FormulaEditorView : Panel
    {
        protected StackLayout ParameterButtonsStackLayout { get; set; }
        protected ComboBox TechSelection { get; set; }

        public FormulaEditorView()
        {
            XamlReader.Load(this);
            DataContextChanged += FormulaEditorView_DataContextChanged;

            TechSelection.BindDataContext(c => c.DataStore, (DictionaryVM<object, string, string> m) => m.DisplayList);
            TechSelection.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string, string> m) => m.SelectedIndex);
        }


        private void FormulaEditorView_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is FormulaEditorVM)
            {
                FormulaEditorVM dc = (FormulaEditorVM)DataContext;
                SetViewModel(dc);
            }
        }

        private void SetViewModel(FormulaEditorVM viewModel)
        {
            ParameterButtonsStackLayout.Orientation = Orientation.Horizontal;
            foreach (var item in viewModel.ParameterButtons)
            {
                Button button = new Button();
                button.BindDataContext(c => c.Command, (ButtonInfo m) => m.AddCommand);
                button.BindDataContext(c => c.ToolTip, (ButtonInfo m) => m.ToolTipText);
                button.BindDataContext(c => c.Text, (ButtonInfo m) => m.Text);
                button.DataContext = item;
                ParameterButtonsStackLayout.Items.Add(button);
            }

        }
    }
}
