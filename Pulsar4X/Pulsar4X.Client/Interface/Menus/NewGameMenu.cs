using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;

namespace Pulsar4X.Client;

enum Page
{
    SelectMods,
    ConfigureGalaxy,
    SelectDetails
}

static class Helper
{
    public static byte[] ToByteArray(this string str)
    {
        return System.Text.Encoding.UTF8.GetBytes(str);
    }
}

public class NewGameMenu : PulsarGuiWindow
{
    private const int NAME_BUFFER_SIZE = 32;
    private const int SHORTNAME_BUFFER_SIZE = 5;
    private const string DEFAULT_NAME = "United Earth Corp";
    private const string DEFAULT_ABBREVIATION = "UEC";
    private const int MIN_STARTING_FUNDS = 1_000_000;
    private const int MAX_STARTING_FUNDS = 1_000_000_000;

    Page _currentPage = Page.SelectMods;
    ModLoader _modLoader = new ModLoader();
    ModDataStore _modDataStore = new ModDataStore();
    string _selectedSpeciesId = "";
    string _selectedThemeId = "";
    string _selectedSystemId = "";
    string _selectedBodyId = "";
    string _selectedColonyId = "";
    private bool _eleStart = true;

    List<string> _enabledSystems = new ();

    enum GameType { Nethost, Standalone }
    int _gameTypeButtonGrp = 0;
    GameType _selectedGameType = GameType.Standalone;
    byte[] _netPortInputBuffer = new byte[8];
    string _netPortString { get { return System.Text.Encoding.UTF8.GetString(_netPortInputBuffer); } }
    int _maxSystems = NewGameSettings.DEFAULT_NUM_SYSTEMS;
    int _startingFunds = 100_000_000;

    byte[] _corporationNameBuffer = Utils.BytesFromString(DEFAULT_NAME, NAME_BUFFER_SIZE);
    byte[] _corporationAbbreviationBuffer = Utils.BytesFromString(DEFAULT_ABBREVIATION, SHORTNAME_BUFFER_SIZE);
    byte[] _passInputBuffer = Utils.BytesFromString("", 16);

    byte[] _smPassInputbuffer = Utils.BytesFromString("", 16);

    int _masterSeed = 12345678;

    Vector2 _contentRegion = new Vector2();
    Vector2 _windowPos = new Vector2();
    Vector2 _windowSize = new Vector2();
    float _footerHeight = 0f;
    float _contentHeight = 0f;
    float _buttonWidth = 100f;
    private NewGameMenu()
    {
        _masterSeed = RandomNumberGenerator.GetInt32(999999999);
    }
    internal static NewGameMenu GetInstance()
    {
        if (!_uiState.LoadedWindows.ContainsKey(typeof(NewGameMenu)))
        {
            return new NewGameMenu();
        }
        return (NewGameMenu)_uiState.LoadedWindows[typeof(NewGameMenu)];
    }

    internal override void Display()
    {
        if(!IsActive) return;

        if (Window.Begin("New Game Setup", _flags | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse))
        {
            _contentRegion = ImGui.GetContentRegionAvail();
            // Get window dimensions
            _windowPos = ImGui.GetWindowPos();
            _windowSize = ImGui.GetContentRegionAvail();
            _footerHeight = ImGui.GetFrameHeightWithSpacing();

            // Calculate content area height (window height minus footer)
            _contentHeight = _windowSize.Y - _footerHeight;// - ImGui.GetFrameHeightWithSpacing();

            switch(_currentPage)
            {
                case Page.SelectMods:
                    DisplayModsPage();
                    break;
                case Page.ConfigureGalaxy:
                    DisplayConfigureGalaxy();
                    break;
                case Page.SelectDetails:
                    DisplayDetailsPage();
                    break;
            }
            Window.End();
        }
    }

