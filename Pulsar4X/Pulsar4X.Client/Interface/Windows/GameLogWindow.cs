using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class GameLogWindow : UniquePulsarGuiWindow<GameLogWindow>
    {
        public HashSet<string> HidenEvents = new HashSet<string>();

        private static readonly (string Group, (string Type, string Label)[] Items)[] PauseGroups =
        {
            ("Research", new[]
            {
                ("ResearchCompleted", "Research completed"),
            }),
            ("Surveys and exploration", new[]
            {
                ("GeoSurveyCompleted", "Geological survey completed"),
                ("JumpPointSurveyCompleted", "Jump point survey completed"),
                ("JumpPointFound", "Jump point found"),
                ("NewSystemDiscovered", "New system discovered"),
            }),
            ("Orders and movement", new[]
            {
                ("WarpExited", "Warp exited"),
                ("OrdersCompleted", "Orders completed"),
            }),
            ("Economy", new[]
            {
                ("ProductionCompleted", "Production completed"),
                ("ShipRefitCompleted", "Ship refit completed"),
                ("FuelExhausted", "Fuel exhausted"),
            }),
            ("Combat", new[]
            {
                ("ShipDestroyed", "Ship destroyed"),
            }),
        };

        // Rows to highlight after the player clicks the pause reason in TimeControl.
        private PauseReasonSnapshot? _highlight;
        private bool _scrollToHighlight;

        private GameLogWindow()
        {
        }

        /// <summary>The curated label for an engine event type, or the type name when it has none.</summary>
        internal static string FriendlyLabel(string eventType)
        {
            foreach (var (_, items) in PauseGroups)
                foreach (var (type, label) in items)
                    if (type == eventType)
                        return label;
            return eventType == "OrdersHalt" ? "Order halt" : eventType;
        }

        /// <summary>Open the log with the rows behind <paramref name="reason"/> highlighted.</summary>
        internal void ShowPauseReason(PauseReasonSnapshot reason)
        {
            _highlight = reason;
            _scrollToHighlight = true;
            SetActive(true);
        }

        // The pause reason's own row, plus the other pausing rows at that instant (its "and N more").
        private bool IsHighlighted(LogEvent e)
        {
            if (_highlight == null || e.StarDate != _highlight.StarDate)
                return false;
            if (e.EventType == _highlight.EventType || e.EventType == "OrdersHalt")
                return true;
            var settings = _uiState.GameClient?.Galaxy.Faction?.EventHaltSettings;
            return settings != null && settings.TryGetValue(e.EventType, out var on) && on;
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
                DisplayPauseSettings();

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

                    bool highlighted = IsHighlighted(e);
                    ImGui.Separator();
                    if (highlighted)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1f, 0.85f, 0.3f, 1f));
                        if (_scrollToHighlight)
                        {
                            ImGui.SetScrollHereY();
                            _scrollToHighlight = false;
                        }
                    }
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
                    if (highlighted)
                        ImGui.PopStyleColor();
                }

                ImGui.Separator();
                Window.End();
            }
        }

        private void DisplayPauseSettings()
        {
            if (!ImGui.CollapsingHeader("Pause on"))
                return;

            var settings = _uiState.GameClient?.Galaxy.Faction?.EventHaltSettings;
            int factionId = _uiState.FactionId;

            foreach (var (group, items) in PauseGroups)
            {
                ImGui.TextDisabled(group);
                foreach (var (type, label) in items)
                {
                    bool halt = settings != null && settings.TryGetValue(type, out var on) && on;
                    if (ImGui.Checkbox($"{label}###halt-{type}", ref halt))
                    {
                        _uiState.GameClient?.SubmitCommandAsync(
                            new SetEventHaltCommand(factionId, type, halt));
                    }
                }
            }
        }
    }
}
