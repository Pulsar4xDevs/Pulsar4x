using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public abstract class BluePrintsUI
{
    private protected int _selecteditem = 0;
    private protected string[] _itemNames;
    private protected Blueprint[] _itemBlueprints;
    private protected bool[] _isActive;

    public abstract void Display();
}

public class TechCatBlueprintUI : BluePrintsUI
{

    
    public TechCatBlueprintUI(SafeDictionary<string, TechCategoryBlueprint> blueprints)
    {
        _itemNames = new string[blueprints.Count];
        _itemBlueprints = new Blueprint[blueprints.Count];
        _isActive = new bool[blueprints.Count];
        int i = 0;
        foreach (var kvp in blueprints)
        {
            _itemNames[i] = kvp.Key;
            _itemBlueprints[i] = kvp.Value;
            _isActive[i] = false;
            i++;
        }


    }
    
    public override void Display()
    {
        //BorderListOptions.Begin("Tech Blueprints", _itemNames, ref _selecteditem, 200);
        //ImGui.BeginChild("");
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,200);
        ImGui.SetColumnWidth(1,100);

        int i = 0;
        foreach (TechCategoryBlueprint item in _itemBlueprints)
        {
            ImGui.Text(item.Name);
            ImGui.NextColumn();
            if(ImGui.Checkbox("Edit##"+_itemNames[i], ref _isActive[i]));
            {
            }
            DisplayEditorWindow(i);
            ImGui.NextColumn();
            i++;
        }
        
    }
    private void DisplayEditorWindow(int selectedIndex)
    {
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (TechCategoryBlueprint)_itemBlueprints[selectedIndex];
        string name = selectedItem.Name;
        string desc = selectedItem.Description;
        if (ImGui.Begin("Tech Category Editor: " + name))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,100);
            ImGui.SetColumnWidth(1,300);
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

public static class TextEditWidget
{
    private static uint _buffSize = 128;
    private static byte[] _strInputBuffer = new byte[128];
    private static string? _editingID;
    
    public static uint BufferSize
    {
        get { return _buffSize ;}
        set
        {
            _buffSize = value;
            _strInputBuffer = new byte[value];
        }
    }
    
    public static bool Display(string label, ref string text)
    {
        bool hasChanged = false;
        if(label != _editingID)
        {
            ImGui.Text(text);
            if(ImGui.IsItemClicked())
            {
                _editingID = label;
                _strInputBuffer = ImGuiSDL2CSHelper.BytesFromString(text);

            }
        }
        else
        {
            if (ImGui.InputText(label, _strInputBuffer, _buffSize, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                text = ImGuiSDL2CSHelper.StringFromBytes(_strInputBuffer);
                _editingID = null;
                hasChanged = true;
            }
        }

        return hasChanged;
    }
}

public class TechBlueprintUI : BluePrintsUI
{
    public TechBlueprintUI(IDictionary<string, TechBlueprint> blueprints)
    {
        _itemNames = new string[blueprints.Count];
        _itemBlueprints = new Blueprint[blueprints.Count];
        _isActive = new bool[blueprints.Count];
        int i = 0;
        foreach (var kvp in blueprints)
        {
            _itemNames[i] = kvp.Key;
            _itemBlueprints[i] = kvp.Value;
            _isActive[i] = false;
            i++;
        }
    }

    public override void Display()
    {
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,200);
        ImGui.SetColumnWidth(1,100);

        int i = 0;
        foreach (TechBlueprint item in _itemBlueprints)
        {
            ImGui.Text(item.Name);
            ImGui.NextColumn();
            if(ImGui.Checkbox("Edit##"+_itemNames[i], ref _isActive[i]));
            {
            }
            DisplayEditorWindow(i);
            ImGui.NextColumn();
            i++;
        }
    }
    
    public void DisplayEditorWindow(int selectedIndex)
    {
        
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (TechBlueprint)_itemBlueprints[selectedIndex];
        string name = selectedItem.Name;
        string editStr; 
        if (ImGui.Begin("Tech Category Editor: " + name))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,100);
            ImGui.SetColumnWidth(1,300);
            ImGui.Text("Name: ");
            ImGui.NextColumn();

            editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.Name, ref editStr))
            {
                selectedItem.Name = editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("Description: ");
            ImGui.NextColumn();
            editStr = selectedItem.Description;
            if (TextEditWidget.Display("##desc" + selectedItem.Description, ref editStr))
            {
                selectedItem.Name = editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("Category: ");
            ImGui.NextColumn();
            editStr = selectedItem.Category;
            if (TextEditWidget.Display("##cat" + selectedItem.Category, ref editStr))
            {
                selectedItem.Category = editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("CostFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.CostFormula;
            if (TextEditWidget.Display("##cf" + selectedItem.CostFormula, ref editStr))
            {
                selectedItem.CostFormula = editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("DataFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.DataFormula;
            if (TextEditWidget.Display("##cf" + selectedItem.DataFormula, ref editStr))
            {
                selectedItem.DataFormula = editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("MaxLevel: ");
            ImGui.NextColumn();   
            ImGui.Text("MaxLevel: "); 
            ImGui.SameLine();
            ImGui.Text(selectedItem.MaxLevel.ToString());
            ImGui.NextColumn();
            
            ImGui.Text("Unlocks: ");
            ImGui.NextColumn();
            ImGui.Text("Unlocks: "); 
            ImGui.SameLine();
            ImGui.Text(selectedItem.Unlocks.ToString());
        

            ImGui.End();
        }
    }
}


public class ComponentBluprintUI : BluePrintsUI
{
    public ComponentBluprintUI(Dictionary<string, ComponentTemplateBlueprint> blueprints)
    {
        _itemNames = new string[blueprints.Count];
        _itemBlueprints = new Blueprint[blueprints.Count];
        int i = 0;
        foreach (var kvp in blueprints)
        {
            _itemNames[i] = kvp.Key;
            _itemBlueprints[i] = kvp.Value;
            i++;
        }
    }
    
    public override void Display()
    {
        //BorderListOptions.Begin("Tech Blueprints", _itemNames, ref _selecteditem, 200);

        var selected = (ComponentTemplateBlueprint)_itemBlueprints[_selecteditem];
        ImGui.Text("Name: ");
        ImGui.SameLine();
        ImGui.Text(selected.Name);

        ImGui.Text("ComponentType: ");
        ImGui.SameLine();
        ImGui.Text(selected.ComponentType);
        
        ImGui.Text("CargoType: ");
        ImGui.SameLine();
        ImGui.Text(selected.CargoTypeID);
        
        ImGui.Text("ResourceCosts: ");
        ImGui.SameLine();
        ImGui.Text(selected.ResourceCost.ToString());
        
        ImGui.Text("IndustryType: ");
        ImGui.SameLine();
        ImGui.Text(selected.IndustryTypeID);
        
        ImGui.Text("Attributes: ");
        ImGui.SameLine();
        ImGui.Text(selected.Attributes.ToString());
        
        ImGui.Text("Formulas: ");
        ImGui.SameLine();
        ImGui.Text(selected.Formulas.ToString());
        
        ImGui.Text("MountType: ");
        ImGui.SameLine();
        ImGui.Text(selected.MountType.ToString());
        
        //BorderListOptions.End(new Vector2(400, 600));
    }
} 