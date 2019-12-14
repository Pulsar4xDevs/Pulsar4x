using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CargoTransferDB : BaseDataBlob
    {


        internal Guid TransferJobID { get; } = Guid.NewGuid();

        internal Entity CargoFromEntity { get; set; }
        [JsonIgnore]
        internal CargoStorageDB CargoFromDB { get; set; }

        internal Entity CargoToEntity { get; set; }
        [JsonIgnore]
        internal CargoStorageDB CargoToDB { get; set; }

        internal List<(ICargoable item, long amount)> OrderedToTransfer { get; set; }
        internal List<(ICargoable item, long amount)> ItemsLeftToTransfer;

        internal double DistanceBetweenEntitys { get; set; }
        internal int TransferRateInKG { get; set; } = 1000;



        public CargoTransferDB()
        {
        }


        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
