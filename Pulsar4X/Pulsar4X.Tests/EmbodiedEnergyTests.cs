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

    static double X(string mineral) => EnergyPriceOracle.Ext(mineral);
    static double Plastic => (12 + 1 * X("hydrocarbons")) / 2;

    /// <summary>Hand-worked kWh per unit of every good a stock design uses (materials.json recipes, minerals.json extraction).</summary>
    static readonly Dictionary<string, double> HandE = new()
    {
        ["stainless-steel"] = EnergyPriceOracle.Steel_kWh,     // (70 + 88 Fe + 11 Cr + 1 HC) / 100
        ["methalox"] = EnergyPriceOracle.Methalox_kWh,         // (4250 + 4250 HC) / 8500
        ["rp-1"] = (2975 + 4250 * X("hydrocarbons")) / 8500,
        ["hydrolox"] = (19550 + 4250 * X("hydrocarbons")) / 8500,
        ["ntp"] = (1200 + 100 * X("hydrocarbons") + 1 * X("fissionables")) / 100,
        ["plastic"] = Plastic,
        ["fissile-fuels"] = (400 + 100 * X("fissionables") + 10 * X("hydrocarbons")) / 1,
        ["electronics"] = (1000 + 2 * X("copper") + 2 * Plastic + 1 * X("aluminium") + 1 * X("silicon")) / 5,
        ["space-crete"] = (50 + 70 * X("regolith") + 10 * X("silicon") + 10 * X("aluminium") + 5 * X("iron") + 5 * X("water")) / 100,
    };

    static double Hand(string id) => HandE.TryGetValue(id, out var e) ? e : X(id);

    // ---- E by hand ----

    [Test]
    public void SteelAndMethalox_MatchTheHandCalculation()
    {
        // (70 kWh + 88 x 0.388 iron + 11 x 0.8 chromium + 1 x 0.3 hydrocarbons) / 100
        Assert.That(E.PerUnit("stainless-steel"), Is.EqualTo(1.13244).Within(1e-12));
        // (4,250 kWh + 4,250 kg hydrocarbons x 0.3) / 8,500
        Assert.That(E.PerUnit("methalox"), Is.EqualTo(0.65).Within(1e-12));
        Assert.That(E.PerUnit("iron"), Is.EqualTo(0.388).Within(1e-12), "extraction only");
        Assert.That(E.PerUnit("water"), Is.EqualTo(0.188).Within(1e-12));
        foreach (var (id, e) in HandE)
            Assert.That(E.PerUnit(id), Is.EqualTo(e).Within(1e-9), id);
        Assert.That(E.PerKg("methalox"), Is.EqualTo(0.65).Within(1e-12), "1 kg a unit");
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
        Assert.That(EmbodiedEnergy.For(locked).PerUnit("methalox"), Is.EqualTo(EnergyPriceOracle.Methalox_kWh).Within(1e-12));
        // A mineral's extraction energy comes from the data and can be changed.
        var water = (Mineral)locked.LockedCargoGoods.GetAny("water")!;
        Assert.That(water.ExtractionEnergy, Is.EqualTo(EnergyPriceOracle.Water_kWh));
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
        Assert.That(EnergyPricing.SeedAsk(_game, Info.Data, "stainless-steel"), Is.EqualTo(5.66m), "1.13244 kWh x 1 x 5");
        Assert.That(EnergyPricing.SeedAsk(_game, Info.Data, "methalox"), Is.EqualTo(3.25m), "0.65 x 5, no delivery off a depot");

        _game.Settings.CreditsPerKWh = 2;
        _game.Settings.PriceMarkup = 0;
        Assert.That(EnergyPricing.SeedAsk(_game, Info.Data, "stainless-steel"), Is.EqualTo(2.26m), "1.13244 x 2");
        Assert.That(EnergyPricing.SeedBid(_game, Info.Data, "stainless-steel"), Is.EqualTo(1.13m));
        _game.Settings.CreditsPerKWh = double.NaN;
        Assert.That(EnergyPricing.CreditsPerKWh(_game), Is.EqualTo(1.0), "a bad setting falls back");
    }

    /// <summary>Ceres seeds: the original hand-set price, then phase 5 (3afb7b5c, 0.05 kWh/kg minerals, no delivery).</summary>
    static readonly (string good, string side, decimal original, decimal phase5)[] CeresBefore =
    {
        ("methalox", "ask", 3m, 2.63m),
        ("iron", "ask", 2m, 0.25m),
        ("iron", "bid", 1m, 0.13m),
        ("water", "ask", 1m, 0.25m),
    };

    [Test]
    public void CeresSeeds_ComeFromE_PlusDelivery_AllInTheBand()
    {
        var (game, depot, data) = PlacedDepot();
        var policy = depot.GetDataBlob<ColonyMarketPolicyDB>();
        double delivery = EnergyPriceOracle.Delivery_kWhPerKg(game);
        Assert.That(delivery, Is.GreaterThan(0));
        foreach (var (good, side, original, phase5) in CeresBefore)
        {
            double kWh = (good == "methalox" ? EnergyPriceOracle.Methalox_kWh : EnergyPriceOracle.Ext(good)) + delivery;
            decimal expected = side == "ask" ? EnergyPriceOracle.Ask(game, kWh) : EnergyPriceOracle.Bid(game, kWh);
            decimal now = side == "ask" ? policy.Rows[good].Ask : policy.Rows[good].Bid;
            Assert.That(now, Is.EqualTo(expected), $"{good} {side}");
            double change = (double)(now / original) - 1;
            TestContext.Out.WriteLine($"Ceres {good} {side}: E {kWh - delivery:0.####} + delivery {delivery:0.######} kWh; original {original}, phase 5 {phase5}, now {now} ({change:+0.0%;-0.0%})");
            Assert.That(Math.Abs(change), Is.LessThanOrEqualTo(0.5), $"{good} {side} within +/-50% of the original");
        }
        Assert.That(policy.Rows["methalox"].Bid, Is.EqualTo(0));
        Assert.That(policy.Rows["water"].Bid, Is.EqualTo(0));
        decimal lot = CeresStart.LotPrice(game, data);
        Assert.That(lot, Is.EqualTo(EnergyPriceOracle.LotPrice(game)));
        Assert.That(Math.Abs((double)(lot / 0.5m) - 1), Is.LessThanOrEqualTo(0.5));
        TestContext.Out.WriteLine($"Ceres lot price per mineral: original 0.5, phase 5 0.065, now {lot}");
    }

    [Test]
    public void ExtractionEnergy_ComesFromTheData_PerMineral()
    {
        var data = new FactionDataStore(_store);
        var e = EmbodiedEnergy.For(data);
        var fromData = EnergyPriceOracle.ExtractionFromData;
        Assert.That(fromData.Keys, Is.EquivalentTo(_store.Minerals.Keys), "every mineral has a figure");
        foreach (var (id, kWh) in fromData)
        {
            Assert.That(e.PerUnit(id), Is.EqualTo(kWh).Within(1e-12), id);
            TestContext.Out.WriteLine($"{id}: {kWh} kWh/kg");
        }
        // Loose and shallow is cheap, lean and hard is dear.
        Assert.That(fromData["regolith"], Is.LessThan(fromData["silicon"]));
        Assert.That(fromData["water"], Is.LessThan(fromData["iron"]));
        Assert.That(fromData["water"], Is.GreaterThan(fromData["regolith"]), "ice is cheap, not free: it has to be melted");
        Assert.That(fromData["iron"], Is.LessThan(fromData["copper"]));
        Assert.That(fromData["copper"], Is.LessThan(fromData["fissionables"]));
        Assert.That(fromData["fissionables"], Is.LessThan(fromData["tungsten"]));
        Assert.That(e.Unknown, Is.Empty);
    }

    [Test]
    public void DepotDelivery_IsTheReferenceHaulByHand_AndTunable()
    {
        var data = Info.Data;
        double hand = EnergyPriceOracle.Delivery_kWhPerKg(_game);
        // 2 x (4e6/3 kJ + 15 kW x 0.2 AU / 20,233.5 m/s) / 3600 / 1e6 kg = 0.013064, plus (energy
        // phase 6) the turbine's fissile fuel for those kJ at 1,403 kWh/kg: 0.0000055. Crew plan
        // phase 1: the miner's crew quarters (+2,028 kg dry) slowed its drive from 22,029 m/s
        // (0.012064 before); phase 4: its 4 t reactor (+2,000 kg) from 21,087 m/s (0.012570 before).
        // Life support (17 kW) runs anyway and is not charged to the haul.
        Assert.That(hand, Is.EqualTo(0.013069).Within(1e-6));
        Assert.That(EnergyPricing.DepotDeliveryKWhPerKg(_game, data), Is.EqualTo(hand).Within(1e-12));
        var legs = EnergyPricing.DepotDeliveryLegs(_game);
        Assert.That(legs, Has.Count.EqualTo(2));
        Assert.That(legs.All(l => l.Feasible), Is.True);
        Assert.That(legs.All(l => l.Mode == MoveCostMode.Warp), Is.True, "warped, no reaction fuel");
        // Energy phase 6: the only fuel is the reactor's, priced per kJ of the leg.
        foreach (var l in legs)
        {
            Assert.That(l.FuelUsed_kg.Keys, Is.EquivalentTo(new[] { "fissile-fuels" }));
            Assert.That(l.FuelUsed_kg["fissile-fuels"], Is.EqualTo(l.EnergyUsed_kJ * 50 / 40e9 / 15).Within(1e-12));
        }
        TestContext.Out.WriteLine($"Delivery: {legs[0].EnergyUsed_kJ / 3600:N1} kWh back full + {legs[1].EnergyUsed_kJ / 3600:N1} kWh out empty"
            + $" over {EnergyPricing.ReferenceHold_kg:N0} kg = {hand:0.######} kWh/kg = {EnergyPricing.Credits(_game, hand):0.####} credits/kg"
            + $", {legs[0].Duration_s / 86400:0.0} days a leg");

        // Delivered price = E + delivery, priced once.
        Assert.That(EnergyPricing.DepotSeedAsk(_game, data, "iron"), Is.EqualTo(EnergyPriceOracle.Ask(_game, 0.388 + hand)));

        _game.Settings.DepotDeliveryRoundTrip = false;
        Assert.That(EnergyPricing.DepotDeliveryKWhPerKg(_game, data), Is.EqualTo(EnergyPriceOracle.Delivery_kWhPerKg(_game)).Within(1e-12));
        Assert.That(EnergyPricing.DepotDeliveryKWhPerKg(_game, data), Is.EqualTo(hand / 2).Within(1e-12), "one way, same leg");
        _game.Settings.DepotDeliveryRoundTrip = true;
        _game.Settings.DepotDeliveryDistance_AU = 0.4;
        Assert.That(EnergyPricing.DepotDeliveryKWhPerKg(_game, data), Is.EqualTo(EnergyPriceOracle.Delivery_kWhPerKg(_game)).Within(1e-12));
        _game.Settings.DepotDeliveryDistance_AU = 0;
        Assert.That(EnergyPricing.DepotDeliveryKWhPerKg(_game, data), Is.EqualTo(0), "made at the depot: no delivery");
        Assert.That(EnergyPricing.DepotSeedAsk(_game, data, "iron"), Is.EqualTo(EnergyPricing.SeedAsk(_game, data, "iron")));
    }

    [Test]
    public void SeededHistory_FollowsTheNewSeeds()
    {
        var (game, depot, _) = PlacedDepot();
        MarketHistory.SeedPriorDays(depot);
        var book = depot.GetDataBlob<LogiBaseDB>();
        double delivery = EnergyPriceOracle.Delivery_kWhPerKg(game);
        foreach (var (good, kWh) in new[] { ("methalox", EnergyPriceOracle.Methalox_kWh + delivery), ("iron", EnergyPriceOracle.Iron_kWh + delivery) })
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
            }
        }
    }

    // ---- freight ----

    [Test]
    public void Freight_IsFuelTimesItsE_PlusWarpEnergy_PerKgOfCargo()
    {
        var cost = new MoveCost(MoveCostMode.Burn, true, "", "",
            new Dictionary<string, double> { ["methalox"] = 1000 }, 3.6e6, 0, 0, 0, 0, 0);
        // (1,000 kg x E(methalox) kWh/kg + 3.6e6 kJ / 3600) / 10,000 kg
        double perKg = (1000 * EnergyPriceOracle.Methalox_kWh + 1000) / 10000.0;
        Assert.That(perKg, Is.EqualTo(0.165).Within(1e-12));
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
        var burn = MoveCostCalculator.Burn(full, 3000);
        Assert.That(burn.FuelUsed_kg.Keys, Does.Contain("methalox").Or.Contain("rp-1").Or.Contain("hydrolox"));

        // The depot's reference hauler is this ship as it starts, with a full 1,000 t hold.
        var live = MoveCostShip.FromEntity(prospector);
        var reference = EnergyPricing.ReferenceHauler;
        Assert.That(prospector.GetDataBlob<ShipInfoDB>().Design.UniqueID, Is.EqualTo("default-ship-design-miner"));
        Assert.That(hold_kg, Is.EqualTo(EnergyPricing.ReferenceHold_kg));
        Assert.That(reference.DryMass_kg, Is.EqualTo(live.DryMass_kg).Within(1e-6));
        Assert.That(reference.FuelMass_kg, Is.EqualTo(live.FuelMass_kg).Within(1));
        Assert.That(reference.FuelType, Is.EqualTo(live.FuelType));
        Assert.That(reference.ExhaustVelocity, Is.EqualTo(live.ExhaustVelocity).Within(1e-6));
        Assert.That(reference.HasWarp, Is.EqualTo(live.HasWarp));
        Assert.That(reference.WarpPower, Is.EqualTo(live.WarpPower).Within(1e-6));
        Assert.That(reference.BubbleCreationCost, Is.EqualTo(live.BubbleCreationCost).Within(1e-6));
        Assert.That(reference.BubbleSustainCost, Is.EqualTo(live.BubbleSustainCost).Within(1e-9));
        Assert.That(reference.BubbleCollapseCost, Is.EqualTo(live.BubbleCollapseCost).Within(1e-9));
        Assert.That(reference.Coupling, Is.EqualTo(live.Coupling).Within(1e-12));
        Assert.That(reference.BleedFraction, Is.EqualTo(live.BleedFraction).Within(1e-12));
        Assert.That(reference.EnergyOutputMax, Is.EqualTo(live.EnergyOutputMax).Within(1e-9));
        Assert.That(reference.LifeSupport_kW, Is.EqualTo(live.LifeSupport_kW).Within(1e-9), "crew plan phase 4: life support");
        Assert.That(reference.EnergyStoreMax, Is.EqualTo(live.EnergyStoreMax).Within(1e-6));
        Assert.That(reference.ReactorOutputMax, Is.EqualTo(live.ReactorOutputMax).Within(1e-9));
        Assert.That(reference.ReactorFuelType, Is.EqualTo(live.ReactorFuelType));
        Assert.That(reference.ReactorFuelPerKJ, Is.EqualTo(live.ReactorFuelPerKJ).Within(1e-18));
        Assert.That(reference.FreeOutputMax, Is.EqualTo(live.FreeOutputMax).Within(1e-9));
        Assert.That(reference.CellOutputMax, Is.EqualTo(live.CellOutputMax).Within(1e-9));
        var leg = MoveCostCalculator.Warp(full, 0.2 * UniversalConstants.Units.MetersPerAu, sun(2.77), 0);
        Assert.That(EnergyPricing.DepotDeliveryLegs(game)[0].EnergyUsed_kJ, Is.EqualTo(leg.EnergyUsed_kJ).Within(1e-6 * leg.EnergyUsed_kJ));
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
