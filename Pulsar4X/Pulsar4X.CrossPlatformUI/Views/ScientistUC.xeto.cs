using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ScientistUC : Panel
    {
        private ScientistControlVM _viewModel { get; set; }

        protected Label FirstName { get; set; }
        protected Label LastName { get; set; }
        protected NumericUpDown AssignedLabs { get; set; }
        protected Label MaxLabs { get; set; }



        protected ScientistResearchView CurrentResearch { get; set; }
        protected StackLayout ResearchQueue { get; set; }

        public ScientistUC()
        {
            XamlReader.Load(this);
        }

        public ScientistUC(ScientistControlVM viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;

            //FirstName.Text = viewModel.ScientistFirstName;
            //LastName.Text = viewModel.ScientistLastName;
            //AssignedLabs.Value = viewModel.ScientistAssignedLabs;
            //MaxLabs.Text = viewModel.ScientistMaxLabs.ToString();

            //AvailibleProjects.DataStore = viewModel.ResearchableTechs.DisplayList;
            //AvailibleProjects.ItemTextBinding = Binding.Property<string>("Name");

            

            SetResearchViews();

        }

        public void SetResearchViews()
        {
            ResearchQueue.Items.Clear();
            if (_viewModel.ProjectQueue.Count > 0)
            {
                CurrentResearch.SetViewModel(_viewModel.ProjectQueue[0]);

                foreach (var item in _viewModel.ProjectQueue)
                {
                    ResearchQueue.Items.Add(new ScientistResearchView(item));
                }
            }
        }

        private void AddSelectedProjectOnClick(object sender, EventArgs eventArgs)
        {
            SetResearchViews();
        }
    }
}
