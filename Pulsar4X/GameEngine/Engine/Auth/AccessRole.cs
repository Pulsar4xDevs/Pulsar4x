using System;

namespace Pulsar4X.Engine.Auth
{
    [Flags]
    public enum AccessRole : uint
    {
        None            = 0,            // Player can't do anything with this faction.
        SystemKnowledge = 1,            // Player can see systems this faction knows about.
        ColonyVision    = 2,            // UNUSED - EXAMPLE ONLY Player can see locations of colonies.
        UnitVision      = 4,            // Player can see units.
        SensorVision    = 8,            // UNUSED - EXAMPLE ONLY Player can see sensor data.
        FactionEvents   = 16,           // Used for Faction-wide events in the eventlog.
        IssueOrders     = 32,           // UNUSED - EXAMPLE ONLY Player can issue orders to units.
        ManageIndustry  = 64,           // UNUSED - EXAMPLE ONLY Player can manage colony industry.
        ManageShipyards = 128,          // UNUSED - EXAMPLE ONLY Player can manage shipyards.
        ManageResearch  = 256,          // UNUSED - EXAMPLE ONLY Player can manage research projects.
        ManageTeams     = 512,          // UNUSED - EXAMPLE ONLY Player can manage teams.
        Intelligence    = 1024,
        Unused4         = 2048,
        Unused5         = 4096,
        Unused6         = 8192,
        Unused7         = 16384,
        Unused8         = 32768,
        Unused9         = 65536,
        Unused10        = 131072,
        Unused11        = 262144,
        Unused12        = 524288,
        Unused13        = 1048576,
        Unused14        = 2097152,
        Unused15        = 4194304,
        Unused16        = 8388608,
        Unused17        = 16777216,
        Unused18        = 33554432,
        Unused19        = 67108864,
        Unused20        = 134217728,
        Unused21        = 268435456,
        Unused22        = 536870912,
            FullAccess      = 1073741823,   // Player can do anything with this faction, except edit the FullAccess players.
        EditFullAccess  = 1073741824,
            Owner           = 2147483647,   // Player can do anything with this faction, except edit the Owner players.
        EditOwners      = 2147483648,
            SM              = 4294967295    // Player can do anything with this faction.
    }
}