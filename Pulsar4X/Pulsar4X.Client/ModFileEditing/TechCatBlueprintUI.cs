using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class TechCatBlueprintUI : BluePrintsUI
{
    public TechCatBlueprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.TechCategory)
    {
        Dictionary<string, TechCategoryBlueprint> blueprints = _modDataStore.TechCategories;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }

    public sealed override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (TechCategoryBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new TechCategoryBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }
    
    
    public override void DisplayEditorWindow(int selectedIndex)
    {
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (TechCategoryBlueprint)_itemBlueprints[selectedIndex];
        string name = selectedItem.Name;
        string desc = selectedItem.Description;
        if (ImGui.Begin("Tech Category Editor: " + name, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);
            ImGui.Text("Name: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##name" + selectedItem.Name, ref name))
            {
                selectedItem.Name = name;
            }
            
            ImGui.NextColumn();
            ImGui.Text("Description: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##description" + selectedItem.Name, ref desc))
            {
                selectedItem.Description = desc;
            }
            ImGui.End();
        }
    }
}