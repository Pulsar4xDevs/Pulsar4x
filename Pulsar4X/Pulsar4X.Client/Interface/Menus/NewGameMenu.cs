using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client;

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

    // The mod list, the scenario list and the option catalog come from the lifecycle seam
    // (composition-root work); this window only holds the player's selections.
    IReadOnlyList<ModOption> _availableMods = Array.Empty<ModOption>();
    IReadOnlyList<ScenarioOption> _availableScenarios = Array.Empty<ScenarioOption>();
    Dictionary<string, bool> _modEnabled = new ();
    string _selectedScenarioId = "";
    bool _scenariosLoaded;
    bool _scenarioListFailed;
    NewGameCatalog? _catalog;

    // Scenario id and enabled mod paths the current _catalog was loaded for. Reloaded when either
    // differs; a failed load leaves _catalog null so Create stays disabled.
    string? _catalogScenarioId;
    List<string>? _catalogModPaths;

    // The last failure from loading mods or creating the game, shown on the page so the wizard
    // never fails silently or takes the whole client down.
    string _errorMessage = "";

    byte[] _corporationNameBuffer = Utils.BytesFromString("", NAME_BUFFER_SIZE);
    byte[] _corporationAbbreviationBuffer = Utils.BytesFromString("", SHORTNAME_BUFFER_SIZE);

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

            DisplayPage();
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

        ScenarioOption? firstSelectable = null;
        ScenarioOption? firstDefault = null;
        foreach (var scenario in _availableScenarios)
        {
            if (ScenarioCompatibility.NotInstalledMods(scenario, _availableMods).Count > 0)
                continue;
            if (firstSelectable == null)
                firstSelectable = scenario;
            if (scenario.DefaultEnabled)
            {
                firstDefault = scenario;
                break;
            }
        }

        var pick = firstDefault ?? firstSelectable;
        if (pick != null)
            SelectScenario(pick);
    }

    private ScenarioOption? SelectedScenario()
        => _availableScenarios.FirstOrDefault(s => s.Id == _selectedScenarioId);

    private void SelectScenario(ScenarioOption next)
    {
        var previous = SelectedScenario();
        if (previous != null && previous.Id == next.Id)
            return;
        ScenarioCompatibility.ApplySelection(previous, next, _modEnabled, _availableMods);
        _selectedScenarioId = next.Id;
    }

    /// <summary>
    /// The red error for the scenario list. A load or create failure does not use these
    /// messages, so a valid selection leaves that failure on screen.
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
        if (scenario == null
            || ScenarioCompatibility.NotInstalledMods(scenario, _availableMods).Count > 0)
        {
            _errorMessage = "Select a scenario to continue.";
            return;
        }

        if (_errorMessage.StartsWith("No scenarios are installed.")
            || _errorMessage == "Select a scenario to continue.")
            _errorMessage = "";
    }

    /// <summary>
    /// Reloads the catalog when the selected scenario or the enabled mod set is not what the
    /// current catalog was loaded for. A scenario change (including the first selection) refills
    /// the corporation name and abbreviation from DefaultStart; a mod toggle does not.
    /// </summary>
    private void EnsureCatalogCurrent()
    {
        var selected = SelectedScenario();
        if (selected == null
            || _scenarioListFailed
            || ScenarioCompatibility.NotInstalledMods(selected, _availableMods).Count > 0)
        {
            return;
        }

        foreach (var mod in _availableMods)
        {
            if (ScenarioCompatibility.IsRequired(selected, mod.Namespace))
                _modEnabled[mod.Namespace] = true;
        }

        var enabledPaths = EnabledModPaths();
        bool scenarioChanged = _catalogScenarioId != _selectedScenarioId;
        bool modsChanged = _catalogModPaths == null || !enabledPaths.SequenceEqual(_catalogModPaths);
        if (!scenarioChanged && !modsChanged)
            return;

        _catalogScenarioId = _selectedScenarioId;
        _catalogModPaths = new List<string>(enabledPaths);

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

        _catalog = catalog;
        if (catalog == null)
            return;

        _errorMessage = "";
        if (!scenarioChanged)
            return;

        var start = catalog.DefaultStart;
        _corporationNameBuffer = Utils.BytesFromString(start?.FactionName ?? "", NAME_BUFFER_SIZE);
        _corporationAbbreviationBuffer = Utils.BytesFromString(start?.FactionAbbreviation ?? "", SHORTNAME_BUFFER_SIZE);
    }

    private void DisplayPage()
    {
        EnsureNewGameOptions();
        UpdateScenarioGate();
        EnsureCatalogCurrent();

        ImGui.BeginChild("ScrollingRegion", new Vector2(0, _contentHeight), ImGuiChildFlags.None);

        DisplayHelpers.Header("Select Scenario");
        DisplayError();

        foreach (var scenario in _availableScenarios)
        {
            var missing = ScenarioCompatibility.NotInstalledMods(scenario, _availableMods);
            bool selectable = missing.Count == 0;
            if (!selectable)
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
            if (!selectable)
                ImGui.Text("Missing mod: " + string.Join(", ", missing) + " (not installed)");
            ImGui.Unindent();

            if (!selectable)
                ImGui.EndDisabled();
            ImGui.Separator();
        }

        var selected = SelectedScenario();
        var enabledNames = new List<string>();
        foreach (var mod in _availableMods)
        {
            if (ScenarioCompatibility.IsRequired(selected, mod.Namespace))
                _modEnabled[mod.Namespace] = true;
            if (_modEnabled.TryGetValue(mod.Namespace, out bool on) && on)
                enabledNames.Add(mod.Name);
        }

        string modsHeader = "Mods (" + enabledNames.Count + " enabled";
        if (enabledNames.Count > 0)
            modsHeader += ": " + string.Join(", ", enabledNames);
        modsHeader += ")###mods-header";

        if (ImGui.CollapsingHeader(modsHeader))
        {
            if (ImGui.BeginTable("ModsList", 4, Styles.TableFlags))
            {
                ImGui.TableNextColumn();
                ImGui.TableHeader("Mod Name");
                ImGui.TableNextColumn();
                ImGui.TableHeader("Version");
                ImGui.TableNextColumn();
                ImGui.TableHeader("Hash");
                ImGui.TableNextColumn();
                ImGui.TableHeader("Enable?");

                string requiredBy = selected == null ? "" : selected.Name;
                foreach (var mod in _availableMods)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text(mod.Name);
                    ImGui.TableNextColumn();
                    ImGui.Text(mod.Version);
                    ImGui.TableNextColumn();
                    ImGui.Text(mod.ManifestHash);
                    bool required = ScenarioCompatibility.IsRequired(selected, mod.Namespace);
                    _modEnabled.TryGetValue(mod.Namespace, out bool isEnabled);
                    if (required)
                        isEnabled = true;
                    ImGui.TableNextColumn();
                    if (required)
                        ImGui.BeginDisabled();
                    if (ImGui.Checkbox("###" + mod.Namespace + "-checkbox", ref isEnabled) && !required)
                        _modEnabled[mod.Namespace] = isEnabled;
                    if (required)
                    {
                        ImGui.EndDisabled();
                        ImGui.SameLine();
                        ImGui.Text("(required by " + requiredBy + ")");
                    }
                }

                ImGui.EndTable();
            }
        }

        EnsureCatalogCurrent();

        DisplayHelpers.Header("CORPORATION");
        ImGui.InputText("Corporation Name", _corporationNameBuffer, NAME_BUFFER_SIZE);
        ImGui.InputText("Corporation Abbreviation", _corporationAbbreviationBuffer, SHORTNAME_BUFFER_SIZE);

        ImGui.NewLine();
        DisplayHelpers.Header("GAME OPTIONS");
        ImGui.InputInt("Game Seed", ref _masterSeed);
        ImGui.SameLine();
        if (ImGui.Button("Reroll"))
            _masterSeed = RandomNumberGenerator.GetInt32(999999999);

        ImGui.EndChild();
        ImGui.BeginChild("Footer", new Vector2(0, _footerHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        CancelButton();
        ImGui.SameLine();

        float buttonX = _windowSize.X - _buttonWidth - ImGui.GetStyle().WindowPadding.X;
        ImGui.SetCursorPosX(buttonX);
        bool canCreate = selected != null
            && !_scenarioListFailed
            && ScenarioCompatibility.NotInstalledMods(selected, _availableMods).Count == 0
            && _catalog != null;
        if (!canCreate)
            ImGui.BeginDisabled();
        bool create = ImGui.Button("Create", new Vector2(_buttonWidth, 0));
        if (!canCreate)
            ImGui.EndDisabled();
        if ((create || _uiState.debugnewgame) && canCreate)
        {
            _uiState.debugnewgame = false;
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
        if (_uiState.Lifecycle == null || _catalog == null)
            return;

        try
        {
            var selectedScenario = SelectedScenario();
            var request = NewGameRequests.FromDefaultStart(
                _catalog,
                EnabledModPaths(),
                Utils.StringFromBytes(_corporationNameBuffer),
                Utils.StringFromBytes(_corporationAbbreviationBuffer),
                _masterSeed,
                selectedScenario?.ManifestPath);

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
        // Reload and refill the corporation fields the next time the window opens.
        _catalog = null;
        _catalogScenarioId = null;
        _catalogModPaths = null;
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
