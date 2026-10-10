using System.Linq;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class CeresDepotTests
{
    [Test]
    public void PlacedColony_HasMarket_Fuel_Refinery_Storage_AndAnAdministrator()
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
        var ceres = NameLookup.GetFirstEntityWithName(sol, "Ceres");
        var colony = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<ColonyInfoDB>().PlanetEntity == ceres);

        Assert.That(colony.GetDataBlob<NameDB>().DefaultName, Is.EqualTo("Ceres Depot"));
        var owner = game.Factions[colony.FactionOwnerID];
        Assert.That(owner.GetDataBlob<NameDB>().DefaultName, Is.EqualTo("Ceres Depot"));
        Assert.That(owner.Id, Is.Not.EqualTo(player.Id));
        Assert.That(FactionStanceRules.CanTrade(game, player.Id, owner.Id), Is.True);

        Assert.That(colony.HasDataBlob<LogiBaseDB>(), Is.True);

        var stores = colony.GetDataBlob<CargoStorageDB>().TypeStores;
        Assert.That(stores["general-storage"].MaxVolume, Is.GreaterThan(0));
        Assert.That(stores["fuel-storage"].MaxVolume, Is.EqualTo(6000));

        Assert.That(
            colony.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values
                .Any(line => line.IndustryTypeRates.ContainsKey("refining")),
            Is.True);

        var seat = colony.GetDataBlob<AdminSpaceDB>().CommanderSeats.Single();
        Assert.That(sol.TryGetEntityById(seat.CommanderID, out var admin), Is.True);
        Assert.That(admin!.FactionOwnerID, Is.EqualTo(owner.Id));
        Assert.That(admin.GetDataBlob<CommanderDB>().Type, Is.EqualTo(CommanderTypes.Civilian));
        Assert.That(admin.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(colony.Id));
        Assert.That(owner.GetDataBlob<FactionInfoDB>().Commanders, Does.Contain(admin));
    }
}
