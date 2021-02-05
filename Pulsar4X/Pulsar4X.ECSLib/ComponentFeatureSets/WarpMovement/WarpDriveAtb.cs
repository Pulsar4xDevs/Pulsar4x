using System;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    public class WarpDriveAtb : IComponentDesignAttribute
    {

        
        [JsonProperty]
        public int WarpPower { get; internal set; }

        public Guid EnergyType { get; internal set; }
        public double BubbleCreationCost { get; internal set; }
        public double BubbleSustainCost { get; internal set; }
        public double BubbleCollapseCost { get; internal set; }
        
        public WarpDriveAtb()
        {
        }

        public WarpDriveAtb(double power)
        {
            WarpPower = (int)power;
        }

        public WarpDriveAtb(int warpPower)
        {
            WarpPower = warpPower;
        }

        public WarpDriveAtb(WarpDriveAtb ability)
        {
            WarpPower = ability.WarpPower;
        }

        public WarpDriveAtb(double warpPower, Guid energyType, double creationCost, double sustainCost, double bubbleCollapseCost)
        {
            WarpPower = (int)warpPower;
            EnergyType = energyType;
            BubbleCreationCost = creationCost;
            BubbleSustainCost = sustainCost;
            BubbleCollapseCost = bubbleCollapseCost;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            
            if (!parentEntity.HasDataBlob<WarpAbilityDB>())
            {
                var ablty = new WarpAbilityDB();
                ablty.EnergyType = EnergyType;
                ablty.BubbleCreationCost = BubbleCreationCost;
                ablty.BubbleSustainCost = BubbleSustainCost;
                ablty.BubbleCollapseCost = BubbleCollapseCost;
                parentEntity.SetDataBlob(ablty);
            }
            else
            {
                var ablty = parentEntity.GetDataBlob<WarpAbilityDB>();
                ablty.BubbleCreationCost += BubbleCreationCost;
                ablty.BubbleSustainCost += BubbleSustainCost;
                ablty.BubbleCollapseCost += BubbleCollapseCost;
            }
            ShipMovementProcessor.CalcMaxWarpAndEnergyUsage(parentEntity);
        }
        
        public string AtbName()
        {
            return "Warp Drive";
        }

        public string AtbDescription()
        {

            return " ";
        }
    }


    public class WarpAbilityDB : BaseDataBlob, IAbilityDescription
    {
        /// <summary>
        /// 
        /// </summary>
        public double MaxSpeed { get; internal set; }
        public double TotalWarpPower { get; internal set; }
        public Guid EnergyType { get; internal set; }
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