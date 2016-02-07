using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ResearchAbilityView : Panel
    {
        protected StackLayout ScientistsLayout { get; set; }
        protected List<ScientistUC> ScientistUCList { get; set; }
        protected ComboBox AvailibleProjects { get; set; }
        //protected Label SelectedScientist { get; set; }
        protected Button AddSelectedProject { get; set; }

        private ColonyResearchVM _viewModel;

        public ResearchAbilityView()
        {
            ScientistUCList = new List<ScientistUC>();
            //todo figure out how to properly bind to the stacklayout to the list 
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
            AddSelectedProject.Click += AddSelectedProject_Click;
            AddSelectedProject.Command = _viewModel.AddNewProject;
            foreach (var scientistControlVM in viewModel.Scientists)
            {
                ScientistUC scientist = new ScientistUC(scientistControlVM);
                ScientistUCList.Add(scientist);
                ScientistsLayout.Items.Add(scientist);

                

            }
        }

        private void AddSelectedProject_Click(object sender, EventArgs e)
        {
            foreach (var control in ScientistUCList)
            {
                control.SetResearchViews();
            }

        }
    }
}
