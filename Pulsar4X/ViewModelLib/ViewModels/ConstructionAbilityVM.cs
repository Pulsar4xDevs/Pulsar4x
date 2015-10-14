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

namespace Pulsar4X.ViewModel
{

    public class ConstructionAbilityVM : IViewModel
    {
        private Entity _colonyEntity;
        private ColonyConstructionDB ConstructionDB { get { return _colonyEntity.GetDataBlob<ColonyConstructionDB>(); } }
        private StaticDataStore _staticData;
        private FactionInfoDB FactionInfo { get { return _colonyEntity.GetDataBlob<ColonyInfoDB>().FactionEntity.GetDataBlob<FactionInfoDB>(); } }
        public int PointsPerDay { get { return ConstructionDB.ConstructionPoints; } }

        private ObservableCollection<JobVM> _itemJobs;
        public ObservableCollection<JobVM> ItemJobs
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
            foreach (var kvp in FactionInfo.ComponentDesigns)
            {
                ItemDictionary.Add(kvp.Value.GetDataBlob<NameDB>().DefaultName, kvp.Key);
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
            ConstructionJob newjob = new ConstructionJob();
            newjob.ItemGuid = NewJobSelectedItem;
            newjob.NumberCompleted = 0;
            newjob.NumberOrdered = NewJobBatchCount;
            newjob.PointsLeft = FactionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>().BuildPointCost;
            newjob.Auto = NewJobRepeat;
            ConstructionProcessor.AddJob(_colonyEntity, newjob);
            Refresh();
        }

        #region Refresh

        private void SetupConstructionJobs()
        {
            var jobs = ConstructionDB.JobBatchList;
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
            SetupConstructionJobs();
        }

        #endregion

    }
}
