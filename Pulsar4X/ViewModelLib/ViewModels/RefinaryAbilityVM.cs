using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class RefinaryAbilityVM : IViewModel 
    {
        private Entity _colonyEntity;
        private ColonyRefiningDB RefiningDB { get { return _colonyEntity.GetDataBlob<ColonyRefiningDB>(); } }
        private StaticDataStore _staticData;
        public JobIncreasePriorityCommand JobIncPriorityCommand { get; set; }
        public JobDecreasePriorityCommand JobDecPriorityCommand { get; set; }
        public int PointsPerDay { get { return RefiningDB.RefinaryPoints; } }

        private ObservableCollection<JobVM> _itemJobs;
        public ObservableCollection<JobVM> ItemJobs
        {
            get { return _itemJobs;}
            set{_itemJobs = value; OnPropertyChanged();}
        }

        public Dictionary<string, Guid> ItemDictionary { get; set; }
        public Guid NewJobSelectedItem { get; set; }
        public ushort NewJobBatchCount { get; set; }
        public bool NewJobRepeat { get; set; }


        #region Constructor
        public RefinaryAbilityVM(StaticDataStore staticData, Entity colonyEntity)
        {
            JobIncPriorityCommand = new JobIncreasePriorityCommand();
            JobDecPriorityCommand = new JobDecreasePriorityCommand();
            _staticData = staticData;
            _colonyEntity = colonyEntity;
            SetupRefiningJobs();
            
            ItemDictionary = new Dictionary<string, Guid>();
            foreach (var kvp in _staticData.RefinedMaterials)
            {
                ItemDictionary.Add(kvp.Value.Name, kvp.Key);
            }
            NewJobSelectedItem = ItemDictionary[ItemDictionary.ElementAt(0).Key];
            NewJobBatchCount = 1;
            NewJobRepeat = false;
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


        public void OnNewBatchJob()
        {
            RefineingJob newjob = new RefineingJob(NewJobSelectedItem,NewJobBatchCount, _staticData.RefinedMaterials[NewJobSelectedItem].RefinaryPointCost, NewJobRepeat);
            RefiningProcessor.AddJob(_staticData, _colonyEntity, newjob);
            Refresh();
        }

        #region Refresh

        private void SetupRefiningJobs()
        {
            var jobs = RefiningDB.JobBatchList;
            _itemJobs = new ObservableCollection<JobVM>();
            foreach (var item in jobs)
            {
                _itemJobs.Add(new JobVM(_staticData, _colonyEntity, item));
            }
            ItemJobs = ItemJobs;
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
            SetupRefiningJobs();
            //if (_refiningDB.JobBatchList.Count != RefinaryJobs.Count)
            //    SetupRefiningJobs();
            //else
            //    foreach (var job in RefinaryJobs)
            //    {
            //        job.Refresh();
            //    }
        }

        #endregion

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