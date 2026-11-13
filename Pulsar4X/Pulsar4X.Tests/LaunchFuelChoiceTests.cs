using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Interfaces;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Technology;

namespace Pulsar4X.Tests;

/// <summary>
/// Jon: "the launch complex should be a design choice on which fuel it uses." The launch complex
/// design picks one launch fuel (the engines' fuel picker, rocket propellants only). Its pads burn
/// only that fuel and hold a launch with "Not enough {fuel}" when it is short. A design or pad
/// with no choice (the default design, an old save) burns RP-1.
/// </summary>
[TestFixture]
public class LaunchFuelChoiceTests
{
    Game _game = null!;
    Entity _player = null!;
    StarSystem _sol = null!;
    Entity _planet = null!;
    Entity _earth = null!;
    FactionInfoDB _info = null!;

    [SetUp]
    public void SetUp()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        _game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        _game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(_game, store.Systems["system-sol"]);
        _player = FactionFactory.CreateBasicFaction(_game, "United Earth Corp", "UEC", 0);
        _player.FactionOwnerID = _player.Id;
        _sol = _game.Systems.Single(s => s.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(_sol, store.Species["species-human"]);
        species.FactionOwnerID = _player.Id;
        _info = _player.GetDataBlob<FactionInfoDB>();
        _info.Species.Add(species);
        _planet = NameLookup.GetFirstEntityWithName(_sol, "Earth");
        _earth = ColonyFactory.CreateFromBlueprint(_game, _player, species, _sol, _planet, store.Colonies["colony-earth"]);
    }

    ICargoable Fuel(string id) => _info.Data.CargoGoods.GetAny(id)!;

    long Stock(string id)
    {
        var item = Fuel(id);
        var store = _earth.GetDataBlob<CargoStorageDB>().TypeStores[item.CargoTypeID];
        return store.CurrentStoreInUnits.TryGetValue(item.ID, out var units) ? units : 0;
    }

    void SetStock(string id, double kg)
    {
        var cargo = _earth.GetDataBlob<CargoStorageDB>();
        CargoTransferProcessor.AddRemoveCargoMass(_earth, Fuel(id), -cargo.GetMassStored(Fuel(id), false));
        if (kg > 0)
            CargoTransferProcessor.AddRemoveCargoMass(_earth, Fuel(id), kg);
    }

    int Ships() => _sol.GetAllEntitiesWithDataBlob<ShipInfoDB>().Count;

    double HermesCost()
    {
        var hermes = _earth.GetDataBlob<LaunchComplexDB>().LaunchQueue.Single(e => e.ShipName == "ISS Hermes");
        var design = _info.ShipDesigns[hermes.DesignId];
        return OrbitMath.FuelCostToOrbit(_planet, design.MassPerUnit, OrbitMath.LowOrbitRadius(_planet));
    }

    ComponentDesign MethaloxComplex()
    {
        var designer = new ComponentDesigner(_info.Data.ComponentTemplates["launch-complex"], _info.Data, _player.GetDataBlob<FactionTechDB>())
        {
            Name = "Methalox Launch Complex",
        };
        designer.ComponentDesignProperties["Launch Fuel"].SetValueFromString("methalox");
        designer.EvalAll();
        return designer.CreateDesign(_player);
    }

    /// <summary>Installs a methalox launch complex and takes out the default pad, so Hermes goes to the new one.</summary>
    LaunchPad UseMethaloxPad()
    {
        var launch = _earth.GetDataBlob<LaunchComplexDB>();
        var oldPads = launch.Pads.Keys.ToList();
        _earth.AddComponent(MethaloxComplex(), 1);
        foreach (var id in oldPads)
            launch.Pads.Remove(id);
        return launch.Pads.Values.Single();
    }

    [Test]
    public void Designer_PicksTheLaunchFuel_WithTheEnginesFuelPicker()
    {
        var designer = new ComponentDesigner(_info.Data.ComponentTemplates["launch-complex"], _info.Data, _player.GetDataBlob<FactionTechDB>());
        var fuel = designer.ComponentDesignProperties["Launch Fuel"];
        Assert.That(fuel.GuiHint, Is.EqualTo(GuiHint.GuiFuelTypeSelection));
        Assert.That(fuel.ValueString, Is.EqualTo("rp-1"), "the template default");
        Assert.That(fuel.GuidDictionary["fuel-storage"].StrResult, Is.EqualTo("conventional"), "rocket propellants, like the conventional engine");

        var design = MethaloxComplex();
        Assert.That(design.GetAttribute<LaunchComplexAtb>().LaunchFuel, Is.EqualTo("methalox"));
        Assert.That(design.GetAttribute<LaunchComplexAtb>().AtbDescription(), Does.Contain("methalox"));

        // The client's submit path carries the choice to the server's designer.
        var methaloxDesigner = new ComponentDesigner(_info.Data.ComponentTemplates["launch-complex"], _info.Data, _player.GetDataBlob<FactionTechDB>());
        methaloxDesigner.ComponentDesignProperties["Launch Fuel"].SetValueFromString("methalox");
        var inputs = DesignerInputs.Extract(methaloxDesigner);
        Assert.That(inputs.Single(i => i.PropertyName == "Launch Fuel").StringValue, Is.EqualTo("methalox"));
        var rebuilt = DesignerInputs.Build(_info.Data, _player.GetDataBlob<FactionTechDB>(), _info.Data.ComponentTemplates["launch-complex"], inputs);
        Assert.That(rebuilt.ComponentDesignProperties["Launch Fuel"].ValueString, Is.EqualTo("methalox"));
    }

