using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ScientistResearchView : Panel
    {
        public ScientistResearchView()
        {
            XamlReader.Load(this);
        }

        public ScientistResearchView(ResearchTechControlVM viewModel) : this()
        {
            SetViewModel(viewModel);

        }

        public void SetViewModel(ResearchTechControlVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}
