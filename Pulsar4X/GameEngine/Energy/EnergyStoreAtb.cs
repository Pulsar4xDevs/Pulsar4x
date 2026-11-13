using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;

namespace Pulsar4X.Energy
{
    public class EnergyStoreAtb : IComponentDesignAttribute
    {
        //<type, amount>
        public string EnergyTypeID;
        /// <summary>
        /// In Kjouls
        /// </summary>
        public double MaxStore;

        public EnergyStoreAtb(string energyTypeID, double maxStore)
        {
            EnergyTypeID = energyTypeID;
            MaxStore = maxStore;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            // A colony's batteries belong to its grid; ColonyPowerProcessor reads the installed designs.
            if (parentEntity.HasDataBlob<ColonyInfoDB>())
                return;
            EnergyGenAbilityDB genDB;

            if (!parentEntity.HasDataBlob<EnergyGenAbilityDB>())
            {
                genDB = new EnergyGenAbilityDB(parentEntity.StarSysDateTime);
                parentEntity.SetDataBlob(genDB);
            }
            else
            {
                genDB = parentEntity.GetDataBlob<EnergyGenAbilityDB>();
                EnergyGenProcessor.CatchUp(parentEntity, genDB);
            }
            if (genDB.EnergyStoreMax.ContainsKey(EnergyTypeID))
            {
                genDB.EnergyStoreMax[EnergyTypeID] += MaxStore;
            }
            else
            {
                genDB.EnergyStored[EnergyTypeID] = 0;
                genDB.EnergyStoreMax[EnergyTypeID] = MaxStore;
            }
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (parentEntity.HasDataBlob<ColonyInfoDB>())
                return;
            if (!parentEntity.TryGetDataBlob<EnergyGenAbilityDB>(out var genDB))
                return;
            EnergyGenProcessor.CatchUp(parentEntity, genDB);
            EnergyGenAbilityDB.RemoveStore(genDB, EnergyTypeID, MaxStore);
        }

        public string AtbName()
        {
            return "Energy Storage";
        }

        public string AtbDescription()
        {
            return "Adds " + MaxStore + " Energy Storage to parent";
        }
    }
}