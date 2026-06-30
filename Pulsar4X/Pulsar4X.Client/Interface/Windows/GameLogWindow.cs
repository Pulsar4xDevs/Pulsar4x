using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class GameLogWindow : UniquePulsarGuiWindow<GameLogWindow>
    {
        public HashSet<string> HidenEvents = new HashSet<string>();

        private GameLogWindow()
        {
        }

        internal static GameLogWindow GetInstance()
        {
            if(_uiState.TryGetUniqueWindow<GameLogWindow>(out var window))
            {
                return window;
            }

            return _uiState.AddUniqueWindow(new GameLogWindow());
        }

        internal override void Display()
        {
            if (!IsActive)
            {
                return;
            }

            // Set window size and position
            System.Numerics.Vector2 size = new System.Numerics.Vector2(800, 600);
            System.Numerics.Vector2 pos = new System.Numerics.Vector2(0, 0);
            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

            if (Window.Begin("GameLog", ref IsActive))
            {
                var events = _uiState.GameClient?.Galaxy.EventLog;
                if (events == null || events.Count == 0)
                {
                    ImGui.Text("No events available.");
                    Window.End();
                    return;
                }

                // Display the event count
                ImGui.Text($"Number of events: {events.Count}");

                ImGui.Columns(5, "Events", true);
                ImGui.SetColumnWidth(0, 164);
                ImGui.SetColumnWidth(1, 128);
                ImGui.SetColumnWidth(2, 128);
                ImGui.SetColumnWidth(3, 128);
                ImGui.SetColumnWidth(4, 240);

                ImGui.Text("DateTime");
                ImGui.NextColumn();
                ImGui.Text("Type");
                ImGui.NextColumn();
                ImGui.Text("Faction");
                ImGui.NextColumn();
                ImGui.Text("Entity");
                ImGui.NextColumn();
                ImGui.Text("Event Message");
                ImGui.NextColumn();

                foreach (var e in events)
                {
                    if (HidenEvents.Contains(e.EventType))
                        continue;

                    ImGui.Separator();
                    ImGui.Text(e.StarDate.ToString(_uiState.GameSettings.GetDateTimeFormat()));
                    ImGui.NextColumn();
                    ImGui.Text(e.EventType);
                    ImGui.NextColumn();
                    ImGui.Text(e.FactionName ?? "");
                    ImGui.NextColumn();
                    ImGui.Text(e.EntityName ?? "N/A");
                    ImGui.NextColumn();
                    ImGui.TextWrapped(e.Message);
                    ImGui.NextColumn();
                }

                ImGui.Separator();
                Window.End();
            }
        }
    }
}
