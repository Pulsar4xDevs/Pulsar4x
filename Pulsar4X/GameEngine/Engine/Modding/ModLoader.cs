using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Blueprints;
using Pulsar4X.Industry;
using Pulsar4X.Damage;

namespace Pulsar4X.Modding
{
    public class ModLoader
    {
        public Dictionary<string, ModManifest> LoadedMods { get; private set; } = new Dictionary<string, ModManifest>();

        /// <summary>
        /// Loads the requested mods plus anything they depend on, dependencies first. Mods that do
        /// not depend on each other load in namespace order so the result never depends on the
        /// order directories were listed in.
        /// </summary>
        /// <param name="requestedManifestPaths">The mods to load.</param>
        /// <param name="availableManifestPaths">Every manifest a dependency may be found in.</param>
        public void LoadModManifests(
            IEnumerable<string> requestedManifestPaths,
            IEnumerable<string> availableManifestPaths,
            ModDataStore baseData)
        {
            var available = new Dictionary<string, (string Path, ModManifest Manifest)>();
            foreach (var path in availableManifestPaths.Concat(requestedManifestPaths))
            {
                var manifest = ReadManifest(path);
                if (!available.ContainsKey(manifest.Namespace))
                    available.Add(manifest.Namespace, (path, manifest));
            }

            var ordered = new List<string>();
            var visiting = new HashSet<string>();

            void Visit(string ns, string requiredBy)
            {
                if (ordered.Contains(ns)) return;
                if (!available.TryGetValue(ns, out var entry))
                    throw new FileNotFoundException($"Mod '{requiredBy}' requires mod '{ns}', which is not installed.");
                if (!visiting.Add(ns))
                    throw new InvalidOperationException($"Mod '{ns}' depends on itself through a dependency cycle.");

                foreach (var dependency in entry.Manifest.Dependencies ?? new List<string>())
                    Visit(dependency, ns);

                visiting.Remove(ns);
                ordered.Add(ns);
            }

            var requestedNamespaces = new List<string>();
            foreach (var path in requestedManifestPaths)
            {
                var ns = ReadManifest(path).Namespace;
                if (!requestedNamespaces.Contains(ns))
                    requestedNamespaces.Add(ns);
            }

            foreach (var ns in requestedNamespaces.OrderBy(n => n, StringComparer.Ordinal))
                Visit(ns, ns);

            foreach (var ns in ordered)
                LoadModManifest(available[ns].Path, baseData);
        }

        private static ModManifest ReadManifest(string modManifestPath)
        {
            var manifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(modManifestPath));
            if (manifest == null || string.IsNullOrEmpty(manifest.Namespace))
                throw new InvalidDataException($"{modManifestPath} is not a valid mod manifest (missing Namespace).");
            return manifest;
        }

        public void LoadModManifest(string modManifestPath, ModDataStore baseData)
        {
            var modManifest = ReadManifest(modManifestPath);

            foreach (var dependency in modManifest.Dependencies ?? new List<string>())
            {
                if (!LoadedMods.ContainsKey(dependency))
                    throw new InvalidOperationException(
                        $"Mod '{modManifest.Namespace}' requires mod '{dependency}' to be loaded first.");
            }

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
                    mod.Data.JsonFileName =  modDataFile;
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
                case ModInstruction.DataType.PartMat:
                    ApplyModGeneric<ParticleMaterialBlueprint>(baseData.ParticleMaterials, mod, modNamespace);
                    break;
                case ModInstruction.DataType.Species:
                    ApplyModGeneric<SpeciesBlueprint>(baseData.Species, mod, modNamespace);
                    break;
                case ModInstruction.DataType.System:
                    ApplyModGeneric<SystemBlueprint>(baseData.Systems, mod, modNamespace);
                    break;
                case ModInstruction.DataType.Star:
                    ApplyModGeneric<StarBlueprint>(baseData.Stars, mod, modNamespace);
                    break;
                case ModInstruction.DataType.SystemBody:
                    ApplyModGeneric<SystemBodyBlueprint>(baseData.SystemBodies, mod, modNamespace);
                    break;
                case ModInstruction.DataType.Colony:
                    ApplyModGeneric<ColonyBlueprint>(baseData.Colonies, mod, modNamespace);
                    break;
                case ModInstruction.DataType.Faction:
                    ApplyModGeneric<FactionBlueprint>(baseData.Factions, mod, modNamespace);
                    break;
                case ModInstruction.DataType.ComponentDesign:
                    ApplyModGeneric<ComponentDesignBlueprint>(baseData.ComponentDesigns, mod, modNamespace);
                    break;
                case ModInstruction.DataType.ShipDesign:
                    ApplyModGeneric<ShipDesignBlueprint>(baseData.ShipDesigns, mod, modNamespace);
                    break;
                case ModInstruction.DataType.BootstrapDefaults:
                    ApplyModGeneric<BootstrapDefaultsBlueprint>(baseData.BootstrapDefaults, mod, modNamespace);
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
            else if (instruction.Operation == ModInstruction.OperationType.Remove)
            {
                // Nothing to remove. Do not fall through and add the Remove's stub payload.
                return;
            }
            else
            {
                instruction.Data.SetFullIdentifier(modNamespace);
                dataDict[instruction.Data.UniqueID] = (T)instruction.Data;
            }
        }
    }
}