using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.JumpPoints;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests;

[TestFixture]
public class CeresSurveyTests
{
    [Test]
    public void PlacedFaction_HasOneGeoSurveyShip_AndNoColony()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);

        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);

        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        player.GetDataBlob<FactionInfoDB>().KnownSystems.Add("system-sol");

        ColonyFactory.PlaceOwnedColonies(game, store, player, store.Species["species-human"]);
        FactionFactory.PlaceFactions(game, store);

        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var ceres = NameLookup.GetFirstEntityWithName(sol, "Ceres");

        var owner = game.Factions.Values.Single(f => f.GetDataBlob<NameDB>().DefaultName == "Strata Survey");
        Assert.That(owner.GetDataBlob<FactionInfoDB>().Colonies, Is.Empty);
        Assert.That(FactionStanceRules.CanTrade(game, player.Id, owner.Id), Is.True);

        var luna = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<NameDB>().DefaultName == "Luna Concord");
        Assert.That(FactionStanceRules.CanTrade(game, luna.FactionOwnerID, owner.Id), Is.True);
        Assert.That(owner.GetDataBlob<FactionInfoDB>().Stances.ContainsKey(game.GameMasterFaction.Id), Is.False);

        var ships = sol.GetAllEntitiesWithDataBlob<ShipInfoDB>()
            .Where(s => s.FactionOwnerID == owner.Id)
            .ToList();
        Assert.That(ships, Has.Count.EqualTo(1));

        var ship = ships[0];
        Assert.That(ship.GetDataBlob<NameDB>().GetName(owner.Id), Is.EqualTo("Pathfinder"));
        Assert.That(ship.GetDataBlob<GeoSurveyAbilityDB>().Speed, Is.EqualTo(2u));
        Assert.That(ship.HasDataBlob<JPSurveyAbilityDB>(), Is.False);
        Assert.That(ship.GetDataBlob<PositionDB>().Parent, Is.EqualTo(ceres));

        var info = ship.GetDataBlob<ShipInfoDB>();
        Assert.That(sol.TryGetEntityById(info.CommanderID, out var captain), Is.True);
        Assert.That(captain!.FactionOwnerID, Is.EqualTo(owner.Id));
        Assert.That(captain.GetDataBlob<CommanderDB>().Type, Is.EqualTo(CommanderTypes.Navy));
        Assert.That(owner.GetDataBlob<FactionInfoDB>().Commanders, Does.Contain(captain));

        var fleet = sol.GetAllEntitiesWithDataBlob<FleetDB>()
            .Single(f => f.FactionOwnerID == owner.Id);
        Assert.That(fleet.GetDataBlob<NameDB>().DefaultName, Is.EqualTo("Survey Flight"));
        Assert.That(fleet.GetDataBlob<FleetDB>().FlagShipID, Is.EqualTo(ship.Id));
        Assert.That(fleet.GetDataBlob<FleetDB>().GetChildren(), Is.EquivalentTo(new[] { ship }));
    }
}
