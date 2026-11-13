using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Sensors;

namespace Pulsar4X.Tests;

/// <summary>
/// QoL engine/API batch: star peak wavelength, mount-flag metadata, and design display stats
/// plus range-partner persistence when a design is saved.
/// </summary>
[TestFixture]
public class QolEngineDataTests
{
    [Test]
    public void StarPeakWavelengthNm_UsesWiensLaw_AndReturnsZeroAtOrBelowAbsoluteZero()
    {
        Assert.That(SensorTools.StarPeakWavelengthNm(-273.15), Is.EqualTo(0));
        Assert.That(SensorTools.StarPeakWavelengthNm(-300), Is.EqualTo(0));
        Assert.That(SensorTools.StarPeakWavelengthNm(0), Is.EqualTo(2898000.0 / 273.15).Within(1e-9));
        // Wien's law: 2,898,000 nm·K / 5,772 K ≈ 502.08 nm (a solar photosphere).
        Assert.That(SensorTools.StarPeakWavelengthNm(5772 - 273.15), Is.EqualTo(2898000.0 / 5772).Within(1e-9));
    }

    [Test]
    public void ComponentMountFlags_MatchEngineBitValues()
    {
        Assert.That((int)ComponentMountFlags.None, Is.EqualTo((int)ComponentMountType.None));
        Assert.That((int)ComponentMountFlags.ShipComponent, Is.EqualTo((int)ComponentMountType.ShipComponent));
        Assert.That((int)ComponentMountFlags.ShipCargo, Is.EqualTo((int)ComponentMountType.ShipCargo));
        Assert.That((int)ComponentMountFlags.PlanetInstallation, Is.EqualTo((int)ComponentMountType.PlanetInstallation));
        Assert.That((int)ComponentMountFlags.Pdc, Is.EqualTo((int)ComponentMountType.PDC));
        Assert.That((int)ComponentMountFlags.Fighter, Is.EqualTo((int)ComponentMountType.Fighter));
        Assert.That((int)ComponentMountFlags.Missile, Is.EqualTo((int)ComponentMountType.Missile));
        Assert.That((int)ComponentMountFlags.ShipComponent, Is.EqualTo(1 << 0));
        Assert.That((int)ComponentMountFlags.Missile, Is.EqualTo(1 << 5));
    }

    [Test]
    public void StarView_PeakWavelengthNm_IsProjectedFromTheStarsTemperature()
    {
        var (game, faction, sol) = StartSol();
        var star = sol.GetAllEntitiesWithDataBlob<StarInfoDB>().Single();
        var view = new GameProjector(game).ProjectEntity(star, faction.Id).GetView<StarView>();
        Assert.That(view, Is.Not.Null);
        Assert.That(view!.PeakWavelengthNm, Is.EqualTo(SensorTools.StarPeakWavelengthNm(view.SurfaceTemperatureC)).Within(1e-9));
        Assert.That(view.PeakWavelengthNm, Is.GreaterThan(0));
    }

    [Test]
    public void TemplatesAndConstructibles_CarryMountFlags()
    {
        var (game, faction, _) = StartSol();
        UnlockCatalogue(faction);
        var snapshot = new GameProjector(game).ProjectComponentDesigns(faction.Id)!;

        var geo = snapshot.Templates.Single(t => t.Id == "geo-surveyor");
        Assert.That(geo.MountFlags, Is.EqualTo(ComponentMountFlags.ShipComponent | ComponentMountFlags.ShipCargo));

        var lab = snapshot.Templates.Single(t => t.Id == "research-lab");
        Assert.That(lab.MountFlags, Is.EqualTo(ComponentMountFlags.PlanetInstallation));
    }

