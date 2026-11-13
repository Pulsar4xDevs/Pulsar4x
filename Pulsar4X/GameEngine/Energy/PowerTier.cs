namespace Pulsar4X.Energy
{
    /// <summary>
    /// Shortfall priority of a colony power consumer, highest first. A shortfall would be
    /// met tier by tier in this order, with a proportional brownout inside a tier.
    /// When power is short, higher tiers are served first (see ColonyPowerProcessor.ShareByTier).
    /// </summary>
    public enum PowerTier
    {
        LifeSupport = 1,
        Control = 2,
        Storage = 3,
        Industry = 4,
        Mining = 5,
        /// <summary>
        /// Reserved. Grid battery charging is not a tier: the daily pass charges from what is left
        /// after all demand (ColonyPowerProcessor.RunDay), so nothing is placed in this tier.
        /// </summary>
        Charging = 6,
        Research = 7,
    }
}
