using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class BalanceBaselineTests
{
    [Test]
    public void ChemicalFuel_IsOneRefineryDay_AndSteelStaysASmallBatch()
    {
        var store = Load();

        foreach (var id in new[] { "rp-1", "methalox", "hydrolox" })
        {
            var fuel = store.ProcessedMaterials[id];
            Assert.That(fuel.IndustryPointCosts, Is.EqualTo(500), id);
            Assert.That(fuel.OutputAmount, Is.EqualTo(8500), id);
            Assert.That(fuel.ResourceCosts["hydrocarbons"], Is.EqualTo(4250), id);
        }

        var steel = store.ProcessedMaterials["stainless-steel"];
        Assert.That(steel.IndustryPointCosts, Is.EqualTo(20));
        Assert.That(steel.OutputAmount, Is.EqualTo(100));
        Assert.That(store.ProcessedMaterials["rp-1"].Formulas["ExhaustVelocity"], Is.EqualTo("3510"));
    }

    [Test]
    public void Installations_MatchTheClockLadder()
    {
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, Load());
        var faction = FactionFactory.CreateBasicFaction(game, "Balance", "BAL", 0);
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        foreach (var good in data.LockedCargoGoods.GetAll().Values.Select(cargo => cargo.UniqueID).ToList())
            data.Unlock(good);
        data.Unlock("tech-factory-max-size");
        data.Unlock("tech-shipyard-slip-max-size");

        ComponentDesigner.StartResearched = true;
        try
        {
            var mine = Design(faction, data, "mine");
            Assert.That(mine.GetAttribute<MineResourcesAtbDB>().ResourcesPerEconTick["iron"], Is.EqualTo(4500));

            var miner = Design(faction, data, "automine");
            Assert.That(miner.GetAttribute<MineResourcesAtbDB>().ResourcesPerEconTick["iron"], Is.EqualTo(500));

            var factory = Design(faction, data, "factory");
            Assert.That(factory.MassPerUnit, Is.EqualTo(5000));
            Assert.That(factory.IndustryPointCosts, Is.EqualTo(45000));
            Assert.That(factory.CrewReq, Is.EqualTo(500));
            Assert.That(factory.ResourceCosts["iron"], Is.EqualTo(3750));

            var yard = Design(faction, data, "shipyard");
            Assert.That(yard.MassPerUnit, Is.EqualTo(800000));
            Assert.That(yard.IndustryPointCosts, Is.EqualTo(200000));
            Assert.That(yard.GetAttribute<ConstructionAtb>().ConstructionPoints["ship-refit"], Is.EqualTo(5));

            var reactor = Design(faction, data, "reactor");
            Assert.That(reactor.ResourceCosts.ContainsKey("fissile-fuels"), Is.False);
            Assert.That(reactor.ResourceCosts["stainless-steel"], Is.EqualTo(750));
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }

    [Test]
    public void WarpPower_IsADayOnTheFreighterBatteries()
    {
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, Load());
        var faction = FactionFactory.CreateBasicFaction(game, "Balance", "BAL", 0);
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        foreach (var good in data.LockedCargoGoods.GetAll().Values.Select(cargo => cargo.UniqueID).ToList())
            data.Unlock(good);
        foreach (var tech in new[]
        {
            "tech-alcubierre-miniaturization",
            "tech-alcubierre-bubble-creation-cost",
            "tech-alcubierre-sustain-cost",
            "tech-alcubierre-collapse-efficiency",
            "tech-conventional-engine-max-power",
            "tech-conventional-engine-min-power",
            "tech-battery-capacity",
        })
            data.Unlock(tech);

        ComponentDesigner.StartResearched = true;
        try
        {
            var drive = Design(faction, data, "alcubierre-warp-drive", ("Mass", 2000));
            var warp = drive.GetAttribute<WarpDriveAtb>();
            Assert.That(warp.WarpPower, Is.EqualTo(2_000_000));
            Assert.That(warp.BubbleCreationCost, Is.EqualTo(4_000_000d / 3d).Within(0.01));
            Assert.That(warp.BubbleSustainCost, Is.EqualTo(15).Within(0.001));
            Assert.That(warp.BubbleCollapseCost, Is.EqualTo(0).Within(0.001));

            var battery = Design(faction, data, "battery-bank", ("Mass", 2000));
            Assert.That(battery.GetAttribute<EnergyStoreAtb>().MaxStore, Is.EqualTo(1_000_000).Within(0.01));

            var reactor = Design(faction, data, "steam-turbine-reactor");
            Assert.That(reactor.MassPerUnit, Is.EqualTo(2000));
            Assert.That(reactor.GetAttribute<EnergyGenerationAtb>().PowerOutputMax, Is.EqualTo(15).Within(0.001));
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }

    [Test]
    public void EnergyGen_IntegratesKilowattsAndPaysCreationOnce()
    {
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, Load());
        var sys = new StarSystem();
        sys.Initialize(game, "Power");
        var start = sys.StarSysDateTime;

        var charging = ShipWithPower(sys, start, stored: 0, storeMax: 2_000_000, reactorKw: 15);
        EnergyGenProcessor.EnergyGen(charging, start.AddSeconds(10));
        Assert.That(charging.GetDataBlob<EnergyGenAbilityDB>().EnergyStored["electricity"], Is.EqualTo(150).Within(0.001));

        var ship = ShipWithPower(sys, start, stored: 2_000_000, storeMax: 2_000_000, reactorKw: 15);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        double creation = 4_000_000d / 3d;
        EnergyGenProcessor.EnergyGen(ship, start);
        EnergyGenProcessor.EnergyGen(ship, start.AddSeconds(-1));
        power.AddDemand(creation, start.AddSeconds(-1));
        EnergyGenProcessor.EnergyGen(ship, start);
        power.AddDemand(-creation, start);
        power.AddDemand(15, start);

        Assert.That(power.Demand, Is.EqualTo(15).Within(0.001));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(2_000_000d - creation + 15).Within(0.01));

        EnergyGenProcessor.EnergyGen(ship, start.AddDays(30));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(2_000_000d - creation + 15).Within(0.01));
    }

    static Entity ShipWithPower(StarSystem sys, DateTime at, double stored, double storeMax, double reactorKw)
    {
        var energy = new EnergyGenAbilityDB(at)
        {
            EnergyType = new TestElectricity(),
            MaxOutputFromReactor = reactorKw,
        };
        energy.EnergyStored["electricity"] = stored;
        energy.EnergyStoreMax["electricity"] = storeMax;
        var ship = Entity.Create();
        sys.AddEntity(ship, new BaseDataBlob[] { energy });
        return ship;
    }

    sealed class TestElectricity : ICargoable
    {
        public int ID => 0;
        public string UniqueID => "electricity";
        public string CargoTypeID => "energy";
        public string Name => "electricity";
        public long MassPerUnit => 0;
        public double VolumePerUnit => 0;
    }

    static ModDataStore Load()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        return store;
    }

    static ComponentDesign Design(Entity faction, FactionDataStore data, string templateId, params (string Key, double Value)[] props)
    {
        data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = "balance-" + templateId,
            Name = templateId,
            TemplateId = templateId,
        };
        if (props.Length > 0)
        {
            blueprint.Properties = props.Select(prop => new ComponentDesignBlueprint.Property
            {
                Key = prop.Key,
                Value = new JValue(prop.Value),
            }).ToList();
        }
        return ComponentDesignFromJson.Create(faction, data, blueprint);
    }
}
