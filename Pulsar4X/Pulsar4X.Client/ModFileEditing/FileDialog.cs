using ImGuiNET;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public static class FileDialog
{
    public enum SaveOrLoad
    {
        Save,
        Load
    }
    private static byte[] _strInputBuffer = new byte[128];
    private static string _pathString;
    
    public static SaveOrLoad DialogType = SaveOrLoad.Save;
    
    public static void Display(string path)
    {
        _pathString = path;
        ImGui.Begin("File Dialog");
        ImGui.Text("Name:"); 
        ImGui.SameLine();
        ImGui.InputText("##Name", _strInputBuffer, 128);
        
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,128);
        
        ImGui.NextColumn();
        
        ImGui.BeginTable("", 4);
        
        ImGui.TableHeader("Name");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Size");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Type");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Modified");
        ImGui.TableNextColumn();

        var files = System.IO.Directory.EnumerateFiles(_pathString);
        foreach (var file in files)
        {
            
        }
        
        ImGui.EndTable();
        
        
        ImGui.End();
    }
}