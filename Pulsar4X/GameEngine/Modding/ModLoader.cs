using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Blueprints;
using Pulsar4X.Engine.Damage;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Industry;

namespace Pulsar4X.Modding
{
    public class ModLoader
    {
        public Dictionary<string, ModManifest> LoadedMods { get; private set; } = new Dictionary<string, ModManifest>();

        public void LoadModManifest(string modManifestPath, ModDataStore baseData)
        {
            var manifestJson = File.ReadAllText(modManifestPath);
            var modManifest = JsonConvert.DeserializeObject<ModManifest>(manifestJson);

            if(LoadedMods.ContainsKey(modManifest.Namespace))
            {
                throw new DuplicateNameException("A mod with the namespace " + modManifest.Namespace + " has already been loaded.");
            }

            // Get the directory of the mod manifest
            string? modDirectory = Path.GetDirectoryName(modManifestPath);

            if(string.IsNullOrEmpty(modDirectory)) throw new DirectoryNotFoundException($"Could not find {modManifestPath}");

            modManifest.ModDirectory = modDirectory;

            foreach (var modDataFile in modManifest.DataFiles)
            {
                // Combine the directory with the mod data file name
                string modDataFilePath = Path.Combine(modDirectory, modDataFile);

                var modInstructions = JsonConvert.DeserializeObject<List<ModInstruction>>(
                    File.ReadAllText(modDataFilePath),
                    new JsonSerializerSettings { Converters = new List<JsonConverter> { new ModInstructionJsonConverter(), new WeightedListConverter() } });

                foreach (var mod in modInstructions)
                {
                    ApplyMod(baseData, mod, modManifest.Namespace);
                }
            }

            baseData.ModManifests.Add(modManifest);

            LoadedMods.Add(modManifest.Namespace, modManifest);
        }

        private void ApplyMod(ModDataStore baseData, ModInstruction mod, string modNamespace)
        {
            switch (mod.Type)
            {
                case ModInstruction.DataType.Armor:
                    ApplyModGeneric<ArmorBlueprint>(baseData.Armor, mod, modNamespace);
                    break;
                case ModInstruction.DataType.CargoType:
                    ApplyModGeneric<CargoTypeBlueprint>(baseData.CargoTypes, mod, modNamespace);
                    break;
                case ModInstruction.DataType.ComponentTemplate:
                    ApplyModGeneric<ComponentTemplateBlueprint>(baseData.ComponentTemplates, mod, modNamespace);
                    break;
                case ModInstruction.DataType.DefaultItems:
                    ApplyModGeneric<DefaultItemsBlueprint>(baseData.DefaultItems, mod, modNamespace);
                    break;
                case ModInstruction.DataType.Gas:
                    ApplyModGeneric<GasBlueprint>(baseData.AtmosphericGas, mod, modNamespace);
                    break;
                case ModInstruction.DataType.IndustryType:
                    ApplyModGeneric<IndustryTypeBlueprint>(baseData.IndustryTypes, mod, modNamespace);
                    break;
                case ModInstruction.DataType.Mineral:
                    ApplyModGeneric<Mineral>(baseData.Minerals, mod, modNamespace);
                    break;
                case ModInstruction.DataType.ProcessedMaterial:
                    ApplyModGeneric<ProcessedMaterial>(baseData.ProcessedMaterials, mod, modNamespace);
                    break;
                case ModInstruction.DataType.SystemGenSettings:
                    ApplyModGeneric<SystemGenSettingsBlueprint>(baseData.SystemGenSettings, mod, modNamespace);
                    break;
                case ModInstruction.DataType.Tech:
                    ApplyModGeneric<TechBlueprint>(baseData.Techs, mod, modNamespace);
                    break;
                case ModInstruction.DataType.TechCategory:
                    ApplyModGeneric<TechCategoryBlueprint>(baseData.TechCategories, mod, modNamespace);
                    break;
                case ModInstruction.DataType.Theme:
                    ApplyModGeneric<ThemeBlueprint>(baseData.Themes, mod, modNamespace);
                    break;
                case ModInstruction.DataType.DamageResistance:
                    ApplyModGeneric<DamageResistBlueprint>(baseData.DamageResists, mod, modNamespace);
                    break;
            }
        }


        private void ApplyModGeneric<T>(Dictionary<string, T> dataDict, ModInstruction instruction, string modNamespace) where T : Blueprint
        {
            if (dataDict.TryGetValue(instruction.Data.UniqueID, out var existingData))
            {
                if (instruction.Operation == ModInstruction.OperationType.Default)
                {
                    // Update the namespace
                    existingData.SetFullIdentifier(modNamespace);

                    // Use reflection to overwrite specific properties
                    foreach (var property in instruction.Data.GetType().GetProperties())
                    {
                        var modValue = property.GetValue(instruction.Data);
                        if (modValue != null)
                        {
                            // If property is a collection and CollectionOperation is specified
                            if (property.PropertyType.IsGenericType
                                && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                                && instruction.CollectionOperation.HasValue)
                            {
                                var originalList = (IList?)property.GetValue(existingData);
                                var modList = (IList)modValue;

                                if(originalList == null) throw new NullReferenceException($"Unable to resolve List for {existingData.FullIdentifier}");

                                switch(instruction.CollectionOperation.Value)
                                {
                                    case ModInstruction.CollectionOperationType.Add:
                                        foreach(var item in modList)
                                        {
                                            originalList.Add(item);
                                        }
                                        break;
                                    case ModInstruction.CollectionOperationType.Remove:
                                        foreach(var item in modList)
                                        {
                                            originalList.Remove(item);
                                        }
                                        break;
                                    case ModInstruction.CollectionOperationType.Overwrite:
                                        property.SetValue(existingData, modValue);
                                        break;
                                }
                            }
                            // If property is a dictionary and CollectionOperation is specified
                            else if (property.PropertyType.IsGenericType
                                && property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                                && instruction.CollectionOperation.HasValue)
                            {
                                var originalDict = (IDictionary?)property.GetValue(existingData);
                                var modDict = (IDictionary)modValue;

                                if(originalDict == null) throw new NullReferenceException($"Unable to resolve Dictionary for {existingData.FullIdentifier}");

                                switch(instruction.CollectionOperation.Value)
                                {
                                    case ModInstruction.CollectionOperationType.Add:
                                        foreach(DictionaryEntry entry in modDict)
                                        {
                                            originalDict[entry.Key] = entry.Value;
                                        }
                                        break;
                                    case ModInstruction.CollectionOperationType.Remove:
                                        foreach(DictionaryEntry entry in modDict)
                                        {
                                            originalDict.Remove(entry.Key);
                                        }
                                        break;
                                    case ModInstruction.CollectionOperationType.Overwrite:
                                        property.SetValue(existingData, modValue);
                                        break;
                                }
                            }
                            // Check if the property is of type Guid and if it's equal to Guid.Empty
                            else if (property.PropertyType == typeof(Guid) && (Guid)modValue == Guid.Empty)
                            {
                                // Skip overwriting for empty Guid values
                                continue;
                            }
                            else
                            {
                                property.SetValue(existingData, modValue);
                            }
                        }
                    }
                }
                else if (instruction.Operation == ModInstruction.OperationType.Remove)
                {
                    dataDict.Remove(instruction.Data.UniqueID);
                    return;
                }
            }
            else
            {
                instruction.Data.SetFullIdentifier(modNamespace);
                dataDict[instruction.Data.UniqueID] = (T)instruction.Data;
            }
        }
    }
}