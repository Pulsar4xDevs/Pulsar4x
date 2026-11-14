using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Ships;

namespace Pulsar4X.Factions;

/// <summary>
/// Crew plan phase 5: who pays crew wages, and the negative-balance spending block.
/// Multi-faction phase 6: the local human player and every NPC with its own economy
/// (<see cref="FactionInfoDB.NpcEconomy"/>, the Earth blocs) pay crew and staff wages; only the player is
/// blocked while negative. Other NPCs (Luna, Ceres) are exempt, and a game with no local human (headless,
/// most tests) charges nobody.
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

    /// <summary>True for the local human player's faction and for NPCs with their own economy (multi-faction phase 6):
    /// they pay crew and staff wages. Always false in a game with no local human.</summary>
    public static bool PaysWages(Game? game, int factionId)
        => game != null && game.LocalHumanFactionId >= 0
            && (factionId == game.LocalHumanFactionId || IsNpcEconomy(game, factionId));

    /// <summary>An NPC faction that runs its own economy (pays wages, collects tax).</summary>
    public static bool IsNpcEconomy(Game game, int factionId)
        => game.Factions.TryGetValue(factionId, out var f) && f.TryGetDataBlob<FactionInfoDB>(out var info) && info.NpcEconomy;

    /// <summary>True while the faction's balance is below zero and it is the player (NPCs are never blocked).</summary>
    public static bool IsBlocked(Game? game, int factionId)
    {
        if (game == null || game.LocalHumanFactionId < 0 || factionId != game.LocalHumanFactionId)
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

    /// <summary>Multi-faction phase 6: own staff at work across the faction's colonies.</summary>
    public static long StaffOnPayroll(FactionInfoDB info)
    {
        long staff = 0;
        foreach (var colony in info.Colonies)
            if (colony.IsValid && colony.TryGetDataBlob<ColonyWorkforceDB>(out var db))
                staff += db.Employed;
        return staff;
    }

    /// <summary>Multi-faction phase 6: contract staff at the faction's colonies, by the bloc that supplies them.</summary>
    public static Dictionary<int, long> CommutersByBloc(FactionInfoDB info)
    {
        var result = new Dictionary<int, long>();
        foreach (var colony in info.Colonies)
            if (colony.IsValid && colony.TryGetDataBlob<ColonyWorkforceDB>(out var db))
                foreach (var (bloc, n) in db.Commuters)
                    result[bloc] = (result.TryGetValue(bloc, out var t) ? t : 0) + n;
        return result;
    }

    /// <summary>Residents of the faction's colonies (the tax base).</summary>
    public static long Residents(FactionInfoDB info)
    {
        long people = 0;
        foreach (var colony in info.Colonies)
            if (colony.IsValid && colony.TryGetDataBlob<ColonyInfoDB>(out var ci))
                people += Pulsar4X.People.NavalAcademyCourse.PopulationOf(ci);
        return people;
    }

    /// <summary>Average days in a month, for monthly rates and the bid reserve.</summary>
    public const double DaysPerMonth = 30.44;

    /// <summary>
    /// Multi-faction phase 6: the faction's running costs per month at the game's wages, whether or not it is charged
    /// (crew on payroll × crew wage + staff and commuters × staff wage) × <see cref="DaysPerMonth"/>. Labs are not counted.
    /// </summary>
    public static decimal MonthlyRunningCosts(Game game, int factionId)
    {
        if (!game.Factions.TryGetValue(factionId, out var faction) || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return 0;
        long staff = StaffOnPayroll(info);
        foreach (var n in CommutersByBloc(info).Values)
            staff += n;
        decimal perDay = CrewOnPayroll(game, factionId) * Math.Max(0, game.Settings.CrewWagePerDay)
            + staff * Math.Max(0, game.Settings.StaffWagePerDay);
        return perDay * (decimal)DaysPerMonth;
    }

    /// <summary>The Staff wages ledger line booked on the 1st for the month before.</summary>
    public static string MonthlyStaffWagesText(DateTime month, long averageStaff, decimal wage, int days)
        => $"Staff wages for {month:yyyy-MM}: avg {averageStaff:N0} staff x {wage:N0} cr x {days} days";

    /// <summary>The Tax ledger line booked on a bloc on the 1st for the month before.</summary>
    public static string MonthlyTaxText(DateTime month, long people, decimal tax)
        => $"Tax for {month:yyyy-MM}: {people:N0} people x {tax:N0} cr";

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
    public static decimal SettleMonth(FactionInfoDB info, DateTime at, Game? game = null)
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
        // Multi-faction phase 6: staff wages (own staff and commuters) on the same bill; each bloc is paid its
        // commuters' share; a bloc collects its tax.
        if (info.AccruedStaffWages > 0)
        {
            int days = Math.Max(1, info.AccruedWageDays);
            long avgStaff = (long)Math.Round((double)info.AccruedStaffDays / days);
            decimal wage = info.AccruedStaffDays > 0 ? info.AccruedStaffWages / info.AccruedStaffDays : 0;
            info.Money.AddExpense(billDate, TransactionCategory.StaffWages, MonthlyStaffWagesText(accrualMonth, avgStaff, wage, days), info.AccruedStaffWages);
            billed += info.AccruedStaffWages;
            if (game != null)
            {
                foreach (var (blocId, amount) in info.AccruedCommuterWages)
                {
                    if (amount > 0 && game.Factions.TryGetValue(blocId, out var bloc) && bloc.TryGetDataBlob<FactionInfoDB>(out var blocInfo))
                        blocInfo.Money.AddIncome(billDate, TransactionCategory.StaffWages, $"Contract staff wages for {accrualMonth:yyyy-MM} from {info.Abbreviation}", amount);
                }
            }
        }
        if (info.NpcEconomy && game != null && game.Settings.BlocTaxPerPersonPerMonth > 0)
        {
            long people = Residents(info);
            decimal tax = game.Settings.BlocTaxPerPersonPerMonth;
            if (people > 0)
                info.Money.AddIncome(billDate, TransactionCategory.Tax, MonthlyTaxText(accrualMonth, people, tax), people * tax);
        }
        info.AccruedStaffWages = 0;
        info.AccruedStaffDays = 0;
        info.AccruedCommuterWages.Clear();
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
        SettleMonth(info, at, game);
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
        // Multi-faction phase 6: the player and every NPC with its own economy (in faction id order).
        if (game.LocalHumanFactionId < 0)
            return 0;
        // Returns the player's crew wages accrued by this run, as before (NPC and staff accruals are on their ledgers).
        decimal player = 0;
        foreach (var id in game.Factions.Keys.OrderBy(k => k).ToList())
        {
            if (!PaysWages(game, id))
                continue;
            decimal crew = PayDay(game, id, at);
            if (id == game.LocalHumanFactionId)
                player = crew;
        }
        return player;
    }

    /// <summary>One faction's pay run (see <see cref="PayDay(Game, DateTime)"/>): crew wages, plus staff wages on own staff
    /// and commuters (the commuters' share is owed to their bloc and paid to it on the 1st). Returns the crew wages accrued.</summary>
    public static decimal PayDay(Game game, int id, DateTime at)
    {
        if (!game.Factions.TryGetValue(id, out var faction) || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return 0;
        if (info.LastCrewPayday.HasValue && info.LastCrewPayday.Value.Date >= at.Date)
        {
            SettleMonth(info, at, game);
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
        decimal staffWage = StaffWagePerDay(game, id);
        long staff = StaffOnPayroll(info);
        var commuters = CommutersByBloc(info);
        long commuterTotal = commuters.Values.Sum();
        decimal staffAmount = (staff + commuterTotal) * staffWage;
        decimal total = 0;
        for (var day = first; day <= at.Date; day = day.AddDays(1))
        {
            SettleMonth(info, day, game);
            if (amount <= 0 && staffAmount <= 0)
                continue;
            info.AccruedWageDays++;
            if (amount > 0)
            {
                info.AccruedWages += amount;
                info.AccruedWageCrewDays += crew;
                total += amount;
            }
            if (staffAmount > 0)
            {
                info.AccruedStaffWages += staffAmount;
                info.AccruedStaffDays += staff + commuterTotal;
                foreach (var (bloc, n) in commuters)
                    info.AccruedCommuterWages[bloc] = (info.AccruedCommuterWages.TryGetValue(bloc, out var owed) ? owed : 0) + n * staffWage;
            }
        }
        return total;
    }
}
