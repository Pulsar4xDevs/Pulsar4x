using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ConstructionAbilityVM : JobAbilityBaseVM<ConstructionDB, ConstructionJob>
    {
        private FactionInfoDB _factionInfo { get 
            {
                Entity faction;
                _colonyEntity_.Manager.FindEntityByGuid(_colonyEntity_.FactionOwner, out faction);
                return faction.GetDataBlob<FactionInfoDB>(); 
            } }

        public ConstructionAbilityVM(StaticDataStore staticData, Entity colonyEntity) : base(staticData, colonyEntity)
        {
            ItemDictionary = new DictionaryVM<Guid, string>(DisplayMode.Value);
            foreach (var kvp in _factionInfo.ComponentDesigns)
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
            ComponentInfoDB componentInfo = _factionInfo.ComponentDesigns[NewJobSelectedItem].GetDataBlob<ComponentInfoDB>();
            int buildpointCost = componentInfo.BuildPointCost;
            Dictionary<Guid, int> mineralCost = componentInfo.MinerialCosts;
            Dictionary<Guid, int> materialCost = componentInfo.MaterialCosts;
            Dictionary<Guid, int> componentCost = componentInfo.ComponentCosts;

            ConstructionJob newjob = new ConstructionJob(NewJobSelectedItem, componentInfo.ConstructionType, NewJobBatchCount, buildpointCost, NewJobRepeat,
                mineralCost, materialCost, componentCost);

            ConstructionProcessor.AddJob(_factionInfo, _colonyEntity_, newjob);
            Refresh();
        }
    }
}
