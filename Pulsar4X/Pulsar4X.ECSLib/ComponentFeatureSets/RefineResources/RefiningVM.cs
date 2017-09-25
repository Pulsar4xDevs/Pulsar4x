using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Pulsar4X.ECSLib
{
    public class RefiningVM : ViewModelBase, IDBViewmodel
    {
        Guid _factionGuid;
        RefiningDB _refineDB;
        StaticDataStore _staticData;
        OrderHandler _orderHandler;
        int _pointsPerDay;
        public int PointsPerDay {
            get { return _pointsPerDay; }
            set {
                if(_pointsPerDay != value)
                {
                    _pointsPerDay = value;
                    OnPropertyChanged();
                }
            }
        }

        Dictionary<Guid, RefineJobVM> _currentJobsDict = new Dictionary<Guid, RefineJobVM>();
        public ObservableCollection<RefineJobVM> CurrentJobs { get; } = new ObservableCollection<RefineJobVM>();

        //public ObservableCollection<object> NewJobSelectionItems { get; } = new ObservableCollection<object>();
        public DictionaryVM<Guid, string> ItemDictionary { get; } = new DictionaryVM<Guid, string>(DisplayMode.Value);
        public int NewJobSelectedIndex { get { return ItemDictionary.SelectedIndex; } }
        public Guid NewJobSelectedItem { get { return ItemDictionary.GetKey(NewJobSelectedIndex); } }
        public ushort NewJobBatchCount { get; set; }
        public bool NewJobRepeat { get; set; }

        private ICommand _addNewJob;
        public ICommand AddNewJob
        {
            get
            {
                return _addNewJob ?? (_addNewJob = new CommandHandler(OnNewBatchJob, true));
            }
        }

        private void OnNewBatchJob()
        {
            DateTime dateTime = _refineDB.OwningEntity.Manager.ManagerSubpulses.SystemLocalDateTime;
            var newBatchCommand = new RefineOrdersCommand(_factionGuid, _refineDB.OwningEntity.Guid, dateTime, NewJobSelectedItem, NewJobBatchCount, NewJobRepeat);
            _orderHandler.HandleOrder(newBatchCommand);
            Update();
        }

        public RefiningVM(Game game, RefiningDB refiningDB)
        {
            _staticData = game.StaticData;
            _refineDB = refiningDB;
            _orderHandler = game.OrderHandler;
            _factionGuid = refiningDB.OwningEntity.GetDataBlob<OwnedDB>().OwnedByFaction.Guid;
            foreach (var kvp in _staticData.ProcessedMaterials)
            {
                ItemDictionary.Add(kvp.Key, kvp.Value.Name);
            }
            ItemDictionary.SelectedIndex = 0;
            NewJobBatchCount = 1;
            NewJobRepeat = false;
        }

        public void Update()
        {
            PointsPerDay = _refineDB.PointsPerTick;

            foreach(var jobItem in _refineDB.JobBatchList)
            {

                if(!_currentJobsDict.ContainsKey(jobItem.JobID))
                {
                    var newJobVM = new RefineJobVM(_staticData, jobItem);
                    _currentJobsDict.Add(jobItem.JobID, newJobVM);
                    CurrentJobs.Add(newJobVM);
                }
                _currentJobsDict[jobItem.JobID].Update();
            }

            //a somewhat backwards way of finding any items in the viewmodel dictionary&list that are not in the datablob list. 
            var keys = _currentJobsDict.Keys.ToList();
            foreach(var job in _refineDB.JobBatchList)
            {
                if(keys.Contains(job.JobID))
                { keys.Remove(job.JobID); }

            }
            if(keys.Count > 0)
            {
                foreach(var item in keys)
                {
                    CurrentJobs.Remove(_currentJobsDict[item]);
                    _currentJobsDict.Remove(item);
                }
            }
        }
    }



    public class RefineJobVM : ViewModelBase
    {
        RefineingJob _job;
        StaticDataStore _staticData;
        public string Item { get; set; }
        public bool Repeat => _job.Auto;
        public int Completed => _job.NumberCompleted;
        public int BatchQuantity => _job.NumberOrdered;
        public int ProductionPointsLeft => _job.ProductionPointsLeft;
        public float ItemPercentRemaining { get; set; }
        internal RefineJobVM(StaticDataStore staticData, RefineingJob job)
        {
            _staticData = staticData;
            _job = job;
            Item = _staticData.ProcessedMaterials[_job.ItemGuid].Name;
        }

        internal void Update()
        {
            OnPropertyChanged(nameof(Repeat));
            OnPropertyChanged(nameof(Completed));
            OnPropertyChanged(nameof(BatchQuantity));
            OnPropertyChanged(nameof(ProductionPointsLeft));
            ItemPercentRemaining = (float)_job.NumberCompleted / _job.NumberOrdered  * 100;
            OnPropertyChanged(nameof(ItemPercentRemaining));
        }
    }
}