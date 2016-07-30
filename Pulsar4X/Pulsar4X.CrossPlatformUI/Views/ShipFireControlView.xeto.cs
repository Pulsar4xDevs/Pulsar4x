using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipFireControlView : Panel
    {
        public ShipFireControlView()
        {
            XamlReader.Load(this);
        }

        public ShipFireControlView(ShipOrderVM viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
