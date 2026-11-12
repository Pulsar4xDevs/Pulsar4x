using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Move-order cost from real Quickstart ships. Prospector I carries a general hold; "full" is that
/// hold's design volume filled with water (1 L = 1 kg), "half" is half of it.
/// Warp: drive speed is fixed at the empty-ship speed, so in open space and downhill the energy
/// does not change with cargo; climbing out of a star's well it does, and below
/// <see cref="WarpMath.MinWarpSpeed_ms"/> the warp is refused.
/// Burns: Δv does not depend on mass, fuel scales with it.
/// </summary>
[TestFixture]
[NonParallelizable]
public class MoveCostCalculatorTests
{
    const string BaseMod = "Pulsar4x";
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";
    const string GeneralHold = "general-storage";
    const double WaterKgPerCubicMetre = 1000; // 1 L = 1 kg
    static readonly double[] HoldFill = { 0, 0.5, 1 };

    Game _game = null!;
    Entity _prospector = null!;
    Entity _sensorSat = null!;
    double _fullHold_kg;

    [OneTimeSetUp]
    public void StartQuickstart()
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == BaseMod).Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), WarpTechStart);
        var catalog = GameLifecycle.BuildCatalog(store);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, catalog, 12345));
        Assert.That(created, Is.Not.Null);
        _game = created!.Value.game;
        var player = created.Value.faction;
        var ships = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            .Where(s => s.FactionOwnerID == player.Id).ToList();
        _prospector = ships.Single(s => s.GetDataBlob<NameDB>().GetName(player.Id) == "Prospector I");
        _sensorSat = ships.Single(s => s.GetDataBlob<NameDB>().GetName(player.Id) == "SensorSat I");

        var design = _prospector.GetDataBlob<ShipInfoDB>().Design;
        double holdVolume_m3 = StorageSpaceProcessor.CalculatedMaxStorage(design)[GeneralHold];
        _fullHold_kg = holdVolume_m3 * WaterKgPerCubicMetre;
        Assert.That(_fullHold_kg, Is.GreaterThan(0));
    }

    MoveCostShip Loaded(double holdFill)
    {
        var ship = MoveCostShip.FromEntity(_prospector);
        return ship with { CargoMass_kg = _fullHold_kg * holdFill };
    }

    static double SunPullAt(double au)
        => GeneralMath.StandardGravitationalParameter(UniversalConstants.Units.SolarMassInKG)
           / Math.Pow(au * UniversalConstants.Units.MetersPerAu, 2);

    [Test]
    public void FromEntity_ReadsTheShipBlobs()
    {
        var spec = MoveCostShip.FromEntity(_prospector);
        var mass = _prospector.GetDataBlob<MassVolumeDB>();
        var thrust = _prospector.GetDataBlob<NewtonThrustAbilityDB>();
        var warp = _prospector.GetDataBlob<WarpAbilityDB>();

        Assert.That(spec.MassTotal_kg, Is.EqualTo(mass.MassTotal).Within(1e-6));
        Assert.That(spec.FuelMass_kg, Is.EqualTo(thrust.TotalFuel_kg));
        Assert.That(spec.FuelType, Is.EqualTo(thrust.FuelType));
        Assert.That(spec.ExhaustVelocity, Is.EqualTo(thrust.ExhaustVelocity));
        Assert.That(spec.WarpPower, Is.EqualTo(warp.TotalWarpPower));
        Assert.That(spec.BubbleCreationCost, Is.EqualTo(warp.BubbleCreationCost));
        Assert.That(spec.FuelMass_kg, Is.GreaterThan(0));
    }

    [Test]
    public void MaxSpeed_IsDerivedFromWarpPowerAndDryMass_AndIgnoresCargo()
    {
        var warp = _prospector.GetDataBlob<WarpAbilityDB>();
        var mass = _prospector.GetDataBlob<MassVolumeDB>();
        Assert.That(mass.MassTotal, Is.GreaterThan(mass.MassDry * 2), "tanks are full");
        Assert.That(warp.MaxSpeed, Is.EqualTo(WarpMath.MaxSpeedCalc(warp.TotalWarpPower, mass.MassDry)));
    }

    [Test]
    public void MaxSpeed_OldSaveValueLoads_WhenThereIsNoWarpPower()
    {
        var settings = new JsonSerializerSettings { ContractResolver = new NonPublicResolver() };
        var loaded = JsonConvert.DeserializeObject<WarpAbilityDB>("{\"MaxSpeed\":12345.0,\"TotalWarpPower\":0.0}", settings)!;
        Assert.That(loaded.MaxSpeed, Is.EqualTo(12345.0));
        var copy = (WarpAbilityDB)loaded.Clone();
        Assert.That(copy.MaxSpeed, Is.EqualTo(12345.0));
    }

    [Test]
    public void Warp_OpenSpace_FliesAtTheLiveDriveSpeed_AndCargoDoesNotChangeEnergy()
    {
        double distance = UniversalConstants.Units.MetersPerAu;
        double liveSpeed = _prospector.GetDataBlob<WarpAbilityDB>().MaxSpeed;
        var costs = HoldFill.Select(f => MoveCostCalculator.Warp(Loaded(f), distance)).ToArray();

        foreach (var cost in costs)
        {
            Assert.That(cost.Feasible, Is.True, cost.Reason);
            Assert.That(cost.Reason, Is.Empty);
            Assert.That(cost.Speed, Is.EqualTo(liveSpeed), "drive speed is the empty-ship MaxSpeed");
            Assert.That(cost.DeltaV, Is.EqualTo(0));
            Assert.That(cost.FuelUsed_kg, Is.Empty);
            var ship = Loaded(0);
            double expected = ship.BubbleCreationCost + ship.BubbleSustainCost * distance / liveSpeed + ship.BubbleCollapseCost;
            Assert.That(cost.EnergyUsed_kJ, Is.EqualTo(expected).Within(1e-6));
        }
        Assert.That(costs[1].EnergyUsed_kJ, Is.EqualTo(costs[0].EnergyUsed_kJ));
        Assert.That(costs[2].EnergyUsed_kJ, Is.EqualTo(costs[0].EnergyUsed_kJ));
    }

    [Test]
    public void Warp_ClimbingFar_IsPlantLimited_AndCostsMoreWithCargo()
    {
        // At 5 AU the pull is 1/25 of Earth's, so the plant holds several km/s.
        double distance = UniversalConstants.Units.MetersPerAu;
        var costs = HoldFill
            .Select(f => MoveCostCalculator.Warp(Loaded(f), distance, SunPullAt(5), radial: 1))
            .ToArray();

        for (int i = 0; i < costs.Length; i++)
        {
            Assert.That(costs[i].Feasible, Is.True, costs[i].Reason);
            Assert.That(costs[i].Reason, Is.Empty, "a feasible warp keeps the limit line out of Reason");
            Assert.That(costs[i].Note, Does.Contain("Plant holds"));
            Assert.That(costs[i].Speed, Is.GreaterThanOrEqualTo(WarpMath.MinWarpSpeed_ms));
            if (i == 0)
                continue;
            Assert.That(costs[i].Speed, Is.LessThan(costs[i - 1].Speed), $"hold {HoldFill[i]}");
            Assert.That(costs[i].Duration_s, Is.GreaterThan(costs[i - 1].Duration_s), $"hold {HoldFill[i]}");
            Assert.That(costs[i].EnergyUsed_kJ, Is.GreaterThan(costs[i - 1].EnergyUsed_kJ), $"hold {HoldFill[i]}");
        }
    }

    [Test]
    public void Warp_ClimbingAt1Au_BelowMinimumSpeed_IsRefused()
    {
        var cost = MoveCostCalculator.Warp(Loaded(1), UniversalConstants.Units.MetersPerAu, SunPullAt(1), radial: 1);

        Assert.That(cost.Speed, Is.LessThan(WarpMath.MinWarpSpeed_ms));
        Assert.That(cost.Feasible, Is.False);
        Assert.That(cost.Reason, Is.EqualTo(WarpMath.WarpRefusal(cost.Speed, Loaded(1).EnergyOutputMax, Loaded(1).EnergyOutputMax)));
        Assert.That(cost.Reason, Does.Contain("minimum"));
    }

    [Test]
    public void Warp_SustainAbovePlantOutput_IsRefused()
    {
        var ship = Loaded(0) with { BubbleSustainCost = Loaded(0).EnergyOutputMax * 2 };
        var cost = MoveCostCalculator.Warp(ship, 1e9);
        Assert.That(cost.Feasible, Is.False);
        Assert.That(cost.Reason, Does.Contain("cannot hold"));
    }

    [Test]
    public void Warp_Downhill_BleedCapIgnoresCargo_AndGoesInNote()
    {
        double distance = UniversalConstants.Units.MetersPerAu;
        var ship = Loaded(0);
        double drive = WarpMath.MaxSpeedCalc(ship.WarpPower, ship.DryMass_kg);
        var costs = HoldFill
            .Select(f => MoveCostCalculator.Warp(Loaded(f), distance, SunPullAt(1), radial: -1))
            .ToArray();

        foreach (var cost in costs)
        {
            Assert.That(cost.Feasible, Is.True, cost.Reason);
            Assert.That(cost.Reason, Is.Empty);
            Assert.That(cost.Note, Does.StartWith("Downhill"));
            Assert.That(cost.Speed, Is.EqualTo(drive * ship.BleedFraction));
        }
        Assert.That(costs[2].EnergyUsed_kJ, Is.EqualTo(costs[0].EnergyUsed_kJ));
    }

    /// <summary>Same leg and numbers as WarpSustainTests.PureRadial_1Au_PlantHoldsAbout25287.</summary>
    [Test]
    public void Warp_PureRadialAt1Au_MatchesWarpSustainPricing()
    {
        var ship = new MoveCostShip
        {
            DryMass_kg = 40_000,
            HasWarp = true,
            WarpPower = 2_000_000,
            BubbleSustainCost = 15,
            Coupling = 0.0025,
            EnergyOutputMax = 15,
        };
        var cost = MoveCostCalculator.Warp(ship, 1e9, SunPullAt(1), radial: 1);

        Assert.That(cost.Feasible, Is.True, cost.Reason);
        Assert.That(cost.Speed, Is.EqualTo(25287).Within(25287 * 0.01));
        Assert.That(cost.EnergyUsed_kJ, Is.EqualTo(15 * cost.Duration_s).Within(1e-3), "plant-limited: demand is the plant output");
    }

    [Test]
    public void Warp_EmptyStore_WaitsToCharge_AndTooSmallAStoreIsInfeasible()
    {
        var ship = Loaded(0) with { EnergyStored = 0 };
        var charging = MoveCostCalculator.Warp(ship, 1e9);
        Assert.That(charging.Feasible, Is.True, charging.Reason);
        Assert.That(charging.ChargeWait_s, Is.EqualTo(ship.BubbleCreationCost / ship.EnergyOutputMax).Within(1e-6));

        var smallStore = ship with { EnergyStoreMax = ship.BubbleCreationCost / 2 };
        var cannot = MoveCostCalculator.Warp(smallStore, 1e9);
        Assert.That(cannot.Feasible, Is.False);
        Assert.That(cannot.Reason, Is.EqualTo("not enough power to open a warp"));
        Assert.That(cannot.EnergyUsed_kJ, Is.GreaterThan(0), "costs are still reported");
    }

    [Test]
    public void Burn_FuelRisesWithCargo_MatchesHandRocketEquation()
    {
        const double deltaV = 1000;
        var costs = HoldFill.Select(f => MoveCostCalculator.Burn(Loaded(f), deltaV)).ToArray();
        string fuel = Loaded(0).FuelType;

        for (int i = 0; i < costs.Length; i++)
        {
            var ship = Loaded(HoldFill[i]);
            // Tsiolkovsky by hand: m0 - m1 = m0 (1 - e^(-Δv / ve)).
            double m0 = ship.DryMass_kg + ship.CargoMass_kg + ship.FuelMass_kg;
            double expected = m0 * (1 - Math.Exp(-deltaV / ship.ExhaustVelocity));
            Assert.That(costs[i].Feasible, Is.True, costs[i].Reason);
            Assert.That(costs[i].DeltaV, Is.EqualTo(deltaV));
            Assert.That(costs[i].EnergyUsed_kJ, Is.EqualTo(0));
            Assert.That(costs[i].FuelUsed_kg[fuel], Is.EqualTo(expected).Within(expected * 1e-9));
            Assert.That(costs[i].BurnSeconds, Is.EqualTo(expected / ship.FuelBurnRate).Within(1e-6));
            if (i > 0)
                Assert.That(costs[i].FuelUsed_kg[fuel], Is.GreaterThan(costs[i - 1].FuelUsed_kg[fuel]), $"hold {HoldFill[i]}");
        }
    }

    [Test]
    public void Hohmann_FuelRisesWithCargo_DeltaVDoesNot()
    {
        var earthMass = _prospector.GetDataBlob<OrbitDB>().Parent!.GetDataBlob<MassVolumeDB>();
        double r1 = earthMass.RadiusInM + 400_000;
        double r2 = r1 * 3;
        var costs = HoldFill
            .Select(f => MoveCostCalculator.Hohmann(Loaded(f), earthMass.MassTotal, r1, r2))
            .ToArray();
        string fuel = Loaded(0).FuelType;

        for (int i = 0; i < costs.Length; i++)
        {
            Assert.That(costs[i].Feasible, Is.True, costs[i].Reason);
            Assert.That(costs[i].DeltaV, Is.EqualTo(costs[0].DeltaV).Within(costs[0].DeltaV * 1e-9));
            Assert.That(costs[i].Duration_s, Is.GreaterThan(0));
            if (i > 0)
                Assert.That(costs[i].FuelUsed_kg[fuel], Is.GreaterThan(costs[i - 1].FuelUsed_kg[fuel]), $"hold {HoldFill[i]}");
        }
    }

    [Test]
    public void Hohmann_NotEnoughFuel_IsInfeasibleAndSaysHowShort()
    {
        var earthMass = _prospector.GetDataBlob<OrbitDB>().Parent!.GetDataBlob<MassVolumeDB>();
        double r1 = earthMass.RadiusInM + 400_000;
        var ship = Loaded(0) with { FuelMass_kg = 1000 };

        var cost = MoveCostCalculator.Hohmann(ship, earthMass.MassTotal, r1, r1 * 3);

        Assert.That(cost.Feasible, Is.False);
        Assert.That(cost.Reason, Does.StartWith("needs "));
        Assert.That(cost.FuelUsed_kg[ship.FuelType], Is.GreaterThan(ship.FuelMass_kg), "the fuel it would need");
    }

    [Test]
    public void Hohmann_FromEntity_MatchesChangeOrbitalAltitudeBurns()
    {
        var orbit = _prospector.GetDataBlob<OrbitDB>();
        double r2 = orbit.SemiMajorAxis * 2;
        var cost = MoveCostCalculator.Hohmann(_prospector, r2);
        Assert.That(cost.Feasible, Is.True, cost.Reason);

        var spec = MoveCostCalculator.Hohmann(MoveCostShip.FromEntity(_prospector),
            orbit.Parent!.GetDataBlob<MassVolumeDB>().MassTotal, orbit.SemiMajorAxis, r2);
        string fuel = spec.FuelUsed_kg.Keys.Single();
        Assert.That(cost.DeltaV, Is.EqualTo(spec.DeltaV));
        Assert.That(cost.Duration_s, Is.EqualTo(spec.Duration_s));
        Assert.That(cost.FuelUsed_kg[fuel], Is.EqualTo(spec.FuelUsed_kg[fuel]));

        Assert.That(ChangeOrbitalAltitudeAction.TryPlanBurns(_prospector, r2, _prospector.StarSysDateTime, out var burns, out string reason),
            Is.True, reason);
        double gameDv = burns.Sum(b =>
            ((Vector3)OrbitMath.GetStateVectors(b.end, b.at).velocity
             - (Vector3)OrbitMath.GetStateVectors(b.start, b.at).velocity).Length());
        Assert.That(cost.DeltaV, Is.EqualTo(gameDv).Within(gameDv * 0.01));
    }

    [Test]
    public void Hohmann_FromEntity_RefusesWhatChangeOrbitalAltitudeRefuses()
    {
        var orbit = _prospector.GetDataBlob<OrbitDB>();
        double inside = orbit.Parent!.GetDataBlob<MassVolumeDB>().RadiusInM * 0.5;
        var now = _prospector.StarSysDateTime;

        var insideCost = MoveCostCalculator.Hohmann(_prospector, inside);
        Assert.That(ChangeOrbitalAltitudeAction.TryPlanBurns(_prospector, inside, now, out _, out string gameReason), Is.False);
        Assert.That(insideCost.Feasible, Is.False);
        Assert.That(insideCost.Reason, Is.EqualTo(gameReason));

        var hereCost = MoveCostCalculator.Hohmann(_prospector, orbit.SemiMajorAxis);
        Assert.That(ChangeOrbitalAltitudeAction.TryPlanBurns(_prospector, orbit.SemiMajorAxis, now, out var burns, out _), Is.True);
        Assert.That(burns, Is.Empty, "the game plans no burns");
        Assert.That(hereCost.Feasible, Is.False);
        Assert.That(hereCost.Reason, Is.EqualTo("already at altitude"));
    }

    [Test]
    public void NoDrives_AreInfeasibleWithAReason()
    {
        Assert.That(MoveCostCalculator.Warp(_sensorSat, 1e9).Reason, Is.EqualTo("no warp drive"));
        var spec = MoveCostShip.FromEntity(_sensorSat);
        Assert.That(MoveCostCalculator.Burn(spec, 100).Reason, Is.EqualTo("no reaction drive"));
        Assert.That(MoveCostCalculator.Hohmann(spec, 6e24, 7e6, 8e6).Feasible, Is.False);
    }
}
