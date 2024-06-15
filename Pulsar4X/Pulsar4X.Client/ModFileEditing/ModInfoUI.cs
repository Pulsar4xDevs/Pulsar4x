using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class ModInfoUI
{
    private bool[] _isActive;
    private ModManifest[] _modManafests;
    private protected string[] _itemNames;
    
    private protected Vector2 _childSize = new Vector2(640, 200);
    
    private protected bool _showFileDialog = false;
    private protected string _fileDialogPath = "";
    private protected string _fileName = "";
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
            ImGui.Button("Save");
            ImGui.SameLine();
            if (ImGui.Button("SaveAs"))
            {
                _showFileDialog = true;
            }
            ImGui.SameLine();
            ImGui.Button("SaveToMemory");
            
            ImGui.BeginChild(label,_childSize, true);
            
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);

            foreach (var item in _modManafests)
            {
                ImGui.Text(_itemNames[i]);
                ImGui.NextColumn();

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
            NewItem("+##"+label, _newEmpty);
            ImGui.EndChild();
            ImGui.TreePop();
        }

        if (_showFileDialog)
        {
            if (FileDialog.Display(ref _fileDialogPath, ref _fileName, ref _showFileDialog))
            {
                Save();
            }
        }
    }
    
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
            if (TextEditWidget.Display("##version" + name, ref modDir))
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
            
            ImGui.End();
        }
    }
    
    private void Save()
    {
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(_fileDialogPath, _fileName)))
        {
            JArray output = new JArray();
            foreach (var bpt in _modManafests)
            {
                
                /*
                ModInstruction modInstruction = new ModInstruction();
                modInstruction.Type = _dataType;
                modInstruction.Data = bpt;

                JObject jObject = new JObject
                {
                    { "Type", modInstruction.Type.ToString() },
                    { "Payload", JObject.FromObject(modInstruction.Data) }
                };
                output.Add(jObject);
                */
            }
            //outputFile.Write(output);
        };
    }
    
    public void NewItem(string label, ModManifest newBlueprint)
    {
        if (ImGui.Button(label))
        {
            Array.Resize(ref _modManafests, _modManafests.Length + 1);
            _modManafests[^1] = newBlueprint;
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