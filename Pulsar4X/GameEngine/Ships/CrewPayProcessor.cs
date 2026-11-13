using System;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Ships;

/// <summary>
/// Crew plan phase 5: pays the player's crew wages once a day, at 0.4 h (before research at 0.5 h),
/// through <see cref="FactionSpending.PayDay"/>. Keyed on ShipCrewDB only so the hotloop schedules it;
/// it pays for the whole faction (ships and pools) and the payday guard keeps it to one line a day.
/// </summary>
public class CrewPayProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency => TimeSpan.FromDays(1);

    public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.4);

    public Type GetParameterType => typeof(ShipCrewDB);

    public void Init(Game game) { }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        if (entity.Manager?.Game is { } game)
            FactionSpending.PayDay(game, entity.Manager.StarSysDateTime);
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        if (manager.Game == null)
            return 0;
        FactionSpending.PayDay(manager.Game, manager.StarSysDateTime);
        return 1;
    }
}
