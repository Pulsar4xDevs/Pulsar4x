using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Movement
{
    public class WarpAbilityDB : BaseDataBlob, IAbilityDescription
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty]
        public double MaxSpeed { get; internal set; }
        [JsonProperty]
        public double TotalWarpPower { get; internal set; }
        [JsonProperty]
        public string EnergyType { get; internal set; }
        [JsonProperty]
        public double BubbleCreationCost { get; internal set; }
        [JsonProperty]
        public double BubbleSustainCost { get; internal set; }
        [JsonProperty]
        public double BubbleCollapseCost { get; internal set; }


        public WarpAbilityDB()
        {
        }

        public WarpAbilityDB(WarpAbilityDB db)
        {
            MaxSpeed = db.MaxSpeed;
            TotalWarpPower = db.TotalWarpPower;
            EnergyType = db.EnergyType;
            BubbleCreationCost = db.BubbleCreationCost;
            BubbleSustainCost = db.BubbleSustainCost;
            BubbleCollapseCost = db.BubbleCollapseCost;
        }

        public override object Clone()
        {
            return new WarpAbilityDB(this);
        }

        public string AbilityName()
        {
            return "Alcubierre Warp Drive";
        }

        public string AbilityDescription()
        {
            string desc = "Power : " + TotalWarpPower + "\n";
            desc += "Bubble Creation : " + BubbleCreationCost + "\n";
            desc += "Bubble Sustain : " + BubbleSustainCost + "\n";
            desc += "Bubble Collapse : " + BubbleCollapseCost + "\n";

            return desc;
        }
    }
}