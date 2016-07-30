using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipAttackView : Panel
    {
        public ShipAttackView()
        {
            XamlReader.Load(this);
        }

        public ShipAttackView(ShipOrderVM viewModel) :this()
        {
            DataContext = viewModel;
        }
    }
}
