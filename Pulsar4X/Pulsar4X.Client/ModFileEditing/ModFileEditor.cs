using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class ModFileEditor : PulsarGuiWindow
{
    private SafeDictionary<string, TechCategoryBlueprint > _techCategoryBlueprint;
    private int _selectedTechCat = 0;
    private string[] _techCatNames;
    private TechCategoryBlueprint[] _techCatBluprints;
    
    
    private Dictionary<string, TechBlueprint> _startingTechBlueprints;
    private int _selectedTech = 0;
    private string[] _techNames;
    private TechBlueprint[] _techBlueprints;
    
    private ModFileEditor()
    {

    }
    internal static ModFileEditor GetInstance()
    {
        ModFileEditor instance;
        if (!_uiState.LoadedWindows.ContainsKey(typeof(ModFileEditor)))
        {
            instance = new ModFileEditor();
            instance.refresh();
        }
        else
        {
            instance = (ModFileEditor)_uiState.LoadedWindows[typeof(ModFileEditor)];
        }
        return instance;
    }

    void refresh()
    {
        _techCategoryBlueprint = _uiState.Game.TechCategories;
        _techCatNames = new string[_techCategoryBlueprint.Count];
        _techCatBluprints = new TechCategoryBlueprint[_techCategoryBlueprint.Count];
        

        
        int i = 0;
        foreach (var kvp in _techCategoryBlueprint)
        {
            _techCatNames[i] = kvp.Key;
            _techCatBluprints[i] = kvp.Value;
            i++;
        }
        
        
        _startingTechBlueprints = _uiState.Game.StartingGameData.Techs;
        _techNames = new string[_startingTechBlueprints.Count];
        _techBlueprints = new TechBlueprint[_startingTechBlueprints.Count];

        i = 0;
        foreach (var kvp in _startingTechBlueprints)
        {
            _techNames[i] = kvp.Key;
            _techBlueprints[i] = kvp.Value;
            i++;
        }
    }

    
    internal override void Display()
    {
        
        if (IsActive)
        {
            if (ImGui.Begin("Debug GUI Window", ref IsActive))
            {
                BorderListOptions.Begin("Tech Categores", _techCatNames, ref _selectedTechCat, 200);
                var selectedcat = _techCatBluprints[_selectedTechCat];
                
                ImGui.Text("Name: "); 
                ImGui.SameLine();
                ImGui.Text(selectedcat.Name);
                
                ImGui.Text("Description: "); 
                ImGui.SameLine();
                ImGui.Text(selectedcat.Description);
                
                BorderListOptions.End(new Vector2(400, 600));
                
                ImGui.NewLine();

                BorderListOptions.Begin("Tech Blueprints", _techNames, ref _selectedTech, 200);

                var selectedTechBnt = _techBlueprints[_selectedTech];
                
                ImGui.Text("Name: "); 
                ImGui.SameLine();
                ImGui.Text(selectedTechBnt.Name);
                
                ImGui.Text("Description: "); 
                ImGui.SameLine();
                ImGui.Text(selectedTechBnt.Description);
                
                ImGui.Text("Category: "); 
                ImGui.SameLine();
                ImGui.Text(selectedTechBnt.Category);
                
                ImGui.Text("CostFormula: "); 
                ImGui.SameLine();
                ImGui.Text(selectedTechBnt.CostFormula);
                
                ImGui.Text("DataFormula: "); 
                ImGui.SameLine();
                ImGui.Text(selectedTechBnt.DataFormula);
                
                ImGui.Text("MaxLevel: "); 
                ImGui.SameLine();
                ImGui.Text(selectedTechBnt.MaxLevel.ToString());
                
                ImGui.Text("Unlocks: "); 
                ImGui.SameLine();
                //ImGui.Text(selected.);
                
                BorderListOptions.End(new Vector2(400, 600));

            }

            ImGui.End();
        }
    }
}