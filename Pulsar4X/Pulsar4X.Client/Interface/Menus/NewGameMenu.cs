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
    SelectScenario,
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

public class NewGameMenu : UniquePulsarGuiWindow<NewGameMenu>
{
    private const int NAME_BUFFER_SIZE = 32;
    private const int SHORTNAME_BUFFER_SIZE = 5;
    private const string DEFAULT_NAME = "United Earth Corp";
    private const string DEFAULT_ABBREVIATION = "UEC";
    private const int MIN_STARTING_FUNDS = 1_000_000;
    private const int MAX_STARTING_FUNDS = 1_000_000_000;
    private const int DEFAULT_NUM_SYSTEMS = 10; // mirrors the engine's NewGameSettings default

    Page _currentPage = Page.SelectScenario;

    // The mod list, the scenario list and the option catalog come from the lifecycle seam
    // (composition-root work); this window only holds the player's selections.
    IReadOnlyList<ModOption> _availableMods = Array.Empty<ModOption>();
    IReadOnlyList<ScenarioOption> _availableScenarios = Array.Empty<ScenarioOption>();
    Dictionary<string, bool> _modEnabled = new ();
    string _selectedScenarioId = "";
    bool _scenariosLoaded;
    bool _scenarioListFailed;
    NewGameCatalog? _catalog;

    string _selectedSpeciesId = "";
    string _selectedThemeId = "";
    string _selectedSystemId = "";
    string _selectedBodyId = "";
    string _selectedColonyId = "";
    private bool _eleStart = true;

    List<string> _enabledSystems = new ();

    // The last failure from loading mods or creating the game, shown on the page so the wizard
    // never fails silently or takes the whole client down.
    string _errorMessage = "";

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
        if(_uiState.TryGetUniqueWindow<NewGameMenu>(out var window))
        {
            return window;
        }

