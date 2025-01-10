using System.Collections.Generic;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Ships;

namespace Pulsar4X.Storage
{
    public static class StorageSpaceProcessor
    {
       internal static void RecalcVolumeCapacityAndRates(Entity parentEntity)
        {
            CargoStorageDB cargoStorageDB = parentEntity.GetDataBlob<CargoStorageDB>();
            var instancesDB = parentEntity.GetDataBlob<ComponentInstancesDB>();
            
            //TODO: needs to be a global library not a faction library or we'll potentialy have problems with captured ships
            var cargoLibrary = parentEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
            
            foreach (var kvp in CalculatedMaxStorage(instancesDB))
            {
                if(!cargoStorageDB.TypeStores.ContainsKey(kvp.Key))
                    cargoStorageDB.TypeStores.Add(kvp.Key, new TypeStore(kvp.Value));
                else
                {
                    var stor = cargoStorageDB.TypeStores[kvp.Key];
                    var dif = kvp.Value - stor.MaxVolume;
                    cargoStorageDB.ChangeMaxVolume(kvp.Key, dif, cargoLibrary);
                }
            }
            
            var randr = CalcRateAndRange(instancesDB);
            cargoStorageDB.TransferRate = randr.rate;
            cargoStorageDB.TransferRangeDv_mps = randr.range;
        }

        internal static Dictionary<string, double> CalculatedMaxStorage(ComponentInstancesDB instancesDB )
        {         
            Dictionary<string, double> calculatedMaxStorage = new ();
            if( instancesDB.TryGetComponentsByAttribute<CargoStorageAtb>(out var componentInstances))
            {

                foreach (var instance in componentInstances)
                {
                    var design = instance.Design;
                    var atbdata = design.GetAttribute<CargoStorageAtb>();

                    if (instance.HealthPercent() > 0.75)
                    {
                        if(!calculatedMaxStorage.ContainsKey(atbdata.StoreTypeID))
                            calculatedMaxStorage[atbdata.StoreTypeID] = atbdata.MaxVolume;
                        else
                            calculatedMaxStorage[atbdata.StoreTypeID] += atbdata.MaxVolume;
                    }
                }
            }
            return calculatedMaxStorage;
        }

        internal static (int rate, double range) CalcRateAndRange(ComponentInstancesDB instancesDB)
        {
            double rate = 0;
            double range = 0;
            
            int i = 0;
            if (instancesDB.TryGetComponentsByAttribute<CargoTransferAtb>(out List<ComponentInstance> componentTransferInstances))
            {
                foreach (var instance in componentTransferInstances)
                {
                    var design = instance.Design;
                    if (!design.HasAttribute<CargoTransferAtb>())
                        continue;

                    var atbdata = design.GetAttribute<CargoTransferAtb>();
                    if (instance.HealthPercent() > 0.75)
                    {
                        rate += atbdata.TransferRate_kgs;
                        range += atbdata.TransferRange_ms;
                        i++;
                    }
                }
            }
            int finalRate = (int)rate;
            double finalRange = range / i;
            return (finalRate, finalRange);
        }
        
        public static Dictionary<string, double> CalculatedMaxStorage(ShipDesign shipDesign)
        {         
            Dictionary<string, double> calculatedMaxStorage = new ();
            foreach (var component in shipDesign.Components)
            {
                if (component.design.HasAttribute<CargoStorageAtb>())
                {
                    var atbdata = component.design.GetAttribute<CargoStorageAtb>();
                    if (!calculatedMaxStorage.ContainsKey(atbdata.StoreTypeID))
                        calculatedMaxStorage[atbdata.StoreTypeID] = atbdata.MaxVolume;
                    else
                        calculatedMaxStorage[atbdata.StoreTypeID] += atbdata.MaxVolume;
                }
            }
            return calculatedMaxStorage;
        }

        public static (int rate, double range) CalcRateAndRange(ShipDesign shipDesign)
        {
            double rate = 0;
            double range = 0;
            int i = 0;
            foreach (var component in shipDesign.Components)
            {
                if (component.design.HasAttribute<CargoTransferAtb>())
                {
                    var atbdata = component.design.GetAttribute<CargoTransferAtb>();
                    rate += atbdata.TransferRate_kgs * component.count;
                    range += atbdata.TransferRange_ms;
                    i++;
                }
            }
            int finalRate = (int)rate;
            double finalRange = range / i;
            return (finalRate, finalRange);
        }
    }
}


