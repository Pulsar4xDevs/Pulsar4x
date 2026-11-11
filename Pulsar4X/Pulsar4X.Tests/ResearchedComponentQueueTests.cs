using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Storage;
using Pulsar4X.Technology;

namespace Pulsar4X.Tests;

/// <summary>
/// A custom component is not constructable until its design tech finishes. Finishing that tech
/// used to file the design under the tech id. The factory list still showed design.UniqueID,
/// and Queue the job looked that id up and was rejected with nothing on screen.
/// </summary>
[TestFixture]
public class ResearchedComponentQueueTests
{
    const string LineId = "factory-line";
    const string DesignId = "custom-geo-surveyor";

    static readonly Game Game = InitializeGame();

    static Game InitializeGame()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
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
    public void ResearchedGeoSurveyor_QueuesByDesignId()
    {
        var world = ResearchedSurveyor();
        var design = world.Design;

        Assert.That(world.Info.IndustryDesigns.ContainsKey(design.TechID), Is.False,
            "the tech id is not the constructable id");
        Assert.That(world.Info.IndustryDesigns.ContainsKey(design.UniqueID), Is.True);

        var colony = Colony(world);
        var listed = FactoryRow(colony, design.UniqueID);
        Assert.That(listed.DesignId, Is.EqualTo(design.UniqueID));
        Assert.That(listed.Name, Is.EqualTo(design.Name));

        var queued = new CommandTranslator(Game).Translate(world.Faction, colony, new QueueIndustryJobCommand(
            colony.Id, LineId, listed.DesignId, 1, false, false));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        var job = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs.Single();
        Assert.That(job.ItemGuid, Is.EqualTo(design.UniqueID));
        Assert.That(job.Name, Is.EqualTo(design.Name));
        Assert.That(job.NumberOrdered, Is.EqualTo(1));
    }

    [Test]
    public void ResearchedGeoSurveyor_FiledUnderTechId_StillQueues()
    {
        var world = ResearchedSurveyor();
        var design = world.Design;
        world.Info.IndustryDesigns.Remove(design.UniqueID);
        world.Info.IndustryDesigns[design.TechID] = design;
        Assert.That(world.Info.IndustryDesigns.ContainsKey(design.UniqueID), Is.False);

        var colony = Colony(world);
        Assert.That(FactoryRow(colony, design.UniqueID).DesignId, Is.EqualTo(design.UniqueID),
            "the list still offers the design id, not the tech id");

        var queued = new CommandTranslator(Game).Translate(world.Faction, colony, new QueueIndustryJobCommand(
            colony.Id, LineId, design.UniqueID, 1, false, false));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);
        Assert.That(world.Info.IndustryDesigns.ContainsKey(design.UniqueID), Is.True);
        Assert.That(world.Info.IndustryDesigns.ContainsKey(design.TechID), Is.False);
        Assert.That(
            colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs.Single().ItemGuid,
            Is.EqualTo(design.UniqueID));
    }

    static World ResearchedSurveyor()
    {
        bool wasResearched = ComponentDesigner.StartResearched;
        ComponentDesigner.StartResearched = false;
        try
        {
            var faction = FactionFactory.CreateFaction(Game, "Queue-" + Guid.NewGuid().ToString("N"));
            var info = faction.GetDataBlob<FactionInfoDB>();
            foreach (var id in new[] { "geo-surveyor", "stainless-steel", "plastic", "aluminium", "copper" })
                info.Data.Unlock(id);

            var template = info.Data.ComponentTemplates["geo-surveyor"];
            var design = new ComponentDesigner(template, info.Data, faction.GetDataBlob<FactionTechDB>(), DesignId)
            {
                Name = "Custom Geo Surveyor",
            }.CreateDesign(faction);

            Assert.That(design.ResearchCostValue, Is.GreaterThan(0), "the design must still need research");
            Assert.That(info.IndustryDesigns.ContainsKey(design.UniqueID), Is.False,
                "an unresearched design is not constructable");
            Assert.That(design.TechID, Is.EqualTo("tech-" + design.UniqueID));

            var researcher = new ResearcherDB(design)
            {
                PointsPerDay = new ModifiableValue<int>(10),
                CostPerDay = new ModifiableValue<decimal>(0),
            };
            researcher.TechQueue.Enqueue(design.TechID);

            var system = new StarSystem();
            system.Initialize(Game, "Queue-" + Guid.NewGuid().ToString("N"), -1);
            system.SetActivityState(SystemActivityState.Stasis);
            var lab = Entity.Create(faction.Id);
            system.AddEntity(lab, new List<BaseDataBlob> { researcher });

            new ResearchProcessor().DoResearch(lab);

            Assert.That(info.Data.Techs[design.TechID].Level, Is.EqualTo(1));
            return new World { System = system, Faction = faction, Info = info, Design = design };
        }
        finally
        {
            ComponentDesigner.StartResearched = wasResearched;
        }
    }

    static Entity Colony(World world)
    {
        var planet = Entity.Create();
        world.System.AddEntity(planet, new List<BaseDataBlob>
        {
            new NameDB("Body"),
            new PositionDB(),
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
        });

        var colony = Entity.Create(world.Faction.Id);
        world.System.AddEntity(colony, new List<BaseDataBlob>
        {
            new NameDB("Colony"),
            new ColonyInfoDB { PlanetEntity = planet },
            new CargoStorageDB("general-storage", 100000),
            new IndustryAbilityDB(LineId, new IndustryAbilityDB.ProductionLine
            {
                Name = "Factory",
                IndustryTypeRates = new Dictionary<string, int> { ["component-construction"] = 100 },
            }),
        });
        return colony;
    }

    static ConstructibleItemView FactoryRow(Entity colony, string designId)
    {
        var industry = new GameProjector(Game).ProjectEntity(colony, colony.FactionOwnerID).GetView<IndustryView>();
        Assert.That(industry, Is.Not.Null);
        return industry!.ProductionLines.Single(line => line.Id == LineId)
            .Constructibles.Single(item => item.DesignId == designId);
    }

    sealed class World
    {
        public StarSystem System = null!;
        public Entity Faction = null!;
        public FactionInfoDB Info = null!;
        public ComponentDesign Design = null!;
    }
}
