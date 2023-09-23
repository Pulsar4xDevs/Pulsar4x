using System;
using System.Collections.Generic;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Datablobs
{
    public class MiningDB : BaseDataBlob, IAbilityDescription
    {
        public Dictionary<string, long> BaseMiningRate { get; set; }
        public Dictionary<string, long> ActualMiningRate { get; set; }

        public int NumberOfMines { get; set;} = 0;

        public Dictionary<string, MineralDeposit> MineralDeposit => OwningEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<MineralsDB>().Minerals;

        public MiningDB()
        {
            BaseMiningRate = new Dictionary<string, long>();
            ActualMiningRate = new Dictionary<string, long>();
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