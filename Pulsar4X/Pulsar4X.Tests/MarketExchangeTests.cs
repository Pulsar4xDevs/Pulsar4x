using System.Collections.Generic;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class MarketExchangeTests
{
    [Test]
    public void SameFactionBuy_MovesCargo_AndDoesNotTouchTheLedger()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, shipFunds: 500);
        int ledgerBefore = scene.ShipInfo.Money.GetAllTransactions().Count;
        decimal fundsBefore = scene.ShipInfo.Money.GetCurrentFunds();

        var action = scene.Exchange(MarketSide.BuyFromMarket, 12);
        Assert.That(action.IsFinished(), Is.False);

        scene.Run(action);

        Assert.That(action.IsFinished(), Is.True);
        Assert.That(action.Status, Is.Not.EqualTo(ActionStatus.Failed));
        Assert.That(MarketBook.Stock(scene.Ship, scene.ShipIron), Is.EqualTo(12));
        Assert.That(MarketBook.Stock(scene.Market, scene.MarketIron), Is.EqualTo(28));
        Assert.That(scene.Listing.SellQuantity, Is.EqualTo(18));
        Assert.That(scene.ShipInfo.Money.GetCurrentFunds(), Is.EqualTo(fundsBefore));
        Assert.That(scene.ShipInfo.Money.GetAllTransactions().Count, Is.EqualTo(ledgerBefore));
        Assert.That(scene.ShipInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade), Is.Empty);
        Assert.That(scene.Ship.GetDataBlob<ActionQueueDB>().ActionList, Is.Empty);
        Assert.That(scene.Ship.HasDataBlob<CargoTransferDB>(), Is.False);
    }

    [Test]
    public void FriendlyBuy_PaysTheAsk()
    {
        var scene = Scene.Build(sameFaction: false, mutualFriendly: true, marketStock: 20, sell: 20, ask: 10, shipFunds: 1000);

        var action = scene.Exchange(MarketSide.BuyFromMarket, 4);
        scene.Run(action);

        Assert.That(MarketBook.Stock(scene.Ship, scene.ShipIron), Is.EqualTo(4));
        Assert.That(MarketBook.Stock(scene.Market, scene.MarketIron), Is.EqualTo(16));
        Assert.That(scene.Listing.SellQuantity, Is.EqualTo(16));
        Assert.That(scene.ShipInfo.Money.GetCurrentFunds(), Is.EqualTo(1000 - 40));
        Assert.That(scene.MarketInfo.Money.GetCurrentFunds(), Is.EqualTo(40));

        var paid = scene.ShipInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade);
        var received = scene.MarketInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade);
        Assert.That(paid, Has.Count.EqualTo(1));
        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(paid[0].Amount, Is.EqualTo(-40));
        Assert.That(received[0].Amount, Is.EqualTo(40));
        Assert.That(paid[0].Description, Does.Contain("iron").And.Contain("4").And.Contain(scene.MarketFaction.Id.ToString()));
        Assert.That(received[0].Description, Does.Contain("iron").And.Contain("4").And.Contain(scene.ShipFaction.Id.ToString()));
    }

    [Test]
    public void SellToMarket_PaysTheBidFromTheMarketFaction()
    {
        var scene = Scene.Build(
            sameFaction: false,
            mutualFriendly: true,
            marketStock: 0,
            shipStock: 15,
            sell: 0,
            buy: 10,
            bid: 12,
            marketFunds: 500);

        var action = scene.Exchange(MarketSide.SellToMarket, 6);
        scene.Run(action);

        Assert.That(MarketBook.Stock(scene.Ship, scene.ShipIron), Is.EqualTo(9));
        Assert.That(MarketBook.Stock(scene.Market, scene.MarketIron), Is.EqualTo(6));
        Assert.That(scene.Listing.BuyQuantity, Is.EqualTo(4));
        Assert.That(scene.MarketInfo.Money.GetCurrentFunds(), Is.EqualTo(500 - 72));
        Assert.That(scene.ShipInfo.Money.GetCurrentFunds(), Is.EqualTo(72));
        Assert.That(scene.MarketInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade)[0].Amount, Is.EqualTo(-72));
        Assert.That(scene.ShipInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade)[0].Amount, Is.EqualTo(72));
    }

    [Test]
    public void Hostile_SetsTheMessage_AndChangesNothing()
    {
        var scene = Scene.Build(sameFaction: false, mutualFriendly: false, marketStock: 20, sell: 20, ask: 10, shipFunds: 200);
        decimal buyer = scene.ShipInfo.Money.GetCurrentFunds();
        decimal seller = scene.MarketInfo.Money.GetCurrentFunds();

        var action = scene.Exchange(MarketSide.BuyFromMarket, 5);
        scene.Run(action);

        Assert.That(action.Details, Is.EqualTo("Cannot trade"));
        Assert.That(action.IsFinished(), Is.True);
        Assert.That(MarketBook.Stock(scene.Market, scene.MarketIron), Is.EqualTo(20));
        Assert.That(MarketBook.Stock(scene.Ship, scene.ShipIron), Is.EqualTo(0));
        Assert.That(scene.Listing.SellQuantity, Is.EqualTo(20));
        Assert.That(scene.ShipInfo.Money.GetCurrentFunds(), Is.EqualTo(buyer));
        Assert.That(scene.MarketInfo.Money.GetCurrentFunds(), Is.EqualTo(seller));
        Assert.That(scene.ShipInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade), Is.Empty);
        Assert.That(scene.MarketInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade), Is.Empty);
    }

    [Test]
    public void OutOfRange_SetsTheMessage_AndChangesNothing()
    {
        var scene = Scene.Build(sameFaction: false, mutualFriendly: true, inRange: false, marketStock: 20, sell: 20, ask: 10, shipFunds: 200);
        decimal buyer = scene.ShipInfo.Money.GetCurrentFunds();

        var action = scene.Exchange(MarketSide.BuyFromMarket, 5);
        scene.Run(action);

        Assert.That(action.Details, Is.EqualTo("Out of range"));
        Assert.That(MarketBook.Stock(scene.Market, scene.MarketIron), Is.EqualTo(20));
        Assert.That(MarketBook.Stock(scene.Ship, scene.ShipIron), Is.EqualTo(0));
        Assert.That(scene.Listing.SellQuantity, Is.EqualTo(20));
        Assert.That(scene.ShipInfo.Money.GetCurrentFunds(), Is.EqualTo(buyer));
        Assert.That(scene.MarketInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade), Is.Empty);
    }

    [Test]
    public void MovedAmount_IsTheMinimumOfTheOffer_Sellable_AndFunds()
    {
        var byOffer = Scene.Build(marketStock: 100, sell: 4, ask: 10);
        byOffer.Run(byOffer.Exchange(MarketSide.BuyFromMarket, 50));
        Assert.That(MarketBook.Stock(byOffer.Ship, byOffer.ShipIron), Is.EqualTo(4));
        Assert.That(byOffer.Listing.SellQuantity, Is.EqualTo(0));

        var bySellable = Scene.Build(marketStock: 10, sell: 100, reserve: 7, ask: 10);
        bySellable.Run(bySellable.Exchange(MarketSide.BuyFromMarket, 50));
        Assert.That(MarketBook.Stock(bySellable.Ship, bySellable.ShipIron), Is.EqualTo(3));
        Assert.That(MarketBook.Stock(bySellable.Market, bySellable.MarketIron), Is.EqualTo(7));
        Assert.That(bySellable.Listing.SellQuantity, Is.EqualTo(97));

        var byFunds = Scene.Build(sameFaction: false, mutualFriendly: true, marketStock: 100, sell: 100, ask: 10, shipFunds: 25);
        byFunds.Run(byFunds.Exchange(MarketSide.BuyFromMarket, 50));
        Assert.That(MarketBook.Stock(byFunds.Ship, byFunds.ShipIron), Is.EqualTo(2));
        Assert.That(byFunds.ShipInfo.Money.GetCurrentFunds(), Is.EqualTo(5));
        Assert.That(byFunds.MarketInfo.Money.GetCurrentFunds(), Is.EqualTo(20));

        var iron = Scene.Build(marketStock: 0, sell: 0).ShipIron;
        long roomFor = 3;
        var bySpace = Scene.Build(marketStock: 100, sell: 100, ask: 10, shipVolume: iron.VolumePerUnit * roomFor);
        long space = bySpace.Ship.GetDataBlob<CargoStorageDB>().GetFreeUnitSpace(bySpace.ShipIron);
        Assert.That(space, Is.GreaterThan(0).And.LessThan(50));
        bySpace.Run(bySpace.Exchange(MarketSide.BuyFromMarket, 50));
        Assert.That(MarketBook.Stock(bySpace.Ship, bySpace.ShipIron), Is.EqualTo(space));
    }

    [Test]
    public void NoListing_AndUnknownGood_ChangeNothing()
    {
        var unlisted = Scene.Build(list: false, marketStock: 10);
        var missing = unlisted.Exchange(MarketSide.BuyFromMarket, 1);
        unlisted.Run(missing);
        Assert.That(missing.Details, Is.EqualTo("No listing"));
        Assert.That(MarketBook.Stock(unlisted.Market, unlisted.MarketIron), Is.EqualTo(10));

        var unknown = Scene.Build(unlockShip: false, sell: 10);
        var action = unknown.Exchange(MarketSide.BuyFromMarket, 1);
        unknown.Run(action);
        Assert.That(action.Details, Is.EqualTo("Unknown good"));
        Assert.That(unknown.ShipInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade), Is.Empty);
    }

    [Test]
    public void IsFinished_IsFalseBeforeExecute()
    {
        var action = new MarketExchangeAction();
        Assert.That(action.IsFinished(), Is.False);
    }

    sealed class Scene
    {
        public Entity ShipFaction = null!;
        public Entity MarketFaction = null!;
        public Entity Ship = null!;
        public Entity Market = null!;
        public ICargoable ShipIron = null!;
        public ICargoable MarketIron = null!;
        public FactionInfoDB ShipInfo = null!;
        public FactionInfoDB MarketInfo = null!;
        public MarketListing Listing = null!;

        public MarketExchangeAction Exchange(MarketSide side, long units)
            => MarketExchangeAction.Create(Ship, Market.Id, "iron", side, units);

        public void Run(MarketExchangeAction action)
        {
            Assert.That(action.IsValidCommand(Ship.Manager.Game), Is.True);
            action.Execute(Ship.Manager.ManagerSubpulses.StarSysDateTime);
        }

        public static Scene Build(
            bool sameFaction = true,
            bool mutualFriendly = false,
            bool inRange = true,
            bool list = true,
            bool unlockShip = true,
            bool unlockMarket = true,
            double shipVolume = 1000,
            double marketVolume = 1000,
            long marketStock = 0,
            long shipStock = 0,
            long sell = 0,
            decimal ask = 0,
            long buy = 0,
            decimal bid = 0,
            long reserve = 0,
            decimal shipFunds = 0,
            decimal marketFunds = 0)
        {
            var modLoader = new ModLoader();
            var store = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);

            var shipFaction = FactionFactory.CreateFaction(game, "Haulers");
            var marketFaction = sameFaction ? shipFaction : FactionFactory.CreateFaction(game, "Market");
            if (!sameFaction && mutualFriendly)
            {
                shipFaction.GetDataBlob<FactionInfoDB>().Stances[marketFaction.Id] = FactionStance.Friendly;
                marketFaction.GetDataBlob<FactionInfoDB>().Stances[shipFaction.Id] = FactionStance.Friendly;
            }

            var shipInfo = shipFaction.GetDataBlob<FactionInfoDB>();
            var marketInfo = marketFaction.GetDataBlob<FactionInfoDB>();
            if (unlockShip)
                shipInfo.Data.Unlock("iron");
            if (!sameFaction && unlockMarket)
                marketInfo.Data.Unlock("iron");

            ICargoable? shipIron = shipInfo.Data.CargoGoods.Contains("iron") ? shipInfo.Data.CargoGoods.GetAny("iron") : null;
            ICargoable? marketIron = marketInfo.Data.CargoGoods.Contains("iron") ? marketInfo.Data.CargoGoods.GetAny("iron") : null;

            var system = new StarSystem();
            system.Initialize(game, "Exchange", -1);
            var star = Entity.Create();
            system.AddEntity(star, new List<BaseDataBlob>
            {
                new PositionDB(0, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
            });

            var here = new Vector3(1.496e11, 0, 0);
            var far = new Vector3(3.0e11, 0, 0);

            var market = Entity.Create(marketFaction.Id);
            var marketStore = new CargoStorageDB("general-storage", marketVolume);
            system.AddEntity(market, new List<BaseDataBlob>
            {
                marketStore,
                new LogiBaseDB { Capacity = 5 },
                new PositionDB(here, star),
                new ActionQueueDB(),
            });

            var ship = Entity.Create(shipFaction.Id);
            var shipStore = new CargoStorageDB("general-storage", shipVolume);
            system.AddEntity(ship, new List<BaseDataBlob>
            {
                shipStore,
                new PositionDB(inRange ? here : far, star),
                new ActionQueueDB(),
            });

            if (marketStock > 0)
                Assert.That(marketStore.AddCargoByUnit(marketIron!, marketStock), Is.EqualTo(marketStock));
            if (shipStock > 0)
                Assert.That(shipStore.AddCargoByUnit(shipIron!, shipStock), Is.EqualTo(shipStock));

            MarketListing? listing = null;
            if (list)
            {
                Assert.That(MarketBook.SetListing(market, new MarketListing
                {
                    CargoId = "iron",
                    SellQuantity = sell,
                    Ask = ask,
                    BuyQuantity = buy,
                    Bid = bid,
                    Reserve = reserve,
                }), Is.True);
                Assert.That(MarketBook.TryGet(market, "iron", out listing), Is.True);
            }

            if (shipFunds > 0)
                shipInfo.Money.AddIncome(game.TimePulse.GameGlobalDateTime, TransactionCategory.InitialInvestment, "test", shipFunds);
            if (marketFunds > 0 && !sameFaction)
                marketInfo.Money.AddIncome(game.TimePulse.GameGlobalDateTime, TransactionCategory.InitialInvestment, "test", marketFunds);

            return new Scene
            {
                ShipFaction = shipFaction,
                MarketFaction = marketFaction,
                Ship = ship,
                Market = market,
                ShipIron = shipIron!,
                MarketIron = marketIron!,
                ShipInfo = shipInfo,
                MarketInfo = marketInfo,
                Listing = listing!,
            };
        }
    }
}
