using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI;

public class GameLogWindow : PulsarGuiWindow
{
    private EventLog _eventLog;
    
    private GameLogWindow()
    {
        _eventLog = StaticRefLib.EventLog;
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
                if(ImGui.SmallButton("Type")) //ImGui.Text("Type");
                    GameLogSettingsWindow.GetInstance().ToggleActive();
                ImGui.NextColumn();
                ImGui.Text("Faction");
                ImGui.NextColumn();
                ImGui.Text("Entity");
                ImGui.NextColumn();
                ImGui.Text("Event Message");
                ImGui.NextColumn();
                
                
                
                foreach (var gameEvent in _eventLog.GetAllEvents())
                {

                    var entity = gameEvent.Entity;
                    string entityStr = "N/A";
                    if (gameEvent.Entity != null)
                        entityStr = gameEvent.EntityName;
                    string factionStr = "";
                    if (gameEvent.Faction != null)
                        if (gameEvent.Faction.HasDataBlob<NameDB>())
                            factionStr = gameEvent.Faction.GetDataBlob<NameDB>().DefaultName;
                        else
                            factionStr = gameEvent.Faction.Guid.ToString();
                    string typStr = gameEvent.EventType.ToString();
                    ImGui.Separator();
                    ImGui.Text(gameEvent.Time.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(typStr);
                    ImGui.NextColumn();
                    ImGui.Text(factionStr);
                    ImGui.NextColumn();
                    ImGui.Text(entityStr);
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip(entity.Guid.ToString());
                    ImGui.NextColumn();
                    ImGui.TextWrapped(gameEvent.Message);

                    ImGui.NextColumn();


                }
                ImGui.Separator();
                
            }
        }
    }
}


public class GameLogSettingsWindow : PulsarGuiWindow
{
    private FactionInfoDB _facInfo;
    private GameLogSettingsWindow()
    {
        _facInfo = _uiState.Faction.GetDataBlob<FactionInfoDB>();
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
            System.Numerics.Vector2 size = new System.Numerics.Vector2(224, 600);
            System.Numerics.Vector2 pos = new System.Numerics.Vector2(0, 0);
            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
            if (ImGui.Begin("Event Settings", ref IsActive))
            {
                foreach (EventType etype in EventType.GetValues(typeof(EventType)))
                {
                    bool halts = false;
                    if (_facInfo.HaltsOnEvent.ContainsKey(etype))
                        halts = _facInfo.HaltsOnEvent[etype];

                    ImGui.Checkbox(etype.ToString(), ref halts);
                }
                ImGui.Separator();
                
            }
        }
    }
}