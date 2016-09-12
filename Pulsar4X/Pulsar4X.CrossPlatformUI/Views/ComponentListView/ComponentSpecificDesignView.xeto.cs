using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentListView
{
    public class ComponentSpecificDesignView : Panel
    {
        StackLayout ComponentInstancesStack;
        public ComponentSpecificDesignView()
        {
            XamlReader.Load(this);
            DataContextChanged += ComponentInstancesView_DataContextChanged;
        }

        private void ComponentInstancesView_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is ComponentSpecificDesignVM)
            {
                ComponentSpecificDesignVM vm = (ComponentSpecificDesignVM)DataContext;
                ComponentInstancesStack.Items.Clear();
                foreach (var item in vm.Instances)
                {
                    ComponentSpecificInstanceView component = new ComponentSpecificInstanceView() { DataContext = item};
                    ComponentInstancesStack.Items.Add(component);                   
                }
            }
        }
    }
}
