using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.DataStructures;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// The ship Power tab's read surface: ShipPowerView's sources (solar, RTG after decay, reactor fuel
    /// left, fuel-cell propellant in the hold) and load groups with their supply, owner only, and the
    /// per-copy crew rows on ShipView. Built from the real phase 6 templates on a real ship.
    /// </summary>
    [TestFixture]
    public class ShipPowerTabTests
    {
        Game _game = null!;
        StarSystem _sys = null!;
        Entity _faction = null!;
        Entity _planet = null!;
        GameProjector _projector = null!;
        int _designs;

        FactionInfoDB Info => _faction.GetDataBlob<FactionInfoDB>();
        FactionDataStore Data => Info.Data;

        [SetUp]
        public void SetUp()
        {
            var store = new ModDataStore();
            new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
            new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
            _game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
            _game.Settings.EnforceSingleThread = true;
            _faction = FactionFactory.CreateBasicFaction(_game, "Power Tab", "PT", 0);
            _faction.FactionOwnerID = _faction.Id;
            foreach (var good in Data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
                Data.Unlock(good);
            Data.Unlock("plastic-armor");
            _sys = new StarSystem();
            _sys.Initialize(_game, "Power Tab");
            TestingUtilities.BasicSol(_sys);
            _planet = TestingUtilities.BasicEarth(_sys);
            _planet.GetDataBlob<PositionDB>().AbsolutePosition = new Vector3(1.496e11, 0, 0);
            _projector = new GameProjector(_game);
        }

        ComponentDesign Design(string templateId, params (string key, double value)[] properties)
        {
            Data.Unlock(templateId);
            var blueprint = new ComponentDesignBlueprint
            {
                UniqueID = templateId + "-tab-" + (++_designs),
                Name = templateId,
                TemplateId = templateId,
            };
            if (properties.Length > 0)
                blueprint.Properties = properties.Select(p => new ComponentDesignBlueprint.Property { Key = p.key, Value = new JValue(p.value) }).ToList();
            ComponentDesigner.StartResearched = true;
            try { return ComponentDesignFromJson.Create(_faction, Data, blueprint); }
            finally { ComponentDesigner.StartResearched = false; }
        }

        sealed record Parts(ComponentDesign Solar, ComponentDesign Rtg, ComponentDesign Reactor, ComponentDesign Cell,
            ComponentDesign Sensor, ComponentDesign Warp, ComponentDesign Miner);

        (Entity ship, Parts parts) BuildShip()
        {
            var parts = new Parts(Design("solarArray"), Design("rtg"), Design("steam-turbine-reactor"), Design("fuel-cell"),
                Design("passive-sensor"), Design("alcubierre-warp-drive"), Design("asteroid-miner"));
            var components = new List<(ComponentDesign, int)>
            {
                (parts.Solar, 1), (parts.Rtg, 2), (parts.Reactor, 1), (parts.Cell, 1),
                (parts.Sensor, 2), (parts.Warp, 1), (parts.Miner, 1),
            };
            var design = new ShipDesign(Info, "Power Tab Test", components, (Data.Armor["plastic-armor"], 0f));
            design.Initialise(Info);
            var ship = ShipFactory.CreateShip(design, _faction, _planet, "Tabby");
            // The test hull has no bridge or quarters, so it would sail with nobody aboard; crew it fully
            // so its miner runs (crew plan phase 3).
            ship.GetDataBlob<ShipCrewDB>().Aboard = ShipCrew.SplitOf(ship).Required;

            // The cells' propellant in the hold.
            var fuel = Data.CargoGoods.GetAny(parts.Cell.GetAttribute<EnergyGenerationAtb>().FuelType)!;
            var cargo = new CargoStorageDB(fuel.CargoTypeID, 1e9);
            ship.SetDataBlob(cargo);
            Assert.That(cargo.AddCargoByUnit(fuel, 10), Is.EqualTo(10));
            return (ship, parts);
        }

        static void FillBatteries(EnergyGenAbilityDB power)
        {
            string id = power.EnergyType.UniqueID;
            power.EnergyStored[id] = power.EnergyStoreMax[id];
        }

        [Test]
        public void Owner_SeesEachSource_WithDecay_FuelLeft_AndPropellant()
        {
            var (ship, parts) = BuildShip();
            var power = ship.GetDataBlob<EnergyGenAbilityDB>();

            // Age the RTGs by one half-life: their output now is half their output new.
            var rtg = parts.Rtg.GetAttribute<EnergyGenerationAtb>();
            Assert.That(power.DecaySources, Has.Count.EqualTo(2));
            foreach (var source in power.DecaySources)
                source.Since = power.dateTimeLastProcess - TimeSpan.FromSeconds(rtg.HalfLife_s);
            power.LocalFuel = 123.5;

            var view = _projector.ProjectEntity(ship, _faction.Id).GetView<ShipPowerView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view!.HasPlant, Is.True);
            Assert.That(view.Sources.Select(s => s.Kind), Is.EqualTo(new[] { "Solar", "RTG", "Reactor", "Fuel cell" }).AsCollection,
                "dispatch order");

            var solar = view.Sources.Single(s => s.Kind == "Solar");
            Assert.That(solar.Name, Is.EqualTo(parts.Solar.Name));
            Assert.That(solar.Count, Is.EqualTo(1));
            Assert.That(solar.Output_kW, Is.EqualTo(power.MaxOutputFromSolar).Within(1e-9));
            Assert.That(solar.FuelRemaining_kg, Is.Null);

            var isotopes = view.Sources.Single(s => s.Kind == "RTG");
            Assert.That(isotopes.Count, Is.EqualTo(2));
            Assert.That(isotopes.Nameplate_kW, Is.EqualTo(2 * rtg.PowerOutputMax).Within(1e-9), "two units, as new");
            Assert.That(isotopes.Output_kW, Is.EqualTo(rtg.PowerOutputMax).Within(1e-9), "half after one half-life");
            Assert.That(isotopes.DecayRemaining, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(isotopes.HalfLife_years, Is.EqualTo(rtg.HalfLife_s / (365.25 * 86400)).Within(1e-9));
            Assert.That(isotopes.HalfLife_years, Is.EqualTo(87.7).Within(0.1));
            Assert.That(isotopes.FuelRemaining_kg, Is.Null, "an RTG burns nothing");

            var reactor = view.Sources.Single(s => s.Kind == "Reactor");
            var gen = parts.Reactor.GetAttribute<EnergyGenerationAtb>();
            Assert.That(reactor.Name, Is.EqualTo(parts.Reactor.Name));
            Assert.That(reactor.Output_kW, Is.EqualTo(gen.PowerOutputMax).Within(1e-9));
            Assert.That(reactor.FuelRemaining_kg, Is.EqualTo(123.5));
            Assert.That(reactor.FuelBurnAtFull_kgPerS, Is.EqualTo(gen.FuelUsedAtMax).Within(1e-24));
            Assert.That(reactor.RunwayAtFull_s, Is.EqualTo(123.5 / gen.FuelUsedAtMax).Within(1));
            Assert.That(reactor.FuelId, Is.EqualTo("fissile-fuels"));
            Assert.That(reactor.FuelName, Is.EqualTo(Data.CargoGoods.GetAny("fissile-fuels")!.Name));
            Assert.That(reactor.Note, Is.Null);

            var cells = view.Sources.Single(s => s.Kind == "Fuel cell");
            var cell = parts.Cell.GetAttribute<EnergyGenerationAtb>();
            var propellant = Data.CargoGoods.GetAny(cell.FuelType)!;
            Assert.That(cells.Output_kW, Is.EqualTo(cell.PowerOutputMax).Within(1e-9));
            Assert.That(cells.FuelId, Is.EqualTo(cell.FuelType));
            Assert.That(cells.FuelName, Is.EqualTo(propellant.Name));
            Assert.That(cells.FuelRemaining_kg, Is.EqualTo(10 * propellant.MassPerUnit).Within(1e-9), "the propellant in the hold");
            Assert.That(cells.FuelBurnAtFull_kgPerS, Is.EqualTo(cell.FuelUsedAtMax).Within(1e-12));

            double expected = power.MaxOutputFromSolar + rtg.PowerOutputMax + gen.PowerOutputMax + cell.PowerOutputMax;
            Assert.That(view.Generation_kW, Is.EqualTo(expected).Within(1e-9), "generation sums the sources now");
            Assert.That(view.Sources.Sum(s => s.Output_kW), Is.EqualTo(view.Generation_kW).Within(1e-9));

            // Spent reactor and an empty hold: both give nothing and say why.
            power.LocalFuel = 0;
            ship.GetDataBlob<CargoStorageDB>().RemoveCargoByUnit(propellant, 10);
            var dry = _projector.ProjectEntity(ship, _faction.Id).GetView<ShipPowerView>()!;
            var dryReactor = dry.Sources.Single(s => s.Kind == "Reactor");
            Assert.That(dryReactor.Output_kW, Is.EqualTo(0));
            Assert.That(dryReactor.Nameplate_kW, Is.EqualTo(gen.PowerOutputMax).Within(1e-9));
            Assert.That(dryReactor.Note, Is.EqualTo("Out of fuel"));
            var dryCells = dry.Sources.Single(s => s.Kind == "Fuel cell");
            Assert.That(dryCells.Output_kW, Is.EqualTo(0));
            Assert.That(dryCells.FuelRemaining_kg, Is.EqualTo(0));
            Assert.That(dryCells.Note, Is.EqualTo("No propellant in the hold"));
        }

        [Test]
        public void Owner_SeesLoadGroups_WithTheirSupply()
        {
            var (ship, parts) = BuildShip();
            var power = ship.GetDataBlob<EnergyGenAbilityDB>();
            FillBatteries(power);
            double sensorKw = parts.Sensor.GetAttribute<PowerConsumerAtb>().Draw_kW;
            Assert.That(sensorKw, Is.GreaterThan(0));
            Assert.That(power.AuxDemand, Is.EqualTo(2 * sensorKw).Within(1e-12), "two sensors");

            // At rest with full batteries: warp idle, sensors powered from spare output, miner idle.
            var view = _projector.ProjectEntity(ship, _faction.Id).GetView<ShipPowerView>()!;
            Assert.That(view.LoadGroups.Select(g => g.Kind), Is.EqualTo(new[] { "Engines and warp", "Sensors", "Miners" }).AsCollection,
                "no other consumers on this ship");

            var warp = view.LoadGroups.Single(g => g.Kind == "Engines and warp");
            Assert.That(warp.State, Is.EqualTo("Idle"));
            Assert.That(warp.Demand_kW, Is.EqualTo(0));
            Assert.That(warp.Satisfaction, Is.EqualTo(1));
            Assert.That(warp.Loads.Single().DesignId, Is.EqualTo(parts.Warp.UniqueID));
            Assert.That(warp.Loads.Single().Draw_kW, Is.EqualTo(parts.Warp.GetAttribute<WarpDriveAtb>().BubbleSustainCost).Within(1e-9));

            var sensors = view.LoadGroups.Single(g => g.Kind == "Sensors");
            Assert.That(sensors.State, Is.EqualTo("Powered"));
            Assert.That(sensors.Satisfaction, Is.EqualTo(1).Within(1e-12));
            Assert.That(sensors.Demand_kW, Is.EqualTo(2 * sensorKw).Within(1e-12));
            Assert.That(sensors.Loads.Single().Count, Is.EqualTo(2));
            Assert.That(view.AuxDemand_kW, Is.EqualTo(2 * sensorKw).Within(1e-12));
            Assert.That(view.AuxServed_kW, Is.EqualTo(2 * sensorKw).Within(1e-12));
            Assert.That(view.SupplyShare, Is.EqualTo(1));

            var miners = view.LoadGroups.Single(g => g.Kind == "Miners");
            Assert.That(miners.State, Is.EqualTo("Idle"), "no mining order running");
            Assert.That(miners.Satisfaction, Is.Null, "the engine keeps no kW load for the miner");
            Assert.That(miners.Loads.Single().DesignId, Is.EqualTo(parts.Miner.UniqueID));

            // A warp asking twice what the plant gives: warp gets half, and nothing is spare for sensors.
            double generation = power.TotalOutputMax;
            power.AddDemand(2 * generation, power.dateTimeLastProcess);
            var warping = _projector.ProjectEntity(ship, _faction.Id).GetView<ShipPowerView>()!;
            var warpNow = warping.LoadGroups.Single(g => g.Kind == "Engines and warp");
            Assert.That(warpNow.State, Is.EqualTo("Partial"));
            Assert.That(warpNow.Satisfaction, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(warpNow.Demand_kW, Is.EqualTo(2 * generation).Within(1e-9));
            Assert.That(warpNow.Served_kW, Is.EqualTo(generation).Within(1e-9));
            var sensorsNow = warping.LoadGroups.Single(g => g.Kind == "Sensors");
            Assert.That(sensorsNow.State, Is.EqualTo("No power"));
            Assert.That(sensorsNow.Satisfaction, Is.EqualTo(0));
            Assert.That(sensorsNow.Note, Is.Not.Null);
            Assert.That(warping.Share, Is.EqualTo(0.5).Within(1e-9), "the entity card's warp-only ring is unchanged");
            Assert.That(warping.SupplyShare, Is.EqualTo(generation / (2 * generation + 2 * sensorKw)).Within(1e-9));
        }

        [Test]
        public void EmptyBatteries_WithSpareOutput_AreCharging_NotNoPower()
        {
            var (ship, _) = BuildShip();
            var power = ship.GetDataBlob<EnergyGenAbilityDB>();
            string id = power.EnergyType.UniqueID;
            power.EnergyStored[id] = 0;
            double generation = power.TotalOutputMax;
            double max = power.EnergyStoreMax[id];
            Assert.That(generation, Is.GreaterThan(0));
            Assert.That(power.AuxServed, Is.EqualTo(0), "engine: other loads wait for full batteries");

            var view = _projector.ProjectEntity(ship, _faction.Id).GetView<ShipPowerView>()!;
            Assert.That(view.Charging_kW, Is.EqualTo(generation).Within(1e-9), "no warp demand: all output charges");
            Assert.That(view.LoadsWaitOnCharge, Is.True);
            Assert.That(view.FullIn_s, Is.EqualTo(max / generation).Within(1e-6));
            Assert.That(view.Delivered_kW, Is.EqualTo(0), "charging is not delivered to a load");
            Assert.That(view.SupplyShare, Is.EqualTo(0));
            var sensors = view.LoadGroups.Single(g => g.Kind == "Sensors");
            Assert.That(sensors.State, Is.EqualTo("Charging"), "waiting on a charge, not a shortage");
            Assert.That(sensors.Satisfaction, Is.EqualTo(0));
            Assert.That(sensors.Note, Does.Contain("Waiting for the batteries"));

            // Warp taking all output with empty batteries: nothing charges, so it is a real shortage.
            power.AddDemand(2 * generation, power.dateTimeLastProcess);
            var warping = _projector.ProjectEntity(ship, _faction.Id).GetView<ShipPowerView>()!;
            Assert.That(warping.Charging_kW, Is.EqualTo(0));
            Assert.That(warping.LoadsWaitOnCharge, Is.False);
            Assert.That(warping.FullIn_s, Is.Null);
            Assert.That(warping.LoadGroups.Single(g => g.Kind == "Sensors").State, Is.EqualTo("No power"));

            // Full batteries: nothing to charge.
            power.AddDemand(-2 * generation, power.dateTimeLastProcess);
            FillBatteries(power);
            var full = _projector.ProjectEntity(ship, _faction.Id).GetView<ShipPowerView>()!;
            Assert.That(full.Charging_kW, Is.EqualTo(0));
            Assert.That(full.LoadGroups.Single(g => g.Kind == "Sensors").State, Is.EqualTo("Powered"));
        }

        [Test]
        public void Miners_ShowTheRunningOrdersReport_PoweredOrNoPower()
        {
            Data.Unlock("iron");
            var iron = Data.CargoGoods.GetMineral("iron");
            _planet.SetDataBlob(new MineralsDB());
            _planet.GetDataBlob<MineralsDB>().Minerals[iron.ID] = new MineralDeposit
            {
                Amount = new Masked<long>(1_000_000, AccessLevel.Full),
                HalfOriginalAmount = 500_000,
                Accessibility = 1,
            };

            MinerGroupAfterADay(powered: true, out var powered);
            Assert.That(powered.State, Is.EqualTo("Powered"));
            Assert.That(powered.Satisfaction, Is.EqualTo(1));

            MinerGroupAfterADay(powered: false, out var unpowered);
            Assert.That(unpowered.State, Is.EqualTo("No power"));
            Assert.That(unpowered.Satisfaction, Is.EqualTo(0));
        }

        void MinerGroupAfterADay(bool powered, out ShipPowerLoadGroup group)
        {
            var (ship, _) = BuildShip();
            ship.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"] = new TypeStore(1e9);
            var power = ship.GetDataBlob<EnergyGenAbilityDB>();
            if (powered)
                FillBatteries(power);
            else
            {
                // Nothing left anywhere: no sun, spent RTGs, reactor and hold dry, flat batteries.
                power.MaxOutputFromSolar = 0;
                power.DecaySources.Clear();
                power.LocalFuel = 0;
                var cell = power.CellFuelUseAtMax.type;
                ship.GetDataBlob<CargoStorageDB>().RemoveCargoByUnit(Data.CargoGoods.GetAny(cell)!, 10);
                power.EnergyStored[power.EnergyType.UniqueID] = 0;
                Assert.That(power.TotalOutputMax, Is.EqualTo(0));
            }
            var order = new Pulsar4X.Industry.AsteroidMineOrder(ship, _planet);
            ship.GetDataBlob<GameEngine.Engine.Orders.ActionQueueDB>().Enqueue(order);
            DateTime at = power.dateTimeLastProcess;
            order.Execute(at);
            order.Execute(at + TimeSpan.FromDays(1));
            Assert.That(order.Details, Is.EqualTo(powered ? "Mining" : "No power for the miner"));
            group = _projector.ProjectEntity(ship, _faction.Id).GetView<ShipPowerView>()!.LoadGroups.Single(g => g.Kind == "Miners");
        }

        [Test]
        public void NonOwner_GetsNoPowerBreakdown_AndNoCrewRows()
        {
            var (ship, _) = BuildShip();
            var other = _game.GameMasterFaction.Id;
            var projected = _projector.ProjectEntity(ship, other);
            Assert.That(projected.GetView<ShipPowerView>(), Is.Null, "ship power stays with the owner");
            Assert.That(projected.GetView<EnergyView>(), Is.Null);
            var shipView = projected.GetView<ShipView>();
            Assert.That(shipView, Is.Not.Null, "non-owners still see the class");
            Assert.That(shipView!.CrewRequired, Is.EqualTo(0));
            Assert.That(shipView.CrewByComponent, Is.Empty);
        }

        [Test]
        public void Owner_SeesCrewPerCopyTimesCopies_AddingUpToTheTotal()
        {
            var (ship, parts) = BuildShip();
            var design = ship.GetDataBlob<ShipInfoDB>().Design;
            var view = _projector.ProjectEntity(ship, _faction.Id).GetView<ShipView>()!;

            Assert.That(view.CrewRequired, Is.EqualTo(design.CrewReq));
            Assert.That(view.CrewRequired, Is.GreaterThan(0));
            Assert.That(view.CrewByComponent.Sum(r => r.Crew), Is.EqualTo(view.CrewRequired), "the rows add up to the total");
            foreach (var row in view.CrewByComponent)
            {
                var component = design.Components.Single(c => c.design.UniqueID == row.DesignId);
                Assert.That(row.Count, Is.EqualTo(component.count), row.Name);
                Assert.That(row.CrewPerCopy, Is.EqualTo(component.design.CrewReq), row.Name);
                Assert.That(row.Crew, Is.EqualTo(component.design.CrewReq * component.count), row.Name);
            }
            int expected = design.Components.Sum(c => c.design.CrewReq * c.count);
            Assert.That(view.CrewRequired, Is.EqualTo(expected), "per copy x copies, not one per design");
            var multi = design.Components.Where(c => c.count > 1 && c.design.CrewReq > 0).ToList();
            TestContext.Out.WriteLine(string.Join("; ", view.CrewByComponent.Select(r => $"{r.Name} {r.CrewPerCopy}x{r.Count}={r.Crew}")));
            if (multi.Count > 0)
                Assert.That(view.CrewRequired, Is.GreaterThan(design.Components.Sum(c => c.design.CrewReq)), "copies count");
        }
    }
}
