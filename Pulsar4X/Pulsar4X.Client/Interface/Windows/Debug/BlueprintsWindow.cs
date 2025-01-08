using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Extensions;
using Pulsar4X.SDL2UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Pulsar4X.Client.Interface.Windows;

public class BlueprintsWindow : PulsarGuiWindow
{

    private string _selectedBlueprintId = "";
    private Blueprint? _selectedBlueprint = null;

    public static BlueprintsWindow GetInstance()
    {
        BlueprintsWindow instance;
        if (!_uiState.LoadedWindows.ContainsKey(typeof(BlueprintsWindow)))
        {
            instance = new BlueprintsWindow();
        }
        else
        {
            instance = (BlueprintsWindow)_uiState.LoadedWindows[typeof(BlueprintsWindow)];
        }

        return instance;
    }

    private void DisplayBlueprintCategory(string label, List<string> items)
    {
        if(ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.CollapsingHeader | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.OpenOnArrow))
        {
            foreach(var template in items.OrderBy(k => k))
            {
                if(ImGui.Selectable(template, _selectedBlueprintId.Equals(template), ImGuiSelectableFlags.AllowDoubleClick))
                {
                    _selectedBlueprintId = template;
                    _selectedBlueprint = FindBlueprint(_selectedBlueprintId);
                }
            }
        }
    }

    internal override void Display()
    {
        if(!IsActive) return;

        if(Window.Begin("Blueprints Window"))
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("BlueprintListSelection", new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y), true))
            {
                DisplayHelpers.Header("Blueprints", "Select a blueprint to view details.");

                DisplayBlueprintCategory("Armor", _uiState.Game.StartingGameData.Armor.Keys.ToList());
                DisplayBlueprintCategory("Cargo Types", _uiState.Game.StartingGameData.CargoTypes.Keys.ToList());
                DisplayBlueprintCategory("Colonies", _uiState.Game.StartingGameData.Colonies.Keys.ToList());
                DisplayBlueprintCategory("Component Templates", _uiState.Game.StartingGameData.ComponentTemplates.Keys.ToList());
                DisplayBlueprintCategory("Gas", _uiState.Game.StartingGameData.AtmosphericGas.Keys.ToList());
                DisplayBlueprintCategory("Industry Types", _uiState.Game.StartingGameData.IndustryTypes.Keys.ToList());
                DisplayBlueprintCategory("Minerals", _uiState.Game.StartingGameData.Minerals.Keys.ToList());
                DisplayBlueprintCategory("Processed Materials", _uiState.Game.StartingGameData.ProcessedMaterials.Keys.ToList());
                DisplayBlueprintCategory("System Gen Settings", _uiState.Game.StartingGameData.SystemGenSettings.Keys.ToList());
                DisplayBlueprintCategory("Techs", _uiState.Game.StartingGameData.Techs.Keys.ToList());
                DisplayBlueprintCategory("Tech Categories", _uiState.Game.StartingGameData.TechCategories.Keys.ToList());
                DisplayBlueprintCategory("Themes", _uiState.Game.StartingGameData.Themes.Keys.ToList());

                ImGui.EndChild();
            }

            ImGui.SameLine();
            ImGui.SetCursorPosY(27f);

            windowContentSize = ImGui.GetContentRegionAvail();
            if(_selectedBlueprint != null && ImGui.BeginChild("BlueprintContent", windowContentSize, true))
            {
                DisplayKeyValue("Full ID", _selectedBlueprint.FullIdentifier);
                DisplayKeyValue("Unique ID", _selectedBlueprint.UniqueID);
                DisplayKeyValue("Json File Name", _selectedBlueprint.JsonFileName);

                if(_selectedBlueprint is ArmorBlueprint)
                    DisplayArmorBlueprint((ArmorBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is CargoTypeBlueprint)
                    DisplayCargoTypeBlueprint((CargoTypeBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is ColonyBlueprint)
                    DisplayColonyBlueprint((ColonyBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is ComponentTemplateBlueprint)
                    DisplayComponentTemplateBlueprint((ComponentTemplateBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is GasBlueprint)
                    DisplayGasBlueprint((GasBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is IndustryTypeBlueprint)
                    DisplayIndustryTypeBlueprint((IndustryTypeBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is MineralBlueprint)
                    DisplayMineralBlueprint((MineralBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is ProcessedMaterialBlueprint)
                    DisplayProcessedMaterialBlueprint((ProcessedMaterialBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is SystemGenSettingsBlueprint)
                    DisplaySystemGenSettingsBlueprint((SystemGenSettingsBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is TechBlueprint)
                    DisplayTechBlueprint((TechBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is TechCategoryBlueprint)
                    DisplayTechCategoryBlueprint((TechCategoryBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is ThemeBlueprint)
                    DisplayThemeBlueprint((ThemeBlueprint)_selectedBlueprint);

                ImGui.EndChild();
            }

            Window.End();
        }
    }

    private Blueprint? FindBlueprint(string key)
    {
        if(_uiState.Game.StartingGameData.Armor.ContainsKey(key))
            return _uiState.Game.StartingGameData.Armor[key];
        if(_uiState.Game.StartingGameData.CargoTypes.ContainsKey(key))
            return _uiState.Game.StartingGameData.CargoTypes[key];
        if(_uiState.Game.StartingGameData.Colonies.ContainsKey(key))
            return _uiState.Game.StartingGameData.Colonies[key];
        if(_uiState.Game.StartingGameData.ComponentTemplates.ContainsKey(key))
            return _uiState.Game.StartingGameData.ComponentTemplates[key];
        if(_uiState.Game.StartingGameData.AtmosphericGas.ContainsKey(key))
            return _uiState.Game.StartingGameData.AtmosphericGas[key];
        if(_uiState.Game.StartingGameData.IndustryTypes.ContainsKey(key))
            return _uiState.Game.StartingGameData.IndustryTypes[key];
        if(_uiState.Game.StartingGameData.Minerals.ContainsKey(key))
            return _uiState.Game.StartingGameData.Minerals[key];
        if(_uiState.Game.StartingGameData.ProcessedMaterials.ContainsKey(key))
            return _uiState.Game.StartingGameData.ProcessedMaterials[key];
        if(_uiState.Game.StartingGameData.SystemGenSettings.ContainsKey(key))
            return _uiState.Game.StartingGameData.SystemGenSettings[key];
        if(_uiState.Game.StartingGameData.Techs.ContainsKey(key))
            return _uiState.Game.StartingGameData.Techs[key];
        if(_uiState.Game.StartingGameData.TechCategories.ContainsKey(key))
            return _uiState.Game.StartingGameData.TechCategories[key];
        if(_uiState.Game.StartingGameData.Themes.ContainsKey(key))
            return _uiState.Game.StartingGameData.Themes[key];

        return null;
    }

    private void DisplayKeyValue(string key, string? value)
    {
        ImGui.Text(key + ":");
        ImGui.SameLine();
        if(string.IsNullOrEmpty(value))
            ImGui.Text("null");
        else
            ImGui.Text(value);
    }

    private void DisplayStartingItemBlueprint(ColonyBlueprint.StartingItemBlueprint startingItemBlueprint)
    {
        ImGui.Text(startingItemBlueprint.Id);
        ImGui.SameLine();
        ImGui.Text(startingItemBlueprint.Amount.ToString());
        ImGui.SameLine();
        if(string.IsNullOrEmpty(startingItemBlueprint.Type))
            ImGui.Text("null");
        else
            ImGui.Text(startingItemBlueprint.Type);
    }

    private void DisplayArmorBlueprint(ArmorBlueprint armorBlueprint)
    {
        DisplayKeyValue("Resource ID", armorBlueprint.ResourceID);
        DisplayKeyValue("Density", armorBlueprint.Density.ToString());
    }

    private void DisplayCargoTypeBlueprint(CargoTypeBlueprint cargoTypeBlueprint)
    {
        DisplayKeyValue("Name", cargoTypeBlueprint.Name);
        DisplayKeyValue("Description", cargoTypeBlueprint.Description);
    }

    private void DisplayColonyBlueprint(ColonyBlueprint colonyBlueprint)
    {
        DisplayKeyValue("Name", colonyBlueprint.Name);
        DisplayKeyValue("Starting Population", colonyBlueprint.StartingPopulation.ToString());

        // Component Designs
        if(colonyBlueprint?.ComponentDesigns?.Count > 0
            && ImGui.CollapsingHeader("Component Designs"))
        {
            foreach(var value in colonyBlueprint.ComponentDesigns)
            {
                ImGui.Text(value);
            }
        }

        // Ordnance Designs
        if(colonyBlueprint?.OrdnanceDesigns?.Count > 0
            && ImGui.CollapsingHeader("Ordnance Designs"))
        {
            foreach(var value in colonyBlueprint.OrdnanceDesigns)
            {
                ImGui.Text(value);
            }
        }

        // Ship Designs
        if(colonyBlueprint?.ShipDesigns?.Count > 0
            && ImGui.CollapsingHeader("Ship Designs"))
        {
            foreach(var value in colonyBlueprint.ShipDesigns)
            {
                ImGui.Text(value);
            }
        }

        // Starting Items
        if(colonyBlueprint?.StartingItems?.Count > 0
            && ImGui.CollapsingHeader("Starting Items"))
        {
            foreach(var value in colonyBlueprint.StartingItems)
            {
                ImGui.Text(value);
            }
        }

        // Installations
        if(colonyBlueprint?.Installations?.Count > 0
            && ImGui.CollapsingHeader("Installations"))
        {
            foreach(var item in colonyBlueprint.Installations)
            {
                DisplayStartingItemBlueprint(item);
            }
        }

        // Cargo
        if(colonyBlueprint?.Cargo?.Count > 0
            && ImGui.CollapsingHeader("Cargo"))
        {
            foreach(var item in colonyBlueprint.Cargo)
            {
                DisplayStartingItemBlueprint(item);
            }
        }

        // Fleets
        if(colonyBlueprint?.Fleets?.Count > 0
            && ImGui.CollapsingHeader("Fleets"))
        {
            foreach(var fleet in colonyBlueprint.Fleets)
            {
                ImGui.Text(fleet.Name);

                if(fleet.Ships == null) continue;

                ImGui.Indent();
                if(ImGui.CollapsingHeader("Ships###" + fleet.Name))
                {
                    foreach(var ship in fleet.Ships)
                    {
                        ImGui.Text(ship.DesignId);
                        ImGui.Text(ship.Name);

                        if(ship.Cargo == null) continue;

                        ImGui.Indent();
                        if(ImGui.CollapsingHeader("Cargo###" + ship.Name))
                        {
                            foreach(var item in ship.Cargo)
                            {
                                DisplayStartingItemBlueprint(item);
                            }
                        }

                        ImGui.Unindent();
                    }
                }
                ImGui.Unindent();
            }
        }
    }

    private void DisplayComponentTemplateBlueprint(ComponentTemplateBlueprint componentTemplateBlueprint)
    {
        DisplayKeyValue("Name", componentTemplateBlueprint.Name);
        DisplayKeyValue("ComponentType", componentTemplateBlueprint.ComponentType);
        DisplayKeyValue("ComponentMountType", componentTemplateBlueprint.MountType.ToString());
        DisplayKeyValue("CargoTypeID", componentTemplateBlueprint.CargoTypeID);
        DisplayKeyValue("IndustryTypeID", componentTemplateBlueprint.IndustryTypeID);

        if(componentTemplateBlueprint.Formulas.Count > 0
            && ImGui.CollapsingHeader("Formulas"))
        {
            foreach(var kvp in componentTemplateBlueprint.Formulas)
            {
                DisplayKeyValue(kvp.Key, kvp.Value);
            }
        }

        if(componentTemplateBlueprint.ResourceCost.Count > 0
            && ImGui.CollapsingHeader("Resource Cost"))
        {
            foreach(var kvp in componentTemplateBlueprint.ResourceCost)
            {
                DisplayKeyValue(kvp.Key, kvp.Value);
            }
        }

        if(componentTemplateBlueprint.Properties.Count > 0
            && ImGui.CollapsingHeader("Properties"))
        {
            foreach(var prop in componentTemplateBlueprint.Properties)
            {
                ImGui.Indent();
                DisplayComponentTemplatePropertyBlueprint(prop);
                ImGui.Unindent();
                ImGui.Separator();
            }
        }
    }

    private void DisplayComponentTemplatePropertyBlueprint(ComponentTemplatePropertyBlueprint componentTemplatePropertyBlueprint)
    {
        DisplayKeyValue("Name", componentTemplatePropertyBlueprint.Name);
        DisplayKeyValue("DescriptionFormula", componentTemplatePropertyBlueprint.DescriptionFormula);
        DisplayKeyValue("Units", componentTemplatePropertyBlueprint.Units);
        DisplayKeyValue("GuiHint", componentTemplatePropertyBlueprint.GuiHint.ToString());
        DisplayKeyValue("GuiIsEnabledFormula", componentTemplatePropertyBlueprint.GuiIsEnabledFormula);
        DisplayKeyValue("EnumTypeName", componentTemplatePropertyBlueprint.EnumTypeName);
        DisplayKeyValue("MaxFormula", componentTemplatePropertyBlueprint.MaxFormula);
        DisplayKeyValue("MinFormula", componentTemplatePropertyBlueprint.MinFormula);
        DisplayKeyValue("StepFormula", componentTemplatePropertyBlueprint.StepFormula);
        DisplayKeyValue("PropertyFormula", componentTemplatePropertyBlueprint.PropertyFormula);
        DisplayKeyValue("AtributeType", componentTemplatePropertyBlueprint.AttributeType);

        if(componentTemplatePropertyBlueprint.DataDict?.Count > 0
            && ImGui.CollapsingHeader("DataDict"))
        {
            foreach(var kvp in componentTemplatePropertyBlueprint.DataDict)
            {
                DisplayKeyValue(kvp.Key, kvp.Value);
            }
        }
    }

    private void DisplayGasBlueprint(GasBlueprint gasBlueprint)
    {
        DisplayKeyValue("Name", gasBlueprint.Name);
        DisplayKeyValue("Weight", gasBlueprint.Weight.ToString());
        DisplayKeyValue("ChemicalSymbol", gasBlueprint.ChemicalSymbol);
        DisplayKeyValue("IsToxic", gasBlueprint.IsToxic.ToString());
        DisplayKeyValue("IsToxicAtPercentage", gasBlueprint.IsToxicAtPercentage.ToString());
        DisplayKeyValue("IsHighlyToxic", gasBlueprint.IsHighlyToxic.ToString());
        DisplayKeyValue("IsHighlyToxicAtPercentage", gasBlueprint.IsHighlyToxicAtPercentage.ToString());
        DisplayKeyValue("BoilingPoint", gasBlueprint.BoilingPoint.ToString());
        DisplayKeyValue("MeltingPoint", gasBlueprint.MeltingPoint.ToString());
        DisplayKeyValue("MinGravity", gasBlueprint.MinGravity.ToString());
        DisplayKeyValue("GreenhouseEffect", gasBlueprint.GreenhouseEffect.ToString());
    }

    private void DisplayIndustryTypeBlueprint(IndustryTypeBlueprint industryTypeBlueprint)
    {
        DisplayKeyValue("Name", industryTypeBlueprint.Name);
    }

    private void DisplayMineralBlueprint(MineralBlueprint mineralBlueprint)
    {
        DisplayKeyValue("Name", mineralBlueprint.Name);
        DisplayKeyValue("Description", mineralBlueprint.Description);
        DisplayKeyValue("CargoTypeID", mineralBlueprint.CargoTypeID);
        DisplayKeyValue("MassPerUnit", mineralBlueprint.MassPerUnit.ToString());
        DisplayKeyValue("VolumePerUnit", mineralBlueprint.VolumePerUnit.ToString());

        if(mineralBlueprint.Abundance?.Count > 0
            && ImGui.CollapsingHeader("Abundance"))
        {
            foreach(var kvp in mineralBlueprint.Abundance)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value.ToString());
            }
        }
    }

    private void DisplayProcessedMaterialBlueprint(ProcessedMaterialBlueprint processedMaterialBlueprint)
    {
        DisplayKeyValue("Name", processedMaterialBlueprint.Name);
        DisplayKeyValue("Description", processedMaterialBlueprint.Description);
        DisplayKeyValue("IndustryTypeID", processedMaterialBlueprint.IndustryTypeID);
        DisplayKeyValue("IndustryPointsCost", processedMaterialBlueprint.IndustryPointCosts.ToString());
        DisplayKeyValue("CargoTypeID", processedMaterialBlueprint.CargoTypeID);
        DisplayKeyValue("GuiHints", processedMaterialBlueprint.GuiHints.ToString());
        DisplayKeyValue("WealthCost", processedMaterialBlueprint.WealthCost.ToString());
        DisplayKeyValue("OutputAmount", processedMaterialBlueprint.OutputAmount.ToString());
        DisplayKeyValue("MassPerUnit", processedMaterialBlueprint.MassPerUnit.ToString());
        DisplayKeyValue("VolumePerUnit", processedMaterialBlueprint.VolumePerUnit.ToString());

        if(processedMaterialBlueprint.Formulas?.Count > 0
            && ImGui.CollapsingHeader("Formulas"))
        {
            foreach(var kvp in processedMaterialBlueprint.Formulas)
            {
                DisplayKeyValue(kvp.Key, kvp.Value);
            }
        }

        if(processedMaterialBlueprint.ResourceCosts?.Count > 0
            && ImGui.CollapsingHeader("Resource Costs"))
        {
            foreach(var kvp in processedMaterialBlueprint.ResourceCosts)
            {
                DisplayKeyValue(kvp.Key, kvp.Value.ToString());
            }
        }
    }

    private void DisplaySystemGenSettingsBlueprint(SystemGenSettingsBlueprint systemGenSettingsBlueprint)
    {
        ImGui.Text("TODO...");
    }

    private void DisplayTechBlueprint(TechBlueprint techBlueprint)
    {
        DisplayKeyValue("Name", techBlueprint.Name);
        DisplayKeyValue("Description", techBlueprint.Description);
        DisplayKeyValue("MaxLevel", techBlueprint.MaxLevel.ToString());
        DisplayKeyValue("CostFormula", techBlueprint.CostFormula);
        DisplayKeyValue("DataFormula", techBlueprint.DataFormula);
        DisplayKeyValue("Category", techBlueprint.Category);

        if(techBlueprint.Unlocks?.Count > 0
            && ImGui.CollapsingHeader("Unlocks"))
        {
            foreach(var kvp in techBlueprint.Unlocks)
            {
                ImGui.Indent();
                if(ImGui.CollapsingHeader(kvp.Key.ToString()))
                {
                    foreach(var value in kvp.Value)
                    {
                        ImGui.Text(value);
                    }
                }
                ImGui.Unindent();
            }
        }
    }

    private void DisplayTechCategoryBlueprint(TechCategoryBlueprint techCategoryBlueprint)
    {
        DisplayKeyValue("Name", techCategoryBlueprint.Name);
        DisplayKeyValue("Description", techCategoryBlueprint.Description);
    }

    private void DisplayThemeBlueprint(ThemeBlueprint themeBlueprint)
    {
        ImGui.Text("TODO...");
    }
}