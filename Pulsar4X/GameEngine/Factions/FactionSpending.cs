using System;
using System.Collections.Generic;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Ships;

namespace Pulsar4X.Factions;

/// <summary>
/// Crew plan phase 5: who pays crew wages, and the negative-balance spending block.
/// Only the local human player pays and is blocked: NPCs crew from untrained population and pay
/// nothing for now (TODO: NPC economy), and a game with no local human (headless, most tests)
/// charges nobody.
/// </summary>
public static class FactionSpending
{
    /// <summary>The refusal reason at every blocked spending site.</summary>
    public const string FundsNegative = "Funds negative";

    /// <summary>What a negative balance blocks, for the faction snapshot's banner. Income is never blocked.</summary>
    public static readonly IReadOnlyList<string> BlockedWhileNegative = new[]
    {
        "Research",
        "Market buys",
        "Fuel buys",
        "Intel buys",
        "Shipment buys",
        "Lot offers",
        "Construction",
        "Launches",
        "Academy courses",
    };

    /// <summary>True for the local human player's faction: it pays crew wages and is blocked while negative.</summary>
    public static bool PaysWages(Game? game, int factionId)
        => game != null && game.LocalHumanFactionId >= 0 && factionId == game.LocalHumanFactionId;

    /// <summary>True while the faction's balance is below zero and it is the player (NPCs are exempt).</summary>
    public static bool IsBlocked(Game? game, int factionId)
    {
        if (!PaysWages(game, factionId))
            return false;
        if (!game!.Factions.TryGetValue(factionId, out var faction) || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return false;
        return info.Money.GetCurrentFunds() < 0;
    }

    /// <summary><see cref="IsBlocked(Game, int)"/> for an entity's owner.</summary>
    public static bool IsBlocked(Entity? entity)
        => entity?.Manager?.Game is { } game && IsBlocked(game, entity.FactionOwnerID);

    /// <summary>Crew on the faction's payroll: aboard its ships plus trained crew in its colony pools.</summary>
    public static long CrewOnPayroll(Game game, int factionId)
    {
        long crew = 0;
        foreach (var system in game.Systems)
        {
            foreach (var ship in system.GetAllEntitiesWithDataBlob<ShipCrewDB>())
                if (ship.FactionOwnerID == factionId)
                    crew += ship.GetDataBlob<ShipCrewDB>().Aboard;
            foreach (var colony in system.GetAllEntitiesWithDataBlob<ColonyCrewPoolDB>())
                if (colony.FactionOwnerID == factionId)
                    crew += colony.GetDataBlob<ColonyCrewPoolDB>().Total;
        }
        return crew;
    }

    /// <summary>The faction's crew wage per crew per day: the game's wage for the player, 0 for everyone else.</summary>
    public static decimal WagePerDay(Game? game, int factionId)
        => PaysWages(game, factionId) ? Math.Max(0, game!.Settings.CrewWagePerDay) : 0;

    /// <summary>Most unpaid days one pay run books (a background system runs the pay processor every 10 days).</summary>
    public const int MaxCatchUpDays = 31;

    /// <summary>The ledger line for a day's wages.</summary>
    public static string WagesText(long crew, decimal wage) => $"Crew wages: {crew:N0} crew x {wage:N0} cr";

    /// <summary>
    /// Pays one day's crew wages for the player: one Wages line, crew × wage, once per calendar day.
    /// The balance may go negative; there is no desertion. Days missed since the last payday (background systems run it every 10 days) each get their own line, at the current crew. Returns the amount charged (0 when nothing is due).
    /// </summary>
    public static decimal PayDay(Game game, DateTime at)
    {
        // TODO(crew plan, NPC economy): NPC factions pay no wages yet; they crew from untrained population.
        int id = game.LocalHumanFactionId;
        if (id < 0 || !game.Factions.TryGetValue(id, out var faction) || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return 0;
        if (info.LastCrewPayday.HasValue && info.LastCrewPayday.Value.Date >= at.Date)
            return 0;
        // Background systems run hotloop processors at a tenth of the rate, so the pay run can be days
        // apart: book one line for each unpaid day (at today's crew), never two on one day.
        DateTime first = info.LastCrewPayday.HasValue ? info.LastCrewPayday.Value.Date.AddDays(1) : at.Date;
        // The loop below is inclusive of both ends, so cap the span at MaxCatchUpDays lines.
        if ((at.Date - first).TotalDays > MaxCatchUpDays - 1)
            first = at.Date.AddDays(-(MaxCatchUpDays - 1));
        info.LastCrewPayday = at.Date;
        decimal wage = WagePerDay(game, id);
        long crew = CrewOnPayroll(game, id);
        decimal amount = crew * wage;
        if (amount <= 0)
            return 0;
        decimal total = 0;
        for (var day = first; day <= at.Date; day = day.AddDays(1))
        {
            info.Money.AddExpense(day == at.Date ? at : day, TransactionCategory.Wages, WagesText(crew, wage), amount);
            total += amount;
        }
        return total;
    }
}
