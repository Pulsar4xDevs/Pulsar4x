using System;
using System.IO;
using ImGuiNET;
using ImGuiSDL2CS;

namespace Pulsar4X.SDL2UI.ModFileEditing;


/*TODO:
 *Conformation Dialog if overwriting.
 *Ability to filter by file extension
 *Proper colomn sizing
 *Filter by colomns
 */

public static class FileDialog
{
    public enum SaveOrLoad
    {
        Save,
        Load
    }
    private static byte[] _strInputBuffer = new byte[128];
    private static string _pathString;
    private static string _curDir = Directory.GetCurrentDirectory();
    private static int _selectedIndex = -1;
    private static int _i = 0;
    private static bool _b = false;
    private static SaveOrLoad DialogType = SaveOrLoad.Save;

    public static bool DisplaySave(ref string path, ref string fileName, ref bool IsActive)
    {
        DialogType = SaveOrLoad.Save;
        return Display(ref path, ref fileName, ref IsActive);
    }

    public static bool DisplayLoad(ref string path, ref string fileName, ref bool IsActive)
    {
        DialogType = SaveOrLoad.Load;
        return Display(ref path, ref fileName, ref IsActive);
    }
    
    private static bool Display(ref string path, ref string fileName, ref bool IsActive)
    {
        bool isok = false;
        if (string.IsNullOrEmpty(path))
            _pathString = _curDir;
        else
            _pathString = path;

        ImGui.Begin("File Dialog", ref IsActive);
        ImGui.Text("Name:"); 
        ImGui.SameLine();
        if (ImGui.InputText("##Name", _strInputBuffer, 128))
        {
            fileName = ImGuiSDL2CSHelper.StringFromBytes(_strInputBuffer);
        }
        
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,128);


        if (ImGui.Button("Docs"))
        {
            _pathString = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            
        }

        if (ImGui.Button("Desktop"))
        {
            _pathString = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }
        
        //this is Editor specific TODO: add a way to add specific dir to the LH colomn
        if (ImGui.Button("Data/basemod"))
        {
            _pathString = "Data/basemod";
        }

        //this is Editor specific TODO: add a way to add specific dir to the LH colomn
        if (ImGui.Button("GameEngine/Data/basemod"))
        {
            var dir = new DirectoryInfo(_curDir);
            while (dir.Name != "Pulsar4X")
            {
                dir = Directory.GetParent(dir.FullName);
            }
            _pathString = Path.Combine(dir.FullName, "GameEngine/Data/basemod");
        }
        
        
        ImGui.NextColumn();
        
        ImGui.BeginTable("table", 4);
        ImGui.TableNextColumn();
        ImGui.TableHeader("Name");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Size");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Type");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Modified");
        ImGui.TableNextColumn();

        if (ImGui.Selectable("..", _b, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
        {
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _pathString = Directory.GetParent(_pathString).FullName;
            }
        }
        
        var dirs = Directory.EnumerateDirectories(_pathString);
        _i = 0;
        foreach (var dir in dirs)
        {
            DirectoryInfo di = new DirectoryInfo(dir);
            
            _b = _i == _selectedIndex;
            if (ImGui.Selectable(di.Name, _b, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
            {
                _selectedIndex = _i;
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    _pathString = di.FullName;
                }
            }
            
            ImGui.TableNextColumn();
            
            ImGui.Text("");
            ImGui.TableNextColumn();
            
            ImGui.Text(di.Extension);
            ImGui.TableNextColumn();
            
            ImGui.Text(di.LastWriteTime.ToString());
            ImGui.TableNextColumn();
            _i++;
        }
        
        var files = Directory.EnumerateFiles(_pathString);
        foreach (var file in files)
        {

            FileInfo fi = new FileInfo(file);
            
            _b = _i == _selectedIndex;
            if (ImGui.Selectable(fi.Name, _b, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
            {
                _selectedIndex = _i;
                _strInputBuffer = ImGuiSDL2CSHelper.BytesFromString(fi.Name);
                fileName = fi.Name;
                
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    isok = true;
                    IsActive = false;
                }
            }
            _i++;
            ImGui.TableNextColumn();
            
            ImGui.Text(fi.Length.ToString());
            ImGui.TableNextColumn();
            
            ImGui.Text(fi.Extension);
            ImGui.TableNextColumn();
            
            ImGui.Text(fi.LastWriteTime.ToString());
            ImGui.TableNextColumn();
            
        }
        ImGui.EndTable();
        
        ImGui.Columns(1);
        if (DialogType == SaveOrLoad.Load)
        {
            if (ImGui.Button("Load"))
            {
                isok = true;
                IsActive = false;
            }
        }
        else
        {
            if (ImGui.Button("Save"))
            {
                isok = true;
                IsActive = false;
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            IsActive = false;
            isok = false;
        }
        
        ImGui.End();
        path = _pathString;
        return isok;
    }
}