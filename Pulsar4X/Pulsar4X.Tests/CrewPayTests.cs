using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
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
        Assert.That(FactionSpending.CrewOnPayroll(_game, _player.Id), Is.GreaterThan(500), "500 pooled plus the crews aboard");
    }

    [Test]
    public void PayDay_BooksOneWagesLine_CrewTimesWage_OncePerDay()
    {
        long crew = FactionSpending.CrewOnPayroll(_game, _player.Id);
        decimal before = Info.Money.GetCurrentFunds();
        int lines = Wages(Info).Count;

        decimal paid = FactionSpending.PayDay(_game, Now);
        Assert.That(paid, Is.EqualTo(crew * 100m));
        Assert.That(FactionSpending.PayDay(_game, Now.AddHours(5)), Is.EqualTo(0m), "once per calendar day");

        var wages = Wages(Info);
        Assert.That(wages, Has.Count.EqualTo(lines + 1));
        Assert.That(wages.Last().Amount, Is.EqualTo(-crew * 100m));
        Assert.That(wages.Last().Description, Is.EqualTo(FactionSpending.WagesText(crew, 100m)));
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(before - crew * 100m));

        Assert.That(FactionSpending.PayDay(_game, Now.AddDays(1)), Is.EqualTo(crew * 100m), "the next day pays again");
    }

    [Test]
    public void TheClock_PaysOneWagesLinePerDay_EvenWhenTheSystemRunsInTheBackground()
    {
        int lines = Wages(Info).Count;
        var start = Now;
        _earth.Manager!.ManagerSubpulses.ProcessSystem(start + TimeSpan.FromDays(12));
        var wages = Wages(Info).Skip(lines).ToList();
        var dates = wages.Select(w => w.Date.Date).ToList();
        // A background system runs hotloop processors every 10 days; the pay run books each missed day.
        Assert.That(wages.Count, Is.InRange(11, 13), string.Join(",", dates.Select(d => d.ToString("yyyy-MM-dd"))));
        Assert.That(dates.Distinct().Count(), Is.EqualTo(wages.Count), "never two on one day");
        Assert.That(dates, Is.EqualTo(Enumerable.Range(0, wages.Count).Select(d => start.Date.AddDays(d)).ToList()), "no day skipped");
        Assert.That(wages.All(w => w.Amount < 0 && w.Amount % 100m == 0), Is.True);
    }

    [Test]
    public void PayDay_BooksEachMissedDay()
    {
        decimal day = FactionSpending.PayDay(_game, Now);
        int lines = Wages(Info).Count;
        Assert.That(FactionSpending.PayDay(_game, Now.AddDays(3)), Is.EqualTo(3 * day));
        Assert.That(Wages(Info).Skip(lines).Select(w => w.Date.Date), Is.EqualTo(new[] { Now.Date.AddDays(1), Now.Date.AddDays(2), Now.Date.AddDays(3) }));
    }

    [TestCase(31, 31)]
    [TestCase(32, 31)]
    [TestCase(40, 31)]
    public void PayDay_CatchUp_IsCappedAtMaxCatchUpDays(int gap, int billedDays)
    {
        decimal day = FactionSpending.PayDay(_game, Now);
        int lines = Wages(Info).Count;
        Assert.That(FactionSpending.PayDay(_game, Now.AddDays(gap)), Is.EqualTo(billedDays * day));
        Assert.That(Wages(Info).Count - lines, Is.EqualTo(billedDays));
    }

    [Test]
    public void Wages_TakeTheBalanceNegative_WithNoDesertion()
    {
        long crew = FactionSpending.CrewOnPayroll(_game, _player.Id);
        GoNegative(Info, 0m);
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(0m));
        FactionSpending.PayDay(_game, Now);
        Assert.That(Info.Money.GetCurrentFunds(), Is.EqualTo(-crew * 100m));
        Assert.That(FactionSpending.CrewOnPayroll(_game, _player.Id), Is.EqualTo(crew), "nobody leaves");
        Assert.That(FactionSpending.IsBlocked(_game, _player.Id), Is.True);

        var snap = new GameProjector(_game).ProjectFaction(_player.Id)!;
        Assert.That(snap.FundsNegative, Is.True);
        Assert.That(snap.SpendingBlocked, Is.True);
        Assert.That(snap.BlockedSpending, Is.EqualTo(FactionSpending.BlockedWhileNegative));
        Assert.That(snap.CrewWagePerDay, Is.EqualTo(100m));
        Assert.That(snap.LastWages, Is.EqualTo(-crew * 100m));
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
    public void SaveRoundTrip_KeepsTheWagesLineAndThePayday()
    {
        decimal paid = FactionSpending.PayDay(_game, Now);
        Assert.That(paid, Is.GreaterThan(0m));
        var ship = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipCrewDB>()).First(s => s.FactionOwnerID == _player.Id && ShipCrew.Aboard(s) > 0);
        double lifeSupport = ship.GetDataBlob<ShipCrewDB>().LifeSupport_kW;
        Assert.That(lifeSupport, Is.GreaterThan(0));
        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        var info = loaded.Factions[_player.Id].GetDataBlob<FactionInfoDB>();
        Assert.That(info.LastCrewPayday, Is.EqualTo(Now.Date));
        var wages = info.Money.GetTransactionsByCategory(TransactionCategory.Wages);
        Assert.That(wages.First().Amount, Is.EqualTo(-paid), "newest first");
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
