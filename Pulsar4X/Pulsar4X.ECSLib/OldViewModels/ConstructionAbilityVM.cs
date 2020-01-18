using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib.Industry;
/*
namespace Pulsar4X.ECSLib
{
    /*
    public class ConstructionAbilityVM : JobAbilityBaseVM<ConstructAbilityDB, ConstructJob>
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
                ItemDictionary.Add(kvp.Key, kvp.Value.Name);
            }
            //NewJobSelectedItem = ItemDictionary.SelectedKey;
            NewJobSelectedIndex = 0;
            NewJobBatchCount = 1;
            NewJobRepeat = false;
        } */
/*
        public override void OnNewBatchJob()
        {
            ComponentDesign componentInfo = _factionInfo.ComponentDesigns[NewJobSelectedItem];
            int buildpointCost = componentInfo.IndustryPointCosts;
            Dictionary<Guid, int> mineralCost = componentInfo.MineralCosts;
            Dictionary<Guid, int> materialCost = componentInfo.MaterialCosts;
            Dictionary<Guid, int> componentCost = componentInfo.ComponentCosts;

            ConstructJob newjob = new ConstructJob(NewJobSelectedItem, componentInfo.ConstructionType, NewJobBatchCount, buildpointCost, NewJobRepeat,
                mineralCost, materialCost, componentCost);

            ConstructionProcessor.AddJob(_factionInfo, _colonyEntity_, newjob);
            Refresh();
        }*/
/*
[Obsolete]
public override void OnNewBatchJob()
{
    throw new NotImplementedException();
}
    }
}
*/