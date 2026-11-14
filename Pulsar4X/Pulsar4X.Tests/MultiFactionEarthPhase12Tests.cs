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
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Multi-faction Earth phases 1 and 2: every colony on a body mines the shared deposits in entity-id
/// order and the forecasts count the whole body; colonies on one body trade and move goods instantly
/// and for free, while Earth-Luna still hauls.
/// </summary>
[TestFixture]
public class MultiFactionEarthPhase12Tests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";

    Game _game = null!;
    Entity _player = null!;
    Entity _earthBody = null!;
    Entity _company = null!;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), WarpTechStart);
        var request = GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345678);
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null);
        (_game, _player) = (result!.Value.game, result.Value.faction);
        _game.Settings.EnforceSingleThread = true;
        _company = Colonies().Single(c => c.FactionOwnerID == _player.Id && BodyName(c) == "Earth");
        _earthBody = _company.GetDataBlob<ColonyInfoDB>().PlanetEntity;
    }

    List<Entity> Colonies() => _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).ToList();
    static string BodyName(Entity c) => c.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<NameDB>().DefaultName;
    Entity Named(string name) => Colonies().Single(c => c.GetDataBlob<NameDB>().DefaultName == name);
    static FactionInfoDB Info(Entity colony) => colony.GetFactionOwner.GetDataBlob<FactionInfoDB>();
    static ICargoable Iron(Entity colony) => Info(colony).Data.CargoGoods["iron"];

    static long Stock(Entity colony, int mineralId) =>
        colony.GetDataBlob<CargoStorageDB>().TypeStores.Values
            .Sum(s => s.CurrentStoreInUnits.TryGetValue(mineralId, out var v) ? v : 0);

    // ---------------------------------------------------------------- phase 1

    [Test]
    public void SharedDeposit_LowerIdTakesItsRate_TheNextTakesTheRemainder_AndAccessibilityMatchesSequentialDepletes()
    {
        var miners = _earthBody.ColoniesOnBody().Where(c => c.GetDataBlob<MiningDB>().ActualMiningRate.Count > 0)
            .OrderBy(c => c.Id).ToList();
        Assert.That(miners.Count, Is.GreaterThanOrEqualTo(2), "company and bloc mines on Earth");
        var deposits = _earthBody.GetDataBlob<MineralsDB>().Minerals;
        int mineral = miners[0].GetDataBlob<MiningDB>().ActualMiningRate.Keys
            .First(id => deposits.ContainsKey(id) && miners[1].GetDataBlob<MiningDB>().ActualMiningRate.ContainsKey(id));
        var deposit = deposits[mineral];
        var processor = new MineResourcesProcessor();
        processor.Init(_game);
        var manager = _company.Manager!;

        // Pass 1 on a plentiful deposit measures each miner's daily take.
        var start = (deposit.Amount, deposit.Accessibility);
        var before = miners.Select(c => Stock(c, mineral)).ToList();
        processor.ProcessManager(manager, 86400);
        var take = miners.Select((c, i) => Stock(c, mineral) - before[i]).ToList();
        Assert.That(take[0], Is.GreaterThan(1));
        Assert.That(take[1], Is.GreaterThan(1));

        // Pass 2: only enough left for the first miner and half the second's rate.
        long left = take[0] + take[1] / 2;
        var amount = start.Amount;
        amount.Actual = left;
        deposit.Amount = amount;
        deposit.Accessibility = start.Accessibility;
        var expected = new MineralDeposit { Amount = amount, HalfOriginalAmount = deposit.HalfOriginalAmount, Accessibility = start.Accessibility };
        before = miners.Select(c => Stock(c, mineral)).ToList();
        processor.ProcessManager(manager, 86400);
        var got = miners.Select((c, i) => Stock(c, mineral) - before[i]).ToList();

        Assert.That(got[0], Is.EqualTo(take[0]), "lowest id takes its full rate");
        Assert.That(got[1], Is.EqualTo(take[1] / 2), "the next takes the remainder");
        Assert.That(got.Skip(2), Is.All.EqualTo(0), "nothing left for the rest");
        Assert.That(deposit.Amount.Actual, Is.EqualTo(0));
        // Every miner on the body depletes once, in id order (a zero take still cubes accessibility down).
        foreach (var units in got)
            MiningHelper.Deplete(expected, units);
        Assert.That(deposit.Accessibility, Is.EqualTo(expected.Accessibility).Within(1e-12));
    }

    [Test]
    public void MiningRows_CountTheWholeBody_AndYearsUseTheBodyTotal_ALoneColonyMatchesToday()
    {
        var projector = new GameProjector(_game);
        var view = projector.ProjectEntity(_company, _player.Id).GetView<ColonyMiningView>()!;
        var mined = view.Minerals.Where(r => r.CanMine).ToList();
        Assert.That(mined, Is.Not.Empty);
        foreach (var row in mined)
        {
            long body = _earthBody.ColoniesOnBody().Sum(c =>
                c.GetDataBlob<MiningDB>().ActualMiningRate.TryGetValue(row.MineralId, out var r) ? 365 * r : 0);
            Assert.That(row.BodyAnnualProduction, Is.EqualTo(body), row.Name);
            Assert.That(row.BodyAnnualProduction, Is.GreaterThan(row.AnnualProduction), row.Name + ": the blocs mine it too");
            if (row.AvailableToMine is long amount)
                Assert.That(row.YearsToDepletion, Is.EqualTo((double)amount / body).Within(1e-9), row.Name);
        }

        // Mars HQ mines alone: the body total is its own production, as before.
        var mars = Named("Mars HQ");
        var marsRows = projector.ProjectEntity(mars, _player.Id).GetView<ColonyMiningView>()!.Minerals.Where(r => r.CanMine).ToList();
        Assert.That(marsRows, Is.Not.Empty);
        foreach (var row in marsRows)
        {
            Assert.That(row.BodyAnnualProduction, Is.EqualTo(row.AnnualProduction), row.Name);
            if (row.AvailableToMine is long amount && row.AnnualProduction > 0)
                Assert.That(row.YearsToDepletion, Is.EqualTo((double)amount / row.AnnualProduction).Within(1e-9), row.Name);
        }
    }

    // ---------------------------------------------------------------- phase 2

    void List(Entity seller, long units, decimal ask) =>
        Assert.That(MarketBook.SetListing(seller, new MarketListing { CargoId = "iron", SellQuantity = units, Ask = ask }), Is.True);

    [Test]
    public void CompanyBuysFromABloc_SettlesTheSameTick_WithNoShipmentAndNoFee()
    {
        var bloc = Named("Sino-Pacific Coalition");
        Assert.That(SameBody.Is(_company, bloc), Is.True);
        List(bloc, 1000, 5m);
        var iron = Iron(bloc);
        long buyerBefore = MarketBook.Stock(_company, iron), sellerBefore = MarketBook.Stock(bloc, iron);
        decimal buyerFunds = Info(_company).Money.GetCurrentFunds(), sellerFunds = Info(bloc).Money.GetCurrentFunds();

        Assert.That(ShipmentBook.TrySealMarket(_company, bloc, false, new List<(ICargoable, long)> { (iron, 400) }, 999m,
            out var made, out var reason), Is.True, reason);

        Assert.That(made, Is.Empty, "no shipment");
        Assert.That(ShipmentBook.At(bloc), Is.Empty, "no haul at the seller");
        Assert.That(MarketBook.Stock(_company, iron), Is.EqualTo(buyerBefore + 400));
        Assert.That(MarketBook.Stock(bloc, iron), Is.EqualTo(sellerBefore - 400));
        Assert.That(Info(_company).Money.GetCurrentFunds(), Is.EqualTo(buyerFunds - 2000m), "the ask, fee 0");
        Assert.That(Info(bloc).Money.GetCurrentFunds(), Is.EqualTo(sellerFunds + 2000m));
        Assert.That(MarketBook.TryGet(bloc, "iron", out var listing) && listing.SellQuantity == 600, Is.True);

        // The market view tells the viewer which of its colonies would get it delivered here.
        var market = new GameProjector(_game).ProjectEntity(bloc, _player.Id).GetView<MarketView>()!;
        Assert.That(market.SameBodyColonyIds, Is.EqualTo(new[] { _company.Id }));
    }

    [Test]
    public void ALunaBuy_StillPostsAHaul()
    {
        var luna = Named("Luna Concord");
        Assert.That(SameBody.Is(_company, luna), Is.False);
        List(luna, 1000, 5m);
        Assert.That(ShipmentBook.TrySealMarket(_company, luna, false, new List<(ICargoable, long)> { (Iron(luna), 100) }, 50m,
            out var made, out var reason), Is.True, reason);
        Assert.That(made, Is.Not.Empty);
        Assert.That(ShipmentBook.At(luna), Is.Not.Empty, "sealed at Luna for a freighter");
        Assert.That(made.Sum(m => m.Fee), Is.EqualTo(50m));
        Assert.That(new GameProjector(_game).ProjectEntity(luna, _player.Id).GetView<MarketView>()!.SameBodyColonyIds, Is.Empty);
    }

    [Test]
    public void SameBodyBuy_StillNeedsMutualTrade()
    {
        var bloc = Named("Euro Consortium");
        List(bloc, 1000, 5m);
        FactionStanceRules.SetGrant(Info(bloc), _player.Id, FactionGrants.Trade, false);
        Assert.That(ShipmentBook.TrySealMarket(_company, bloc, false, new List<(ICargoable, long)> { (Iron(bloc), 100) }, 0m,
            out _, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo("Trade is not mutual."));
    }

    [Test]
    public void OwnColonyMove_OnOneBody_IsInstant_AndOtherBodiesAreRefused()
    {
        var species = _player.GetDataBlob<FactionInfoDB>().Species[0];
        var second = ColonyFactory.CreateColony(_player, species, _earthBody);
        var iron = Iron(_company);
        second.GetDataBlob<CargoStorageDB>().TypeStores[iron.CargoTypeID] = new TypeStore(1_000_000_000);
        long before = MarketBook.Stock(_company, iron);

        var translator = new CommandTranslator(_game);
        var ok = translator.Translate(_player, _company,
            new MoveGoodsSameBodyCommand(_company.Id, second.Id, new[] { new CargoTransferItem(iron.ID, 250) }));
        Assert.That(ok.Accepted, Is.True, ok.RejectionReason);
        Assert.That(MarketBook.Stock(second, iron), Is.EqualTo(250));
        Assert.That(MarketBook.Stock(_company, iron), Is.EqualTo(before - 250));

        var mars = Named("Mars HQ");
        var refused = translator.Translate(_player, _company,
            new MoveGoodsSameBodyCommand(_company.Id, mars.Id, new[] { new CargoTransferItem(iron.ID, 1) }));
        Assert.That(refused.Accepted, Is.False);
        Assert.That(refused.RejectionReason, Is.EqualTo("The colonies are not on the same body."));
        var foreign = translator.Translate(_player, _company,
            new MoveGoodsSameBodyCommand(_company.Id, Named("Americas Compact").Id, new[] { new CargoTransferItem(iron.ID, 1) }));
        Assert.That(foreign.Accepted, Is.False, "a bloc colony is not ours");
    }
}
