using Pulsar4X.Interfaces;
using Pulsar4X.Orbital;

namespace Pulsar4X.Datablobs
{
    public class WarpAbilityDB : BaseDataBlob, IAbilityDescription
    {
        /// <summary>
        ///
        /// </summary>
        public double MaxSpeed { get; internal set; }
        public double TotalWarpPower { get; internal set; }
        public string EnergyType { get; internal set; }
        public double BubbleCreationCost { get; internal set; }
        public double BubbleSustainCost { get; internal set; }
        public double BubbleCollapseCost { get; internal set; }

        public Vector3 CurrentVectorMS { get; internal set; }

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
            CurrentVectorMS = db.CurrentVectorMS;
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