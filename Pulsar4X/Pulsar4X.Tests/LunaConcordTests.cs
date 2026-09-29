using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Sensors;

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

    [Test]
    public void PlacedColony_IsDetectedByEarthPassiveSensor()
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
        ColonyFactory.CreateFromBlueprint(
            game, player, species, sol, earth, store.Colonies["colony-earth"]);
        ColonyFactory.PlaceOwnedColonies(game, store, player, store.Species["species-human"]);

        var luna = NameLookup.GetFirstEntityWithName(sol, "Luna");
        var colony = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<ColonyInfoDB>().PlanetEntity == luna);

        Assert.That(colony.TryGetDataBlob<SensorProfileDB>(out var profile), Is.True);
        Assert.That(profile!.EmittedEMSpectra, Is.Not.Empty);
        Assert.That(profile.EmittedEMSpectra[0].Magnitude, Is.EqualTo(1e9).Within(1));
        Assert.That(profile.EmittedEMSpectra[0].WaveForm.WavelengthAverage_nm, Is.EqualTo(470).Within(0.01));

        game.PostNewGameInitialization();

        Assert.That(sol.IsEntityVisibleToFaction(colony, player.Id), Is.True);
        Assert.That(sol.GetSensorContacts(player.Id).SensorContactExists(colony.Id), Is.True);
    }
}
