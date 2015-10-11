using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class RefinaryAbilityVM : IViewModel 
    {
        private Entity _colonyEntity;
        private ColonyRefiningDB _refiningDB { get { return _colonyEntity.GetDataBlob<ColonyRefiningDB>(); } }
        private StaticDataStore _staticData;

        public int PointsPerDay { get { return _refiningDB.RefinaryPoints; } }

        private ObservableCollection<RefinaryJobVM> _refinaryJobs;
        public ObservableCollection<RefinaryJobVM> RefinaryJobs
        {
            get { return _refinaryJobs;}
            set{_refinaryJobs = value; OnPropertyChanged();}
        }

        public Dictionary<string, Guid> MaterialDictionary { get; set; }
        public Guid NewJobSelectedMaterial { get; set; }
        public ushort NewJobBatchCount { get; set; }
        public bool NewJobRepeat { get; set; }


        #region Constructor
        public RefinaryAbilityVM(StaticDataStore staticData, Entity colonyEntity)
        {
            _staticData = staticData;
            _colonyEntity = colonyEntity;
            SetupRefiningJobs();
            
            MaterialDictionary = new Dictionary<string, Guid>();
            foreach (var kvp in _staticData.RefinedMaterials)
            {
                MaterialDictionary.Add(kvp.Value.Name, kvp.Key);
            }
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
            RefineingJob newjob = new RefineingJob();
            newjob.jobGuid = NewJobSelectedMaterial;
            newjob.numberCompleted = 0;
            newjob.numberOrdered = NewJobBatchCount;
            newjob.pointsLeft = _staticData.RefinedMaterials[NewJobSelectedMaterial].RefinaryPointCost;
            newjob.auto = NewJobRepeat;
            RefiningProcessor.AddJob(_staticData, _colonyEntity, newjob);
            Refresh();
        }

        #region Refresh

        private void SetupRefiningJobs()
        {
            var jobs = _refiningDB.JobBatchList;
            _refinaryJobs = new ObservableCollection<RefinaryJobVM>();
            foreach (var item in jobs)
            {
                _refinaryJobs.Add(new RefinaryJobVM(_staticData, _colonyEntity, item));
            }
            RefinaryJobs = RefinaryJobs;
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


    public class RefinaryJobVM : IViewModel
    {
        private StaticDataStore _staticData;
        private RefineingJob _job;
        private Entity _colonyEntity;
        private RefinaryAbilityVM _parentRefiningVM;

        public string Material { get { return _staticData.RefinedMaterials[_job.jobGuid].Name; } }
        public ushort Completed { get { return _job.numberCompleted; } }
        public ushort BatchQuantity { get { return _job.numberOrdered; } set { _job.numberOrdered = value; } }
        public bool Repeat { get { return _job.auto; } set { _job.auto = value; } }
        private int PriorityIndex { get { return _parentRefiningVM.RefinaryJobs.IndexOf(this); } }

        public RefinaryJobVM(StaticDataStore staticData, Entity colonyEntity, RefineingJob refiningJob)
        {
            _staticData = staticData;
            _colonyEntity = colonyEntity;
            _job = refiningJob;
        }

        public void IncreasePriority()
        {
            if (PriorityIndex > 0)
            {
                RefiningProcessor.MoveJob(_colonyEntity, _job, -1);
            }
        }
        public void DecresePriorty()
        {
            if (PriorityIndex < _parentRefiningVM.RefinaryJobs.Count - 2)
            {
                RefiningProcessor.MoveJob(_colonyEntity, _job, 1);
            }
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
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Completed"));
                PropertyChanged(this, new PropertyChangedEventArgs("BatchQuantity"));
                PropertyChanged(this, new PropertyChangedEventArgs("Repeat"));
            }
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