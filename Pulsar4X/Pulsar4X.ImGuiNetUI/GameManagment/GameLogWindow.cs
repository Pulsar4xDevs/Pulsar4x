using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Events;
using Pulsar4X.Datablobs;

namespace Pulsar4X.SDL2UI
{

    public class GameLogWindow : PulsarGuiWindow
    {
        //private EventLog _eventLog;
        public HashSet<EventType> HidenEvents = new HashSet<EventType>();
        private GameLogWindow()
        {
          //  _eventLog = StaticRefLib.EventLog;
        }
        internal static GameLogWindow GetInstance()
        {
            GameLogWindow instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(GameLogWindow)))
                instance = new GameLogWindow();
            else
            {
                instance = (GameLogWindow)_uiState.LoadedWindows[typeof(GameLogWindow)];

            }

            return instance;
        }

        internal override void Display()
        {
            if (IsActive)
            {
                System.Numerics.Vector2 size = new System.Numerics.Vector2(800, 600);
                System.Numerics.Vector2 pos = new System.Numerics.Vector2(0, 0);
                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
                if (ImGui.Begin("GameLog", ref IsActive))
                {

                    //ImGui.BeginChild("LogChild", new System.Numerics.Vector2(800, 300), true);
                    ImGui.Columns(5, "Events", true);
                    ImGui.SetColumnWidth(0, 164);
                    ImGui.SetColumnWidth(1, 128);
                    ImGui.SetColumnWidth(2, 128);
                    ImGui.SetColumnWidth(3, 128);
                    ImGui.SetColumnWidth(4, 240);

                    ImGui.Text("DateTime");
                    ImGui.NextColumn();
                    if (ImGui.SmallButton("Type")) //ImGui.Text("Type");
                        GameLogSettingsWindow.GetInstance().ToggleActive();
                    ImGui.NextColumn();
                    ImGui.Text("Faction");
                    ImGui.NextColumn();
                    ImGui.Text("Entity");
                    ImGui.NextColumn();
                    ImGui.Text("Event Message");
                    ImGui.NextColumn();



                    // foreach (var gameEvent in _eventLog.GetAllEvents())
                    // {
                    //     if (HidenEvents.Contains(gameEvent.EventType))
                    //         continue;//skip this event if it's hidden.

                    //     var entity = gameEvent.Entity;
                    //     string entityStr = "N/A";
                    //     if (gameEvent.Entity != null)
                    //         entityStr = gameEvent.EntityName;
                    //     string factionStr = "";
                    //     if (gameEvent.Faction != null)
                    //         if (gameEvent.Faction.HasDataBlob<NameDB>())
                    //             factionStr = gameEvent.Faction.GetDataBlob<NameDB>().DefaultName;
                    //         else
                    //             factionStr = gameEvent.Faction.Guid.ToString();
                    //     string typStr = gameEvent.EventType.ToString();
                    //     ImGui.Separator();
                    //     ImGui.Text(gameEvent.Time.ToString());
                    //     ImGui.NextColumn();
                    //     ImGui.Text(typStr);
                    //     ImGui.NextColumn();
                    //     ImGui.Text(factionStr);
                    //     ImGui.NextColumn();
                    //     ImGui.Text(entityStr);
                    //     if (ImGui.IsItemHovered() && entity != null)
                    //         ImGui.SetTooltip(entity.Guid.ToString());
                    //     ImGui.NextColumn();
                    //     ImGui.TextWrapped(gameEvent.Message);

                    //     ImGui.NextColumn();


                    // }
                    ImGui.Separator();

                }
            }
        }
    }


    public class GameLogSettingsWindow : PulsarGuiWindow
    {
        private FactionInfoDB _facInfo;
        private HashSet<EventType> _hidenEvents;
        private GameLogSettingsWindow()
        {
            _facInfo = _uiState.Faction.GetDataBlob<FactionInfoDB>();
            _hidenEvents = GameLogWindow.GetInstance().HidenEvents;
        }
        internal static GameLogSettingsWindow GetInstance()
        {
            GameLogSettingsWindow instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(GameLogSettingsWindow)))
                instance = new GameLogSettingsWindow();
            else
            {
                instance = (GameLogSettingsWindow)_uiState.LoadedWindows[typeof(GameLogSettingsWindow)];

            }

            return instance;
        }

        internal override void Display()
        {
            if (IsActive)
            {
                System.Numerics.Vector2 size = new System.Numerics.Vector2(264, 600);
                System.Numerics.Vector2 pos = new System.Numerics.Vector2(0, 0);
                ImGui.SetNextWindowSize(size, ImGuiCond.Always);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);


                if (ImGui.Begin("Event Settings", ref IsActive))
                {
                    ImGui.Columns(3);
                    ImGui.SetColumnWidth(0, 164);
                    ImGui.Text("Type");
                    ImGui.NextColumn();
                    ImGui.SetColumnWidth(1, 38);
                    ImGui.Text("Halts");
                    ImGui.NextColumn();
                    ImGui.SetColumnWidth(2, 38);
                    ImGui.Text("Hide");
                    ImGui.Separator();
                    ImGui.NextColumn();

                    foreach (EventType etype in EventType.GetValues(typeof(EventType)))
                    {
                        string typestr = etype.ToString();

                        bool halts = false;
                        if (_facInfo.HaltsOnEvent.ContainsKey(etype))
                            halts = _facInfo.HaltsOnEvent[etype];
                        bool isHidden = _hidenEvents.Contains(etype);

                        ImGui.Text(typestr);
                        ImGui.NextColumn();

                        if (ImGui.Checkbox("##halt" + typestr, ref halts))
                        {
                            _facInfo.HaltsOnEvent[etype] = halts;
                        }
                        ImGui.NextColumn();
                        //ImGui.SameLine();
                        if (ImGui.Checkbox("##hidden" + typestr, ref isHidden))
                        {
                            if (isHidden)
                                _hidenEvents.Add(etype);
                            else
                                _hidenEvents.Remove(etype);


                        }
                        ImGui.NextColumn();
                    }
                    ImGui.Separator();

                }
            }
        }
    }
}