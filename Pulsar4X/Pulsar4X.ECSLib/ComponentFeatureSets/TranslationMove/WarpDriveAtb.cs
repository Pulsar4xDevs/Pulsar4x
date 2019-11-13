using System;
using Newtonsoft.Json;

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

        public WarpDriveAtb(int warpPower, Guid energyType, double creationCost, double sustainCost, double bubbleCollapseCost)
        {
            WarpPower = warpPower;
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
    }


    public class WarpAbilityDB : BaseDataBlob
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

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

}