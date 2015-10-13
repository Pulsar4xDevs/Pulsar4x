using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel;

namespace ViewModelLib.ViewModels
{

    public class ConstructionAbilityVM : IViewModel
    {
        private Entity _colonyEntity;
        private ColonyConstructionDB ConstructionDB { get { return _colonyEntity.GetDataBlob<ColonyConstructionDB>(); } }
        private StaticDataStore _staticData;

        public int PointsPerDay { get { return ConstructionDB.ConstructionPoints; } }

        private ObservableCollection<ConstructionJobVM> _itemJobs;
        public ObservableCollection<ConstructionJobVM> ItemJobs
        {
            get { return _itemJobs; }
            set { _itemJobs = value; OnPropertyChanged(); }
        }

        public Dictionary<string, Guid> ItemDictionary { get; set; }
        public Guid NewJobSelectedItem { get; set; }
        public ushort NewJobBatchCount { get; set; }
        public bool NewJobRepeat { get; set; }


        #region Constructor
        public ConstructionAbilityVM(StaticDataStore staticData, Entity colonyEntity)
        {
            _staticData = staticData;
            _colonyEntity = colonyEntity;
            SetupConstructionJobs();

            ItemDictionary = new Dictionary<string, Guid>();
            foreach (var kvp in _staticData.RefinedMaterials)
            {
                ItemDictionary.Add(kvp.Value.Name, kvp.Key);
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
            newjob.MaterialGuid = NewJobSelectedItem;
            newjob.NumberCompleted = 0;
            newjob.NumberOrdered = NewJobBatchCount;
            newjob.PointsLeft = _staticData.RefinedMaterials[NewJobSelectedItem].RefinaryPointCost;
            newjob.Auto = NewJobRepeat;
            RefiningProcessor.AddJob(_staticData, _colonyEntity, newjob);
            Refresh();
        }

        #region Refresh

        private void SetupConstructionJobs()
        {
            var jobs = ConstructionDB.JobBatchList;
            _itemJobs = new ObservableCollection<ConstructionJobVM>();
            foreach (var item in jobs)
            {
                _itemJobs.Add(new ConstructionJobVM(_staticData, _colonyEntity, item, this));
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
            SetupConstructionJobs();
        }

        #endregion

    }


    public class ConstructionJobVM : IViewModel
    {
        private StaticDataStore _staticData;
        private ConstructionJob _job;
        private Entity _colonyEntity;
        private ConstructionAbilityVM _parentConstructionVM;

        public string Component { get { return _staticData.Components[_job.ComponentDesignGuid].Name; } }
        public ushort Completed { get { return _job.NumberCompleted; } }
        public ushort BatchQuantity { get { return _job.NumberOrdered; } set { _job.NumberOrdered = value; } }
        public bool Repeat { get { return _job.Auto; } set { _job.Auto = value; } }
        private int PriorityIndex { get { return _parentConstructionVM.ItemJobs.IndexOf(this); } }

        public ConstructionJobVM(StaticDataStore staticData, Entity colonyEntity, ConstructionJob constructionJob, ConstructionAbilityVM parentConstructionVM)
        {
            _staticData = staticData;
            _colonyEntity = colonyEntity;
            _job = constructionJob;
            _parentConstructionVM = parentConstructionVM;
        }

        public void IncreasePriority()
        {
            if (PriorityIndex > 0)
            {
                ConstructionProcessor.MoveJob(_colonyEntity, _job, -1);
            }
        }
        public void DecresePriorty()
        {
            if (PriorityIndex < _parentConstructionVM.ItemJobs.Count - 2)
            {
                ConstructionProcessor.MoveJob(_colonyEntity, _job, 1);
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
