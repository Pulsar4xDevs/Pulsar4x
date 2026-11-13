using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Energy economy phase 5: embodied energy (E = direct + mining + Σ inputs), prices anchored at
/// 1 credit per kWh with a markup, the Ceres depot seeded from E, and freight from the move-cost hook.
/// Expected values are worked out by hand from the data files (<see cref="EnergyPriceOracle"/>).
/// </summary>
[TestFixture]
public class EmbodiedEnergyTests
{
    ModDataStore _store = null!;
    Game _game = null!;
    Entity _player = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", _store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", _store);

        _game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, _store);
        _game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(_game, _store.Systems["system-sol"]);

        _player = FactionFactory.CreateBasicFaction(_game, "United Earth Corp", "UEC", 0);
        _player.FactionOwnerID = _player.Id;
        var sol = _game.Systems.Single(s => s.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(sol, _store.Species["species-human"]);
        species.FactionOwnerID = _player.Id;
        Info.Species.Add(species);
        ColonyFactory.CreateFromBlueprint(_game, _player, species, sol, NameLookup.GetFirstEntityWithName(sol, "Earth"), _store.Colonies["colony-earth"]);
    }

    FactionInfoDB Info => _player.GetDataBlob<FactionInfoDB>();
    EmbodiedEnergy E => EmbodiedEnergy.For(Info.Data);

    /// <summary>Hand-worked kWh per unit of every good a stock design uses (materials.json, 0.05 kWh/kg mining).</summary>
    static readonly Dictionary<string, double> HandE = new()
    {
        ["stainless-steel"] = EnergyPriceOracle.Steel_kWh,                          // (70 + 100 x 0.05) / 100
        ["methalox"] = EnergyPriceOracle.Methalox_kWh,                              // (4250 + 4250 x 0.05) / 8500
        ["rp-1"] = (2975 + 4250 * 0.05) / 8500,
        ["hydrolox"] = (19550 + 4250 * 0.05) / 8500,
        ["ntp"] = (1200 + 101 * 0.05) / 100,
        ["plastic"] = (12 + 1 * 0.05) / 2,
        ["fissile-fuels"] = (400 + 110 * 0.05) / 1,
        ["electronics"] = (1000 + 2 * 0.05 + 2 * ((12 + 0.05) / 2) + 1 * 0.05 + 1 * 0.05) / 5,
        ["space-crete"] = (50 + 100 * 0.05) / 100,
    };

    static double Hand(string id) => HandE.TryGetValue(id, out var e) ? e : EnergyPriceOracle.Mineral_kWh;

    // ---- E by hand ----

    [Test]
    public void SteelAndMethalox_MatchTheHandCalculation()
    {
        Assert.That(E.PerUnit("stainless-steel"), Is.EqualTo(0.75).Within(1e-12), "(70 kWh + 100 kg ore x 0.05) / 100");
        Assert.That(E.PerUnit("methalox"), Is.EqualTo(0.525).Within(1e-12), "(4,250 kWh + 4,250 kg hydrocarbons x 0.05) / 8,500");
        Assert.That(E.PerUnit("iron"), Is.EqualTo(0.05).Within(1e-12), "extraction only");
        Assert.That(E.PerUnit("water"), Is.EqualTo(0.05).Within(1e-12));
        foreach (var (id, e) in HandE)
            Assert.That(E.PerUnit(id), Is.EqualTo(e).Within(1e-9), id);
        Assert.That(E.PerKg("methalox"), Is.EqualTo(0.525).Within(1e-12), "1 kg a unit");
        Assert.That(E.PerUnit(EmbodiedEnergy.ElectricityId), Is.EqualTo(1 / 3600.0).Within(1e-15), "a unit of electricity is 1 kJ");
        Assert.That(E.PerUnit("no-such-good"), Is.EqualTo(0));
        Assert.That(E.Unknown, Does.Contain("no-such-good"));
        Assert.That(E.Cycles, Is.Empty);
    }

    [TestCase("default-ship-design-freighter")]
    [TestCase("default-ship-design-tanker")]
    public void StockShip_IsAssemblyPlusItsComponents_ByHand(string shipId)
    {
        var ship = Info.ShipDesigns[shipId];
        double assembly = (long)(ship.MassPerUnit * 0.1) * 5.0; // 5 kWh per assembly IP, 1 IP per 10 kg
        double components = 0;
        foreach (var (design, count) in ship.Components)
        {
            double one = design.IndustryPointCosts * 10.0; // no stock component overrides its EnergyCost formula
            Assert.That(design.EnergyCost, Is.EqualTo(one).Within(1e-9), design.UniqueID);
            foreach (var (id, amount) in design.ResourceCosts)
            {
                Assert.That(_store.Minerals.ContainsKey(id) || HandE.ContainsKey(id), Is.True, $"{design.UniqueID} uses {id}");
                one += amount * Hand(id);
            }
            components += one * count;
        }
        double hand = assembly + components;
        double e = E.OfDesign(ship, Info);
        TestContext.Out.WriteLine($"{shipId}: assembly {assembly:N0} kWh + components {components:N0} kWh = {hand:N0} kWh; calculator {e:N0} kWh");
        Assert.That(e, Is.EqualTo(hand).Within(1e-6 * hand));
        Assert.That(E.PerUnit(shipId, Info), Is.EqualTo(hand).Within(1e-6 * hand), "by id too");
        Assert.That(e, Is.GreaterThan(ship.TotalBuildEnergy), "materials carry energy on top of the build energy");
    }

    [Test]
    public void Cycles_AreGuarded_CountedAsZero_AndNotCached()
    {
        var data = new FactionDataStore();
        ProcessedMaterial Make(string id, params (string id, long amount)[] inputs) => new ProcessedMaterial
        {
            UniqueID = id,
            Name = id,
            CargoTypeID = "general-storage",
            IndustryTypeID = "refining",
            IndustryPointCosts = 1,
            EnergyCost = 10,
            OutputAmount = 1,
            MassPerUnit = 1,
            ResourceCosts = inputs.ToDictionary(i => i.id, i => i.amount),
        };
        data.CargoGoods.Add(Make("a", ("b", 1)));
        data.CargoGoods.Add(Make("b", ("a", 1)));
        data.CargoGoods.Add(Make("self", ("self", 2)));
        data.CargoGoods.Add(Make("c", ("a", 1)));

        var e = EmbodiedEnergy.For(data);
        Assert.That(e.PerUnit("a"), Is.EqualTo(10 + (10 + 0)), "a -> b -> a: the repeat counts 0");
        Assert.That(e.PerUnit("self"), Is.EqualTo(10));
        Assert.That(e.Cycles, Is.EquivalentTo(new[] { "a", "self" }));
        Assert.That(e.PerUnit("b"), Is.EqualTo(10 + (10 + 0)), "b was not cached with a's partial value");
        Assert.That(e.PerUnit("c"), Is.EqualTo(10 + 20));

        EmbodiedEnergy.Invalidate(data);
        Assert.That(EmbodiedEnergy.For(data), Is.Not.SameAs(e));
        Assert.That(EmbodiedEnergy.For(data).Cycles, Is.Empty);
    }

    [Test]
    public void Calculator_IsCachedPerFactionData()
    {
        Assert.That(EmbodiedEnergy.For(Info.Data), Is.SameAs(E));
        Assert.That(EmbodiedEnergy.For(new FactionDataStore(_store)), Is.Not.SameAs(E));
        // Locked goods are priced too (a faction can be offered what it cannot make yet).
        var locked = new FactionDataStore(_store);
        Assert.That(locked.CargoGoods.Contains("methalox"), Is.False);
        Assert.That(EmbodiedEnergy.For(locked).PerUnit("methalox"), Is.EqualTo(0.525).Within(1e-12));
        // A mineral may carry its own extraction energy.
        var water = (Mineral)locked.LockedCargoGoods.GetAny("water")!;
        Assert.That(water.ExtractionEnergy, Is.Null);
        water.ExtractionEnergy = 0.1;
        EmbodiedEnergy.Invalidate(locked);
        Assert.That(EmbodiedEnergy.For(locked).PerUnit("water"), Is.EqualTo(0.1).Within(1e-12));
    }

    // ---- prices ----

    [Test]
    public void Anchor_IsOneCreditPerKWh_WithTheMarkup_AndTunable()
    {
        Assert.That(_game.Settings.CreditsPerKWh, Is.EqualTo(1.0));
        Assert.That(_game.Settings.PriceMarkup, Is.EqualTo(4.0));
        Assert.That(_game.Settings.SeedBidShare, Is.EqualTo(0.5));
        Assert.That(EnergyPricing.SeedAsk(_game, Info.Data, "stainless-steel"), Is.EqualTo(3.75m), "0.75 kWh x 1 x 5");
        Assert.That(EnergyPricing.SeedAsk(_game, Info.Data, "methalox"), Is.EqualTo(2.63m), "0.525 x 5 = 2.625");

        _game.Settings.CreditsPerKWh = 2;
        _game.Settings.PriceMarkup = 0;
        Assert.That(EnergyPricing.SeedAsk(_game, Info.Data, "stainless-steel"), Is.EqualTo(1.5m));
        Assert.That(EnergyPricing.SeedBid(_game, Info.Data, "stainless-steel"), Is.EqualTo(0.75m));
        _game.Settings.CreditsPerKWh = double.NaN;
        Assert.That(EnergyPricing.CreditsPerKWh(_game), Is.EqualTo(1.0), "a bad setting falls back");
    }

    /// <summary>Today's (pre phase 5) hand-set Ceres prices, CeresStart.cs before this change.</summary>
    static readonly (string good, string side, decimal before, double kWh)[] CeresBefore =
    {
        ("methalox", "ask", 3m, EnergyPriceOracle.Methalox_kWh),
        ("iron", "ask", 2m, EnergyPriceOracle.Mineral_kWh),
        ("iron", "bid", 1m, EnergyPriceOracle.Mineral_kWh),
        ("water", "ask", 1m, EnergyPriceOracle.Mineral_kWh),
    };

    [Test]
    public void CeresSeeds_ComeFromE_MethaloxInTheBand_MineralsBelowIt()
    {
        var (game, depot, data) = PlacedDepot();
        var policy = depot.GetDataBlob<ColonyMarketPolicyDB>();
        var outside = new List<string>();
        foreach (var (good, side, before, kWh) in CeresBefore)
        {
            decimal expected = side == "ask" ? EnergyPriceOracle.Ask(game, kWh) : EnergyPriceOracle.Bid(game, kWh);
            decimal now = side == "ask" ? policy.Rows[good].Ask : policy.Rows[good].Bid;
            Assert.That(now, Is.EqualTo(expected), $"{good} {side}");
            double change = (double)(now / before) - 1;
            TestContext.Out.WriteLine($"Ceres {good} {side}: E {kWh} kWh, before {before}, after {now} ({change:+0.0%;-0.0%})");
            if (Math.Abs(change) > 0.5)
                outside.Add($"{good} {side}");
        }
        Assert.That(policy.Rows["methalox"].Bid, Is.EqualTo(0));
        Assert.That(policy.Rows["water"].Bid, Is.EqualTo(0));
        // Minerals carry extraction energy only (Jon's decision 1 puts ore-to-metal energy in the
        // refinery), 0.05 kWh/kg against the plan's 3.5 for iron, so they fall well below today's.
        Assert.That(outside, Is.EquivalentTo(new[] { "iron ask", "iron bid", "water ask" }));
        Assert.That(CeresStart.LotPrice(game, data), Is.EqualTo(EnergyPriceOracle.LotPrice(game)));
        TestContext.Out.WriteLine($"Ceres lot price per mineral: before 0.5, after {CeresStart.LotPrice(game, data)}");
    }

    [Test]
    public void SeededHistory_FollowsTheNewSeeds()
    {
        var (game, depot, _) = PlacedDepot();
        MarketHistory.SeedPriorDays(depot);
        var book = depot.GetDataBlob<LogiBaseDB>();
        foreach (var (good, kWh) in new[] { ("methalox", EnergyPriceOracle.Methalox_kWh), ("iron", EnergyPriceOracle.Mineral_kWh) })
        {
            decimal ask = EnergyPriceOracle.Ask(game, kWh);
            decimal bid = good == "iron" ? EnergyPriceOracle.Bid(game, kWh) : 0;
            decimal anchor = bid > 0 ? (bid + ask) / 2 : ask;
            var today = depot.StarSysDateTime.Date;
            var days = book.History[good].Where(d => d.Day.Date < today).ToList(); // today's live bucket may already exist
            Assert.That(days, Has.Count.EqualTo(MarketHistory.SeedDays), good);
            Assert.That(days[^1].Close, Is.EqualTo(Math.Round(anchor, 2, MidpointRounding.AwayFromZero)), good);
            foreach (var day in days)
            {
                Assert.That(day.Close, Is.InRange(Math.Max(0.01m, 0.69m * anchor), 1.31m * anchor + 0.01m), good);
                if (good == "iron")
                    Assert.That(day.Close, Is.LessThan(1m), "iron no longer charts in the old 1-2 credit band");
            }
        }
    }

    // ---- freight ----

    [Test]
    public void Freight_IsFuelTimesItsE_PlusWarpEnergy_PerKgOfCargo()
    {
        var cost = new MoveCost(MoveCostMode.Burn, true, "", "",
            new Dictionary<string, double> { ["methalox"] = 1000 }, 3.6e6, 0, 0, 0, 0, 0);
        // (1,000 kg x 0.525 kWh/kg + 3.6e6 kJ / 3600) / 10,000 kg
        double perKg = (1000 * 0.525 + 1000) / 10000.0;
        Assert.That(EnergyPricing.FreightKWhPerKg(cost, 10000, Info.Data), Is.EqualTo(perKg).Within(1e-12));
        Assert.That(EnergyPricing.FreightPerKg(_game, cost, 10000, Info.Data), Is.EqualTo(perKg * 5).Within(1e-12));
        Assert.That(EnergyPricing.FreightKWhPerKg(cost, 0, Info.Data), Is.EqualTo(0), "no cargo, no rate");
        Assert.That(EnergyPricing.FreightKWhPerKg(new[] { cost, cost }, 10000, Info.Data), Is.EqualTo(2 * perKg).Within(1e-12));
    }

    [Test]
    public void Freight_ForAStockShip_FromTheMoveCostHook()
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), "Pulsar4x.Scenario.WarpTechStart");
        var catalog = GameLifecycle.BuildCatalog(store);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, catalog, 12345));
        var game = created!.Value.game;
        var faction = created.Value.faction;
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        var prospector = game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            .Single(s => s.FactionOwnerID == faction.Id && s.GetDataBlob<NameDB>().GetName(faction.Id) == "Prospector I");
        double hold_kg = StorageSpaceProcessor.CalculatedMaxStorage(prospector.GetDataBlob<ShipInfoDB>().Design)["general-storage"] * 1000;
        var full = MoveCostShip.FromEntity(prospector) with { CargoMass_kg = hold_kg };
        double sun(double au) => GeneralMath.StandardGravitationalParameter(UniversalConstants.Units.SolarMassInKG)
                                 / Math.Pow(au * UniversalConstants.Units.MetersPerAu, 2);

        foreach (var (name, cost) in new[]
        {
            ("warp 0.52 AU (Earth-Mars, open space)", MoveCostCalculator.Warp(full, 0.52 * UniversalConstants.Units.MetersPerAu)),
            ("warp 1.77 AU out of the Sun's well (Earth-Ceres)", MoveCostCalculator.Warp(full, 1.77 * UniversalConstants.Units.MetersPerAu, sun(1), radial: 1)),
            ("burn 3 km/s", MoveCostCalculator.Burn(full, 3000)),
        })
        {
            double fuelKWh = cost.FuelUsed_kg.Sum(f => f.Value * EmbodiedEnergy.For(data).PerKg(f.Key));
            double expected = (fuelKWh + cost.EnergyUsed_kJ / 3600) / hold_kg;
            double perKg = EnergyPricing.FreightKWhPerKg(cost, hold_kg, data);
            Assert.That(perKg, Is.EqualTo(expected).Within(1e-12 + 1e-9 * expected), name);
            Assert.That(perKg, Is.GreaterThan(0), name);
            TestContext.Out.WriteLine($"Prospector I, {hold_kg:N0} kg of cargo, {name}: fuel {string.Join(", ", cost.FuelUsed_kg.Select(f => $"{f.Value:N0} kg {f.Key}"))}"
                + $", warp {cost.EnergyUsed_kJ / 3600:N1} kWh -> {perKg:0.######} kWh/kg = {EnergyPricing.FreightPerKg(game, cost, hold_kg, data):0.######} credits/kg"
                + $" ({EnergyPricing.FreightPerKg(game, cost, hold_kg, data) * 1000:0.###} credits/t)");
        }
        // Burn fuel is methalox at 0.525 kWh/kg.
        var burn = MoveCostCalculator.Burn(full, 3000);
        Assert.That(burn.FuelUsed_kg.Keys, Does.Contain("methalox").Or.Contain("rp-1").Or.Contain("hydrolox"));
    }

    (Game game, Entity depot, FactionDataStore data) PlacedDepot()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);
        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        ColonyFactory.PlaceOwnedColonies(game, store, player, store.Species["species-human"]);
        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var depot = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<NameDB>().DefaultName == "Ceres Depot");
        return (game, depot, depot.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data);
    }
}
