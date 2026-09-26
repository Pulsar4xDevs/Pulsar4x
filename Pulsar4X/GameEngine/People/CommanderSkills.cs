using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Ships;

namespace Pulsar4X.People;

/// <summary>
/// Read/write commander XP and the quality knobs AgentProcessor already stubbed
/// (relay / recheck). Does not change planners or actions.
/// </summary>
public static class CommanderSkills
{
    public static readonly TimeSpan DefaultRelayDelay = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan DefaultRecheckInterval = TimeSpan.FromMinutes(30);

    const int DefaultCap = 100;

    public static int Get(CommanderDB db, SkillDomain domain)
        => db.Skills.TryGetValue(domain, out var v) ? v : 0;

    public static void Grant(CommanderDB db, SkillDomain domain, int amount = 1)
    {
        if (amount <= 0)
            return;
        int cap = db.ExperienceCap > 0 ? db.ExperienceCap : DefaultCap;
        int next = Math.Min(cap, Get(db, domain) + amount);
        db.Skills[domain] = next;
        int total = 0;
        foreach (var kv in db.Skills)
            total += kv.Value;
        db.Experience = total;
    }

    /// <summary>1.0 at skill 0, up to 2.0 at cap.</summary>
    public static float Quality(CommanderDB db, SkillDomain domain)
    {
        int cap = db.ExperienceCap > 0 ? db.ExperienceCap : DefaultCap;
        if (cap <= 0)
            return 1f;
        return 1f + Get(db, domain) / (float)cap;
    }

    public static void GrantForCompleted(Entity unit, Goal goal)
    {
        if (goal.Status != GoalStatus.Completed)
            return;
        if (!TryGetCommander(unit, out _, out var db))
            return;
        var domain = SkillDomains.DomainOf(goal.Type, unit.HasDataBlob<FleetDB>());
        if (domain == null)
            return;
        Grant(db, domain.Value);
    }

    public static TimeSpan RelayDelay(Entity unit)
    {
        if (!TryGetCommander(unit, out _, out var db))
            return DefaultRelayDelay;
        float q = Quality(db, SkillDomain.Command);
        return TimeSpan.FromTicks((long)(DefaultRelayDelay.Ticks / q));
    }

    /// <summary>
    /// Kit Survey Speed scaled by Survey quality. No commander → kit rate unchanged.
    /// </summary>
    public static uint SurveyRate(uint kitSpeed, Entity unit)
    {
        if (!TryGetCommander(unit, out _, out var db))
            return Math.Max(1u, kitSpeed);
        float q = Quality(db, SkillDomain.Survey);
        uint rate = (uint)Math.Max(1, Math.Round(kitSpeed * (double)q));
        return rate;
    }

    public static TimeSpan RecheckInterval(Entity unit)
    {
        if (!TryGetCommander(unit, out _, out var db))
            return DefaultRecheckInterval;
        float q = Math.Max(Quality(db, SkillDomain.Command), Quality(db, SkillDomain.Nav));
        return TimeSpan.FromTicks((long)(DefaultRecheckInterval.Ticks * q));
    }

    public static bool TryGetCommander(Entity unit, out Entity commander, out CommanderDB db)
    {
        commander = null!;
        db = null!;
        if (unit.TryGetDataBlob<CommanderDB>(out db))
        {
            commander = unit;
            return true;
        }

        if (unit.TryGetDataBlob<ShipInfoDB>(out var ship) && ship.CommanderID >= 0
            && unit.Manager != null
            && unit.Manager.TryGetEntityById(ship.CommanderID, out commander)
            && commander.TryGetDataBlob(out db))
            return true;

        if (unit.TryGetDataBlob<FleetDB>(out var fleet) && fleet.FlagShipID >= 0
            && unit.Manager != null
            && unit.Manager.TryGetEntityById(fleet.FlagShipID, out var flag)
            && flag.TryGetDataBlob<ShipInfoDB>(out var flagInfo) && flagInfo.CommanderID >= 0
            && unit.Manager.TryGetEntityById(flagInfo.CommanderID, out commander)
            && commander.TryGetDataBlob(out db))
            return true;

        return false;
    }
}
