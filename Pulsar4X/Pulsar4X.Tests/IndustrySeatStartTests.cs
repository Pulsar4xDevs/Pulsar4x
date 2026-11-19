using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Civilian industry plan, slice 4: who starts with an industry head. Early Tech player: nobody. Warp Tech
/// player: Earth only. Every NPC colony: always, and a monthly auto-hire back-fills an empty NPC seat.
/// </summary>
[TestFixture]
[NonParallelizable]
public class IndustrySeatStartTests
{
    const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";

    static Game Start(string? scenario)
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), scenario);
        var catalog = GameLifecycle.BuildCatalog(store);
        var systems = catalog.Systems.Where(s => s.StartingBodies.Count > 0).ToList();
        var request = new NewGameRequest(paths, "United Earth Corp", "UEC", catalog.Species.First().Id, catalog.Colonies.First().Id,
            systems.First().Id, systems.First().StartingBodies.First().Id, systems.Select(s => s.Id).ToList(), 10, 12345678, 100_000_000, true, "", "");
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null, "CreateGameCore returned null");
        result!.Value.game.Settings.EnforceSingleThread = true;
        return result.Value.game;
    }

    static List<Entity> Colonies(Game game) =>
        game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).ToList();

    static Entity Named(Game game, string name) =>
        Colonies(game).Single(c => c.GetDataBlob<NameDB>().DefaultName == name);

    static bool IsPlayer(Game game, Entity colony)
        => game.Factions.Values.Single(f => f.Id == colony.FactionOwnerID).GetDataBlob<NameDB>().DefaultName == "United Earth Corp";

    static void AssertHeadNotChair(Entity colony)
    {
        Assert.That(IndustrySeat.TryGetHead(colony, out var head), Is.True, colony.GetDataBlob<NameDB>().DefaultName);
        Assert.That(head.TryGetDataBlob<CommanderDB>(out var db), Is.True);
        Assert.That(db.Type, Is.EqualTo(Pulsar4X.DataStructures.CommanderTypes.Civilian));
        Assert.That(db.AssignedTo, Is.EqualTo(colony.Id));
        if (colony.TryGetDataBlob<AdminSpaceDB>(out var admin))
            Assert.That(admin.CommanderSeats.Select(s => s.CommanderID), Does.Not.Contain(head.Id), "the head is not in a city-hall chair");
    }

    // ----- scenario starts -----

    [Test]
    public void WarpStart_PlayerEarthHasAHead_MarsOutpostDoesNot()
    {
        var game = Start(WarpTechStart);

        var earth = Named(game, "Earth HQ");
        Assert.That(IsPlayer(game, earth), Is.True);
        AssertHeadNotChair(earth);
        Assert.That(IndustrySeat.HasHead(Named(game, "Mars HQ")), Is.False);
        Assert.That(IsPlayer(game, Named(game, "Mars HQ")), Is.True);
    }

    [Test]
    public void WarpStart_EveryNpcColony_StartsWithAHead()
    {
        var game = Start(WarpTechStart);
        var npc = new[] { "Luna Concord", "Ceres Depot", "Americas Compact", "Sino-Pacific Coalition", "Euro Consortium" };

        foreach (var name in npc)
        {
            var colony = Named(game, name);
            Assert.That(IsPlayer(game, colony), Is.False, name);
            AssertHeadNotChair(colony);
        }
    }

    [Test]
    public void CeresDepot_HeadAndChairHolder_AreTwoPeople()
    {
        var game = Start(WarpTechStart);
        var ceres = Named(game, "Ceres Depot");

        int chair = ceres.GetDataBlob<AdminSpaceDB>().CommanderSeats.Single(s => s.SeatType >= AdminLevel.Colony).CommanderID;
        Assert.That(chair, Is.GreaterThanOrEqualTo(0), "Ceres seats its chair from the start");
        Assert.That(IndustrySeat.HeadId(ceres), Is.GreaterThanOrEqualTo(0));
        Assert.That(IndustrySeat.HeadId(ceres), Is.Not.EqualTo(chair));
    }

    [TestCase(EarlyTech)]
    [TestCase(null)] // the default start is Early Tech
    public void EarlyStart_PlayerEarthHasNoHead_ButTheBlocsDo(string? scenario)
    {
        var game = Start(scenario);

        var earth = Named(game, "Earth HQ");
        Assert.That(IsPlayer(game, earth), Is.True);
        Assert.That(IndustrySeat.HasHead(earth), Is.False);
        foreach (var name in new[] { "Americas Compact", "Sino-Pacific Coalition", "Euro Consortium" })
            AssertHeadNotChair(Named(game, name));
    }

    [Test]
    public void EveryColony_InEveryStart_FollowsTheRule()
    {
        foreach (var scenario in new string?[] { EarlyTech, WarpTechStart })
        {
            var game = Start(scenario);
            foreach (var colony in Colonies(game))
            {
                string name = colony.GetDataBlob<NameDB>().DefaultName;
                if (!IsPlayer(game, colony))
                    Assert.That(IndustrySeat.HasHead(colony), Is.True, $"{scenario}: NPC {name} always has a head");
                else
                    Assert.That(IndustrySeat.HasHead(colony), Is.EqualTo(scenario == WarpTechStart && name == "Earth HQ"), $"{scenario}: player {name}");
            }
        }
    }

    // ----- day one -----

    // The first job on this colony that a head would chain: a line, a design it accepts, and a buildable input short in stock.
    static bool TryFindChainableJob(Entity colony, out string lineId, out string designId)
    {
        lineId = designId = "";
        var info = colony.GetFactionOwner.GetDataBlob<FactionInfoDB>();
        var industry = colony.GetDataBlob<IndustryAbilityDB>();
        var stock = colony.GetDataBlob<CargoStorageDB>();
        foreach (var (id, line) in industry.ProductionLines)
        {
            foreach (var design in info.IndustryDesigns.Values)
            {
                if (!IndustryTools.LineAccepts(line, design) || design.ResourceCosts.Count == 0)
                    continue;
                foreach (var (inputId, need) in design.ResourceCosts)
                {
                    var item = info.Data.CargoGoods.GetAny(inputId) ?? (info.IndustryDesigns.TryGetValue(inputId, out var d) ? (ICargoable)d : null);
                    if (item is not IConstructableDesign)
                        continue;
                    long have = stock.GetUnitsStoredByDesign(item, false);
                    if (have > 0)
                        stock.RemoveUnitsByDesign(item, have); // make the input short
                    (lineId, designId) = (id, design.UniqueID);
                    return true;
                }
            }
        }
        return false;
    }

    static int JobCount(Entity colony)
        => colony.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values.Sum(l => l.Jobs.Count);

    static int QueueAndCount(Game game, Entity colony, string lineId, string designId)
    {
        int before = JobCount(colony);
        var result = new CommandTranslator(game).Translate(colony.GetFactionOwner, colony,
            new QueueIndustryJobCommand(colony.Id, lineId, designId, 1, false, false));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        return JobCount(colony) - before;
    }

    [Test]
    public void EarlyStart_PlayerEarth_DoesNotChainOnDayOne()
    {
        var game = Start(EarlyTech);
        var earth = Named(game, "Earth HQ");
        Assert.That(TryFindChainableJob(earth, out var line, out var design), Is.True, "a chainable job on Earth");

        Assert.That(QueueAndCount(game, earth, line, design), Is.EqualTo(1), "no head: the job adds only itself");
    }

    [Test]
    public void WarpStart_PlayerEarth_ChainsOnDayOne()
    {
        var game = Start(WarpTechStart);
        var earth = Named(game, "Earth HQ");
        Assert.That(TryFindChainableJob(earth, out var line, out var design), Is.True, "a chainable job on Earth");

        Assert.That(QueueAndCount(game, earth, line, design), Is.GreaterThan(1), "a head is seated: feeders are chained");
    }

    // ----- the blueprint rule -----

    [Test]
    public void Blueprint_SeatsAnNpc_EvenWithoutTheFlag()
    {
        Assert.That(new ColonyBlueprint { OwnerFaction = "Some Co." }.ShouldSeatIndustryHead(), Is.True);
        Assert.That(new ColonyBlueprint { OwnerFaction = "Some Co.", SeatIndustryHead = false }.ShouldSeatIndustryHead(), Is.True);
        Assert.That(new ColonyBlueprint().ShouldSeatIndustryHead(), Is.False, "a player colony needs the flag");
        Assert.That(new ColonyBlueprint { SeatIndustryHead = true }.ShouldSeatIndustryHead(), Is.True);
    }

    // ----- back-fill -----

    [Test]
    public void AutoHire_BackFillsAnEmptyNpcSeat_WithoutTouchingTheChair()
    {
        var game = Start(WarpTechStart);
        var bloc = Named(game, "Americas Compact");
        Recruitment.Refresh(bloc, bloc.StarSysDateTime); // the monthly pass: it fills the blocs' empty chair
        int oldHead = IndustrySeat.HeadId(bloc);
        int chairBefore = bloc.GetDataBlob<AdminSpaceDB>().CommanderSeats.Single(s => s.SeatType >= AdminLevel.Colony).CommanderID;
        Assert.That(chairBefore, Is.GreaterThanOrEqualTo(0));
        IndustrySeat.Unseat(bloc, bloc.StarSysDateTime);
        Assert.That(IndustrySeat.HasHead(bloc), Is.False);
        Assert.That(bloc.GetDataBlob<CandidatePoolDB>().Candidates, Is.Not.Empty);
        long popBefore = bloc.GetDataBlob<ColonyInfoDB>().Population.Values.Sum();

        Recruitment.AutoHire(bloc);

        Assert.That(IndustrySeat.HasHead(bloc), Is.True, "the monthly auto-hire fills the vacancy");
        Assert.That(IndustrySeat.HeadId(bloc), Is.Not.EqualTo(oldHead), "a fresh hire, not the person who left");
        Assert.That(IndustrySeat.HeadId(bloc), Is.Not.EqualTo(chairBefore));
        Assert.That(bloc.GetDataBlob<AdminSpaceDB>().CommanderSeats.Single(s => s.SeatType >= AdminLevel.Colony).CommanderID, Is.EqualTo(chairBefore));
        AssertHeadNotChair(bloc);
        Assert.That(bloc.GetDataBlob<ColonyInfoDB>().Population.Values.Sum(), Is.EqualTo(popBefore - 1), "a hire takes one civilian");
    }

    [Test]
    public void AutoHire_LeavesAFilledSeatAlone()
    {
        var game = Start(WarpTechStart);
        var bloc = Named(game, "Americas Compact");
        int head = IndustrySeat.HeadId(bloc);
        Recruitment.Refresh(bloc, bloc.StarSysDateTime);

        Recruitment.AutoHire(bloc);

        Assert.That(IndustrySeat.HeadId(bloc), Is.EqualTo(head));
    }

    [Test]
    public void AutoHire_NeverSeatsAPlayerColony()
    {
        var game = Start(EarlyTech);
        var earth = Named(game, "Earth HQ");
        Recruitment.Refresh(earth, earth.StarSysDateTime);

        Recruitment.AutoHire(earth);

        Assert.That(IndustrySeat.HasHead(earth), Is.False, "a player seats their own head");
    }

    // ----- the log -----

    [Test]
    public void QueuingAJob_WritesASensibleIndustryLine_NotNewJob()
    {
        var game = Start(EarlyTech);
        var earth = Named(game, "Earth HQ");
        Assert.That(TryFindChainableJob(earth, out var line, out var design), Is.True);
        QueueAndCount(game, earth, line, design);

        var lines = earth.GetDataBlob<GameEngine.Engine.Orders.ShipLogDB>().Entries.Select(e => e.Text).ToList();
        Assert.That(lines, Has.None.EqualTo("Industry: NewJob."));
        Assert.That(lines.Last(), Does.StartWith("Industry: queued 1 ").And.Contain(" on ").And.EndWith("."));
    }

    [Test]
    public void DestroyedHead_ThenAutoHire_BackFillsTheNpcSeat()
    {
        var game = Start(WarpTechStart);
        var bloc = Named(game, "Americas Compact");
        Recruitment.Refresh(bloc, bloc.StarSysDateTime);
        Assert.That(IndustrySeat.TryGetHead(bloc, out var head), Is.True);
        int oldId = head.Id;
        Assert.That(bloc.GetDataBlob<CandidatePoolDB>().Candidates, Is.Not.Empty);

        CommanderFactory.DestroyCommander(head);
        Assert.That(IndustrySeat.HasHead(bloc), Is.False, "a dead head leaves the seat empty");

        Recruitment.AutoHire(bloc);

        Assert.That(IndustrySeat.HasHead(bloc), Is.True, "the monthly auto-hire refills it");
        Assert.That(IndustrySeat.HeadId(bloc), Is.Not.EqualTo(oldId));
        AssertHeadNotChair(bloc);
    }

    [Test]
    public void ADanglingHeadId_ThenAutoHire_BackFillsTheNpcSeat()
    {
        var game = Start(WarpTechStart);
        var bloc = Named(game, "Americas Compact");
        Recruitment.Refresh(bloc, bloc.StarSysDateTime);
        IndustrySeat.Of(bloc).HeadId = 987_654_321;
        Assert.That(IndustrySeat.HasHead(bloc), Is.False);

        Recruitment.AutoHire(bloc);

        Assert.That(IndustrySeat.HasHead(bloc), Is.True);
        AssertHeadNotChair(bloc);
    }
}
