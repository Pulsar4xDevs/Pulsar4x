using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class ModInfoUI
{
    private bool[] _isActive;
    private ModManifest[] _modManafests;
    private protected string[] _itemNames;
    
    private protected Vector2 _childSize = new Vector2(640, 200);
    
    private protected bool _showSaveDialog = false;
    private protected bool _showLoadDialog = false;
    private protected string _fileDialogPath = "";
    private protected string _fileName = "";
    private protected int _selectedIndex = -1;
    private protected ModManifest _newEmpty;
    public ModInfoUI(ModDataStore modDataStore)
    {
        _modManafests = modDataStore.ModManifests.ToArray();
        Refresh();
    }

    public void Refresh()
    {
        _isActive = new bool[_modManafests.Length];
        _itemNames = new string[_modManafests.Length];
        int i = 0;
        foreach (var modinfo in _modManafests)
        {
            _isActive[i] = false;
            _itemNames[i] = modinfo.ModName;
            

            i++;
        }
    }

    public void Display(string label)
    {
        int i = 0;
        if(ImGui.TreeNode(label))
        {

            
            ImGui.BeginChild(label,_childSize, true);
            
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);

            foreach (var item in _modManafests)
            {
                ImGui.Text(_itemNames[i]);
                ImGui.NextColumn();
                ImGui.Button("Save");
                ImGui.SameLine();
                if (ImGui.Button("SaveAs"))
                {
                    _showSaveDialog = true;
                    _fileName = _itemNames[i] + ".json";
                    _selectedIndex = i;
                }
                ImGui.SameLine();
                ImGui.Button("SaveToMemory");
                ImGui.SameLine();
                if(ImGui.Button("Edit##" + label + item.Namespace))
                {
                    _isActive[i] = !_isActive[i];
                }
                ImGui.SameLine();
                if(ImGui.Button("Delete##" + label + item.Namespace))
                {
                    RemoveAtIndex(i);
                    break;
                }
                DisplayEditorWindow(i);
                ImGui.NextColumn();
                i++;
            }
            NewItem("+##"+label);
            ImGui.SameLine();
            if (ImGui.Button("Load"))
            {
                _showLoadDialog = true;

            }
            ImGui.EndChild();
            ImGui.TreePop();
        }

        if (_showSaveDialog)
        {
            if (FileDialog.DisplaySave(ref _fileDialogPath, ref _fileName, ref _showSaveDialog))
            {
                Save();
            }
        }
        if (_showLoadDialog)
        {
            if (FileDialog.DisplayLoad(ref _fileDialogPath, ref _fileName, ref _showLoadDialog))
            {
                Load(_fileDialogPath, _fileName);
            }
        }
    }

    private static bool _showLoadFileDialogDatafiles = false;
    public void DisplayEditorWindow(int selectedIndex)
    {
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = _modManafests[selectedIndex];
        string name = selectedItem.ModName;
        string author = selectedItem.Author;
        string version = selectedItem.Version;
        string modDir = selectedItem.ModDirectory;
        string nameSpace = selectedItem.Namespace;
        int removeDatafileIndex = -1;
        
        if (ImGui.Begin("Tech Category Editor: " + name, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);
            ImGui.Text("Name: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##name" + name, ref name))
            {
                selectedItem.ModName = name;
                _itemNames[selectedIndex] = name;

            }
            
            ImGui.NextColumn();
            ImGui.Text("Author: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##author" + name, ref author))
            {
                selectedItem.Author = author;
            }
            
            ImGui.NextColumn();
            ImGui.Text("Version: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##version" + name, ref version))
            {
                selectedItem.Version = version;
            }
            
            ImGui.NextColumn();
            ImGui.Text("ModDir: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##modDir" + name, ref modDir))
            {
                selectedItem.ModDirectory = modDir;
            }
            
            ImGui.NextColumn();
            ImGui.Text("Namespace: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##namespace" + name, ref nameSpace))
            {
                selectedItem.Namespace = nameSpace;
            }
            
            ImGui.NextColumn();
            ImGui.Text("DataFiles: ");
            ImGui.NextColumn();
            int datafileIndex = 0;
            foreach (var dataFile in selectedItem.DataFiles)
            {
                ImGui.Text(dataFile);
                ImGui.SameLine();
                if (ImGui.SmallButton("X##datafileRemove" + datafileIndex))
                {
                    removeDatafileIndex = datafileIndex;
                }

                datafileIndex++;
            }
            if (ImGui.Button("Add Datafile"))
            {
                _showLoadFileDialogDatafiles = true;
            }
            if (removeDatafileIndex > -1)
            {
                selectedItem.DataFiles.RemoveAt(removeDatafileIndex);
            }
            
            if (_showLoadFileDialogDatafiles && (FileDialog.DisplayLoad(ref _fileDialogPath, ref _fileName, ref _showLoadFileDialogDatafiles)))
            {
                selectedItem.DataFiles.Add(_fileName);
            }
            ImGui.End();
        }
    }
    
    private void Save()
    {
        var selectedItem = _modManafests[_selectedIndex];

        //var modManifest = JsonConvert.DeserializeObject<ModManifest>(manifestJson);

        var serialisedItem = JsonConvert.SerializeObject(selectedItem, Formatting.Indented);
        
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(_fileDialogPath, _fileName)))
        {
            //output.Add(selectedItem);
            outputFile.Write(serialisedItem);
        }
    }

    private void Load(string path, string filename)
    {
        ModLoader modLoader = new ModLoader();
        ModDataStore modDataStore = new ModDataStore();
        modLoader.LoadModManifest(Path.Combine(path,filename), modDataStore);
        var editor = ModFileEditor.GetInstance();
        editor.Refresh(modDataStore);
    }
    
    public void NewItem(string label)
    {
        ModManifest newManifest = new ModManifest();
        
        if (ImGui.Button(label))
        {
            Array.Resize(ref _modManafests, _modManafests.Length + 1);
            _modManafests[^1] = newManifest;
            Array.Resize(ref _itemNames, _itemNames.Length + 1);
            _itemNames[^1] = "newBluprint";
            Array.Resize(ref _isActive, _isActive.Length + 1);
            _isActive[^1] = true;
        }
    }
    
    void RemoveAtIndex(int index)
    {
        int newlen = _modManafests.Length - 1;
        ModManifest[] newArray = new ModManifest[newlen];
        int i = 0;
        foreach (var item in _modManafests)
        {
            if(i == index)
            {
                index = -1;
                continue;
            }
            newArray[i] = item;
            i++;
        }

        _modManafests = newArray;
        Refresh();
    }
}