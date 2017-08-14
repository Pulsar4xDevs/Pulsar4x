using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ColonyMinesDB : BaseDataBlob
    {
        public Dictionary<Guid, int> MineingRate { get; set; }

        public Dictionary<Guid, MineralDepositInfo> MineralDeposit => OwningEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<SystemBodyInfoDB>().Minerals;

        public ColonyMinesDB()
        {
            MineingRate = new Dictionary<Guid, int>();
        }

        public ColonyMinesDB(ColonyMinesDB db)
        {
            
        }

        public override object Clone()
        {
            return new ColonyMinesDB(this);
        }
    }
}