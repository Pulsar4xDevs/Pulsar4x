using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class PrefabFacilitiesTests
{
    [Test]
    public void CratesStayFreightable_AndTheNewSitesDoNot()
    {
        var store = Load();

        Assert.That(store.ComponentTemplates.ContainsKey("space-port"), Is.False);

        AssertSiteOnly(store, "research-lab");
        AssertSiteOnly(store, "shipyard");
        AssertSiteOnly(store, "factory-site");
        AssertSiteOnly(store, "refinery-site");
        AssertSiteOnly(store, "local-construction-site");
        AssertSiteOnly(store, "logistics-office-site");
        AssertSiteOnly(store, "warehouse-site");
        AssertSiteOnly(store, "infrastructure-site");
        AssertSiteOnly(store, "spaceport");

        AssertCrate(store, "factory");
        AssertCrate(store, "refinery");
        AssertCrate(store, "local-construction");
        AssertCrate(store, "warehouse-facility");
        AssertCrate(store, "planetary-spaceport-crate");
        AssertCrate(store, "infrastructure");

        var solar = store.ComponentTemplates["solarArray"];
        Assert.That(solar.MountType.HasFlag(ComponentMountType.ShipCargo), Is.True);
        Assert.That(solar.MountType.HasFlag(ComponentMountType.ShipComponent), Is.True);
        Assert.That(solar.MountType.HasFlag(ComponentMountType.PlanetInstallation), Is.False);
    }

    [Test]
    public void ThePrefabPortTransfers_AndTheSitePortAlsoStores()
    {
        var (faction, data, store) = Faction();
        var crate = Design(faction, data, store, "default-design-planetary-spaceport-crate");
        var port = Design(faction, data, store, "default-design-spaceport");

        Assert.That(crate.HasAttribute<CargoTransferAtb>(), Is.True);
        Assert.That(crate.HasAttribute<CargoStorageAtb>(), Is.False);
        Assert.That(port.HasAttribute<CargoTransferAtb>(), Is.True);
        Assert.That(port.HasAttribute<CargoStorageAtb>(), Is.True);
    }

    [Test]
    public void StockInfrastructureWindows_CoverTheirPlanets()
    {
        var (faction, data, store) = Faction();
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);
        var sol = game.Systems.Single(s => s.ID == "system-sol");

        var mars = Design(faction, data, store, "default-design-infrastructure-mars");
        Assert.That(mars.GetAttribute<GravityToleranceAtb>().SupportsBodyGravity(3.72), Is.True);
        Assert.That(mars.GetAttribute<GravityToleranceAtb>().SupportsBodyGravity(9.81), Is.False);
        Assert.That(mars.GetAttribute<PressureToleranceAtb>().SupportsBodyPressure(0.87), Is.True);

        var luna = Design(faction, data, store, "default-design-infrastructure-luna");
        Assert.That(luna.GetAttribute<GravityToleranceAtb>().SupportsBodyGravity(1.625), Is.True);
        Assert.That(luna.GetAttribute<PressureToleranceAtb>().SupportsBodyPressure(0), Is.True);

        var ceresBody = NameLookup.GetFirstEntityWithName(sol, "Ceres");
        double ceresGravity = ceresBody.GetDataBlob<MassVolumeDB>().SurfaceGravity;
        var ceres = Design(faction, data, store, "default-design-infrastructure-ceres");
        Assert.That(ceres.GetAttribute<GravityToleranceAtb>().SupportsBodyGravity(ceresGravity), Is.True, $"Ceres gravity {ceresGravity}");
        Assert.That(ceres.GetAttribute<PressureToleranceAtb>().SupportsBodyPressure(0), Is.True);
    }

    [Test]
    public void OutpostsStartOnCrates_AndOnlyLunaAndCeresKeepCityHall()
    {
        var store = Load();
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);

        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(sol, store.Species["species-human"]);
        species.FactionOwnerID = player.Id;
        player.GetDataBlob<FactionInfoDB>().Species.Add(species);
        var earth = NameLookup.GetFirstEntityWithName(sol, "Earth");
        ColonyFactory.CreateFromBlueprint(game, player, species, sol, earth, store.Colonies["colony-earth"]);
        ColonyFactory.PlacePlayerColonies(game, store, player, species, "colony-earth");
        ColonyFactory.PlaceOwnedColonies(game, store, player, store.Species["species-human"]);

        var mars = ColonyOn(sol, "Mars");
        // Energy phase 2 seeds every start colony with a site-built power plant (no plant
        // template ships as a crate yet), so the outpost's reactor is the one exception.
        Assert.That(SiteOnly(mars).Where(d => !d.HasAttribute<Pulsar4X.Energy.PowerPlantAtb>()), Is.Empty);
        Assert.That(SiteOnly(mars).Select(d => d.UniqueID), Is.EqualTo(new[] { "default-design-fission-plant-2" }));

        Assert.That(SiteOnly(ColonyOn(sol, "Luna")).Where(d => !d.HasAttribute<Pulsar4X.Energy.PowerPlantAtb>()).Select(d => d.UniqueID), Is.EqualTo(new[] { "default-design-city-hall" }));
        Assert.That(SiteOnly(ColonyOn(sol, "Ceres")).Where(d => !d.HasAttribute<Pulsar4X.Energy.PowerPlantAtb>()).Select(d => d.UniqueID), Is.EqualTo(new[] { "default-design-city-hall" }));
    }

    [Test]
    public void EarthStartsWithSiteInfrastructure()
    {
        var store = Load();
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);

        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(sol, store.Species["species-human"]);
        species.FactionOwnerID = player.Id;
        var earthBody = NameLookup.GetFirstEntityWithName(sol, "Earth");
        var earth = ColonyFactory.CreateFromBlueprint(game, player, species, sol, earthBody, store.Colonies["colony-earth"]);

        var instances = earth.GetDataBlob<ComponentInstancesDB>();
        int site = instances.DesignsAndComponentCount
            .Where(row => row.Key.UniqueID == "default-design-infrastructure-site")
            .Sum(row => row.Value);
        int prefab = instances.DesignsAndComponentCount
            .Where(row => row.Key.UniqueID == "default-design-infrastructure")
            .Sum(row => row.Value);
        Assert.That(site, Is.EqualTo(5));
        Assert.That(prefab, Is.EqualTo(0));
        Assert.That(instances.GetPopulationSupportValue(earthBody), Is.EqualTo(20000));
    }

    [Test]
    public void SiteWindows_MatchThePrefab_AndAMarsWindowCoversMars()
    {
        var (faction, data, store) = Faction();
        data.Unlock("tech-infra-gravity-range");
        data.Unlock("tech-infra-pressure-range");
        var tech = faction.GetDataBlob<FactionTechDB>();
        var site = new ComponentDesigner(store.ComponentTemplates["infrastructure-site"], data, tech, "site-window");
        var prefab = new ComponentDesigner(store.ComponentTemplates["infrastructure"], data, tech, "prefab-window");
        var siteGravity = site.ComponentDesignProperties["Min Gravity"];
        var prefabGravity = prefab.ComponentDesignProperties["Min Gravity"];
        var sitePressure = site.ComponentDesignProperties["Min Pressure"];
        var prefabPressure = prefab.ComponentDesignProperties["Min Pressure"];
        siteGravity.SetMaxRange();
        prefabGravity.SetMaxRange();
        sitePressure.SetMaxRange();
        prefabPressure.SetMaxRange();

        Assert.That(siteGravity.MaxRangeValue, Is.EqualTo(prefabGravity.MaxRangeValue).Within(1e-6));
        Assert.That(siteGravity.MaxRangeValue, Is.GreaterThanOrEqualTo(1.9));
        Assert.That(sitePressure.MaxRangeValue, Is.EqualTo(prefabPressure.MaxRangeValue).Within(1e-6));

        var mars = Design(faction, data, store, "mars-site-infra", "infrastructure-site",
            ("Min Gravity", 3.0),
            ("Max Gravity", 4.9),
            ("Min Pressure", 0.85),
            ("Max Pressure", 1.05));
        Assert.That(mars.GetAttribute<GravityToleranceAtb>().SupportsBodyGravity(3.72076), Is.True);
        Assert.That(mars.GetAttribute<PressureToleranceAtb>().SupportsBodyPressure(0.87), Is.True);
    }

    [Test]
    public void SiteInfrastructure_CostsMoreAwayFromEarth()
    {
        var (faction, data, store) = Faction();
        var earth = Design(faction, data, store, "default-design-infrastructure-site");
        var luna = Design(faction, data, store, "balance-luna-infra", "infrastructure-site",
            ("Min Gravity", 1.4),
            ("Max Gravity", 1.8),
            ("Min Pressure", 0.0),
            ("Max Pressure", 0.2));

        double earthFactor = earth.ResourceCosts["iron"] / (earth.MassPerUnit * 0.45);
        double lunaFactor = luna.ResourceCosts["iron"] / (luna.MassPerUnit * 0.45);
        Assert.That(earthFactor, Is.EqualTo(1).Within(0.05));
        Assert.That(lunaFactor, Is.GreaterThan(earthFactor));
    }

    static void AssertSiteOnly(ModDataStore store, string id)
    {
        var mount = store.ComponentTemplates[id].MountType;
        Assert.That(mount.HasFlag(ComponentMountType.PlanetInstallation), Is.True, id);
        Assert.That(mount.HasFlag(ComponentMountType.ShipCargo), Is.False, id);
    }

    static void AssertCrate(ModDataStore store, string id)
    {
        var mount = store.ComponentTemplates[id].MountType;
        Assert.That(mount.HasFlag(ComponentMountType.PlanetInstallation), Is.True, id);
        Assert.That(mount.HasFlag(ComponentMountType.ShipCargo), Is.True, id);
    }

    static List<ComponentDesign> SiteOnly(Entity colony)
    {
        return colony.GetDataBlob<ComponentInstancesDB>().AllDesigns.Values
            .Where(design => design.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation)
                && !design.ComponentMountType.HasFlag(ComponentMountType.ShipCargo))
            .OrderBy(design => design.UniqueID)
            .ToList();
    }

    static Entity ColonyOn(StarSystem sol, string bodyName)
    {
        var body = NameLookup.GetFirstEntityWithName(sol, bodyName);
        return sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<ColonyInfoDB>().PlanetEntity == body);
    }

    static ModDataStore Load()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        return store;
    }

    static (Entity Faction, FactionDataStore Data, ModDataStore Store) Faction()
    {
        var store = Load();
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        var faction = FactionFactory.CreateBasicFaction(game, "Prefab", "PF", 0);
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        foreach (var good in data.LockedCargoGoods.GetAll().Values.Select(cargo => cargo.UniqueID).ToList())
            data.Unlock(good);
        return (faction, data, store);
    }

    static ComponentDesign Design(Entity faction, FactionDataStore data, ModDataStore store, string designId)
    {
        var blueprint = store.ComponentDesigns[designId];
        data.Unlock(blueprint.TemplateId);
        ComponentDesigner.StartResearched = true;
        try
        {
            return ComponentDesignFromJson.Create(faction, data, blueprint);
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }

    static ComponentDesign Design(
        Entity faction,
        FactionDataStore data,
        ModDataStore store,
        string designId,
        string templateId,
        params (string Key, double Value)[] props)
    {
        data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = designId,
            Name = designId,
            TemplateId = templateId,
            Properties = props.Select(prop => new ComponentDesignBlueprint.Property
            {
                Key = prop.Key,
                Value = new JValue(prop.Value),
            }).ToList(),
        };
        ComponentDesigner.StartResearched = true;
        try
        {
            return ComponentDesignFromJson.Create(faction, data, blueprint);
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }
}
