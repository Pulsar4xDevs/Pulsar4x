using ImGuiNET;
using Pulsar4X.Client;
using Pulsar4X.Client.Interface.Windows;
using SDL3;

namespace Pulsar4X.Input;

public class SystemMapHotKeys : IHotKeyHandler
{
    public void HandleEvent(SDL.Event e)
    {
        if (!ImGui.IsAnyItemActive() && e.Type == (uint)SDL.EventType.KeyUp)
        {
            if (e.Key.Key == SDL.Keycode.Space)
            {
                var tc = TimeControl.GetInstance();
                if((e.Key.Mod & SDL.Keymod.Ctrl) != 0)
                {
                    // Ctrl + Space for single step.
                    tc.OneStepPressed();
                }
                else
                {
                    tc.PausePlayPressed();
                }
            }
            else if (e.Key.Key == SDL.Keycode.Escape)
            {
                MainMenuItems.GetInstance().ToggleActive();
            }
            else if(e.Key.Key == SDL.Keycode.F1)
            {
                PulsarGuiWindow._uiState.ToggleDevTool("debug-window");
            }
            else if(e.Key.Key == SDL.Keycode.F2)
            {
                PulsarGuiWindow._uiState.ToggleDevTool("performance-window");
            }
            else if(e.Key.Key == SDL.Keycode.F3)
            {
                GameLogWindow.GetInstance().ToggleActive();
            }
            else if(e.Key.Key == SDL.Keycode.F4)
            {
                PulsarGuiWindow._uiState.ToggleDevTool("blueprints-window");
            }
            else if(e.Key.Key == SDL.Keycode.F5)
            {
                ComponentsWindow.GetInstance().ToggleActive();
            }
            else if(e.Key.Key == SDL.Keycode.Alpha1)
            {
                ComponentDesignWindow.GetInstance().ToggleActive();
            }
            else if(e.Key.Key == SDL.Keycode.Alpha2)
            {
                ShipDesignWindow.GetInstance().ToggleActive();
            }
            else if(e.Key.Key == SDL.Keycode.Alpha3)
            {
                ColonyManagementWindow.GetInstance().ToggleActive();
            }
            else if(e.Key.Key == SDL.Keycode.Alpha4)
            {
                ResearchWindow.GetInstance().ToggleActive();
            }
            else if(e.Key.Key == SDL.Keycode.Alpha5)
            {
                FleetWindow.GetInstance().ToggleActive();
            }
            else if(e.Key.Key == SDL.Keycode.Alpha6)
            {
                CommanderWindow.GetInstance().ToggleActive();
            }
        }
    }
}