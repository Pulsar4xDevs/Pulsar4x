using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using System.Collections.ObjectModel;
using Pulsar4X.ECSLib;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class ItemDictionaryView : Panel
    {
        private ComponentAbilityTemplateVM _vm;
        protected StackLayout ControlStack { get; set; }
        protected ComboBox TypesComBox { get; set; }
        public ItemDictionaryView()
        {
            XamlReader.Load(this);
            TypesComBox.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            TypesComBox.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            
        }

        public void SetViewmodel(ComponentAbilityTemplateVM vm)
        {
            DataContext = vm;
            _vm = vm;
            TypesComBox.DataContext = vm.ItemDictTypes;
            vm.ItemDictTypes.SelectionChangedEvent += ItemDictTypes_SelectionChangedEvent;
            vm.ItemDict.CollectionChanged += ItemDict_CollectionChanged;

        }

        private void ItemDict_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ItemDictTypes_SelectionChangedEvent(0, 0);
        }

        private void ItemDictTypes_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            ControlStack.SuspendLayout();
            ControlStack.Items.Clear();
            EnumFormulaView efv = new EnumFormulaView();
            foreach (var item in _vm.ItemDict)
            {
                efv = new EnumFormulaView();
                efv.SetDatacontext(item);
                ControlStack.Items.Add(efv);
            }
            ControlStack.ResumeLayout();

        }
    }

}
