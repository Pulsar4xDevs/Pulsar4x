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
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// A shipyard refit is a normal industry job on the yard's line. It spends the yard's ship-refit points,
/// shows in the line's job list with progress, and completes like any job, beside a new-ship build.
/// </summary>
[TestFixture]
[NonParallelizable]
public class RefitIndustryJobTests
{
    const string WarpTech = "Pulsar4x.Scenario.WarpTechStart";
    const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";

    [TestCase(WarpTech, false)]
    [TestCase(EarlyTech, false)]
    [TestCase(WarpTech, true)]
    [TestCase(EarlyTech, true)]
    public void RefitAndNewShipBuild_BothComplete_InTheSameYard(string scenario, bool buildQueuedFirst)
    {
        var (game, faction) = Start(scenario);
        var (yard, lineId) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var line = yard.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId];
        Assert.That(yard.GetDataBlob<LocalConstructionDB>().RefitPointsPerDay, Is.GreaterThan(0), "the start's yard has a refit rate");

        var (from, to) = PickPair(yard, info, line);
        var ship = NewShip(yard, faction, from);
        var (remove, mount) = Diff(yard, ship, to);
        Assert.That(remove.Count + mount.Count, Is.GreaterThan(0), "the two designs differ");

        var translator = new CommandTranslator(game);
        void QueueBuild()
        {
            var queuedBuild = translator.Translate(faction, yard, new QueueIndustryJobCommand(yard.Id, lineId, from.UniqueID, 1, false, false));
            Assert.That(queuedBuild.Accepted, Is.True, queuedBuild.RejectionReason);
        }
        void QueueRefit()
        {
            var queuedRefit = translator.Translate(faction, yard, new QueueShipRefitCommand(
                yard.Id, lineId, ship.Id, remove.Select(p => p.ID).ToList(), mount.Select(p => p.ID).ToList(), to.UniqueID));
            Assert.That(queuedRefit.Accepted, Is.True, queuedRefit.RejectionReason);
        }
        if (buildQueuedFirst) { QueueBuild(); QueueRefit(); } else { QueueRefit(); QueueBuild(); }
        StockResources(yard, line, info);
        int hullsBefore = line.Hulls.Count;

        var view = new GameProjector(game).ProjectEntity(yard, faction.Id).GetView<IndustryView>()!
            .ProductionLines.Single(l => l.Id == lineId);
        var refitView = view.Jobs.Single(j => j.IsRefit);
        Assert.That(view.Jobs, Has.Count.EqualTo(2));
        Assert.That(refitView.Refit!.ShipEntityId, Is.EqualTo(ship.Id));
        Assert.That(refitView.Refit.FromDesign, Is.EqualTo(from.Name));
        Assert.That(refitView.Refit.ToDesign, Is.EqualTo(to.Name));
        Assert.That(refitView.NumberOrdered, Is.EqualTo(1));
        Assert.That(refitView.ProductionPointsLeft, Is.GreaterThan(0));

        int days = 0;
        while (days++ < 600 && (ShipRefitJob.JobsOf(yard).Any() || line.Hulls.Count == hullsBefore || line.Jobs.Count > 0))
            new IndustryProcessor().ProcessEntity(yard, 0);