        return _uiState.AddUniqueWindow(new NewGameMenu());
    }

    internal override void Display()
    {
        if(!IsActive) return;

        // Without an explicit size ImGui auto-fits the first frame around the empty child regions
        // (their height is derived from the window height) and saves a tiny window to imgui.ini.
        // Open at a usable size, and never allow a window smaller than the wizard needs.
        var viewport = new Vector2(_uiState.ViewPort.Size.Width, _uiState.ViewPort.Size.Height);
        var openSize = new Vector2(Math.Min(720f, viewport.X - 40f), Math.Min(560f, viewport.Y - 40f));
        ImGui.SetNextWindowSize(openSize, ImGuiCond.Appearing);
        ImGui.SetNextWindowPos((viewport - openSize) / 2f, ImGuiCond.Appearing);
        ImGui.SetNextWindowSizeConstraints(new Vector2(Math.Min(480f, openSize.X), Math.Min(360f, openSize.Y)),
            new Vector2(float.MaxValue, float.MaxValue));
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
                case Page.SelectScenario:
                    DisplayScenarioPage();
                    break;
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
        }
        // ImGui needs End() even when Begin() returned false (collapsed or clipped window).
        Window.End();
    }

    private void DisplayError()
    {
        if (string.IsNullOrEmpty(_errorMessage))
            return;

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.35f, 0.35f, 1f));
        ImGui.TextWrapped(_errorMessage);
        ImGui.PopStyleColor();
    }

    private void RefreshAvailableMods()
    {
        _availableMods = _uiState.Lifecycle?.GetAvailableMods() ?? Array.Empty<ModOption>();
        foreach (var mod in _availableMods)
        {
            if (!_modEnabled.ContainsKey(mod.Namespace))
                _modEnabled[mod.Namespace] = mod.EnabledByDefault;
        }
    }

    private void EnsureNewGameOptions()
    {
        if (_availableMods.Count == 0)
            RefreshAvailableMods();

        if (_scenariosLoaded || _uiState.Lifecycle == null)
            return;

        try
        {
            _availableScenarios = _uiState.Lifecycle.GetAvailableScenarios();
            _scenarioListFailed = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"New game: failed to list scenarios: {ex}");
            _errorMessage = $"Could not list scenarios: {ex.Message}";
            _availableScenarios = Array.Empty<ScenarioOption>();
            _scenarioListFailed = true;
        }

        _scenariosLoaded = true;
        if (_scenarioListFailed || !string.IsNullOrEmpty(_selectedScenarioId))
            return;

        ScenarioOption? firstCompatible = null;
        ScenarioOption? firstDefault = null;
        foreach (var scenario in _availableScenarios)
        {
            if (ScenarioCompatibility.MissingMods(scenario, _modEnabled).Count > 0)
                continue;
            if (firstCompatible == null)
                firstCompatible = scenario;
            if (scenario.DefaultEnabled)
            {
                firstDefault = scenario;
                break;
            }
        }

        var pick = firstDefault ?? firstCompatible;
        if (pick != null)
            SelectScenario(pick);
    }

    private ScenarioOption? SelectedScenario()
        => _availableScenarios.FirstOrDefault(s => s.Id == _selectedScenarioId);

    private bool SelectedScenarioIsCompatible()
    {
        var scenario = SelectedScenario();
        return scenario != null && ScenarioCompatibility.MissingMods(scenario, _modEnabled).Count == 0;
    }

    private void SelectScenario(ScenarioOption next)
    {
        var previous = SelectedScenario();
        if (previous != null && previous.Id == next.Id)
            return;
        ScenarioCompatibility.ApplySelection(previous, next, _modEnabled, _availableMods);
        _selectedScenarioId = next.Id;
    }

    /// <summary>
    /// The red error for the scenario gate. A load or create failure does not start with these
    /// prefixes, so a compatible selection leaves that message alone.
    /// </summary>
    private void UpdateScenarioGate()
    {
        if (_scenarioListFailed)
            return;

        if (_availableScenarios.Count == 0)
        {
            _errorMessage = "No scenarios are installed. Add a folder with a scenarioInfo.json in a scenarios directory next to the mods.";
            return;
        }

        var scenario = SelectedScenario();
        if (scenario == null)
        {
            _errorMessage = "Select a scenario to continue.";
            return;
        }

        var missing = ScenarioCompatibility.MissingMods(scenario, _modEnabled);
        if (missing.Count > 0)
        {
            _errorMessage = "Requires: " + string.Join(", ", missing);
            return;
        }

        if (_errorMessage.StartsWith("Requires: ")
            || _errorMessage.StartsWith("No scenarios are installed.")
            || _errorMessage == "Select a scenario to continue.")
            _errorMessage = "";
    }

    private void DisplayScenarioPage()
    {
        EnsureNewGameOptions();
        UpdateScenarioGate();

        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight), ImGuiChildFlags.None);

        DisplayHelpers.Header("Select Scenario");
        DisplayError();

        foreach (var scenario in _availableScenarios)
        {
            var missing = ScenarioCompatibility.MissingMods(scenario, _modEnabled);
            bool compatible = missing.Count == 0;
            if (!compatible)
                ImGui.BeginDisabled();

            if (ImGui.Selectable(scenario.Name + "###" + scenario.Id, scenario.Id == _selectedScenarioId))
                SelectScenario(scenario);

            ImGui.Indent();
            ImGui.Text(scenario.Id);
            if (!string.IsNullOrEmpty(scenario.Description))
                ImGui.TextWrapped(scenario.Description);
            if (scenario.RequiredMods.Count == 0)
                ImGui.Text("Required mods: none");
            else
                ImGui.TextWrapped("Required mods: " + string.Join(", ", scenario.RequiredMods));
            if (!compatible)
                ImGui.Text("Requires: " + string.Join(", ", missing));
            ImGui.Unindent();

            if (!compatible)
                ImGui.EndDisabled();
            ImGui.Separator();
        }

        ImGui.EndChild();
        ImGui.BeginChild("Footer", new Vector2(0, _footerHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        CancelButton();
        ImGui.SameLine();

        float buttonX = _windowSize.X - _buttonWidth - ImGui.GetStyle().WindowPadding.X;
        ImGui.SetCursorPosX(buttonX);
        // An incompatible selection still opens the mods page (the error says why, and that
        // page blocks Next) so the player can tick the missing mod. No selection at all cannot.
        bool canContinue = SelectedScenario() != null && !_scenarioListFailed;
        if (!canContinue)
            ImGui.BeginDisabled();
        // debugnewgame is cleared on the mods page, so this only steps forward.
        if (ImGui.Button("Next", new Vector2(_buttonWidth, 0)) || (_uiState.debugnewgame && canContinue))
        {
            if (canContinue)
                _currentPage = Page.SelectMods;
        }
        if (!canContinue)
            ImGui.EndDisabled();
        ImGui.EndChild();
    }

    private void DisplayModsPage()
    {
        EnsureNewGameOptions();
        UpdateScenarioGate();

        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight), ImGuiChildFlags.None);

        DisplayHelpers.Header("Select Mods to Enable");
        var scenario = SelectedScenario();
        if (scenario != null)
            ImGui.Text("Scenario: " + scenario.Name);
        DisplayError();
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
                _modEnabled.TryGetValue(mod.Namespace, out bool isEnabled);
                ImGui.TableNextColumn();
                if(ImGui.Checkbox("###" + mod.Namespace + "-checkbox", ref isEnabled))
                {
                    _modEnabled[mod.Namespace] = isEnabled;
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
            _currentPage = Page.SelectScenario;
        }
        ImGui.SameLine();

        // Right-align the button by calculating its position
        float buttonX = _windowSize.X - _buttonWidth - ImGui.GetStyle().WindowPadding.X;
        ImGui.SetCursorPosX(buttonX);
        // Ticks changed while the table was drawn, so decide Next from the dictionary now.
        bool scenarioOk = SelectedScenarioIsCompatible();
        if (!scenarioOk)
            ImGui.BeginDisabled();
        bool next = ImGui.Button("Next", new Vector2(_buttonWidth, 0));
        if (!scenarioOk)
            ImGui.EndDisabled();
        if ((next || _uiState.debugnewgame) && scenarioOk)
        {
            _uiState.debugnewgame = false;
            var enabledPaths = EnabledModPaths();

            NewGameCatalog? catalog = null;
            if (enabledPaths.Count == 0)
            {
                _errorMessage = "Select at least one mod to continue.";
            }
            else if (_uiState.Lifecycle != null)
            {
                try
                {
                    catalog = _uiState.Lifecycle.LoadMods(enabledPaths, _selectedScenarioId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"New game: failed to load mods: {ex}");
                    _errorMessage = $"Could not load the selected mods: {ex.Message}";
                }
            }

            if (catalog != null)
            {
                _catalog = catalog;
                _errorMessage = "";
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

                if (_catalog.DefaultStart is { } start)
                {
                    if (_catalog.Species.Any(s => s.Id == start.SpeciesId))
                        _selectedSpeciesId = start.SpeciesId;
                    if (_catalog.Colonies.Any(c => c.Id == start.ColonyId))
                        _selectedColonyId = start.ColonyId;
                    if (_enabledSystems.Contains(start.SystemId))
                        _selectedSystemId = start.SystemId;
                    var selectedSystem = _catalog.Systems.FirstOrDefault(s => s.Id == _selectedSystemId);
                    if (selectedSystem != null && selectedSystem.StartingBodies.Any(b => b.Id == start.BodyId))
                        _selectedBodyId = start.BodyId;
                }

                _currentPage = Page.ConfigureGalaxy;
            }
        }
        ImGui.EndChild();
    }

    private void DisplayConfigureGalaxy()
    {
        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight), ImGuiChildFlags.None);

        DisplayError();
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

        DisplayError();
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
            if(_modEnabled.TryGetValue(mod.Namespace, out bool enabled) && enabled)
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

        var selectedScenario = SelectedScenario();
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
            PlayerPassword: Utils.StringFromBytes(_passInputBuffer),
            ScenarioId: selectedScenario?.Id,
            ScenarioManifestPath: selectedScenario?.ManifestPath);

        try
        {
            var activation = _uiState.Lifecycle.CreateNewGame(request);
            if (activation == null)
            {
                _errorMessage = "Could not create the game: the chosen system and starting location were not found.";
                return;
            }

            _uiState.ActivateGameUI(activation);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"New game: failed to create the game: {ex}");
            _errorMessage = $"Could not create the game: {ex.Message}";
            return;
        }

        _errorMessage = "";
        IsActive = false;
        _currentPage = Page.SelectScenario;
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
    /// Creates a new game instantly with default settings, bypassing the wizard.
    /// Returns a player-facing error message, or null when the game was created.
    /// </summary>
    public static string? QuickstartGame()
    {
        try
        {
            var activation = _uiState.Lifecycle?.Quickstart();
            if (activation == null)
                return "Quickstart: could not create the game.";

            _uiState.ActivateGameUI(activation);
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Quickstart: {ex}");
            return ex.Message;
        }
    }
}
