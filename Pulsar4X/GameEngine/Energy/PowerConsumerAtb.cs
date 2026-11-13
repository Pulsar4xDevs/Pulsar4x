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
    /// designs, so install and uninstall do nothing there. On a ship (energy phase 6) install adds
    /// the draw to <see cref="EnergyGenAbilityDB.AuxDemand"/>: it runs on spare output once the
    /// store is full, so warp and charging come first.
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

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (parentEntity.HasDataBlob<Colonies.ColonyInfoDB>() || !(Draw_kW > 0))
                return;
            EnergyGenAbilityDB genDB;
            if (!parentEntity.TryGetDataBlob(out genDB))
            {
                genDB = new EnergyGenAbilityDB(parentEntity.StarSysDateTime);
                parentEntity.SetDataBlob(genDB);
            }
            else
            {
                EnergyGenProcessor.CatchUp(parentEntity, genDB);
            }
            genDB.AuxDemand += Draw_kW;
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (parentEntity.HasDataBlob<Colonies.ColonyInfoDB>() || !(Draw_kW > 0))
                return;
            if (!parentEntity.TryGetDataBlob<EnergyGenAbilityDB>(out var genDB))
                return;
            EnergyGenProcessor.CatchUp(parentEntity, genDB);
            genDB.AuxDemand = Math.Max(0, genDB.AuxDemand - Draw_kW);
            if (genDB.AuxDemand < 1e-12)
                genDB.AuxDemand = 0;
        }

        public string AtbName() => "Power Draw";

        public string AtbDescription() => "Draws " + Draw_kW.ToString("0.###") + " kW (" + Tier + " priority)";
    }
}