        Assert.That(ShipRefitJob.JobsOf(yard), Is.Empty, "the refit finished");
        Assert.That(ship.GetDataBlob<ShipInfoDB>().Design.UniqueID, Is.EqualTo(to.UniqueID), "the ship wears the new design");
        var counts = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values
            .GroupBy(c => c.Design.UniqueID).ToDictionary(g => g.Key, g => g.Count());
        foreach (var (part, count) in to.Components)
            Assert.That(counts.GetValueOrDefault(part.UniqueID), Is.EqualTo(count), part.Name);
        Assert.That(line.Hulls, Has.Count.EqualTo(hullsBefore + 1), "the new-ship build finished too");
        Assert.That(line.Jobs, Is.Empty);
        TestContext.Out.WriteLine($"{scenario}: both done in {days - 1} days");
    }

    [TestCase(WarpTech)]
    [TestCase(EarlyTech)]
    public void Shipyard_StillRefusesComponentJobs_ButAcceptsRefits(string scenario)
    {
        var (game, faction) = Start(scenario);
        var (yard, lineId) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var component = info.IndustryDesigns.Values.OfType<ComponentDesign>().First(c => c.IndustryTypeID == "component-construction");

        var refused = new CommandTranslator(game).Translate(faction, yard,
            new QueueIndustryJobCommand(yard.Id, lineId, component.UniqueID, 1, false, false));

        Assert.That(refused.Accepted, Is.False);
        Assert.That(refused.RejectionReason, Does.Contain("Shipyards build only ships"));
        var line = yard.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId];
        Assert.That(IndustryTools.LineAccepts(line, component), Is.False);
        Assert.That(IndustryTools.LineAccepts(line, new ShipRefitDesign("Refit", 1)), Is.True);
        Assert.That(line.Jobs, Is.Empty);
    }

    [Test]
    public void RefitJob_CanBeCancelled_AndAShipOutOfRangeWaitsWithAReason()
    {
        var (game, faction) = Start(WarpTech);
        var (yard, lineId) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var line = yard.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId];
        var (from, to) = PickPair(yard, info, line);
        var ship = NewShip(yard, faction, from);
        var (remove, mount) = Diff(yard, ship, to);
        var translator = new CommandTranslator(game);
        var queued = translator.Translate(faction, yard, new QueueShipRefitCommand(
            yard.Id, lineId, ship.Id, remove.Select(p => p.ID).ToList(), mount.Select(p => p.ID).ToList()));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        // Take a part off the ship behind the job's back: the job waits and says why.
        var gone = remove.FirstOrDefault() ?? mount.First();
        if (remove.Count > 0)
            ship.RemoveComponent(gone);
        else
            yard.GetDataBlob<CargoStorageDB>().RemoveCargoByUnit(gone, 1);
        new IndustryProcessor().ProcessEntity(yard, 0);
        var job = line.Jobs.Single();
        Assert.That(job.Status, Is.EqualTo(Pulsar4X.DataStructures.IndustryJobStatus.MissingResources));
        Assert.That(job.ProductionPointsLeft, Is.EqualTo(job.ProductionPointsCost), "no points spent while it waits");
        var view = new GameProjector(game).ProjectEntity(yard, faction.Id).GetView<IndustryView>()!
            .ProductionLines.Single(l => l.Id == lineId).Jobs.Single();
        Assert.That(view.Refit!.WaitReason, Is.Not.Empty);

        var cancel = translator.Translate(faction, yard, new CancelIndustryJobCommand(yard.Id, lineId, job.JobID));
        Assert.That(cancel.Accepted, Is.True, cancel.RejectionReason);
        Assert.That(line.Jobs, Is.Empty);
        Assert.That(ShipRefitJob.TryFind(ship, out _, out _), Is.False);
    }

    [Test]
    public void Warp_RefitMountingThenRemovingPassengerQuarters_Completes_OnCorrectData()
    {
        var (game, faction) = Start(WarpTech);
        var (yard, lineId) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var line = yard.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId];
        var storage = yard.GetDataBlob<CargoStorageDB>();
        var quarters = PassengerQuarters(info);
        Assert.That(quarters.CargoTypeID, Is.EqualTo("general-storage"), "a Passenger Quarters part is stored as general cargo");
        Assert.That(storage.TypeStores.ContainsKey(quarters.CargoTypeID), Is.True, "so the warehouse can hold it");
        var ship = NewShip(yard, faction, info.ShipDesigns.Values.First(d => d.MassPerUnit <= line.MaxVolume));
        var translator = new CommandTranslator(game);

        // Mount: a Passenger Quarters in the warehouse goes onto the ship.
        var crate = new ComponentInstance(quarters);
        Assert.That(storage.AddCargoByUnit(crate, 1), Is.EqualTo(1));
        var mounted = translator.Translate(faction, yard, new QueueShipRefitCommand(
            yard.Id, lineId, ship.Id, Array.Empty<int>(), new[] { crate.ID }));
        Assert.That(mounted.Accepted, Is.True, mounted.RejectionReason);
        RunDays(yard, 60, () => ShipRefitJob.JobsOf(yard).Any());
        Assert.That(ShipRefitJob.JobsOf(yard), Is.Empty, "the mount completed");
        Assert.That(ShipRefitJob.OnShip(ship, crate.ID), Is.True);
        Assert.That(storage.GetUnitsStored(crate, false), Is.EqualTo(0));

        // Remove it again with the warehouse's general store full: the part still comes off and is kept.
        foreach (var store in storage.TypeStores.Values)
            store.FreeVolume = 0;
        var removed = translator.Translate(faction, yard, new QueueShipRefitCommand(
            yard.Id, lineId, ship.Id, new[] { crate.ID }, Array.Empty<int>()));
        Assert.That(removed.Accepted, Is.True, removed.RejectionReason);
        RunDays(yard, 60, () => ShipRefitJob.JobsOf(yard).Any());
        Assert.That(ShipRefitJob.JobsOf(yard), Is.Empty, "the removal completed");
        Assert.That(ShipRefitJob.OnShip(ship, crate.ID), Is.False);
        Assert.That(storage.GetUnitsStored(crate, false), Is.EqualTo(1), "the removed part is in the warehouse");
    }

    [Test]
    public void RemovingAYardWithNoStoreForThePart_WaitsWithAReason_AndChangesNothing()
    {
        var (game, faction) = Start(WarpTech);
        var (yard, lineId) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var line = yard.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId];
        var storage = yard.GetDataBlob<CargoStorageDB>();
        var quarters = PassengerQuarters(info);
        var ship = NewShip(yard, faction, info.ShipDesigns.Values.First(d => d.MassPerUnit <= line.MaxVolume));
        var part = new ComponentInstance(quarters);
        ship.AddComponent(part);
        var queued = new CommandTranslator(game).Translate(faction, yard, new QueueShipRefitCommand(
            yard.Id, lineId, ship.Id, new[] { part.ID }, Array.Empty<int>()));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);
        storage.TypeStores.Remove(quarters.CargoTypeID);

        RunDays(yard, 5, () => true);

        var job = line.Jobs.Single();
        Assert.That(job.WaitReason, Is.EqualTo("This yard has no storage for " + part.Name + "."));
        Assert.That(ShipRefitJob.OnShip(ship, part.ID), Is.True);
        Assert.That(job.ProductionPointsLeft, Is.EqualTo(job.ProductionPointsCost), "nothing spent while it waits");
    }

    sealed class HoldScene
    {
        public Game Game = null!;
        public Entity Faction = null!, Yard = null!, Ship = null!;
        public string LineId = "";
        public IndustryAbilityDB.ProductionLine Line = null!;
        public CargoStorageDB Colony = null!, Cargo = null!;
        public FactionInfoDB Info = null!;
        public ComponentInstance Hold = null!, Other = null!, Fresh = null!;
        public string TypeId = "";
        public ICargoable Load = null!;
        public long Units;
        public long OverflowUnits;
        public double CapacityBefore;
    }

    /// <summary>A ship with an extra hold of <paramref name="what"/> filled to the brim, and a refit-ready swap of another part.</summary>
    static HoldScene OccupiedHoldScene(string what)
    {
        var (game, faction) = Start(WarpTech);
        var (yard, lineId) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var line = yard.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId];
        var colony = yard.GetDataBlob<CargoStorageDB>();
        var holdDesign = what switch
        {
            "passengers" => PassengerQuarters(info),
            "fuel" => info.ComponentDesigns.Values.First(d => d.TryGetAttribute<CargoStorageAtb>(out var atb)
                && atb.StoreTypeID == "fuel-storage" && ShipRefitJob.IsShipPart(d)),
            _ => info.ComponentDesigns.Values.First(d => d.Name.StartsWith("Cargo Hold")
                && d.TryGetAttribute<CargoStorageAtb>(out var atb) && atb.StoreTypeID == "general-storage"
                && ShipRefitJob.IsShipPart(d)),
        };
        var ship = NewShip(yard, faction, info.ShipDesigns.Values.First(d => d.MassPerUnit <= line.MaxVolume));
        var hold = new ComponentInstance(holdDesign);
        ship.AddComponent(hold);
        var cargo = ship.GetDataBlob<CargoStorageDB>();
        var atbData = holdDesign.GetAttribute<CargoStorageAtb>();
        string typeId = atbData.StoreTypeID;

        ICargoable load;
        if (what == "passengers")
        {
            SpeciesCargo.RegisterOwned(faction);
            load = new SpeciesCargo(info.Species.First());
        }
        else
            load = info.Data.CargoGoods.GetAll().Values.First(c => c.CargoTypeID == typeId && c.VolumePerUnit > 0);
        long units = (long)(cargo.TypeStores[typeId].FreeVolume / load.VolumePerUnit);
        Assert.That(cargo.AddCargoByUnit(load, units), Is.GreaterThan(0), "the hold is loaded");

        var other = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.First(c =>
            c.ID != hold.ID && ShipRefitJob.IsShipPart(c.Design) && !c.Design.HasAttribute<CargoStorageAtb>()
            && !c.Design.HasAttribute<GameEngine.People.AdminSpaceAtb>());
        var fresh = new ComponentInstance(other.Design);
        Assert.That(colony.AddCargoByUnit(fresh, 1), Is.EqualTo(1));
        return new HoldScene
        {
            Game = game, Faction = faction, Yard = yard, Ship = ship, LineId = lineId, Line = line, Colony = colony,
            Cargo = cargo, Info = info, Hold = hold, Other = other, Fresh = fresh, TypeId = typeId, Load = load,
            Units = units, CapacityBefore = cargo.TypeStores[typeId].MaxVolume,
            OverflowUnits = (long)Math.Ceiling(atbData.MaxVolume / load.VolumePerUnit),
        };
    }

    /// <summary>Sets how many units of the load the yard colony can take: store room, or housing for people.</summary>
    static void SetColonyRoom(HoldScene s, long units)
    {
        if (s.Load is SpeciesCargo people)
        {
            var colonyInfo = s.Yard.GetDataBlob<ColonyInfoDB>();
            s.Game.Systems.First().TryGetGlobalEntityById(people.SpeciesId, out var species);
            long capacity = Pulsar4X.Colonies.MigrationPlanner.Capacity(s.Yard, species);
            Assert.That(capacity, Is.GreaterThan(units), "the colony can house this species");
            colonyInfo.Population[people.SpeciesId] = capacity - units;
            return;
        }
        var store = s.Colony.TypeStores[s.TypeId];
        double volume = units * s.Load.VolumePerUnit;
        if (store.MaxVolume < volume)
            store.MaxVolume = volume;
        store.FreeVolume = volume;
    }

    static long ColonyHolds(HoldScene s)
        => s.Load is SpeciesCargo people
            ? s.Yard.GetDataBlob<ColonyInfoDB>().Population.GetValueOrDefault(people.SpeciesId)
            : s.Colony.GetUnitsStored(s.Load, false);

    static void QueueSwap(HoldScene s)
    {
        var queued = new CommandTranslator(s.Game).Translate(s.Faction, s.Yard, new QueueShipRefitCommand(
            s.Yard.Id, s.LineId, s.Ship.Id, new[] { s.Hold.ID, s.Other.ID }, new[] { s.Fresh.ID }));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);
    }

    [TestCase("passengers")]
    [TestCase("cargo")]
    [TestCase("fuel")]
    public void RemovingAnOccupiedHold_UnloadsTheOverflowToTheColony_AndCompletes(string what)
    {
        var s = OccupiedHoldScene(what);
        SetColonyRoom(s, s.OverflowUnits * 2);
        long colonyBefore = ColonyHolds(s);
        QueueSwap(s);

        RunDays(s.Yard, 60, () => ShipRefitJob.JobsOf(s.Yard).Any());

        Assert.That(ShipRefitJob.JobsOf(s.Yard), Is.Empty, "the refit completed with no one emptying the hold");
        Assert.That(ShipRefitJob.OnShip(s.Ship, s.Hold.ID), Is.False);
        Assert.That(ShipRefitJob.OnShip(s.Ship, s.Fresh.ID), Is.True);
        Assert.That(ShipRefitJob.OnShip(s.Ship, s.Other.ID), Is.False);
        long moved = ColonyHolds(s) - colonyBefore;
        Assert.That(moved, Is.GreaterThan(0), "the overflow went to the colony");
        Assert.That(moved, Is.LessThanOrEqualTo(s.OverflowUnits + 1), "only the overflow moved");
        Assert.That(s.Cargo.GetUnitsStored(s.Load, false), Is.EqualTo(s.Units - moved), "the rest stayed aboard");
        Assert.That(s.Cargo.TypeStores[s.TypeId].MaxVolume, Is.LessThan(s.CapacityBefore));
    }

    [TestCase("passengers", "Waiting for passengers to disembark from ")]
    [TestCase("cargo", "Waiting for cargo to be unloaded from ")]
    [TestCase("fuel", "Waiting for fuel to be unloaded from ")]
    public void RemovingAnOccupiedHold_ColonyFull_AppliesTheRest_WaitsForThatHold_ThenCompletes(string what, string reasonStart)
    {
        var s = OccupiedHoldScene(what);
        SetColonyRoom(s, 0);
        QueueSwap(s);

        RunDays(s.Yard, 60, () => ShipRefitJob.OnShip(s.Ship, s.Fresh.ID) == false);

        Assert.That(ShipRefitJob.OnShip(s.Ship, s.Fresh.ID), Is.True, "the mount was applied");
        Assert.That(ShipRefitJob.OnShip(s.Ship, s.Other.ID), Is.False, "the other removal was applied");
        Assert.That(ShipRefitJob.OnShip(s.Ship, s.Hold.ID), Is.True, "the occupied hold stays on");
        var job = s.Line.Jobs.Single();
        Assert.That(job.WaitReason, Is.EqualTo(reasonStart + s.Hold.Name));
        Assert.That(job.Status, Is.EqualTo(Pulsar4X.DataStructures.IndustryJobStatus.MissingResources));
        Assert.That(job.Refit!.Remove.Select(p => p.ID), Is.EqualTo(new[] { s.Hold.ID }));
        Assert.That(job.Refit.Mount, Is.Empty);
        Assert.That(s.Cargo.GetUnitsStored(s.Load, false), Is.EqualTo(s.Units), "nothing fitted in the colony");
        RunDays(s.Yard, 5, () => true);
        Assert.That(ShipRefitJob.OnShip(s.Ship, s.Hold.ID), Is.True, "still waiting");
        var view = new GameProjector(s.Game).ProjectEntity(s.Yard, s.Faction.Id).GetView<IndustryView>()!
            .ProductionLines.Single(l => l.Id == s.LineId).Jobs.Single();
        Assert.That(view.Refit!.WaitReason, Is.EqualTo(reasonStart + s.Hold.Name));

        // Empty the hold by hand: the job takes it off and completes.
        s.Cargo.RemoveCargoByUnit(s.Load, s.Units);
        RunDays(s.Yard, 5, () => ShipRefitJob.JobsOf(s.Yard).Any());

        Assert.That(ShipRefitJob.JobsOf(s.Yard), Is.Empty, "the job completed once the hold was empty");
        Assert.That(ShipRefitJob.OnShip(s.Ship, s.Hold.ID), Is.False);
        Assert.That(s.Cargo.TypeStores[s.TypeId].MaxVolume, Is.LessThan(s.CapacityBefore), "the hold took its volume with it");
        Assert.That(s.Colony.GetUnitsStored(s.Hold, false), Is.EqualTo(1), "the hold is in the warehouse");
    }

    [TestCase("passengers")]
    [TestCase("cargo")]
    [TestCase("fuel")]
    public void RemovingAnOccupiedHold_PartialRoom_UnloadsWhatFits_AndWaitsForTheRest(string what)
    {
        var s = OccupiedHoldScene(what);
        long fits = s.OverflowUnits / 2;
        Assert.That(fits, Is.GreaterThan(0));
        SetColonyRoom(s, fits);
        long colonyBefore = ColonyHolds(s);
        QueueSwap(s);

        RunDays(s.Yard, 60, () => ShipRefitJob.OnShip(s.Ship, s.Fresh.ID) == false);

        Assert.That(ShipRefitJob.OnShip(s.Ship, s.Hold.ID), Is.True, "the rest does not fit, so the hold waits");
        Assert.That(ColonyHolds(s) - colonyBefore, Is.EqualTo(fits), "the colony took what it had room for");
        Assert.That(s.Cargo.GetUnitsStored(s.Load, false), Is.EqualTo(s.Units - fits));
        var job = s.Line.Jobs.Single();
        Assert.That(job.WaitReason, Does.StartWith("Waiting for "));
        Assert.That(job.Refit!.Remove.Select(p => p.ID), Is.EqualTo(new[] { s.Hold.ID }));

        // The colony makes room: the next pass unloads the rest and finishes.
        SetColonyRoom(s, s.OverflowUnits * 2);
        RunDays(s.Yard, 5, () => ShipRefitJob.JobsOf(s.Yard).Any());
        Assert.That(ShipRefitJob.JobsOf(s.Yard), Is.Empty);
        Assert.That(ShipRefitJob.OnShip(s.Ship, s.Hold.ID), Is.False);
    }

    [Test]
    public void Basemod_NoComponentIsStoredAsTheTypeItHolds_ExceptGeneralStorage()
    {
        // A part's CargoTypeID is what the part itself is stored as while it is cargo; the type its storage
        // attribute holds is what it carries once mounted. They are the same only for general storage (a
        // warehouse or cargo hold is stored as general cargo and holds general cargo). A specialised hold
        // (passengers, fuel) stored as the type it holds could not sit in an ordinary warehouse.
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var offenders = new List<string>();
        int holds = 0;
        foreach (var file in System.IO.Directory.GetFiles(
                     System.IO.Path.Combine(System.IO.Path.GetDirectoryName(paths[0])!, "TemplateFiles"), "*.json", System.IO.SearchOption.AllDirectories))
        {
            var entries = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(file));
            foreach (var entry in entries)
            {
                if ((string?)entry["Type"] != "ComponentTemplate" || entry["Payload"] is not { } payload)
                    continue;
                string? cargoType = (string?)payload["CargoTypeID"];
                foreach (var property in payload["Properties"] ?? new Newtonsoft.Json.Linq.JArray())
                {
                    string attribute = (string?)property["AttributeType"] ?? "";
                    if (!attribute.EndsWith("StorageAtb"))
                        continue;
                    holds++;
                    string formula = (string?)property["PropertyFormula"] ?? "";
                    foreach (var type in new[] { "passenger-storage", "fuel-storage", "ordnance-storage" })
                    {
                        if (formula.Contains("'" + type + "'") && cargoType == type)
                            offenders.Add((string?)payload["UniqueID"] + " is stored as " + type);
                    }
                }
            }
        }
        Assert.That(holds, Is.GreaterThan(5), "the audit found the storage components");
        Assert.That(offenders, Is.Empty, string.Join("; ", offenders));
    }

    [TestCase(WarpTech)]
    [TestCase(EarlyTech)]
    public void MissingPart_WaitsForIt_SpendsNothing_ThenCompletesWhenItIsStored(string scenario)
    {
        var (game, faction) = Start(scenario);
        var (yard, lineId) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var line = yard.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId];
        var storage = yard.GetDataBlob<CargoStorageDB>();
        var quarters = AnyShipPart(info);
        var ship = NewShip(yard, faction, info.ShipDesigns.Values.First(d => d.MassPerUnit <= line.MaxVolume));
        // No industry head: with one seated the missing part is queued and built by itself (RefitPartsTests).
        if (GameEngine.People.IndustrySeat.HasHead(yard))
            GameEngine.People.IndustrySeat.Unseat(yard, yard.StarSysDateTime);

        var queued = new CommandTranslator(game).Translate(faction, yard, new QueueShipRefitCommand(
            yard.Id, lineId, ship.Id, Array.Empty<int>(), Array.Empty<int>(), null, new[] { quarters.UniqueID }));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);
        var job = line.Jobs.Single();
        long cost = job.ProductionPointsCost;
        Assert.That(job.WaitReason, Is.EqualTo("Waiting for " + quarters.Name));

        RunDays(yard, 5, () => true);

        Assert.That(job.ProductionPointsLeft, Is.EqualTo(cost), "no points spent while a part is missing");
        Assert.That(job.Status, Is.EqualTo(Pulsar4X.DataStructures.IndustryJobStatus.MissingResources));
        Assert.That(job.WaitReason, Is.EqualTo("Waiting for " + quarters.Name));
        var view = new GameProjector(game).ProjectEntity(yard, faction.Id).GetView<IndustryView>()!
            .ProductionLines.Single(l => l.Id == lineId).Jobs.Single();
        Assert.That(view.Refit!.WaitReason, Is.EqualTo("Waiting for " + quarters.Name));
        Assert.That(view.Refit.WaitingFor, Is.EqualTo(new[] { quarters.Name }));

        var arrived = new ComponentInstance(quarters);
        Assert.That(storage.AddCargoByUnit(arrived, 1), Is.EqualTo(1));
        RunDays(yard, 60, () => ShipRefitJob.JobsOf(yard).Any());

        Assert.That(ShipRefitJob.JobsOf(yard), Is.Empty, "it completed once the part was stored");
        Assert.That(ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Any(c => c.Design.UniqueID == quarters.UniqueID), Is.True);
        Assert.That(storage.GetUnitsStored(arrived, false), Is.EqualTo(0));
    }

        static ComponentDesign AnyShipPart(FactionInfoDB info)
        => info.ComponentDesigns.Values.FirstOrDefault(d => d.Name.StartsWith("Passenger Quarters")
                && d.ComponentMountType.HasFlag(Pulsar4X.DataStructures.ComponentMountType.ShipComponent))
            ?? info.ComponentDesigns.Values.First(d => d.IsValid && ShipRefitJob.IsShipPart(d)
                && !LocalConstructionJob.IsSiteOnly(d));

