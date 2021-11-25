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
            if (ImGui.Begin("GameLog", ref IsActive))
            {

                //ImGui.BeginChild("LogChild", new System.Numerics.Vector2(800, 300), true);
                ImGui.Columns(5, "Events", true);
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
            if (ImGui.Begin("Event Settings", ref IsActive))
            {

                //ImGui.BeginChild("LogChild", new System.Numerics.Vector2(800, 300), true);
                ImGui.Columns(2, "Events", true);
                ImGui.Text("Type");
                ImGui.NextColumn();
                ImGui.Text("Halts");
                ImGui.NextColumn();

                foreach (var item in _facInfo.HaltsOnEvent)
                {
                    var etype = item.Key;
                    var halts = item.Value;
                    
                    ImGui.Separator();
                    ImGui.Text(etype.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(halts.ToString());
                    ImGui.NextColumn();

                }
                ImGui.Separator();
                
            }
        }
    }
}