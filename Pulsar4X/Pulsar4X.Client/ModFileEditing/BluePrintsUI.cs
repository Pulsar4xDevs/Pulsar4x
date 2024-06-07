using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;
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
                //if(ImGui.Checkbox("Edit##" + label + item.UniqueID, ref _isActive[i]));
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

public class TechBlueprintUI : BluePrintsUI
{
    private int _selectedIndex = -1;
    public TechBlueprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.Tech)
    {
        var blueprints = modDataStore.Techs;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }
    
    public override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (TechBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new TechBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }
    

    public override void DisplayEditorWindow(int selectedIndex)
    {
        
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (TechBlueprint)_itemBlueprints[selectedIndex];
        string name = selectedItem.Name;
        string editStr; 
        if (ImGui.Begin("Tech Editor: " + name, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);
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
            
            _selectedIndex = Array.IndexOf(_techCatTypes, selectedItem.Category);
            if (SelectFromListWiget.Display("##cat" + selectedItem.Category, _techCatTypes, ref _selectedIndex))
            {
                selectedItem.Category = _techCatTypes[_selectedIndex];
                _selectedIndex = -1;
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
            if (TextEditWidget.Display("##df" + selectedItem.DataFormula, ref editStr))
            {
                selectedItem.DataFormula = editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("MaxLevel: ");
            ImGui.NextColumn();
            int editInt = selectedItem.MaxLevel;
            if (IntEditWidget.Display("##ml" + selectedItem.MaxLevel.ToString(), ref editInt))
            {
                selectedItem.MaxLevel = editInt;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Unlocks: ");
            ImGui.NextColumn();
            var editDic = selectedItem.Unlocks;
            if (DictEditWidget.Display("##ul" + selectedItem.Name, ref editDic, _techTypes))
            {
                
            }
        

            ImGui.End();
        }
    }
}


public class ComponentBluprintUI : BluePrintsUI
{
    private AttributeBlueprintUI? _attributeBlueprintUI;
    private List<ComponentTemplateAttributeBlueprint> _selectedAttributes;
    public ComponentBluprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.ComponentTemplate)
    {
        Dictionary<string, ComponentTemplateBlueprint> blueprints = modDataStore.ComponentTemplates;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }
    public sealed override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (ComponentTemplateBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new ComponentTemplateBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }
    

    public override void DisplayEditorWindow(int selectedIndex)
    {

        if (!_isActive[selectedIndex])
            return;
        var selectedItem = (ComponentTemplateBlueprint)_itemBlueprints[selectedIndex];
        _selectedAttributes = selectedItem.Attributes;
        
        if(_attributeBlueprintUI == null)
            _attributeBlueprintUI = new AttributeBlueprintUI(_modDataStore, selectedItem);
            
        string name = selectedItem.Name;
        string editStr;
        if (ImGui.Begin("Tech Category Editor: " + name, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 150);
            ImGui.SetColumnWidth(1, 500);
            
            ImGui.Text("Name: ");
            ImGui.NextColumn();
            editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.Name, ref editStr))
            {
                selectedItem.Name = editStr;
            }
            ImGui.NextColumn();


            ImGui.Text("ComponentType: ");
            ImGui.NextColumn();
            editStr = selectedItem.ComponentType;
            if (TextEditWidget.Display("##cmpt" + selectedItem.ComponentType, ref editStr))
            {
                selectedItem.Name = editStr;
            }
            ImGui.NextColumn();

            
            ImGui.Text("CargoType: ");
            ImGui.NextColumn();
            _editInt = Array.IndexOf(_cargoTypes, selectedItem.CargoTypeID);
            if (SelectFromListWiget.Display("##cgot" + selectedItem.CargoTypeID, _cargoTypes, ref _editInt))
            {
                selectedItem.Name = _cargoTypes[_editInt];
            }
            ImGui.NextColumn();

            
            ImGui.Text("Fomula: ");
            ImGui.NextColumn();
            var editDicf = selectedItem.Formulas;
            if (DictEditWidget.Display("##fmula", ref editDicf))
            {
                selectedItem.Formulas = editDicf;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("ResourceCosts: ");
            ImGui.NextColumn();
            var editDic = selectedItem.ResourceCost;
            if (DictEditWidget.Display("##resc", ref editDic))
            {
                selectedItem.ResourceCost = editDic;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("IndustryType: ");
            ImGui.NextColumn();
            _editInt = Array.IndexOf(_industryTypes, selectedItem.IndustryTypeID);
            if (SelectFromListWiget.Display("##indt" + selectedItem.IndustryTypeID, _industryTypes, ref _editInt))
            {
                selectedItem.IndustryTypeID = _industryTypes[_editInt];
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("MountType: ");
            ImGui.NextColumn();
            _editInt = Array.IndexOf(_mountTypes, selectedItem.MountType);
            if (SelectFromListWiget.Display("##mntt" + selectedItem.UniqueID, _mountTypes, ref _editInt))
            {

                if(Enum.TryParse(typeof(ComponentMountType), _mountTypes[_editInt], out var mtype))
                    selectedItem.MountType = (ComponentMountType)mtype;
            }
            ImGui.NextColumn();
            
            _attributeBlueprintUI.Display();

            ImGui.End();
        }
    }
}


public class ArmorBlueprintUI : BluePrintsUI
{
    public ArmorBlueprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.Armor)
    {
        Dictionary<string, ArmorBlueprint> blueprints = _modDataStore.Armor;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }

    public sealed override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (ArmorBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.UniqueID;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new ArmorBlueprint();
        newEmpty.UniqueID = "New Blueprint";
        _newEmpty = newEmpty;
    }
    

    public override void DisplayEditorWindow(int selectedIndex)
    {
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (ArmorBlueprint)_itemBlueprints[selectedIndex];
        _editStr = selectedItem.UniqueID;
        //string desc = selectedItem.Description;
        if (ImGui.Begin("Tech Category Editor: " + _editStr, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);
            ImGui.Text("Name: ");
            ImGui.NextColumn();
            if (TextEditWidget.Display("##name" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.UniqueID = _editStr;
            }
            
            ImGui.NextColumn();
            ImGui.Text("ResourceID: ");
            ImGui.NextColumn();
            _editStr = selectedItem.ResourceID;
            if (TextEditWidget.Display("##resourceid" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.ResourceID = _editStr;
            }
            
            
            ImGui.NextColumn();
            ImGui.Text("Density: ");
            ImGui.NextColumn();
            var editDoub = selectedItem.Density;
            if (DoubleEditWidget.Display("##density"+selectedItem.UniqueID, ref editDoub))
            {
                selectedItem.Density = editDoub;
            }

            ImGui.End();
        }
    }
}


public class ProcessedMateralsUI : BluePrintsUI
{
    private int _selectedIndex = -1;
    public ProcessedMateralsUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.ProcessedMaterial)
    {
        var blueprints = modDataStore.ProcessedMaterials;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }

    public sealed override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (ProcessedMaterialBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new ProcessedMaterialBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }
    

    public override void DisplayEditorWindow(int selectedIndex)
    {
        
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (ProcessedMaterialBlueprint)_itemBlueprints[selectedIndex];
        
        if (ImGui.Begin("Processed Materials Editor: " + selectedItem.Name, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0,150);
            ImGui.SetColumnWidth(1,500);
            ImGui.Text("Name: ");
            ImGui.NextColumn();

            _editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.Name = _editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("Description: ");
            ImGui.NextColumn();
            _editStr = selectedItem.Description;
            if (TextEditWidget.Display("##desc" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.Name = _editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("IndustryType: ");
            ImGui.NextColumn();
            _selectedIndex = Array.IndexOf(_industryTypes, selectedItem.IndustryTypeID);
            if (SelectFromListWiget.Display("##itype" + selectedItem.UniqueID, _industryTypes, ref _selectedIndex))
            {
                selectedItem.IndustryTypeID = _industryTypes[_selectedIndex];
                _selectedIndex = -1;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("CostFormula: ");
            ImGui.NextColumn();
            var edtdic = selectedItem.ResourceCosts;
            if (DictEditWidget.Display("##cf" + selectedItem.UniqueID, ref edtdic))
            {
                selectedItem.ResourceCosts = edtdic;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("Formula: ");
            ImGui.NextColumn();
            var editDict2 = selectedItem.Formulas;
            if (DictEditWidget.Display("##formula" + selectedItem.UniqueID, ref editDict2))
            {
                selectedItem.Formulas = editDict2;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("Output Amount: ");
            ImGui.NextColumn();
            int editInt = selectedItem.OutputAmount;
            if (IntEditWidget.Display("##out" + selectedItem.UniqueID, ref editInt))
            {
                selectedItem.OutputAmount = (ushort)editInt;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Industry Point Cost: ");
            ImGui.NextColumn();
            _editInt = (int)selectedItem.IndustryPointCosts;
            if (IntEditWidget.Display("##ip" + selectedItem.UniqueID, ref editInt))
            {
                selectedItem.IndustryPointCosts = _editInt;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Wealth Point Cost: ");
            ImGui.NextColumn();
            _editInt = selectedItem.WealthCost;
            if (IntEditWidget.Display("##wealth" + selectedItem.UniqueID, ref editInt))
            {
                selectedItem.WealthCost = (ushort)_editInt;
            }
            ImGui.NextColumn();

            ImGui.Text("Constructable Hint: ");
            ImGui.NextColumn();
            _editInt = Array.IndexOf(_constrGuiHints, selectedItem.GuiHints);
            if (SelectFromListWiget.Display("##mntt" + selectedItem.UniqueID, _constrGuiHints, ref _editInt))
            {
                if(Enum.TryParse(typeof(ConstructableGuiHints), _constrGuiHints[_editInt], out var mtype))
                    selectedItem.GuiHints = (ConstructableGuiHints)mtype;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Cargo Type: ");
            ImGui.NextColumn();
            _selectedIndex = Array.IndexOf(_cargoTypes, selectedItem.CargoTypeID);
            if (SelectFromListWiget.Display("##ctype" + selectedItem.UniqueID, _cargoTypes, ref _selectedIndex))
            {
                selectedItem.CargoTypeID = _cargoTypes[_selectedIndex];
                _selectedIndex = -1;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Volume: ");
            ImGui.NextColumn();
            var editDouble= selectedItem.VolumePerUnit;
            if (DoubleEditWidget.Display("##vol" + selectedItem.UniqueID, ref editDouble))
            {
                selectedItem.VolumePerUnit = editDouble;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Mass: ");
            ImGui.NextColumn();
            _editInt = (int)selectedItem.MassPerUnit;
            if (IntEditWidget.Display("##mass" + selectedItem.UniqueID, ref _editInt))
            {
                selectedItem.MassPerUnit = _editInt;
            }
            ImGui.NextColumn();
            
            ImGui.End();
        }
    }
}

public class MineralBlueprintUI : BluePrintsUI
{
    public MineralBlueprintUI(ModDataStore modDataStore) : base(modDataStore, ModInstruction.DataType.Mineral)
    {
        var blueprints = modDataStore.Minerals;
        _itemBlueprints = blueprints.Values.ToArray();
        Refresh();
    }

    public override void Refresh()
    {
        _itemNames = new string[_itemBlueprints.Length];
        _isActive = new bool[_itemBlueprints.Length];
        int i = 0;
        foreach (MineralBlueprint item in _itemBlueprints)
        {
            _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        var newEmpty = new MineralBlueprint();
        newEmpty.Name = "New Blueprint";
        _newEmpty = newEmpty;
    }


    public override void DisplayEditorWindow(int selectedIndex)
    {
        if(!_isActive[selectedIndex])
            return;
        var selectedItem = (MineralBlueprint)_itemBlueprints[selectedIndex];

        if (ImGui.Begin("Processed Materials Editor: " + selectedItem.Name, ref _isActive[selectedIndex]))
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 150);
            ImGui.SetColumnWidth(1, 500);
            ImGui.Text("Name: ");
            ImGui.NextColumn();
            
            
            _editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.Name = _editStr;
            }
            ImGui.NextColumn();
            
            
            ImGui.Text("Description: ");
            ImGui.NextColumn();
            _editStr = selectedItem.Description;
            if (TextEditWidget.Display("##desc" + selectedItem.UniqueID, ref _editStr))
            {
                selectedItem.Name = _editStr;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Cargo Type: ");
            ImGui.NextColumn();
            _editInt = Array.IndexOf(_cargoTypes, selectedItem.CargoTypeID);
            if (SelectFromListWiget.Display("##ctype" + selectedItem.UniqueID, _cargoTypes, ref _editInt))
            {
                selectedItem.CargoTypeID = _cargoTypes[_editInt];
                _editInt = -1;
            }
            ImGui.NextColumn();
                        
            ImGui.Text("Mass: ");
            ImGui.NextColumn();
            _editInt = (int)selectedItem.MassPerUnit;
            if (IntEditWidget.Display("##mass" + selectedItem.UniqueID, ref _editInt))
            {
                selectedItem.MassPerUnit = _editInt;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Volume: ");
            ImGui.NextColumn();
            var editDouble= selectedItem.VolumePerUnit;
            if (DoubleEditWidget.Display("##vol" + selectedItem.UniqueID, ref editDouble))
            {
                selectedItem.VolumePerUnit = editDouble;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Abundance: ");
            ImGui.NextColumn();
            var editDict = selectedItem.Abundance;
            if (DictEditWidget.Display("##Abundance" + selectedItem.UniqueID, ref editDict))
            {
                selectedItem.Abundance = editDict;
            }
            ImGui.NextColumn();
            
            ImGui.End();
        }
    }
}




public class AttributeBlueprintUI : BluePrintsUI
{
    private protected string _parentID;
    private ComponentTemplateAttributeBlueprint[] _blueprints;
    private string[] _attributeTypeNames;
    private string[] _attributeFullNames;
    public AttributeBlueprintUI(ModDataStore modDataStore, ComponentTemplateBlueprint componentBlueprint) : base(modDataStore, ModInstruction.DataType.ComponentTemplate )
    {
        _parentID = componentBlueprint.UniqueID;
        if(componentBlueprint.Attributes != null)
            _blueprints = componentBlueprint.Attributes.ToArray();
        else
            _blueprints = new ComponentTemplateAttributeBlueprint[1];
        
        Refresh();
        

    }
    


    public sealed override void Refresh()
    {
        _itemNames = new string[_blueprints.Length];
        _isActive = new bool[_blueprints.Length];
        int i = 0;
        foreach (ComponentTemplateAttributeBlueprint item in _blueprints)
        {
            if (item is null)
                _itemNames[i] = "?";
            else
                _itemNames[i] = item.Name;
            _isActive[i] = false;
            i++;
        }
        //var newEmpty = new ComponentTemplateAttributeBlueprint();
        //newEmpty.Name = "New Blueprint";
        //_newEmpty = newEmpty;
        
        var type = typeof(IComponentDesignAttribute);
        var attributeTypes = AppDomain.CurrentDomain.GetAssemblies()
                                      .SelectMany(s => s.GetTypes())
                                      .Where(p => type.IsAssignableFrom(p));
        _attributeTypeNames = new string[attributeTypes.Count()];
        _attributeFullNames = new string[attributeTypes.Count()];
        i = 0;
        foreach (var item in attributeTypes)
        {
            _attributeTypeNames[i] = item.Name;
            _attributeFullNames[i] = item.FullName;
            i++;
        }
    }

    public void Display()
    {
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0,150);
        ImGui.SetColumnWidth(1,400);

        int i = 0;
        foreach (var item in _blueprints)
        {
            ImGui.Text("Attrubutes: ");
            ImGui.NextColumn();
            DisplayEditorWindow(i);
            i++;
        }
        ImGui.Columns(0);
    }

    public override void DisplayEditorWindow(int selectedIndex)
    {

        var selectedItem = _blueprints[selectedIndex];
        if (selectedItem is null)
        {
            selectedItem = new ComponentTemplateAttributeBlueprint();
            selectedItem.Name = "newAttribute";
        }
        
        string name = selectedItem.Name;
        string editStr;
        
            ImGui.NextColumn();
            ImGui.Text("Name: ");
            ImGui.NextColumn();
            editStr = selectedItem.Name;
            if (TextEditWidget.Display("##name" + selectedItem.Name, ref editStr))
            {
                selectedItem.Name = editStr;
            }
            ImGui.NextColumn();
            
            ImGui.Text("Units: ");
            ImGui.NextColumn();
            var editIndex = Array.IndexOf(_units, selectedItem.Units);
            if (SelectFromListWiget.Display("##indt" + selectedItem.Units, _units, ref editIndex))
            {
                selectedItem.Units = _units[editIndex];
            }
            ImGui.NextColumn();

            ImGui.Text("MaxFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.MaxFormula;
            if (TextEditWidget.Display("##maxf" + selectedItem.MaxFormula, ref editStr))
            {
                selectedItem.MaxFormula = editStr;
            }
            ImGui.NextColumn();

            ImGui.Text("AttributeFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.AttributeFormula;
            if (TextEditWidget.Display("##atbf", ref editStr))
            {
                selectedItem.AttributeFormula = editStr;
            }
            ImGui.NextColumn();

            ImGui.Text("DescriptionFormula: ");
            ImGui.NextColumn();
            editStr = selectedItem.DescriptionFormula;
            if (TextEditWidget.Display("##descf", ref editStr))
            {
                selectedItem.DescriptionFormula = editStr;
            }
            ImGui.NextColumn();

           
            ImGui.Text("AttributeType: ");
            ImGui.NextColumn();
            editIndex = Array.IndexOf(_attributeFullNames, selectedItem.AttributeType);
            if (SelectFromListWiget.Display("##indt" + selectedItem.AttributeType, _attributeTypeNames, ref editIndex))
            {
                selectedItem.AttributeType = _attributeFullNames[editIndex];
            }
            ImGui.NextColumn();
    }
    public bool Equals(ComponentTemplateBlueprint componentBlueprint)
    {
        if (_parentID == componentBlueprint.UniqueID)
            return true;
        return false;
    }
}