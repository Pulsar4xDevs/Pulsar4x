using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ViewModel
{
    public class ConstructionAbilityVM : JobAbilityBaseVM<ColonyConstructionDB, ConstructionJob>
    {
        private FactionInfoDB FactionInfo { get { return _colonyEntity_.GetDataBlob<OwnedDB>().ObjectOwner.GetDataBlob<FactionInfoDB>(); } }

        public ConstructionAbilityVM(StaticDataStore staticData, Entity colonyEntity) : base(staticData, colonyEntity)
        {
            ItemDictionary = new DictionaryVM<Guid, string>(DisplayMode.Value);
            foreach (var kvp in FactionInfo.ComponentDesigns)
            {
                ItemDictionary.Add(kvp.Key, kvp.Value.GetDataBlob<NameDB>().DefaultName);
            }
            //NewJobSelectedItem = ItemDictionary.SelectedKey;
            NewJobSelectedIndex = 0;
            NewJobBatchCount = 1;
            NewJobRepeat = false;
        }

        public override void OnNewBatchJob()
        {
            ComponentInfoDB componentInfo = FactionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>();
            int buildpointCost = componentInfo.BuildPointCost;
            Dictionary<Guid, int> mineralCost = componentInfo.MinerialCosts;
            Dictionary<Guid, int> materialCost = componentInfo.MaterialCosts;
            Dictionary<Guid, int> componentCost = componentInfo.ComponentCosts;

            ConstructionJob newjob = new ConstructionJob(NewJobSelectedItem, componentInfo.ConstructionType, NewJobBatchCount, buildpointCost, NewJobRepeat,
                mineralCost, materialCost, componentCost);

            ConstructionProcessor.AddJob(_colonyEntity_, newjob);
            Refresh();
        }
    }
}
