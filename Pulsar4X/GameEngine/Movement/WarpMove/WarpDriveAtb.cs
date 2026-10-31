using System;
using Newtonsoft.Json;
using Pulsar4X.Interfaces;
using Pulsar4X.Components;
using Pulsar4X.Engine;

namespace Pulsar4X.Movement
{
    public class WarpDriveAtb : IComponentDesignAttribute
    {


        [JsonProperty]
        public int WarpPower { get; internal set; }

        public string EnergyType { get; internal set; }
        public double BubbleCreationCost { get; internal set; }
        public double BubbleSustainCost { get; internal set; }
        public double BubbleCollapseCost { get; internal set; }
        public double Coupling { get; internal set; }
        public double BleedFraction { get; internal set; }

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

        public WarpDriveAtb(double warpPower, string energyType, double creationCost, double sustainCost, double bubbleCollapseCost, double coupling, double bleed)
            : this(warpPower, energyType, creationCost, sustainCost, bubbleCollapseCost)
        {
            Coupling = coupling;
            BleedFraction = bleed;
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
                ablty.Coupling = Coupling;
                ablty.BleedFraction = BleedFraction;
                parentEntity.SetDataBlob(ablty);
            }
            else
            {
                var ablty = parentEntity.GetDataBlob<WarpAbilityDB>();
                ablty.BubbleCreationCost += BubbleCreationCost;
                ablty.BubbleSustainCost += BubbleSustainCost;
                ablty.BubbleCollapseCost += BubbleCollapseCost;
                // A zero coupling is the free drive. The greater number is the one that climbs.
                ablty.Coupling = Math.Max(ablty.Coupling, Coupling);
                if (BleedFraction > 0)
                {
                    ablty.BleedFraction = ablty.BleedFraction > 0
                        ? Math.Min(ablty.BleedFraction, BleedFraction)
                        : BleedFraction;
                }
            }
            WarpMath.CalcMaxWarpAndEnergyUsage(parentEntity);
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {

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