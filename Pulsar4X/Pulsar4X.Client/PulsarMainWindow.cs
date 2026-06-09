#if TRACE
#define DEBUG
#endif

using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Diagnostics;
using ImGuiNET;
using SDL3;
using Microsoft.Extensions.Configuration;
using Pulsar4X.Client.Interface.Themes;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Pulsar4X.Client
{
    public enum MouseButtons
    {
        Primary,
        Alt,
        Middle
    }

    public class PulsarMainWindow : SDL3Window
    {
        public const string PreferencesFile = "preferences.ini";
        public const string UserOrbitSettingsFile = "orbit-settings.json";
        public const string SavesPath = "Saves";
        public static string ModsPath = "Mods";
        public static string ResourcesPath = "Resources";
        private readonly GlobalUIState _state;
        private ITheme _theme;

        float mouseX;
        float mouseY;

        int _debugSDLFontHeight;

        ulong _fpsFrames = 0;
        ulong _fpsLastMeasurementTime = 0;
        float _fpsLastMeasurement = 0;

        public PulsarMainWindow(string[] args)
            : base(AppName)
        {
            _state = new GlobalUIState(this);
            _state.GalacticMap = new GalacticMapRender(this, _state);

            try
            {
                string? appDataDirectory = GetAppDataPath();

                if(string.IsNullOrEmpty(appDataDirectory)) throw new NullReferenceException("App data directory cannot be null");

                // Set the deafault mods path
                ModsPath = Path.Combine(appDataDirectory, ModsPath);

                // Set the default resources path
                {
                    var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var exeDiretory = Path.GetDirectoryName(exePath);

                    if(string.IsNullOrEmpty(exeDiretory)) throw new NullReferenceException("exe path cannot be null");

                    ResourcesPath = Path.Combine(exeDiretory, ResourcesPath);
                }

                // Parse optional command line arguments
                ParseCommandLineArguments(args);

                // Create directories we need if they don't exist
                TryCreateDirectory(appDataDirectory, SavesPath);
                TryCreateDirectory(appDataDirectory, ModsPath);

                // Make sure the base game mod is copied over to the mod directory
                // string sourceData = "Data";
                // string modsDirectory = Path.Combine(appDataDirectory, ModsPath);
                // DeleteThenCopyToDirectory(sourceData, modsDirectory);

                // Load the available mods
                ModsState.RefreshModsList(ModsPath);

                // Read and apply any window preferences
                LoadPreferences();

                // Apply game settings (resolution, display mode, etc.) - this should override old preferences
                _state.GameSettings.ApplyDisplaySettings(this);

                // Apply UI scaling
                ImGui.GetStyle().FontScaleMain = _state.GameSettings.UIScale;

                // Apply any saved user orbit settings
                LoadUserOrbitSettings();

                PopulateStyles();

            }
            catch(Exception e)
            {
                Console.WriteLine($"Error setting up game data: {e.Message}");
                Trace.WriteLine($"Error setting up game data: {e}");
            }

            _debugSDLFontHeight = SDL3.TTF.GetFontHeight(Styles.SDLDefaultFont);
        }

        private void PopulateStyles()
        {
            // Load fonts - texture will be created automatically by the new texture system
            var defaultFont = "ProggyClean.ttf";
            var defaultFontPath = Path.Combine(ResourcesPath, defaultFont);
            var defaultFontSize = 13f;

            Trace.WriteLine("loading font: " + defaultFontPath);
            if (!File.Exists(defaultFontPath))
                Trace.WriteLine("WARNING: font file does not exist: " + defaultFontPath);
            Styles.SDLDefaultFont = SDL3.TTF.OpenFont(defaultFontPath, 16f); // FIXME: set this and imgui font to same size. 13f looks terrible.
            if (Styles.SDLDefaultFont == IntPtr.Zero)
                Trace.WriteLine("WARNING: TTF.OpenFont failed: " + SDL.GetError());
            Styles.DefaultFont = PlatformBackend.LoadFont(ResourcesPath, defaultFont, defaultFontSize);

            PlatformBackend.LoadFont(ResourcesPath, "DejaVuSans.ttf", 13f, "ΩωΝνΔδθΘϖ⚙", true);
            Styles.MonospaceFont = PlatformBackend.LoadFont(ResourcesPath, "JetBrainsMono-Regular.ttf", 14f);
            Styles.MediumFont = PlatformBackend.LoadFont(ResourcesPath, "Roboto-Medium.ttf", 14f);

            // Theme
            Styles.Theme = _theme;
            _theme.Apply();
        }

        internal event EventHandler<SDL.Event> MouseMoveOccured;
        internal event EventHandler<SDL.Event> MouseButtonDownOccured;
        internal event EventHandler<SDL.Event> MouseButtonUpOccured;
        internal event EventHandler<SDL.Event> MouseWheelOccured;

        public override void HandleEvent(SDL.Event e)
        {
            (float mX, float mY, SDL.MouseButtonFlags mouseFlags) = GetMouseState();

            if (mX != mouseX || mY != mouseY)
                MouseMoveOccured?.Invoke(this, e);

            mouseX = mX;
            mouseY = mY;

            if(!_state.IsGameLoaded)
            {
                var compare = 0;
#if DEBUG
                // Debug builds have the git hash displayed in the bottom left corner
                compare = 1;
#endif
                // Open the main menu if no other windows are open
                if(ImGui.GetIO().MetricsRenderWindows == compare)
                    MainMenuItems.GetInstance().SetActive(true);
                return;
            }

            if (!PlatformBackend.WantsMouseCapture())
            {
                switch (e.Type)
                {
                    case (uint)SDL.EventType.MouseButtonDown:
                        MouseButtonDownOccured?.Invoke(this, e);
                        break;
                    case (uint)SDL.EventType.MouseButtonUp:
                        MouseButtonUpOccured?.Invoke(this, e);
                        break;
                    case (uint)SDL.EventType.MouseWheel:
                        MouseWheelOccured?.Invoke(this, e);
                        break;
                }
            }

            // The top of the hotkey stack should list for hotkeys
            _state.HotKeys.Peek().HandleEvent(e);
        }

        public override void Update()
        {
            base.Update();

            //update and refresh state for GameDateTimechange
            if(_state.Game != null)
            {
                DateTime curTime = _state.Game.TimePulse.GameGlobalDateTime;
                if (curTime != _state.LastGameUpdateTime)
                {
                    foreach (var item in _state.UpdateableWindows)
                    {
                        if (item.GetActive() == true)
                            item.OnGameTickChange(curTime);
                    }

                    _state.LastGameUpdateTime = curTime;
                }

                //update and refresh state for SystemDateTimechage
                curTime = _state.SelectedSystemTime;
                if (curTime != _state.SelectedSysLastUpdateTime)
                {
                    foreach (var item in _state.UpdateableWindows)
                    {
                        if (item.GetActive() == true)
                            item.OnSystemTickChange(curTime);
                    }

                    _state.SelectedSysLastUpdateTime = curTime;
                }
            }

            _state.Update();
        }

        public override void Render()
        {
            base.Render();

            // Render the game
            _state.GalacticMap?.Draw();

            // Render the UI
            RenderUI();

            // If in DEBUG render the git hash as the version in the corner of the screen
#if DEBUG
            var version = "Version: " + AssemblyInfo.GetGitHash();
            RenderDebugText(this.Renderer, version, 50);

            var iver = "ImGui version: " + ImGui.GetVersion();
            RenderDebugText(this.Renderer, iver, 50 + _debugSDLFontHeight);

            var sver = "SDL version: " + SDL.GetRevision();
            RenderDebugText(this.Renderer, sver, 50 + _debugSDLFontHeight * 2);
#endif

            // Show FPS counter if enabled
            if (_state.GameSettings.ShowFPS)
            {
                _fpsFrames += 1;

                var currentTime = SDL.GetTicks();
                var elapsedTime = currentTime - _fpsLastMeasurementTime;

                if (elapsedTime >= 1000)
                {
                    _fpsLastMeasurement = _fpsFrames / (elapsedTime / 1000f);
                    _fpsFrames = 0;
                    _fpsLastMeasurementTime = currentTime;
                }

                var fps = "FPS: " + _fpsLastMeasurement.ToString();
                RenderDebugText(this.Renderer, fps, 50 + _debugSDLFontHeight * 4);
            }
        }

        public override void PostFrameUpdate()
        {
            base.PostFrameUpdate();

            foreach (var (_, systemState) in _state.StarSystemStates)
            {
                systemState.PostFrameCleanup();
            }
        }

        /// <summary>
        /// Render the UI
        /// </summary>
        public void RenderUI()
        {
            // ImGui helper windows
            if (_state.ShowMetrixWindow)
                ImGui.ShowMetricsWindow(ref _state.ShowMetrixWindow);

            if (_state.ShowDemoWindow)
            {
                ImGui.ShowDemoWindow();
                ImGui.ShowUserGuide();
            }

            // Render name icons
            _state.GalacticMap?.DrawNameIcons();

            // Render any windows that have registered themselves
            foreach (var item in _state.LoadedWindows.Values.ToArray())
            {
                item.Display();
            }

            foreach (var entityWindow in _state.EntityWindows.Values.ToArray())
            {
                entityWindow.Display();
            }

            foreach (var item in _state.LoadedNonUniqueWindows.Values.ToArray())
            {
                item.Display();
            }

            // Render the maneuver node panel overlay (if active)
            _state.DisplayManeuverNodePanel();
        }

        public override void Exit()
        {
            // save the user orbit settings on exit
            SaveOrbitSettings();

            // save the game settings on exit
            _state.GameSettings.Save();

            // Cleanup SDL TTF
            SDL3.TTF.CloseFont(Styles.SDLDefaultFont);
        }

        /// <summary>
        /// If the given path & name don't exist create it
        /// </summary>
        /// <param name="path">A path to where to create the given name folder</param>
        /// <param name="name">The name of the folder to create</param>
        private void TryCreateDirectory(string path, string name)
        {
            string directory = Path.Combine(path, name);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Parse command line arguments to setup the data and
        /// resource paths
        /// </summary>
        /// <param name="args"></param>
        private void ParseCommandLineArguments(string[] args)
        {
            for(int i = 0; i < args.Length; i++)
            {
                switch(args[i].ToLower())
                {
                    case "--data":
                    case "-d":
                        if(i + 1 < args.Length)
                        {
                            Console.WriteLine($"Using {args[i].ToLower()} = {ModsPath}");
                            ModsPath = args[i + 1];
                            i++;
                        }
                        break;
                    case "--resources":
                    case "-r":
                        if(i + 1 < args.Length)
                        {
                            Console.WriteLine($"Using {args[i].ToLower()} = {ResourcesPath}");
                            ResourcesPath = args[i + 1];
                            i++;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Load the players preferences
        /// </summary>
        private void LoadPreferences()
        {
            string? appDataDirectory = GetAppDataPath();

            // If the app data path is bad here, just return its only the preferences
            if(string.IsNullOrEmpty(appDataDirectory)) return;

            string preferencesPath = Path.Combine(appDataDirectory, PreferencesFile);
            if(!File.Exists(preferencesPath))
            {
                File.Create(preferencesPath).Close();
            }

            IConfiguration preferences = new ConfigurationBuilder().AddIniFile(preferencesPath).Build();
            IConfigurationSection windowSection = preferences.GetSection("Window Settings");
            string? xPosition = windowSection["X"];
            string? yPosition = windowSection["Y"];
            string? width = windowSection["Width"];
            string? height = windowSection["Height"];
            string? maximized = windowSection["Maximized"];
            string? themeEnabled = windowSection["Theme"];

            if(xPosition != null) X = int.Parse(xPosition);
            if(yPosition != null) Y = int.Parse(yPosition);
            if(width != null) Width = int.Parse(width);
            if(height != null) Height = int.Parse(height);

            // if maximized is set to true it will override the other preferences
            if(maximized != null)
            {
                if(bool.Parse(maximized))
                    Maximize();
            }

            // TODO: more themes
            switch (themeEnabled)
            {
                case null:
                case "Default":
                    _theme = new DefaultTheme();
                    break;
                case "Futuristic":
                    _theme = new FuturisticTheme();
                    break;
                default:
                    Trace.WriteLine("WARNING: Unrecognized theme '" + themeEnabled + "', falling back to default");
                    _theme = new DefaultTheme();
                    break;
            }
        }

        /// <summary>
        /// Load the UserOrbitSettingsFile
        /// </summary>
        private void LoadUserOrbitSettings()
        {
            string? appDataDirectory = GetAppDataPath();

            if(string.IsNullOrEmpty(appDataDirectory))
                return;

            // Give up if the file doesn't exist
            string filePath = Path.Combine(appDataDirectory, UserOrbitSettingsFile);
            if(!File.Exists(filePath))
                return;

            string text = File.ReadAllText(filePath);
            var result = JsonConvert.DeserializeObject<List<List<UserOrbitSettings>>>(text);

            if(result != null)
                _state.UserOrbitSettingsMtx = result;
        }

        public void SaveOrbitSettings()
        {
            string? appDataDirectory = GetAppDataPath();
            if(appDataDirectory == null)
                return;

            string filePath = Path.Combine(appDataDirectory, UserOrbitSettingsFile);
            string output = JsonConvert.SerializeObject(_state.UserOrbitSettingsMtx);

            File.WriteAllText(filePath, output);
        }

        /// <summary>
        /// Deletes the contents of the destination directory and then copies the
        /// contents of the source directory to the destination directory.
        /// </summary>
        /// <param name="sourceDir">The directory to copy from</param>
        /// <param name="destinationDir">The directory to delete and then receive a copy of the source directory</param>
        public static void DeleteThenCopyToDirectory(string sourceDir, string destinationDir)
        {
            // Check if destination exists, if so delete it and all its contents
            if (Directory.Exists(destinationDir))
            {
                Directory.Delete(destinationDir, recursive: true);
            }

            // Create the destination directory fresh
            Directory.CreateDirectory(destinationDir);

            // Get all files and copy them
            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string destFile = Path.Combine(destinationDir, fileName);
                File.Copy(filePath, destFile, true);
            }

            // Recursively copy all subdirectories
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destinationDir, subDirName);
                DeleteThenCopyToDirectory(subDir, destSubDir);
            }
        }

        private static void RenderDebugText(IntPtr renderer, string text, int y)
        {
            if (renderer == IntPtr.Zero)
                return;

            SDL.Color white = new () {
                R = 255,
                G = 255,
                B = 255,
                A = 255
            };

            IntPtr surface = SDL3.TTF.RenderTextSolid(
                    Styles.SDLDefaultFont,
                    text,
                    0,
                    white);

            if (surface == IntPtr.Zero) {
                Trace.WriteLine("RenderDebugText: failed to create surface");
                return;
            }

            IntPtr texture = SDL.CreateTextureFromSurface(renderer, surface);

            if (texture == IntPtr.Zero) {
                SDL.DestroySurface(surface);

                Trace.WriteLine("RenderDebugText: failed to create texture from surface");
                return;
            }

            int h;
            int w;
            SDL3.TTF.GetStringSize(Styles.SDLDefaultFont, text, 0, out w, out h);

            SDL.FRect frect = new () {
                X = 5,
                Y = y,
                W = w,
                H = h
            };

            SDL.RenderTexture(renderer, texture, IntPtr.Zero, ref frect);

            SDL.DestroyTexture(texture);
            SDL.DestroySurface(surface);
        }
    }
}
