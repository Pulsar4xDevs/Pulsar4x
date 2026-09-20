using Pulsar4X.Modding;

namespace Pulsar4X.Engine;

public static class GameFactory
{
    public static Game CreateGame(string[] modFiles, NewGameSettings newGameSettings)
    {
        ModLoader modLoader = new ModLoader();
        ModDataStore modDataStore = new ModDataStore();

        foreach (var mod in modFiles)
        {
            modLoader.LoadModManifest(mod, modDataStore);
        }

        return new Game(newGameSettings, modDataStore);
    }

    public static Game CreateGame(ModDataStore modDataStore, NewGameSettings newGameSettings)
    {
        return new Game(newGameSettings, modDataStore);
    }
}