static ComponentDesign PassengerQuarters(FactionInfoDB info)
        => info.ComponentDesigns.Values.First(d => d.Name.StartsWith("Passenger Quarters")
            && d.ComponentMountType.HasFlag(Pulsar4X.DataStructures.ComponentMountType.ShipComponent));

    static void RunDays(Entity yard, int max, Func<bool> keepGoing)
    {
        for (int day = 0; day < max && keepGoing(); day++)
            new IndustryProcessor().ProcessEntity(yard, 0);
    }

    [TestCase(WarpTech)]
    [TestCase(EarlyTech)]
    public void RemovingABridge_DropsItsSeat_AndDoesNotThrow(string scenario)
    {
        var (game, faction) = Start(scenario);
        var (yard, _) = FindYard(game, faction);
        var info = faction.GetDataBlob<FactionInfoDB>();
        var design = info.ShipDesigns.Values.First(d => d.Components.Any(c => c.Item1.HasAttribute<GameEngine.People.AdminSpaceAtb>()));
        var ship = NewShip(yard, faction, design);
        var seats = ship.GetDataBlob<GameEngine.People.AdminSpaceDB>().CommanderSeats;
        int before = seats.Count;
        Assert.That(before, Is.GreaterThan(0));
        var bridge = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values
            .First(c => c.Design.HasAttribute<GameEngine.People.AdminSpaceAtb>());

        Assert.DoesNotThrow(() => ship.RemoveComponent(bridge));

        Assert.That(ship.GetDataBlob<GameEngine.People.AdminSpaceDB>().CommanderSeats, Has.Count.EqualTo(before - 1));
    }

    static (Game game, Entity faction) Start(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), scenario);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345));
        Assert.That(created, Is.Not.Null);
        var game = created!.Value.game;
        game.Settings.EnforceSingleThread = true;
        return (game, created.Value.faction);
    }

    static (Entity yard, string lineId) FindYard(Game game, Entity faction)
    {
        foreach (var system in game.Systems)
        {
            foreach (var entity in system.GetAllEntitiesWithDataBlob<IndustryAbilityDB>())
            {
                if (entity.FactionOwnerID != faction.Id || !entity.HasDataBlob<LocalConstructionDB>())
                    continue;
                foreach (var (id, line) in entity.GetDataBlob<IndustryAbilityDB>().ProductionLines)
                {
                    if (IndustryTools.IsShipyardLine(line))
                        return (entity, id);
                }
            }
        }
        Assert.Fail("no player shipyard");
        throw new InvalidOperationException();
    }

    /// <summary>Two ship designs that fit the slip and differ by ship parts. Storage type does not matter to a refit.</summary>
    static (ShipDesign from, ShipDesign to) PickPair(Entity yard, FactionInfoDB info, IndustryAbilityDB.ProductionLine line)
    {
        var designs = info.ShipDesigns.Values.Where(d => d.MassPerUnit <= line.MaxVolume).ToList();
        foreach (var from in designs)
        {
            foreach (var to in designs)
            {
                if (from.UniqueID == to.UniqueID)
                    continue;
                var a = from.Components.GroupBy(c => c.Item1.UniqueID).ToDictionary(g => g.Key, g => g.Sum(c => c.Item2));
                var b = to.Components.GroupBy(c => c.Item1.UniqueID).ToDictionary(g => g.Key, g => g.Sum(c => c.Item2));
                if (a.Count == b.Count && a.All(p => b.GetValueOrDefault(p.Key) == p.Value))
                    continue;
                var moved = from.Components.Concat(to.Components)
                    .Where(c => a.GetValueOrDefault(c.Item1.UniqueID) != b.GetValueOrDefault(c.Item1.UniqueID))
                    .Select(c => c.Item1);
                if (moved.All(ShipRefitJob.IsShipPart))
                    return (from, to);
            }
        }
        Assert.Fail("no two ship designs that differ by parts");
        throw new InvalidOperationException();
    }

    static Entity NewShip(Entity yard, Entity faction, ShipDesign design)
    {
        var planet = yard.GetDataBlob<ColonyInfoDB>().PlanetEntity;
        var ship = ShipFactory.CreateShip(design, faction, planet, "Refit Subject");
        if (!ship.HasDataBlob<CargoStorageDB>())
            ship.SetDataBlob(new CargoStorageDB());
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        yard.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        return ship;
    }

    /// <summary>Parts to take off the ship and fresh parts (put in the warehouse) to mount, to turn it into <paramref name="to"/>.</summary>
    static (List<ComponentInstance> remove, List<ComponentInstance> mount) Diff(Entity yard, Entity ship, ShipDesign to)
    {
        var onShip = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.ToList();
        var remove = new List<ComponentInstance>();
        var mount = new List<ComponentInstance>();
        var wanted = new Dictionary<string, (ComponentDesign Design, int Count)>();
        foreach (var (part, count) in to.Components)
            wanted[part.UniqueID] = wanted.TryGetValue(part.UniqueID, out var have) ? (part, have.Count + count) : (part, count);
        foreach (var group in onShip.GroupBy(c => c.Design.UniqueID))
        {
            int need = wanted.TryGetValue(group.Key, out var w) ? w.Count : 0;
            remove.AddRange(group.Skip(need));
        }
        var storage = yard.GetDataBlob<CargoStorageDB>();
        foreach (var (id, (design, count)) in wanted)
        {
            int have = onShip.Count(c => c.Design.UniqueID == id);
            for (int i = have; i < count; i++)
            {
                var fresh = new ComponentInstance(design);
                Assert.That(storage.AddCargoByUnit(fresh, 1), Is.EqualTo(1),
                    $"{design.Name} ({design.CargoTypeID}, vol {design.VolumePerUnit}); yard stores {string.Join(",", storage.TypeStores.Select(t => t.Key + ":" + t.Value.FreeVolume))}");
                mount.Add(fresh);
            }
        }
        return (remove, mount);
    }

    static void StockResources(Entity yard, IndustryAbilityDB.ProductionLine line, FactionInfoDB info)
    {
        var storage = yard.GetDataBlob<CargoStorageDB>();
        foreach (var job in line.Jobs.Where(j => j.Refit == null))
        {
            foreach (var (id, amount) in job.ResourcesCosts)
            {
                var item = info.Data.CargoGoods.GetAny(id);
                Assert.That(item, Is.Not.Null, id);
                storage.AddCargoByUnit(item!, amount * 2);
            }
        }
    }
}
