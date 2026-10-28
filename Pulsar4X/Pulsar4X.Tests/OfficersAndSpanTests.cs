using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.JumpPoints;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using AcademyCourseKind = Pulsar4X.People.AcademyCourseKind;

namespace Pulsar4X.Tests;

[TestFixture]
public class OfficersAndSpanTests
{
    static readonly Game Game = InitializeGame();

    static Game InitializeGame()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 4, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        return game;
    }

    [SetUp]
    public void QuietSystems()
    {
        foreach (var sys in Game.Systems)
            sys.SetActivityState(SystemActivityState.Stasis);
    }

    [Test]
    public void Mount_SchedulesNothing_AndTwoAcademiesAreTwoRows()
    {
        var colony = Colony(2000, out var faction, out _);
        int before = faction.GetDataBlob<FactionInfoDB>().Commanders.Count;
        int first = Mount(colony);
        int second = Mount(colony);

        var academies = colony.GetDataBlob<NavalAcademyDB>().Academies;
        Assert.That(academies, Has.Count.EqualTo(2));
        Assert.That(academies[0].ComponentId, Is.EqualTo(first));
        Assert.That(academies[1].ComponentId, Is.EqualTo(second));
        Assert.That(academies[0].CourseKind, Is.EqualTo(AcademyCourseKind.Idle));
        Assert.That(academies[1].CourseKind, Is.EqualTo(AcademyCourseKind.Idle));
        Assert.That(faction.GetDataBlob<FactionInfoDB>().Commanders, Has.Count.EqualTo(before));
        Assert.That(AcademyInterrupt(colony), Is.False);
    }

    [Test]
    public void Class_TakesTwoPeople_Refuses1999_CancelReturnsThem_BusyRejects()
    {
        var colony = Colony(2000, out var faction, out var species);
        int id = Mount(colony);

        colony.GetDataBlob<ColonyInfoDB>().Population[species.Id] = 1999;
        var refused = NavalAcademyCourse.TryTrain(colony, id);
        Assert.That(refused, Is.EqualTo("The colony has 1999 people. This class needs 2000."));
        Assert.That(colony.GetDataBlob<ColonyInfoDB>().Population[species.Id], Is.EqualTo(1999));
        Assert.That(Row(colony).CourseKind, Is.EqualTo(AcademyCourseKind.Idle));

        colony.GetDataBlob<ColonyInfoDB>().Population[species.Id] = 2000;
        Assert.That(NavalAcademyCourse.TryTrain(colony, id), Is.Null);
        Assert.That(colony.GetDataBlob<ColonyInfoDB>().Population[species.Id], Is.EqualTo(1998));
        Assert.That(Row(colony).CourseKind, Is.EqualTo(AcademyCourseKind.Class));
        Assert.That(Row(colony).PopulationTaken, Is.EqualTo(2));
        Assert.That(AcademyInterrupt(colony), Is.True);

        Assert.That(NavalAcademyCourse.TryTrain(colony, id), Is.EqualTo("This academy is already running a course."));
        Assert.That(colony.GetDataBlob<ColonyInfoDB>().Population[species.Id], Is.EqualTo(1998));

        Assert.That(NavalAcademyCourse.TryCancel(colony, id), Is.Null);
        Assert.That(colony.GetDataBlob<ColonyInfoDB>().Population[species.Id], Is.EqualTo(2000));
        Assert.That(Row(colony).CourseKind, Is.EqualTo(AcademyCourseKind.Idle));
        Assert.That(Row(colony).PopulationTaken, Is.EqualTo(0));
        Assert.That(faction.GetDataBlob<FactionInfoDB>().Commanders, Is.Empty);

        Assert.That(NavalAcademyCourse.TryTrain(colony, id), Is.Null);
        var when = Row(colony).GraduationDate;
        NavalAcademyCourse.Graduate(colony, when);

        Assert.That(Row(colony).CourseKind, Is.EqualTo(AcademyCourseKind.Idle));
        var graduates = faction.GetDataBlob<FactionInfoDB>().Commanders;
        Assert.That(graduates, Has.Count.EqualTo(2));
        foreach (var person in graduates)
        {
            var commander = person.GetDataBlob<CommanderDB>();
            Assert.That(commander.Rank, Is.EqualTo(1));
            Assert.That(commander.Type, Is.EqualTo(CommanderTypes.Navy));
            Assert.That(commander.AssignedTo, Is.EqualTo(-1));
            Assert.That(commander.CommissionedOn, Is.EqualTo(when));
            Assert.That(commander.RankedOn, Is.EqualTo(when));
            Assert.That(NavalAcademyCourse.IsSeated(person), Is.False);
        }
    }

    [Test]
    public void CommandCourse_SeatedRefused_PromotesOneStep_RaisesCapBeforeRank()
    {
        var colony = Colony(2000, out var faction, out _);
        int id = Mount(colony);
        int mean = NavalAcademyCourse.SchoolMean(12, 0);
        Assert.That(mean, Is.EqualTo(88));

        var seated = Officer(colony, faction, experience: 8, cap: mean, rank: 1);
        var ship = Ship(colony.Manager, faction);
        seated.GetDataBlob<CommanderDB>().AssignedTo = ship.Id;
        ship.GetDataBlob<ShipInfoDB>().CommanderID = seated.Id;
        Assert.That(NavalAcademyCourse.TryCommand(colony, id, seated), Is.EqualTo("This officer is assigned."));
        seated.GetDataBlob<CommanderDB>().AssignedTo = -1;
        ship.GetDataBlob<ShipInfoDB>().CommanderID = -1;

        var ensign = Officer(colony, faction, experience: 8, cap: mean, rank: 1);
        Assert.That(NavalAcademyCourse.TryCommand(colony, id, ensign), Is.Null);
        var translator = new CommandTranslator(Game);
        var blocked = translator.Translate(faction, ship, new AssignCaptainCommand(ship.Id, ensign.Id));
        Assert.That(blocked.Accepted, Is.False);
        Assert.That(blocked.RejectionReason, Is.EqualTo("This officer is already on a course."));

        var firstDate = Row(colony).GraduationDate;
        NavalAcademyCourse.Graduate(colony, firstDate);
        var promoted = ensign.GetDataBlob<CommanderDB>();
        Assert.That(promoted.Rank, Is.EqualTo(4));
        Assert.That(promoted.Experience, Is.EqualTo(8));
        Assert.That(promoted.RankedOn, Is.EqualTo(firstDate));
        Assert.That(promoted.AssignedTo, Is.EqualTo(-1));
        Assert.That(Row(colony).CourseKind, Is.EqualTo(AcademyCourseKind.Idle));

        var seatedAgain = translator.Translate(faction, ship, new AssignCaptainCommand(ship.Id, ensign.Id));
        Assert.That(seatedAgain.Accepted, Is.True, seatedAgain.RejectionReason);
        promoted.AssignedTo = -1;
        ship.GetDataBlob<ShipInfoDB>().CommanderID = -1;

        promoted.Experience = 16;
        Assert.That(NavalAcademyCourse.TryCommand(colony, id, ensign), Is.Null);
        var secondDate = Row(colony).GraduationDate;
        NavalAcademyCourse.Graduate(colony, secondDate);
        Assert.That(promoted.Rank, Is.EqualTo(6));
        Assert.That(promoted.Experience, Is.EqualTo(16));

        var skip = Officer(colony, faction, experience: 16, cap: mean, rank: 1);
        Assert.That(NavalAcademyCourse.TryCommand(colony, id, skip), Is.Null);
        NavalAcademyCourse.Graduate(colony, Row(colony).GraduationDate);
        Assert.That(skip.GetDataBlob<CommanderDB>().Rank, Is.EqualTo(4));
        Assert.That(skip.GetDataBlob<CommanderDB>().Experience, Is.EqualTo(16));

        var field = Officer(colony, faction, experience: 8, cap: 40, rank: 1);
        Assert.That(NavalAcademyCourse.TryCommand(colony, id, field), Is.Null);
        NavalAcademyCourse.Graduate(colony, Row(colony).GraduationDate);
        var raised = field.GetDataBlob<CommanderDB>();
        Assert.That(raised.Rank, Is.EqualTo(1));
        Assert.That(raised.Experience, Is.EqualTo(8));
        Assert.That(raised.ExperienceCap, Is.EqualTo(mean));

        Assert.That(NavalAcademyCourse.TryCommand(colony, id, field), Is.Null);
        NavalAcademyCourse.Graduate(colony, Row(colony).GraduationDate);
        Assert.That(raised.Rank, Is.EqualTo(4));
        Assert.That(raised.Experience, Is.EqualTo(8));
    }

    [Test]
    public void Quality_Cap40Is1_4_Cap100Stays2()
    {
        var low = new CommanderDB { ExperienceCap = 40 };
        CommanderSkills.Grant(low, SkillDomain.Nav, 50);
        Assert.That(CommanderSkills.Get(low, SkillDomain.Nav), Is.EqualTo(40));
        Assert.That(CommanderSkills.Quality(low, SkillDomain.Nav), Is.EqualTo(1.4f));

        var full = new CommanderDB { ExperienceCap = 100 };
        CommanderSkills.Grant(full, SkillDomain.Command, 100);
        Assert.That(CommanderSkills.Get(full, SkillDomain.Command), Is.EqualTo(100));
        Assert.That(CommanderSkills.Quality(full, SkillDomain.Command), Is.EqualTo(2f));
    }

    [Test]
    public void Span_ClipsSurveyAndHaul_EmptyChairRejectsMoveGoods_PlotIsNotSpanned()
    {
        var world = HaulWorld();
        var ship = world.Ship;
        var officer = ship.GetDataBlob<ShipInfoDB>();

        SetBridge(ship, AdminLevel.Ship);
        SetRank(ship, 6);
        Assert.That(CommandSpan.Effective(ship), Is.EqualTo(CommandSpanKind.Body));
        AssertSurvey(ship, world.Earth, world.Earth.Id);
        Assert.That(Haul(ship, world.EarthOffice, world.LunaOffice).Accepted, Is.True);
        var mars = Haul(ship, world.EarthOffice, world.MarsOffice);
        Assert.That(mars.Accepted, Is.False);
        Assert.That(mars.RejectionReason, Is.EqualTo("The target is outside this ship's reach."));

        SetBridge(ship, AdminLevel.System);
        SetRank(ship, 1);
        Assert.That(CommandSpan.Effective(ship), Is.EqualTo(CommandSpanKind.Body));
        AssertSurvey(ship, world.Earth, world.Earth.Id);
        Assert.That(Haul(ship, world.EarthOffice, world.LunaOffice).Accepted, Is.True);
        Assert.That(Haul(ship, world.EarthOffice, world.MarsOffice).Accepted, Is.False);

        SetRank(ship, 6);
        Assert.That(CommandSpan.Effective(ship), Is.EqualTo(CommandSpanKind.System));
        AssertSurvey(ship, world.Star, world.Star.Id, world.Earth.Id, world.Luna.Id, world.Mars.Id);
        var jump = Haul(ship, world.EarthOffice, world.NeighborOffice);
        Assert.That(jump.Accepted, Is.True, jump.RejectionReason);

        SetBridge(ship, AdminLevel.Planet);
        SetRank(ship, 4);
        Assert.That(CommandSpan.Effective(ship), Is.EqualTo(CommandSpanKind.Well));
        var well = CommandSpan.Expand(world.Earth, CommandSpan.Effective(ship), _ => true);
        Assert.That(well.Select(body => body.Id), Does.Contain(world.Earth.Id));
        Assert.That(well.Select(body => body.Id), Does.Contain(world.Luna.Id));
        Assert.That(well.Select(body => body.Id), Does.Not.Contain(world.Mars.Id));
        var inSystem = Haul(ship, world.EarthOffice, world.MarsOffice);
        Assert.That(inSystem.Accepted, Is.True, inSystem.RejectionReason);
        Assert.That(Haul(ship, world.EarthOffice, world.NeighborOffice).Accepted, Is.False);

        officer.CommanderID = -1;
        var moveGoods = new CommandTranslator(Game).Translate(
            world.Faction,
            ship,
            new MoveGoodsCommand(ship.Id, world.EarthOffice.Id, world.LunaOffice.Id, new List<CargoTransferItem>()));
        Assert.That(moveGoods.Accepted, Is.False);
        Assert.That(moveGoods.RejectionReason, Is.EqualTo("This ship has no captain. Plot an action instead."));

        var plotted = new CommandTranslator(Game).Translate(
            world.Faction,
            ship,
            new GoToBodyCommand(ship.Id, world.Mars.Id));
        if (!plotted.Accepted)
        {
            Assert.That(plotted.RejectionReason.IndexOf("reach", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
            Assert.That(plotted.RejectionReason.IndexOf("span", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
        }
    }

    [Test]
    public void NewGame_UnlocksTheAcademy_BridgesStayBodyWellSystem()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);

        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        var sol = game.Systems.Single(system => system.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(sol, store.Species["species-human"]);
        species.FactionOwnerID = player.Id;
        var earth = NameLookup.GetFirstEntityWithName(sol, "Earth");
        var colony = ColonyFactory.CreateFromBlueprint(
            game, player, species, sol, earth, store.Colonies["colony-earth"]);

        var info = player.GetDataBlob<FactionInfoDB>();
        Assert.That(info.Data.ComponentTemplates.ContainsKey("naval-academy"), Is.True);
        var academy = info.ComponentDesigns["default-design-naval-academy"];
        var atb = academy.GetAttribute<NavalAcademyAtb>();
        Assert.That(atb.ClassSize, Is.EqualTo(2));
        Assert.That(atb.TrainingPeriodInMonths, Is.EqualTo(12));
        Assert.That(academy.MassPerUnit, Is.EqualTo(90000));
        Assert.That(academy.CrewReq, Is.EqualTo(42));
        Assert.That(academy.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation), Is.True);
        Assert.That(academy.ComponentMountType.HasFlag(ComponentMountType.ShipCargo), Is.False);
        Assert.That(colony.HasDataBlob<NavalAcademyDB>(), Is.False);

        Assert.That(info.ComponentDesigns["default-design-bridge-ship"].GetAttribute<AdminSpaceAtb>().AdminLevel,
            Is.EqualTo(AdminLevel.Ship));
        Assert.That(info.ComponentDesigns["default-design-bridge-well"].GetAttribute<AdminSpaceAtb>().AdminLevel,
            Is.EqualTo(AdminLevel.Planet));
        Assert.That(info.ComponentDesigns["default-design-bridge-system"].GetAttribute<AdminSpaceAtb>().AdminLevel,
            Is.EqualTo(AdminLevel.System));

        var techs = player.GetDataBlob<FactionTechDB>();
        var bridge = new ComponentDesigner(info.Data.ComponentTemplates["ship-command"], info.Data, techs);
        var span = bridge.ComponentDesignProperties["Command Span"];
        Assert.That(Enum.GetNames(span.EnumType), Is.EqualTo(new[] { "Body", "Well", "System" }));
        Assert.That(span.MaxValue, Is.EqualTo(3));

        var school = new ComponentDesigner(info.Data.ComponentTemplates["naval-academy"], info.Data, techs);
        var size = school.ComponentDesignProperties["Class Size"];
        size.SetMax();
        Assert.That(size.MaxValue, Is.EqualTo(8));
        Assert.That(school.ComponentDesignProperties["Class Length"].Description,
            Does.Contain("Mean experience cap 88"));
    }

    static void AssertSurvey(Entity ship, Entity root, params int[] ids)
    {
        var found = CommandSpan.Expand(root, CommandSpan.Effective(ship), _ => true).Select(body => body.Id).ToList();
        foreach (var id in ids)
            Assert.That(found, Does.Contain(id));
        if (CommandSpan.Effective(ship) == CommandSpanKind.Body)
            Assert.That(found, Has.Count.EqualTo(1));
    }

    static CommandResult Haul(Entity ship, Entity source, Entity dest)
        => new CommandTranslator(Game).Translate(
            Game.Factions[ship.FactionOwnerID],
            ship,
            new HaulPairCommand(ship.Id, source.Id, dest.Id, "iron"));

    static void SetBridge(Entity ship, AdminLevel level)
    {
        var admin = new AdminSpaceDB();
        admin.CommanderSeats.Add(new AdminSpaceAbilityState(level, "command-bridge"));
        ship.SetDataBlob(admin);
    }

    static void SetRank(Entity ship, int rank)
    {
        int id = ship.GetDataBlob<ShipInfoDB>().CommanderID;
        Assert.That(ship.Manager.TryGetGlobalEntityById(id, out var officer), Is.True);
        officer.GetDataBlob<CommanderDB>().Rank = rank;
    }

    static HaulScene HaulWorld()
    {
        var faction = FactionFactory.CreateFaction(Game, "SpanHaul-" + Guid.NewGuid().ToString("N"));
        faction.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");
        var iron = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("iron");
        var system = System("SpanHaul");
        var star = Body(system, null, "Star", 1.989e30, 6.96e8, Vector3.Zero);
        var earth = Body(system, star, "Earth", 5.972e24, 6.371e6, new Vector3(1.5e11, 0, 0));
        var luna = Body(system, earth, "Luna", 7.3e22, 1.7e6, new Vector3(3.8e8, 0, 0));
        var mars = Body(system, star, "Mars", 6.4e23, 3.4e6, new Vector3(2.2e11, 0, 0));
        var next = System("Next");
        var nextStar = Body(next, null, "Next", 1.989e30, 6.96e8, Vector3.Zero);
        Link(faction, system, star, next, nextStar);

        var earthOffice = Office(system, faction, earth, iron, sell: 20, buy: 0, stock: 20);
        var lunaOffice = Office(system, faction, luna, iron, sell: 0, buy: 20, stock: 0);
        var marsOffice = Office(system, faction, mars, iron, sell: 0, buy: 20, stock: 0);
        var neighbor = Office(next, faction, nextStar, iron, sell: 0, buy: 20, stock: 0);

        var ship = Entity.Create(faction.Id);
        double hold = iron.VolumePerUnit * 10;
        var cargo = new CargoStorageDB("general-storage", hold) { TransferRangeDv_mps = 1e12 };
        system.AddEntity(ship, new List<BaseDataBlob>
        {
            new ShipInfoDB(),
            cargo,
            new PositionDB(new Vector3(7e6, 0, 0), earth),
            new ActionQueueDB(),
            new WarpAbilityDB { MaxSpeed = 1e9 },
            MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
        });
        var captain = CommanderFactory.CreateShipCaptain(Game);
        var officer = CommanderFactory.Create(system, faction.Id, captain);
        ship.GetDataBlob<ShipInfoDB>().CommanderID = officer.Id;
        captain.AssignedTo = ship.Id;

        return new HaulScene
        {
            Faction = faction,
            Ship = ship,
            Star = star,
            Earth = earth,
            Luna = luna,
            Mars = mars,
            EarthOffice = earthOffice,
            LunaOffice = lunaOffice,
            MarsOffice = marsOffice,
            NeighborOffice = neighbor,
        };
    }

    static Entity Office(StarSystem system, Entity faction, Entity planet, ICargoable iron, long sell, long buy, long stock)
    {
        var office = Entity.Create(faction.Id);
        var cargo = new CargoStorageDB("general-storage", 100000) { TransferRangeDv_mps = 1e12 };
        var info = new ColonyInfoDB { PlanetEntity = planet };
        system.AddEntity(office, new List<BaseDataBlob>
        {
            new NameDB("Office"),
            cargo,
            new LogiBaseDB { Capacity = 5 },
            info,
            new PositionDB(new Vector3(7.1e6, 0, 0), planet),
            MassVolumeDB.NewFromMassAndRadius_m(1e20, 1e5),
        });
        if (stock > 0)
            Assert.That(cargo.AddCargoByUnit(iron, stock), Is.EqualTo(stock));
        Assert.That(MarketBook.SetListing(office, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = sell,
            Ask = 10,
            BuyQuantity = buy,
            Bid = 12,
        }), Is.True);
        return office;
    }

    static void Link(Entity faction, StarSystem from, Entity fromStar, StarSystem to, Entity toStar)
    {
        var gateFrom = Entity.Create(Game.NeutralFactionId);
        from.AddEntity(gateFrom, new List<BaseDataBlob>
        {
            new NameDB("Gate"),
            new PositionDB(new Vector3(4e11, 0, 0), fromStar),
        });
        var gateTo = Entity.Create(Game.NeutralFactionId);
        to.AddEntity(gateTo, new List<BaseDataBlob>
        {
            new NameDB("Gate"),
            new PositionDB(new Vector3(4e11, 0, 0), toStar),
        });
        gateFrom.SetDataBlob(new JumpPointDB(gateTo));
        gateTo.SetDataBlob(new JumpPointDB(gateFrom));
        var info = faction.GetDataBlob<FactionInfoDB>();
        if (!info.KnownSystems.Contains(from.ID))
            info.KnownSystems.Add(from.ID);
        if (!info.KnownSystems.Contains(to.ID))
            info.KnownSystems.Add(to.ID);
        info.RememberJumpPoint(gateFrom);
        info.RememberJumpPoint(gateTo);
    }

    static Entity Colony(long population, out Entity faction, out Entity species)
    {
        faction = FactionFactory.CreateFaction(Game, "Academy-" + Guid.NewGuid().ToString("N"));
        var system = System("Academy");
        var planet = Body(system, null, "Earth", 5.972e24, 6.371e6, Vector3.Zero);
        species = Entity.Create(faction.Id);
        system.AddEntity(species, new List<BaseDataBlob> { new NameDB("Human") });
        return ColonyFactory.CreateColony(faction, species, planet, population);
    }

    static int Mount(Entity colony, int classSize = 2, int months = 12)
    {
        var design = new ComponentDesign
        {
            Name = "Naval Academy",
            UniqueID = "naval-academy-" + Guid.NewGuid().ToString("N"),
        };
        design.AttributesByType[typeof(NavalAcademyAtb)] = new NavalAcademyAtb(classSize, months);
        colony.AddComponent(design);
        return colony.GetDataBlob<NavalAcademyDB>().Academies[^1].ComponentId;
    }

    static NavalAcademy Row(Entity colony)
        => colony.GetDataBlob<NavalAcademyDB>().Academies[0];

    static bool AcademyInterrupt(Entity colony)
    {
        foreach (var item in colony.Manager.ManagerSubpulses.InstanceProcessorsQueue)
        {
            if (item.Item.Item1 == nameof(NavalAcademyProcessor) && item.Item.Item2.Id == colony.Id)
                return true;
        }
        return false;
    }

    static Entity Officer(Entity colony, Entity faction, int experience, int cap, int rank)
    {
        var db = CommanderFactory.CreateAcademyGraduate(colony.Manager.Game);
        db.Experience = experience;
        db.ExperienceCap = cap;
        db.Rank = rank;
        return CommanderFactory.Create(colony.Manager, faction.Id, db);
    }

    static Entity Ship(EntityManager system, Entity faction)
    {
        var ship = Entity.Create(faction.Id);
        system.AddEntity(ship, new List<BaseDataBlob>
        {
            new ShipInfoDB(),
            new ActionQueueDB(),
            new NameDB("Chair"),
        });
        return ship;
    }

    static StarSystem System(string name)
    {
        var system = new StarSystem();
        system.Initialize(Game, name + "-" + Guid.NewGuid().ToString("N"), -1);
        system.SetActivityState(SystemActivityState.Stasis);
        return system;
    }

    static Entity Body(StarSystem system, Entity parent, string name, double mass, double radius, Vector3 at)
    {
        var body = Entity.Create();
        var surface = new SystemBodyInfoDB { LengthOfDay = TimeSpan.Zero };
        var blobs = new List<BaseDataBlob>
        {
            new NameDB(name),
            surface,
            MassVolumeDB.NewFromMassAndRadius_m(mass, radius),
            parent == null ? new PositionDB(at.X, at.Y, at.Z) : new PositionDB(at, parent),
        };
        system.AddEntity(body, blobs);
        return body;
    }

    sealed class HaulScene
    {
        public Entity Faction;
        public Entity Ship;
        public Entity Star;
        public Entity Earth;
        public Entity Luna;
        public Entity Mars;
        public Entity EarthOffice;
        public Entity LunaOffice;
        public Entity MarsOffice;
        public Entity NeighborOffice;
    }
}
