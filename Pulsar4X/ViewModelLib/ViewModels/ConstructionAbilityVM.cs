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
    public class ConstructionAbilityVM : JobAbilityBaseVM<ColonyConstructionDB, ConstructionJob>
    {
        private FactionInfoDB FactionInfo { get { return _colonyEntity_.GetDataBlob<ColonyInfoDB>().FactionEntity.GetDataBlob<FactionInfoDB>(); } }

        public ConstructionAbilityVM(StaticDataStore staticData, Entity colonyEntity) : base(staticData, colonyEntity)
        {
            ItemDictionary = new Dictionary<string, Guid>();
            foreach (var kvp in FactionInfo.ComponentDesigns)
            {
                ItemDictionary.Add(kvp.Value.GetDataBlob<NameDB>().DefaultName, kvp.Key);
            }
            NewJobSelectedItem = ItemDictionary[ItemDictionary.ElementAt(0).Key];
            NewJobBatchCount = 1;
            NewJobRepeat = false;
        }

        public override void OnNewBatchJob()
        {
            int buildpointCost = FactionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>().BuildPointCost;
            JDictionary<Guid, int> mineralCost = FactionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>().MinerialCosts;
            JDictionary<Guid, int> materialCost = FactionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>().MaterialCosts;
            JDictionary<Guid, int> componentCost = FactionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>().ComponentCosts;

            ConstructionJob newjob = new ConstructionJob(NewJobSelectedItem, NewJobBatchCount, buildpointCost, NewJobRepeat,
                mineralCost, materialCost, componentCost);

            ConstructionProcessor.AddJob(_colonyEntity_, newjob);
            Refresh();
        }
    }
}
