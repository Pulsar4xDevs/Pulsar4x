using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class JobAbilityView : Panel
    {
        protected Label PointsPerDay { get; set; }
        
        protected StackLayout ItemJobs { get; set; }

        protected ComboBox ItemComboBox { get; set; }
        protected NumericUpDown NewJobBatchAmount { get; set; }
        protected CheckBox NewJobIsRepeated { get; set; }
        protected Button NewJobAdd { get; set; }

        //private JobAbilityBaseVM<BaseDataBlob, object> _viewModel;

        public JobAbilityView()
        {
            XamlReader.Load(this);
            //NewJobAdd.Click += AddSelectedProjectOnClick;
        }

        public JobAbilityView(RefineryAbilityVM viewModel) :this()
        {
            SetViewModel(viewModel);
        }

        public JobAbilityView(ConstructionAbilityVM viewModel) : this()
        {
            SetViewModel(viewModel);
        }


        public void SetViewModel(ConstructionAbilityVM viewModel)
        {

            DataContext = viewModel;

            viewModel.ItemJobs.CollectionChanged += OnItemJobsChanged;
            ItemComboBox.DataStore = viewModel.ItemDictionary.DisplayList;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            NewJobAdd.Command = viewModel.AddNewJob;
            NewJobAdd.Click += OnItemJobsChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConstructionAbilityVM.ItemJobs))
                OnItemJobsChanged(sender, e);
        }



        public void SetViewModel(RefineryAbilityVM viewModel)
        {
            DataContext = viewModel;

            //ItemJobs.DataStore = new ObservableCollection<JobVM<ColonyRefiningDB, RefineingJob>>(viewModel.ItemJobs);
            viewModel.ItemJobs.CollectionChanged += OnItemJobsChanged; //ItemJobs_CollectionChanged;
            //ItemJobs.DataStore = viewModel.ItemJobs;
            ItemComboBox.DataStore = viewModel.ItemDictionary.DisplayList;
            NewJobAdd.Command = viewModel.AddNewJob;
            NewJobAdd.Click += OnItemJobsChanged;

        }

        private void OnItemJobsChanged(object sender, EventArgs e)
        {
            if (DataContext is RefineryAbilityVM)
            {
                RefineryAbilityVM viewModel = (RefineryAbilityVM)DataContext;
                ItemJobs.Items.Clear();
                ItemJobs.SuspendLayout();
                foreach (var vm in viewModel.ItemJobs)
                    ItemJobs.Items.Add(new JobUC(vm));
                ItemJobs.ResumeLayout();
            }

            if (DataContext is ConstructionAbilityVM)
            {
                ConstructionAbilityVM viewModel = (ConstructionAbilityVM)DataContext;
                ItemJobs.Items.Clear();
                ItemJobs.SuspendLayout();
                foreach (var vm in viewModel.ItemJobs)
                    ItemJobs.Items.Add(new JobUC(vm));
                ItemJobs.ResumeLayout();
            }
        }


    }
}
