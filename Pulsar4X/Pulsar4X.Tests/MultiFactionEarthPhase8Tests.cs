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
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Modding;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>Multi-faction phase 8: the crew market.</summary>
[TestFixture]
[NonParallelizable]
public class MultiFactionEarthPhase8Tests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";

    Game _game = null!;
    Entity _player = null!;
    Entity _company = null!;
    Entity _ecn = null!;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), WarpTechStart);
        var result = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345678));
        Assert.That(result, Is.Not.Null);
        (_game, _player) = (result!.Value.game, result.Value.faction);
        _game.Settings.EnforceSingleThread = true;
        _company = Colonies().Single(c => c.FactionOwnerID == _player.Id && BodyName(c) == "Earth");
        _ecn = Named("Euro Consortium");
    }

    List<Entity> Colonies() => _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).ToList();
    static string BodyName(Entity c) => c.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<NameDB>().DefaultName;
    Entity Named(string name) => Colonies().Single(c => c.GetDataBlob<NameDB>().DefaultName == name);
    static FactionInfoDB Info(Entity c) => c.GetFactionOwner.GetDataBlob<FactionInfoDB>();
    static ColonyCrewPoolDB Pool(Entity c) => ColonyCrewPoolDB.Of(c);
    int Species => _ecn.GetDataBlob<ColonyInfoDB>().Population.Keys.First();

    /// <summary>Gets the blocs' first daily pass done (the listing is set up there).</summary>
    void FirstPass()
    {
        foreach (var c in Colonies())
            CrewMarket.RunDay(c, c.StarSysDateTime);
    }

    [Test]
    public void Blocs_ListTheirSparePool_AboveAReserve_AndTheFeeBeatsTuition()
    {
        FirstPass();
        var pool = Pool(_ecn);
        Assert.That(pool.Listed, Is.True);
        Assert.That(pool.Reserve, Is.EqualTo(CrewMarket.NpcReserve));
        Assert.That(pool.Total, Is.GreaterThan(CrewMarket.NpcReserve), "the bloc starts with spare graduates");
        Assert.That(CrewMarket.Available(_ecn, Species), Is.EqualTo(pool.Total - CrewMarket.NpcReserve));
        Assert.That(CrewMarket.TuitionEquivalent(_game), Is.EqualTo(3 * 30 * 100m));
        Assert.That(CrewMarket.Fee(_ecn), Is.EqualTo(CrewMarket.TuitionEquivalent(_game) * 1.5m));
        Assert.That(CrewMarket.Fee(_ecn), Is.GreaterThan(CrewMarket.TuitionEquivalent(_game)));
    }

    [Test]
    public void Buy_MovesCrewAndChargesTheFee_SellerBanksItsShare()
    {
        FirstPass();
        int species = Species;
        long sellerBefore = Pool(_ecn).Total, buyerBefore = Pool(_company).Total;
        decimal buyerFunds = Info(_company).Money.GetCurrentFunds(), sellerFunds = Info(_ecn).Money.GetCurrentFunds();
        decimal fee = CrewMarket.Fee(_ecn);
        var heard = new List<Event>();
        EventManager.Instance.Subscribe(EventType.CrewTransferred, heard.Add);

        var commands = new CommandTranslator(_game);
        var result = commands.Translate(_player, _company, new BuyCrewCommand(_company.Id, _ecn.Id, species, 20));
        EventManager.Instance.Unsubscribe(EventType.CrewTransferred, heard.Add);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(Pool(_ecn).Total, Is.EqualTo(sellerBefore - 20));
        Assert.That(Pool(_company).Total, Is.EqualTo(buyerBefore + 20));
        Assert.That(Info(_company).Money.GetCurrentFunds(), Is.EqualTo(buyerFunds - 20 * fee));
        Assert.That(Info(_ecn).Money.GetCurrentFunds(), Is.EqualTo(sellerFunds + 20 * fee * CrewMarket.SellerShare));
        Assert.That(Info(_company).Money.GetTransactionsByCategory(TransactionCategory.CrewTransfer), Has.Count.EqualTo(1));
        Assert.That(heard, Has.Count.EqualTo(1));
        // The new crew are ordinary pool crew: they are on the buyer's payroll from now.
        Assert.That(FactionSpending.CrewOnPayroll(_game, _player.Id), Is.GreaterThanOrEqualTo(buyerBefore + 20));
    }

    [Test]
    public void Buy_CannotGoBelowTheSellersReserve_OrBeyondFunds_OrOffBody_OrBlocked()
    {
        FirstPass();
        int species = Species;
        long spare = CrewMarket.Available(_ecn, species);
        Assert.That(CrewMarket.TryBuy(_company, _ecn, species, spare + 1), Does.Contain("reserve"));
        Assert.That(CrewMarket.TryBuy(_company, _ecn, species, 0), Is.Not.Null);
        Assert.That(Pool(_ecn).Total, Is.EqualTo(Pool(_ecn).Reserve + spare), "nothing moved");

        // Off the body: Luna Concord lists nothing here, and Earth cannot buy from another body anyway.
        var luna = Named("Luna Concord");
        Pool(luna).Add(species, 500);
        Assert.That(CrewMarket.TryList(luna, true, 0, null), Is.Null);
        Assert.That(CrewMarket.TryBuy(_company, luna, species, 1), Does.Contain("same body"));

        var info = Info(_company);
        decimal funds = info.Money.GetCurrentFunds();
        // Too poor: leave one fee less than 5 crew cost.
        info.Money.AddExpense(_company.StarSysDateTime, TransactionCategory.Trade, "drain", funds - CrewMarket.Fee(_ecn) * 4);
        Assert.That(CrewMarket.TryBuy(_company, _ecn, species, 5), Does.Contain("fee"));
        Assert.That(CrewMarket.TryBuy(_company, _ecn, species, 4), Is.Null);
        // Negative balance blocks spending.
        info.Money.AddExpense(_company.StarSysDateTime, TransactionCategory.Trade, "drain", info.Money.GetCurrentFunds() + 1);
        Assert.That(CrewMarket.TryBuy(_company, _ecn, species, 1), Is.Not.Null);
    }

    [Test]
    public void Buy_RefusesAColonyOfTheSameFaction_AndAnUnlistedSeller()
    {
        FirstPass();
        int species = Species;
        var mars = Named("Mars HQ");
        Assert.That(CrewMarket.TryBuy(_company, mars, species, 1), Does.Contain("own crew"));
        Assert.That(CrewMarket.TryList(_ecn, false, 50, null), Is.Null);
        Assert.That(CrewMarket.TryBuy(_company, _ecn, species, 1), Does.Contain("No crew"));
    }

    [Test]
    public void ThePlayerCanList_ItsOwnGraduates_AtAHigherAsk_AndABlocBuysThem()
    {
        FirstPass();
        int species = _company.GetDataBlob<ColonyInfoDB>().Population.Keys.First();
        Pool(_company).Trained.Clear();
        Pool(_company).Add(species, 100);
        var commands = new CommandTranslator(_game);
        decimal ask = CrewMarket.DefaultFee(_game) * 2;
        Assert.That(commands.Translate(_player, _company, new ListCrewCommand(_company.Id, true, 30, ask)).Accepted, Is.True);
        Assert.That(CrewMarket.Available(_company, species), Is.EqualTo(70));
        Assert.That(CrewMarket.Fee(_company), Is.EqualTo(ask));
        // A listing below the floor falls back to the default fee: no undercutting the academy.
        Assert.That(CrewMarket.TryList(_company, true, 30, 1m), Is.Null);
        Assert.That(CrewMarket.Fee(_company), Is.EqualTo(CrewMarket.DefaultFee(_game)));
        // Bloc colonies are not commanded by the player.
        Assert.That(commands.Translate(_player, _ecn, new ListCrewCommand(_ecn.Id, false, 0, null)).Accepted, Is.False);
        Assert.That(commands.Translate(_player, _ecn, new BuyCrewCommand(_ecn.Id, _company.Id, species, 1)).Accepted, Is.False);
    }

    [Test]
    public void Academy_RefillsABlocsPoolSlowly_TowardReservePlusTheSpareTarget()
    {
        FirstPass();
        var pool = Pool(_ecn);
        int species = Species;
        pool.Trained.Clear();
        pool.Add(species, CrewMarket.NpcReserve);
        var t = _ecn.StarSysDateTime;
        CrewMarket.RunDay(_ecn, t);                       // sets the refill clock
        CrewMarket.RunDay(_ecn, t.AddDays(15));
        Assert.That(pool.Total, Is.EqualTo(CrewMarket.NpcReserve), "not yet a month");
        CrewMarket.RunDay(_ecn, t.AddDays(30));
        Assert.That(pool.Total, Is.EqualTo(CrewMarket.NpcReserve + CrewMarket.NpcMonthlyOutput));
        for (int m = 2; m <= 10; m++)
            CrewMarket.RunDay(_ecn, t.AddDays(30 * m));
        Assert.That(pool.Total, Is.EqualTo(CrewMarket.NpcReserve + CrewMarket.NpcSpareTarget), "capped at the target");
    }

    [Test]
    public void CrewMarketView_ShowsOwnPoolListingAndTheBlocsOffers()
    {
        FirstPass();
        var view = new GameProjector(_game).ProjectEntity(_company, _player.Id).GetView<CrewMarketView>();
        Assert.That(view, Is.Not.Null);
        Assert.That(view!.PoolTotal, Is.EqualTo(Pool(_company).Total));
        Assert.That(view.DefaultFee, Is.EqualTo(CrewMarket.DefaultFee(_game)));
        Assert.That(view.TuitionEquivalent, Is.EqualTo(9000m));
        Assert.That(view.SellerShare, Is.EqualTo(0.6m));
        Assert.That(view.Listed, Is.False);
        var offers = view.Offers.Where(o => o.SellerColonyId == _ecn.Id).ToList();
        Assert.That(offers, Has.Count.EqualTo(1));
        Assert.That(offers[0].Available, Is.EqualTo(CrewMarket.Available(_ecn, Species)));
        Assert.That(offers[0].Fee, Is.EqualTo(CrewMarket.Fee(_ecn)));
        Assert.That(offers[0].Reason, Is.Empty, "friendly, same body, trade granted");
        Assert.That(offers[0].SellerFactionName, Is.Not.Empty);
        Assert.That(view.Offers.Select(o => o.SellerFactionId).Distinct().Count(), Is.EqualTo(3), "all three blocs list");
    }
}
