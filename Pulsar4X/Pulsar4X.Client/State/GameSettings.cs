using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SDL3;

namespace Pulsar4X.Client
{
    public class GameSettings
    {
        public static string SettingsFileName = "game-settings.json";

        // Display Settings
        public int WindowWidth { get; set; } = -1;
        public int WindowHeight { get; set; } = -1;
        public DisplayModeType DisplayMode { get; set; } = DisplayModeType.Windowed;
        public bool VSync { get; set; } = true;

        // Audio Settings
        public float MasterVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.8f;
        public float SoundEffectsVolume { get; set; } = 0.9f;
        public bool AudioEnabled { get; set; } = true;

        // UI Settings
        public float UIScale { get; set; } = 1.0f;
        public bool ShowTooltips { get; set; } = true;
        public bool ShowFPS { get; set; } = false;
        public bool EuropeClock { get; set; } = false;

        // Input Settings
        public bool MouseInvertY { get; set; } = false;
        public float MouseSensitivity { get; set; } = 1.0f;

        // Available display modes
        public enum DisplayModeType
        {
            Windowed,
            Fullscreen,
            BorderlessFullscreen
        }

        // Available display modes
        public static readonly SDL.DisplayMode[] DisplayModes = Utils.GetDisplayModes().ToArray();

        public void Save()
        {
            try
            {
                string? appDataPath = PulsarMainWindow.GetAppDataPath();
                if (string.IsNullOrEmpty(appDataPath))
                    return;

                string settingsPath = Path.Combine(appDataPath, SettingsFileName);
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save game settings: {ex.Message}");
            }
        }

        public static GameSettings Load()
        {
            try
            {
                string? appDataPath = PulsarMainWindow.GetAppDataPath();
                if (string.IsNullOrEmpty(appDataPath))
                    return new GameSettings();

                string settingsPath = Path.Combine(appDataPath, SettingsFileName);
                if (!File.Exists(settingsPath))
                    return new GameSettings();

                string json = File.ReadAllText(settingsPath);

                var settings = JsonConvert.DeserializeObject<GameSettings>(json) ?? new GameSettings();

                Helpers.SetClock((settings.EuropeClock) ? "en-150" : "en");

                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load game settings: {ex.Message}");
                return new GameSettings();
            }
        }

        public void ApplyDisplaySettings(SDL3Window window)
        {
            try
            {
                // Apply display mode
                SDL.SetWindowFullscreen(window.Window, DisplayMode == DisplayModeType.Fullscreen);
                if (DisplayMode == DisplayModeType.Windowed)
                {
                    SDL.SetWindowBordered(window.Window, true);

                    if (WindowWidth > 0 && WindowHeight > 0)
                        window.Size = new (WindowWidth, WindowHeight);
                }
                else if (DisplayMode == DisplayModeType.BorderlessFullscreen)
                {
                    SDL.SetWindowBordered(window.Window, false);

                    // This won't work with wayland: https://wiki.libsdl.org/SDL3/README-wayland
                    // Get desktop resolution for borderless fullscreen
                    var mode = SDL.GetCurrentDisplayMode(SDL.GetPrimaryDisplay());
                    if (mode != null)
                    {
                        window.Size = new (mode.Value.W, mode.Value.H);
                        SDL.SetWindowPosition(window.Window, 0, 0);
                    }
                }

                // Apply VSync
                SDL.SetRenderVSync(window.Renderer, VSync ? 1 : 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to apply display settings: {ex.Message}");
            }
        }

        public string GetTimeFormat() => EuropeClock ? "HH:mm:ss" : "hh:mm:ss tt";
        public string GetDateFormat() => EuropeClock ? "dd/MM/yyyy" : "MM/dd/yyyy";
        public string GetDateTimeFormat() => GetDateFormat() + " " + GetTimeFormat();
    }
}
