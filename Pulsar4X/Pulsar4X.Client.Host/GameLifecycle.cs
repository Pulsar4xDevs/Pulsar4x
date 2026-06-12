using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.People;

namespace Pulsar4X.Client.Host;

/// <summary>
/// The host's implementation of the game-lifecycle seam: all the engine work of creating,
/// loading and saving games (mod loading, factories, faction/species/colony setup) lives here,
/// in the composition root. After bringing a game up it hands the UI an engine-free
/// <see cref="GameActivation"/>.
/// </summary>
public sealed class GameLifecycle : IGameLifecycle
{
    private const string DEFAULT_NAME = "United Earth Corp";
    private const string DEFAULT_ABBREVIATION = "UEC";

    private readonly GlobalUIState _state;
    private ModDataStore _modDataStore = new ();

    public GameLifecycle(GlobalUIState state)
    {
        _state = state;
        ModsState.RefreshModsList(PulsarMainWindow.ModsPath);
    }

    public IReadOnlyList<ModOption> GetAvailableMods()
        => ModsState.AvailableMods
            .Select(m => new ModOption(m.Mod.ModName, m.Mod.Version, m.ManifestHash, m.Path,
                ModsState.IsModEnabled.TryGetValue(m.Mod.ModName, out bool enabled) && enabled))
            .ToList();

    public NewGameCatalog LoadMods(IReadOnlyList<string> modManifestPaths)
    {
        var modLoader = new ModLoader();
        _modDataStore = new ModDataStore();
        foreach (var manifestPath in modManifestPaths)
        {
            modLoader.LoadModManifest(manifestPath, _modDataStore);
        }

        return BuildCatalog(_modDataStore);
    }

    private static NewGameCatalog BuildCatalog(ModDataStore data)
    {
        var systems = new List<CatalogSystemOption>();
        foreach (var (id, system) in data.Systems)
        {
            var startingBodies = data.SystemBodies
                .Where(kvp => kvp.Value.CanStartHere && system.Bodies.Contains(kvp.Key))
                .Select(kvp => new CatalogOption(kvp.Key, kvp.Value.Name))
                .ToList();
            systems.Add(new CatalogSystemOption(id, system.Name, startingBodies));
        }

        return new NewGameCatalog(
            data.Species.Where(kvp => kvp.Value.Playable)
                .Select(kvp => new CatalogOption(kvp.Key, kvp.Value.Name)).ToList(),
            data.Themes.Select(kvp => new CatalogOption(kvp.Key, kvp.Value.Name)).ToList(),
            data.Colonies.Select(kvp => new CatalogOption(kvp.Key, kvp.Value.Name)).ToList(),
            systems);
    }

    public GameActivation? CreateNewGame(NewGameRequest request)
    {
        // Reload the requested mod set so creation is self-contained (Quickstart and a re-entered
        // wizard both pass through here with their own paths).
        LoadMods(request.ModManifestPaths);

        var result = CreateGameCore(_modDataStore, request);
        if (result == null) return null;

        var (game, playerFaction, startingSystem, startingBody) = result.Value;
        return Activate(game, playerFaction, startingSystem.ManagerID, startingBody);
    }

