using Pulsar4X.Modding;

namespace Pulsar4X.Engine;

public static class GameFactory
{
    public static Game CreateGame(ModDataStore modDataStore, NewGameSettings newGameSettings)
    {
        return new Game(newGameSettings, modDataStore);
    }
}