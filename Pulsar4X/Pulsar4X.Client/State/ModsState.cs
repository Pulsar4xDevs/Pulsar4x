using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Pulsar4X.Modding;

namespace Pulsar4X.Client;

public class ModsState
{
    public class ModMetaData
    {
        public string Path;
        public ModManifest Mod;
        public string ManifestHash { get; }

        public ModMetaData(string path, ModManifest modManifest, string? manifestHash = null)
        {
            Path = path;
            Mod = modManifest;
            ManifestHash = manifestHash ?? string.Empty;
        }
    }

    public static List<ModMetaData> AvailableMods { get; private set; } = new ();
    public static Dictionary<string, bool> IsModEnabled { get; private set; } = new ();

    /// <summary>
    /// Clear the list of available mods
    /// </summary>
    public static void ClearModList()
    {
        AvailableMods.Clear();
        IsModEnabled.Clear();
    }

    /// <summary>
    /// Refresh the list of mods in a given path
    /// </summary>
    /// <param name="modsPath">The directory the mods are located in</param>
    /// <param name="clearExistingMods">If true clears the list of existing mods</param>
    public static void RefreshModsList(string modsPath, bool clearExistingMods = true)
    {
        if(clearExistingMods) ClearModList();

        foreach(var directory in Directory.GetDirectories(modsPath))
        {
            // All mods must have a modInfo.json file that acts as the mod manifest
            var manifestPath = Path.Combine(directory, "modInfo.json");
            if(File.Exists(manifestPath))
            {
                string manifestHash = GetSha1Hash(manifestPath);
                var modManifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(manifestPath));
                if(modManifest != null)
                {
#if DEBUG
                    Console.WriteLine($"Found mod '{modManifest.ModName}' from {manifestPath}");
#endif
                    AvailableMods.Add(new ModMetaData(manifestPath, modManifest, manifestHash));
                    IsModEnabled.Add(modManifest.ModName, modManifest.DefaultEnabled);
                }
            }
        }
    }

    private static string GetSha1Hash(string filePath, int charCount = 8)
    {
        using (FileStream stream = File.OpenRead(filePath))
        using (SHA1 sha1 = SHA1.Create())
        {
            byte[] hashBytes = sha1.ComputeHash(stream);
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString()[..Math.Min(charCount, builder.Length)];
        }
    }
}