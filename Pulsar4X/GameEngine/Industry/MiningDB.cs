using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Industry
{
    public class MiningDB : BaseDataBlob, IAbilityDescription
    {
        [JsonProperty]
        public Dictionary<int, long> BaseMiningRate { get; set; }
        [JsonProperty]
        public Dictionary<int, long> ActualMiningRate { get; set; }
        [JsonProperty]
        public int NumberOfMines { get; set;} = 0;
        public Dictionary<int, MineralDeposit> MineralDeposit => OwningEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<MineralsDB>().Minerals;

        public MiningDB()
        {
            BaseMiningRate = new Dictionary<int, long>();
            ActualMiningRate = new Dictionary<int, long>();
        }

        public MiningDB(MiningDB db)
        {

        }

        public override object Clone()
        {
            return new MiningDB(this);
        }

        public string AbilityName()
        {
            return "Resource Mining";
        }

        public string AbilityDescription()
        {
            // FIXME:
            //string time = StaticRefLib.Game.Settings.EconomyCycleTime.ToString();
            string desc = "Mines Resources at Rates of: \n";
            foreach (var kvp in BaseMiningRate)
            {
                //string resourceName = StaticRefLib.StaticData.CargoGoods.GetMineral(kvp.Key).Name;
                //desc += resourceName + "\t" + Stringify.Number(kvp.Value) + "\n";
            }

            return desc; // + "per " + time;
        }
    }
}