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

    /// <summary>Multi-faction phase 6: the faction's staff wage per employee per day (0 when it pays no wages).</summary>
    public static decimal StaffWagePerDay(Game? game, int factionId)
        => PaysWages(game, factionId) ? Math.Max(0, game!.Settings.StaffWagePerDay) : 0;

    /// <summary>Most unpaid days one pay run books (a background system runs the pay processor every 10 days).</summary>
    public const int MaxCatchUpDays = 31;

    /// <summary>The Wages ledger line booked on the 1st for the month before.</summary>
    public static string MonthlyWagesText(DateTime month, long averageCrew, decimal wage, int days)
        => $"Crew wages for {month:yyyy-MM}: avg {averageCrew:N0} crew x {wage:N0} cr x {days} days";

    /// <summary>The Research ledger line booked on the 1st for the month before.</summary>
    public static string MonthlyResearchText(DateTime month, int labDays)
        => $"Research labs for {month:yyyy-MM}: {labDays:N0} lab-days";

    /// <summary>The first day of the game month holding <paramref name="at"/>.</summary>
    public static DateTime MonthStart(DateTime at) => new(at.Year, at.Month, 1, 0, 0, 0, at.Kind);

    /// <summary>Payday: the 1st of the month after <paramref name="at"/>, when this month's wages and research are billed.</summary>
    public static DateTime NextPayday(DateTime at) => MonthStart(at).AddMonths(1);

    /// <summary>Days in the game month holding <paramref name="at"/>: a daily rate times this is the month's bill.</summary>
    public static int DaysInMonth(DateTime at) => DateTime.DaysInMonth(at.Year, at.Month);

    /// <summary>
    /// What the faction can still spend: funds less what it already owes this month (accrued, not yet billed).
    /// A research lab only runs when this covers its day's cost.
    /// </summary>
    public static decimal FundsAfterOwed(FactionInfoDB info) => info.Money.GetCurrentFunds() - info.AccruedOwed;

    /// <summary>
    /// Monthly billing. Wages and research accrue daily on the faction; on the first run in a later game month
    /// (normally the 1st), the month's accrual is billed as one Wages line and one Research line dated the 1st
    /// after the accrual month, then reset. The whole bill is booked even when funds can't cover it, so the balance
    /// may go negative (the player is then blocked, as with daily wages before). Months skipped entirely (a long
    /// background gap) are billed together on the first of them. Starts the accrual month when none is set
    /// (a new game, or a save from before monthly billing). Returns the amount billed.
    /// </summary>
    public static decimal SettleMonth(FactionInfoDB info, DateTime at)
    {
        var month = MonthStart(at);
        if (info.AccrualMonth is not { } accrualMonth)
        {
            info.AccrualMonth = month;
            return 0;
        }
        if (accrualMonth >= month)
            return 0;
        var billDate = accrualMonth.AddMonths(1);
        decimal billed = 0;
        if (info.AccruedWages > 0)
        {
            int days = Math.Max(1, info.AccruedWageDays);
            long avgCrew = (long)Math.Round((double)info.AccruedWageCrewDays / days);
            decimal wage = info.AccruedWageCrewDays > 0 ? info.AccruedWages / info.AccruedWageCrewDays : 0;
            info.Money.AddExpense(billDate, TransactionCategory.Wages, MonthlyWagesText(accrualMonth, avgCrew, wage, days), info.AccruedWages);
            billed += info.AccruedWages;
        }
        if (info.AccruedResearch > 0)
        {
            info.Money.AddExpense(billDate, TransactionCategory.Research, MonthlyResearchText(accrualMonth, info.AccruedLabDays), info.AccruedResearch);
            billed += info.AccruedResearch;
        }
        info.AccruedWages = 0;
        info.AccruedWageCrewDays = 0;
        info.AccruedWageDays = 0;
        info.AccruedResearch = 0;
        info.AccruedLabDays = 0;
        info.AccrualMonth = month;
        return billed;
    }

    /// <summary>
    /// A research lab's day: refused (false) when the faction is blocked or its funds less what it already owes
    /// this month don't cover <paramref name="cost"/>; otherwise the cost is added to this month's research bill.
    /// Bills last month first when the month has turned.
    /// </summary>
    public static bool TryAccrueResearch(Game? game, int factionId, FactionInfoDB info, DateTime at, decimal cost)
    {
        SettleMonth(info, at);
        if (IsBlocked(game, factionId) || FundsAfterOwed(info) < cost)
            return false;
        if (cost > 0)
            info.AccruedResearch += cost;
        info.AccruedLabDays++;
        return true;
    }

    /// <summary>
    /// Accrues the player's crew wages for each day since the last pay run: crew × wage a day, once per calendar day,
    /// added to this month's wage bill (<see cref="FactionInfoDB.AccruedWages"/>). Nothing is booked on the ledger
    /// until the 1st of the next month, when <see cref="SettleMonth"/> books one Wages line for the whole month.
    /// Days missed since the last run (background systems run it every 10 days) are each accrued at the current crew,
    /// up to <see cref="MaxCatchUpDays"/>, and a month boundary crossed during the catch-up bills the month before.
    /// Returns the wages accrued by this run (0 when nothing is due).
    /// </summary>
    public static decimal PayDay(Game game, DateTime at)
    {
        // TODO(crew plan, NPC economy): NPC factions pay no wages yet; they crew from untrained population.
        int id = game.LocalHumanFactionId;
        if (id < 0 || !game.Factions.TryGetValue(id, out var faction) || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return 0;
        if (info.LastCrewPayday.HasValue && info.LastCrewPayday.Value.Date >= at.Date)
        {
            SettleMonth(info, at);
            return 0;
        }
        // Background systems run hotloop processors at a tenth of the rate, so the pay run can be days
        // apart: accrue each unpaid day (at today's crew), never two on one day.
        DateTime first = info.LastCrewPayday.HasValue ? info.LastCrewPayday.Value.Date.AddDays(1) : at.Date;
        // The loop below is inclusive of both ends, so cap the span at MaxCatchUpDays days.
        if ((at.Date - first).TotalDays > MaxCatchUpDays - 1)
            first = at.Date.AddDays(-(MaxCatchUpDays - 1));
        info.LastCrewPayday = at.Date;
        decimal wage = WagePerDay(game, id);
        long crew = CrewOnPayroll(game, id);
        decimal amount = crew * wage;
        decimal total = 0;
        for (var day = first; day <= at.Date; day = day.AddDays(1))
        {
            SettleMonth(info, day);
            if (amount <= 0)
                continue;
            info.AccruedWages += amount;
            info.AccruedWageCrewDays += crew;
            info.AccruedWageDays++;
            total += amount;
        }
        return total;
    }
}
