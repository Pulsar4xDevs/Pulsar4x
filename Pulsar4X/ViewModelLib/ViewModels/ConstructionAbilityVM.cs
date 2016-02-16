using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ViewModel
{
    public class ConstructionAbilityVM : JobAbilityBaseVM<ColonyConstructionDB, ConstructionJob>
    {
        private FactionInfoDB FactionInfo { get { return _colonyEntity_.GetDataBlob<OwnedDB>().Faction.GetDataBlob<FactionInfoDB>(); } }

        public ConstructionAbilityVM(StaticDataStore staticData, Entity colonyEntity) : base(staticData, colonyEntity)
        {
            ItemDictionary = new DictionaryVM<string, Guid, string>(DisplayMode.Key);
            foreach (var kvp in FactionInfo.ComponentDesigns)
            {
                ItemDictionary.Add(kvp.Value.GetDataBlob<NameDB>().DefaultName, kvp.Key);
            }
            //NewJobSelectedItem = ItemDictionary[ItemDictionary.ElementAt(0).Key];
            NewJobSelectedIndex = 0;
            NewJobBatchCount = 1;
            NewJobRepeat = false;
        }

        public override void OnNewBatchJob()
        {
            int buildpointCost = FactionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>().BuildPointCost;
            Dictionary<Guid, int> mineralCost = FactionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>().MinerialCosts;
            Dictionary<Guid, int> materialCost = FactionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>().MaterialCosts;
            Dictionary<Guid, int> componentCost = FactionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>().ComponentCosts;

            ConstructionJob newjob = new ConstructionJob(NewJobSelectedItem, NewJobBatchCount, buildpointCost, NewJobRepeat,
                mineralCost, materialCost, componentCost);

            ConstructionProcessor.AddJob(_colonyEntity_, newjob);
            Refresh();
        }
    }
}
