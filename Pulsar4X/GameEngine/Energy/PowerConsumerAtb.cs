using System;
using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Energy
{
    /// <summary>
    /// A colony installation's nameplate electrical draw, in kW, and its shortfall tier.
    /// The colony grid (<see cref="ColonyPowerProcessor"/>) sums these from the installed
    /// designs, so install and uninstall have nothing to do. On a ship this has no effect yet.
    /// </summary>
    public class PowerConsumerAtb : IComponentDesignAttribute
    {
        /// <summary>Nameplate draw of one installation, in kW.</summary>
        [JsonProperty]
        public double Draw_kW { get; internal set; }

        [JsonProperty]
        public PowerTier Tier { get; internal set; }

        [JsonConstructor]
        private PowerConsumerAtb() { }

        public PowerConsumerAtb(double drawKw, double tier)
        {
            Draw_kW = double.IsFinite(drawKw) ? Math.Max(0, drawKw) : 0;
            int t = (int)Math.Round(tier);
            Tier = Enum.IsDefined(typeof(PowerTier), t) ? (PowerTier)t : PowerTier.Industry;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance) { }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance) { }

        public string AtbName() => "Power Draw";

        public string AtbDescription() => "Draws " + Draw_kW.ToString("0.###") + " kW (" + Tier + " priority)";
    }
}
