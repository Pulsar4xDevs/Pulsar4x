using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public abstract class JobAbilityBaseVM<TDataBlob, TJob> : IViewModel
        where TDataBlob : BaseDataBlob
    {
        protected Entity _colonyEntity_;

        private dynamic DataBlob { get { return _colonyEntity_.GetDataBlob<TDataBlob>(); } }
        protected StaticDataStore _staticData_;
        //public JobPriorityCommand<TDataBlob, TJob> JobChangePriorityCommand { get; set; }

        public int PointsPerDay { get { return DataBlob.PointsPerTick; } }


        public MVMCollectionSyncher<TJob> JobCollectionSyncher { get { return _jobCollectionSyncher; } }
        private MVMCollectionSyncher<TJob> _jobCollectionSyncher { get; set; }
        private ObservableCollection<JobVM<TDataBlob, TJob>> _itemJobs;
        public ObservableCollection<JobVM<TDataBlob, TJob>> ItemJobs
        {
            get { return _itemJobs; }
            set { _itemJobs = value; OnPropertyChanged(); }
        }
        public int JobSelectedIndex { get; set; }


        public Dictionary<string, Guid> ItemDictionary { get; set; }
        public Guid NewJobSelectedItem { get; set; }
        public ushort NewJobBatchCount { get; set; }
        public bool NewJobRepeat { get; set; }



        #region Constructor
        public JobAbilityBaseVM(StaticDataStore staticData, Entity colonyEntity)
        {
            //JobChangePriorityCommand = new JobPriorityCommand<TDataBlob, TJob>(this);

            _staticData_ = staticData;
            _colonyEntity_ = colonyEntity;

            _jobCollectionSyncher = new MVMCollectionSyncher<TJob>(DataBlob.JobBatchList);
            _jobCollectionSyncher.CollectionChanged += _jobCollectionSyncher_CollectionChanged;

            SetupJobs();


        }

        void _jobCollectionSyncher_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add :
                    ItemJobs.Add(new JobVM<TDataBlob, TJob>(_staticData_, _colonyEntity_, (JobBase)e.NewItems[0], this));
                    break;
                case NotifyCollectionChangedAction.Move:
                    ItemJobs.Insert(e.NewStartingIndex, new JobVM<TDataBlob, TJob>(_staticData_, _colonyEntity_, (JobBase)e.NewItems[0], this));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ItemJobs.RemoveAt(e.OldStartingIndex);
                    break;
            }

        }
        #endregion


        private ICommand _addNewJob;
        public ICommand AddNewJob
        {
            get
            {
                return _addNewJob ?? (_addNewJob = new CommandHandler(OnNewBatchJob, true));
            }
        }

        public abstract void OnNewBatchJob();

        private void SetupJobs()
        {
            var jobs = DataBlob.JobBatchList;
            _itemJobs = new ObservableCollection<JobVM<TDataBlob, TJob>>();
            foreach (var item in jobs)
            {
                _itemJobs.Add(new JobVM<TDataBlob, TJob>(_staticData_, _colonyEntity_, item, this));
            }
            ItemJobs = ItemJobs;
        }

        public void ChangeJobPriority(object job, int delta)
        {
            TJob jobitem = (TJob)job;
            ListPriority<TJob>.ChangeJobPriority(JobCollectionSyncher, (TJob)job, delta);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Refresh(bool partialRefresh = false)
        {
            SetupJobs();//TODO make this smarter. this rebuilds the VMs whichis not ideal

        }
    }

    public class RefinaryAbilityVM : JobAbilityBaseVM<ColonyRefiningDB, RefineingJob>
    {
        public RefinaryAbilityVM(StaticDataStore staticData, Entity colonyEntity) : base(staticData, colonyEntity)
        {
            ItemDictionary = new Dictionary<string, Guid>();
            foreach (var kvp in _staticData_.RefinedMaterials)
            {
                ItemDictionary.Add(kvp.Value.Name, kvp.Key);
            }
            NewJobSelectedItem = ItemDictionary[ItemDictionary.ElementAt(0).Key];
            NewJobBatchCount = 1;
            NewJobRepeat = false;
        }

        public override void OnNewBatchJob()
        {
            RefineingJob newjob = new RefineingJob(NewJobSelectedItem, NewJobBatchCount, _staticData_.RefinedMaterials[NewJobSelectedItem].RefinaryPointCost, NewJobRepeat);
            RefiningProcessor.AddJob(_staticData_, _colonyEntity_, newjob);
            Refresh();
        }
    }


    public class CommandHandler : ICommand
    {
        private Action _action;
        private bool _canExecute;
        public CommandHandler(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action();
        }
    }

}