using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Names;
using Pulsar4X.People;
using EngineCourse = Pulsar4X.People.AcademyCourseKind;

namespace Pulsar4X.Tests;

/// <summary>
/// Crew plan phase 2: the academy's crew course (200 seats, 3 months, one colonist per trainee),
/// the colony crew pool it fills, the scenario seeds and the Earth academy.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CrewAcademyTests
{
    Game _game = null!;
    Entity _player = null!;
    Entity _earth = null!;

    static (Game game, Entity faction) Start(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), scenario);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345))!.Value;
        return (created.game, created.faction);
    }

    static Entity HomeColony(Game game, Entity faction)
        => game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .First(c => c.FactionOwnerID == faction.Id && c.HasDataBlob<NavalAcademyDB>());

    [SetUp]
    public void SetUp()
    {
        (_game, _player) = Start("Pulsar4x.Scenario.WarpTechStart");
        _earth = HomeColony(_game, _player);
    }

    NavalAcademy Academy => _earth.GetDataBlob<NavalAcademyDB>().Academies[0];
    long People => NavalAcademyCourse.PopulationOf(_earth.GetDataBlob<ColonyInfoDB>());
    long Pool => _earth.TryGetDataBlob<ColonyCrewPoolDB>(out var pool) ? pool.Total : 0;

    [TestCase("Pulsar4x.Scenario.WarpTechStart")]
    [TestCase("Pulsar4x.Scenario.EarlyTech")]
    public void EveryStart_HasAnEarthAcademyWithACrewCourse_AndASeededPool(string scenario)
    {
        var (game, faction) = Start(scenario);
        var earth = HomeColony(game, faction);
        var academy = earth.GetDataBlob<NavalAcademyDB>().Academies.Single();
        Assert.That(academy.CrewSeats, Is.EqualTo(200));
        Assert.That(academy.CrewCourseMonths, Is.EqualTo(3));
        Assert.That(academy.ClassSize, Is.EqualTo(2), "the officer class is unchanged");

        var pool = earth.GetDataBlob<ColonyCrewPoolDB>();
        int human = earth.GetDataBlob<ColonyInfoDB>().Population.Keys.Single();
        Assert.That(pool.Trained.Keys, Is.EquivalentTo(new[] { human }), "seeded as Earth's own species");
        Assert.That(pool.Total, Is.EqualTo(500));
    }

    [Test]
    public void Seeds_Luna50_MarsAndCeresNone()
    {
        long PoolAt(string body)
        {
            var colony = _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
                .Single(c => c.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<NameDB>().DefaultName == body);
            return colony.TryGetDataBlob<ColonyCrewPoolDB>(out var pool) ? pool.Total : 0;
        }
        Assert.That(PoolAt("Earth"), Is.EqualTo(500));
        Assert.That(PoolAt("Luna"), Is.EqualTo(50));
        Assert.That(PoolAt("Mars"), Is.EqualTo(0));
        Assert.That(PoolAt("Ceres"), Is.EqualTo(0));
    }

    [Test]
    public void Enrol_TakesOneColonistPerTrainee_AndUsesCrewSeatsNotOfficerSeats()
    {
        long before = People;
        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, Academy.ComponentId, 150), Is.Null);
        Assert.That(People, Is.EqualTo(before - 150));
        Assert.That(NavalAcademyCourse.CrewSeatsUsed(Academy), Is.EqualTo(150));
        Assert.That(NavalAcademyCourse.SeatsUsed(Academy), Is.EqualTo(0), "officer seats stay free");
        var cohort = Academy.Cohorts.Single();
        Assert.That(cohort.Kind, Is.EqualTo(EngineCourse.Crew));
        Assert.That(cohort.GraduationDate, Is.EqualTo(_earth.StarSysDateTime.AddDays(90)));
        Assert.That(Pool, Is.EqualTo(500), "trainees join the pool only when they graduate");

        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, Academy.ComponentId, 51), Is.EqualTo("This crew course needs 51 seats. 50 are free."));
        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, Academy.ComponentId, 0), Is.EqualTo("Enrol at least one trainee."));
        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, 999999, 10), Is.EqualTo("No such academy."));
        Assert.That(NavalAcademyCourse.TryTrain(_earth, Academy.ComponentId, 2), Is.Null, "the officer class still fills its own seats");
    }

    [Test]
    public void Enrol_IsRefusedWhenTheColonyIsShortOfPeople()
    {
        var info = _earth.GetDataBlob<ColonyInfoDB>();
        int human = info.Population.Keys.Single();
        info.Population[human] = 20;
        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, Academy.ComponentId, 30), Is.EqualTo("The colony has 20 people. This crew course needs 30."));
        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, Academy.ComponentId, 20, human), Is.Null);
        Assert.That(info.Population.ContainsKey(human), Is.False, "all 20 went to class");
    }

    [Test]
    public void Graduation_AddsTheCohortToThePool_AndFiresAnEvent()
    {
        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, Academy.ComponentId, 120), Is.Null);
        var grad = Academy.Cohorts.Single().GraduationDate;
        NavalAcademyCourse.Graduate(_earth, grad.AddDays(-1));
        Assert.That(Pool, Is.EqualTo(500), "not yet");
        NavalAcademyCourse.Graduate(_earth, grad);
        Assert.That(Pool, Is.EqualTo(620));
        Assert.That(Academy.Cohorts, Is.Empty);
        Assert.That(NavalAcademyCourse.CrewSeatsUsed(Academy), Is.EqualTo(0));
    }

    [Test]
    public void Graduation_RunsFromTheAcademyProcessor()
    {
        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, Academy.ComponentId, 10), Is.Null);
        var grad = Academy.Cohorts.Single().GraduationDate;
        _game.Settings.EnforceSingleThread = true;
        _earth.Manager!.ManagerSubpulses.ProcessSystem(grad.AddHours(1));
        Assert.That(Pool, Is.EqualTo(510));
    }

    [Test]
    public void Cancel_ReturnsTheTraineesToThePopulation()
    {
        long before = People;
        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, Academy.ComponentId, 40), Is.Null);
        int cohort = Academy.Cohorts.Single().CohortId;
        Assert.That(NavalAcademyCourse.TryCancel(_earth, Academy.ComponentId, cohort), Is.Null);
        Assert.That(People, Is.EqualTo(before));
        Assert.That(Pool, Is.EqualTo(500));
    }

    [Test]
    public void Command_TrainsCrew_AndTheViewsShowThePoolAndTheCourse()
    {
        var translator = new CommandTranslator(_game);
        var result = translator.Translate(_player, _earth, new TrainAcademyCrewCommand(_earth.Id, Academy.ComponentId, 100));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var refused = translator.Translate(_player, _earth, new TrainAcademyCrewCommand(_earth.Id, Academy.ComponentId, 101));
        Assert.That(refused.Accepted, Is.False);
        Assert.That(refused.RejectionReason, Is.EqualTo("This crew course needs 101 seats. 100 are free."));

        var projected = new GameProjector(_game).ProjectEntity(_earth, _player.Id);
        var colony = projected.GetView<ColonyView>()!;
        Assert.That(colony.CrewPool, Is.EqualTo(500));
        Assert.That(colony.CrewPoolBySpecies.Single().Trained, Is.EqualTo(500));
        var academy = projected.GetView<NavalAcademyView>()!.Academies.Single();
        Assert.That(academy.CrewSeats, Is.EqualTo(200));
        Assert.That(academy.CrewSeatsInUse, Is.EqualTo(100));
        Assert.That(academy.CrewCourseLengthMonths, Is.EqualTo(3));
        Assert.That(academy.NextCrewGraduation, Is.EqualTo(_earth.StarSysDateTime.AddDays(90)));
        Assert.That(academy.NextCrewGraduationCount, Is.EqualTo(100));
        Assert.That(academy.CrewCourseReason, Is.Empty, "100 seats are still free");
        Assert.That(academy.SeatsInUse, Is.EqualTo(0));
        Assert.That(academy.Cohorts.Single().Course, Is.EqualTo(Pulsar4X.Api.AcademyCourseKind.Crew));

        // The cancel command reuses CancelAcademyCourseCommand.
        var cancel = translator.Translate(_player, _earth, new CancelAcademyCourseCommand(_earth.Id, Academy.ComponentId, Academy.Cohorts.Single().CohortId));
        Assert.That(cancel.Accepted, Is.True, cancel.RejectionReason);
        Assert.That(Academy.Cohorts, Is.Empty);
    }

    [Test]
    public void SaveRoundTrip_KeepsThePoolTheCrewCohortAndTheCrewSeats()
    {
        Assert.That(NavalAcademyCourse.TryTrainCrew(_earth, Academy.ComponentId, 75), Is.Null);
        int earthId = _earth.Id;
        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        Assert.That(loaded.GlobalManager.TryGetGlobalEntityById(earthId, out var earth), Is.True);
        Assert.That(earth.GetDataBlob<ColonyCrewPoolDB>().Total, Is.EqualTo(500));
        var row = earth.GetDataBlob<NavalAcademyDB>().Academies.Single();
        Assert.That(row.CrewSeats, Is.EqualTo(200));
        Assert.That(row.CrewCourseMonths, Is.EqualTo(3));
        var cohort = row.Cohorts.Single();
        Assert.That(cohort.Kind, Is.EqualTo(EngineCourse.Crew));
        Assert.That(cohort.Count, Is.EqualTo(75));

        NavalAcademyCourse.Graduate(earth, cohort.GraduationDate);
        Assert.That(earth.GetDataBlob<ColonyCrewPoolDB>().Total, Is.EqualTo(575));
    }

    [Test]
    public void LegacyAcademyRow_GetsTheStockCrewCourse()
    {
        var row = new NavalAcademy { ClassSize = 2, TrainingPeriodInMonths = 12 };
        NavalAcademyCourse.FoldLegacy(ref row);
        Assert.That(row.CrewSeats, Is.EqualTo(NavalAcademyAtb.DefaultCrewSeats));
        Assert.That(row.CrewCourseMonths, Is.EqualTo(NavalAcademyAtb.DefaultCrewCourseMonths));
    }
}
