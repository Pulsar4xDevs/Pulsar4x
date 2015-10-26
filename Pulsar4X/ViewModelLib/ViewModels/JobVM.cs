using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class JobVM<TDataBlob, TJob> : IViewModel
        where TDataBlob : BaseDataBlob
    {
        private StaticDataStore _staticData;
        private JobBase _job;
        private Entity _colonyEntity;
        JobAbilityBaseVM<TDataBlob, TJob> _parentJobAbility { get; set; }

        public JobPriorityCommand<TDataBlob, TJob> JobPriorityCommand { get; set; }

        private int _jobTotalPoints;
        public string Item
        {
            get
            {
                if (_job is RefineingJob)
                    return _staticData.RefinedMaterials[_job.ItemGuid].Name;
                else if (_job is ConstructionJob)
                    return _colonyEntity.GetDataBlob<ColonyInfoDB>().FactionEntity.GetDataBlob<FactionInfoDB>().ComponentDesigns[_job.ItemGuid].GetDataBlob<NameDB>().DefaultName;
                else
                    return "Unknown Jobtype";

            }
        }

        public ushort Completed { get { return _job.NumberCompleted; } set { OnPropertyChanged(); } }
        public ushort BatchQuantity { get { return _job.NumberOrdered; } set { _job.NumberOrdered = value; OnPropertyChanged(); } } //note that we're directly changing the data here.
        public bool Repeat { get { return _job.Auto; } set { _job.Auto = value; OnPropertyChanged(); } } //note that we're directly changing the data here.

        public int ItemBuildPointsRemaining { get { return _job.PointsLeft; } set { OnPropertyChanged(); } }
        public double ItemPercentRemaining { get { return (double)_job.PointsLeft / _jobTotalPoints * 100; } set { OnPropertyChanged(); } }



        public JobVM()
        {
        }


        public JobVM(StaticDataStore staticData, Entity colonyEntity, JobBase job, JobAbilityBaseVM<TDataBlob, TJob> parentJobAbilityVM)
        {
            _staticData = staticData;
            _colonyEntity = colonyEntity;
            _job = job;
            _parentJobAbility = parentJobAbilityVM;

            if (_job is RefineingJob)
                _jobTotalPoints = _staticData.RefinedMaterials[_job.ItemGuid].RefinaryPointCost;
            else if (_job is ConstructionJob)
                _jobTotalPoints = _colonyEntity.GetDataBlob<ColonyInfoDB>().FactionEntity.GetDataBlob<FactionInfoDB>().ComponentDesigns[_job.ItemGuid].GetDataBlob<ComponentInfoDB>().BuildPointCost;

            JobPriorityCommand = new JobPriorityCommand<TDataBlob, TJob>(this);
        }

        public void ChangePriority(int delta)
        {
            _parentJobAbility.ChangeJobPriority(_job, delta);
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
                Completed = Completed;
                BatchQuantity = BatchQuantity;
                Repeat = Repeat;
                ItemBuildPointsRemaining = ItemBuildPointsRemaining;
                ItemPercentRemaining = ItemPercentRemaining;
            }
        }
    }
}
