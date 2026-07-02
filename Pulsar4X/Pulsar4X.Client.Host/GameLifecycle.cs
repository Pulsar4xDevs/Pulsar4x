using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Engine.Api;
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
public sealed class GameLifecycle : IGameLifecycle, IDesignDataProvider
{
    private const string DEFAULT_NAME = "United Earth Corp";
    private const string DEFAULT_ABBREVIATION = "UEC";

    private readonly GlobalUIState _state;
    private ModDataStore _modDataStore = new ();

    private Game? _game;
    private EngineGameServer? _server;
    private Entity? _playerFaction;

    /// <summary>The lifecycle instance the composition root built, for the host's dev tools —
    /// their window into the live engine objects the UI library no longer exposes.</summary>
    public static GameLifecycle? Instance { get; private set; }

    public GameLifecycle(GlobalUIState state)
    {
        // Ensure the composition root only constructs one lifecycle, so Instance is well-defined for the dev tools.
        Debug.Assert(Instance is null, "GameLifecycle constructed more than once; Instance would be overwritten.");

        _state = state;
        Instance = this;
        ModsState.RefreshModsList(PulsarMainWindow.ModsPath);
    }

    /// <summary>The running engine game; dev tooling only.</summary>
    public Game? Game => _game;

    /// <summary>The engine entity of the faction this session is bound to; dev tooling only.</summary>
    public Entity? Faction
        => _game != null && _game.Factions.TryGetValue(_state.FactionId, out var faction) ? faction : null;

    /// <summary>The engine star system the UI is looking at; dev tooling only.</summary>
    public StarSystem? SelectedSystem
        => _game?.Systems.FirstOrDefault(s => s.ID.Equals(_state.SelectedStarSystemId));

    /// <summary>The selected system wrapped for the dev-tool windows; dev tooling only.</summary>
    public SystemState? SelectedSystemState
        => SelectedSystem is { } system ? new SystemState(system) : null;

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
        SetGame(loadedGame);
        BindFaction(faction, setAsPlayer: true);

        return faction.TryGetDataBlob<FactionInfoDB>(out var factionInfoDB)
            ? new GameActivation(factionInfoDB.KnownSystems[0])
            : null;
    }

    public void SaveGame(string filePath)
    {
        if (_game == null) return;

        // Update the save git hash
        _game.LastSaveGitHash = AssemblyInfo.GetGitHash();

        string gameJson = Game.Save(_game);
        File.WriteAllText(filePath, gameJson);
    }

    public void SetGameMasterMode(bool enabled)
    {
        if (_game == null) return;

        if (enabled)
            BindFaction(_game.GameMasterFaction, setAsPlayer: false);
        else if (_playerFaction != null)
            BindFaction(_playerFaction, setAsPlayer: false);
    }

    public GameRules? GetGameRules()
        => _game == null
            ? null
            : new GameRules(
                _game.Settings.EnableMultiThreading,
                _game.Settings.EnforceSingleThread,
                _game.Settings.UseRelativeVelocity,
                _game.Settings.StrictNewtonion);

    public void ApplyGameRules(GameRules rules)
    {
        if (_game == null) return;

        _game.Settings.EnableMultiThreading = rules.EnableMultiThreading;
        _game.Settings.EnforceSingleThread = rules.EnforceSingleThread;
        _game.Settings.UseRelativeVelocity = rules.UseRelativeVelocity;
        _game.Settings.StrictNewtonion = rules.StrictNewtonion;
    }

    public bool TryGetDesignData(out FactionInfoDB info, out FactionTechDB techs)
    {
        info = null!;
        techs = null!;
        if (_server == null || _state.GameClient is not { } client) return false;
        if (_server.GetFactionDesignData(client.Session) is not { } data) return false;

        (info, techs) = data;
        return true;
    }

    private void SetGame(Game game)
    {
        _server?.Dispose();
        _game = game;
        _server = new EngineGameServer(game);
        _playerFaction = null;
    }

    /// <summary>Connects a session for the faction and hands the resulting client to the UI.</summary>
    private void BindFaction(Entity faction, bool setAsPlayer)
    {
        if (_server == null) throw new InvalidOperationException("No game is loaded.");

        if (setAsPlayer)
            _playerFaction = faction;

        var client = ClientFactory.CreateLocalClient(_server);
        // The trusted host presents the SM credential so it can bind to the GameMaster for SM mode.
        string? credential = faction == _game?.GameMasterFaction ? ConnectRequest.SpaceMasterCredential : null;
        var connect = client.ConnectAsync(new ConnectRequest { PlayerName = "Player", FactionId = faction.Id, Credential = credential }).Result;
        _state.OnGameClientBound(client, connect?.Game);
    }

    /// <summary>Binds the freshly created game to the UI state (faction + game client) and
    /// describes the rest engine-free.</summary>
    private GameActivation Activate(Game game, Entity playerFaction, string systemId, Entity startingBody)
    {
        _state.ClearGameState();
        SetGame(game);
        BindFaction(playerFaction, setAsPlayer: true);

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
        if (!playerFaction.TryGetDataBlob<FactionInfoDB>(out var factionInfoDB))
        {
            throw new Exception("Missing FactionInfoDB on the players faction");
        }

        playerFaction.FactionOwnerID = playerFaction.Id;
        factionInfoDB.KnownSystems.Add(startingSystem.ID);

        var playerSpecies = SpeciesFactory.CreateFromBlueprint(startingSystem, modDataStore.Species[request.SpeciesId]);
        playerSpecies.FactionOwnerID = playerFaction.Id;
        factionInfoDB.Species.Add(playerSpecies);

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
