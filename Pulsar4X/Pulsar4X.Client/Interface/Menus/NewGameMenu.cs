using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;

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
    private const int DEFAULT_NUM_SYSTEMS = 10; // mirrors the engine's NewGameSettings default

    Page _currentPage = Page.SelectMods;

    // The mod list and the option catalog come from the lifecycle seam (composition-root work);
    // this window only holds the player's selections.
    IReadOnlyList<ModOption> _availableMods = Array.Empty<ModOption>();
    Dictionary<string, bool> _modEnabled = new ();
    NewGameCatalog? _catalog;

    string _selectedSpeciesId = "";
    string _selectedThemeId = "";
    string _selectedSystemId = "";
    string _selectedBodyId = "";
    string _selectedColonyId = "";
    private bool _eleStart = true;

    List<string> _enabledSystems = new ();

    int _maxSystems = DEFAULT_NUM_SYSTEMS;
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

    private void RefreshAvailableMods()
    {
        _availableMods = _uiState.Lifecycle?.GetAvailableMods() ?? Array.Empty<ModOption>();
        foreach (var mod in _availableMods)
        {
            if (!_modEnabled.ContainsKey(mod.Name))
                _modEnabled[mod.Name] = mod.EnabledByDefault;
        }
    }

    private void DisplayModsPage()
    {
        if (_availableMods.Count == 0)
            RefreshAvailableMods();

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

            foreach(var mod in _availableMods)
            {
                ImGui.TableNextColumn();
                ImGui.Text(mod.Name);
                ImGui.TableNextColumn();
                ImGui.Text(mod.Version);
                ImGui.TableNextColumn();
                ImGui.Text(mod.ManifestHash);
                var isEnabled = _modEnabled[mod.Name];
                ImGui.TableNextColumn();
                if(ImGui.Checkbox("###" + mod.Name + "-checkbox", ref isEnabled))
                {
                    _modEnabled[mod.Name] = isEnabled;
                }
            }

            ImGui.EndTable();
        }

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
            var enabledPaths = EnabledModPaths();

            // FIXME: this should show some error in the UI if no mods are selected
            if (enabledPaths.Count > 0 && _uiState.Lifecycle != null)
            {
                _catalog = _uiState.Lifecycle.LoadMods(enabledPaths);
                _selectedSpeciesId = _catalog.Species.FirstOrDefault()?.Id ?? "";
                _selectedThemeId = _catalog.Themes.FirstOrDefault()?.Id ?? "";
                _selectedColonyId = _catalog.Colonies.FirstOrDefault()?.Id ?? "";

                // Enable all the startable systems by default
                _enabledSystems.Clear();
                foreach (var system in _catalog.Systems)
                {
                    if (system.StartingBodies.Count == 0)
                        continue;
                    _enabledSystems.Add(system.Id);
                }
                _selectedSystemId = _enabledSystems.Any() ? _enabledSystems.First() : "";
                ResetSelectedBodyId();

                _currentPage = Page.ConfigureGalaxy;
            }
        }
        ImGui.EndChild();
    }

    private void DisplayConfigureGalaxy()
    {
        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight), ImGuiChildFlags.None);

        DisplayHelpers.Header("Select pre-configured Systems to include");

        if(_catalog != null && ImGui.BeginTable("SystemsSelection", 2, Styles.TableFlags))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Include");
            ImGui.TableHeadersRow();

            foreach(var system in _catalog.Systems)
            {
                ImGui.TableNextColumn();
                ImGui.Text(system.Name);
                ImGui.TableNextColumn();
                bool enabled = _enabledSystems.Contains(system.Id);
                if(ImGui.Checkbox("###" + system.Id, ref enabled))
                {
                    if(!enabled)
                        _enabledSystems.Remove(system.Id);
                    else
                        _enabledSystems.Add(system.Id);
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
        if (_catalog == null)
            return;

        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight), ImGuiChildFlags.None);

        DisplayHelpers.Header("CORPORTATION SETUP");
        ImGui.InputText("Corporation Name", _corporationNameBuffer, NAME_BUFFER_SIZE);
        ImGui.InputText("Corporation Abbreviation", _corporationAbbreviationBuffer, SHORTNAME_BUFFER_SIZE);

        var display = _catalog.Species.FirstOrDefault(s => s.Id == _selectedSpeciesId)?.Name ?? "";
        if(ImGui.BeginCombo("Select Species", display))
        {
            foreach(var species in _catalog.Species)
            {
                if(ImGui.Selectable(species.Name, _selectedSpeciesId.Equals(species.Id)))
                {
                    _selectedSpeciesId = species.Id;
                }
            }
            ImGui.EndCombo();
        }

        display = _catalog.Themes.FirstOrDefault(t => t.Id == _selectedThemeId)?.Name ?? "";
        if(ImGui.BeginCombo("Select Theme", display))
        {
            foreach(var theme in _catalog.Themes)
            {
                if(ImGui.Selectable(theme.Name, _selectedThemeId.Equals(theme.Id)))
                {
                    _selectedThemeId = theme.Id;
                }
            }
            ImGui.EndCombo();
        }

        display = _catalog.Colonies.FirstOrDefault(c => c.Id == _selectedColonyId)?.Name ?? "";
        if(ImGui.BeginCombo("Starting Corporation Configuration", display))
        {
            foreach(var colony in _catalog.Colonies)
            {
                if(ImGui.Selectable(colony.Name, _selectedColonyId.Equals(colony.Id)))
                {
                    _selectedColonyId = colony.Id;
                }
            }
            ImGui.EndCombo();
        }

        display = _catalog.Systems.FirstOrDefault(s => s.Id == _selectedSystemId)?.Name
            ?? (_selectedSystemId.Equals("random") ? "Randomly Generated" : "");
        if(ImGui.BeginCombo("Select Starting System", display))
        {
            foreach(var system in _catalog.Systems.Where(s => _enabledSystems.Contains(s.Id)))
            {
                if(ImGui.Selectable(system.Name, _selectedSystemId.Equals(system.Id)))
                {
                    _selectedSystemId = system.Id;
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

        if(!_selectedSystemId.Equals("random") && !string.IsNullOrEmpty(_selectedSystemId))
        {
            var selectedSystem = _catalog.Systems.FirstOrDefault(s => s.Id == _selectedSystemId);
            display = selectedSystem?.StartingBodies.FirstOrDefault(b => b.Id == _selectedBodyId)?.Name ?? "";
            if(selectedSystem != null && ImGui.BeginCombo("Select Starting Location", display))
            {
                foreach(var body in selectedSystem.StartingBodies)
                {
                    if(ImGui.Selectable(body.Name, _selectedBodyId.Equals(body.Id)))
                    {
                        _selectedBodyId = body.Id;
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

    private List<string> EnabledModPaths()
    {
        List<string> enabledMods = new ();

        foreach(var mod in _availableMods)
        {
            if(_modEnabled.TryGetValue(mod.Name, out bool enabled) && enabled)
            {
                enabledMods.Add(mod.ManifestPath);
            }
        }

        return enabledMods;
    }

    void CreateNewGame()
    {
        if (_uiState.Lifecycle == null)
            return;

        var request = new NewGameRequest(
            ModManifestPaths: EnabledModPaths(),
            FactionName: Utils.StringFromBytes(_corporationNameBuffer),
            FactionAbbreviation: Utils.StringFromBytes(_corporationAbbreviationBuffer),
            SpeciesId: _selectedSpeciesId,
            ColonyId: _selectedColonyId,
            SystemId: _selectedSystemId,
            BodyId: _selectedBodyId,
            EnabledSystems: _enabledSystems.ToList(),
            MaxSystems: _maxSystems,
            MasterSeed: _masterSeed,
            StartingFunds: _startingFunds,
            EleStart: _eleStart,
            SMPassword: Utils.StringFromBytes(_smPassInputbuffer),
            PlayerPassword: Utils.StringFromBytes(_passInputBuffer));

        var activation = _uiState.Lifecycle.CreateNewGame(request);
        if (activation == null) return;

        _uiState.ActivateGameUI(activation);
        IsActive = false;
        _currentPage = Page.SelectMods;
    }

    private void ResetSelectedBodyId()
    {
        var system = _catalog?.Systems.FirstOrDefault(s => s.Id == _selectedSystemId);
        _selectedBodyId = system?.StartingBodies.FirstOrDefault()?.Id ?? "";
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
        var activation = _uiState.Lifecycle?.Quickstart();
        if (activation == null) return;

        _uiState.ActivateGameUI(activation);
    }
}
