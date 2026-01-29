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
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
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
    int _maxSystems = 5;
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

    NewGameSettings gameSettings = new NewGameSettings();

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
        gameSettings = new NewGameSettings
        {
            MaxSystems = _maxSystems,
            SMPassword = Utils.StringFromBytes(_smPassInputbuffer),
            CreatePlayerFaction = true,
            DefaultFactionName = Utils.StringFromBytes(_corporationNameBuffer),
            DefaultPlayerPassword = Utils.StringFromBytes(_passInputBuffer),
            DefaultSolStart = true,
            MasterSeed = _masterSeed,
            EleStart = _eleStart
        };

        SpeciesBlueprint startingSpeciesBlueprint = _modDataStore.Species[_selectedSpeciesId];
        ThemeBlueprint startingThemeBlueprint = _modDataStore.Themes[_selectedThemeId];
        ColonyBlueprint startingColonyBlueprint = _modDataStore.Colonies[_selectedColonyId];
        SystemBlueprint? startingSystemBlueprint = null;
        SystemBodyBlueprint? startingBodyBlueprint = null;


        Game game = GameFactory.CreateGame(_modDataStore, gameSettings);
        game.CreatedOnGitHash = AssemblyInfo.GetGitHash(); // Save the git hash to the game
        game.LastSaveGitHash = AssemblyInfo.GetGitHash();

        StarSystem? startingSystem = null;
        Entity? startingBody = null;

        // Generate random systems up to the number of "Galaxy Size" minus the
        // number of included pre-made systems
        int numberToGenerate = _maxSystems - _enabledSystems.Count;
        if(numberToGenerate > 0)
        {
            for(int i = 0; i < numberToGenerate; i++)
            {
                // TODO: add random system names
                string systemName = $"Generated System #{i + 1}";
                game.GalaxyGen.GenerateSystem(game, systemName, _masterSeed);
            }
        }


        if(_selectedSystemId.Equals("random"))
        {
            // TODO: support starting in a random system
            return;
        }
        else
        {
            startingSystemBlueprint = _modDataStore.Systems[_selectedSystemId];
            startingBodyBlueprint = _modDataStore.SystemBodies[_selectedBodyId];
        }

        // Load in the selected systems
        foreach(var id in _enabledSystems)
        {
            var system = StarSystemFactory.LoadFromBlueprint(game, _modDataStore.Systems[id]);
            if(id.Equals(_selectedSystemId))
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

        if(startingSystem == null || startingBody == null) return;

        // Create the players faction
        var playerFaction = FactionFactory.CreateBasicFaction(
                                game,
                                gameSettings.DefaultFactionName,
                                Utils.StringFromBytes(_corporationAbbreviationBuffer),
                                _startingFunds);

        if(playerFaction == null) return;

        playerFaction.FactionOwnerID = playerFaction.Id;
        playerFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(startingSystem.ID);

        var playerSpecies = SpeciesFactory.CreateFromBlueprint(startingSystem, _modDataStore.Species[_selectedSpeciesId]);
        playerSpecies.FactionOwnerID = playerFaction.Id;
        playerFaction.GetDataBlob<FactionInfoDB>().Species.Add(playerSpecies);

        // Setup the starting colony
        var playerColony = ColonyFactory.CreateFromBlueprint(game, playerFaction, playerSpecies, startingSystem, startingBody, _modDataStore.Colonies[_selectedColonyId]);
        if(_eleStart)
            AsteroidFactory.CreateAsteroid(startingSystem, startingBody, game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(365));

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

        // TODO: need to add the implementation for a random start
        // TODO: need to find a way to handle this via the mods instead of loading it here
        //var (newGameFaction, systemId) = Pulsar4X.Engine.DefaultStartFactory.LoadFromJson(game, "Data/basemod/defaultStart.json");

        // Call the game post new game initialization
        game.PostNewGameInitialization();

        _uiState.ClearGameState();
        _uiState.Game = game;
        _uiState.SetFaction(playerFaction, true);
        _uiState.SetActiveSystem(startingSystem.ManagerID);
        _uiState.Camera.CenterOnEntity(startingBody);
        _uiState.Camera.ZoomLevel = 2_245_000f;

        DebugWindow.GetInstance().SetGameEvents();
        IsActive = false;
        _currentPage = Page.SelectMods; // reset the page
        //we initialize window instances so that they get always displayed and automatically open after new game is created.
        TimeControl.GetInstance().SetActive();
        ToolBarWindow.GetInstance().SetActive();
        Selector.GetInstance().SetActive();
        //EntityUIWindowSelector.GetInstance().SetActive();
        //EntityInfoPanel.GetInstance().SetActive();
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

            // Load all default-enabled mods
            foreach (var modMetadata in ModsState.AvailableMods)
            {
                if (ModsState.IsModEnabled[modMetadata.Mod.ModName])
                {
                    modLoader.LoadModManifest(modMetadata.Path, modDataStore);
                }
            }

            // Validate we have required data
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

            if (enabledSystems.Count == 0)
            {
                Console.WriteLine("Quickstart Error: No compatible starting systems found");
                return;
            }

            // Select first available system and body
            string selectedSystemId = enabledSystems.First();
            SystemBlueprint selectedSystemBlueprint = modDataStore.Systems[selectedSystemId];

            string selectedBodyId = modDataStore.SystemBodies
                .Where(kvp => kvp.Value.CanStartHere && selectedSystemBlueprint.Bodies.Contains(kvp.Key))
                .First().Key;

            // Generate random seed
            int masterSeed = RandomNumberGenerator.GetInt32(999999999);
            int maxSystems = 2;
            int startingFunds = 100_000_000;
            bool eleStart = true;

            // Create game settings
            NewGameSettings gameSettings = new NewGameSettings
            {
                MaxSystems = maxSystems,
                SMPassword = "",
                CreatePlayerFaction = true,
                DefaultFactionName = DEFAULT_NAME,
                DefaultPlayerPassword = "",
                DefaultSolStart = true,
                MasterSeed = masterSeed,
                EleStart = eleStart
            };

            // Create game
            SpeciesBlueprint startingSpeciesBlueprint = modDataStore.Species[selectedSpeciesId];
            ThemeBlueprint startingThemeBlueprint = modDataStore.Themes[selectedThemeId];
            ColonyBlueprint startingColonyBlueprint = modDataStore.Colonies[selectedColonyId];
            SystemBlueprint? startingSystemBlueprint = null;
            SystemBodyBlueprint? startingBodyBlueprint = null;

            Game game = GameFactory.CreateGame(modDataStore, gameSettings);
            game.CreatedOnGitHash = AssemblyInfo.GetGitHash();
            game.LastSaveGitHash = AssemblyInfo.GetGitHash();

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

            if(startingSystem == null || startingBody == null)
            {
                Console.WriteLine("Quickstart Error: Could not create starting system or body");
                return;
            }

            // Create player faction
            var playerFaction = FactionFactory.CreateBasicFaction(
                game,
                DEFAULT_NAME,
                DEFAULT_ABBREVIATION,
                startingFunds);

            if(playerFaction == null)
            {
                Console.WriteLine("Quickstart Error: Could not create player faction");
                return;
            }

            playerFaction.FactionOwnerID = playerFaction.Id;
            playerFaction.GetDataBlob<FactionInfoDB>().KnownSystems.Add(startingSystem.ID);

            var playerSpecies = SpeciesFactory.CreateFromBlueprint(startingSystem, modDataStore.Species[selectedSpeciesId]);
            playerSpecies.FactionOwnerID = playerFaction.Id;
            playerFaction.GetDataBlob<FactionInfoDB>().Species.Add(playerSpecies);

            // Setup starting colony
            var playerColony = ColonyFactory.CreateFromBlueprint(game, playerFaction, playerSpecies, startingSystem, startingBody, modDataStore.Colonies[selectedColonyId]);
            if(eleStart)
                AsteroidFactory.CreateAsteroid(startingSystem, startingBody, game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(365));

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

            _uiState.ClearGameState();
            _uiState.Game = game;
            _uiState.SetFaction(playerFaction, true);
            _uiState.SetActiveSystem(startingSystem.ManagerID);
            _uiState.Camera.CenterOnEntity(startingBody);
            _uiState.Camera.ZoomLevel = 2_245_000f;

            DebugWindow.GetInstance().SetGameEvents();

            // Initialize game windows
            TimeControl.GetInstance().SetActive();
            ToolBarWindow.GetInstance().SetActive();
            Selector.GetInstance().SetActive();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Quickstart Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}