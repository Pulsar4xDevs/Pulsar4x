using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class EarthStartTests
{
    [Test]
    public void QuickstartEarthColony_HasALogisticsOffice()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);

        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);

        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;

        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(sol, store.Species["species-human"]);
        species.FactionOwnerID = player.Id;

        var earth = NameLookup.GetFirstEntityWithName(sol, "Earth");
        var colony = ColonyFactory.CreateFromBlueprint(
            game, player, species, sol, earth, store.Colonies["colony-earth"]);

        Assert.That(colony.TryGetDataBlob<LogiBaseDB>(out var book), Is.True);
        Assert.That(book!.Capacity, Is.EqualTo(5));
        Assert.That(colony.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.OfferStock));
        Assert.That(colony.GetDataBlob<GoalsDB>().ActiveGoal!.Name, Is.EqualTo("Offer stock"));

        var expected = LargestPiles(colony, book.Capacity);
        Assert.That(expected, Does.Contain("iron"));
        Assert.That(book.Listings.Keys, Is.EquivalentTo(expected));
        foreach (var cargoId in expected)
        {
            Assert.That(book.Listings[cargoId].SellQuantity, Is.EqualTo(StockOf(colony, cargoId)));
            Assert.That(book.Listings[cargoId].BuyQuantity, Is.EqualTo(0));
            Assert.That(book.Listings[cargoId].Ask, Is.EqualTo(0));
        }
    }

    static List<string> LargestPiles(Entity colony, int capacity)
    {
        var piles = new List<(string Id, long Stock)>();
        foreach (var store in colony.GetDataBlob<CargoStorageDB>().TypeStores.Values)
        {
            foreach (var cargo in store.GetCargoables().Values)
            {
                long stock = MarketBook.Stock(colony, cargo);
                if (stock > 0 && !string.IsNullOrEmpty(cargo.UniqueID))
                    piles.Add((cargo.UniqueID, stock));
            }
        }
        piles.Sort((a, b) =>
        {
            int byStock = b.Stock.CompareTo(a.Stock);
            return byStock != 0 ? byStock : string.CompareOrdinal(a.Id, b.Id);
        });
        return piles.Take(capacity).Select(pile => pile.Id).ToList();
    }

    static long StockOf(Entity colony, string cargoId)
    {
        var cargo = colony.GetFactionCargoDefinitions()!.GetAny(cargoId);
        return MarketBook.Stock(colony, cargo!);
    }
}
