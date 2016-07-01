using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipView : Panel
    {
        public ShipView()
        {
            XamlReader.Load(this);
        }

        public ShipView(ShipOrderVM viewModel) :this()
        {

        }
    }
}
