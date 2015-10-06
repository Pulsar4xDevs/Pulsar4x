using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ColonyMinesDB : BaseDataBlob
    {
        [JsonIgnore] //recalc on game load instead of saving.
        public JDictionary<Guid, int> MineingRate { get; set; }

        //Is this legal?
        public JDictionary<Guid, MineralDepositInfo> MineralDeposit
        {
            get { return OwningEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<SystemBodyDB>().Minerals; }
        }

        public ColonyMinesDB()
        {
            MineingRate = new JDictionary<Guid, int>();
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