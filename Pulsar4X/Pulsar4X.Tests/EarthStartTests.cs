using System.Linq;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;

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
        Assert.That(book.Listings, Is.Empty);
    }
}
