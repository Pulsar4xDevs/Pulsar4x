using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class MissileDesignView : Panel
    {
        ChainedSlidersControl ChainedSlidersCtrl { get; set; }

        private MissileDesignView()
        {
            XamlReader.Load(this);
        }

        public MissileDesignView(MissileDesignVM viewmodel) :this()
        {
            ChainedSlidersCtrl.SetViewModel(viewmodel.ChainedSliders);
            DataContext = viewmodel;
        }
    }
}
