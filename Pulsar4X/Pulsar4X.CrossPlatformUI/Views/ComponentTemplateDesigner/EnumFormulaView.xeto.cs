using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class EnumFormulaView : Panel
    {
        protected ComboBox EnumComBox { get; set; }
        protected TextBox FormulaTBx { get; set; }
        public EnumFormulaView()
        {
            XamlReader.Load(this);

            EnumComBox.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            EnumComBox.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            FormulaTBx.GotFocus += (sender, e) => ((ComponentTemplateDesignerBaseVM)DataContext).SubControlInFocus = FocusedControl.AbilityFormulaControl;
        }

        public void SetDatacontext(ItemDictVM<object> _vm)
        {
            EnumComBox.DataContext = _vm.Items;
            DataContext = _vm;           
        }
    }
}
