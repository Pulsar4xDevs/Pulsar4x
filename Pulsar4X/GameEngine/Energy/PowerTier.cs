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
        Charging = 6,
        Research = 7,
    }
}
