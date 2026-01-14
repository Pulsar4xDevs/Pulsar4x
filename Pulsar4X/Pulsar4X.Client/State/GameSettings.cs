using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SDL3;

namespace Pulsar4X.Client
{
    public class GameSettings
    {
        public static string SettingsFileName = "game-settings.json";
        
        // Display Settings
        public int WindowWidth { get; set; } = 1280;
        public int WindowHeight { get; set; } = 720;
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
        
        // Available resolutions (common ones)
        public static readonly List<(int width, int height)> CommonResolutions = new List<(int, int)>
        {
            (1280, 720),
            (1366, 768),
            (1920, 1080),
            (2560, 1440),
            (3840, 2160),
            (1680, 1050),
            (1440, 900),
            (1600, 900),
            (2560, 1080),
            (3440, 1440)
        };
        
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
                // Apply resolution
                window.Size = new System.Numerics.Vector2(WindowWidth, WindowHeight);
                
                // Apply display mode
                switch (DisplayMode)
                {
                    case DisplayModeType.Windowed:
                        SDL.SetWindowFullscreen(window.Window, false);
                        SDL.SetWindowBordered(window.Window, true);
                        break;
                        
                    case DisplayModeType.Fullscreen:
                        SDL.SetWindowFullscreen(window.Window, true);
                        break;
                        
                    case DisplayModeType.BorderlessFullscreen:
                        SDL.SetWindowFullscreen(window.Window, false);
                        SDL.SetWindowBordered(window.Window, false);
                        // Get desktop resolution for borderless fullscreen
                        var mode = SDL.GetCurrentDisplayMode(SDL.GetPrimaryDisplay());
                        if (mode != null)
                        {
                            window.Size = new System.Numerics.Vector2(mode.Value.W, mode.Value.H);
                            SDL.SetWindowPosition(window.Window, 0, 0);
                        }
                        break;
                }
                
                // Apply VSync
                SDL.SetRenderVSync(window.Renderer, VSync ? 1 : 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to apply display settings: {ex.Message}");
            }
        }
    }
} 
