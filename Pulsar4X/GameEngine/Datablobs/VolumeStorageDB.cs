using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{

    public class VolumeStorageDB : BaseDataBlob, IAbilityDescription
    {
        public Dictionary<string, TypeStore> TypeStores = new Dictionary<string, TypeStore>();
        public double TotalStoredMass { get; internal set; } = 0;

        public int TransferRateInKgHr { get; set; } = 500;

        public double TransferRangeDv_mps { get; set; } = 100;

        [JsonConstructor]
        internal VolumeStorageDB()
        {
        }

        public VolumeStorageDB(string type, double maxVolume)
        {
            TypeStores.Add(type, new TypeStore(maxVolume));
        }


        public VolumeStorageDB(VolumeStorageDB db)
        {
            TypeStores = new Dictionary<string, TypeStore>();
            foreach (var kvp in db.TypeStores)
            {
                TypeStores.Add(kvp.Key, kvp.Value.Clone());
            }
            TotalStoredMass = db.TotalStoredMass;
            TransferRangeDv_mps = db.TransferRangeDv_mps;
            TransferRateInKgHr = db.TransferRateInKgHr;
        }

        public override object Clone()
        {
            return new VolumeStorageDB(this);
        }

        public string AbilityName()
        {
            return "Cargo Volume";
        }

        public string AbilityDescription()
        {
            string desc = "Total Volume storage\n";
            foreach (var kvp in TypeStores)
            {
                //string name = StaticRefLib.StaticData.CargoTypes[kvp.Key].Name;
                //desc += name + "\t" + kvp.Value.MaxVolume + "\n";
                desc += kvp.Value.MaxVolume + "\n";
            }

            return desc;
        }
    }

    public class TypeStore
    {
        public double MaxVolume;
        internal double FreeVolume;
        public SafeDictionary<string, long> CurrentStoreInUnits = new SafeDictionary<string, long>();
        internal Dictionary<string, ICargoable> Cargoables =  new Dictionary<string, ICargoable>();
        public TypeStore(double maxVolume)
        {
            MaxVolume = maxVolume;
            FreeVolume = maxVolume;
        }

        public Dictionary<string, ICargoable> GetCargoables()
        {
            return new Dictionary<string, ICargoable>(Cargoables);
        }

        public bool HasCargoInStore(string cargoID)
        {
            return CurrentStoreInUnits.ContainsKey(cargoID);
        }


        public TypeStore Clone()
        {
            TypeStore clone = new TypeStore(MaxVolume);
            clone.FreeVolume = FreeVolume;
            clone.CurrentStoreInUnits = new SafeDictionary<string, long>(CurrentStoreInUnits);
            clone.Cargoables = new Dictionary<string, ICargoable>(Cargoables);
            return clone;
        }

    }
}
