using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.People;

namespace BenchmarkProject;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class Benchmarks
{
    private Game _game;

    [GlobalSetup]
    public void Setup()
    {
        // Initialize mod loader and data store
        ModLoader modLoader = new ModLoader();
        ModDataStore modDataStore = new ModDataStore();

        modLoader.LoadModManifest("../../../../../../../../GameData/basemod/modInfo.json", modDataStore);

        // Select default values
        string selectedSpeciesId = modDataStore.Species.First(kvp => kvp.Value.Playable).Key;
        string selectedThemeId = modDataStore.Themes.First().Key;
        string selectedColonyId = modDataStore.Colonies.First().Key;

        // Find all systems with CanStartHere bodies
        List<string> enabledSystems = new();
        foreach (var (id, system) in modDataStore.Systems)
        {
            if (modDataStore.SystemBodies.Any(kvp =>
                        kvp.Value.CanStartHere && system.Bodies.Contains(kvp.Key)))
            {
                enabledSystems.Add(id);
            }
        }

        // Select first available system and body
        string selectedSystemId = enabledSystems.First();
        SystemBlueprint selectedSystemBlueprint = modDataStore.Systems[selectedSystemId];

        string selectedBodyId = modDataStore.SystemBodies
            .Where(kvp => kvp.Value.CanStartHere && selectedSystemBlueprint.Bodies.Contains(kvp.Key))
            .First().Key;

        // Generate random seed
        int masterSeed = 123;
        int maxSystems = 2;

        // Create game settings
        NewGameSettings gameSettings = new NewGameSettings
        {
            MaxSystems = maxSystems,
            CreatePlayerFaction = true,
            DefaultFactionName = "BENCH",
            DefaultSolStart = true,
            MasterSeed = masterSeed,
            EleStart = false
        };

        // Create game
        SpeciesBlueprint startingSpeciesBlueprint = modDataStore.Species[selectedSpeciesId];
        ThemeBlueprint startingThemeBlueprint = modDataStore.Themes[selectedThemeId];
        ColonyBlueprint startingColonyBlueprint = modDataStore.Colonies[selectedColonyId];
        SystemBlueprint? startingSystemBlueprint = null;
        SystemBodyBlueprint? startingBodyBlueprint = null;

        Game game = GameFactory.CreateGame(modDataStore, gameSettings);

        // Need to enforce single thread so that TimeStep blocks
        game.Settings.EnforceSingleThread = true;

        StarSystem? startingSystem = null;
        Entity? startingBody = null;

        // Generate random systems
        int numberToGenerate = maxSystems - enabledSystems.Count;
        if(numberToGenerate > 0)
        {
            for(int i = 0; i < numberToGenerate; i++)
            {
                string systemName = $"Generated System #{i + 1}";
                game.GalaxyGen.GenerateSystem(game, systemName, masterSeed);
            }
        }

        startingSystemBlueprint = modDataStore.Systems[selectedSystemId];
        startingBodyBlueprint = modDataStore.SystemBodies[selectedBodyId];

        // Load pre-made systems
        foreach(var id in enabledSystems)
        {
            var system = StarSystemFactory.LoadFromBlueprint(game, modDataStore.Systems[id]);
            if(id.Equals(selectedSystemId))
            {
                startingSystem = system;
                foreach(var systemBody in startingSystem.GetAllDataBlobsOfType<SystemBodyInfoDB>())
                {
                    if(startingBodyBlueprint != null && systemBody.OwningEntity?.GetDefaultName()?.Equals(startingBodyBlueprint.Name) == true)
                    {
                        startingBody = systemBody.OwningEntity;
                    }
                }
            }
        }

        // Create player faction
        var playerFaction = FactionFactory.CreateBasicFaction(
                game,
                "BENCH",
                "BENCH",
                0);

        playerFaction.FactionOwnerID = playerFaction.Id;
        playerFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(startingSystem.ID);

        var playerSpecies = SpeciesFactory.CreateFromBlueprint(startingSystem, modDataStore.Species[selectedSpeciesId]);
        playerSpecies.FactionOwnerID = playerFaction.Id;
        playerFaction.GetDataBlob<FactionInfoDB>().Species.Add(playerSpecies);

        // Setup starting colony
        var playerColony = ColonyFactory.CreateFromBlueprint(game, playerFaction, playerSpecies, startingSystem, startingBody, modDataStore.Colonies[selectedColonyId]);

        // Create starting people
        var scientistDB = CommanderFactory.CreateScientist(game);
        var scientist = CommanderFactory.Create(startingSystem, playerFaction.Id, scientistDB);

        var adminDB = CommanderFactory.CreateAdmin(game);
        var admin = CommanderFactory.Create(startingSystem, playerFaction.Id, adminDB);

        if(scientist.TryGetDataBlob<BonusesDB>(out var bonusesDB))
        {
            bonusesDB.Bonuses.Add(new Bonus(
                        "Research Points",
                        0.1,
                        BonusType.Perentage,
                        BonusCategory.ResearchPoints,
                        "tech-category-power-propulsion"
                        ));
        }

        // Initialize game
        game.PostNewGameInitialization();
        _game = game;
    }

    [Benchmark]
    public void Benchmark1DaysTimeStep()
    {
        _game.TimePulse.Ticklength = TimeSpan.FromDays(1);
        _game.TimePulse.TimeStep();
        Console.WriteLine("Benchmark1DaysTimeStep DONE");
    }

    [Benchmark]
    public void Benchmark30DaysTimeStep()
    {
        _game.TimePulse.Ticklength = TimeSpan.FromDays(30);
        _game.TimePulse.TimeStep();
        Console.WriteLine("Benchmark30DaysTimeStep DONE");
    }

    [Benchmark]
    public void Benchmark365DaysTimeStep()
    {
        _game.TimePulse.Ticklength = TimeSpan.FromDays(365);
        _game.TimePulse.TimeStep();
        Console.WriteLine("Benchmark365DaysTimeStep DONE");
    }
}
