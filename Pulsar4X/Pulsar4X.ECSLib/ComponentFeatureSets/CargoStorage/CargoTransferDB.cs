using System;
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

        internal ICargoable ItemToTranfer { get; set; }
        internal long TotalAmountToTransfer { get; set; }

        internal double DistanceBetweenEntitys { get; set; }
        internal int TransferRate { get; set; }

        internal long AmountTransfered { get; set; }

        public CargoTransferDB()
        {
        }


        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
