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

        public JobAbilityView(RefinaryAbilityVM viewModel) :this()
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

            //ItemJobs.DataStore = new ObservableCollection<JobVM<ColonyConstructionDB, ConstructionJob>>(viewModel.ItemJobs);
            viewModel.ItemJobs.CollectionChanged += NewJobAdd_Click;//ItemJobs_CollectionChanged;
            ItemComboBox.DataStore = viewModel.ItemDictionary.DisplayList;
            //NewJobIsRepeated.Checked = viewModel.NewJobRepeat;
            
            NewJobAdd.Command = viewModel.AddNewJob;
            NewJobAdd.Click += NewJobAdd_Click;
        }

        //private void ItemJobs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    switch (e.Action)
        //    {
        //        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
        //            foreach (var vm in e.NewItems)
        //            {
        //                ItemJobs.Items.Add(new JobUC((JobVM<BaseDataBlob, object>)vm));
        //            }
        //            break;
        //        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
        //            ItemJobs.Items.Clear();

        //            break;

        //    }

        //}

        public void SetViewModel(RefinaryAbilityVM viewModel)
        {
            DataContext = viewModel;

            //ItemJobs.DataStore = new ObservableCollection<JobVM<ColonyRefiningDB, RefineingJob>>(viewModel.ItemJobs);
            viewModel.ItemJobs.CollectionChanged += NewJobAdd_Click; //ItemJobs_CollectionChanged;
            //ItemJobs.DataStore = viewModel.ItemJobs;
            ItemComboBox.DataStore = viewModel.ItemDictionary.DisplayList;
            NewJobAdd.Command = viewModel.AddNewJob;
            NewJobAdd.Click += NewJobAdd_Click;

        }

        private void NewJobAdd_Click(object sender, EventArgs e)
        {
            if (DataContext is RefinaryAbilityVM)
            {
                RefinaryAbilityVM viewModel = (RefinaryAbilityVM)DataContext;
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
