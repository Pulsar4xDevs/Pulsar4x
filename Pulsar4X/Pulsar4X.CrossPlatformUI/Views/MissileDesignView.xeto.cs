using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class MissileDesignView : Panel
    {
        protected ChainedSlidersControl ChainedSlidersCtrl { get; set; }
        protected ComboBox PayloadTypeCombox { get; set; }

        private MissileDesignView()
        {
            XamlReader.Load(this);
            PayloadTypeCombox.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string> m) => m.DisplayList);
            PayloadTypeCombox.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string> m) => m.SelectedIndex);
        }

        public MissileDesignView(MissileDesignVM viewmodel) :this()
        {
            ChainedSlidersCtrl.SetViewModel(viewmodel.ChainedSliders);
            DataContext = viewmodel;
        }
    }
}
