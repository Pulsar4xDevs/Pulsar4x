using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Pulsar4X.Client.ModFileEditing;

namespace Pulsar4X.Client.Interface.Windows;

public class BlueprintsWindow : PulsarGuiWindow
{

    List<object> _editStack = new List<object>();
    
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
            if(ImGui.BeginChild("BlueprintListSelection", new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y), ImGuiChildFlags.Borders))
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
                DisplayBlueprintCategory("Stars", _uiState.Game.StartingGameData.Stars.Keys.ToList());
                DisplayBlueprintCategory("Systems", _uiState.Game.StartingGameData.Systems.Keys.ToList());
                DisplayBlueprintCategory("System Bodies", _uiState.Game.StartingGameData.SystemBodies.Keys.ToList());
                DisplayBlueprintCategory("System Gen Settings", _uiState.Game.StartingGameData.SystemGenSettings.Keys.ToList());
                DisplayBlueprintCategory("Techs", _uiState.Game.StartingGameData.Techs.Keys.ToList());
                DisplayBlueprintCategory("Tech Categories", _uiState.Game.StartingGameData.TechCategories.Keys.ToList());
                DisplayBlueprintCategory("Themes", _uiState.Game.StartingGameData.Themes.Keys.ToList());

                ImGui.EndChild();
            }

            ImGui.SameLine();
            //ImGui.SetCursorPosY(27f);

            windowContentSize = ImGui.GetContentRegionAvail();
            if(_selectedBlueprint != null && ImGui.BeginChild("BlueprintContent", windowContentSize, ImGuiChildFlags.Borders))
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
                else if(_selectedBlueprint is StarBlueprint)
                    DisplayStarBlueprint((StarBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is SystemBlueprint)
                    DisplaySystemBlueprint((SystemBlueprint)_selectedBlueprint);
                else if(_selectedBlueprint is SystemBodyBlueprint)
                    DisplaySystemBodyBlueprint((SystemBodyBlueprint)_selectedBlueprint);
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
        if(_uiState.Game.StartingGameData.Stars.ContainsKey(key))
            return _uiState.Game.StartingGameData.Stars[key];
        if(_uiState.Game.StartingGameData.Systems.ContainsKey(key))
            return _uiState.Game.StartingGameData.Systems[key];
        if(_uiState.Game.StartingGameData.SystemBodies.ContainsKey(key))
            return _uiState.Game.StartingGameData.SystemBodies[key];
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
        ImGui.Text("Density: ");
        ImGui.SameLine();
        float density = armorBlueprint.Density;
        if (FloatEditWidget.Display("##density"+ armorBlueprint.UniqueID, ref density))
        {
            armorBlueprint.Density = density;
        }
        //DisplayKeyValue("Density", armorBlueprint.Density.ToString());
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

    private void DisplayStarBlueprint(StarBlueprint starBlueprint)
    {
        DisplayKeyValue("Name", starBlueprint.Name);
        if(ImGui.CollapsingHeader("Info"))
        {
            DisplayKeyValue("Mass", starBlueprint.Info.Mass.ToString());
            DisplayKeyValue("Radius", starBlueprint.Info.Radius.ToString());
            DisplayKeyValue("Age", starBlueprint.Info.Age.ToString());
            DisplayKeyValue("Class", starBlueprint.Info.Class);
            DisplayKeyValue("Temperature", starBlueprint.Info.Temperature.ToString());
            DisplayKeyValue("Luminosity", starBlueprint.Info.Luminosity.ToString());
            DisplayKeyValue("LuminosityClass", starBlueprint.Info.LuminosityClass);
            DisplayKeyValue("SpectralType", starBlueprint.Info.SpectralType);
        }
    }

    private void DisplaySystemBlueprint(SystemBlueprint systemBlueprint)
    {
        DisplayKeyValue("Name", systemBlueprint.Name);
        DisplayKeyValue("Seed", systemBlueprint.Seed.ToString());

        if(ImGui.CollapsingHeader("Stars"))
        {
            foreach(var star in systemBlueprint.Stars)
            {
                ImGui.Text(star);
            }
        }

        if(ImGui.CollapsingHeader("Bodies"))
        {
            foreach(var body in systemBlueprint.Bodies)
            {
                ImGui.Text(body);
            }
        }

        if(ImGui.CollapsingHeader("Survey Rings"))
        {
            foreach(var ring in systemBlueprint.SurveyRings)
            {
                DisplayKeyValue("RingRadiusInAU", ring.RingRadiusInAU.ToString());
                ImGui.SameLine();
                DisplayKeyValue("Count", ring.Count.ToString());
            }
        }
    }

    private void DisplaySystemBodyBlueprint(SystemBodyBlueprint systemBodyBlueprint)
    {
        DisplayKeyValue("Name", systemBodyBlueprint.Name);
        DisplayKeyValue("CanStartHere", systemBodyBlueprint.CanStartHere.ToString());
        DisplayKeyValue("Parent", systemBodyBlueprint.Parent);
        DisplayKeyValue("Colonizable", systemBodyBlueprint.Colonizable.ToString());
        DisplayKeyValue("GeoSurveyPointsRequired", systemBodyBlueprint.GeoSurveyPointsRequired.ToString());
        DisplayKeyValue("GenerateMinerals", systemBodyBlueprint.GenerateMinerals);

        if(ImGui.CollapsingHeader("Info"))
        {
            DisplayKeyValue("Gravity", systemBodyBlueprint.Info.Gravity.ToString());
            DisplayKeyValue("Type", systemBodyBlueprint.Info.Type);
            DisplayKeyValue("Tectonics", systemBodyBlueprint.Info.Tectonics);
            DisplayKeyValue("Albedo", systemBodyBlueprint.Info.Albedo.ToString());
            DisplayKeyValue("AxialTilt", systemBodyBlueprint.Info.AxialTilt.ToString());
            DisplayKeyValue("MagneticField", systemBodyBlueprint.Info.MagneticField.ToString());
            DisplayKeyValue("BaseTemperature", systemBodyBlueprint.Info.BaseTemperature.ToString());
            DisplayKeyValue("RadiationLevel", systemBodyBlueprint.Info.RadiationLevel.ToString());
            DisplayKeyValue("AtmosphericDust", systemBodyBlueprint.Info.AtmosphericDust.ToString());
            DisplayKeyValue("LengthOfDay", systemBodyBlueprint.Info.LengthOfDay.ToString());
            DisplayKeyValue("Mass", systemBodyBlueprint.Info.Mass.ToString());
            DisplayKeyValue("Radius", systemBodyBlueprint.Info.Radius.ToString());
        }

        if(ImGui.CollapsingHeader("Orbit"))
        {
            DisplayKeyValue("SemiMajorAxis", systemBodyBlueprint.Orbit.SemiMajorAxis.ToString());
            DisplayKeyValue("SemiMajorAxis_m", systemBodyBlueprint.Orbit.SemiMajorAxis_m.ToString());
            DisplayKeyValue("SemiMajorAxis_km", systemBodyBlueprint.Orbit.SemiMajorAxis_km.ToString());
            DisplayKeyValue("SemiMajorAxis_au", systemBodyBlueprint.Orbit.SemiMajorAxis_au.ToString());
            DisplayKeyValue("Eccentricity", systemBodyBlueprint.Orbit.Eccentricity.ToString());
            DisplayKeyValue("EclipticInclination", systemBodyBlueprint.Orbit.EclipticInclination.ToString());
            DisplayKeyValue("EclipticInclination_r", systemBodyBlueprint.Orbit.EclipticInclination_r.ToString());
            DisplayKeyValue("EclipticInclination_d", systemBodyBlueprint.Orbit.EclipticInclination_d.ToString());
            DisplayKeyValue("LoAN", systemBodyBlueprint.Orbit.LoAN.ToString());
            DisplayKeyValue("LoAN_r", systemBodyBlueprint.Orbit.LoAN_r.ToString());
            DisplayKeyValue("LoAN_d", systemBodyBlueprint.Orbit.LoAN_d.ToString());
            DisplayKeyValue("AoP", systemBodyBlueprint.Orbit.AoP.ToString());
            DisplayKeyValue("AoP_r", systemBodyBlueprint.Orbit.AoP_r.ToString());
            DisplayKeyValue("AoP_d", systemBodyBlueprint.Orbit.AoP_d.ToString());
            DisplayKeyValue("MeanAnomaly", systemBodyBlueprint.Orbit.MeanAnomaly.ToString());
            DisplayKeyValue("MeanAnomaly_r", systemBodyBlueprint.Orbit.MeanAnomaly_r.ToString());
            DisplayKeyValue("MeanAnomaly_d", systemBodyBlueprint.Orbit.MeanAnomaly_d.ToString());
        }

        if(systemBodyBlueprint.Atmosphere.HasValue && ImGui.CollapsingHeader("Atmosphere"))
        {
            var atmosphere = systemBodyBlueprint.Atmosphere.Value;

            DisplayKeyValue("Pressure", atmosphere.Pressure.ToString());
            DisplayKeyValue("Hydrosphere", atmosphere.Hydrosphere.ToString());
            DisplayKeyValue("HydroExtent", atmosphere.HydroExtent.ToString());
            DisplayKeyValue("GreenhouseFactor", atmosphere.GreenhouseFactor.ToString());
            DisplayKeyValue("GreenhousePressure", atmosphere.GreenhousePressure.ToString());
            DisplayKeyValue("SurfaceTemperature", atmosphere.SurfaceTemperature.ToString());

            if(atmosphere.Gases != null)
            {
                ImGui.Indent();
                if(ImGui.CollapsingHeader("Gases"))
                {
                    foreach(var gas in atmosphere.Gases)
                    {
                        DisplayKeyValue("Symbol", gas.Symbol);
                        ImGui.SameLine();
                        DisplayKeyValue("Percent", gas.Percent.ToString());
                    }
                }
                ImGui.Unindent();
            }
        }

        if(systemBodyBlueprint.Minerals != null && ImGui.CollapsingHeader("Minerals"))
        {
            foreach(var mineral in systemBodyBlueprint.Minerals)
            {
                DisplayKeyValue("Id", mineral.Id); ImGui.SameLine();
                DisplayKeyValue("Abundance", mineral.Abundance.ToString()); ImGui.SameLine();
                DisplayKeyValue("Accessibility", mineral.Accessibility.ToString());
            }
        }
    }

    private void DisplaySystemGenSettingsBlueprint(SystemGenSettingsBlueprint systemGenSettingsBlueprint)
    {
        DisplayKeyValue("Real Star Systems", systemGenSettingsBlueprint.RealStarSystems.ToString());
        DisplayKeyValue("NPR Generation Chance", systemGenSettingsBlueprint.NPRGenerationChance.ToString());
        DisplayKeyValue("Planet Generation Chance", systemGenSettingsBlueprint.PlanetGenerationChance.ToString());
        DisplayKeyValue("Max No Of Planets", systemGenSettingsBlueprint.MaxNoOfPlanets.ToString());
        DisplayKeyValue("Max No Of Asteroids Per Belt", systemGenSettingsBlueprint.MaxNoOfAsteroidsPerBelt.ToString());
        DisplayKeyValue("Max No Of Asteroid Belts", systemGenSettingsBlueprint.MaxNoOfAsteroidBelts.ToString());
        DisplayKeyValue("Number Of Asteroids Per Dwarf Planet", systemGenSettingsBlueprint.NumberOfAsteroidsPerDwarfPlanet.ToString());
        DisplayKeyValue("Minimum Comets Per System", systemGenSettingsBlueprint.MiniumCometsPerSystem.ToString());
        DisplayKeyValue("Max No Of Comets", systemGenSettingsBlueprint.MaxNoOfComets.ToString());
        DisplayKeyValue("Max Asteroid Orbit Deviation", systemGenSettingsBlueprint.MaxAsteroidOrbitDeviation.ToString());
        DisplayKeyValue("Max Body Inclination", systemGenSettingsBlueprint.MaxBodyInclination.ToString());
        DisplayKeyValue("Max Moon Mass Relative To Parent Body", systemGenSettingsBlueprint.MaxMoonMassRelativeToParentBody.ToString());
        DisplayKeyValue("Orbit Gravity Factor", systemGenSettingsBlueprint.OrbitGravityFactor.ToString());
        DisplayKeyValue("Terrestrial Body Tectonic Activity Chance", systemGenSettingsBlueprint.TerrestrialBodyTectonicActivityChance.ToString());
        DisplayKeyValue("Minimum Possible Day Length", systemGenSettingsBlueprint.MiniumPossibleDayLength.ToString());
        DisplayKeyValue("Min Moon Orbit Multiplier", systemGenSettingsBlueprint.MinMoonOrbitMultiplier.ToString());
        DisplayKeyValue("Runaway Greenhouse Effect Chance", systemGenSettingsBlueprint.RunawayGreenhouseEffectChance.ToString());
        DisplayKeyValue("Runaway Greenhouse Effect Multiplier", systemGenSettingsBlueprint.RunawayGreenhouseEffectMultiplyer.ToString());
        DisplayKeyValue("J2000", systemGenSettingsBlueprint.J2000.ToString());
        DisplayKeyValue("Ruins Generation Chance", systemGenSettingsBlueprint.RuinsGenerationChance.ToString());
        DisplayKeyValue("Min Mineral Accessibility", systemGenSettingsBlueprint.MinMineralAccessibility.ToString());
        DisplayKeyValue("Min Homeworld Mineral Accessibility", systemGenSettingsBlueprint.MinHomeworldMineralAccessibility.ToString());
        DisplayKeyValue("Min Homeworld Mineral Amount", systemGenSettingsBlueprint.MinHomeworldMineralAmmount.ToString());
        DisplayKeyValue("Homeworld Mineral Amount", systemGenSettingsBlueprint.HomeworldMineralAmmount.ToString());
        DisplayKeyValue("Base Mineral Chance", systemGenSettingsBlueprint.BaseMineralChance.ToString());
        DisplayKeyValue("Min Max Atmospheric Pressure", $"Min: {systemGenSettingsBlueprint.MinMaxAtmosphericPressure.Min}, Max: {systemGenSettingsBlueprint.MinMaxAtmosphericPressure.Max}");

        if (ImGui.CollapsingHeader("Star Type Distribution For Real Stars"))
        {
            foreach (var kvp in systemGenSettingsBlueprint.StarTypeDistributionForRealStars)
            {
                DisplayKeyValue(kvp.Value.ToString(), kvp.Weight.ToString());
            }
        }

        if(ImGui.CollapsingHeader("Star Type Distribution For Fake Stars"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.StarTypeDistributionForFakeStars)
            {
                DisplayKeyValue(kvp.Value.ToString(), kvp.Weight.ToString());
            }
        }

        if(systemGenSettingsBlueprint.StarRadiusBySpectralType?.Count > 0
            && ImGui.CollapsingHeader("Star Radius By Spectral Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.StarRadiusBySpectralType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.StarTemperatureBySpectralType?.Count > 0
            && ImGui.CollapsingHeader("Star Temperature By Spectral Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.StarTemperatureBySpectralType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.StarLuminosityBySpectralType?.Count > 0
            && ImGui.CollapsingHeader("Star Luminosity By Spectral Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.StarLuminosityBySpectralType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.StarMassBySpectralType?.Count > 0
            && ImGui.CollapsingHeader("Star Mass By Spectral Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.StarMassBySpectralType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.StarAgeBySpectralType?.Count > 0
            && ImGui.CollapsingHeader("Star Age By Spectral Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.StarAgeBySpectralType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.StarSpectralTypePlanetGenerationRatio?.Count > 0
            && ImGui.CollapsingHeader("Star Spectral Type Planet Generation Ratio"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.StarSpectralTypePlanetGenerationRatio)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value.ToString());
            }
        }

        if(systemGenSettingsBlueprint.SystemBodyMassByType?.Count > 0
            && ImGui.CollapsingHeader("System Body Mass By Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.SystemBodyMassByType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.SystemBodyDensityByType?.Count > 0
            && ImGui.CollapsingHeader("System Body Density By Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.SystemBodyDensityByType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.OrbitalDistanceByStarSpectralType_AU?.Count > 0
            && ImGui.CollapsingHeader("Orbital Distance By Star Spectral Type (AU)"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.OrbitalDistanceByStarSpectralType_AU)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.OrbitalDistanceByStarSpectralType?.Count > 0
            && ImGui.CollapsingHeader("Orbital Distance By Star Spectral Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.OrbitalDistanceByStarSpectralType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.BodyEccentricityByType?.Count > 0
            && ImGui.CollapsingHeader("Body Eccentricity By Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.BodyEccentricityByType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.PlanetAlbedoByType?.Count > 0
            && ImGui.CollapsingHeader("Planet Albedo By Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.PlanetAlbedoByType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.PlanetMagneticFieldByType?.Count > 0
            && ImGui.CollapsingHeader("Planet Magnetic Field By Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.PlanetMagneticFieldByType)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.AtmosphereGenerationModifier?.Count > 0
            && ImGui.CollapsingHeader("Atmosphere Generation Modifier"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.AtmosphereGenerationModifier)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value.ToString());
            }
        }

        if(systemGenSettingsBlueprint.MoonGenerationChanceByPlanetType?.Count > 0
            && ImGui.CollapsingHeader("Moon Generation Chance By Planet Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.MoonGenerationChanceByPlanetType)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value.ToString());
            }
        }

        if(systemGenSettingsBlueprint.MaxMoonOrbitDistanceByPlanetType?.Count > 0
            && ImGui.CollapsingHeader("Max Moon Orbit Distance By Planet Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.MaxMoonOrbitDistanceByPlanetType)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value.ToString());
            }
        }

        if(systemGenSettingsBlueprint.MaxNoOfMoonsByPlanetType?.Count > 0
            && ImGui.CollapsingHeader("Max No Of Moons By Planet Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.MaxNoOfMoonsByPlanetType)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value.ToString());
            }
        }

        if(systemGenSettingsBlueprint.BodyTectonicsThresholds?.Count > 0
            && ImGui.CollapsingHeader("Body Tectonics Thresholds"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.BodyTectonicsThresholds)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value.ToString());
            }
        }

        if(ImGui.CollapsingHeader("Band Body Weight"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.BandBodyWeight)
            {
                DisplayKeyValue(kvp.Value.ToString(), kvp.Weight.ToString());
            }
        }

        if(ImGui.CollapsingHeader("Inner Band Type Weights"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.InnerBandTypeWeights)
            {
                DisplayKeyValue(kvp.Value.ToString(), kvp.Weight.ToString());
            }
        }

        if(ImGui.CollapsingHeader("Habitable Band Type Weights"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.HabitableBandTypeWeights)
            {
                DisplayKeyValue(kvp.Value.ToString(), kvp.Weight.ToString());
            }
        }

        if(ImGui.CollapsingHeader("Outer Band Type Weights"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.OuterBandTypeWeights)
            {
                DisplayKeyValue(kvp.Value.ToString(), kvp.Weight.ToString());
            }
        }

        if(ImGui.CollapsingHeader("Ruins Size Distribution"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.RuinsSizeDistribution)
            {
                DisplayKeyValue(kvp.Value.ToString(), kvp.Weight.ToString());
            }
        }

        if(ImGui.CollapsingHeader("Ruins Quality Distribution"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.RuinsQualityDistribution)
            {
                DisplayKeyValue(kvp.Value.ToString(), kvp.Weight.ToString());
            }
        }

        if(systemGenSettingsBlueprint.RuinsCountRangeBySize?.Count > 0
            && ImGui.CollapsingHeader("Ruins Count Range By Size"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.RuinsCountRangeBySize)
            {
                DisplayKeyValue(kvp.Key.ToString(), $"Min: {kvp.Value.Min}, Max: {kvp.Value.Max}");
            }
        }

        if(systemGenSettingsBlueprint.RuinsQualityAdjustment?.Count > 0
            && ImGui.CollapsingHeader("Ruins Quality Adjustment"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.RuinsQualityAdjustment)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value.ToString());
            }
        }

        if(systemGenSettingsBlueprint.MineralGenerationChanceByBodyType?.Count > 0
            && ImGui.CollapsingHeader("Mineral Generation Chance By Body Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.MineralGenerationChanceByBodyType)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value.ToString());
            }
        }

        if(systemGenSettingsBlueprint.MaxMineralAmmountByBodyType?.Count > 0
            && ImGui.CollapsingHeader("Max Mineral Amount By Body Type"))
        {
            foreach(var kvp in systemGenSettingsBlueprint.MaxMineralAmmountByBodyType)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value.ToString());
            }
        }
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
        DisplayKeyValue("Name", themeBlueprint.Name);

        if(themeBlueprint.FleetNames?.Count > 0
            && ImGui.CollapsingHeader("Fleet Names"))
        {
            foreach(var fleetName in themeBlueprint.FleetNames)
            {
                ImGui.Text(fleetName);
            }
        }

        if(themeBlueprint.ShipNames?.Count > 0
            && ImGui.CollapsingHeader("Ship Names"))
        {
            foreach(var shipName in themeBlueprint.ShipNames)
            {
                ImGui.Text(shipName);
            }
        }

        if(themeBlueprint.FirstNames?.Count > 0
            && ImGui.CollapsingHeader("First Names"))
        {
            foreach(var firstName in themeBlueprint.FirstNames)
            {
                ImGui.Text(firstName);
            }
        }

        if(themeBlueprint.LastNames?.Count > 0
            && ImGui.CollapsingHeader("Last Names"))
        {
            foreach(var lastName in themeBlueprint.LastNames)
            {
                ImGui.Text(lastName);
            }
        }

        if(themeBlueprint.NavyRanks?.Count > 0
            && ImGui.CollapsingHeader("Navy Ranks"))
        {
            foreach(var kvp in themeBlueprint.NavyRanks)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value);
            }
        }

        if(themeBlueprint.NavyRanksAbbreviations?.Count > 0
            && ImGui.CollapsingHeader("Navy Ranks Abbreviations"))
        {
            foreach(var kvp in themeBlueprint.NavyRanksAbbreviations)
            {
                DisplayKeyValue(kvp.Key.ToString(), kvp.Value);
            }
        }
    }
}