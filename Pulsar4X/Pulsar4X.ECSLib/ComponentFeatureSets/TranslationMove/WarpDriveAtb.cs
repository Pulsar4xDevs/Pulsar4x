using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class WarpDriveAtb : IComponentDesignAttribute
    {

        
        [JsonProperty]
        public int EnginePower { get; internal set; }

        public Guid EnergyType { get; internal set; }
        public double BubbleCreationCost { get; internal set; }
        public double BubbleSustainCost { get; internal set; }
        public double BubbleCollapseCost { get; internal set; }
        
        public WarpDriveAtb()
        {
        }

        public WarpDriveAtb(double power)
        {
            EnginePower = (int)power;
        }

        public WarpDriveAtb(int enginePower)
        {
            EnginePower = enginePower;
        }

        public WarpDriveAtb(WarpDriveAtb ability)
        {
            EnginePower = ability.EnginePower;
        }

        public WarpDriveAtb(int warpPower, Guid energyType, double creationCost, double sustainCost, double bubbleCollapseCost)
        {
            EnginePower = warpPower;
            EnergyType = energyType;
            BubbleCreationCost = creationCost;
            BubbleSustainCost = sustainCost;
            BubbleCollapseCost = bubbleCollapseCost;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<PropulsionAbilityDB>())
                parentEntity.SetDataBlob(new PropulsionAbilityDB());
            
            if (!parentEntity.HasDataBlob<WarpAbilityDB>())
            {
                var ablty = new WarpAbilityDB();
                ablty.EnergyType = EnergyType;
                ablty.BubbleCreationCost = BubbleCreationCost;
                ablty.BubbleSustainCost = BubbleSustainCost;
                ablty.BubbleCollapseCost = BubbleCollapseCost;
                parentEntity.SetDataBlob(ablty);
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