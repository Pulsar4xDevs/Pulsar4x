using System.Linq;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;

namespace Pulsar4X.Tests;

[TestFixture]
public class MarsColonyTests
{
    [Test]
    public void PlayerMarsOutpost_IsOwnedByTheStartFaction_AndSupported()
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
        player.GetDataBlob<FactionInfoDB>().Species.Add(species);

        var earth = NameLookup.GetFirstEntityWithName(sol, "Earth");
        ColonyFactory.CreateFromBlueprint(
            game, player, species, sol, earth, store.Colonies["colony-earth"]);
        ColonyFactory.PlacePlayerColonies(game, store, player, species, "colony-earth");

        var mars = NameLookup.GetFirstEntityWithName(sol, "Mars");
        var colony = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<ColonyInfoDB>().PlanetEntity == mars);

        Assert.That(colony.FactionOwnerID, Is.EqualTo(player.Id));
        Assert.That(colony.GetDataBlob<NameDB>().DefaultName, Is.EqualTo("Mars HQ"));
        Assert.That(colony.GetDataBlob<ColonyInfoDB>().Population[species.Id], Is.EqualTo(8000));
        Assert.That(player.GetDataBlob<FactionInfoDB>().Colonies, Does.Contain(colony));

        var infra = colony.GetDataBlob<InfrastructureDB>();
        Assert.That(infra.CapacityProvided, Is.GreaterThan(0));
        Assert.That(infra.Efficiency, Is.EqualTo(1.0));
        Assert.That(
            colony.GetDataBlob<ComponentInstancesDB>().GetPopulationSupportValue(mars),
            Is.GreaterThanOrEqualTo(8000));

        var techs = player.GetDataBlob<FactionInfoDB>().Data.Techs;
        Assert.That(techs["tech-infra-gravity-min-extension"].Level, Is.EqualTo(3));
        Assert.That(techs["tech-infra-pressure-min-extension"].Level, Is.EqualTo(1));
    }
}
