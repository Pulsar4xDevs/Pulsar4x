using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class MarketBookTests
{
    [Test]
    public void SetListing_RejectsANewIdPastCapacity_AndAcceptsAReplace()
    {
        var colony = Office(capacity: 1);

        Assert.That(MarketBook.SetListing(colony, Listing("iron", sell: 10, ask: 1)), Is.True);
        Assert.That(MarketBook.SetListing(colony, Listing("copper", sell: 1, ask: 1)), Is.False);
        Assert.That(colony.GetDataBlob<LogiBaseDB>().Listings.Keys, Is.EquivalentTo(new[] { "iron" }));

        Assert.That(MarketBook.SetListing(colony, Listing("iron", sell: 25, ask: 4)), Is.True);
        Assert.That(MarketBook.TryGet(colony, "iron", out var iron), Is.True);
        Assert.That(iron.SellQuantity, Is.EqualTo(25));
        Assert.That(iron.Ask, Is.EqualTo(4));
    }

    [Test]
    public void SetListing_RejectsNegatives_SellableSubtractsReserve_StockIsNotOnTheListing()
    {
        var colony = Office(capacity: 5);
        var mineral = new Mineral
        {
            UniqueID = "iron",
            CargoTypeID = "minerals",
            Name = "Iron",
            MassPerUnit = 1,
            VolumePerUnit = 1,
        };
        var storage = colony.GetDataBlob<CargoStorageDB>();
        storage.TypeStores["minerals"].CurrentStoreInUnits[mineral.ID] = 10;

        Assert.That(MarketBook.SetListing(colony, Listing("iron", sell: 1, ask: -1)), Is.False);
        Assert.That(MarketBook.SetListing(colony, Listing("iron", sell: -1, ask: 1)), Is.False);
        Assert.That(MarketBook.SetListing(colony, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 100,
            Ask = 0,
            Reserve = 4,
        }), Is.True);

        Assert.That(MarketBook.TryGet(colony, "iron", out var listing), Is.True);
        Assert.That(listing.SellQuantity, Is.EqualTo(100));
        Assert.That(MarketBook.Stock(colony, mineral), Is.EqualTo(10));
        Assert.That(MarketBook.Sellable(colony, listing, mineral), Is.EqualTo(6));
    }

    [Test]
    public void Clone_CopiesListings()
    {
        var colony = Office(capacity: 2);
        MarketBook.SetListing(colony, Listing("iron", sell: 8, ask: 3));

        var copy = (LogiBaseDB)colony.GetDataBlob<LogiBaseDB>().Clone();
        copy.Listings["iron"].SellQuantity = 1;

        Assert.That(colony.GetDataBlob<LogiBaseDB>().Listings["iron"].SellQuantity, Is.EqualTo(8));
    }

    [Test]
    public void LogisticsTick_DoesNotEnqueueAWarpOrder()
    {
        var ship = Office(capacity: 1);
        ship.SetDataBlob(new NewtonThrustAbilityDB("methalox"));
        ship.SetDataBlob(new ActionQueueDB());
        ship.SetDataBlob(new LogiShipperDB());

        new LogiBaseProcessor().ProcessEntity(ship, 0);
        new LogiShipProcessor().ProcessEntity(ship, 0);

        Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList, Is.Empty);
    }

    static MarketListing Listing(string id, long sell, decimal ask) => new()
    {
        CargoId = id,
        SellQuantity = sell,
        Ask = ask,
    };

    static Entity Office(int capacity)
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        var system = new StarSystem();
        system.Initialize(game, "Book", -1);
        var colony = Entity.Create();
        system.AddEntity(colony, new System.Collections.Generic.List<Pulsar4X.Datablobs.BaseDataBlob>
        {
            new CargoStorageDB("minerals", 1000),
            new LogiBaseDB { Capacity = capacity },
        });
        return colony;
    }
}
