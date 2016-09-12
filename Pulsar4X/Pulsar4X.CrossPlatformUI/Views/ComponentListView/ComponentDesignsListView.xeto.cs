using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views.ComponentListView
{
    public class ComponentDesignsListView : Scrollable
    {
        protected StackLayout ComponentDesignsStack;

        public ComponentDesignsListView()
        {
            XamlReader.Load(this);
            DataContextChanged += ComponentsByDesignView_DataContextChanged;
        }

        private void ComponentsByDesignView_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is ComponentDesignsListVM)
            {
                ComponentDesignsListVM vm = (ComponentDesignsListVM)DataContext;
                ComponentDesignsStack.Items.Clear();
                foreach (var item in vm.Designs)
                {
                    ComponentSpecificDesignView component = new ComponentSpecificDesignView() { DataContext = item };
                    ComponentDesignsStack.Items.Add(component);
                }
                vm.Designs.CollectionChanged += Designs_CollectionChanged;
            }
        }

        private void Designs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ComponentsByDesignView_DataContextChanged(null, null);
        }
    }
}
