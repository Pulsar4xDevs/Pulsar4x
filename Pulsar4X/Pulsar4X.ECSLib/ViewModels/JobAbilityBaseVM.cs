using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Pulsar4X.ECSLib
{
    public abstract class JobAbilityBaseVM<TDataBlob, TJob> : ViewModelBase
        where TDataBlob : BaseDataBlob
    {
        protected Entity _colonyEntity_;

        private dynamic DataBlob { get { return _colonyEntity_.GetDataBlob<TDataBlob>(); } }
        protected StaticDataStore _staticData_;
        //public JobPriorityCommand<TDataBlob, TJob> JobChangePriorityCommand { get; set; }

        public int PointsPerDay { get { return DataBlob.PointsPerTick; } }


        public MVMCollectionSyncher<TJob> JobCollectionSyncher { get { return _jobCollectionSyncher; } }
        private MVMCollectionSyncher<TJob> _jobCollectionSyncher { get; set; }

        public RangeEnabledObservableCollection<JobVM<TDataBlob, TJob>> ItemJobs { get { return _itemJobs; } }
        private RangeEnabledObservableCollection<JobVM<TDataBlob, TJob>> _itemJobs = new RangeEnabledObservableCollection<JobVM<TDataBlob, TJob>>();

        public int JobSelectedIndex { get; set; }


        public DictionaryVM<Guid, string> ItemDictionary { get; set; }
        public int NewJobSelectedIndex { get; set; }
        public Guid NewJobSelectedItem { get { return ItemDictionary.GetKey(NewJobSelectedIndex); } }// set; }
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
            //Refresh();
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


            List<JobVM<TDataBlob, TJob>> newJobs = new List<JobVM<TDataBlob, TJob>>();

            foreach (var item in jobs)
            {
                newJobs.Add(new JobVM<TDataBlob, TJob>(_staticData_, _colonyEntity_, item, this));
            }
            ItemJobs.Clear();
            ItemJobs.AddRange(newJobs);
        }

        public void ChangeJobPriority(object job, int delta)
        {
            TJob jobitem = (TJob)job;
            ListPriority<TJob>.ChangeJobPriority(JobCollectionSyncher, (TJob)job, delta);
        }

        public void Refresh(bool partialRefresh = false)
        {
            SetupJobs();//TODO make this smarter. this rebuilds the VMs whichis not ideal

        }
    }
}