using System;
using System.Linq;
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

namespace Pulsar4X.Tests;

/// <summary>
/// Move-order cost from real Quickstart ships. Cargo levels are 0, ½ and 1 × the ship's dry
/// mass of extra cargo, standing in for empty, half and full holds.
/// Warp: drive speed is fixed at the empty-ship speed, so in free space and downhill the energy
/// does not change with cargo; climbing out of a star's well it does.
/// Burns: Δv does not depend on mass, fuel scales with it.
/// </summary>
[TestFixture]
[NonParallelizable]
public class MoveCostCalculatorTests
{
    const string BaseMod = "Pulsar4x";
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";
    static readonly double[] CargoFractions = { 0, 0.5, 1 };

    Game _game = null!;
    Entity _surveyor = null!;
    Entity _sensorSat = null!;

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
        _surveyor = ships.Single(s => s.GetDataBlob<NameDB>().GetName(player.Id) == "Surveyor I");
        _sensorSat = ships.Single(s => s.GetDataBlob<NameDB>().GetName(player.Id) == "SensorSat I");
    }

    MoveCostShip Loaded(double cargoFraction)
    {
        var ship = MoveCostShip.FromEntity(_surveyor);
        return ship with { CargoMass_kg = ship.DryMass_kg * cargoFraction };
    }

    static double SunPullAt1Au()
        => GeneralMath.StandardGravitationalParameter(UniversalConstants.Units.SolarMassInKG)
           / Math.Pow(UniversalConstants.Units.MetersPerAu, 2);

    [Test]
    public void FromEntity_ReadsTheShipBlobs()
    {
        var spec = MoveCostShip.FromEntity(_surveyor);
        var mass = _surveyor.GetDataBlob<MassVolumeDB>();
        var thrust = _surveyor.GetDataBlob<NewtonThrustAbilityDB>();
        var warp = _surveyor.GetDataBlob<WarpAbilityDB>();

        Assert.That(spec.MassTotal_kg, Is.EqualTo(mass.MassTotal).Within(1e-6));
        Assert.That(spec.FuelMass_kg, Is.EqualTo(thrust.TotalFuel_kg));
        Assert.That(spec.FuelType, Is.EqualTo(thrust.FuelType));
        Assert.That(spec.ExhaustVelocity, Is.EqualTo(thrust.ExhaustVelocity));
        Assert.That(spec.WarpPower, Is.EqualTo(warp.TotalWarpPower));
        Assert.That(spec.BubbleCreationCost, Is.EqualTo(warp.BubbleCreationCost));
        Assert.That(spec.FuelMass_kg, Is.GreaterThan(0));
    }

    [Test]
    public void Warp_FreeSpace_FliesAtTheLiveDriveSpeed_AndCargoDoesNotChangeEnergy()
    {
        double distance = UniversalConstants.Units.MetersPerAu;
        double liveSpeed = _surveyor.GetDataBlob<WarpAbilityDB>().MaxSpeed;
        var costs = CargoFractions.Select(f => MoveCostCalculator.Warp(Loaded(f), distance)).ToArray();

        foreach (var cost in costs)
        {
            Assert.That(cost.Feasible, Is.True, cost.Reason);
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
    public void Warp_ClimbingOutOfTheSunsWell_CostsMoreWithCargo()
    {
        double distance = UniversalConstants.Units.MetersPerAu;
        var costs = CargoFractions
            .Select(f => MoveCostCalculator.Warp(Loaded(f), distance, SunPullAt1Au(), radial: 1))
            .ToArray();

        for (int i = 1; i < costs.Length; i++)
        {
            Assert.That(costs[i].Speed, Is.LessThan(costs[i - 1].Speed), $"cargo {CargoFractions[i]}");
            Assert.That(costs[i].Duration_s, Is.GreaterThan(costs[i - 1].Duration_s), $"cargo {CargoFractions[i]}");
            Assert.That(costs[i].EnergyUsed_kJ, Is.GreaterThan(costs[i - 1].EnergyUsed_kJ), $"cargo {CargoFractions[i]}");
        }
    }

    [Test]
    public void Warp_Downhill_BleedCapIgnoresCargo()
    {
        double distance = UniversalConstants.Units.MetersPerAu;
        var ship = Loaded(0);
        double drive = WarpMath.MaxSpeedCalc(ship.WarpPower, ship.DryMass_kg);
        var costs = CargoFractions
            .Select(f => MoveCostCalculator.Warp(Loaded(f), distance, SunPullAt1Au(), radial: -1))
            .ToArray();

        foreach (var cost in costs)
            Assert.That(cost.Speed, Is.EqualTo(drive * ship.BleedFraction));
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
        var cost = MoveCostCalculator.Warp(ship, 1e9, SunPullAt1Au(), radial: 1);

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
    public void Burn_FuelRisesWithCargo()
    {
        const double deltaV = 1000;
        var costs = CargoFractions.Select(f => MoveCostCalculator.Burn(Loaded(f), deltaV)).ToArray();
        string fuel = Loaded(0).FuelType;

        for (int i = 0; i < costs.Length; i++)
        {
            var ship = Loaded(CargoFractions[i]);
            Assert.That(costs[i].Feasible, Is.True, costs[i].Reason);
            Assert.That(costs[i].DeltaV, Is.EqualTo(deltaV));
            Assert.That(costs[i].EnergyUsed_kJ, Is.EqualTo(0));
            Assert.That(costs[i].FuelUsed_kg[fuel],
                Is.EqualTo(OrbitMath.TsiolkovskyFuelUse(ship.MassTotal_kg, ship.ExhaustVelocity, deltaV)).Within(1e-6));
            Assert.That(costs[i].BurnSeconds, Is.EqualTo(costs[i].FuelUsed_kg[fuel] / ship.FuelBurnRate).Within(1e-9));
            if (i > 0)
                Assert.That(costs[i].FuelUsed_kg[fuel], Is.GreaterThan(costs[i - 1].FuelUsed_kg[fuel]), $"cargo {CargoFractions[i]}");
        }
    }

    [Test]
    public void Hohmann_FuelRisesWithCargo_DeltaVDoesNot()
    {
        var earth = _surveyor.GetDataBlob<OrbitDB>().Parent!;
        var earthMass = earth.GetDataBlob<MassVolumeDB>();
        double r1 = earthMass.RadiusInM + 400_000;
        double r2 = r1 * 3;
        var costs = CargoFractions
            .Select(f => MoveCostCalculator.Hohmann(Loaded(f), earthMass.MassTotal, r1, r2))
            .ToArray();
        string fuel = Loaded(0).FuelType;

        for (int i = 0; i < costs.Length; i++)
        {
            Assert.That(costs[i].Feasible, Is.True, costs[i].Reason);
            Assert.That(costs[i].DeltaV, Is.EqualTo(costs[0].DeltaV).Within(costs[0].DeltaV * 1e-9));
            Assert.That(costs[i].Duration_s, Is.GreaterThan(0));
            if (i > 0)
                Assert.That(costs[i].FuelUsed_kg[fuel], Is.GreaterThan(costs[i - 1].FuelUsed_kg[fuel]), $"cargo {CargoFractions[i]}");
        }
    }

    [Test]
    public void Hohmann_NotEnoughFuel_IsInfeasibleAndSaysHowShort()
    {
        var earth = _surveyor.GetDataBlob<OrbitDB>().Parent!;
        var earthMass = earth.GetDataBlob<MassVolumeDB>();
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
        var orbit = _surveyor.GetDataBlob<OrbitDB>();
        double r2 = orbit.SemiMajorAxis * 2;
        var cost = MoveCostCalculator.Hohmann(_surveyor, r2);
        Assert.That(cost.Feasible, Is.True, cost.Reason);

        var spec = MoveCostCalculator.Hohmann(MoveCostShip.FromEntity(_surveyor),
            orbit.Parent!.GetDataBlob<MassVolumeDB>().MassTotal, orbit.SemiMajorAxis, r2);
        Assert.That(cost, Is.EqualTo(spec).Using<MoveCost>((a, b) =>
            a.DeltaV == b.DeltaV && a.Duration_s == b.Duration_s
            && a.FuelUsed_kg[spec.FuelUsed_kg.Keys.Single()] == b.FuelUsed_kg[spec.FuelUsed_kg.Keys.Single()]));

        Assert.That(ChangeOrbitalAltitudeAction.TryPlanBurns(_surveyor, r2, _surveyor.StarSysDateTime, out var burns, out string reason),
            Is.True, reason);
        double gameDv = burns.Sum(b =>
            ((Vector3)OrbitMath.GetStateVectors(b.end, b.at).velocity
             - (Vector3)OrbitMath.GetStateVectors(b.start, b.at).velocity).Length());
        Assert.That(cost.DeltaV, Is.EqualTo(gameDv).Within(gameDv * 0.01));
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
