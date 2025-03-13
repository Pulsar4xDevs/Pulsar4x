using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Pulsar4X.Modding;

namespace Pulsar4X.Client;

public class ModsState
{
    public struct ModMetaData
    {
        public string Path;
        public ModManifest Mod;
    }

    public static List<ModMetaData> AvailableMods { get; private set; } = new ();
    public static Dictionary<string, bool> IsModEnabled { get; private set; } = new ();

    public static void RefreshModListFromModsDirectory()
    {
        AvailableMods.Clear();
        IsModEnabled.Clear();

        var modsDirectory = Path.Combine(PulsarMainWindow.GetAppDataPath(), PulsarMainWindow.ModsPath);

        foreach(var directory in Directory.GetDirectories(modsDirectory))
        {
            var manifestPath = Path.Combine(directory, "modInfo.json");
            if(File.Exists(manifestPath))
            {
                var modManifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(manifestPath));
                if(modManifest != null)
                {
                    AvailableMods.Add(new ModMetaData() { Mod = modManifest, Path = manifestPath });
                    IsModEnabled.Add(modManifest.ModName, modManifest.DefaultEnabled);
                }
            }
        }
    }
}