using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests;

/// <summary>
/// Crew plan phase 4: ship life support (1 kW per crew aboard, must-run, in the warp and move-cost
/// checks), the Closed-Loop Life Support tech, garden/sealed colony life support with pooled crew,
/// the sized stock reactors, and the "life support failing" event that pauses the clock.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CrewLifeSupportTests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";
    const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";

    static (Game game, Entity player) Start(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), scenario);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345));
        Assert.That(created, Is.Not.Null);
        created!.Value.game.Settings.EnforceSingleThread = true;
        return (created.Value.game, created.Value.faction);
    }

    static IEnumerable<Entity> Ships(Game game)
        => game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>());

    static Entity ShipOfDesign(Game game, Entity faction, string designId)
        => Ships(game).First(s => s.FactionOwnerID == faction.Id && s.GetDataBlob<ShipInfoDB>().Design.UniqueID == designId);

    static double ReactorOutput(IEnumerable<(ComponentDesign design, int count)> parts)
        => parts.Where(p => p.design.HasAttribute<EnergyGenerationAtb>()).Sum(p => p.design.GetAttribute<EnergyGenerationAtb>().PowerOutputMax * p.count);

    static double Sustain(IEnumerable<(ComponentDesign design, int count)> parts)
        => parts.Where(p => p.design.HasAttribute<WarpDriveAtb>()).Sum(p => p.design.GetAttribute<WarpDriveAtb>().BubbleSustainCost * p.count);

    // ---- stock designs ----

    [TestCase(WarpTechStart)]
    [TestCase(EarlyTech)]
    public void EveryStockWarpDesign_OfEveryFaction_WarpsWithHeadroom_CountingLifeSupport(string scenario)
    {
        var (game, _) = Start(scenario);
        int checkedDesigns = 0;
        foreach (var faction in game.Factions.Values)
        {
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info))
                continue;
            double perCrew = Pulsar4X.Engine.GameSettings.DefaultShipLifeSupport_kWPerCrew * CrewTech.LifeSupportFactor(info.Data);
            foreach (var design in info.ShipDesigns.Values)
            {
                var parts = design.Components;
                double sustain = Sustain(parts);
                if (!(sustain > 0))
                    continue;
                double lifeSupport = ShipCrew.Split(parts).Capacity * perCrew;
                double reactors = ReactorOutput(parts);
                TestContext.Out.WriteLine($"{scenario} {faction.GetDataBlob<NameDB>().DefaultName} {design.Name}: reactors {reactors:0.#} kW,"
                    + $" sustain {sustain:0.#} + life support {lifeSupport:0.#} = {sustain + lifeSupport:0.#} kW, headroom {reactors / (sustain + lifeSupport) - 1:P0}");
                Assert.That(reactors, Is.GreaterThanOrEqualTo(1.1 * (sustain + lifeSupport)), design.Name + ": 10 % over warp plus life support, solar not counted");
                Assert.That(WarpMath.DesignWarpWarnings(parts), Is.Empty, design.Name);
                checkedDesigns++;
            }
        }
        if (scenario == WarpTechStart)
            Assert.That(checkedDesigns, Is.GreaterThanOrEqualTo(10), "player and NPC warp designs");
    }

    [TestCase(WarpTechStart)]
    [TestCase(EarlyTech)]
    public void EveryStartingShip_CarriesItsLifeSupport_AndEveryWarpShipCanWarp(string scenario)
    {
        var (game, _) = Start(scenario);
        foreach (var ship in Ships(game).Where(s => s.HasDataBlob<ShipCrewDB>()))
        {
            string name = ship.GetDataBlob<NameDB>().DefaultName;
            int aboard = ShipCrew.Aboard(ship);
            if (!ship.TryGetDataBlob<EnergyGenAbilityDB>(out var power))
            {
                Assert.That(ShipCrew.LifeSupport(ship), Is.EqualTo(0), name + ": no plant books nothing");
                continue;
            }
            double expected = aboard * ShipCrew.KWPerCrew(ship);
            Assert.That(ShipCrew.LifeSupport(ship), Is.EqualTo(expected).Within(1e-9), name);
            Assert.That(power.Demand, Is.GreaterThanOrEqualTo(expected - 1e-9), name + ": life support is in the must-run demand");
            Assert.That(power.TotalOutputMax, Is.GreaterThan(expected), name + ": the plant carries its crew");
            if (ship.TryGetDataBlob<WarpAbilityDB>(out var warp) && warp.MaxSpeed > 0)
                Assert.That(WarpMath.WarpRefusal(warp.MaxSpeed, warp.BubbleSustainCost, power.TotalOutputMax - expected, expected), Is.Empty, name);
        }
    }

    [Test]
    public void TheStarship_GetsA2tReactor_AndBerthsForItsCrew()
    {
        var (game, player) = Start(WarpTechStart);
        var design = player.GetDataBlob<FactionInfoDB>().ShipDesigns["default-ship-design-starship"];
        Assert.That(design.Components.Where(c => c.design.UniqueID == "default-design-reactor-2t").Sum(c => c.count), Is.EqualTo(1));
        Assert.That(ShipCrew.DesignCrewWarnings(design.Components), Is.Empty);
        Assert.That(ShipCrew.Split(design.Components).Required, Is.EqualTo(7), "4 crew plus the reactor's 3");
        Assert.That(ShipCrew.Split(design.Components).Capacity, Is.EqualTo(7));
        Assert.That(ReactorOutput(design.Components), Is.EqualTo(15).Within(1e-9));
        Assert.That(ReactorOutput(design.Components), Is.GreaterThan(1.1 * 7), "carries its crew's 7 kW with headroom");
    }

    // ---- ship life support ----

    [Test]
    public void ShipLifeSupport_FollowsTheCrewAboard_InTheMustRunDemand()
    {
        var (game, player) = Start(WarpTechStart);
        var ship = ShipOfDesign(game, player, "default-ship-design-courier");
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        var crew = ship.GetDataBlob<ShipCrewDB>();
        int aboard = crew.Aboard;
        Assert.That(aboard, Is.GreaterThan(4));
        double before = power.Demand;
        Assert.That(ShipCrew.LifeSupport(ship), Is.EqualTo(aboard * 1.0).Within(1e-9));

        crew.Aboard = aboard - 4;
        Assert.That(ShipCrew.LifeSupport(ship), Is.EqualTo((aboard - 4) * 1.0).Within(1e-9));
        Assert.That(power.Demand, Is.EqualTo(before - 4).Within(1e-9));

        crew.Aboard = aboard;
        Assert.That(power.Demand, Is.EqualTo(before).Within(1e-9));

        // The move-cost hook sees it too.
        var spec = MoveCostShip.FromEntity(ship);
        Assert.That(spec.LifeSupport_kW, Is.EqualTo(aboard * 1.0).Within(1e-9));
        Assert.That(spec.WarpOutputMax, Is.EqualTo(power.TotalOutputMax - aboard).Within(1e-9));
    }

    [Test]
    public void LifeSupport_TakesOutputFromTheWarp_AndTheRefusalSaysSo()
    {
        var (game, player) = Start(WarpTechStart);
        var ship = ShipOfDesign(game, player, "default-ship-design-courier");
        var warp = ship.GetDataBlob<WarpAbilityDB>();
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        Assert.That(MoveCostCalculator.Warp(MoveCostShip.FromEntity(ship), 1e10).Feasible, Is.True);

        // Pretend the crew needs as much as the plant makes less half the bubble: the warp is refused.
        double lifeSupport = power.TotalOutputMax - warp.BubbleSustainCost / 2;
        var spec = MoveCostShip.FromEntity(ship) with { LifeSupport_kW = lifeSupport };
        var cost = MoveCostCalculator.Warp(spec, 1e10);
        Assert.That(cost.Feasible, Is.False);
        Assert.That(cost.Reason, Does.Contain("of life support"));

        string text = WarpMath.WarpRefusal(warp.MaxSpeed, warp.BubbleSustainCost, power.TotalOutputMax - lifeSupport, lifeSupport);
        Assert.That(text, Does.StartWith("Plant output"));
        Assert.That(text, Does.Contain("less"));
        Assert.That(text, Does.Contain("of life support"));
        Assert.That(WarpMath.WarpRefusal(warp.MaxSpeed, warp.BubbleSustainCost, power.TotalOutputMax - ShipCrew.LifeSupport(ship), ShipCrew.LifeSupport(ship)), Is.Empty);
    }

    // ---- tech ----

    [Test]
    public void ClosedLoopLifeSupport_IsInBothStarts_AndLevel1Is085PerCrew_OnShipsAndTheSealedTerm()
    {
        var (game, player) = Start(WarpTechStart);
        var data = player.GetDataBlob<FactionInfoDB>().Data;
        Assert.That(data.Techs.ContainsKey(CrewTech.LifeSupportEfficiency), Is.True);
        var tech = data.Techs[CrewTech.LifeSupportEfficiency];
        Assert.That(tech.Name, Is.EqualTo("Closed-Loop Life Support"));
        Assert.That(tech.Category, Is.EqualTo("tech-category-biology-genetics"));
        Assert.That(tech.MaxLevel, Is.EqualTo(8));
        Assert.That(tech.Level, Is.EqualTo(0));
        Assert.That(data.IsResearchable(CrewTech.LifeSupportEfficiency), Is.True, "no prerequisite: available at start");
        Assert.That(CrewTech.LifeSupportFactor(data), Is.EqualTo(1));

        var ship = ShipOfDesign(game, player, "default-ship-design-courier");
        int aboard = ShipCrew.Aboard(ship);
        var mars = game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .Single(c => c.FactionOwnerID == player.Id && c.GetDataBlob<NameDB>().DefaultName == "Mars HQ");
        var info = mars.GetDataBlob<ColonyInfoDB>();
        double cost = Math.Min(SpeciesDBExtensions.MAX_COST, game.GlobalManager.GetGlobalEntityById(info.Population.Keys.First())
            .GetDataBlob<SpeciesDB>().ColonyCost(info.PlanetEntity));
        Assert.That(cost, Is.GreaterThan(0));
        long marsPeople = info.Population.Values.Sum();
        Assert.That(ColonyPowerProcessor.LifeSupport(mars, info).kW, Is.EqualTo(marsPeople * (0.05 + 0.6 * cost)).Within(1e-6));

        data.IncrementTechLevel(CrewTech.LifeSupportEfficiency);
        ShipCrew.RefreshFactionLifeSupport(game, player.Id);
        Assert.That(CrewTech.LifeSupportFactor(data), Is.EqualTo(0.85).Within(1e-12));
        Assert.That(ShipCrew.KWPerCrew(ship), Is.EqualTo(0.85).Within(1e-12), "tech L1 = 0.85 kW per crew");
        Assert.That(ShipCrew.LifeSupport(ship), Is.EqualTo(aboard * 0.85).Within(1e-9));
        Assert.That(ColonyPowerProcessor.LifeSupport(mars, info).kW, Is.EqualTo(marsPeople * (0.05 + 0.6 * 0.85 * cost)).Within(1e-6),
            "the tech multiplies the sealed-world hostility term only");

        var earth = game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .Single(c => c.FactionOwnerID == player.Id && c.GetDataBlob<NameDB>().DefaultName == "Earth HQ");
        var earthInfo = earth.GetDataBlob<ColonyInfoDB>();
        long earthResidents = earthInfo.Population.Values.Sum() + earth.GetDataBlob<ColonyCrewPoolDB>().Total;
        Assert.That(ColonyPowerProcessor.LifeSupport(earth, earthInfo).kW, Is.EqualTo(earthResidents * 0.01).Within(1e-6),
            "garden worlds stay at 0.01 kW per person");

        var early = Start(EarlyTech).player.GetDataBlob<FactionInfoDB>().Data;
        Assert.That(early.Techs.ContainsKey(CrewTech.LifeSupportEfficiency), Is.True, "the early-tech start has it too");
    }

    [Test]
    public void Research_CompletingTheTech_RebooksTheFleetAtOnce()
    {
        var (game, player) = Start(WarpTechStart);
        var data = player.GetDataBlob<FactionInfoDB>().Data;
        var ship = ShipOfDesign(game, player, "default-ship-design-courier");
        int aboard = ShipCrew.Aboard(ship);
        var tech = data.Techs[CrewTech.LifeSupportEfficiency];

        var lab = game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<Pulsar4X.Technology.ResearcherDB>())
            .FirstOrDefault(e => e.FactionOwnerID == player.Id);
        if (lab == null)
            Assert.Ignore("no starting lab in this start");
        var researcher = lab!.GetDataBlob<Pulsar4X.Technology.ResearcherDB>();
        while (researcher.TechQueue.TryDequeue(out _)) { }
        researcher.TechQueue.Enqueue(tech.UniqueID);
        tech.ResearchProgress = tech.ResearchCost; // one day of points finishes it
        new Pulsar4X.Technology.ResearchProcessor().DoResearch(lab);
        if (tech.Level == 0)
            Assert.Ignore("the lab could not run today (power, points or funds)");
        Assert.That(ShipCrew.LifeSupport(ship), Is.EqualTo(aboard * 0.85).Within(1e-9));
    }

    // ---- the event ----

    [Test]
    public void LifeSupportFailing_FiresOnce_AndPausesTheClock_ForThePlayerOnly()
    {
        var (game, player) = Start(WarpTechStart);
        var log = (FactionEventLog)player.GetDataBlob<FactionInfoDB>().EventLog;
        Assert.That(log.HaltsOn(EventType.LifeSupportFailing), Is.True, "default on");
        Assert.That(game.Settings.DefaultHaltOnEvents[EventType.LifeSupportFailing], Is.True);

        var events = new List<Event>();
        Action<Event> on = e => events.Add(e);
        EventManager.Instance.Subscribe(EventType.LifeSupportFailing, on);
        try
        {
            var ship = ShipOfDesign(game, player, "default-ship-design-courier");
            var power = ship.GetDataBlob<EnergyGenAbilityDB>();
            string id = power.EnergyType.UniqueID;
            DateTime at = ship.Manager!.ManagerSubpulses.NextSafeInterruptTime;

            // Empty batteries and a plant that cannot carry the must-run load.
            power.EnergyStored[id] = 0;
            power.AddDemand(power.TotalOutputMax, at);
            EnergyGenProcessor.EnergyGen(ship, at + TimeSpan.FromSeconds(10));
            Assert.That(events.Count(e => e.EntityId == ship.Id), Is.EqualTo(1));
            Assert.That(ship.GetDataBlob<ShipCrewDB>().LifeSupportFailing, Is.True);
            var reason = game.TimePulse.GetPauseReason(player.Id);
            Assert.That(reason, Is.Not.Null, "the player's clock pauses");
            Assert.That(reason!.Event.EventType, Is.EqualTo(EventType.LifeSupportFailing));
            Assert.That(reason.Event.EntityId, Is.EqualTo(ship.Id));
            Assert.That(reason.Event.Message, Does.Contain("life support failing"));

            EnergyGenProcessor.EnergyGen(ship, at + TimeSpan.FromSeconds(20));
            Assert.That(events.Count(e => e.EntityId == ship.Id), Is.EqualTo(1), "one event per failure");

            // Fixed: the flag clears, and a new failure warns again.
            power.AddDemand(-power.TotalOutputMax, at + TimeSpan.FromSeconds(30));
            EnergyGenProcessor.EnergyGen(ship, at + TimeSpan.FromSeconds(40));
            Assert.That(ship.GetDataBlob<ShipCrewDB>().LifeSupportFailing, Is.False);

            // An NPC ship's failure is filed for the NPC and does not stop the shared clock.
            var npcShip = Ships(game).First(s => s.FactionOwnerID != player.Id && s.HasDataBlob<ShipCrewDB>()
                && s.HasDataBlob<EnergyGenAbilityDB>() && ShipCrew.LifeSupport(s) > 0);
            var npcPower = npcShip.GetDataBlob<EnergyGenAbilityDB>();
            DateTime npcAt = npcShip.Manager!.ManagerSubpulses.NextSafeInterruptTime;
            npcPower.EnergyStored[npcPower.EnergyType.UniqueID] = 0;
            npcPower.AddDemand(npcPower.TotalOutputMax, npcAt);
            EnergyGenProcessor.EnergyGen(npcShip, npcAt + TimeSpan.FromSeconds(10));
            Assert.That(events.Count(e => e.EntityId == npcShip.Id), Is.EqualTo(1));
            Assert.That(game.TimePulse.GetPauseReason(player.Id)!.Event.EntityId, Is.EqualTo(ship.Id), "the player's reason is unchanged");
            Assert.That(game.TimePulse.GetPauseReason(npcShip.FactionOwnerID), Is.Null, "NPCs cannot stop the clock");
        }
        finally
        {
            EventManager.Instance.Unsubscribe(EventType.LifeSupportFailing, on);
            game.TimePulse.PauseTime();
        }
    }
}
