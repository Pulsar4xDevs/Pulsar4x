using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Modding
{
    public class ModLoader
    {
        public void LoadModManifest(string modManifestPath, ModDataStore baseData)
        {
            var manifestJson = File.ReadAllText(modManifestPath);
            var modManifest = JsonConvert.DeserializeObject<ModManifest>(manifestJson);

            // Get the directory of the mod manifest
            string modDirectory = Path.GetDirectoryName(modManifestPath);

            foreach (var modDataFile in modManifest.DataFiles)
            {
                // Combine the directory with the mod data file name
                string modDataFilePath = Path.Combine(modDirectory, modDataFile);

                var modInstructions = JsonConvert.DeserializeObject<List<ModInstruction>>(
                    File.ReadAllText(modDataFilePath),
                    new JsonSerializerSettings { Converters = new List<JsonConverter> { new ModInstructionJsonConverter() } });

                foreach (var mod in modInstructions)
                {
                    ApplyMod(baseData, mod, modManifest.Namespace);
                }
            }
        }

        public void ApplyMod(ModDataStore baseData, ModInstruction mod, string modNamespace)
        {
            switch (mod.Type)
            {
                case ModInstruction.DataType.Armor:
                    ApplyModGeneric<ArmorSD>(baseData.Armor, mod, modNamespace);
                    break;

                case ModInstruction.DataType.CargoType:
                    ApplyModGeneric<CargoTypeSD>(baseData.CargoTypes, mod, modNamespace);
                    break;
            }
        }


        private void ApplyModGeneric<T>(Dictionary<string, T> dataDict, ModInstruction instruction, string modNamespace) where T : SerializableGameData
        {
            if (dataDict.TryGetValue(instruction.Data.UniqueId, out var existingData))
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
                            // Check if the property is of type Guid and if it's equal to Guid.Empty
                            if (property.PropertyType == typeof(Guid) && (Guid)modValue == Guid.Empty)
                            {
                                // Skip overwriting for empty Guid values
                                continue;
                            }
                            property.SetValue(existingData, modValue);
                        }
                    }
                }
                else if (instruction.Operation == ModInstruction.OperationType.Remove)
                {
                    dataDict.Remove(instruction.Data.UniqueId);
                    return;
                }
            }
            else
            {
                instruction.Data.SetFullIdentifier(modNamespace);
                dataDict[instruction.Data.UniqueId] = (T)instruction.Data;
            }
        }


    }
}