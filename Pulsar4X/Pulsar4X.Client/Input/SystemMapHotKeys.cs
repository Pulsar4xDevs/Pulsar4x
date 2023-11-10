using Pulsar4X.SDL2UI;
using SDL2;

namespace Pulsar4X.Input;

public class SystemMapHotKeys : IHotKeyHandler
{
    public void HandleEvent(SDL.SDL_Event e)
    {
        if (e.type == SDL.SDL_EventType.SDL_KEYUP)
        {
            if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
            {
                MainMenuItems.GetInstance().ToggleActive();
            }
            else if(e.key.keysym.sym == SDL.SDL_Keycode.SDLK_F1)
            {
                DebugWindow.GetInstance().ToggleActive();
            }
            else if(e.key.keysym.sym == SDL.SDL_Keycode.SDLK_F2)
            {
                PerformanceWindow.GetInstance().ToggleActive();
            }
            else if(e.key.keysym.sym == SDL.SDL_Keycode.SDLK_1)
            {
                ComponentDesignWindow.GetInstance().ToggleActive();
            }
            else if(e.key.keysym.sym == SDL.SDL_Keycode.SDLK_2)
            {
                ShipDesignWindow.GetInstance().ToggleActive();
            }
            else if(e.key.keysym.sym == SDL.SDL_Keycode.SDLK_3)
            {
                EconomicsWindow.GetInstance().ToggleActive();
            }
            else if(e.key.keysym.sym == SDL.SDL_Keycode.SDLK_4)
            {
                ResearchWindow.GetInstance().ToggleActive();
            }
            else if(e.key.keysym.sym == SDL.SDL_Keycode.SDLK_5)
            {
                FleetWindow.GetInstance().ToggleActive();
            }
            else if(e.key.keysym.sym == SDL.SDL_Keycode.SDLK_6)
            {
                CommanderWindow.GetInstance().ToggleActive();
            }
        }
    }
}