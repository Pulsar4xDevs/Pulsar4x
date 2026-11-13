using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests;

/// <summary>
/// Crew plan phase 3: crew aboard ships. Launches draw from the colony pool (NPCs from population),
/// short launches go under-crewed, scrap, crew-down and refits return crew, top-ups board more,
/// engines fill first and scale by crew, and work parts run at F² of the crew left after the engines and bridge.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CrewAboardTests
{
    Game _game = null!;
    Entity _player = null!;
    Entity _earth = null!;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), "Pulsar4x.Scenario.WarpTechStart");
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345))!.Value;
        _game = created.game;
        _player = created.faction;
        _earth = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .First(c => c.FactionOwnerID == _player.Id && c.HasDataBlob<NavalAcademyDB>());
    }

    FactionInfoDB Info => _player.GetDataBlob<FactionInfoDB>();
    ColonyCrewPoolDB Pool => ColonyCrewPoolDB.Of(_earth);
    int Human => _earth.GetDataBlob<ColonyInfoDB>().Population.Keys.First();

    ShipDesign Design(string id) => Info.ShipDesigns[id];

    void SetPool(long count)
    {
        Pool.Trained.Clear();
        Pool.Add(Human, count);
    }

    /// <summary>Puts the design on a free Earth pad and launches it.</summary>
    Entity Launch(ShipDesign design, string name)
    {
        var launch = _earth.GetDataBlob<LaunchComplexDB>();
        var pad = launch.Pads.First(p => p.Value.ShipDesignId == null && p.Value.MaxTonnage >= design.MassPerUnit);
        pad.Value.ShipDesignId = design.UniqueID;
        pad.Value.ShipName = name;
        var before = ShipIds();
        Assert.That(LaunchComplexProcessor.TryLaunchShip(_earth, pad.Key), Is.True, "launch");
        return NewShip(before);
    }

    System.Collections.Generic.HashSet<int> ShipIds()
        => _earth.Manager!.GetAllEntitiesWithDataBlob<ShipInfoDB>().Select(s => s.Id).ToHashSet();

    Entity NewShip(System.Collections.Generic.HashSet<int> before)
        => _earth.Manager!.GetAllEntitiesWithDataBlob<ShipInfoDB>().Single(s => !before.Contains(s.Id));

    Entity Spawn(ShipDesign design, string name, Entity? faction = null, Entity? colony = null)
        => ShipFactory.CreateShip(design, faction ?? _player, (colony ?? _earth).GetDataBlob<ColonyInfoDB>().PlanetEntity, name);

    [Test]
    public void EveryScenarioShip_StartsFullyCrewed()
    {
        var ships = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>()).ToList();
        Assert.That(ships, Is.Not.Empty);
        foreach (var ship in ships)
        {
            var design = ship.GetDataBlob<ShipInfoDB>().Design;
            Assert.That(ShipCrew.Tracked(ship), Is.True, design.Name);
            Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(design.CrewReq), design.Name);
            Assert.That(ShipCrew.WorkFactor(ship), Is.EqualTo(1), design.Name);
            Assert.That(ShipCrew.EngineFactor(ship), Is.EqualTo(1), design.Name);
            Assert.That(ShipCrew.Note(ship), Is.Empty, design.Name);
        }
        Assert.That(Pool.Total, Is.EqualTo(500), "scenario ships draw nobody");
    }

    [Test]
    public void Launch_WithAFullPool_BoardsTheWholeCrew()
    {
        var courier = Design("default-ship-design-courier");
        var ship = Launch(courier, "Crewtest Full");
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(courier.CrewReq));
        Assert.That(Pool.Total, Is.EqualTo(500 - courier.CrewReq));
        var view = new GameProjector(_game).ProjectEntity(ship, _player.Id).GetView<ShipView>()!;
        Assert.That(view.CrewTracked, Is.True);
        Assert.That(view.CrewAboard, Is.EqualTo(courier.CrewReq));
        Assert.That(view.UnderCrewed, Is.False);
        Assert.That(view.CrewFactor, Is.EqualTo(1));
        Assert.That(view.CrewNote, Is.Empty);
    }

    [Test]
    public void Launch_ShortOfCrew_GoesUnderCrewed_WithAWarningAndAnEvent()
    {
        var courier = Design("default-ship-design-courier");
        SetPool(5);

        // The pad says so before the launch.
        var launch = _earth.GetDataBlob<LaunchComplexDB>();
        var pad = launch.Pads.First(p => p.Value.ShipDesignId == null && p.Value.MaxTonnage >= courier.MassPerUnit);
        pad.Value.ShipDesignId = courier.UniqueID;
        pad.Value.ShipName = "Crewtest Short";
        var industry = new GameProjector(_game).ProjectEntity(_earth, _player.Id).GetView<IndustryView>()!;
        var padView = industry.Pads.Single(p => p.CrewRequired == courier.CrewReq);
        Assert.That(padView.CrewToBoard, Is.EqualTo(5));
        Assert.That(padView.CrewShort, Is.True);
        Assert.That(padView.CrewWarning, Is.EqualTo($"Launches with 5/{courier.CrewReq} crew; train crew at an academy"));

        var before = ShipIds();
        Assert.That(LaunchComplexProcessor.TryLaunchShip(_earth, pad.Key), Is.True);
        var ship = NewShip(before);
        string name = ship.GetName(_player.Id);
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(5));
        Assert.That(Pool.Total, Is.EqualTo(0));

        var log = (FactionEventLog)Info.EventLog;
        var ev = log.GetEvents().Where(e => e.EventType == EventType.ShipUnderCrewed).ToList();
        Assert.That(ev.Select(e => e.Message), Has.Member($"{name} launched with 5/{courier.CrewReq} crew; train crew at an academy"));
        Assert.That(ev.Single().EntityId, Is.EqualTo(ship.Id));

        var view = new GameProjector(_game).ProjectEntity(ship, _player.Id).GetView<ShipView>()!;
        Assert.That(view.UnderCrewed, Is.True);
        Assert.That(view.CrewAboard, Is.EqualTo(5));
        Assert.That(view.CrewNote, Does.StartWith($"5/{courier.CrewReq} crew"));
    }

    [Test]
    public void ShipJob_WarnsWhenThePoolWillNotFillIt()
    {
        var courier = Design("default-ship-design-courier");
        var projector = new GameProjector(_game);
        var line = projector.ProjectEntity(_earth, _player.Id).GetView<IndustryView>()!.ProductionLines
            .First(l => l.Constructibles.Any(c => c.DesignId == courier.UniqueID));
        var job = new Pulsar4X.Industry.IndustryJob(Info, courier.UniqueID);
        job.InitialiseJob(1, false);
        Pulsar4X.Industry.IndustryTools.AddJob(_earth, line.Id, job);

        IndustryJobView Row() => projector.ProjectEntity(_earth, _player.Id).GetView<IndustryView>()!.ProductionLines
            .Single(l => l.Id == line.Id).Jobs.Single(j => j.JobId == job.JobID);
        Assert.That(Row().CrewWarning, Is.Empty, "500 in the pool");
        SetPool(5);
        Assert.That(Row().CrewRequired, Is.EqualTo(courier.CrewReq));
        Assert.That(Row().CrewWarning, Is.EqualTo($"Pool 5 of {courier.CrewReq} crew; the ship will launch under-crewed"));
    }

    [Test]
    public void Scrap_ReturnsTheCrewToThePool()
    {
        var courier = Design("default-ship-design-courier");
        var ship = Launch(courier, "Crewtest Scrap");
        Assert.That(Pool.Total, Is.EqualTo(500 - courier.CrewReq));
        Assert.That(ShipCrew.Scrap(ship, _earth), Is.EqualTo(courier.CrewReq));
        Assert.That(Pool.Total, Is.EqualTo(500));
        Assert.That(ship.IsValid, Is.False);
    }

    [Test]
    public void TopUp_AndCrewDown_Commands_MoveCrewInOrbit()
    {
        var courier = Design("default-ship-design-courier");
        SetPool(4);
        var ship = Launch(courier, "Crewtest TopUp");
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(4));
        var translator = new CommandTranslator(_game);

        var empty = translator.Translate(_player, ship, new TopUpCrewCommand(ship.Id, _earth.Id));
        Assert.That(empty.Accepted, Is.False);
        Assert.That(empty.RejectionReason, Is.EqualTo("The colony's crew pool is empty."));

        SetPool(100);
        var some = translator.Translate(_player, ship, new TopUpCrewCommand(ship.Id, _earth.Id, 3));
        Assert.That(some.Accepted, Is.True, some.RejectionReason);
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(7));
        var fill = translator.Translate(_player, ship, new TopUpCrewCommand(ship.Id, _earth.Id));
        Assert.That(fill.Accepted, Is.True, fill.RejectionReason);
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(courier.CrewReq));
        Assert.That(Pool.Total, Is.EqualTo(100 - (courier.CrewReq - 4)));
        var full = translator.Translate(_player, ship, new TopUpCrewCommand(ship.Id, _earth.Id));
        Assert.That(full.RejectionReason, Is.EqualTo("The ship has a full crew."));
        Assert.That(translator.Translate(_player, ship, new TopUpCrewCommand(ship.Id, _earth.Id, 0)).RejectionReason,
            Is.EqualTo("Top up at least one crew, or -1 to fill."));

        var down = translator.Translate(_player, ship, new CrewToPoolCommand(ship.Id, _earth.Id, 3));
        Assert.That(down.Accepted, Is.True, down.RejectionReason);
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(courier.CrewReq - 3));
        var all = translator.Translate(_player, ship, new CrewToPoolCommand(ship.Id, _earth.Id));
        Assert.That(all.Accepted, Is.True, all.RejectionReason);
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(0));
        Assert.That(Pool.Total, Is.EqualTo(100 + 4));
        Assert.That(translator.Translate(_player, ship, new CrewToPoolCommand(ship.Id, _earth.Id)).RejectionReason, Is.EqualTo("Nobody is aboard."));
    }

    [Test]
    public void Transfers_NeedTheShipInOrbitOfTheColony()
    {
        var mars = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .First(c => c.FactionOwnerID == _player.Id && c != _earth && c.Manager == _earth.Manager);
        var ship = Launch(Design("default-ship-design-courier"), "Crewtest Away");
        var result = new CommandTranslator(_game).Translate(_player, ship, new CrewToPoolCommand(ship.Id, mars.Id));
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("The ship must be in orbit of the colony."));
    }

    [Test]
    public void Engines_FillFirst_AndScaleWarpByTheirOwnCrew()
    {
        var courier = Design("default-ship-design-courier");
        var ship = Spawn(courier, "Crewtest Engines");
        var split = ShipCrew.SplitOf(ship);
        Assert.That(split.Engine, Is.GreaterThan(1), "the warp drive is crewed");
        var warp = ship.GetDataBlob<WarpAbilityDB>();
        double full = warp.MaxSpeed;
        Assert.That(full, Is.GreaterThan(0));

        var crew = ship.GetDataBlob<ShipCrewDB>();
        crew.Aboard = split.Engine / 2;
        Assert.That(ShipCrew.EngineFactor(ship), Is.EqualTo((split.Engine / 2) / (double)split.Engine));
        Assert.That(warp.MaxSpeed, Is.EqualTo(WarpMath.MaxSpeedCalc(warp.TotalWarpPower * ShipCrew.EngineFactor(ship),
            ship.GetDataBlob<MassVolumeDB>().MassDry)));
        Assert.That(warp.MaxSpeed, Is.LessThan(full));

        crew.Aboard = 0;
        Assert.That(warp.MaxSpeed, Is.EqualTo(0));
        crew.Aboard = split.Engine;
        Assert.That(warp.MaxSpeed, Is.EqualTo(full), "the engines take the first crew");
    }

    [Test]
    public void WorkParts_RunAtFSquared_OfTheCrewLeftAfterEnginesAndBridge()
    {
        var surveyor = Design("default-ship-design-surveyor");
        var ship = Spawn(surveyor, "Crewtest Work");
        var split = ShipCrew.SplitOf(ship);
        Assert.That(split.Work, Is.GreaterThan(1));
        Assert.That(split.Bridge, Is.GreaterThan(0));
        var crew = ship.GetDataBlob<ShipCrewDB>();

        crew.Aboard = split.Engine;
        Assert.That(ShipCrew.WorkFactor(ship), Is.EqualTo(0), "the bridge is filled before any work part");
        crew.Aboard = split.Engine + split.Bridge;
        Assert.That(ShipCrew.WorkFactor(ship), Is.EqualTo(0));
        crew.Aboard = split.Engine + split.Bridge + split.Work / 2;
        double f = (split.Work / 2) / (double)split.Work;
        Assert.That(ShipCrew.WorkFactor(ship), Is.EqualTo(f));
        Assert.That(ShipCrew.WorkRate(ship), Is.EqualTo(f * f).Within(1e-12));
        Assert.That(ShipCrew.EngineFactor(ship), Is.EqualTo(1));
        crew.Aboard = split.Engine + split.Bridge + split.Work;
        Assert.That(ShipCrew.WorkRate(ship), Is.EqualTo(1));
        Assert.That(ShipCrew.WorkRate(_earth), Is.EqualTo(1), "colonies are not crewed");
    }

    [Test]
    public void Surveys_AtFSquared_KeepTheFractionForTheNextPass()
    {
        var ship = Spawn(Design("default-ship-design-surveyor"), "Crewtest Survey");
        var split = ShipCrew.SplitOf(ship);
        // Exactly half the work crew: F = 0.5, F² = 0.25 when the work crew is even.
        var crew = ship.GetDataBlob<ShipCrewDB>();
        crew.Aboard = split.Engine + split.Bridge + split.Work / 2;
        double rate = ShipCrew.WorkRate(ship);
        uint total = 0;
        for (int i = 0; i < 8; i++)
            total += ShipCrew.ScaleSurvey(ship, 10, grav: false);
        Assert.That(total, Is.EqualTo((uint)Math.Floor(80 * rate + 1e-9)));
        Assert.That(ShipCrew.ScaleSurvey(ship, 10, grav: true), Is.EqualTo((uint)Math.Floor(10 * rate + 1e-9)), "grav keeps its own carry");
        crew.Aboard = split.Capacity;
        Assert.That(ShipCrew.ScaleSurvey(ship, 10, grav: false), Is.EqualTo(10));
    }

    [Test]
    public void NpcLaunch_CrewsFromPopulation_WithNoTraining()
    {
        Assert.That(_game.LocalHumanFactionId, Is.EqualTo(_player.Id));
        // Luna Concord and Ceres Depot belong to the NPC factions.
        var npcColony = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .First(c => c.FactionOwnerID != _player.Id);
        var npc = _game.Factions[npcColony.FactionOwnerID];
        var design = Design("default-ship-design-courier");
        Assert.That(NavalAcademyCourse.PopulationOf(npcColony.GetDataBlob<ColonyInfoDB>()), Is.GreaterThan(design.CrewReq));
        Assert.That(ShipCrew.CrewsFromPopulation(npcColony), Is.True);
        Assert.That(ShipCrew.CrewsFromPopulation(_earth), Is.False);

        var ship = Spawn(design, "Crewtest Npc", npc, npcColony);
        var info = npcColony.GetDataBlob<ColonyInfoDB>();
        long before = NavalAcademyCourse.PopulationOf(info);
        long poolBefore = ColonyCrewPoolDB.Of(npcColony).Total;
        int boarded = ShipCrew.BoardAtLaunch(ship, npcColony, npcColony.StarSysDateTime);
        Assert.That(boarded, Is.EqualTo(design.CrewReq));
        Assert.That(NavalAcademyCourse.PopulationOf(info), Is.EqualTo(before - design.CrewReq));
        Assert.That(ColonyCrewPoolDB.Of(npcColony).Total, Is.EqualTo(poolBefore), "no pool for NPCs");
        Assert.That(ShipCrew.Note(ship), Is.Empty, "shown fully crewed");
    }

    [Test]
    public void Refit_ThatDropsBerths_SendsTheExcessToTheYardPool()
    {
        var courier = Design("default-ship-design-courier");
        var ship = Spawn(courier, "Crewtest Refit");
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(courier.CrewReq));
        // Take out the big quarters (as a refit that drops them would).
        var instances = ship.GetDataBlob<ComponentInstancesDB>();
        var quarters = instances.AllComponents.Values.First(i => i.Design.UniqueID == "default-design-crew-quarters-10");
        ship.RemoveComponent(quarters);
        var split = ShipCrew.SplitOf(ship);
        Assert.That(split.Berths, Is.LessThan(courier.CrewReq));

        int excess = ShipCrew.AfterRefit(ship, _earth);
        Assert.That(excess, Is.EqualTo(courier.CrewReq - split.Capacity));
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(split.Capacity));
        Assert.That(Pool.Total, Is.EqualTo(500 + excess));
        Assert.That(ShipCrew.AfterRefit(ship, _earth), Is.EqualTo(0));
    }

    [Test]
    public void Launch_DrawsTheLargestSpeciesPoolFirst()
    {
        int other = Human + 100_000;
        Pool.Trained.Clear();
        Pool.Add(Human, 20);
        Pool.Add(other, 30);
        var courier = Design("default-ship-design-courier");
        Launch(courier, "Crewtest Species");
        // 14 from the 30, the 20 untouched.
        Assert.That(Pool.Trained[other], Is.EqualTo(30 - courier.CrewReq));
        Assert.That(Pool.Trained[Human], Is.EqualTo(20));

        Assert.That(Pool.Take(10, out int first), Is.EqualTo(10));
        Assert.That(first, Is.EqualTo(Human), "now the larger");
        Assert.That(Pool.Trained[Human], Is.EqualTo(10));
        Assert.That(Pool.Trained[other], Is.EqualTo(30 - courier.CrewReq));
    }

    [Test]
    public void SaveRoundTrip_KeepsTheCrewAboard()
    {
        var courier = Design("default-ship-design-courier");
        SetPool(6);
        var ship = Launch(courier, "Crewtest Save");
        ship.GetDataBlob<ShipCrewDB>().GeoSurveyCarry = 0.25;
        int shipId = ship.Id;
        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        Assert.That(loaded.GlobalManager.TryGetGlobalEntityById(shipId, out var after), Is.True);
        var crew = after.GetDataBlob<ShipCrewDB>();
        Assert.That(crew.Aboard, Is.EqualTo(6));
        Assert.That(crew.GeoSurveyCarry, Is.EqualTo(0.25));
        Assert.That(ShipCrew.EngineFactor(after), Is.EqualTo(ShipCrew.EngineFactor(ship)));
        Assert.That(ShipCrew.WorkFactor(after), Is.EqualTo(ShipCrew.WorkFactor(ship)));
    }
}
