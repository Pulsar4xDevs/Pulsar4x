using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;


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
