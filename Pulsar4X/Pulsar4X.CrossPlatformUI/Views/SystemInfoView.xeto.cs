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
        protected DropDown SystemSelection;
        protected DropDown EntitySelection;
        protected DropDown BlobSelection;

        public SystemInfoView()
        {
            XamlReader.Load(this);
            SystemSelection.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            SystemSelection.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            EntitySelection.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            EntitySelection.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            BlobSelection.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            BlobSelection.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            
        }
    }
}
