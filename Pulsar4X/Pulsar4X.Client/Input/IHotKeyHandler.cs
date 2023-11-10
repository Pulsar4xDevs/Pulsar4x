using SDL2;

namespace Pulsar4X.Input;

public interface IHotKeyHandler
{
    void HandleEvent(SDL.SDL_Event e);
}

public abstract class HotKeyFactory
{
    public static IHotKeyHandler CreateDefault() => new SystemMapHotKeys();
}