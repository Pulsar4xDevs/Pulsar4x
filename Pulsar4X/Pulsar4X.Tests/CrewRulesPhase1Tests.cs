using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using GameEngine.Engine.Orders;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Modding;
using Pulsar4X.People;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests;

/// <summary>
/// Crew rules phase 1 (Jon, 18 Nov 2026): crew and officers are two groups; a ship with no bridge is automated
/// (no crew, no captain, no life support, no orders of its own, may ride in a fleet); the captain boards an officer
/// slot (2 / 4 / 6) and is not part crew; under-crewed compares crew only; old player designs only warn.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CrewRulesPhase1Tests
{
    Game _game = null!;
    Entity _player = null!;
    Entity _earth = null!;
    CommandTranslator _translator = null!;

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
        _translator = new CommandTranslator(_game);
    }

    FactionInfoDB Info => _player.GetDataBlob<FactionInfoDB>();
    ColonyCrewPoolDB Pool => ColonyCrewPoolDB.Of(_earth);
    int Human => _earth.GetDataBlob<ColonyInfoDB>().Population.Keys.First();
    void SetPool(long n) { Pool.Trained.Clear(); Pool.Add(Human, n); }

    Entity Spawn(string designId, string name)
        => ShipFactory.CreateShip(Info.ShipDesigns[designId], _player, _earth.GetDataBlob<ColonyInfoDB>().PlanetEntity, name);

    Entity Sat() => Spawn("default-ship-design-sensorsat", "Rules Sat");

    // ---- bridgeless ships are automated ----

    [Test]
    public void BridgelessShip_IsAutomated_NoCrewNoLifeSupportNeverUnderCrewed()
    {
        var sat = Sat();
        var design = sat.GetDataBlob<ShipInfoDB>().Design;
        Assert.That(design.NeedsCaptain, Is.False);
        Assert.That(design.CrewReq, Is.Zero, "0 crew even though the sensor has a crew figure");
        Assert.That(ShipCrew.PartCrew(design.Components), Is.GreaterThan(0), "the part itself still lists crew");
        Assert.That(ShipCrew.Automated(sat), Is.True);
        Assert.That(ShipCrew.Aboard(sat), Is.Zero);
        Assert.That(ShipCrew.LifeSupport(sat), Is.Zero, "0 life support");
        Assert.That(ShipCrew.IsUnderCrewed(sat), Is.False);
        Assert.That(ShipCrew.NoCaptain(sat), Is.False, "it needs no captain");
        Assert.That(ShipCrew.EngineFactor(sat), Is.EqualTo(1));
        Assert.That(ShipCrew.WorkFactor(sat), Is.EqualTo(1));

        var view = new GameProjector(_game).ProjectEntity(sat, _player.Id).GetView<ShipView>()!;
        Assert.That(view.Automated, Is.True);
        Assert.That(view.CrewRequired, Is.Zero);
        Assert.That(view.CrewBerths, Is.Zero);
        Assert.That(view.OfficerSlots, Is.Zero);
        Assert.That(view.OfficersRequired, Is.Zero);
        Assert.That(view.UnderCrewed, Is.False);
        Assert.That(view.NoCaptain, Is.False);
        Assert.That(view.CrewByComponent, Is.Empty);
    }

    [Test]
    public void BridgelessShip_TakesNoCaptain_NoCrewTopUp_AndCannotBeFlagship()
    {
        var sat = Sat();
        SetPool(50);
        var assign = _translator.Translate(_player, sat, new AssignCaptainCommand(sat.Id, 12345));
        Assert.That(assign.Accepted, Is.False);
        Assert.That(assign.RejectionReason, Does.Contain("automated"));
        var topUp = _translator.Translate(_player, sat, new TopUpCrewCommand(sat.Id, _earth.Id));
        Assert.That(topUp.Accepted, Is.False);
        Assert.That(topUp.RejectionReason, Does.Contain("automated"));
        Assert.That(ShipCrew.TopUp(sat, _earth), Is.Zero, "no crew bunks, nobody boards");
        Assert.That(Pool.Total, Is.EqualTo(50));
    }

    [Test]
    public void BridgelessShip_HasNoOrdersOfItsOwn_ButMayRideInAFleet()
    {
        var sat = Sat();
        var body = _earth.GetDataBlob<ColonyInfoDB>().PlanetEntity;

        // Goal and plotted-action orders on the ship itself are refused, with one reason the UI can show.
        var goal = _translator.Translate(_player, sat, new MoveToBodyCommand(sat.Id, body.Id));
        Assert.That(goal.Accepted, Is.False);
        Assert.That(goal.RejectionReason, Is.EqualTo(CommandTranslator.AutomatedShipNoOrders));
        var action = _translator.Translate(_player, sat, new WarpMoveCommand(sat.Id, body.Id));
        Assert.That(action.Accepted, Is.False);
        Assert.That(action.RejectionReason, Is.EqualTo(CommandTranslator.AutomatedShipNoOrders));

        // A bridged ship without a captain still gets the old reason, not the automated one.
        var starship = Spawn("default-ship-design-starship", "Rules Starship");
        var noCaptain = _translator.Translate(_player, starship, new MoveToBodyCommand(starship.Id, body.Id));
        Assert.That(noCaptain.RejectionReason, Is.Not.EqualTo(CommandTranslator.AutomatedShipNoOrders));

        // It may join a fleet and follow: assigning it is accepted, it cannot be made the flagship, and a fleet order is not
        // refused for the member's sake.
        var fleet = NewFleet();
        var join = _translator.Translate(_player, sat, new ReassignShipCommand(sat.Id, fleet.Id));
        Assert.That(join.Accepted, Is.True, join.RejectionReason);
        Assert.That(_translator.Translate(_player, fleet, new SetFlagshipCommand(fleet.Id, sat.Id)).Accepted, Is.False);
        var asFleet = _translator.Translate(_player, fleet, new MoveToBodyCommand(fleet.Id, body.Id));
        Assert.That(asFleet.RejectionReason, Is.Not.EqualTo(CommandTranslator.AutomatedShipNoOrders));
    }

    Entity NewFleet()
    {
        var root = _player.GetDataBlob<FleetDB>();
        var before = root.Children.Select(c => c.Id).ToHashSet();
        var made = _translator.Translate(_player, _player, new CreateFleetCommand(_player.Id, _earth.Manager!.ManagerID));
        Assert.That(made.Accepted, Is.True, made.RejectionReason);
        return root.Children.First(c => !before.Contains(c.Id) && c.HasDataBlob<FleetDB>());
    }

    // ---- officers: the captain is not crew ----

    [Test]
    public void Captain_BoardsAnOfficerSlot_NotACrewBunk_AndIsNotPartCrew()
    {
        var ship = Spawn("default-ship-design-courier", "Rules Courier");
        var design = ship.GetDataBlob<ShipInfoDB>().Design;
        Assert.That(design.OfficerSlots, Is.EqualTo(2), "a Shuttle Bridge");
        int crew = design.CrewReq;
        Assert.That(ShipCrew.NoCaptain(ship), Is.True, "bridge, no captain");
        Assert.That(ShipCrew.IsUnderCrewed(ship), Is.False, "full crew: a missing captain is 'No captain', not under-crewed");
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(crew));

        ship.GetDataBlob<ShipInfoDB>().CommanderID = 4242;
        var split = ShipCrew.SplitOf(ship);
        Assert.That(split.Captain, Is.EqualTo(1));
        Assert.That(split.Required, Is.EqualTo(crew), "the captain is not part crew");
        Assert.That(split.Capacity, Is.EqualTo(crew));
        Assert.That(ShipCrew.OfficersAboard(ship), Is.EqualTo(1));
        Assert.That(ShipCrew.CrewAboard(ship), Is.EqualTo(crew));
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(crew + 1), "life support and wages count everyone");
        Assert.That(ShipCrew.LifeSupport(ship), Is.EqualTo((crew + 1) * ShipCrew.KWPerCrew(ship)).Within(1e-9));
        Assert.That(ShipCrew.NoCaptain(ship), Is.False);
        Assert.That(ShipCrew.IsUnderCrewed(ship), Is.False);

        // Top Up cannot steal the captain's slot, and crew down leaves the captain.
        SetPool(100);
        Assert.That(ShipCrew.ToPool(ship, _earth), Is.EqualTo(crew));
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(1));
        Assert.That(ShipCrew.IsUnderCrewed(ship), Is.True, "no crew at all, captain or not");
        Assert.That(ShipCrew.TopUp(ship, _earth), Is.EqualTo(crew));
        Assert.That(ShipCrew.CrewAboard(ship), Is.EqualTo(crew));
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(crew + 1));
        Assert.That(ShipCrew.TopUp(ship, _earth), Is.Zero, "full");

        // The captain leaves: nobody returns to the pool.
        long pool = Pool.Total;
        ship.GetDataBlob<ShipInfoDB>().CommanderID = -1;
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(crew));
        Assert.That(Pool.Total, Is.EqualTo(pool));
        Assert.That(ShipCrew.NoCaptain(ship), Is.True);
    }

    [Test]
    public void BridgedShip_ShortOfCrew_IsUnderCrewed_WithOrWithoutACaptain()
    {
        var ship = Spawn("default-ship-design-courier", "Rules Short");
        var db = ship.GetDataBlob<ShipCrewDB>();
        db.Aboard = ShipCrew.SplitOf(ship).Required - 1;
        Assert.That(ShipCrew.IsUnderCrewed(ship), Is.True);
        ship.GetDataBlob<ShipInfoDB>().CommanderID = 4242;
        Assert.That(ShipCrew.IsUnderCrewed(ship), Is.True, "a captain does not make up a missing crew member");
        var view = new GameProjector(_game).ProjectEntity(ship, _player.Id).GetView<ShipView>()!;
        Assert.That(view.UnderCrewed, Is.True);
        Assert.That(view.NoCaptain, Is.False);
        Assert.That(view.CrewAboard, Is.EqualTo(view.CrewRequired - 1));
    }

    [Test]
    public void EngineAndWorkFactors_IgnoreTheCaptain()
    {
        var ship = Spawn("default-ship-design-surveyor", "Rules Surveyor");
        var db = ship.GetDataBlob<ShipCrewDB>();
        var split = ShipCrew.SplitOf(ship);
        Assert.That(split.Engine, Is.GreaterThan(0));
        db.Aboard = split.Engine / 2; // half the engine crew, no captain
        double engine = ShipCrew.EngineFactor(ship), work = ShipCrew.WorkFactor(ship);
        Assert.That(engine, Is.EqualTo(0.5).Within(0.1).And.LessThan(1));

        ship.GetDataBlob<ShipInfoDB>().CommanderID = 4242; // the captain boards: +1 aboard, not +1 crew
        Assert.That(ShipCrew.CrewAboard(ship), Is.EqualTo(split.Engine / 2));
        Assert.That(ShipCrew.EngineFactor(ship), Is.EqualTo(engine), "the captain does not crew the engines");
        Assert.That(ShipCrew.WorkFactor(ship), Is.EqualTo(work), "nor the work parts");
    }

    // ---- saves and old designs ----

    [Test]
    public void OldSave_AboardPlusCaptainBit_SplitsIntoCrewAndOfficer()
    {
        var ship = Spawn("default-ship-design-starship", "Rules Old Save");
        int crew = ShipCrew.SplitOf(ship).Required;
        ship.GetDataBlob<ShipInfoDB>().CommanderID = 4242;
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(crew + 1));
        // Round-trip the datablob the way a save does: Aboard (everyone) and the captain bit are what is stored.
        var copy = new ShipCrewDB(ship.GetDataBlob<ShipCrewDB>());
        Assert.That(copy.Aboard, Is.EqualTo(crew + 1));
        Assert.That(copy.CaptainAboard, Is.True);
        ship.SetDataBlob(copy);
        Assert.That(ShipCrew.CrewAboard(ship), Is.EqualTo(crew));
        Assert.That(ShipCrew.OfficersAboard(ship), Is.EqualTo(1));
        ShipCrew.AfterLoad(ship);
        Assert.That(ShipCrew.Aboard(ship), Is.EqualTo(crew + 1), "loading changes nothing for a ship that is already split");
    }

    [Test]
    public void AfterLoad_ClearsTheCrewOfABridgelessShip()
    {
        var sat = Sat();
        sat.GetDataBlob<ShipCrewDB>().Aboard = 1; // a save from before the rule: the Sat carried one operator
        ShipCrew.AfterLoad(sat);
        Assert.That(ShipCrew.Aboard(sat), Is.Zero);
        Assert.That(ShipCrew.LifeSupport(sat), Is.Zero);
    }

    [Test]
    public void OldPlayerDesign_WithTooFewBunks_OnlyWarns_AndIsNotRewritten()
    {
        // Starship GeoS in Jon's save: part crew 8, 6 bunks, and the bridge's berths used to count as bunks.
        var starship = Info.ShipDesigns["default-ship-design-starship"];
        var bunk = Info.ComponentDesigns["default-design-crew-quarters-1"];
        var parts = starship.Components.Where(c => c.design.UniqueID != bunk.UniqueID).ToList();
        parts.Add((bunk, 2)); // 2 bunks for 6 crew
        var old = new ShipDesign(Info, "Old Player Design", parts, starship.Armor, "old-player-" + Guid.NewGuid().ToString("N"));
        old.Initialise(Info);

        Assert.That(old.CrewBerths, Is.EqualTo(2));
        Assert.That(old.CrewReq, Is.EqualTo(starship.CrewReq));
        var warnings = ShipCrew.DesignCrewWarnings(old.Components);
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0], Does.Contain("crew bunks").And.Contain($"{old.CrewReq - 2} more"));
        // Warn only: the design is kept as the player made it, nothing auto-adds bunks.
        Assert.That(old.Components.Single(c => c.design.UniqueID == bunk.UniqueID).count, Is.EqualTo(2));
        Assert.That(Info.ShipDesigns.ContainsKey(old.UniqueID), Is.True, "still a usable design");
        Assert.That(ShipCrew.DesignCrewWarnings(starship.Components), Is.Empty, "stock designs do not warn");
        Assert.That(ShipCrew.DesignCrewWarnings(Info.ShipDesigns["default-ship-design-sensorsat"].Components), Is.Empty, "automated: nothing to warn about");
    }

    [Test]
    public void Refit_ToAFullyBunkedDesign_KeepsTheCaptainAndFlagsNothing()
    {
        // Jon's playtest, new rules: Starship refitted to a surveyor layout whose crew bunks cover the part crew,
        // plus its captain in an officer slot: not under-crewed.
        var ship = Spawn("default-ship-design-starship", "Rules Refit");
        ship.GetDataBlob<ShipInfoDB>().CommanderID = 4242;
        SetPool(100);
        var geo = Info.InternalComponentDesigns.Values.First(d => d.HasAttribute<Pulsar4X.GeoSurveys.GeoSurveyAtb>() && d.CrewReq > 0);
        var bunk = Info.ComponentDesigns["default-design-crew-quarters-1"];
        ship.AddComponent(new ComponentInstance(geo));
        for (int i = 0; i < geo.CrewReq; i++)
            ship.AddComponent(new ComponentInstance(bunk));
        ShipCrew.AfterRefit(ship, _earth);
        ShipCrew.TopUp(ship, _earth);

        var split = ShipCrew.SplitOf(ship);
        Assert.That(split.Capacity, Is.EqualTo(split.Required));
        Assert.That(ShipCrew.CrewAboard(ship), Is.EqualTo(split.Required));
        Assert.That(ShipCrew.OfficersAboard(ship), Is.EqualTo(1));
        Assert.That(ShipCrew.IsUnderCrewed(ship), Is.False);
        var view = new GameProjector(_game).ProjectEntity(ship, _player.Id).GetView<ShipView>()!;
        Assert.That(view.UnderCrewed, Is.False);
        Assert.That(view.CrewAboard, Is.EqualTo(view.CrewRequired));
        Assert.That(view.CrewBerths, Is.GreaterThanOrEqualTo(view.CrewRequired));
    }

    [Test]
    public void BoardAtLaunch_DrawsCrewOnly_AndAnAutomatedShipDrawsNobody()
    {
        SetPool(100);
        var courier = Spawn("default-ship-design-courier", "Rules Launch");
        courier.GetDataBlob<ShipInfoDB>().CommanderID = 4242;
        int boarded = ShipCrew.BoardAtLaunch(courier, _earth, _earth.StarSysDateTime);
        Assert.That(boarded, Is.EqualTo(courier.GetDataBlob<ShipInfoDB>().Design.CrewReq), "crew drawn; the captain is not");
        Assert.That(Pool.Total, Is.EqualTo(100 - boarded));
        Assert.That(ShipCrew.Aboard(courier), Is.EqualTo(boarded + 1));

        long pool = Pool.Total;
        var sat = Sat();
        Assert.That(ShipCrew.BoardAtLaunch(sat, _earth, _earth.StarSysDateTime), Is.Zero);
        Assert.That(ShipCrew.Aboard(sat), Is.Zero);
        Assert.That(Pool.Total, Is.EqualTo(pool));
    }

    // ---- span and reach text for automated ships ----

    [Test]
    public void AutomatedShip_SpanText_SaysAutomated_NotBridgeChairOrCaptain()
    {
        // The scenario's own Sensor Sat, so it shows in the player's fleet hierarchy.
        var sat = _game.Systems.SelectMany(x => x.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            .First(x => x.FactionOwnerID == _player.Id && x.GetDataBlob<ShipInfoDB>().Design.Name == "Sensor Sat");
        Assert.That(CommandSpan.Describe(sat), Is.EqualTo(CrewText.AutomatedSpan));
        Assert.That(CommandSpan.LimiterOf(sat), Is.EqualTo(CommandSpan.SpanLimiter.None));
        Assert.That(CommandSpan.Fix(sat, CommandSpanKind.Well), Is.EqualTo(CrewText.AutomatedSpan));
        Assert.That(CommandSpan.Fix(sat, CommandSpanKind.System), Is.EqualTo(CrewText.AutomatedSpan));
        Assert.That(CommandSpan.Fix(sat, CommandSpanKind.Body), Is.Null, "nothing wider is wanted");
        Assert.That(CrewText.AutomatedSpan, Does.Contain("Automated").And.Contain("no bridge").And.Contain("follows its fleet"));

        // The projected ship carries the same lines; none mention a bridge to fit, a chair or a captain.
        var projector = new GameProjector(_game);
        var (fleets, loose) = projector.ProjectFleetHierarchy(_player.Id);
        var snap = loose.Concat(fleets.SelectMany(f => f.Ships)).Single(x => x.Id == sat.Id);
        Assert.That(snap.Automated, Is.True);
        foreach (var line in new[] { snap.SpanLine, snap.ReachFixWell, snap.ReachFixSystem })
        {
            Assert.That(line, Is.EqualTo(CrewText.AutomatedSpan));
            Assert.That(line, Does.Not.Contain("chair").And.Not.Contain("Fit a").And.Not.Contain("captain").And.Not.Contain("officer"));
        }
        Assert.That(BootstrapPicker.CheckShip(snap), Is.EqualTo(CrewText.AutomatedNoOrders), "never 'no captain'");

        // A bridged ship keeps the bridge / chair wording.
        var starship = Spawn("default-ship-design-starship", "Rules Span Starship");
        Assert.That(CommandSpan.Describe(starship), Does.Contain("bridge").And.Not.Contain("Automated"));
        Assert.That(CommandSpan.Fix(starship, CommandSpanKind.System), Does.Not.Contain("Automated"));
    }
}
