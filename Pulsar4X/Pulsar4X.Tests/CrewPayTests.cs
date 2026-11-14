using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Technology;

namespace Pulsar4X.Tests;

/// <summary>
/// Crew plan phase 5: 100 cr per crew per day (aboard plus pool), one Wages line a day for the player,
/// the balance may go negative, and while it is negative the player's spending is blocked (income is not).
/// NPCs pay nothing and are never blocked. Market, fuel, intel, shipment and lot-offer blocks are tested
/// beside their own sites (MarketExchangeTests, CeresFuelBuyTests, ApiIntelBookTests, ShipmentTests,
/// CeresDirectOfferTests); construction, launches, academy courses and research are here.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CrewPayTests
{
    Game _game = null!;
    Entity _player = null!;
    Entity _earth = null!;

    static (Game game, Entity faction) Start(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), scenario);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345))!.Value;
        return (created.game, created.faction);
    }

    [SetUp]
    public void SetUp()
    {
        (_game, _player) = Start("Pulsar4x.Scenario.WarpTechStart");
        _earth = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .First(c => c.FactionOwnerID == _player.Id && c.HasDataBlob<NavalAcademyDB>());
    }

    FactionInfoDB Info => _player.GetDataBlob<FactionInfoDB>();
    DateTime Now => _earth.StarSysDateTime;

    List<Transaction> Wages(FactionInfoDB info) => info.Money.GetTransactionsByCategory(TransactionCategory.Wages).OrderBy(t => t.Date).ToList(); // the ledger lists newest first

    void GoNegative(FactionInfoDB info, decimal below = 1000m)
        => info.Money.AddExpense(Now, TransactionCategory.Trade, "test drain", info.Money.GetCurrentFunds() + below);

    Entity Npc => _game.Factions.Values.First(f => f.Id != _player.Id && f.Id != _game.GameMasterFaction.Id && f.HasDataBlob<FactionInfoDB>());

    // ---- wages ----

    [Test]
    public void Quickstart_IsThePlayer_AndPaysTheGameWage()
    {
        Assert.That(_game.LocalHumanFactionId, Is.EqualTo(_player.Id));
        Assert.That(_game.Settings.CrewWagePerDay, Is.EqualTo(100m));
        Assert.That(FactionSpending.WagePerDay(_game, _player.Id), Is.EqualTo(100m));
        Assert.That(FactionSpending.CrewOnPayroll(_game, _player.Id), Is.GreaterThan(100), "100 pooled plus the crews aboard");
    }

    [Test]
    public void PayDay_AccruesCrewTimesWage_OncePerDay_AndBooksNoLineMidMonth()
    {
        Assume.That(Now.Day, Is.LessThan(28), "the quickstart starts early in a month");
        long crew = FactionSpending.CrewOnPayroll(_game, _player.Id);
        decimal before = Info.Money.GetCurrentFunds();
        int lines = Wages(Info).Count;

        decimal accrued = FactionSpending.PayDay(_game, Now);
        Assert.That(accrued, Is.EqualTo(crew * 100m));
        Assert.That(FactionSpending.PayDay(_game, Now.AddHours(5)), Is.EqualTo(0m), "once per calendar day");
        Assert.That(Info.AccruedWages, Is.EqualTo(crew * 100m));
        Assert.That(Info.AccruedWageDays, Is.EqualTo(1));
        Assert.That(Info.AccrualMonth, Is.EqualTo(FactionSpending.MonthStart(Now)));

        Assert.That(Wages(Info), Has.Count.EqualTo(lines), "wages are billed on the 1st, not daily");
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(before), "nothing leaves the account mid-month");

        Assert.That(FactionSpending.PayDay(_game, Now.AddDays(1)), Is.EqualTo(crew * 100m), "the next day accrues again");
        Assert.That(Info.AccruedWages, Is.EqualTo(2 * crew * 100m));
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(before));
    }

    [Test]
    public void TheMonth_IsBilledOnThe1st_AsOneWagesLine_AndOneResearchLine()
    {
        var lab = PlayerLabWithQueue();
        long crew = FactionSpending.CrewOnPayroll(_game, _player.Id);
        var start = FactionSpending.MonthStart(Now);
        var research = new ResearchProcessor();
        decimal labCost = lab.GetDataBlob<ResearcherDB>().CostPerDay.GetValue();
        decimal before = Info.Money.GetCurrentFunds();
        int wageLines = Wages(Info).Count;
        int researchLines = ResearchLines(Info).Count;
        int days = DateTime.DaysInMonth(start.Year, start.Month);

        for (int d = 0; d < days; d++)
        {
            var day = start.AddDays(d).AddHours(0.4);
            FactionSpending.PayDay(_game, day);
            research.DoResearch(lab, day.AddHours(0.1));
        }
        Assert.That(Wages(Info), Has.Count.EqualTo(wageLines), "nothing billed during the month");
        Assert.That(ResearchLines(Info), Has.Count.EqualTo(researchLines));
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(before));
        Assert.That(Info.AccruedWages, Is.EqualTo(days * crew * 100m));
        Assert.That(Info.AccruedResearch, Is.EqualTo(days * labCost));
        Assert.That(Info.AccruedLabDays, Is.EqualTo(days));

        var payday = start.AddMonths(1);
        Assert.That(FactionSpending.NextPayday(Now), Is.EqualTo(payday));
        FactionSpending.PayDay(_game, payday.AddHours(0.4));
        research.DoResearch(lab, payday.AddHours(0.5));

        var wages = Wages(Info).Skip(wageLines).ToList();
        var labs = ResearchLines(Info).Skip(researchLines).ToList();
        Assert.That(wages, Has.Count.EqualTo(1), "one Wages line for the month");
        Assert.That(labs, Has.Count.EqualTo(1), "one Research line for the month");
        Assert.That(wages[0].Date, Is.EqualTo(payday));
        Assert.That(labs[0].Date, Is.EqualTo(payday));
        Assert.That(wages[0].Amount, Is.EqualTo(-days * crew * 100m));
        Assert.That(labs[0].Amount, Is.EqualTo(-days * labCost));
        Assert.That(wages[0].Description, Is.EqualTo(FactionSpending.MonthlyWagesText(start, crew, 100m, days)));
        Assert.That(labs[0].Description, Is.EqualTo(FactionSpending.MonthlyResearchText(start, days)));
        // Multi-faction phase 6: the staff wage line is billed on the same day.
        long staff = FactionSpending.StaffOnPayroll(Info);
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(before - days * (crew * 100m + labCost + staff * 2m)));
        // The 1st's own wages and research start the new month's bill.
        Assert.That(Info.AccrualMonth, Is.EqualTo(payday));
        Assert.That(Info.AccruedWages, Is.EqualTo(crew * 100m));
        Assert.That(Info.AccruedResearch, Is.EqualTo(labCost));
    }

    [Test]
    public void GameStartMidMonth_BillsOnlyTheDaysAccrued_OnTheNext1st()
    {
        long crew = FactionSpending.CrewOnPayroll(_game, _player.Id);
        var midMonth = FactionSpending.MonthStart(Now).AddDays(19).AddHours(0.4); // the 20th
        int lines = Wages(Info).Count;
        Assert.That(FactionSpending.PayDay(_game, midMonth), Is.EqualTo(crew * 100m), "the first pay run accrues its own day only");
        Assert.That(Info.AccrualMonth, Is.EqualTo(FactionSpending.MonthStart(midMonth)));
        var payday = FactionSpending.NextPayday(midMonth);
        int daysLeft = (payday - midMonth.Date).Days;
        for (var day = midMonth.Date.AddDays(1); day < payday; day = day.AddDays(1))
            FactionSpending.PayDay(_game, day.AddHours(0.4));
        Assert.That(Wages(Info), Has.Count.EqualTo(lines));
        FactionSpending.PayDay(_game, payday.AddHours(0.4));
        var wages = Wages(Info).Skip(lines).ToList();
        Assert.That(wages, Has.Count.EqualTo(1));
        Assert.That(wages[0].Amount, Is.EqualTo(-daysLeft * crew * 100m), "the 20th to the month's end");
        Assert.That(wages[0].Date, Is.EqualTo(payday));
    }

    [Test]
    public void TheClock_AccruesEveryDay_EvenWhenTheSystemRunsInTheBackground()
    {
        var start = Now;
        Assume.That(start.Day, Is.LessThan(15), "twelve days stay inside the month");
        int lines = Wages(Info).Count;
        long crew = FactionSpending.CrewOnPayroll(_game, _player.Id);
        _earth.Manager!.ManagerSubpulses.ProcessSystem(start + TimeSpan.FromDays(12));
        // A background system runs hotloop processors every 10 days; the pay run accrues each missed day.
        Assert.That(Info.AccruedWageDays, Is.InRange(11, 13));
        Assert.That(Info.LastCrewPayday, Is.Not.Null);
        Assert.That(Info.AccruedWages, Is.GreaterThan(0m));
        Assert.That(Info.AccruedWages % 100m, Is.EqualTo(0m));
        Assert.That(Wages(Info), Has.Count.EqualTo(lines), "no Wages line before the 1st");
    }

    [Test]
    public void TheClock_BillsTheMonth_WhenItRunsPastThe1st()
    {
        var start = Now;
        int lines = Wages(Info).Count;
        var payday = FactionSpending.NextPayday(start);
        // A background system runs the pay processor every 10 days, so run far enough past the 1st for one run.
        _earth.Manager!.ManagerSubpulses.ProcessSystem(payday + TimeSpan.FromDays(12));
        var wages = Wages(Info).Skip(lines).ToList();
        Assert.That(wages, Has.Count.EqualTo(1), string.Join(",", wages.Select(w => w.Date.ToString("yyyy-MM-dd"))));
        Assert.That(wages[0].Date, Is.EqualTo(payday));
        Assert.That(wages[0].Description, Does.StartWith("Crew wages for " + start.ToString("yyyy-MM")));
        Assert.That(Info.AccrualMonth, Is.EqualTo(payday));
    }

    [Test]
    public void PayDay_AccruesEachMissedDay()
    {
        decimal day = FactionSpending.PayDay(_game, Now);
        Assert.That(FactionSpending.PayDay(_game, Now.AddDays(3)), Is.EqualTo(3 * day));
        Assert.That(Info.AccruedWageDays, Is.EqualTo(4));
        Assert.That(Info.AccruedWages, Is.EqualTo(4 * day));
    }

    [TestCase(31, 31)]
    [TestCase(32, 31)]
    [TestCase(40, 31)]
    public void PayDay_CatchUp_IsCappedAtMaxCatchUpDays(int gap, int accruedDays)
    {
        decimal day = FactionSpending.PayDay(_game, Now);
        Assert.That(FactionSpending.PayDay(_game, Now.AddDays(gap)), Is.EqualTo(accruedDays * day));
    }

    [Test]
    public void PayDay_CatchUpAcrossThe1st_BillsTheOldMonth_AndKeepsTheNewDays()
    {
        var start = FactionSpending.MonthStart(Now);
        decimal day = FactionSpending.PayDay(_game, start.AddDays(24)); // the 25th
        int lines = Wages(Info).Count;
        var payday = start.AddMonths(1);
        // Ten days later: the rest of the old month, then the new month's first days.
        var at = start.AddDays(34);
        FactionSpending.PayDay(_game, at);
        int oldDays = (payday - start.AddDays(24)).Days; // the 25th to the month's end, inclusive
        int newDays = (at.Date - payday).Days + 1;
        var wages = Wages(Info).Skip(lines).ToList();
        Assert.That(wages, Has.Count.EqualTo(1));
        Assert.That(wages[0].Date, Is.EqualTo(payday));
        Assert.That(wages[0].Amount, Is.EqualTo(-oldDays * day));
        Assert.That(Info.AccruedWages, Is.EqualTo(newDays * day));
        Assert.That(Info.AccruedWageDays, Is.EqualTo(newDays));
    }

    [Test]
    public void TheBill_TakesTheBalanceNegative_WithNoDesertion_AndBlocks()
    {
        long crew = FactionSpending.CrewOnPayroll(_game, _player.Id);
        var start = FactionSpending.MonthStart(Now);
        FactionSpending.PayDay(_game, start.AddHours(0.4));
        GoNegative(Info, 0m);
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(0m));
        Assert.That(FactionSpending.IsBlocked(_game, _player.Id), Is.False, "owing is not yet negative");
        var payday = start.AddMonths(1);
        FactionSpending.PayDay(_game, payday.AddHours(0.4)); // catches up the rest of the month, then bills it
        int days = DateTime.DaysInMonth(start.Year, start.Month);
        // Multi-faction phase 6: staff wages are on the same bill.
        long staff = FactionSpending.StaffOnPayroll(Info);
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(-days * (crew * 100m + staff * 2m)), "the whole bill is booked even when it can't be covered");
        Assert.That(FactionSpending.CrewOnPayroll(_game, _player.Id), Is.EqualTo(crew), "nobody leaves");
        Assert.That(FactionSpending.IsBlocked(_game, _player.Id), Is.True);

        var snap = new GameProjector(_game).ProjectFaction(_player.Id)!;
        Assert.That(snap.FundsNegative, Is.True);
        Assert.That(snap.SpendingBlocked, Is.True);
        Assert.That(snap.BlockedSpending, Is.EqualTo(FactionSpending.BlockedWhileNegative));
        Assert.That(snap.CrewWagePerDay, Is.EqualTo(100m));
        Assert.That(snap.LastWages, Is.EqualTo(-days * crew * 100m));
        Assert.That(snap.LastWagesDate, Is.EqualTo(payday.Date));
    }

    // ---- research: monthly billing ----

    Entity PlayerLabWithQueue()
    {
        var lab = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ResearcherDB>()).FirstOrDefault(e => e.FactionOwnerID == _player.Id);
        if (lab == null)
            Assert.Ignore("no starting lab in this start");
        var researcher = lab!.GetDataBlob<ResearcherDB>();
        // A slow lab on a long tech, so it keeps researching (and paying) for the whole month.
        researcher.PointsPerDay = new ModifiableValue<int>(100);
        if (researcher.CostPerDay.GetValue() <= 0)
            researcher.CostPerDay = new ModifiableValue<decimal>(1000m);
        var tech = Info.Data.Techs.Values.Where(t => Info.Data.IsResearchable(t.UniqueID))
            .OrderByDescending(t => t.ResearchCost).First();
        Assume.That(tech.ResearchCost - tech.ResearchProgress, Is.GreaterThan(100 * 40), "the tech outlasts a month");
        while (researcher.TechQueue.TryDequeue(out _)) { }
        researcher.TechQueue.Enqueue(tech.UniqueID);
        return lab;
    }

    List<Transaction> ResearchLines(FactionInfoDB info) => info.Money.GetTransactionsByCategory(TransactionCategory.Research).OrderBy(t => t.Date).ToList();

    [Test]
    public void Research_AccruesDaily_WithNoLedgerLine_AndAddsPoints()
    {
        var lab = PlayerLabWithQueue();
        var researcher = lab.GetDataBlob<ResearcherDB>();
        var tech = Info.Data.Techs[researcher.TechQueue.TryPeek(out var queued) ? queued : ""];
        double progress = tech.ResearchProgress;
        decimal funds = Info.Money.GetCurrentFunds();
        int lines = ResearchLines(Info).Count;
        new ResearchProcessor().DoResearch(lab);
        Assert.That(tech.ResearchProgress, Is.GreaterThan(progress));
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(funds), "billed on the 1st");
        Assert.That(ResearchLines(Info), Has.Count.EqualTo(lines));
        Assert.That(Info.AccruedResearch, Is.EqualTo(researcher.CostPerDay.GetValue()));
    }

    [Test]
    public void Research_CanOnlySpend_FundsLessWhatIsOwedThisMonth()
    {
        var lab = PlayerLabWithQueue();
        var researcher = lab.GetDataBlob<ResearcherDB>();
        decimal cost = researcher.CostPerDay.GetValue();
        var tech = Info.Data.Techs[researcher.TechQueue.TryPeek(out var queued) ? queued : ""];
        // Leave funds that cover the owed wages plus half a lab day: positive, but not enough once the bill is counted.
        FactionSpending.PayDay(_game, Now);
        decimal owed = Info.AccruedOwed;
        Assume.That(owed, Is.GreaterThan(0m));
        Info.Money.AddExpense(Now, TransactionCategory.Trade, "test drain", Info.Money.GetCurrentFunds() - owed - cost / 2);
        Assert.That(Info.Money.GetCurrentFunds(), Is.GreaterThan(cost), "funds alone would pay the lab");
        Assert.That(FactionSpending.FundsAfterOwed(Info), Is.EqualTo(cost / 2));
        Assert.That(FactionSpending.IsBlocked(_game, _player.Id), Is.False);

        double progress = tech.ResearchProgress;
        new ResearchProcessor().DoResearch(lab);
        Assert.That(tech.ResearchProgress, Is.EqualTo(progress), "the lab can't pay: no points");
        Assert.That(Info.AccruedResearch, Is.EqualTo(0m), "and nothing owed for it");

        Info.Money.AddIncome(Now, TransactionCategory.Trade, "top up", cost);
        new ResearchProcessor().DoResearch(lab);
        Assert.That(tech.ResearchProgress, Is.GreaterThan(progress), "funds less owed now cover the day");
        Assert.That(Info.AccruedResearch, Is.EqualTo(cost));
        Assert.That(FactionSpending.FundsAfterOwed(Info), Is.EqualTo(cost / 2));
    }

    [Test]
    public void SaveRoundTrip_KeepsTheAccrual_AndBillsItOnThe1st()
    {
        long crew = FactionSpending.CrewOnPayroll(_game, _player.Id);
        decimal accrued = FactionSpending.PayDay(_game, Now);
        FactionSpending.PayDay(_game, Now.AddDays(1));
        Assert.That(Info.AccruedWages, Is.EqualTo(2 * accrued));
        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        var info = loaded.Factions[_player.Id].GetDataBlob<FactionInfoDB>();
        Assert.That(info.AccrualMonth, Is.EqualTo(FactionSpending.MonthStart(Now)));
        Assert.That(info.AccruedWages, Is.EqualTo(2 * accrued));
        Assert.That(info.AccruedWageDays, Is.EqualTo(2));
        Assert.That(info.AccruedWageCrewDays, Is.EqualTo(2 * crew));
        loaded.LocalHumanFactionId = _player.Id;
        int lines = info.Money.GetTransactionsByCategory(TransactionCategory.Wages).Count;
        var payday = FactionSpending.NextPayday(Now);
        // Multi-faction phase 6: the staff accrual survives the save and is billed with the crew wages.
        Assert.That(info.AccruedStaffWages, Is.EqualTo(2 * 2m * FactionSpending.StaffOnPayroll(info)));
        decimal staffOwed = info.AccruedStaffWages;
        Assert.That(FactionSpending.SettleMonth(info, payday), Is.EqualTo(2 * accrued + staffOwed));
        Assert.That(info.Money.GetTransactionsByCategory(TransactionCategory.Wages), Has.Count.EqualTo(lines + 1));
    }

    [Test]
    public void SettleMonth_WithNoAccrualMonth_StartsFresh()
    {
        var info = new FactionInfoDB();
        Assert.That(FactionSpending.SettleMonth(info, new DateTime(2050, 3, 14)), Is.EqualTo(0m));
        Assert.That(info.AccrualMonth, Is.EqualTo(new DateTime(2050, 3, 1)));
        Assert.That(info.Money.GetAllTransactions(), Is.Empty);
        info.AccruedResearch = 500m;
        info.AccruedLabDays = 5;
        Assert.That(FactionSpending.SettleMonth(info, new DateTime(2050, 3, 31)), Is.EqualTo(0m), "same month: nothing billed");
        // Two months skipped: billed once, dated the 1st after the accrual month.
        Assert.That(FactionSpending.SettleMonth(info, new DateTime(2050, 5, 2)), Is.EqualTo(500m));
        var line = info.Money.GetAllTransactions().Single();
        Assert.That(line.Date, Is.EqualTo(new DateTime(2050, 4, 1)));
        Assert.That(line.Category, Is.EqualTo(TransactionCategory.Research));
        Assert.That(line.Amount, Is.EqualTo(-500m));
        Assert.That(info.AccrualMonth, Is.EqualTo(new DateTime(2050, 5, 1)));
        Assert.That(info.AccruedOwed, Is.EqualTo(0m));
    }

    [Test]
    public void NextPayday_AndDaysInMonth()
    {
        Assert.That(FactionSpending.NextPayday(new DateTime(2050, 1, 1)), Is.EqualTo(new DateTime(2050, 2, 1)));
        Assert.That(FactionSpending.NextPayday(new DateTime(2050, 12, 31, 23, 0, 0)), Is.EqualTo(new DateTime(2051, 1, 1)));
        Assert.That(FactionSpending.DaysInMonth(new DateTime(2052, 2, 10)), Is.EqualTo(29));
    }

    [Test]
    public void Npc_PaysNothing_AndIsNeverBlocked()
    {
        var npc = Npc;
        var info = npc.GetDataBlob<FactionInfoDB>();
        GoNegative(info);
        FactionSpending.PayDay(_game, Now);
        Assert.That(Wages(info), Is.Empty);
        Assert.That(FactionSpending.WagePerDay(_game, npc.Id), Is.EqualTo(0m));
        Assert.That(FactionSpending.IsBlocked(_game, npc.Id), Is.False);
        var snap = new GameProjector(_game).ProjectFaction(npc.Id)!;
        Assert.That(snap.SpendingBlocked, Is.False);
        Assert.That(snap.BlockedSpending, Is.Empty);
        Assert.That(snap.PayrollPerDay, Is.EqualTo(0m));
    }

    [Test]
    public void NoLocalHuman_NobodyPays()
    {
        _game.LocalHumanFactionId = -1;
        Assert.That(FactionSpending.PayDay(_game, Now), Is.EqualTo(0m));
        GoNegative(Info);
        Assert.That(FactionSpending.IsBlocked(_game, _player.Id), Is.False);
    }

    [Test]
    public void SaveRoundTrip_KeepsThePayday()
    {
        decimal accrued = FactionSpending.PayDay(_game, Now);
        Assert.That(accrued, Is.GreaterThan(0m));
        var ship = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipCrewDB>()).First(s => s.FactionOwnerID == _player.Id && ShipCrew.Aboard(s) > 0);
        double lifeSupport = ship.GetDataBlob<ShipCrewDB>().LifeSupport_kW;
        Assert.That(lifeSupport, Is.GreaterThan(0));
        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        var info = loaded.Factions[_player.Id].GetDataBlob<FactionInfoDB>();
        Assert.That(info.LastCrewPayday, Is.EqualTo(Now.Date));
        Assert.That(info.AccruedWages, Is.EqualTo(accrued));
        Assert.That(loaded.Settings.CrewWagePerDay, Is.EqualTo(100m));
        Assert.That(loaded.GlobalManager.TryGetGlobalEntityById(ship.Id, out var after), Is.True);
        Assert.That(after.GetDataBlob<ShipCrewDB>().LifeSupport_kW, Is.EqualTo(lifeSupport));
        loaded.LocalHumanFactionId = _player.Id;
        Assert.That(FactionSpending.PayDay(loaded, Now.AddHours(3)), Is.EqualTo(0m), "the payday guard survives the save");
    }

    // ---- block sites ----

    [Test]
    public void Construction_SpendsNoPoints_WhileNegative_AndResumesAfter()
    {
        var industry = _earth.GetDataBlob<IndustryAbilityDB>();
        string itemId = "stainless-steel";
        var design = Info.IndustryDesigns[itemId];
        string lineId = industry.ProductionLines.First(l => l.Value.IndustryTypeRates.ContainsKey(design.IndustryTypeID)).Key;
        var job = new IndustryJob(Info, itemId);
        job.InitialiseJob(1, false);
        IndustryTools.AddJob(_earth, lineId, job);
        var stock = _earth.GetDataBlob<CargoStorageDB>();
        foreach (var (id, amount) in design.ResourceCosts)
            stock.AddCargoByUnit(Info.Data.CargoGoods.GetAny(id)!, amount);

        GoNegative(Info);
        long left = job.ProductionPointsLeft;
        var remaining = job.ResourcesRequiredRemaining.ToDictionary(r => r.Key, r => r.Value);
        IndustryTools.ConstructStuff(_earth);
        Assert.That(job.ProductionPointsLeft, Is.EqualTo(left));
        Assert.That(job.ResourcesRequiredRemaining, Is.EquivalentTo(remaining));
        var view = new GameProjector(_game).ProjectEntity(_earth, _player.Id).GetView<IndustryView>()!;
        var jobView = view.ProductionLines.Single(l => l.Id == lineId).Jobs.Single(j => j.JobId == job.JobID);
        Assert.That(jobView.FundsReason, Is.EqualTo(FundsText.PausedFundsNegative));

        Info.Money.AddIncome(Now, TransactionCategory.Trade, "test", 1_000_000m);
        IndustryTools.ConstructStuff(_earth);
        Assert.That(job.ProductionPointsLeft < left || !job.ResourcesRequiredRemaining.SequenceEqual(remaining) || job.NumberCompleted > 0, Is.True,
            "back above zero, the job progresses");
    }

    [Test]
    public void Launches_AreHeld_WhileNegative()
    {
        if (!_earth.TryGetDataBlob<LaunchComplexDB>(out var launch) || launch.Pads.Count == 0)
            Assert.Ignore("no launch complex on Earth");
        var pad = launch.Pads.Values.First();
        var design = Info.ShipDesigns.Values.First();
        pad.ShipDesignId = design.UniqueID;
        pad.ShipName = "Held";
        pad.ShipMass = 1000;
        pad.ReadyToLaunch = true;
        GoNegative(Info);

        new LaunchComplexProcessor().ProcessEntity(_earth, 0);
        Assert.That(pad.ShipDesignId, Is.EqualTo(design.UniqueID), "still on the pad");
        Assert.That(LaunchComplexProcessor.PadHoldReason(_earth, pad), Is.EqualTo(FundsText.PausedFundsNegative));
        Assert.That(LaunchComplexProcessor.HoldReason(_earth, 1000), Is.EqualTo(FundsText.PausedFundsNegative));

        _game.LocalHumanFactionId = -1;
        Assert.That(LaunchComplexProcessor.PadHoldReason(_earth, pad), Is.Not.EqualTo(FundsText.PausedFundsNegative), "NPC/headless exempt");
    }

    [Test]
    public void AcademyCourses_AreRefused_WhileNegative()
    {
        var academy = _earth.GetDataBlob<NavalAcademyDB>().Academies[0];
        Assert.That(NavalAcademyCourse.CrewRefusal(_earth, academy, 10), Is.Null.Or.Empty);
        GoNegative(Info);
        Assert.That(NavalAcademyCourse.CrewRefusal(_earth, academy, 10), Is.EqualTo(FundsText.PausedFundsNegative));
        int enrolled = _earth.GetDataBlob<NavalAcademyDB>().Academies[0].Cohorts.Count;
        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, academy.ComponentId, 10), Is.EqualTo(FundsText.PausedFundsNegative));
        Assert.That(_earth.GetDataBlob<NavalAcademyDB>().Academies[0].Cohorts.Count, Is.EqualTo(enrolled), "nobody enrolled");
        var view = new GameProjector(_game).ProjectEntity(_earth, _player.Id).GetView<NavalAcademyView>()!;
        Assert.That(view.Academies[0].FundsReason, Is.EqualTo(FundsText.PausedFundsNegative));
    }

    [Test]
    public void Research_SpendsNothing_WhileNegative()
    {
        var lab = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ResearcherDB>()).FirstOrDefault(e => e.FactionOwnerID == _player.Id);
        if (lab == null)
            Assert.Ignore("no starting lab in this start");
        var researcher = lab!.GetDataBlob<ResearcherDB>();
        var tech = Info.Data.Techs.Values.First(t => Info.Data.IsResearchable(t.UniqueID));
        while (researcher.TechQueue.TryDequeue(out _)) { }
        researcher.TechQueue.Enqueue(tech.UniqueID);
        GoNegative(Info);
        double progress = tech.ResearchProgress;
        decimal funds = Info.Money.GetCurrentFunds();
        new ResearchProcessor().DoResearch(lab);
        Assert.That(tech.ResearchProgress, Is.EqualTo(progress));
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(funds));
    }

    [Test]
    public void Income_IsNeverBlocked()
    {
        GoNegative(Info);
        decimal before = Info.Money.GetCurrentFunds();
        Info.Money.AddIncome(Now, TransactionCategory.Trade, "sale", 500m);
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(before + 500m));
    }
}