    private void DisplayModsPage()
    {
        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight), ImGuiChildFlags.None);

        DisplayHelpers.Header("Select Mods to Enable");
        if(ImGui.BeginTable("ModsList", 4, Styles.TableFlags))
        {
            ImGui.TableNextColumn();
            ImGui.TableHeader("Mod Name");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Version");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Hash");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Enable?");

            foreach(var modMetadata in ModsState.AvailableMods)
            {
                ImGui.TableNextColumn();
                ImGui.Text(modMetadata.Mod.ModName);
                ImGui.TableNextColumn();
                ImGui.Text(modMetadata.Mod.Version);
                ImGui.TableNextColumn();
                ImGui.Text(modMetadata.ManifestHash);
                var isEnabled = ModsState.IsModEnabled[modMetadata.Mod.ModName];
                ImGui.TableNextColumn();
                if(ImGui.Checkbox("###" + modMetadata.Mod.ModName + "-checkbox", ref isEnabled))
                {
                    ModsState.IsModEnabled[modMetadata.Mod.ModName] = !ModsState.IsModEnabled[modMetadata.Mod.ModName];
                }
            }

            ImGui.EndTable();
        }

        // if (ImGui.RadioButton("Host Network Game", ref _gameTypeButtonGrp, 1))
        //     _selectedGameType = gameType.Nethost;
        // if (ImGui.RadioButton("Start Standalone Game", ref _gameTypeButtonGrp, 0))
        //     _selectedGameType = gameType.Standalone;
        // if (_selectedGameType == gameType.Nethost)
        //     ImGui.InputText("Network Port", _netPortInputBuffer, 8);

        ImGui.EndChild();
        ImGui.BeginChild("Footer", new Vector2(0, _footerHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        CancelButton();
        ImGui.SameLine();

        // Right-align the button by calculating its position
        float buttonX = _windowSize.X - _buttonWidth - ImGui.GetStyle().WindowPadding.X;
        ImGui.SetCursorPosX(buttonX);
        if (ImGui.Button("Next", new Vector2(_buttonWidth, 0)) || _uiState.debugnewgame)
        {
            _uiState.debugnewgame = false;
            LoadEnabledMods();
            _selectedSpeciesId = _modDataStore.Species.First().Key;
            _selectedThemeId = _modDataStore.Themes.First().Key;
            _selectedColonyId = _modDataStore.Colonies.First().Key;

            // Enable all the systems by default
            _enabledSystems.Clear();
            foreach(var (id, system) in _modDataStore.Systems)
            {
                if(!_modDataStore.SystemBodies.Any(kvp => kvp.Value.CanStartHere && _modDataStore.Systems[id].Bodies.Contains(kvp.Key)))
                    continue;
                _enabledSystems.Add(id);
            }
            _selectedSystemId = _enabledSystems.Any() ? _enabledSystems.First() : "";
            ResetSelectedBodyId();

            _currentPage = Page.ConfigureGalaxy;
        }
        ImGui.EndChild();
    }

    private void DisplayConfigureGalaxy()
    {
        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight), ImGuiChildFlags.None);

        DisplayHelpers.Header("Select pre-configured Systems to include");

        if(ImGui.BeginTable("SystemsSelection", 2, Styles.TableFlags))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Include");
            ImGui.TableHeadersRow();

            foreach(var (id, system) in _modDataStore.Systems)
            {
                ImGui.TableNextColumn();
                ImGui.Text(system.Name);
                ImGui.TableNextColumn();
                bool enabled = _enabledSystems.Contains(id);
                if(ImGui.Checkbox("###" + id, ref enabled))
                {
                    if(!enabled)
                        _enabledSystems.Remove(id);
                    else
                        _enabledSystems.Add(id);
                }

            }
            ImGui.EndTable();
        }

        ImGui.EndChild();
        ImGui.BeginChild("Footer", new Vector2(0, _footerHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        CancelButton();
        ImGui.SameLine();

        if (ImGui.Button("Back", new Vector2(_buttonWidth, 0)))
        {
            _currentPage = Page.SelectMods;
        }
        ImGui.SameLine();
        // Right-align the button by calculating its position
        float buttonX = _windowSize.X - _buttonWidth - ImGui.GetStyle().WindowPadding.X;
        ImGui.SetCursorPosX(buttonX);
        if (ImGui.Button("Next", new Vector2(_buttonWidth, 0)))
        {
            _currentPage = Page.SelectDetails;
        }
        ImGui.EndChild();
    }

    private void DisplayDetailsPage()
    {
        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight), ImGuiChildFlags.None);

        DisplayHelpers.Header("CORPORTATION SETUP");
        ImGui.InputText("Corporation Name", _corporationNameBuffer, NAME_BUFFER_SIZE);
        ImGui.InputText("Corporation Abbreviation", _corporationAbbreviationBuffer, SHORTNAME_BUFFER_SIZE);

        var display = _modDataStore.Species.TryGetValue(_selectedSpeciesId, out var speciesBlueprint) ? speciesBlueprint.Name : "";
        if(ImGui.BeginCombo("Select Species", display))
        {
            foreach(var (id, species) in _modDataStore.Species)
            {
                if(!species.Playable) continue;

                if(ImGui.Selectable(species.Name, _selectedSpeciesId.Equals(id)))
                {
                    _selectedSpeciesId = id;
                }
            }
            ImGui.EndCombo();
        }

        display = _modDataStore.Themes.TryGetValue(_selectedThemeId, out var themeBlueprint) ? themeBlueprint.Name : "";
        if(ImGui.BeginCombo("Select Theme", display))
        {
            foreach(var (id, theme) in _modDataStore.Themes)
            {
                if(ImGui.Selectable(theme.Name, _selectedThemeId.Equals(id)))
                {
                    _selectedThemeId = id;
                }
            }
            ImGui.EndCombo();
        }

        display = _modDataStore.Colonies.TryGetValue(_selectedColonyId, out var colonyBlueprint) ? colonyBlueprint.Name : "";
        if(ImGui.BeginCombo("Starting Corporation Configuration", display))
        {
            foreach(var (id, colony) in _modDataStore.Colonies)
            {
                if(ImGui.Selectable(colony.Name, _selectedColonyId.Equals(id)))
                {
                    _selectedColonyId = id;
                }
            }
            ImGui.EndCombo();
        }

        display = _modDataStore.Systems.TryGetValue(_selectedSystemId, out var systemBlueprint) ? systemBlueprint.Name : _selectedSystemId.Equals("random") ? "Randomly Generated" : "";
        if(ImGui.BeginCombo("Select Starting System", display))
        {
            foreach(var id in _enabledSystems)
            {
                if(ImGui.Selectable(_modDataStore.Systems[id].Name, _selectedSystemId.Equals(id)))
                {
                    _selectedSystemId = id;
                    ResetSelectedBodyId();
                }
            }
            ImGui.Separator();
            if(ImGui.Selectable("Randomly Generated", _selectedSystemId.Equals("random")))
            {
                _selectedSystemId = "random";
            }
            ImGui.EndCombo();
        }

        if(!_selectedSystemId.Equals("random") && _selectedSystemId.IsNotNullOrEmpty())
        {
            display = _modDataStore.SystemBodies.TryGetValue(_selectedBodyId, out var bodyBlueprint) ? bodyBlueprint.Name : "";
            if(ImGui.BeginCombo("Select Starting Location", display))
            {
                foreach(var (id, body) in _modDataStore.SystemBodies.Where(kvp => _modDataStore.Systems[_selectedSystemId].Bodies.Contains(kvp.Key)))
                {
                    if(!body.CanStartHere) continue;
                    if(ImGui.Selectable(body.Name, _selectedBodyId.Equals(id)))
                    {
                        _selectedBodyId = id;
                    }
                }
                ImGui.EndCombo();
            }
        }

        int tempStartingFunds = _startingFunds;

        if (ImGui.SliderInt("Starting Funds", ref tempStartingFunds,
                            MIN_STARTING_FUNDS, MAX_STARTING_FUNDS,
                            tempStartingFunds.ToString("C0", CultureInfo.CurrentCulture),
                            ImGuiSliderFlags.ClampOnInput))
        {
            // Round to the nearest million when the value changes
            _startingFunds = (int)Math.Round(tempStartingFunds / 1000000.0) * 1000000;
        }

        ImGui.NewLine();
        DisplayHelpers.Header("GAME OPTIONS");

        ImGui.InputInt("Game Seed", ref _masterSeed);
        ImGui.InputInt("Galaxy Size", ref _maxSystems);
        if(ImGui.IsItemHovered())
        {
            DisplayHelpers.DescriptiveTooltip(
                "Galaxy Size",
                "",
                "How many playable star systems the galaxy will have.");
        }
        ImGui.Checkbox("Include ELE", ref _eleStart);
        if(ImGui.IsItemHovered())
        {
            DisplayHelpers.DescriptiveTooltip(
                "End of Life Event",
                "",
                "Adds an end of life event the player must endeavor to discover and prevent.");
        }

        ImGui.EndChild();
        ImGui.BeginChild("Footer", new Vector2(0, _footerHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        CancelButton();
        ImGui.SameLine();

        if (ImGui.Button("Back", new Vector2(_buttonWidth, 0)))
        {
            _currentPage = Page.ConfigureGalaxy;
        }
        ImGui.SameLine();
        // Right-align the button by calculating its position
        float buttonX = _windowSize.X - _buttonWidth - ImGui.GetStyle().WindowPadding.X;
        ImGui.SetCursorPosX(buttonX);
        if (ImGui.Button("Create Game!", new Vector2(_buttonWidth, 0)))
        {
            CreateNewGame();
        }
        ImGui.EndChild();
    }

    private void LoadEnabledMods()
    {
        List<string> enabledMods = new ();

        foreach(var modMetadata in ModsState.AvailableMods)
        {
            if(ModsState.IsModEnabled[modMetadata.Mod.ModName])
            {
                enabledMods.Add(modMetadata.Path);
            }
        }

        // FIXME: this is show some error in the UI if no mods are selected
        if(enabledMods.Count == 0)
            return;

        _modLoader.LoadedMods.Clear();
        _modDataStore = new ModDataStore();
        foreach (var mod in enabledMods)
        {
            _modLoader.LoadModManifest(mod, _modDataStore);
        }
    }

    void CreateNewGame()
    {
        var p = new GameCreationParams
        {
            ModDataStore = _modDataStore,
            FactionName = Utils.StringFromBytes(_corporationNameBuffer),
            FactionAbbreviation = Utils.StringFromBytes(_corporationAbbreviationBuffer),
            SpeciesId = _selectedSpeciesId,
            ColonyId = _selectedColonyId,
            SystemId = _selectedSystemId,
            BodyId = _selectedBodyId,
            EnabledSystems = _enabledSystems,
            MaxSystems = _maxSystems,
            MasterSeed = _masterSeed,
            StartingFunds = _startingFunds,
            EleStart = _eleStart,
            SMPassword = Utils.StringFromBytes(_smPassInputbuffer),
            PlayerPassword = Utils.StringFromBytes(_passInputBuffer)
        };

        var result = CreateGameCore(p);
        if (result == null) return;

        var (game, playerFaction, startingSystem, startingBody) = result.Value;
        ActivateGameUI(game, playerFaction, startingSystem, startingBody);
        IsActive = false;
        _currentPage = Page.SelectMods;
    }

    private struct GameCreationParams
    {
        public ModDataStore ModDataStore;
        public string FactionName;
        public string FactionAbbreviation;
        public string SpeciesId;
        public string ColonyId;
        public string SystemId;
        public string BodyId;
        public List<string> EnabledSystems;
        public int MaxSystems;
        public int MasterSeed;
        public int StartingFunds;
        public bool EleStart;
        public string SMPassword;
        public string PlayerPassword;
    }

    private static (Game game, Entity faction, StarSystem system, Entity body)? CreateGameCore(GameCreationParams p)
    {
        var gameSettings = new NewGameSettings
        {
            MaxSystems = Math.Min(p.MaxSystems, NewGameSettings.DEFAULT_MAX_SYSTEMS),
            SMPassword = p.SMPassword,
            CreatePlayerFaction = true,
            DefaultFactionName = p.FactionName,
            DefaultPlayerPassword = p.PlayerPassword,
            DefaultSolStart = true,
            MasterSeed = p.MasterSeed,
            EleStart = p.EleStart
        };

        Game game = GameFactory.CreateGame(p.ModDataStore, gameSettings);
        game.CreatedOnGitHash = AssemblyInfo.GetGitHash();
        game.LastSaveGitHash = AssemblyInfo.GetGitHash();

        // Generate random systems up to the number of "Galaxy Size" minus the
        // number of included pre-made systems
        int numberToGenerate = p.MaxSystems - p.EnabledSystems.Count;
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
        foreach (var id in p.EnabledSystems)
        {
            StarSystemFactory.LoadFromBlueprint(game, p.ModDataStore.Systems[id]);
        }

        StarSystem? startingSystem = null;
        Entity? startingBody = null;

        if (p.SystemId.Equals("random"))
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
            var startingBodyBlueprint = p.ModDataStore.SystemBodies[p.BodyId];

            foreach (var system in game.Systems)
            {
                if (system.ManagerID != p.SystemId) continue;

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
            p.FactionName,
            p.FactionAbbreviation,
            p.StartingFunds);

        if (playerFaction == null) return null;

        playerFaction.FactionOwnerID = playerFaction.Id;
        playerFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(startingSystem.ID);

        var playerSpecies = SpeciesFactory.CreateFromBlueprint(startingSystem, p.ModDataStore.Species[p.SpeciesId]);
        playerSpecies.FactionOwnerID = playerFaction.Id;
        playerFaction.GetDataBlob<FactionInfoDB>().Species.Add(playerSpecies);

        // Setup the starting colony
        ColonyFactory.CreateFromBlueprint(game, playerFaction, playerSpecies, startingSystem, startingBody, p.ModDataStore.Colonies[p.ColonyId]);
        if (p.EleStart && !p.SystemId.Equals("random"))
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

    private static void ActivateGameUI(Game game, Entity playerFaction, StarSystem startingSystem, Entity startingBody)
    {
        _uiState.ClearGameState();
        _uiState.Game = game;
        _uiState.SetFaction(playerFaction, true);
        _uiState.SetActiveSystem(startingSystem.ManagerID);
        _uiState.Camera.CenterOnEntity(startingBody);
        _uiState.Camera.ZoomLevel = 2_245_000f;

        DebugWindow.GetInstance().SetGameEvents();
        TimeControl.GetInstance().SetActive();
        ToolBarWindow.GetInstance().SetActive();
        Selector.GetInstance().SetActive();
        EntityFilterBar.GetInstance().SetActive();
    }

    private void ResetSelectedBodyId()
    {
        if(_modDataStore.Systems.TryGetValue(_selectedSystemId, out var systemBlueprint))
        {
            var candidates = _modDataStore.SystemBodies.Where(kvp => kvp.Value.CanStartHere && systemBlueprint.Bodies.Contains(kvp.Key));
            _selectedBodyId = candidates.Any() ? candidates.First().Key : "";
        }
        else
        {
            _selectedBodyId = "";
        }
    }

    private void CancelButton()
    {
        if(ImGui.Button("Cancel", new Vector2(_buttonWidth, 0)))
        {
            IsActive = false;
            MainMenuItems.GetInstance().SetActive(true);
        }
    }

    /// <summary>
    /// Creates a new game instantly with default settings, bypassing the wizard
    /// </summary>
    public static void QuickstartGame()
    {
        try
        {
            // Initialize mod loader and data store
            ModLoader modLoader = new ModLoader();
            ModDataStore modDataStore = new ModDataStore();

            foreach (var modMetadata in ModsState.AvailableMods)
            {
                if (ModsState.IsModEnabled[modMetadata.Mod.ModName])
                {
                    modLoader.LoadModManifest(modMetadata.Path, modDataStore);
                }
            }

            if (!modDataStore.Species.Any(kvp => kvp.Value.Playable))
            {
                Console.WriteLine("Quickstart Error: No playable species found in loaded mods");
                return;
            }

            if (!modDataStore.Colonies.Any())
            {
                Console.WriteLine("Quickstart Error: No colonies found in loaded mods");
                return;
            }

            // Select defaults
            string selectedSpeciesId = modDataStore.Species.First(kvp => kvp.Value.Playable).Key;
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

            if (enabledSystems.Count == 0)
            {
                Console.WriteLine("Quickstart Error: No compatible starting systems found");
                return;
            }

            string selectedSystemId = enabledSystems.First();
            var selectedSystemBlueprint = modDataStore.Systems[selectedSystemId];
            string selectedBodyId = modDataStore.SystemBodies
                .Where(kvp => kvp.Value.CanStartHere && selectedSystemBlueprint.Bodies.Contains(kvp.Key))
                .First().Key;

            var p = new GameCreationParams
            {
                ModDataStore = modDataStore,
                FactionName = DEFAULT_NAME,
                FactionAbbreviation = DEFAULT_ABBREVIATION,
                SpeciesId = selectedSpeciesId,
                ColonyId = selectedColonyId,
                SystemId = selectedSystemId,
                BodyId = selectedBodyId,
                EnabledSystems = enabledSystems,
                MaxSystems = NewGameSettings.DEFAULT_NUM_SYSTEMS,
                MasterSeed = RandomNumberGenerator.GetInt32(999999999),
                StartingFunds = 100_000_000,
                EleStart = true,
                SMPassword = "",
                PlayerPassword = ""
            };

            var result = CreateGameCore(p);
            if (result == null)
            {
                Console.WriteLine("Quickstart Error: Could not create game");
                return;
            }

            var (game, playerFaction, startingSystem, startingBody) = result.Value;
            ActivateGameUI(game, playerFaction, startingSystem, startingBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Quickstart Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}