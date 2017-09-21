using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class RefiningVM : ViewModelBase, IDBViewmodel
    {
        RefiningDB _refineDB;
        StaticDataStore _staticData;
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
        ObservableCollection<RefineJobVM> CurrentJobs = new ObservableCollection<RefineJobVM>();

        public RefiningVM(StaticDataStore staticData, RefiningDB refiningDB)
        {
            _staticData = staticData;
            _refineDB = refiningDB;
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
        ProcessedMaterialSD _materail;
        public string MaterialName { get; set; 
        }
        public bool RepeatJob => _job.Auto;
        public int NumberComplete => _job.NumberCompleted;
        public int NumberOrdered => _job.NumberOrdered;
        public int ProductionPointsLeft => _job.ProductionPointsLeft;

        internal RefineJobVM(StaticDataStore staticData, RefineingJob job)
        {
            _staticData = staticData;
            _job = job;
            MaterialName = _staticData.ProcessedMaterials[_job.ItemGuid].Name;
        }

        internal void Update()
        {
            OnPropertyChanged(nameof(RepeatJob));
            OnPropertyChanged(nameof(NumberComplete));
            OnPropertyChanged(nameof(NumberOrdered));
            OnPropertyChanged(nameof(ProductionPointsLeft));
        }
    }
}