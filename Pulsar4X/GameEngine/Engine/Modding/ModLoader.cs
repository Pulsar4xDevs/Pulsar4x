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

            ApplyDataFiles(modDirectory, modManifest.DataFiles, modManifest.Namespace, baseData);

            baseData.ModManifests.Add(modManifest);

            LoadedMods.Add(modManifest.Namespace, modManifest);
        }

        /// <summary>
        /// Scans each root's immediate subfolders for scenarioInfo.json, deserializes them, and
        /// returns them keyed by Namespace. Duplicate namespaces throw one ModLoadException.
        /// </summary>
        public Dictionary<string, ScenarioManifest> DiscoverScenarios(IEnumerable<string> scenarioRootFolders)
        {
            var found = new Dictionary<string, ScenarioManifest>();
            var errors = new List<ModLoadError>();

            foreach (var root in scenarioRootFolders.Distinct())
            {
                if (!Directory.Exists(root))
                    continue;

                foreach (var dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
                {
                    var path = Path.Combine(dir, "scenarioInfo.json");
                    if (!File.Exists(path))
                        continue;

                    var manifest = ReadScenarioManifest(path);
                    if (found.ContainsKey(manifest.Namespace))
                    {
                        errors.Add(new ModLoadError(
                            manifest.Namespace,
                            "scenarioInfo.json",
                            "ScenarioManifest",
                            manifest.Namespace,
                            "Namespace",
                            manifest.Namespace));
                        continue;
                    }

                    found.Add(manifest.Namespace, manifest);
                }
            }

            if (errors.Count > 0)
                throw new ModLoadException(errors);

            return found;
        }

        /// <summary>
        /// Scans each root's immediate subfolders for systemInfo.json, deserializes them, and
        /// returns them keyed by Namespace. Duplicate namespaces throw one ModLoadException.
        /// </summary>
        public Dictionary<string, SystemManifest> DiscoverSystems(IEnumerable<string> systemRootFolders)
        {
            var found = new Dictionary<string, SystemManifest>();
            var errors = new List<ModLoadError>();

            foreach (var root in systemRootFolders.Distinct())
            {
                if (!Directory.Exists(root))
                    continue;

                foreach (var dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
                {
                    var path = Path.Combine(dir, "systemInfo.json");
                    if (!File.Exists(path))
                        continue;

                    var manifest = ReadSystemManifest(path);
                    if (found.ContainsKey(manifest.Namespace))
                    {
                        errors.Add(new ModLoadError(
                            manifest.Namespace,
                            "systemInfo.json",
                            "SystemManifest",
                            manifest.Namespace,
                            "Namespace",
                            manifest.Namespace));
                        continue;
                    }

                    found.Add(manifest.Namespace, manifest);
                }
            }

            if (errors.Count > 0)
                throw new ModLoadException(errors);

            return found;
        }

        /// <summary>
        /// Among discovered scenarios whose RequiredMods are all loaded, pick the one with the most
        /// RequiredMods, then DefaultEnabled first, then Namespace ordinal. Null if none qualify.
        /// </summary>
        public static string? ChooseDefaultScenario(IEnumerable<ScenarioManifest> discovered, ModDataStore store)
        {
            var loaded = new HashSet<string>(store.ModManifests.Select(m => m.Namespace));
            var best = discovered
                .Where(s => (s.RequiredMods ?? new List<string>()).All(ns => loaded.Contains(ns)))
                .OrderByDescending(s => (s.RequiredMods ?? new List<string>()).Count)
                .ThenByDescending(s => s.DefaultEnabled)
                .ThenBy(s => s.Namespace, StringComparer.Ordinal)
                .FirstOrDefault();
            return best?.Namespace;
        }

        /// <summary>
        /// Resolves the chosen scenario's RequiredScenarios (base first, each once), checks every
        /// scenario in the chain, including the system packs it includes, then applies each.
        /// Throws one ModLoadException before applying anything if the chain is unsound.
        /// <paramref name="scenarioManifestPath"/> selects a manifest that may not be in
        /// <paramref name="discovered"/> (a fixture or a later user folder).
        /// When <paramref name="discoveredSystems"/> is null, each scenario's packs are discovered
        /// from the systems folder next to that scenario's scenarios root.
        /// </summary>
        public void ApplyScenario(
            string? scenarioId,
            string? scenarioManifestPath,
            IReadOnlyDictionary<string, ScenarioManifest> discovered,
            ModDataStore store,
            IReadOnlyDictionary<string, SystemManifest>? discoveredSystems = null)
        {
            var byNamespace = discovered.ToDictionary(entry => entry.Key, entry => entry.Value);
            string? chosenNs = scenarioId;
            string? chosenPath = null;

            if (!string.IsNullOrEmpty(scenarioManifestPath))
            {
                var fromPath = ReadScenarioManifest(scenarioManifestPath);
                byNamespace[fromPath.Namespace] = fromPath;
                chosenNs = fromPath.Namespace;
                chosenPath = scenarioManifestPath;
            }

            if (string.IsNullOrEmpty(chosenNs))
                return;

            if (!byNamespace.ContainsKey(chosenNs))
            {
                throw new ModLoadException(new ModLoadError(
                    chosenNs,
                    "scenarioInfo.json",
                    "ScenarioManifest",
                    chosenNs,
                    "ScenarioId",
                    chosenNs));
            }

            var errors = new List<ModLoadError>();
            var ordered = new List<string>();
            var visiting = new HashSet<string>();
            var systemsByRoot = new Dictionary<string, IReadOnlyDictionary<string, SystemManifest>>();

            IReadOnlyDictionary<string, SystemManifest> SystemsFor(ScenarioManifest manifest)
            {
                if (discoveredSystems != null)
                    return discoveredSystems;

                string root = DefaultSystemsRoot(manifest.ScenarioDirectory);
                if (!systemsByRoot.TryGetValue(root, out var found))
                {
                    found = DiscoverSystems(new[] { root });
                    systemsByRoot[root] = found;
                }
                return found;
            }

            void Visit(string ns)
            {
                if (ordered.Contains(ns)) return;
                if (!byNamespace.TryGetValue(ns, out var manifest))
                {
                    errors.Add(new ModLoadError(
                        ns,
                        "scenarioInfo.json",
                        "ScenarioManifest",
                        ns,
                        "RequiredScenarios",
                        ns));
                    return;
                }
                if (!visiting.Add(ns))
                {
                    errors.Add(new ModLoadError(
                        ns,
                        "scenarioInfo.json",
                        "ScenarioManifest",
                        ns,
                        "RequiredScenarios",
                        ns));
                    return;
                }

                foreach (var required in manifest.RequiredScenarios ?? new List<string>())
                {
                    if (!byNamespace.ContainsKey(required))
                    {
                        errors.Add(new ModLoadError(
                            manifest.Namespace,
                            "scenarioInfo.json",
                            "ScenarioManifest",
                            manifest.Namespace,
                            "RequiredScenarios",
                            required));
                    }
                    else
                    {
                        Visit(required);
                    }
                }

                visiting.Remove(ns);
                if (!ordered.Contains(ns))
                    ordered.Add(ns);

                foreach (var required in manifest.RequiredMods ?? new List<string>())
                {
                    if (!store.ModManifests.Any(m => m.Namespace == required))
                    {
                        errors.Add(new ModLoadError(
                            manifest.Namespace,
                            "scenarioInfo.json",
                            "ScenarioManifest",
                            manifest.Namespace,
                            "RequiredMods",
                            required));
                    }
                }

                foreach (var included in manifest.IncludedSystems ?? new List<string>())
                {
                    if (!SystemsFor(manifest).ContainsKey(included))
                    {
                        errors.Add(new ModLoadError(
                            manifest.Namespace,
                            "scenarioInfo.json",
                            "ScenarioManifest",
                            manifest.Namespace,
                            "IncludedSystems",
                            included));
                    }
                }
            }

            Visit(chosenNs);

            if (errors.Count > 0)
                throw new ModLoadException(errors);

            IReadOnlyDictionary<string, SystemManifest> systemsForLoad;
            if (discoveredSystems != null)
            {
                systemsForLoad = discoveredSystems;
            }
            else
            {
                var merged = new Dictionary<string, SystemManifest>();
                foreach (var found in systemsByRoot.Values)
                {
                    foreach (var pair in found)
                    {
                        if (!merged.ContainsKey(pair.Key))
                            merged.Add(pair.Key, pair.Value);
                    }
                }
                systemsForLoad = merged;
            }

            ScenarioManifest? chosen = null;
            foreach (var ns in ordered)
            {
                string path = ns == chosenNs && chosenPath != null
                    ? chosenPath
                    : Path.Combine(byNamespace[ns].ScenarioDirectory, "scenarioInfo.json");
                var loaded = LoadScenario(path, store, systemsForLoad);
                if (ns == chosenNs)
                    chosen = loaded;
            }

            if (chosen?.DefaultStart != null)
                ValidateDefaultStart(chosen, store);

            store.ScenarioNamespace = chosen?.Namespace;
            store.ScenarioDefaultStart = chosen?.DefaultStart;
        }

        /// <summary>
        /// Applies a scenario's included system packs, then its own data files, on top of an
        /// already-loaded mod store. Does not register the scenario as a mod and does not
        /// walk RequiredScenarios. When <paramref name="systems"/> is null, packs are
        /// discovered from the systems folder next to this scenario's scenarios root.
        /// </summary>
        public ScenarioManifest LoadScenario(
            string scenarioManifestPath,
            ModDataStore baseData,
            IReadOnlyDictionary<string, SystemManifest>? systems = null)
        {
            var scenarioManifest = ReadScenarioManifest(scenarioManifestPath);

            var missingMods = new List<ModLoadError>();
            foreach (var required in scenarioManifest.RequiredMods ?? new List<string>())
            {
                if (!baseData.ModManifests.Any(m => m.Namespace == required))
                {
                    missingMods.Add(new ModLoadError(
                        scenarioManifest.Namespace,
                        "scenarioInfo.json",
                        "ScenarioManifest",
                        scenarioManifest.Namespace,
                        "RequiredMods",
                        required));
                }
            }
            if (missingMods.Count > 0)
                throw new ModLoadException(missingMods);

            var systemManifests = systems
                ?? DiscoverSystems(new[] { DefaultSystemsRoot(scenarioManifest.ScenarioDirectory) });

            var missingSystems = new List<ModLoadError>();
            foreach (var included in scenarioManifest.IncludedSystems ?? new List<string>())
            {
                if (!systemManifests.ContainsKey(included))
                {
                    missingSystems.Add(new ModLoadError(
                        scenarioManifest.Namespace,
                        "scenarioInfo.json",
                        "ScenarioManifest",
                        scenarioManifest.Namespace,
                        "IncludedSystems",
                        included));
                }
            }
            if (missingSystems.Count > 0)
                throw new ModLoadException(missingSystems);

            foreach (var included in scenarioManifest.IncludedSystems ?? new List<string>())
                ApplySystemPack(systemManifests[included], baseData);

            ApplyDataFiles(
                scenarioManifest.ScenarioDirectory,
                scenarioManifest.DataFiles ?? new List<string>(),
                scenarioManifest.Namespace,
                baseData);

            return scenarioManifest;
        }

        private static ScenarioManifest ReadScenarioManifest(string scenarioManifestPath)
        {
            var scenarioManifest = JsonConvert.DeserializeObject<ScenarioManifest>(
                File.ReadAllText(scenarioManifestPath), ScenarioManifest.SerializerSettings);
            if (scenarioManifest == null || string.IsNullOrEmpty(scenarioManifest.Namespace))
                throw new InvalidDataException($"{scenarioManifestPath} is not a valid scenario manifest (missing Namespace).");

            string? scenarioDirectory = Path.GetDirectoryName(scenarioManifestPath);
            if (string.IsNullOrEmpty(scenarioDirectory))
                throw new DirectoryNotFoundException($"Could not find {scenarioManifestPath}");

            scenarioManifest.ScenarioDirectory = scenarioDirectory;
            return scenarioManifest;
        }

        private static SystemManifest ReadSystemManifest(string systemManifestPath)
        {
            var systemManifest = JsonConvert.DeserializeObject<SystemManifest>(
                File.ReadAllText(systemManifestPath), SystemManifest.SerializerSettings);
            if (systemManifest == null || string.IsNullOrEmpty(systemManifest.Namespace))
                throw new InvalidDataException($"{systemManifestPath} is not a valid system manifest (missing Namespace).");

            string? systemDirectory = Path.GetDirectoryName(systemManifestPath);
            if (string.IsNullOrEmpty(systemDirectory))
                throw new DirectoryNotFoundException($"Could not find {systemManifestPath}");

            systemManifest.SystemDirectory = systemDirectory;
            return systemManifest;
        }

        /// <summary>The systems folder next to the scenarios root that contains
        /// <paramref name="scenarioDirectory"/>. A scenario folder Data/scenarios/name resolves to Data/systems.</summary>
        private static string DefaultSystemsRoot(string scenarioDirectory)
        {
            if (string.IsNullOrEmpty(scenarioDirectory))
                return "";

            string? scenariosRoot = Path.GetDirectoryName(scenarioDirectory);
            string? parent = string.IsNullOrEmpty(scenariosRoot) ? null : Path.GetDirectoryName(scenariosRoot);
            if (string.IsNullOrEmpty(parent))
                return "";

            return Path.Combine(parent, "systems");
        }

        /// <summary>Applies one system pack's data files under the pack namespace.
        /// A pack already listed in <see cref="ModDataStore.LoadedSystems"/> is skipped.</summary>
        private void ApplySystemPack(SystemManifest manifest, ModDataStore store)
        {
            if (store.LoadedSystems.Contains(manifest.Namespace))
                return;

            ApplyDataFiles(
                manifest.SystemDirectory,
                manifest.DataFiles ?? new List<string>(),
                manifest.Namespace,
                store);
            store.LoadedSystems.Add(manifest.Namespace);
        }

        private static void ValidateDefaultStart(ScenarioManifest scenario, ModDataStore store)
        {
            var start = scenario.DefaultStart;
            var errors = new List<ModLoadError>();

            if (!string.IsNullOrEmpty(start.Species) && !store.Species.ContainsKey(start.Species))
            {
                errors.Add(new ModLoadError(
                    scenario.Namespace, "scenarioInfo.json", "ScenarioManifest",
                    scenario.Namespace, "DefaultStart.Species", start.Species));
            }
            if (!string.IsNullOrEmpty(start.Colony) && !store.Colonies.ContainsKey(start.Colony))
            {
                errors.Add(new ModLoadError(
                    scenario.Namespace, "scenarioInfo.json", "ScenarioManifest",
                    scenario.Namespace, "DefaultStart.Colony", start.Colony));
            }
            if (!string.IsNullOrEmpty(start.System) && !store.Systems.ContainsKey(start.System))
            {
                errors.Add(new ModLoadError(
                    scenario.Namespace, "scenarioInfo.json", "ScenarioManifest",
                    scenario.Namespace, "DefaultStart.System", start.System));
            }
            if (!string.IsNullOrEmpty(start.Body) && !store.SystemBodies.ContainsKey(start.Body))
            {
                errors.Add(new ModLoadError(
                    scenario.Namespace, "scenarioInfo.json", "ScenarioManifest",
                    scenario.Namespace, "DefaultStart.Body", start.Body));
            }

            if (errors.Count > 0)
                throw new ModLoadException(errors);
        }

        private void ApplyDataFiles(string directory, List<string> dataFiles, string modNamespace, ModDataStore baseData)
        {
            // One corrupt file still stops the load, but the message names the mod and file first.
            foreach (var modDataFile in dataFiles)
            {
                try
                {
                    LoadModDataFile(baseData, modNamespace, directory, modDataFile);
                }
                catch (Exception ex) when (ex is JsonException or FormatException)
                {
                    throw new InvalidDataException(
                        $"Mod '{modNamespace}', file '{modDataFile}': {ex.Message}", ex);
                }
            }
        }

        private void LoadModDataFile(ModDataStore baseData, string modNamespace, string modDirectory, string modDataFile)
        {
            // Combine the directory with the mod data file name
            string modDataFilePath = Path.Combine(modDirectory, modDataFile);

            if (!File.Exists(modDataFilePath))
                throw new FileNotFoundException(
                    $"Mod '{modNamespace}', file '{modDataFile}': the data file listed in its manifest is not there.",
                    modDataFilePath);

            var modInstructions = JsonConvert.DeserializeObject<List<ModInstruction>>(
                File.ReadAllText(modDataFilePath),
                new JsonSerializerSettings { Converters = new List<JsonConverter> { new ModInstructionJsonConverter(), new WeightedListConverter() } });

            if (modInstructions == null)
                return;

            foreach (var mod in modInstructions)
            {
                mod.Data.JsonFileName =  modDataFile;
                ApplyMod(baseData, mod, modNamespace);
            }
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