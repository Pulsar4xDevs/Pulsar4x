using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

[TestFixture]
public class LunaConcordTests
{
    [Test]
    public void PlacedColony_IsOnLuna_OwnedByAFriendlyFaction()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);

        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);

        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;

        ColonyFactory.PlaceOwnedColonies(game, store, player, store.Species["species-human"]);

        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var luna = NameLookup.GetFirstEntityWithName(sol, "Luna");
        var colony = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<ColonyInfoDB>().PlanetEntity == luna);

        Assert.That(colony.GetDataBlob<NameDB>().DefaultName, Is.EqualTo("Luna Concord"));
        Assert.That(colony.FactionOwnerID, Is.Not.EqualTo(player.Id));

        var owner = game.Factions[colony.FactionOwnerID];
        Assert.That(owner.GetDataBlob<NameDB>().DefaultName, Is.EqualTo("Luna Concord"));
        Assert.That(FactionStanceRules.CanTrade(game, player.Id, owner.Id), Is.True);
        Assert.That(player.GetDataBlob<FactionInfoDB>().Stances[owner.Id], Is.EqualTo(FactionStance.Friendly));
        Assert.That(owner.GetDataBlob<FactionInfoDB>().Stances[player.Id], Is.EqualTo(FactionStance.Friendly));
    }
}
