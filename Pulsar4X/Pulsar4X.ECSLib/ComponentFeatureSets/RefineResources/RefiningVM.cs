using System;
using System.Collections.ObjectModel;

namespace Pulsar4X.ECSLib
{
    public class RefiningVM : ViewModelBase,  IDBViewmodel 
    {
        RefiningDB _refineDB;

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


        ObservableCollection<object> CurrentJobs = new ObservableCollection<object>();

        public RefiningVM()
        {
        }

        public void Update()
        {
            PointsPerDay = _refineDB.PointsPerTick;

        }
    }
}
