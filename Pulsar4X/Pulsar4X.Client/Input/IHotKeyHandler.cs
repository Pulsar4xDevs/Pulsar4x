using SDL3;

namespace Pulsar4X.Input;

public interface IHotKeyHandler
{
    void HandleEvent(SDL.Event e);
}

public abstract class HotKeyFactory
{
    public static IHotKeyHandler CreateDefault() => new SystemMapHotKeys();
}