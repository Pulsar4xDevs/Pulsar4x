namespace Pulsar4X.Energy
{
    /// <summary>
    /// Shortfall priority of a colony power consumer, highest first. A shortfall would be
    /// met tier by tier in this order, with a proportional brownout inside a tier.
    /// Phase 1 records and reports the tier; nothing is enforced yet.
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