    [Test]
    public void CreateDesign_StoresDisplayProperties_AndProjectsDisplayStats()
    {
        var (game, faction, _) = StartSol();
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        UnlockCatalogue(faction);

        ComponentDesigner.StartResearched = true;
        try
        {
            var template = data.ComponentTemplates["geo-surveyor"];
            var designer = new ComponentDesigner(template, data, faction.GetDataBlob<FactionTechDB>());
            designer.ComponentDesignProperties["Survey Speed"].SetValueFromInput(2);
            designer.EvalAll();
            var design = designer.CreateDesign(faction);

            Assert.That(design.DisplayProperties.ContainsKey("Power Draw"), Is.True);
            Assert.That(design.DisplayProperties["Power Draw"], Is.EqualTo(10).Within(1e-9), "5 kW per survey-speed point");

            var summary = new GameProjector(game).ProjectComponentDesigns(faction.Id)!
                .Designs.Single(d => d.Id == design.UniqueID);
            Assert.That(summary.MassPerUnit, Is.EqualTo(design.MassPerUnit));
            Assert.That(summary.VolumePerUnit, Is.EqualTo(design.VolumePerUnit));
            Assert.That(summary.CreditCost, Is.EqualTo(design.CreditCost));
            Assert.That(summary.ResearchCost, Is.EqualTo(design.ResearchCostValue));
            Assert.That(summary.IndustryPoints, Is.EqualTo(design.IndustryPointCosts));
            Assert.That(summary.EnergyCost, Is.EqualTo(design.EnergyCost));
            Assert.That(summary.DisplayStats["Power Draw"], Is.EqualTo(10).Within(1e-9));
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }

    [Test]
    public void CreateDesign_PersistsRangePartners_InTemplatePropertyValues()
    {
        var (game, faction, _) = StartSol();
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        UnlockCatalogue(faction);

        ComponentDesigner.StartResearched = true;
        try
        {
            var template = data.ComponentTemplates["infrastructure"];
            var designer = new ComponentDesigner(template, data, faction.GetDataBlob<FactionTechDB>());
            var minGravity = designer.ComponentDesignProperties["Min Gravity"];
            var maxGravity = designer.ComponentDesignProperties["Max Gravity"];
            minGravity.SetMin();
            minGravity.SetMax();
            double low = minGravity.MinValue + (minGravity.MaxValue - minGravity.MinValue) * 0.25;
            double high = minGravity.MinValue + (minGravity.MaxValue - minGravity.MinValue) * 0.5;
            minGravity.SetValueFromInput(low);
            maxGravity.SetValueFromInput(high);
            designer.EvalAll();

            var design = designer.CreateDesign(faction);
            var names = design.TemplatePropertyValues.Select(v => v.propName).ToList();
            Assert.That(names, Does.Contain("Min Gravity"));
            Assert.That(names, Does.Contain("Max Gravity"),
                "GuiHint.None range partners must be saved so a reload keeps the full window");
            Assert.That(names, Does.Contain("Max Pressure"));
            var savedHigh = design.TemplatePropertyValues.Single(v => v.propName == "Max Gravity").propValue;
            Assert.That(Convert.ToDouble(savedHigh), Is.EqualTo(high).Within(0.0001));

            var summary = new GameProjector(game).ProjectComponentDesigns(faction.Id)!
                .Designs.Single(d => d.Id == design.UniqueID);
            Assert.That(summary.PropertyValues.Select(p => p.PropertyName), Does.Contain("Max Gravity"));
            Assert.That(summary.PropertyValues.Single(p => p.PropertyName == "Max Gravity").NumericValue, Is.EqualTo(high).Within(0.0001));
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }

    static (Game game, Entity faction, StarSystem sol) StartSol()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);
        var faction = FactionFactory.CreateBasicFaction(game, "QoL", "QOL", 0);
        faction.FactionOwnerID = faction.Id;
        var sol = game.Systems.Single(s => s.ID == "system-sol");
        return (game, faction, sol);
    }

    static void UnlockCatalogue(Entity faction)
    {
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        foreach (var good in data.LockedCargoGoods.GetAll().Values.Select(cargo => cargo.UniqueID).ToList())
            data.Unlock(good);
        foreach (var id in data.LockedComponentTemplates.Keys.ToList())
            data.Unlock(id);
        foreach (var id in data.LockedTechs.Keys.ToList())
            data.Unlock(id);
        foreach (var id in data.Techs.Keys.ToList())
            data.IncrementTechLevel(id);
    }
}
