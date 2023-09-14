using System;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class FuelCondition : ComparisonCondition
    {
        public FuelCondition(float threshold, ComparisonType comparisonType) : base(threshold, comparisonType)
        {
            Description = "percent";
            MaxValue = 100;
            MinValue = 0;
        }

        public override bool Evaluate(Entity fleet)
        {
            var fleetDB = fleet.GetDataBlob<FleetDB>();
            var ships = fleetDB.Children.Where(c => c.HasDataBlob<ShipInfoDB>());

            if(ships.Count() == 0) return false;

            double totalFuelPercentage = 0;
            foreach(var ship in ships)
            {
                double fuelPercent = ship.GetFuelPercent();
                totalFuelPercentage += fuelPercent;
            }

            // Round the average so the equals to have a change to fire in the comparisons
            var average = Math.Round(totalFuelPercentage / ships.Count());

            switch(ComparisionType)
            {
                case ComparisonType.LessThan:
                    return average < Threshold;
                case ComparisonType.LessThanOrEqual:
                    return average <= Threshold;
                case ComparisonType.EqualTo:
                    return average == Threshold;
                case ComparisonType.GreaterThan:
                    return average > Threshold;
                case ComparisonType.GreaterThanOrEqual:
                    return average >= Threshold;
                default:
                    throw new InvalidOperationException("Unknown comparison type.");
            }
        }
    }
}