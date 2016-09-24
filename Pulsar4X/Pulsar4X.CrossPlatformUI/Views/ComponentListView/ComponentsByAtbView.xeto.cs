using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentListView
{
    public class ComponentsByAtbView : Scrollable
    {
        public ComponentsByAtbView()
        {
            XamlReader.Load(this);
            DataContextChanged += ComponentsByAtbView_DataContextChanged;
        }

        private void ComponentsByAtbView_DataContextChanged(object sender, EventArgs e)
        {
            //if(DataContext is )
        }
    }
}
