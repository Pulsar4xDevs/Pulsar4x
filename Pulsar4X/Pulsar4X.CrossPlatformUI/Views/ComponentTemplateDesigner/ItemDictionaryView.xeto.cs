using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using System.Collections.ObjectModel;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class ItemDictionaryView : Panel
    {
        protected StackLayout ControlStack { get; set; }
        protected ComboBox TypesComBox { get; set; }
        public ItemDictionaryView()
        {
            XamlReader.Load(this);
            TypesComBox.BindDataContext(c => c.DataStore, (DictionaryVM<object, string, string> m) => m.DisplayList);
            TypesComBox.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string, string> m) => m.SelectedIndex);
            
        }

        public void SetViewmodel(ComponentAbilityTemplateVM vm)
        {
            DataContext = vm;
            TypesComBox.DataContext = vm.ItemDictTypes;
            foreach (var item in vm.ItemDict.Items)
            {
                EnumFormulaView efv = new EnumFormulaView();
                efv.DataContext = item;
                ControlStack.Items.Add(efv);
            }
        }
    }

}
