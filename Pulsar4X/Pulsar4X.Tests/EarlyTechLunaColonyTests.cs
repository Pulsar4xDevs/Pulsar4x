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
using Pulsar4X.Engine.Factories;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Orbital;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests;

/// <summary>
/// Headless walk of early-tech: research Geological Surveyor, survey Luna, found a colony there.
/// Does not change engine or data; workarounds are test scaffolding only and are named in the log.
/// </summary>
[TestFixture]
[NonParallelizable]
public class EarlyTechLunaColonyTests
{
    const string BaseMod = "Pulsar4x";
    const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";
    const string GeoSurveyorTech = "tech-geo-surveyor";
    const string EmSensorsTech = "tech-electromagnetic-sensors";
    const string GeoTemplate = "geo-surveyor";
    const string GeoDesignId = "default-design-geo-surveyor";

    [Test, Timeout(180000)]
    public void EarlyTech_SurveyThenColony_OnLuna()
    {
        var (game, faction) = StartGameLikeTheMenu();
        game.Settings.EnforceSingleThread = true;
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        var info = faction.GetDataBlob<FactionInfoDB>();
        var translator = new CommandTranslator(game);

        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var luna = NameLookup.GetFirstEntityWithName(sol, "Luna"); // unique id moon-luna
        var ship = sol.GetAllEntitiesWithDataBlob<ShipInfoDB>()
            .Single(s => s.GetDataBlob<NameDB>().GetName(faction.Id) == "Starship I");

        // Research to Geological Surveyor L1. Foundations start unresearched; Electromagnetic
        // Sensors must be completed first or the geo tech stays locked (techs.json Unlocks).
        Assert.That(data.IsResearchable(EmSensorsTech), Is.True, EmSensorsTech);
        data.IncrementTechLevel(EmSensorsTech);
        Assert.That(data.IsResearchable(GeoSurveyorTech), Is.True,
            "tech-geo-surveyor should open after Electromagnetic Sensors (techs.json, EM Unlocks).");
        data.IncrementTechLevel(GeoSurveyorTech);
        Assert.That(data.Techs[GeoSurveyorTech].Level, Is.EqualTo(1));
        Assert.That(data.ComponentTemplates.ContainsKey(GeoTemplate), Is.True,
            "Expected geo-surveyor template to unlock at tech L1 (techs.json Unlocks of tech-geo-surveyor).");

        // Use the existing Mk1 blueprint if the start did not already instantiate it
        // (early-tech ComponentDesigns does not list it).
        if (!info.ComponentDesigns.ContainsKey(GeoDesignId))
        {
            Assert.That(game.StartingGameData.ComponentDesigns.ContainsKey(GeoDesignId), Is.True,
                "default-design-geo-surveyor is in warp-tech-start/designs/componentDesigns.json, reused by early-tech.");
            ComponentDesigner.StartResearched = true;
            try
            {
                ComponentDesignFromJson.Create(faction, data, game.StartingGameData.ComponentDesigns[GeoDesignId]);
            }
            finally
            {
                ComponentDesigner.StartResearched = false;
            }
        }
        Assert.That(info.ComponentDesigns.ContainsKey(GeoDesignId), Is.True, GeoDesignId);
        // Scaffolding: mount the Mk1 on the starting Starship. The early-tech ship does not
        // carry a surveyor, and the template is not in StartingItems, so this is a test-only fit.
        ship.AddComponent(new ComponentInstance(info.ComponentDesigns[GeoDesignId]));
        Assert.That(ship.HasDataBlob<GeoSurveyAbilityDB>(), Is.True,
            "AddComponent should install GeoSurveyAtb (GeoSurveyAtb.OnComponentInstallation).");

        var survey = translator.Translate(faction, ship, new GeoSurveyCommand(ship.Id, luna.Id));
        if (!survey.Accepted)
        {
            // Captained ships get a ServeyBodies goal that should include travel
            // (CommandTranslator.TranslateGeoSurvey). If the planner cannot reach Luna,
            // park at the body (test scaffolding only) and re-issue.
            TestContext.WriteLine($"First GeoSurveyCommand rejected: {survey.RejectionReason}");
            ParkAtBody(ship, luna);
            TestContext.WriteLine("Workaround: ParkAtBody (copied from GeoSurveyFleetTests) after the first command failed.");
            survey = translator.Translate(faction, ship, new GeoSurveyCommand(ship.Id, luna.Id));
        }
        Assert.That(survey.Accepted, Is.True, "GeoSurveyCommand failed: " + survey.RejectionReason);

        // Mk1 Survey Speed is 1; Luna is 250 points; ticks are daily (GeoSurveyOrder.Execute).
        // Leave headroom for travel from Earth and a late first survey tick.
        const int maxDays = 400;
        var start = game.TimePulse.GameGlobalDateTime;
        while (!luna.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(faction.Id)
               && game.TimePulse.GameGlobalDateTime < start + TimeSpan.FromDays(maxDays))
        {
            Step(game, TimeSpan.FromDays(10));
            Assert.That(game.TimePulse.LastFault, Is.Null, game.TimePulse.LastFault?.Report);
        }
        Assert.That(luna.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(faction.Id), Is.True,
            $"Luna survey did not complete in {maxDays} days. Ship parent={ship.GetSOIParentEntity()?.GetDataBlob<NameDB>().DefaultName}; "
            + "points applied daily in GeoSurveyOrder.cs after the first day.");

        var found = translator.Translate(faction, faction, new CreateColonyCommand(faction.Id, luna.Id));
        Assert.That(found.Accepted, Is.True, "CreateColonyCommand failed: " + found.RejectionReason);
        Assert.That(luna.TryGetFactionColonyOnBody(faction.Id, out var colony), Is.True,
            "Expected a player colony on Luna after CreateColonyCommand.");
        Assert.That(colony.GetDataBlob<ColonyInfoDB>().PlanetEntity, Is.EqualTo(luna));
    }

    static (Game game, Entity faction) StartGameLikeTheMenu()
    {
        ModsState.RefreshModsList("Data");
        var paths = new[] { ModsState.AvailableMods.Single(m => m.Mod.ModName == BaseMod).Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), EarlyTech);
        var catalog = GameLifecycle.BuildCatalog(store);
        var request = GameLifecycle.BuildQuickstartRequest(paths, catalog, 12345678);
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null, "CreateGameCore returned null");
        return (result!.Value.game, result.Value.faction);
    }

    static void Step(Game game, TimeSpan length)
    {
        game.TimePulse.TimeStep(game.TimePulse.GameGlobalDateTime + length);
        game.TimePulse.WaitUntilStopped();
    }

    /// <summary>Test scaffolding copied from GeoSurveyFleetTests.ParkAtBody. Used only if the survey command cannot travel.</summary>
    static void ParkAtBody(Entity ship, Entity body)
    {
        if (ship.HasDataBlob<OrbitDB>())
            ship.RemoveDataBlob<OrbitDB>();
        var pos = ship.GetDataBlob<PositionDB>();
        pos.SetParent(body);
        pos.RelativePosition = Vector3.Zero;
    }
}
