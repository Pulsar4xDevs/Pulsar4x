using System;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Atb
{
    public class WarpDriveAtb : IComponentDesignAttribute
    {

        
        [JsonProperty]
        public int WarpPower { get; internal set; }

        public string EnergyType { get; internal set; }
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

        public WarpDriveAtb(double warpPower, string energyType, double creationCost, double sustainCost, double bubbleCollapseCost)
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
}