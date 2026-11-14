using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>
/// Multi-faction Earth, phase 0: the company shares Earth with three NPC blocs in both starts,
/// a second faction can found on a body another faction already holds, and every placed faction
/// is Friendly with Trade and Positions both ways.
/// </summary>
[TestFixture]
public class MultiFactionEarthTests
{
    const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";
    static readonly string[] Blocs = { "Americas Compact", "Sino-Pacific Coalition", "Euro Consortium" };

    static (Game game, Entity player) Start(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), scenario);
        var catalog = GameLifecycle.BuildCatalog(store);
        var request = GameLifecycle.BuildQuickstartRequest(paths, catalog, 12345678);
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null);
        return (result!.Value.game, result.Value.faction);
    }

    static List<Entity> Colonies(Game game) =>
        game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).ToList();

    static string BodyName(Entity colony) =>
        colony.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<NameDB>().DefaultName;

    static Entity Faction(Game game, string name) =>
        game.Factions.Values.Single(f => f.GetDataBlob<NameDB>().DefaultName == name);

    static long Pop(Entity colony) => colony.GetDataBlob<ColonyInfoDB>().Population.Values.Sum();

    [TestCase(WarpTechStart, "default-design-fission-plant-75")]
    [TestCase(EarlyTech, "default-design-thermal-plant-75")]
    public void BothStarts_PlaceThreeBlocsOnEarth_BesideTheSmallCompany(string scenario, string plant)
    {
        var (game, player) = Start(scenario);
        var earth = Colonies(game).Where(c => BodyName(c) == "Earth").ToList();
        Assert.That(earth, Has.Count.EqualTo(4), "company + three blocs");

        var company = earth.Single(c => c.FactionOwnerID == player.Id);
        Assert.That(Pop(company), Is.EqualTo(8000));
        Assert.That(ColonyCrewPoolDB.Of(company).Total, Is.EqualTo(100));
        Assert.That(player.GetDataBlob<FactionInfoDB>().Money.GetCurrentFunds(), Is.EqualTo(25_000_000m));

        // §5.4 staffing shrink: ~6,210 CrewReq on the company's Earth installations.
        long crewReq = company.GetDataBlob<ComponentInstancesDB>().ComponentsByDesign.Values
            .SelectMany(list => list).Sum(i => (long)i.Design.CrewReq);
        TestContext.Out.WriteLine($"{scenario} company Earth CrewReq = {crewReq}");
        Assert.That(crewReq, Is.EqualTo(6210));
        // The 2,000-crew stock yard keeps its 200 component / 400 assembly points (x10 per crew).
        var yard = company.GetDataBlob<ComponentInstancesDB>().ComponentsByDesign["default-design-shipyard"].Single().Design;
        Assert.That(yard.CrewReq, Is.EqualTo(2000));
        var points = yard.GetAttribute<Pulsar4X.Industry.IndustryAtb>().IndustryPoints;
        Assert.That(points["component-construction"], Is.EqualTo(200));
        Assert.That(points["ship-assembly"], Is.EqualTo(400));

        var playerInfo = player.GetDataBlob<FactionInfoDB>();
        foreach (var name in Blocs)
        {
            var bloc = Faction(game, name);
            var colony = earth.Single(c => c.FactionOwnerID == bloc.Id);
            Assert.That(colony.GetDataBlob<NameDB>().DefaultName, Is.EqualTo(name));
            Assert.That(Pop(colony), Is.EqualTo(1_000_000), name);
            Assert.That(ColonyCrewPoolDB.Of(colony).Total, Is.EqualTo(200), name);
            var info = bloc.GetDataBlob<FactionInfoDB>();
            Assert.That(info.Money.GetCurrentFunds(), Is.EqualTo(50_000_000m), name);
            Assert.That(colony.HasDataBlob<LogiBaseDB>(), Is.True, name + " has a market (logistics) office");
            var designs = colony.GetDataBlob<ComponentInstancesDB>().ComponentsByDesign.Keys;
            Assert.That(designs, Does.Contain(plant), name);
            Assert.That(designs, Does.Not.Contain("default-design-shipyard"), name + ": the yard is the company's niche");

            // StartingItems, not UnlockAll: whatever the company has not researched is locked for the bloc too.
            Assert.That(info.Data.LockedTechs.Keys, Is.Not.Empty, name);
            Assert.That(info.Data.LockedTechs.Keys, Is.SupersetOf(playerInfo.Data.LockedTechs.Keys), name);
        }
        if (scenario == EarlyTech)
            Assert.That(Colonies(game), Has.Count.EqualTo(4), "no modern blocs or outposts left in Early Tech");
    }

    [Test]
    public void PlacedFactions_AreAllFriendly_WithTradeAndPositionsBothWays()
    {
        var (game, player) = Start(WarpTechStart);
        var placed = Blocs.Select(n => Faction(game, n)).Append(Faction(game, "Luna Concord")).Append(player).ToList();
        foreach (var a in placed)
        foreach (var b in placed)
        {
            if (a == b) continue;
            string pair = a.GetDataBlob<NameDB>().DefaultName + " -> " + b.GetDataBlob<NameDB>().DefaultName;
            var info = a.GetDataBlob<FactionInfoDB>();
            Assert.That(info.Stances.TryGetValue(b.Id, out var stance) ? stance : (FactionStance?)null,
                Is.EqualTo(FactionStance.Friendly), pair);
            Assert.That(FactionStanceRules.Granted(game, a.Id, b.Id, FactionGrants.Positions), Is.True, pair);
            Assert.That(FactionStanceRules.CanTrade(game, a.Id, b.Id), Is.True, pair);
        }
    }

    [Test]
    public void ASecondFaction_CanFoundOnAHeldBody_ButNoFactionFoundsTwice()
    {
        var (game, player) = Start(WarpTechStart);
        var luna = Colonies(game).Single(c => c.GetDataBlob<NameDB>().DefaultName == "Luna Concord");
        var lunaBody = luna.GetDataBlob<ColonyInfoDB>().PlanetEntity;
        var species = player.GetDataBlob<FactionInfoDB>().Species[0];
        Assert.That(lunaBody.TryGetFactionColonyOnBody(player.Id, out _), Is.False);
        Assert.That(lunaBody.ColoniesOnBody(), Has.Count.EqualTo(1));

        Assert.That(game.OrderHandler.HandleOrder(CreateColonyOrder.CreateCommand(player, species, lunaBody)), Is.True);
        game.TimePulse.TimeStep(game.TimePulse.GameGlobalDateTime + System.TimeSpan.FromHours(1));

        Assert.That(lunaBody.TryGetFactionColonyOnBody(player.Id, out var founded), Is.True, "the company founded beside Luna Concord");
        Assert.That(founded, Is.Not.EqualTo(luna));
        Assert.That(lunaBody.ColoniesOnBody(), Has.Count.EqualTo(2));

        // The same faction cannot found again on a body it holds (Luna now, Earth from the start).
        Assert.That(game.OrderHandler.HandleOrder(CreateColonyOrder.CreateCommand(player, species, lunaBody)), Is.False);
        var earthBody = Colonies(game).Single(c => c.FactionOwnerID == player.Id && BodyName(c) == "Earth")
            .GetDataBlob<ColonyInfoDB>().PlanetEntity;
        Assert.That(game.OrderHandler.HandleOrder(CreateColonyOrder.CreateCommand(player, species, earthBody)), Is.False);
        Assert.That(earthBody.ColoniesOnBody(), Has.Count.EqualTo(4));
    }
}
