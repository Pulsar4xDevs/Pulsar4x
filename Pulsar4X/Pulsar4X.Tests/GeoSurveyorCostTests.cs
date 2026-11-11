using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.JumpPoints;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests;

/// <summary>
/// Geological surveyor costs used to grow with the square or cube of survey speed, so the
/// cheapest survey point was always the smallest component. They now scale with that same
/// speed, and a speed of 1 keeps the old numbers.
/// </summary>
[TestFixture]
public class GeoSurveyorCostTests
{
    [Test]
    public void GeoSurveyor_CostsScaleLinearlyWithSurveySpeed() => AssertLinear("geo-surveyor");

    // The gravitational surveyor shared the geo surveyor's square/cube formulas.
    [Test]
    public void GravSurveyor_CostsScaleLinearlyWithSurveySpeed() => AssertLinear("gravitational-surveyor");

    static void AssertLinear(string templateId)
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        var faction = FactionFactory.CreateBasicFaction(game, "Survey", "SRV", 0);
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        foreach (var id in new[] { templateId, "stainless-steel", "plastic", "aluminium", "copper" })
            data.Unlock(id);

        var slow = Design(faction, data, templateId, 1);
        var mid = Design(faction, data, templateId, 2);
        var fast = Design(faction, data, templateId, 10);

        AssertCosts(slow, 1);
        AssertCosts(mid, 2);
        AssertCosts(fast, 10);
    }

    static void AssertCosts(ComponentDesign design, int speed)
    {
        uint actual = design.TryGetAttribute<GeoSurveyAtb>(out var geo) ? geo.Speed : design.GetAttribute<GravSurveyAtb>().Speed;
        Assert.That(actual, Is.EqualTo((uint)speed), "survey speed");
        Assert.That(design.MassPerUnit, Is.EqualTo(100L * speed), "mass");
        Assert.That(design.VolumePerUnit, Is.EqualTo(10d * speed).Within(1e-9), "volume");
        Assert.That(design.CrewReq, Is.EqualTo(8 * speed), "crew");
        Assert.That(design.ResearchCostValue, Is.EqualTo((long)speed), "research");
        Assert.That(design.CreditCost, Is.EqualTo(100 * speed), "credits");
        Assert.That(design.IndustryPointCosts, Is.EqualTo(100L * speed), "build points");
        Assert.That(design.ResourceCosts["stainless-steel"], Is.EqualTo(40L * speed));
        Assert.That(design.ResourceCosts["plastic"], Is.EqualTo(20L * speed));
        Assert.That(design.ResourceCosts["aluminium"], Is.EqualTo(10L * speed));
        Assert.That(design.ResourceCosts["copper"], Is.EqualTo(30L * speed));
        Assert.That(design.DestructionPercent, Is.EqualTo(0.5f).Within(1e-6f), "HTK stays flat");
    }

    static ComponentDesign Design(Entity faction, FactionDataStore data, string templateId, int speed)
    {
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = templateId + "-cost-" + speed,
            Name = templateId + " " + speed,
            TemplateId = templateId,
            Properties = new()
            {
                new ComponentDesignBlueprint.Property
                {
                    Key = "Survey Speed",
                    Value = new JValue(speed),
                },
            },
        };
        return ComponentDesignFromJson.Create(faction, data, blueprint);
    }
}
