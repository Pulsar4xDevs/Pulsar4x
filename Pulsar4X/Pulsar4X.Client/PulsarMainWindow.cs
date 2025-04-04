using System;
using System.IO;
using System.Linq;
using System.Numerics;
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
#if DEBUG
        private ImGuiWindowFlags _gitHashFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
#endif
        public const string PreferencesFile = "preferences.ini";
        public const string UserOrbitSettingsFile = "orbit-settings.json";
        public const string SavesPath = "Saves";
        public static string ModsPath = "Mods";
        public static string ResourcesPath = "Resources";
        private readonly GlobalUIState _state;

        float mouseDownX;
        float mouseDownY;
        int mouseDownAltX;
        int mouseDownAltY;

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

                // Apply any saved user orbit settings
                LoadUserOrbitSettings();

                // Load fonts
                var fontPtr = PlatformBackend.LoadFont(ResourcesPath, "ProggyClean.ttf", 13f);
                var texturePtr = ImGuiRenderer.CreateFontsTexture(fontPtr);
                fontPtr = PlatformBackend.LoadFont(ResourcesPath, "DejaVuSans.ttf", 13f, "ΩωΝνΔδθΘϖ", true);
                texturePtr = ImGuiRenderer.CreateFontsTexture(fontPtr);

                // This one works
                // var fontPtr = PlatformBackend.LoadFont(ResourcesPath, "PixelOperator.ttf", 16f);
                // var fontTexture = ImGuiRenderer.CreateFontsTexture(fontPtr);

                // This one doesn't
                // var fontPtr = PlatformBackend.LoadFont(ResourcesPath, "JetBrainsMono-Regular.ttf", 16f);
                // var fontTexture = ImGuiRenderer.CreateFontsTexture(fontPtr);
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error setting up game data: {e.Message}");
            }
        }

        public override void HandleEvent(SDL.Event e)
        {
            (float mouseX, float mouseY, SDL.MouseButtonFlags mouseFlags) = GetMouseState();

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

            if (e.Type == (uint)SDL.EventType.MouseButtonDown && e.Button.Button == 1 & !PlatformBackend.WantsMouseCapture())
            {
                _state.OnFocusMoved();
                _state.Camera.IsGrabbingMap = true;
                _state.Camera.MouseFrameIncrementX = e.Motion.X;
                _state.Camera.MouseFrameIncrementY = e.Motion.Y;
                mouseDownX = mouseX;
                mouseDownY = mouseY;
            }

            if (e.Type == (uint)SDL.EventType.MouseButtonUp && e.Button.Button == 1)
            {
                _state.Camera.IsGrabbingMap = false;

                if (mouseDownX == mouseX && mouseDownY == mouseY) //click on map.
                {
                    _state.MapClicked(_state.Camera.WorldCoordinate_m(mouseX, mouseY), MouseButtons.Primary); //sdl and imgu use different numbers for buttons.
                }
            }

            if (e.Type == (uint)SDL.EventType.MouseButtonDown && e.Button.Button == 3 & !PlatformBackend.WantsMouseCapture())
            {
                _state.OnFocusMoved();
                mouseDownAltX = (int)mouseX;
                mouseDownAltY = (int)mouseY;
            }

            if (e.Type == (uint)SDL.EventType.MouseButtonUp && e.Button.Button == 3)
            {
                _state.OnFocusMoved();
                _state.Camera.IsGrabbingMap = false;

                if (mouseDownAltX == mouseX && mouseDownAltY == mouseY) //click on map.
                {
                    _state.MapClicked(_state.Camera.WorldCoordinate_m(mouseX, mouseY), MouseButtons.Alt);//sdl and imgu use different numbers for buttons.
                }
            }

            if (_state.Camera.IsGrabbingMap && e.Type == (uint)SDL.EventType.MouseMotion)
            {
                int deltaX = (int)(_state.Camera.MouseFrameIncrementX - e.Motion.X);
                int deltaY = (int)(_state.Camera.MouseFrameIncrementY - e.Motion.Y);
                _state.Camera.WorldOffset_m(deltaX, deltaY);

                _state.Camera.MouseFrameIncrementX = e.Motion.X;
                _state.Camera.MouseFrameIncrementY = e.Motion.Y;

            }

            // The top of the hotkey stack should list for hotkeys
            _state.HotKeys.Peek().HandleEvent(e);

            if (e.Type == (uint)SDL.EventType.MouseWheel & !PlatformBackend.WantsMouseCapture())
            {
                _state.OnFocusMoved();
                if (e.Wheel.Y > 0)
                {
                    _state.Camera.ZoomIn((int)mouseX, (int)mouseY);
                }
                else if (e.Wheel.Y < 0)
                {
                    _state.Camera.ZoomOut((int)mouseX, (int)mouseY);
                }
            }
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

            foreach (var (_, systemState) in _state.StarSystemStates)
            {
                systemState.PreFrameSetup();
            }

            _state.GalacticMap?.Update();
        }

        public override void Render()
        {
            base.Render();

            // Render the game
            _state.GalacticMap?.Draw();

            // Render the UI
            RenderUI();
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

            // If in DEBUG render the git hash as the version in the corner of the screen
#if DEBUG
            var dispsize = ImGui.GetIO().DisplaySize;
            var pos = new Vector2(0, dispsize.Y - ImGui.GetFrameHeightWithSpacing());
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            if (Client.Interface.Widgets.Window.Begin("GitHash", _gitHashFlags))
            {
                ImGui.Text("Version: " + AssemblyInfo.GetGitHash());
                Client.Interface.Widgets.Window.End();
            }
#endif
        }

        public override void Exit()
        {
            // save the user orbit settings on exit
            SaveOrbitSettings();
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

            if(themeEnabled != null)
            {
                if(bool.Parse(themeEnabled))
                {
                    var theme = new FuturisticTheme();
                    theme.Apply();
                }
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

            if(!File.Exists(filePath))
                File.Create(filePath);

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
    }
}