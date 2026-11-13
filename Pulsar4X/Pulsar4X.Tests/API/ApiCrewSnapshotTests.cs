using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests.API;

/// <summary>
/// Crew plan phases 1-3, step A: the crew fields the UI binds to. Required crew is real; berths,
/// crew aboard, the pool, crew courses and launch warnings hold their defaults until the engine
/// phases fill them in.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ApiCrewSnapshotTests
{
    Game _game = null!;
    Entity _player = null!;
    GameProjector _projector = null!;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), "Pulsar4x.Scenario.WarpTechStart");
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345))!.Value;
        _game = created.game;
        _player = created.faction;
        _projector = new GameProjector(_game);
    }

    [Test]
    public void Ships_CarryRequiredCrew_AndStartFullyCrewed()
    {
        var ships = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            .Where(s => s.FactionOwnerID == _player.Id).ToList();
        Assert.That(ships, Is.Not.Empty);
        foreach (var ship in ships)
        {
            var design = ship.GetDataBlob<ShipInfoDB>().Design;
            var view = _projector.ProjectEntity(ship, _player.Id).GetView<ShipView>()!;
            Assert.That(view.CrewRequired, Is.EqualTo(design.CrewReq), design.Name);
            Assert.That(view.CrewBerths, Is.EqualTo(design.CrewBerths).And.GreaterThanOrEqualTo(design.CrewReq), design.Name);
            Assert.That(view.BerthsByComponent, Is.Not.Empty, "a bridge or crew quarters");
            Assert.That(view.BerthsByComponent.Sum(r => r.Berths), Is.EqualTo(view.CrewBerths));
            Assert.That(view.CrewTracked, Is.True, "crew plan phase 3 tracks crew aboard");
            Assert.That(view.CrewAboard, Is.EqualTo(design.CrewReq), "scenario ships start fully crewed");
            Assert.That(view.CrewFactor, Is.EqualTo(1));
            Assert.That(view.EngineCrewFactor, Is.EqualTo(1));
            Assert.That(view.UnderCrewed, Is.False);
            Assert.That(view.CrewNote, Is.Empty);
        }

        var (fleets, loose) = _projector.ProjectFleetHierarchy(_player.Id);
        var snapshots = loose.ToList();
        Assert.That(snapshots.Concat(fleets.SelectMany(f => f.Ships)).Any(s => s.CrewRequired > 0), Is.True);
        foreach (var snapshot in snapshots.Concat(fleets.SelectMany(f => f.Ships)))
        {
            var design = ships.Single(s => s.Id == snapshot.Id).GetDataBlob<ShipInfoDB>().Design;
            Assert.That(snapshot.CrewRequired, Is.EqualTo(design.CrewReq), snapshot.Name);
            Assert.That(snapshot.CrewBerths, Is.EqualTo(design.CrewBerths));
            Assert.That(snapshot.CrewTracked, Is.True);
            Assert.That(snapshot.CrewAboard, Is.EqualTo(design.CrewReq));
            Assert.That(snapshot.UnderCrewed, Is.False);
        }
    }

    [Test]
    public void Colony_PoolAcademyAndBuildViews_HaveTheCrewFields()
    {
        var earth = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .First(c => c.FactionOwnerID == _player.Id && c.HasDataBlob<Pulsar4X.Industry.IndustryAbilityDB>());
        Assert.That(earth.HasDataBlob<Pulsar4X.People.NavalAcademyDB>(), Is.True, "crew plan phase 2 mounts the Earth academy");
        var projected = _projector.ProjectEntity(earth, _player.Id);

        var colony = projected.GetView<ColonyView>()!;
        Assert.That(colony.CrewPool, Is.EqualTo(500), "the seeded Earth pool");
        Assert.That(colony.CrewPoolBySpecies.Single().Trained, Is.EqualTo(500));

        var academy = projected.GetView<NavalAcademyView>()!.Academies.First();
        Assert.That(academy.CrewSeats, Is.EqualTo(200));
        Assert.That(academy.CrewSeatsInUse, Is.EqualTo(0));
        Assert.That(academy.CrewCourseLengthMonths, Is.EqualTo(3));
        Assert.That(academy.NextCrewGraduation, Is.Null);
        Assert.That(academy.CrewCourseReason, Is.Empty, "a crew course can start");
        Assert.That((int)AcademyCourseKind.Crew, Is.EqualTo(3), "after Idle, Class and Command, like the engine's planned value");

        var industry = projected.GetView<IndustryView>()!;
        var designs = _player.GetDataBlob<FactionInfoDB>().ShipDesigns;
        var shipRows = industry.ProductionLines.SelectMany(l => l.Constructibles).Where(c => designs.ContainsKey(c.DesignId)).ToList();
        Assert.That(shipRows, Is.Not.Empty, "a shipyard line lists ship designs");
        foreach (var row in shipRows)
        {
            Assert.That(row.CrewRequired, Is.EqualTo(designs[row.DesignId].CrewReq), row.Name);
            Assert.That(row.CrewBerths, Is.EqualTo(designs[row.DesignId].CrewBerths).And.GreaterThanOrEqualTo(row.CrewRequired));
            Assert.That(row.CrewWarning, Is.Empty, "every stock design sleeps its crew");
        }
        foreach (var pad in industry.Pads.Where(p => p.Free))
        {
            Assert.That(pad.CrewRequired, Is.EqualTo(0), "a free pad needs nobody");
            Assert.That(pad.CrewToBoard, Is.EqualTo(0));
            Assert.That(pad.CrewShort, Is.False);
            Assert.That(pad.CrewWarning, Is.Empty);
        }
    }

    [Test]
    public void ComponentDesigns_CarryCrewPerCopy()
    {
        var snapshot = _projector.ProjectComponentDesigns(_player.Id)!;
        var info = _player.GetDataBlob<FactionInfoDB>();
        var laser = snapshot.Designs.Single(d => d.Id == "default-design-laser-weapon");
        Assert.That(laser.CrewRequired, Is.EqualTo(info.InternalComponentDesigns["default-design-laser-weapon"].CrewReq).And.EqualTo(5));
        Assert.That(laser.Berths, Is.EqualTo(0));
        Assert.That(snapshot.Designs.Single(d => d.Id == "default-design-crew-quarters-10").Berths, Is.EqualTo(10));
        Assert.That(snapshot.Designs.Single(d => d.Id == "default-design-Cockpit").Berths, Is.EqualTo(2));
    }
}
