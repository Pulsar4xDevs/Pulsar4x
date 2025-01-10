using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Events;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;

namespace Pulsar4X.SDL2UI
{
    public class GameLogWindow : PulsarGuiWindow
    {
        IEventLog _factionEventLog;

        public HashSet<EventType> HidenEvents = new HashSet<EventType>();

        private GameLogWindow()
        {
            InitializeEventLog();

            // Subscribe to the event
            _uiState.OnFactionChanged += OnFactionChanged;
        }

        private void InitializeEventLog()
        {
            if (_uiState.Faction == null)
            {
                Debug.WriteLine("GameLogWindow: _uiState.Faction is null. Unable to initialize event log.");
                _factionEventLog = null;
                return;
            }

            var factionInfo = _uiState.Faction.GetDataBlob<FactionInfoDB>();
            if (factionInfo == null)
            {
                Debug.WriteLine("GameLogWindow: FactionInfoDB is null. Unable to initialize event log.");
                _factionEventLog = null;
                return;
            }

            _factionEventLog = factionInfo.EventLog;
            Debug.WriteLine("GameLogWindow: Event log initialized.");
        }

        private void OnFactionChanged(GlobalUIState uIState)
        {
            Debug.WriteLine("GameLogWindow: Faction changed. Reinitializing event log.");
            InitializeEventLog();
        }

        internal static GameLogWindow GetInstance()
        {
            GameLogWindow instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(GameLogWindow)))
            {
                instance = new GameLogWindow();
                Debug.WriteLine("GameLogWindow: Instance created.");
            }
            else
            {
                instance = (GameLogWindow)_uiState.LoadedWindows[typeof(GameLogWindow)];
                Debug.WriteLine("GameLogWindow: Instance retrieved from LoadedWindows.");
            }

            return instance;
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
                if (_factionEventLog == null)
                {
                    ImGui.Text("Event log is null.");
                    Window.End();
                    return;
                }

                // Check for valid method or property for retrieving events
                var events = _factionEventLog.GetEvents(); // Make sure GetEvents() is a valid method
                if (events == null || !events.Any())
                {
                    ImGui.Text("No events available.");
                    Window.End();
                    return;
                }

                // Display the event count
                ImGui.Text($"Number of events: {events.Count()}");

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

                    string entityStr = "N/A";
                    int eid = e.EntityId ?? -1;
                    if (eid != -1 && _uiState.Game.GlobalManager.TryGetGlobalEntityById(eid, out var entity))
                    {
                        entityStr = entity.GetName(_uiState.Faction.Id);
                    }
                    string factionStr = "";
                    int id = e.FactionId ?? -1;
                    if (id != -1)
                    {
                        factionStr = _uiState.Game.Factions[id].GetFactionName();
                    }

                    string typStr = e.EventType.ToString();
                    ImGui.Separator();
                    ImGui.Text(e.StarDate.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(typStr);
                    ImGui.NextColumn();
                    ImGui.Text(factionStr);
                    ImGui.NextColumn();
                    ImGui.Text(entityStr);
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
