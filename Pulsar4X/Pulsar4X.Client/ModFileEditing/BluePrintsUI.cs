using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public abstract class BluePrintsUI
{
    private protected int _selecteditem = 0;
    private protected string[] _itemNames;
    private protected Blueprint[] _itemBlueprints;
    private protected bool[] _isActive;
    private protected ModDataStore _modDataStore;
    private protected string[] _cargoTypes;
    private protected string[] _techCatTypes;
    private protected string[] _techTypes;
    private protected string[] _industryTypes;
    private protected string[] _units;
    private protected Blueprint _newEmpty;
    private protected string _editStr;
    private protected int _editInt;
    private protected string[] _constrGuiHints;
    private protected string[] _mountTypes;
    private protected string[] _guiHints;

    private protected Vector2 _childSize = new Vector2(640, 200);

    private protected bool _showFileDialog = false;
    private protected string _fileDialogPath = "";
    private protected string _fileName = "";
    private protected ModInstruction.DataType _dataType;
    
    protected BluePrintsUI(ModDataStore modDataStore, ModInstruction.DataType dataType)
    {
        _modDataStore = modDataStore;
        _cargoTypes = modDataStore.CargoTypes.Keys.ToArray();
        _techCatTypes = modDataStore.TechCategories.Keys.ToArray();
        _techTypes = modDataStore.Techs.Keys.ToArray();
        _industryTypes = modDataStore.IndustryTypes.Keys.ToArray();
        _dataType = dataType;
        
        _units = new string[9];
        _units[0] = "";
        _units[1] = "KJ";
        _units[2] = "KW";
        _units[3] = "m^2";
        _units[4] = "nm";
        _units[5] = "kg";
        _units[6] = "m";
        _units[7] = "N";
        _units[8] = "m/s";
        
        _mountTypes = Enum.GetNames(typeof(ComponentMountType));
        _constrGuiHints = Enum.GetNames(typeof(ConstructableGuiHints));
        _guiHints = Enum.GetNames(typeof(GuiHint));
    }

    public abstract void Refresh();

    private void Save()
    {
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(_fileDialogPath, _fileName)))
        {
            JArray output = new JArray();
            foreach (var bpt in _itemBlueprints)
            {
                ModInstruction modInstruction = new ModInstruction();
                modInstruction.Type = _dataType;
                modInstruction.Data = bpt;

                JObject jObject = new JObject
                {
                    { "Type", modInstruction.Type.ToString() },
                    { "Payload", JObject.FromObject(modInstruction.Data) }
                };
                output.Add(jObject);
            }
            outputFile.Write(output);
        };
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

            foreach (var item in _itemBlueprints)
            {
                ImGui.Text(_itemNames[i]);
                ImGui.NextColumn();

                if(ImGui.Button("Edit##" + label + item.UniqueID))
                {
                    _isActive[i] = !_isActive[i];
                }
                ImGui.SameLine();
                if(ImGui.Button("Delete##" + label + item.UniqueID))
                {
                    removeAtIndex(i);
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

    public abstract void DisplayEditorWindow(int index);

    public void NewItem(string label, Blueprint newBlueprint)
    {
        if (ImGui.Button(label))
        {
            Array.Resize(ref _itemBlueprints, _itemBlueprints.Length + 1);
            _itemBlueprints[^1] = newBlueprint;
            Array.Resize(ref _itemNames, _itemNames.Length + 1);
            _itemNames[^1] = "newBluprint";
            Array.Resize(ref _isActive, _isActive.Length + 1);
            _isActive[^1] = true;
        }
    }

    void removeAtIndex(int index)
    {
        int newlen = _itemBlueprints.Length - 1;
        Blueprint[] newArray = new Blueprint[newlen];
        int i = 0;
        foreach (var item in _itemBlueprints)
        {
            if(i == index)
            {
                index = -1;
                continue;
            }
            newArray[i] = item;
            i++;
        }

        _itemBlueprints = newArray;
        Refresh();
    }
}