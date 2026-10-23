using System.Linq;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;

namespace Pulsar4X.Tests;

[TestFixture]
public class ConstructionAtbTests
{
    [Test]
    public void Level1Office_HasFiveInstallationPoints()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);

        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        var faction = FactionFactory.CreateBasicFaction(game, "Office", "OFF", 0);
        var info = faction.GetDataBlob<FactionInfoDB>();
        info.Data.Unlock("local-construction");
        info.Data.Unlock("iron");
        info.Data.Unlock("aluminium");
        info.Data.Unlock("copper");
        info.Data.Unlock("plastic");
        info.Data.Unlock("stainless-steel");

        ComponentDesigner.StartResearched = true;
        try
        {
            var design = ComponentDesignFromJson.Create(
                faction,
                info.Data,
                game.StartingGameData.ComponentDesigns["default-design-local-construction"]);

            var atb = design.GetAttribute<ConstructionAtb>();
            Assert.That(atb.ConstructionPoints["installation"], Is.EqualTo(5));
            Assert.That(atb.ConstructionPoints.ContainsKey("ship-refit"), Is.False);
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }

    [Test]
    public void BasemodFaction_HasTheConstructionParts()
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
        ColonyFactory.CreateFromBlueprint(game, player, species, sol, earth, store.Colonies["colony-earth"]);

        var info = player.GetDataBlob<FactionInfoDB>();
        var office = info.ComponentDesigns["default-design-local-construction"].GetAttribute<ConstructionAtb>();
        Assert.That(office.ConstructionPoints.Keys, Is.EquivalentTo(new[] { "installation" }));
        Assert.That(office.ConstructionPoints["installation"], Is.EqualTo(5));

        var module = info.ComponentDesigns["default-design-ship-construction"].GetAttribute<ConstructionAtb>();
        Assert.That(module.ConstructionPoints["installation"], Is.EqualTo(5));
        Assert.That(module.ConstructionPoints.ContainsKey("ship-refit"), Is.False);

        var yard = info.ComponentDesigns["default-design-shipyard"];
        var refit = yard.GetAttribute<ConstructionAtb>();
        Assert.That(refit.ConstructionPoints.Keys, Is.EquivalentTo(new[] { "ship-refit" }));
        Assert.That(refit.ConstructionPoints["ship-refit"], Is.EqualTo(5));
        var industry = yard.GetAttribute<IndustryAtb>();
        Assert.That(industry.IndustryPoints.Keys, Is.EquivalentTo(new[] { "component-construction", "ship-assembly" }));

        var colonyShip = info.ShipDesigns["default-ship-design-colony-ship"];
        Assert.That(colonyShip.Components.Select(part => part.design.UniqueID), Does.Contain("default-design-ship-construction"));
    }
}
