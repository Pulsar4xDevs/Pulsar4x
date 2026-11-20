using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Recycling: a ship at a colony with a shipyard, or components and installations at a factory that builds them, is
/// taken apart by a normal industry job and returns <see cref="GameSettings.RecycleReturnFraction"/> of its build
/// resources (components broken down). A scrapped ship's cargo is unloaded, its crew go to the pool.
/// </summary>
[TestFixture]
[NonParallelizable]
public class RecyclingTests
{
    const string WarpTech = "Pulsar4x.Scenario.WarpTechStart";

    Game _game = null!;
    Entity _faction = null!;
    Entity _colony = null!;
    string _yardLine = "";
    FactionInfoDB Info => _faction.GetDataBlob<FactionInfoDB>();
    CargoStorageDB Storage => _colony.GetDataBlob<CargoStorageDB>();
    IndustryAbilityDB Industry => _colony.GetDataBlob<IndustryAbilityDB>();

    [SetUp]
    public void SetUp()
    {
        _yardLine = "";
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), WarpTech);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345));
        Assert.That(created, Is.Not.Null);
        (_game, _faction) = (created!.Value.game, created.Value.faction);
        _game.Settings.EnforceSingleThread = true;
        foreach (var system in _game.Systems)
            foreach (var entity in system.GetAllEntitiesWithDataBlob<IndustryAbilityDB>())
                if (_yardLine == "" && entity.FactionOwnerID == _faction.Id && entity.HasDataBlob<ColonyInfoDB>()
                    && Recycling.ShipyardLine(entity) is { } line)
                    (_colony, _yardLine) = (entity, line);
        Assert.That(_yardLine, Is.Not.Empty, "a player colony with a shipyard");
        // Only the jobs these tests queue run, so warehouse changes are theirs.
        foreach (var line in Industry.ProductionLines.Values)
            line.Jobs.Clear();
    }

    Entity NewShip()
    {
        var slip = Industry.ProductionLines[_yardLine];
        var design = Info.ShipDesigns.Values.First(d => d.MassPerUnit <= slip.MaxVolume && d.CrewReq > 0);
        var ship = ShipFactory.CreateShip(design, _faction, _colony.GetDataBlob<ColonyInfoDB>().PlanetEntity, "Old Hull");
        if (!ship.HasDataBlob<CargoStorageDB>())
            ship.SetDataBlob(new CargoStorageDB());
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        Storage.TransferRangeDv_mps = 1e12;
        return ship;
    }

    CommandResult Send(Entity target, GameCommand command) => new CommandTranslator(_game).Translate(_faction, target, command);

    void RunUntil(Func<bool> done, int maxDays = 400)
    {
        for (int day = 0; day < maxDays && !done(); day++)
            new IndustryProcessor().ProcessEntity(_colony, 0);
        Assert.That(done(), Is.True, "finished within " + maxDays + " days");
    }

    long Units(string id) => Info.Data.CargoGoods.GetAny(id) is { } good ? Storage.GetUnitsStored(good, false) : 0;

    /// <summary>The design's build resources with components broken down, independently of the engine helper.</summary>
    Dictionary<string, double> Raw(IConstructableDesign design, double count = 1, Dictionary<string, double>? into = null)
    {
        into ??= new Dictionary<string, double>();
        foreach (var (id, amount) in design.ResourceCosts)
        {
            if (Info.ComponentDesigns.TryGetValue(id, out var part))
                Raw(part, count * amount, into);
            else
                into[id] = into.GetValueOrDefault(id) + count * amount;
        }
        return into;
    }

    void AssertHalf(IConstructableDesign design, IReadOnlyDictionary<string, long> returned, int items)
    {
        var raw = Raw(design);
        Assert.That(raw, Is.Not.Empty);
        foreach (var (id, amount) in raw)
        {
            long expected = items * (long)Math.Floor(amount * Pulsar4X.Engine.GameSettings.RecycleReturnFraction + 1e-9);
            Assert.That(returned.GetValueOrDefault(id), Is.EqualTo(expected), $"{id}: half of {amount} x{items}");
        }
        Assert.That(Pulsar4X.Engine.GameSettings.RecycleReturnFraction, Is.EqualTo(0.5));
    }

    Dictionary<string, long> Snapshot(IEnumerable<string> ids) => ids.Distinct().ToDictionary(id => id, Units);

    [Test]
    public void ScrappingAShip_ReturnsHalfItsResources_UnloadsItsCargo_AndSendsTheCrewToThePool()
    {
        var ship = NewShip();
        var shipCargo = ship.GetDataBlob<CargoStorageDB>();
        var fuel = Info.Data.CargoGoods.GetAny("methalox") ?? Info.Data.CargoGoods.GetAny("rp-1");
        bool loaded = fuel != null && shipCargo.TypeStores.ContainsKey(fuel.CargoTypeID) && shipCargo.AddCargoByUnit(fuel, 10) > 0;
        var aboard = shipCargo.TypeStores.Values.SelectMany(s => s.CurrentStoreInUnits.Where(u => u.Value > 0)
            .Select(u => (s.Cargoables[u.Key].UniqueID, u.Value))).GroupBy(p => p.UniqueID).ToDictionary(g => g.Key, g => g.Sum(p => p.Value));
        int crew = ShipCrew.CrewAboard(ship);
        Assert.That(crew, Is.GreaterThan(0), "a crewed ship");
        int captainId = ship.GetDataBlob<ShipInfoDB>().CommanderID;
        int poolBefore = ShipCrew.Available(_colony, int.MaxValue);
        var design = ship.GetDataBlob<ShipInfoDB>().Design;
        var ids = Raw(design).Keys.Concat(aboard.Keys).ToList();
        var before = Snapshot(ids);

        var result = Send(_colony, new RecycleShipCommand(_colony.Id, ship.Id));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var job = Industry.ProductionLines[_yardLine].Jobs.Single();
        Assert.That(job.Recycle!.Kind, Is.EqualTo(RecycleKind.Ship));
        Assert.That(job.ProductionPointsCost, Is.EqualTo((long)Math.Ceiling(design.IndustryPointCosts * 0.5)));
        var view = new GameProjector(_game).ProjectEntity(_colony, _faction.Id).GetView<IndustryView>()!;
        var jobView = view.ProductionLines.Single(l => l.Id == _yardLine).Jobs.Single();
        Assert.That(jobView.IsRecycle, Is.True);
        Assert.That(jobView.Recycle!.ShipEntityId, Is.EqualTo(ship.Id));
        Assert.That(jobView.Recycle.Returns, Is.Not.Empty);
        Assert.That(Send(ship, new RenameCommand(ship.Id, "Still flying")).Accepted, Is.False, "a ship queued for scrapping takes no orders");

        RunUntil(() => !ship.IsValid);

        var returned = ids.ToDictionary(id => id, id => Units(id) - before[id] - aboard.GetValueOrDefault(id));
        AssertHalf(design, returned, 1);
        if (loaded)
            Assert.That(aboard.ContainsKey(fuel!.UniqueID), Is.True, "the fuel aboard was unloaded to the colony");
        Assert.That(ShipCrew.Available(_colony, int.MaxValue), Is.EqualTo(poolBefore + crew), "the crew went to the pool");
        Assert.That(Industry.ProductionLines[_yardLine].Jobs, Is.Empty);
        if (captainId >= 0)
        {
            Assert.That(_colony.Manager!.TryGetGlobalEntityById(captainId, out var captain), Is.True, "the captain is not killed");
            Assert.That(captain.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
        }
    }

    ComponentDesign Recyclable(Func<ComponentDesign, bool> kind)
        => Info.ComponentDesigns.Values.Where(d => d.IsValid && kind(d) && d.ResourceCosts.Count > 0 && Recycling.LineFor(_colony, d) != null)
            .OrderBy(d => d.UniqueID, StringComparer.Ordinal).First();

    [Test]
    public void RecyclingStoredComponents_ReturnsHalfTheirResources()
    {
        var part = Recyclable(ShipRefitJob.IsShipPart);
        Assert.That(Storage.AddCargoByUnit(new ComponentInstance(part), 1), Is.EqualTo(1));
        Assert.That(Storage.AddCargoByUnit(part, 1), Is.EqualTo(1), "a stack of the design counts too");
        var ids = Raw(part).Keys.ToList();
        var before = Snapshot(ids);

        var result = Send(_colony, new RecycleComponentCommand(_colony.Id, part.UniqueID, 2));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        string line = Recycling.LineFor(_colony, part)!;
        Assert.That(IndustryTools.IsShipyardLine(Industry.ProductionLines[line]), Is.False, "a factory line");
        RunUntil(() => !Recycling.JobsOf(_colony).Any());

        Assert.That(Recycling.Available(_colony, Storage, part), Is.EqualTo(0), "both taken apart");
        AssertHalf(part, ids.ToDictionary(id => id, id => Units(id) - before[id]), 2);
    }

    [Test]
    public void RecyclingAStoredInstallation_ReturnsHalfItsResources()
    {
        var installation = Recyclable(ShipRefitJob.IsColonyInstallation);
        Assert.That(Storage.AddCargoByUnit(installation, 1), Is.EqualTo(1));
        var ids = Raw(installation).Keys.ToList();
        var before = Snapshot(ids);

        var result = Send(_colony, new RecycleComponentCommand(_colony.Id, installation.UniqueID, 1));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var candidates = new GameProjector(_game).ProjectEntity(_colony, _faction.Id).GetView<IndustryView>()!;
        Assert.That(candidates.ProductionLines.SelectMany(l => l.Jobs).Single().Recycle!.Kind, Is.EqualTo("Component"));
        RunUntil(() => !Recycling.JobsOf(_colony).Any());

        AssertHalf(installation, ids.ToDictionary(id => id, id => Units(id) - before[id]), 1);
    }

    [Test]
    public void Refusals()
    {
        var ship = NewShip();
        string factory = Industry.ProductionLines.First(l => !IndustryTools.IsShipyardLine(l.Value)).Key;
        var part = Recyclable(ShipRefitJob.IsShipPart);

        Assert.That(Send(_colony, new RecycleShipCommand(_colony.Id, ship.Id, factory)).RejectionReason, Is.EqualTo(Recycling.NoShipyard));
        Assert.That(Send(_colony, new RecycleShipCommand(_colony.Id, 987_654)).Accepted, Is.False, "unknown ship");
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 0;
        Storage.TransferRangeDv_mps = 0;
        Assert.That(Send(_colony, new RecycleShipCommand(_colony.Id, ship.Id)).RejectionReason, Is.EqualTo(Recycling.OutOfRange));
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        Storage.TransferRangeDv_mps = 1e12;
        Assert.That(Send(_colony, new RecycleShipCommand(_colony.Id, ship.Id)).Accepted, Is.True);
        Assert.That(Send(_colony, new RecycleShipCommand(_colony.Id, ship.Id)).RejectionReason, Is.EqualTo(Recycling.AlreadyQueued));
        var job = Recycling.JobsOf(_colony).Single();
        Assert.That(Send(_colony, new EditIndustryJobCommand(_colony.Id, job.LineId, job.Job.JobID, 2, false)).Accepted, Is.False, "not editable");

        Assert.That(Send(_colony, new RecycleComponentCommand(_colony.Id, part.UniqueID, 1)).RejectionReason, Is.EqualTo(Recycling.NotInWarehouse));
        Assert.That(Storage.AddCargoByUnit(part, 1), Is.EqualTo(1));
        Assert.That(Send(_colony, new RecycleComponentCommand(_colony.Id, part.UniqueID, 2)).RejectionReason, Is.EqualTo(Recycling.NotInWarehouse));
        Assert.That(Send(_colony, new RecycleComponentCommand(_colony.Id, part.UniqueID, 0)).RejectionReason, Is.EqualTo(Recycling.NothingToRecycle));
        Assert.That(Send(_colony, new RecycleComponentCommand(_colony.Id, part.UniqueID, 1, _yardLine)).RejectionReason, Is.EqualTo(Recycling.NotThisLine));
        Assert.That(Send(_colony, new RecycleComponentCommand(_colony.Id, "no-such-part", 1)).RejectionReason, Is.EqualTo(Recycling.UnknownDesign));
        Assert.That(Send(_colony, new RecycleComponentCommand(_colony.Id, part.UniqueID, 1)).Accepted, Is.True);
    }

    [Test]
    public void AComponentThatLeavesTheWarehouse_WaitsWithAReason()
    {
        var part = Recyclable(ShipRefitJob.IsShipPart);
        Assert.That(Storage.AddCargoByUnit(part, 1), Is.EqualTo(1));
        Assert.That(Send(_colony, new RecycleComponentCommand(_colony.Id, part.UniqueID, 1)).Accepted, Is.True);
        Assert.That(Storage.RemoveCargoByUnit(part, 1), Is.EqualTo(1));
        new IndustryProcessor().ProcessEntity(_colony, 0);
        var job = Recycling.JobsOf(_colony).Single().Job;
        Assert.That(job.WaitReason, Is.EqualTo(Recycling.WaitingFor + part.Name));
        Assert.That(job.ProductionPointsLeft, Is.EqualTo(job.ProductionPointsCost), "no points spent");
    }

    [Test]
    public void RecycleJobs_SurviveSaveAndLoad_AndFinishAfterwards()
    {
        var ship = NewShip();
        var part = Recyclable(ShipRefitJob.IsShipPart);
        Assert.That(Storage.AddCargoByUnit(part, 1), Is.EqualTo(1));
        Assert.That(Send(_colony, new RecycleShipCommand(_colony.Id, ship.Id)).Accepted, Is.True);
        Assert.That(Send(_colony, new RecycleComponentCommand(_colony.Id, part.UniqueID, 1)).Accepted, Is.True);
        var returns = Recycling.JobsOf(_colony).Single(j => j.Job.Recycle!.Kind == RecycleKind.Component).Job.Recycle!.Returns;

        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        var colony = loaded.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<IndustryAbilityDB>()).Single(e => e.Id == _colony.Id);
        var jobs = Recycling.JobsOf(colony).Select(j => j.Job).ToList();
        Assert.That(jobs, Has.Count.EqualTo(2));
        var shipJob = jobs.Single(j => j.Recycle!.Kind == RecycleKind.Ship);
        Assert.That((shipJob.Recycle!.ShipEntityId, shipJob.Recycle.DesignId), Is.EqualTo((ship.Id, ship.GetDataBlob<ShipInfoDB>().Design.UniqueID)));
        var partJob = jobs.Single(j => j.Recycle!.Kind == RecycleKind.Component);
        Assert.That(partJob.Recycle!.DesignId, Is.EqualTo(part.UniqueID));
        Assert.That(partJob.Recycle.Returns, Is.EqualTo(returns));

        for (int day = 0; day < 400 && Recycling.JobsOf(colony).Any(); day++)
            new IndustryProcessor().ProcessEntity(colony, 0);
        Assert.That(Recycling.JobsOf(colony), Is.Empty, "both finish after the load");
        Assert.That(colony.Manager!.TryGetEntityById(ship.Id, out var gone) && gone.IsValid, Is.False, "the ship is gone");
    }
}
