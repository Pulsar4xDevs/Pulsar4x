using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Interface.Windows;

namespace Pulsar4X.Client.Host;

/// <summary>
/// Wires the engine-backed development tooling (debug/SM windows, which live in this host
/// executable) into the UI library's dev-tool registry. The library renders the toggles; only the
/// host knows the windows.
/// </summary>
public static class DevToolWindows
{
    public static void Register(GlobalUIState state)
    {
        state.RegisterDevTool(new DevToolRegistration(
            "debug-window", "Show Pulsar Debug Window",
            () => DebugWindow.GetInstance().ToggleActive(),
            () => DebugWindow.GetInstance().GetActive(),
            DevToolPlacement.SettingsList));

        state.RegisterDevTool(new DevToolRegistration(
            "data-viewer", "Show DataViewer Window",
            () => DataViewerWindow.GetInstance().ToggleActive(),
            () => DataViewerWindow.GetInstance().GetActive(),
            DevToolPlacement.SettingsList));

        state.RegisterDevTool(new DevToolRegistration(
            "orbit-debug", "Show Orbit Debug Lines",
            () => OrbitalDebugWindow.GetInstance().ToggleActive(),
            () => OrbitalDebugWindow.GetInstance().GetActive(),
            DevToolPlacement.SettingsList)
        {
            // Only offered when the clicked entity has a trajectory to debug.
            IsAvailable = () =>
            {
                var clicked = state.LastClickedEntity;
                if (clicked?.StarSystemId is not { } systemId)
                    return false;
                var snapshot = state.GameClient?.Galaxy.GetSystem(systemId)?.GetEntity(clicked.Id);
                return snapshot != null && (snapshot.HasView<OrbitView>() || snapshot.HasView<NewtonMoveView>());
            },
        });

        state.RegisterDevTool(new DevToolRegistration(
            "sensor-draw", "Show Sensor Draw",
            () => SensorDraw.GetInstance().ToggleActive(),
            () => SensorDraw.GetInstance().GetActive(),
            DevToolPlacement.SettingsList));

        state.RegisterDevTool(new DevToolRegistration(
            "debug-gui", "Show Pulsar GUI Debug Window",
            () => DebugGUIWindow.GetInstance().ToggleActive(),
            () => DebugGUIWindow.GetInstance().GetActive(),
            DevToolPlacement.SettingsList));

        state.RegisterDevTool(new DevToolRegistration(
            "performance-window", "Show Pulsar Performance Window",
            () => PerformanceWindow.GetInstance().ToggleActive(),
            () => PerformanceWindow.GetInstance().GetActive(),
            DevToolPlacement.SettingsList));

        state.RegisterDevTool(new DevToolRegistration(
            "damage-viewer", "DamageWindow",
            () => DamageViewerWindow.GetInstance().ToggleActive(),
            () => DamageViewerWindow.GetInstance().GetActive(),
            DevToolPlacement.SettingsList));

        state.RegisterDevTool(new DevToolRegistration(
            "blueprints-window", "Show Blueprints Window",
            () => BlueprintsWindow.GetInstance().ToggleActive(),
            () => BlueprintsWindow.GetInstance().GetActive(),
            DevToolPlacement.SettingsList));

        state.RegisterDevTool(new DevToolRegistration(
            "sm-window", "View SM debug info about a body",
            () => SMWindow.GetInstance().ToggleActive(),
            () => SMWindow.GetInstance().GetActive(),
            DevToolPlacement.Toolbar));

        state.RegisterDevTool(new DevToolRegistration(
            "sm-mode", "SM Mode",
            () =>
            {
                var panel = SMWindow.GetInstance();
                state.ActiveWindow = panel;
                panel.SetActive();
                state.ToggleGameMaster();
            },
            () => SMWindow.GetInstance().GetActive(),
            DevToolPlacement.MainMenu));

        // The debug window tracks engine game events; rehook whenever a game is created or loaded.
        state.OnGameLoaded += () => DebugWindow.GetInstance().SetGameEvents();
    }
}
