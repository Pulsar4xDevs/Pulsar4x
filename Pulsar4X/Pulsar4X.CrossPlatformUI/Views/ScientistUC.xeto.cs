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

        protected ComboBox AvailibleProjects { get; set; }
        protected Label SelectedProject { get; set; }
        protected Button AddSelectedProject { get; set; }

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

            AvailibleProjects.DataStore = viewModel.ResearchableTechs.DisplayList;
            //AvailibleProjects.ItemTextBinding = Binding.Property<string>("Name");


            AddSelectedProject.Click += AddSelectedProjectOnClick;
            
        }

        private void AddSelectedProjectOnClick(object sender, EventArgs eventArgs)
        {
            throw new NotImplementedException();
        }
    }
}