    [Test]
    public void DefaultDesign_BurnsRp1_AndEarthsPadSaysSo()
    {
        Assert.That(_info.ComponentDesigns["default-design-launch-complex"].GetAttribute<LaunchComplexAtb>().LaunchFuel, Is.EqualTo("rp-1"));
        var pad = _earth.GetDataBlob<LaunchComplexDB>().Pads.Values.Single();
        Assert.That(pad.LaunchFuelId, Is.EqualTo("rp-1"));

        var view = new GameProjector(_game).ProjectEntity(_earth, _player.Id).GetView<IndustryView>()!;
        Assert.That(view.Pads.Single().LaunchFuel, Is.EqualTo("RP-1"));
    }

    [Test]
    public void Launch_BurnsOnlyTheDesignsFuel()
    {
        var pad = UseMethaloxPad();
        Assert.That(pad.LaunchFuel, Is.EqualTo("methalox"));
        double cost = HermesCost();
        SetStock("methalox", Math.Ceiling(cost) + 1000);
        long rp1 = Stock("rp-1"), hydrolox = Stock("hydrolox"), fissile = Stock("fissile-fuels");
        long methalox = Stock("methalox");
        int ships = Ships();

        new LaunchComplexProcessor().ProcessEntity(_earth, 0);

        Assert.That(Ships(), Is.EqualTo(ships + 1), "Hermes launches from the methalox pad");
        Assert.That(Stock("methalox"), Is.EqualTo(methalox - cost).Within(1));
        Assert.That(Stock("rp-1"), Is.EqualTo(rp1), "other fuels untouched");
        Assert.That(Stock("hydrolox"), Is.EqualTo(hydrolox));
        Assert.That(Stock("fissile-fuels"), Is.EqualTo(fissile));
    }

    [Test]
    public void Launch_WaitsWithTheFuelNamed_WhenTheDesignsFuelIsShort()
    {
        var pad = UseMethaloxPad();
        double cost = HermesCost();
        // Plenty of RP-1 (50,000 t) but methalox 1 t short.
        SetStock("methalox", Math.Floor(cost) - 1000);
        Assert.That(Stock("rp-1"), Is.GreaterThan(cost));
        long rp1 = Stock("rp-1"), methalox = Stock("methalox"), fissile = Stock("fissile-fuels");
        int ships = Ships();

        new LaunchComplexProcessor().ProcessEntity(_earth, 0);

        Assert.That(Ships(), Is.EqualTo(ships), "the launch waits");
        Assert.That(pad.ShipName, Is.EqualTo("ISS Hermes"), "Hermes stays on the pad");
        Assert.That(Stock("methalox"), Is.EqualTo(methalox), "a launch that cannot go takes nothing");
        Assert.That(Stock("rp-1"), Is.EqualTo(rp1), "RP-1 is not this pad's fuel");
        Assert.That(Stock("fissile-fuels"), Is.EqualTo(fissile));
        Assert.That(LaunchComplexProcessor.PadHoldReason(_earth, pad), Is.EqualTo("Not enough Methalox"));
        var view = new GameProjector(_game).ProjectEntity(_earth, _player.Id).GetView<IndustryView>()!;
        Assert.That(view.Pads.Single().Reason, Is.EqualTo("Not enough Methalox"));
        Assert.That(view.Pads.Single().LaunchFuel, Is.EqualTo("Methalox"));

        // Top methalox up and the waiting launch goes on the next pass.
        SetStock("methalox", Math.Ceiling(cost) + 10);
        new LaunchComplexProcessor().ProcessEntity(_earth, 0);
        Assert.That(Ships(), Is.EqualTo(ships + 1));
        Assert.That(LaunchComplexProcessor.PadHoldReason(_earth, pad), Is.Empty);
    }
}