    public GameActivation? Quickstart()
    {
        try
        {
            var modManifestPaths = GetAvailableMods()
                .Where(m => m.EnabledByDefault)
                .Select(m => m.ManifestPath)
                .ToList();
            var catalog = LoadMods(modManifestPaths);

            if (catalog.Species.Count == 0)
            {
                Console.WriteLine("Quickstart Error: No playable species found in loaded mods");
                return null;
            }

            if (catalog.Colonies.Count == 0)
            {
                Console.WriteLine("Quickstart Error: No colonies found in loaded mods");
                return null;
            }

            var startableSystems = catalog.Systems.Where(s => s.StartingBodies.Count > 0).ToList();
            if (startableSystems.Count == 0)
            {
                Console.WriteLine("Quickstart Error: No compatible starting systems found");
                return null;
            }

            var startingSystem = startableSystems.First();
            var request = new NewGameRequest(
                ModManifestPaths: modManifestPaths,
                FactionName: DEFAULT_NAME,
                FactionAbbreviation: DEFAULT_ABBREVIATION,
                SpeciesId: catalog.Species.First().Id,
                ColonyId: catalog.Colonies.First().Id,
                SystemId: startingSystem.Id,
                BodyId: startingSystem.StartingBodies.First().Id,
                EnabledSystems: startableSystems.Select(s => s.Id).ToList(),
                MaxSystems: NewGameSettings.DEFAULT_NUM_SYSTEMS,
                MasterSeed: RandomNumberGenerator.GetInt32(999999999),
                StartingFunds: 100_000_000,
                EleStart: true,
                SMPassword: "",
                PlayerPassword: "");

            var result = CreateGameCore(_modDataStore, request);
            if (result == null)
            {
                Console.WriteLine("Quickstart Error: Could not create game");
                return null;
            }

            var (game, playerFaction, system, startingBody) = result.Value;
            return Activate(game, playerFaction, system.ManagerID, startingBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Quickstart Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return null;
        }
    }

    public GameActivation? LoadGame(string filePath)
    {
        string contents = File.ReadAllText(filePath);
        var loadedGame = Game.Load(contents);

        // TODO: need to figure out a way to properly handle faction selection on load
        (int id, Entity faction) = loadedGame.Factions.Last();

        _state.ClearGameState();
        _state.Game = loadedGame;
        _state.SetFaction(faction, true);

        return new GameActivation(faction.GetDataBlob<FactionInfoDB>().KnownSystems[0]);
    }

    public void SaveGame(string filePath)
    {
        if (_state.Game == null) return;

        // Update the save git hash
        _state.Game.LastSaveGitHash = AssemblyInfo.GetGitHash();

        string gameJson = Game.Save(_state.Game);
        File.WriteAllText(filePath, gameJson);
    }

    /// <summary>Binds the freshly created game to the UI state (faction + game client) and
    /// describes the rest engine-free.</summary>
    private GameActivation Activate(Game game, Entity playerFaction, string systemId, Entity startingBody)
    {
        _state.ClearGameState();
        _state.Game = game;
        _state.SetFaction(playerFaction, true);

        Vec3? cameraPos = null;
        if (startingBody.TryGetDataBlob<PositionDB>(out var position))
        {
            var absolute = position.AbsolutePosition;
            cameraPos = new Vec3(absolute.X, absolute.Y, absolute.Z);
        }

        return new GameActivation(systemId)
        {
            CameraPositionM = cameraPos,
            CameraZoom = 2_245_000f,
        };
    }

    private static (Game game, Entity faction, StarSystem system, Entity body)? CreateGameCore(
        ModDataStore modDataStore, NewGameRequest request)
    {
        var gameSettings = new NewGameSettings
        {
            MaxSystems = Math.Min(request.MaxSystems, NewGameSettings.DEFAULT_MAX_SYSTEMS),
            SMPassword = request.SMPassword,
            CreatePlayerFaction = true,
            DefaultFactionName = request.FactionName,
            DefaultPlayerPassword = request.PlayerPassword,
            DefaultSolStart = true,
            MasterSeed = request.MasterSeed,
            EleStart = request.EleStart
        };

        Game game = GameFactory.CreateGame(modDataStore, gameSettings);
        game.CreatedOnGitHash = AssemblyInfo.GetGitHash();
        game.LastSaveGitHash = AssemblyInfo.GetGitHash();

        // Generate random systems up to the number of "Galaxy Size" minus the
        // number of included pre-made systems
        int numberToGenerate = request.MaxSystems - request.EnabledSystems.Count;
        if (numberToGenerate > 0)
        {
            for (int i = 0; i < numberToGenerate; i++)
            {
                string systemName = NameFactory.GetSystemName(game);
                var seed = game.GlobalManager.RNG.Next();
                game.GalaxyGen.GenerateSystem(game, systemName, seed);
            }
        }

        // Load in the pre-made systems
        foreach (var id in request.EnabledSystems)
        {
            StarSystemFactory.LoadFromBlueprint(game, modDataStore.Systems[id]);
        }

        StarSystem? startingSystem = null;
        Entity? startingBody = null;

        if (request.SystemId.Equals("random"))
        {
            // Pick a random system that has a terrestrial planet
            var candidates = new List<(StarSystem system, Entity body)>();
            foreach (var system in game.Systems)
            {
                foreach (var bodyInfo in system.GetAllDataBlobsOfType<SystemBodyInfoDB>())
                {
                    if (bodyInfo.BodyType == BodyType.Terrestrial && bodyInfo.OwningEntity != null)
                    {
                        candidates.Add((system, bodyInfo.OwningEntity));
                    }
                }
            }

            if (candidates.Count == 0) return null;

            var pick = candidates[RandomNumberGenerator.GetInt32(candidates.Count)];
            startingSystem = pick.system;
            startingBody = pick.body;
        }
        else
        {
            var startingBodyBlueprint = modDataStore.SystemBodies[request.BodyId];

            foreach (var system in game.Systems)
            {
                if (system.ManagerID != request.SystemId) continue;

                startingSystem = system;
                foreach (var systemBody in system.GetAllDataBlobsOfType<SystemBodyInfoDB>())
                {
                    if (systemBody.OwningEntity?.GetDefaultName()?.Equals(startingBodyBlueprint.Name) == true)
                    {
                        startingBody = systemBody.OwningEntity;
                    }
                }
            }
        }

        if (startingSystem == null || startingBody == null) return null;

        // Create the player's faction
        var playerFaction = FactionFactory.CreateBasicFaction(
            game,
            request.FactionName,
            request.FactionAbbreviation,
            request.StartingFunds);

        if (playerFaction == null) return null;

        playerFaction.FactionOwnerID = playerFaction.Id;
        playerFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(startingSystem.ID);

        var playerSpecies = SpeciesFactory.CreateFromBlueprint(startingSystem, modDataStore.Species[request.SpeciesId]);
        playerSpecies.FactionOwnerID = playerFaction.Id;
        playerFaction.GetDataBlob<FactionInfoDB>().Species.Add(playerSpecies);

        // Setup the starting colony
        ColonyFactory.CreateFromBlueprint(game, playerFaction, playerSpecies, startingSystem, startingBody, modDataStore.Colonies[request.ColonyId]);
        if (request.EleStart && !request.SystemId.Equals("random"))
            AsteroidFactory.CreateAsteroid(startingSystem, startingBody, game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(365));

        // Create starting people
        var scientistDB = CommanderFactory.CreateScientist(game);
        var scientist = CommanderFactory.Create(startingSystem, playerFaction.Id, scientistDB);

        var adminDB = CommanderFactory.CreateAdmin(game);
        CommanderFactory.Create(startingSystem, playerFaction.Id, adminDB);

        if (scientist.TryGetDataBlob<BonusesDB>(out var bonusesDB))
        {
            bonusesDB.Bonuses.Add(new Bonus(
                "Research Points",
                0.1,
                BonusType.Perentage,
                BonusCategory.ResearchPoints,
                "tech-category-power-propulsion"
            ));
        }

        game.PostNewGameInitialization();

        return (game, playerFaction, startingSystem, startingBody);
    }
}
