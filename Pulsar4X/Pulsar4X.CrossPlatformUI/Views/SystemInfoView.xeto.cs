using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SystemInfoView : Panel
    {
        private SystemInfoVM _vm;
        protected DropDown SystemSelection;
        protected DropDown EntitySelection;
        protected DropDown BlobSelection;
        protected TreeHierachView Entitiestree;
        public SystemInfoView()
        {
            XamlReader.Load(this);
            SystemSelection.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            SystemSelection.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            EntitySelection.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            EntitySelection.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            BlobSelection.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            BlobSelection.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            DataContextChanged += SystemInfoView_DataContextChanged;      
        }

        private void SystemInfoView_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is SystemInfoVM)
            {
                _vm = (SystemInfoVM)DataContext;
                _vm.PropertyChanged += _vm_PropertyChanged;
            }
        }

        private void _vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemInfoVM.EBTreePair))
                Entitiestree.DataContext = _vm.EBTreePair;

        }
    }
}
