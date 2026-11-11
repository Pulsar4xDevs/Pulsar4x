using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameEngine.Engine.Orders;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Storage;
using NameDB = Pulsar4X.Names.NameDB;

namespace Pulsar4X.Tests;

[TestFixture]
public class MarketHistoryTests
{
    [Test]
    public void BuyFromListing_RecordsOneBucketAtAsk()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 7);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 12));

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Open, Is.EqualTo(10m));
        Assert.That(bucket.High, Is.EqualTo(10m));
        Assert.That(bucket.Low, Is.EqualTo(10m));
        Assert.That(bucket.Close, Is.EqualTo(10m));
        Assert.That(bucket.Volume, Is.EqualTo(12));
        Assert.That(bucket.Bid, Is.EqualTo(7m));
        Assert.That(bucket.Ask, Is.EqualTo(10m));
        Assert.That(bucket.Stock, Is.EqualTo(28));
    }

    [Test]
    public void SellToListing_RecordsAtBid()
    {
        var scene = Scene.Build(marketStock: 0, shipStock: 15, sell: 0, buy: 10, bid: 12);
        scene.Run(scene.Exchange(MarketSide.SellToMarket, 6));

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Open, Is.EqualTo(12m));
        Assert.That(bucket.High, Is.EqualTo(12m));
        Assert.That(bucket.Low, Is.EqualTo(12m));
        Assert.That(bucket.Close, Is.EqualTo(12m));
        Assert.That(bucket.Volume, Is.EqualTo(6));
        Assert.That(bucket.Bid, Is.EqualTo(12m));
        Assert.That(bucket.Ask, Is.EqualTo(0m));
        Assert.That(bucket.Stock, Is.EqualTo(6));
    }

    [Test]
    public void TwoTradesSameDay_OpenFirstCloseLast_VolumeSummed()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 4));
        Assert.That(MarketBook.SetListing(scene.Market, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 26,
            Ask = 15,
        }), Is.True);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 2));

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Open, Is.EqualTo(10m));
        Assert.That(bucket.Close, Is.EqualTo(15m));
        Assert.That(bucket.High, Is.EqualTo(15m));
        Assert.That(bucket.Low, Is.EqualTo(10m));
        Assert.That(bucket.Volume, Is.EqualTo(6));
    }

    [Test]
    public void TradeOnTheNextDay_StartsANewBucket()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 4));
        var day0 = scene.Market.StarSysDateTime.Date;

        AdvanceDays(scene.Market, 1);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 3));

        var days = Days(scene, "iron");
        Assert.That(days, Has.Count.EqualTo(2));
        Assert.That(days[0].Day, Is.EqualTo(day0));
        Assert.That(days[0].Volume, Is.EqualTo(4));
        Assert.That(days[0].Close, Is.EqualTo(10m));
        Assert.That(days[1].Day, Is.EqualTo(day0.AddDays(1)));
        Assert.That(days[1].Volume, Is.EqualTo(3));
        Assert.That(days[1].Open, Is.EqualTo(10m));
    }

    [Test]
    public void SetListing_QuoteOnlyDay_VolumeZero()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 7);
        Assert.That(MarketBook.SetListing(scene.Market, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 30,
            Ask = 11,
            Bid = 8,
        }), Is.True);

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Volume, Is.EqualTo(0));
        Assert.That(bucket.Bid, Is.EqualTo(8m));
        Assert.That(bucket.Ask, Is.EqualTo(11m));
        Assert.That(bucket.Stock, Is.EqualTo(40));
    }

    [Test]
    public void CapTrimming_DropsOldestBeyondMaxDays()
    {
        var scene = Scene.Build(list: false, marketStock: 10);
        var start = scene.Market.StarSysDateTime.Date;
        int days = MarketHistory.MaxDays + 5;
        for (int i = 0; i < days; i++)
        {
            if (i > 0)
                AdvanceDays(scene.Market, 1);
            Assert.That(MarketBook.SetListing(scene.Market, new MarketListing
            {
                CargoId = "iron",
                SellQuantity = 1,
                Ask = 1,
            }), Is.True);
        }

        var kept = Days(scene, "iron");
        Assert.That(kept, Has.Count.EqualTo(MarketHistory.MaxDays));
        Assert.That(kept[0].Day, Is.EqualTo(start.AddDays(5)));
        Assert.That(kept[kept.Count - 1].Day, Is.EqualTo(start.AddDays(days - 1)));
    }

    [Test]
    public void CloneAndJson_PreserveHistory_RemoveListingKeepsIt()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 7);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 12));
        var original = scene.Market.GetDataBlob<LogiBaseDB>();
        Assert.That(original.History.ContainsKey("iron"), Is.True);
        long volume = original.History["iron"][0].Volume;
        decimal close = original.History["iron"][0].Close;
        DateTime day = original.History["iron"][0].Day;

        var clone = (LogiBaseDB)original.Clone();
        Assert.That(clone.History.ContainsKey("iron"), Is.True);
        Assert.That(clone.History["iron"], Has.Count.EqualTo(1));
        Assert.That(clone.History["iron"][0].Volume, Is.EqualTo(volume));
        clone.History["iron"][0].Volume = 0;
        clone.History["iron"][0].Close = 0;
        Assert.That(original.History["iron"][0].Volume, Is.EqualTo(volume));
        Assert.That(original.History["iron"][0].Close, Is.EqualTo(close));

        MarketBook.RemoveListing(scene.Market, "iron");
        Assert.That(MarketBook.TryGet(scene.Market, "iron", out _), Is.False);
        Assert.That(original.History.ContainsKey("iron"), Is.True);
        Assert.That(original.History["iron"][0].Volume, Is.EqualTo(volume));

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            TypeNameHandling = TypeNameHandling.Objects,
            ContractResolver = new NonPublicResolver(),
        };
        string json = JsonConvert.SerializeObject(original, settings);
        var loaded = JsonConvert.DeserializeObject<LogiBaseDB>(json, settings);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.History, Is.Not.Null);
        Assert.That(loaded.History.ContainsKey("iron"), Is.True);
        Assert.That(loaded.History["iron"], Has.Count.EqualTo(1));
        Assert.That(loaded.History["iron"][0].Volume, Is.EqualTo(volume));
        Assert.That(loaded.History["iron"][0].Close, Is.EqualTo(close));
        Assert.That(loaded.History["iron"][0].Day, Is.EqualTo(day));
        Assert.That(loaded.History["iron"][0].Stock, Is.EqualTo(28));
    }

    [Test]
    public void RelocationListing_NeverCreatesHistory()
    {
        var scene = Scene.Build(list: false, marketStock: 20, sell: 0, ask: 0);
        Assert.That(MarketBook.SetListing(scene.Market, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 20,
            Ask = 10,
            Relocation = true,
        }), Is.True);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 5));

        var book = scene.Market.GetDataBlob<LogiBaseDB>();
        Assert.That(book.History, Is.Empty);
        Assert.That(MarketBook.Stock(scene.Ship, scene.ShipIron), Is.EqualTo(5));
    }

    [Test]
    public void ShipmentSeal_RecordsTradeAtListingPriceOnTheHolder()
    {
        var scene = Scene.Build(
            sameFaction: false,
            mutualFriendly: true,
            marketStock: 100,
            sell: 100,
            ask: 10,
            bid: 0);
        var buyer = scene.ExtraOffice(scene.ShipFaction, funds: 1000);

        Assert.That(ShipmentBook.TrySealMarket(
            buyer, scene.Market, false,
            new List<(ICargoable, long)> { (scene.MarketIron, 30) },
            0m, out _, out var reason), Is.True, reason);

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Open, Is.EqualTo(10m));
        Assert.That(bucket.High, Is.EqualTo(10m));
        Assert.That(bucket.Low, Is.EqualTo(10m));
        Assert.That(bucket.Close, Is.EqualTo(10m));
        Assert.That(bucket.Volume, Is.EqualTo(30));
        Assert.That(bucket.Ask, Is.EqualTo(10m));
        Assert.That(bucket.Stock, Is.EqualTo(70));
    }

    static PriceBucket SoleBucket(Scene scene, string cargoId)
    {
        var days = Days(scene, cargoId);
        Assert.That(days, Has.Count.EqualTo(1));
        return days[0];
    }

    static List<PriceBucket> Days(Scene scene, string cargoId)
    {
        var book = scene.Market.GetDataBlob<LogiBaseDB>();
        Assert.That(book.History, Is.Not.Null);
        Assert.That(book.History.ContainsKey(cargoId), Is.True);
        return book.History[cargoId];
    }

    static void AdvanceDays(Entity office, int days)
    {
        var pulse = office.Manager.ManagerSubpulses;
        var field = typeof(ManagerSubPulse).GetField("_systemLocalDateTime", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field!.SetValue(pulse, office.StarSysDateTime.AddDays(days));
    }

    sealed class Scene
    {
        public Game Game = null!;
        public StarSystem System = null!;
        public Entity Star = null!;
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

        public Entity ExtraOffice(Entity faction, decimal funds = 0)
        {
            var office = Entity.Create(faction.Id);
            var store = new CargoStorageDB("general-storage", 1000);
            System.AddEntity(office, new List<BaseDataBlob>
            {
                store,
                new LogiBaseDB { Capacity = 5 },
                new PositionDB(new Vector3(1.496e11, 0, 0), Star),
                new ActionQueueDB(),
            });
            if (funds > 0)
            {
                var info = faction.GetDataBlob<FactionInfoDB>();
                info.Money.AddIncome(Game.TimePulse.GameGlobalDateTime, TransactionCategory.InitialInvestment, "test", funds);
            }
            return office;
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
            modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);

            var shipFaction = FactionFactory.CreateFaction(game, "Haulers");
            var marketFaction = sameFaction ? shipFaction : FactionFactory.CreateFaction(game, "Market");
            var shipInfo = shipFaction.GetDataBlob<FactionInfoDB>();
            var marketInfo = marketFaction.GetDataBlob<FactionInfoDB>();
            if (!sameFaction && mutualFriendly)
            {
                shipInfo.Stances[marketFaction.Id] = FactionStance.Friendly;
                marketInfo.Stances[shipFaction.Id] = FactionStance.Friendly;
                FactionStanceRules.SetGrant(shipInfo, marketFaction.Id, FactionGrants.Trade, true);
                FactionStanceRules.SetGrant(marketInfo, shipFaction.Id, FactionGrants.Trade, true);
            }
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
                Game = game,
                System = system,
                Star = star,
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

[TestFixture]
public class MarketHistoryApiTests : ApiTestBase
{
    [Test]
    public void ProjectEntity_OwnerSeesHistory_ViewerWithoutGrantDoesNot_EmptyIsNull_CacheReused()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        OfficeWithIron(colony, session.FactionId, stock: 250);

        Assert.That(_projector.ProjectEntity(colony, session.FactionId).GetView<MarketHistoryView>(), Is.Null);

        Assert.That(MarketBook.SetListing(colony, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 150,
            Ask = 11m,
            BuyQuantity = 40,
            Bid = 7m,
            Reserve = 100,
        }), Is.True);

        var owned = _projector.ProjectEntity(colony, session.FactionId);
        var history = owned.GetView<MarketHistoryView>();
        Assert.That(history, Is.Not.Null);
        Assert.That(history!.Goods, Has.Count.EqualTo(1));
        Assert.That(history.Goods[0].CargoId, Is.EqualTo("iron"));
        Assert.That(history.Goods[0].Days, Has.Count.EqualTo(1));
        var point = history.Goods[0].Days[0];
        Assert.That(point.Volume, Is.EqualTo(0));
        Assert.That(point.Ask, Is.EqualTo(11m));
        Assert.That(point.Bid, Is.EqualTo(7m));
        Assert.That(point.Stock, Is.EqualTo(250));

        var again = _projector.ProjectEntity(colony, session.FactionId).GetView<MarketHistoryView>();
        Assert.That(again, Is.SameAs(history));

        var other = OtherFaction(session.FactionId);
        Assert.That(_projector.ProjectEntity(colony, other.Id).HasView<MarketHistoryView>(), Is.False);

        Assert.That(other.TryGetDataBlob<FactionInfoDB>(out var otherInfo), Is.True);
        otherInfo!.Stances[session.FactionId] = FactionStance.Friendly;
        FactionOf(session.FactionId).Stances[other.Id] = FactionStance.Friendly;
        Assert.That(_projector.ProjectEntity(colony, other.Id).HasView<MarketHistoryView>(), Is.False);

        FactionStanceRules.SetGrant(FactionOf(session.FactionId), other.Id, FactionGrants.Trade, true);
        var granted = _projector.ProjectEntity(colony, other.Id).GetView<MarketHistoryView>();
        Assert.That(granted, Is.SameAs(history));
    }

    void OfficeWithIron(Entity colony, int factionId, long stock)
    {
        var info = FactionOf(factionId);
        info.Data.Unlock("iron");
        var store = new CargoStorageDB("general-storage", 100000);
        colony.SetDataBlob(store);
        var iron = info.Data.CargoGoods.GetAny("iron");
        Assert.That(iron, Is.Not.Null);
        Assert.That(store.AddCargoByUnit(iron!, stock), Is.EqualTo(stock));
        colony.SetDataBlob(new LogiBaseDB { Capacity = 5 });
    }

    FactionInfoDB FactionOf(int factionId)
    {
        Assert.That(_game.Factions[factionId].TryGetDataBlob<FactionInfoDB>(out var info), Is.True);
        return info!;
    }

    Entity OtherFaction(int sessionFactionId)
    {
        return _game.Factions.Values.First(f =>
            f.Id != sessionFactionId
            && f.Id != _game.GameMasterFaction.Id
            && f.Id != Game.NeutralFactionId);
    }

    Entity MakeColony(int factionId)
    {
        var faction = _game.Factions[factionId];
        var species = SpeciesFactory.CreateSpeciesHuman(faction, _game.GlobalManager);
        var planet = _game.Systems[0].GetAllEntitiesWithDataBlob<SystemBodyInfoDB>()
            .First(b => b.HasDataBlob<MassVolumeDB>() && b.HasDataBlob<NameDB>());
        return ColonyFactory.CreateColony(faction, species, planet, initialPopulation: 9_000_000);
    }
}
