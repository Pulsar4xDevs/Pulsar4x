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

        public EnumFormulaView()
        {
            XamlReader.Load(this);

            EnumComBox.BindDataContext(c => c.DataStore, (DictionaryVM<object, string, string> m) => m.DisplayList);
            EnumComBox.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string, string> m) => m.SelectedIndex);
            EnumComBox.DataContext = DataContext;
        }
    }
}
