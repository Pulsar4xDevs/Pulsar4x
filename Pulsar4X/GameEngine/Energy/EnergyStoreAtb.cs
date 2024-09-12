using Pulsar4X.Components;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Atb
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
            EnergyGenAbilityDB genDB;

            if (!parentEntity.HasDataBlob<EnergyGenAbilityDB>())
            {
                genDB = new EnergyGenAbilityDB(parentEntity.StarSysDateTime);
                parentEntity.SetDataBlob(genDB);
            }
            else
            {
                genDB = parentEntity.GetDataBlob<EnergyGenAbilityDB>();
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