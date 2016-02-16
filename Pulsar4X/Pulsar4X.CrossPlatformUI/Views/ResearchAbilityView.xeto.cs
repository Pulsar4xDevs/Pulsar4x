using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System.Collections.Generic;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ResearchAbilityView : Panel
    {
        protected StackLayout ScientistsLayout { get; set; }
        protected List<ScientistUC> ScientistUCList { get; set; }
        protected ComboBox AvailibleProjects { get; set; }
        protected Button AddSelectedProject { get; set; }

        private ColonyResearchVM _viewModel;

        public ResearchAbilityView()
        {
            ScientistUCList = new List<ScientistUC>();
            XamlReader.Load(this);
        }

        public ResearchAbilityView(ColonyResearchVM viewModel) :this()
        {
            SetViewModel(viewModel);
        }

        public void SetViewModel(ColonyResearchVM viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
            AvailibleProjects.DataStore = viewModel.ResearchableTechs.DisplayList;
            AvailibleProjects.SelectedIndexBinding.BindDataContext((ColonyResearchVM vm) => vm.SelectedTechIndex);
            AddSelectedProject.Command = _viewModel.AddNewProject;
            foreach (var scientistControlVM in viewModel.Scientists)
            {
                ScientistUC scientist = new ScientistUC(scientistControlVM);
                ScientistUCList.Add(scientist);
                ScientistsLayout.Items.Add(scientist);               
            }
        }
    }
}
