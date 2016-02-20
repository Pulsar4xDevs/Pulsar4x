using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;

using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentTemplateDesigner
{
    public class TechList : Panel
    {
        protected ListBox SelectedItems { get; set; }
        protected ComboBox PossibleItems { get; set; }

        public TechList()
        {
            XamlReader.Load(this);
            PossibleItems.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string, string> m) => m.DisplayList);
            PossibleItems.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string, string> m) => m.SelectedIndex);

            SelectedItems.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string, string> m) => m.DisplayList);
            SelectedItems.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string, string> m) => m.SelectedIndex);
        }

    }
}
