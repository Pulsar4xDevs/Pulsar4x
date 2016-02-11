using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System.Linq;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ComponentAbilityTemplateDesignerView : Panel
    {
        protected ComboBox GuiHint { get; set; }
        protected StackLayout GuiHintControls { get; set; }
        public ComponentAbilityTemplateDesignerView()
        {
            XamlReader.Load(this);
        }

        public ComponentAbilityTemplateDesignerView(ComponentAbilityTemplateVM viewModel)
        {
            DataContext = viewModel;
            GuiHint.DataStore = Enum.GetValues(typeof(ECSLib.GuiHint)).Cast<object>();
            GuiHint.ItemTextBinding = Binding.Property((ECSLib.GuiHint n) => Enum.GetName(typeof(ECSLib.GuiHint), n));
            
            GuiHint.SelectedIndexChanged += GuiHint_SelectedIndexChanged;
        }

        private void GuiHint_SelectedIndexChanged(object sender, EventArgs e)
        {
            ECSLib.GuiHint selected = (ECSLib.GuiHint)GuiHint.SelectedValue;

            switch (selected)
            {
                case ECSLib.GuiHint.GuiSelectionMaxMin:
                    GuiHintMinMax();
                    break;
                case ECSLib.GuiHint.GuiTechSelectionList:
                    break;
                case ECSLib.GuiHint.GuiTextDisplay:
                    break;
                case ECSLib.GuiHint.None:
                    break;

            }
        }

        private void GuiHintMinMax()
        {
            GuiHintControls.Items.Clear();
            TextBox minFormula = new TextBox();
            minFormula.TextBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.MinFormula);
            TextBox maxFormula = new TextBox();
            minFormula.TextBinding.BindDataContext((ComponentAbilityTemplateVM n) => n.MaxFormula);
            GuiHintControls.Items.Add(minFormula);
            GuiHintControls.Items.Add(maxFormula);
        }

        private void GuiTechSelectionList()
        {

        }

        private void GuiTextDisplay()
        {
            GuiHintControls.Items.Clear();
        }
    }
}
