using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Engine.Api;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;
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
        modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);

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
        Assert.That(book!.Capacity, Is.EqualTo(20));
        Assert.That(colony.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.OfferStock));
        Assert.That(colony.GetDataBlob<GoalsDB>().ActiveGoal!.Name, Is.EqualTo("Offer stock"));

        // Site office capacity is 20. OfferStock fills up to that many stocked piles (Earth
        // currently has fewer than 20 distinct stocked goods, so listings stay under capacity).
        Assert.That(book.Listings.Count, Is.GreaterThan(2).And.LessThanOrEqualTo(20));
        Assert.That(book.Listings.Keys, Does.Contain("stainless-steel"));
        foreach (var cargoId in book.Listings.Keys)
        {
            // Plant fuel keeps its runway back from the market (energy phase 2).
            long reserve = Pulsar4X.Energy.ColonyPowerProcessor.FuelReserveUnits(colony, cargoId);
            Assert.That(book.Listings[cargoId].SellQuantity, Is.EqualTo(System.Math.Max(0, StockOf(colony, cargoId) - reserve)));
            Assert.That(book.Listings[cargoId].BuyQuantity, Is.EqualTo(0));
            Assert.That(book.Listings[cargoId].Ask, Is.EqualTo(0));
        }

        long steel = StockOf(colony, "stainless-steel");
        var steelRow = colony.GetDataBlob<ColonyMarketPolicyDB>().Rows["stainless-steel"];
        Assert.That(steelRow.Max, Is.EqualTo(steel + 100));
        Assert.That(steelRow.AutoProduce, Is.False);
        Assert.That(steelRow.Bid, Is.EqualTo(0));
        var jobs = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values
            .SelectMany(line => line.Jobs)
            .ToList();
        Assert.That(jobs, Has.Count.EqualTo(1));
        Assert.That(jobs[0].ItemGuid, Is.EqualTo("stainless-steel"));
        Assert.That(jobs[0].NumberOrdered, Is.EqualTo(1));

        var hermes = colony.GetDataBlob<LaunchComplexDB>().LaunchQueue
            .Single(entry => entry.ShipName == "ISS Hermes");
        Assert.That(hermes.DesignId, Is.EqualTo("default-ship-design-freighter"));
        var express = sol.GetAllEntitiesWithDataBlob<ShipInfoDB>()
            .Single(ship => ship.GetDataBlob<NameDB>().GetName(player.Id) == "Planet Express Ship");
        Assert.That(express.GetDataBlob<ShipInfoDB>().Design.UniqueID, Is.EqualTo("default-ship-design-courier"));

        var stored = colony.GetDataBlob<CargoStorageDB>().TypeStores.Values
            .SelectMany(store => store.GetCargoables().Values)
            .ToList();
        var bays = stored.OfType<ComponentInstance>()
            .Where(instance => instance.Design.UniqueID == "default-design-ship-construction")
            .ToList();
        Assert.That(bays, Has.Count.EqualTo(1));
        Assert.That(stored.OfType<ComponentDesign>().Select(design => design.UniqueID),
            Does.Not.Contain("default-design-ship-construction"));
        Assert.That(stored.OfType<ComponentInstance>()
            .Count(instance => instance.Design.UniqueID == "default-design-merlin"), Is.EqualTo(5));

        var designs = player.GetDataBlob<FactionInfoDB>().ShipDesigns;
        var yard = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values
            .Single(line => line.IndustryTypeRates.ContainsKey("ship-assembly"));
        // The starting yard is sized a little above the Starship (Jon's call), not to fit every
        // starting design: larger hulls need slip research first.
        var starship = designs["default-ship-design-starship"];
        // 38 t since crew plan phase 4 gave the Starship a 2 t reactor and two more bunks (37.7 t).
        Assert.That(yard.MaxVolume, Is.EqualTo(38000));
        Assert.That(starship.MassPerUnit, Is.LessThanOrEqualTo(yard.MaxVolume));
        Assert.That(yard.MaxVolume, Is.LessThanOrEqualTo(starship.MassPerUnit * 1.15), "a modest margin, not a blanket allowance");
        Assert.That(designs["default-ship-design-tanker"].MassPerUnit, Is.GreaterThan(yard.MaxVolume),
            "larger starting hulls need slip research");

        // The starting yard is reproducible with starting research: the designer's slip range allows it.
        var info = player.GetDataBlob<FactionInfoDB>();
        var yardDesigner = new Pulsar4X.Components.ComponentDesigner(
            info.Data.ComponentTemplates["shipyard"], info.Data, player.GetDataBlob<Pulsar4X.Factions.FactionTechDB>());
        var slip = yardDesigner.ComponentDesignProperties["Slip Size"];
        slip.SetMax();
        Assert.That(slip.MaxValue, Is.GreaterThanOrEqualTo(yard.MaxVolume));

        var projected = new GameProjector(game).ProjectEntity(colony, player.Id).GetView<IndustryView>();
        var yardLine = projected!.ProductionLines.Single(line => line.IsShipyard);
        Assert.That(yardLine.RefitCargo.Select(part => part.DesignId), Does.Contain("default-design-ship-construction"));
        Assert.That(yardLine.RefitCargo.Where(part => part.DesignId == "default-design-ship-construction").Sum(part => part.Cost),
            Is.GreaterThan(0));
    }

    [Test]
    public void StartingInstallation_MissingDesign_ThrowsModLoadException()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);

        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);

        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(sol, store.Species["species-human"]);
        species.FactionOwnerID = player.Id;
        var earth = NameLookup.GetFirstEntityWithName(sol, "Earth");

        var blueprint = store.Colonies["colony-earth"];
        const string missingId = "missing-installation-design";
        blueprint.Installations = new List<ColonyBlueprint.StartingItemBlueprint>
        {
            new ColonyBlueprint.StartingItemBlueprint { Id = missingId, Amount = 1 }
        };

        var ex = Assert.Throws<ModLoadException>(() =>
            ColonyFactory.CreateFromBlueprint(game, player, species, sol, earth, blueprint));

        Assert.That(ex!.Errors[0].Field, Is.EqualTo("Installations"));
        Assert.That(ex.Errors[0].MissingId, Is.EqualTo(missingId));
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
