using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;


namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipDesignView : Panel
    {
        protected StackLayout ComponentListStack { get; set; }

        public ShipDesignView()
        {
            XamlReader.Load(this);
        }

        public ShipDesignView(ShipDesignVM viewModel) :this()
        {
            ComponentListStack.Items.Add(new ComponentsListView(viewModel.ComponentsDesignedLists));
        }
    }
}
