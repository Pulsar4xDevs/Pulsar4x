using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;

namespace Pulsar4X.Tests;

/// <summary>
/// Repro for the clock dying while the window stays up.
/// Luna's deposit list is iron only, and its mine design still carries a rate for every
/// mineral. The first mining pass (one hour after the start) indexes a deposit that is
/// not there, and that exception ends the simulation task.
/// </summary>
[TestFixture]
public class TimeStopDiagnosticTests
{
    [Test]
    public void SolStart_FirstHour_MiningDoesNotKillTheClock()
    {
        var scene = BuildSolStart();

        Exception? thrown = null;
        try
        {
            scene.Game.TimePulse.TimeStep();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.That(thrown, Is.Null,
            "The first hour of a Sol start stopped the clock.\n" + thrown);
        Assert.That(scene.Sol.StarSysDateTime, Is.GreaterThan(scene.Start));
        Assert.That(scene.Game.TimePulse.GameGlobalDateTime, Is.GreaterThan(scene.Start));
    }

    [Test]
    public void ProcessorFault_stops_the_clock_and_keeps_the_report()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        game.Settings.EnableMultiThreading = false;

        var system = new StarSystem();
        system.Initialize(game, "Fault System");
        system.SetActivityState(SystemActivityState.Foreground);

        var ship = Entity.Create();
        system.AddEntity(ship, new List<BaseDataBlob> { new NameDB("boom") });
        system.ManagerSubpulses.AddEntityInterupt(
            system.ManagerSubpulses.StarSysDateTime, "NotAProcessor", ship);

        var start = game.TimePulse.GameGlobalDateTime;
        Exception? thrown = null;
        try
        {
            game.TimePulse.TimeStep();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.That(thrown, Is.Null);
        var fault = game.TimePulse.LastFault;
        Assert.That(fault, Is.Not.Null);
        Assert.That(fault!.Process, Is.EqualTo("NotAProcessor"));
        Assert.That(fault.SystemName, Is.EqualTo("Fault System"));
        Assert.That(fault.SystemTime, Is.EqualTo(start));
        Assert.That(fault.GlobalTime, Is.EqualTo(start));
        Assert.That(fault.Report, Does.Contain("NotAProcessor").And.Contain("Fault System"));
        Assert.That(game.TimePulse.GameGlobalDateTime, Is.EqualTo(start));
        Assert.That(game.TimePulse.IsRunning, Is.False);
    }

    static Scene BuildSolStart()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);

        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        game.Settings.EnableMultiThreading = false;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);

        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 100_000_000);
        player.FactionOwnerID = player.Id;

        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(sol, store.Species["species-human"]);
        species.FactionOwnerID = player.Id;

        var earth = NameLookup.GetFirstEntityWithName(sol, "Earth");
        ColonyFactory.CreateFromBlueprint(game, player, species, sol, earth, store.Colonies["colony-earth"]);
        ColonyFactory.PlaceOwnedColonies(game, store, player, store.Species["species-human"]);
        game.PostNewGameInitialization();

        return new Scene(game, sol, game.TimePulse.GameGlobalDateTime);
    }

    sealed record Scene(Game Game, StarSystem Sol, DateTime Start);
}
