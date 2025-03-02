using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Blueprints;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Client.State;
using Pulsar4X.Colonies;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.People;

namespace Pulsar4X.SDL2UI;

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

    enum gameType { Nethost, Standalone }
    int _gameTypeButtonGrp = 0;
    gameType _selectedGameType = gameType.Standalone;
    byte[] _netPortInputBuffer = new byte[8];
    string _netPortString { get { return System.Text.Encoding.UTF8.GetString(_netPortInputBuffer); } }
    int _maxSystems = 5;


    byte[] _nameInputBuffer = ImGuiSDL2CSHelper.BytesFromString("My Game", 32);
    byte[] _factionInputBuffer = ImGuiSDL2CSHelper.BytesFromString("UEF", 16);
    byte[] _passInputBuffer = ImGuiSDL2CSHelper.BytesFromString("", 16);

    byte[] _smPassInputbuffer = ImGuiSDL2CSHelper.BytesFromString("", 16);

    int _masterSeed = 12345678;

    Vector2 _contentRegion = new Vector2();
    Vector2 _windowPos = new Vector2();
    Vector2 _windowSize = new Vector2();
    float _footerHeight = 0f;
    float _contentHeight = 0f;
    float _buttonWidth = 100f;
    private NewGameMenu()
    {

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
            _windowSize = ImGui.GetWindowContentRegionMax();
            _footerHeight = ImGui.GetFrameHeightWithSpacing();

            // Calculate content area height (window height minus footer)
            _contentHeight = _windowSize.Y - _footerHeight - ImGui.GetFrameHeightWithSpacing();

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
        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight));

        DisplayHelpers.Header("New Game Options");
        ImGui.InputText("Game Name", _nameInputBuffer, 32);
        // ImGui.InputText("SM Pass", _smPassInputbuffer, 16);
        // ImGui.InputText("Password", _passInputBuffer, 16);
        //ImGui.InputInt("Max Systems", ref _maxSystems);
        ImGui.InputInt("Seed", ref _masterSeed);

        ImGui.NewLine();
        DisplayHelpers.Header("Select Mods to Enable");
        if(ImGui.BeginTable("ModsList", 3, Styles.TableFlags))
        {
            ImGui.TableNextColumn();
            ImGui.TableHeader("Mod Name");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Version");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Enable?");

            foreach(var modMetadata in ModsState.AvailableMods)
            {
                ImGui.TableNextColumn();
                ImGui.Text(modMetadata.Mod.ModName);
                ImGui.TableNextColumn();
                ImGui.Text(modMetadata.Mod.Version);
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
        ImGui.BeginChild("Footer", new Vector2(0, _footerHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

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
        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight));

        DisplayHelpers.Header("Select pre-configured Systems to include");

        if(ImGui.BeginTable("SystemsSelection", 2, Styles.TableFlags))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Enabled");
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
        ImGui.BeginChild("Footer", new Vector2(0, _footerHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

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
        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight));

        DisplayHelpers.Header("Game Setup");
        ImGui.InputText("Faction Name", _factionInputBuffer, 16);

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
        if(ImGui.BeginCombo("Starting Colony Configuration", display))
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

        if (ImGui.Checkbox("ELE start", ref _eleStart)) ;

        ImGui.EndChild();
        ImGui.BeginChild("Footer", new Vector2(0, _footerHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

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
            GameName = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer),
            MaxSystems = _maxSystems,
            SMPassword = ImGuiSDL2CSHelper.StringFromBytes(_smPassInputbuffer),
            CreatePlayerFaction = true,
            DefaultFactionName = ImGuiSDL2CSHelper.StringFromBytes(_factionInputBuffer),
            DefaultPlayerPassword = ImGuiSDL2CSHelper.StringFromBytes(_passInputBuffer),
            DefaultSolStart = true,
            MasterSeed = _masterSeed,
            EleStart = _eleStart
        };

        SpeciesBlueprint startingSpeciesBlueprint = _modDataStore.Species[_selectedSpeciesId];
        ThemeBlueprint startingThemeBlueprint = _modDataStore.Themes[_selectedThemeId];
        ColonyBlueprint startingColonyBlueprint = _modDataStore.Colonies[_selectedColonyId];
        SystemBlueprint? startingSystemBlueprint;
        SystemBodyBlueprint? startingBodyBlueprint;

        if(_selectedSystemId.Equals("random"))
        {
            // TODO: implement random generation
            return;
        }
        else
        {
            startingSystemBlueprint = _modDataStore.Systems[_selectedSystemId];
            startingBodyBlueprint = _modDataStore.SystemBodies[_selectedBodyId];
        }


        Pulsar4X.Engine.Game game = GameFactory.CreateGame(_modDataStore, gameSettings);
        game.CreatedOnGitHash = AssemblyInfo.GetGitHash(); // Save the git hash to the game
        game.LastSaveGitHash = AssemblyInfo.GetGitHash();

        // Load in the selected systems
        StarSystem? startingSystem = null;
        Entity? startingBody = null;
        foreach(var id in _enabledSystems)
        {
            var system = StarSystemFactory.LoadFromBlueprint(game, _modDataStore.Systems[id]);
            if(id.Equals(_selectedSystemId))
            {
                startingSystem = system;
                foreach(var systemBody in startingSystem.GetAllDataBlobsOfType<SystemBodyInfoDB>())
                {
                    if(systemBody.OwningEntity?.GetDefaultName()?.Equals(startingBodyBlueprint.Name) == true)
                    {
                        startingBody = systemBody.OwningEntity;
                    }
                }
            }
        }

        if(startingSystem == null || startingBody == null) return;

        // Create the players faction
        var playerFaction = FactionFactory.CreateBasicFaction(game, gameSettings.DefaultFactionName);

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
        // TODO: need to add the implementation for a random start
        // TODO: need to find a way to handle this via the mods instead of loading it here
        //var (newGameFaction, systemId) = Pulsar4X.Engine.DefaultStartFactory.LoadFromJson(game, "Data/basemod/defaultStart.json");

        // Call the game post new game initialization
        game.PostNewGameInitialization();

        _uiState.Game = game;
        _uiState.SetFaction(playerFaction, true);
        _uiState.SetActiveSystem(startingSystem.ManagerID);

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
}