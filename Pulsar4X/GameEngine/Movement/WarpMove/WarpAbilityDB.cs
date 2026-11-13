using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.Galaxy;

namespace Pulsar4X.Movement
{
    public class WarpAbilityDB : BaseDataBlob, IAbilityDescription
    {
        /// <summary>
        /// Open-space warp speed of the empty ship, m/s. Derived from <see cref="TotalWarpPower"/>
        /// and the owner's <see cref="MassVolumeDB.MassDry"/> via <see cref="WarpMath.MaxSpeedCalc"/>,
        /// so refits and damage show at once and cargo never changes it.
        /// A blob with no warp power, or no owner yet, returns the value last set: test fixtures
        /// set it directly, and old saves still carry a stored number that loads into it.
        /// </summary>
        public double MaxSpeed
        {
            get
            {
                if (TotalWarpPower > 0
                    && OwningEntity != null
                    && OwningEntity.IsValid
                    && OwningEntity.TryGetDataBlob<MassVolumeDB>(out var mass)
                    && mass.MassDry > 0)
                    // Crew plan phase 3: warp drives short of their crew run at aboard / engine crew.
                    return WarpMath.MaxSpeedCalc(TotalWarpPower * Pulsar4X.Ships.ShipCrew.EngineFactor(OwningEntity), mass.MassDry);
                return _setSpeed;
            }
            internal set => _setSpeed = value;
        }

        double _setSpeed;

        public double TotalWarpPower { get; internal set; }
        public string EnergyType { get; internal set; }
        public double BubbleCreationCost { get; internal set; }
        public double BubbleSustainCost { get; internal set; }
        public double BubbleCollapseCost { get; internal set; }
        public double Coupling { get; internal set; }
        public double BleedFraction { get; internal set; }


        public WarpAbilityDB()
        {
        }

        public WarpAbilityDB(WarpAbilityDB db)
        {
            _setSpeed = db._setSpeed;
            TotalWarpPower = db.TotalWarpPower;
            EnergyType = db.EnergyType;
            BubbleCreationCost = db.BubbleCreationCost;
            BubbleSustainCost = db.BubbleSustainCost;
            BubbleCollapseCost = db.BubbleCollapseCost;
            Coupling = db.Coupling;
            BleedFraction = db.BleedFraction;
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
            desc += "Gravitational Coupling : " + Coupling + "\n";
            desc += "Bleed Fraction : " + BleedFraction + "\n";

            return desc;
        }
    }
}