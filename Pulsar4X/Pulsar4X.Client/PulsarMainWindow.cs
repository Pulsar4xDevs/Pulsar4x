using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using SDL3;
using Microsoft.Extensions.Configuration;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Client.State;

namespace Pulsar4X.SDL2UI
{
    public enum MouseButtons
    {
        Primary,
        Alt,
        Middle
    }

    public class PulsarMainWindow : ImGuiSDL3CSWindow
    {
#if DEBUG
        private ImGuiWindowFlags _gitHashFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
#endif
        public const string OrgName = "Pulsar4X";
        public const string AppName = "Pulsar4X";
        public const string PreferencesFile = "preferences.ini";
        public const string SavesPath = "Saves";
        public const string ModsPath = "Mods";
        private readonly GlobalUIState _state;

        Vector3 backColor;
        float mouseDownX;
        float mouseDownY;
        int mouseDownAltX;
        int mouseDownAltY;

        public PulsarMainWindow()
            : base(AppName)
        {
            _state = new GlobalUIState(this);
            _state.GalacticMap = new GalacticMapRender(this, _state);
            backColor = new Vector3(0 / 255f, 0 / 255f, 28 / 255f);
            OnEvent = MyEventHandler;

            try
            {
                string? appDataDirectory = SDL.GetPrefPath(OrgName, AppName);

                if(appDataDirectory == null) throw new NullReferenceException("App data directory cannot be null");

                // Check for Saves directory and create it if it doesn't exist
                string savesDirectory = Path.Combine(appDataDirectory, SavesPath);
                if (!Directory.Exists(savesDirectory))
                {
                    Directory.CreateDirectory(savesDirectory);
                }

                // Check for Mods directory and create it if it doesn't exist
                string modsDirectory = Path.Combine(appDataDirectory, ModsPath);
                if(!Directory.Exists(modsDirectory))
                {
                    Directory.CreateDirectory(modsDirectory);
                }

                // Make sure the base game mod is copied over to the mod directory
                string sourceData = "Data";
                DeleteThenCopyToDirectory(sourceData, modsDirectory);

                // Load the available mods
                ModsState.RefreshModListFromModsDirectory();


                // Read and apply any window preferences
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

                if(xPosition != null) X = int.Parse(xPosition);
                if(yPosition != null) Y = int.Parse(yPosition);
                if(width != null) Width = int.Parse(width);
                if(height != null) Height = int.Parse(height);

                // if maximized is set to true it will override the other preferences
                if(maximized != null)
                {
                    bool isMaximized = bool.Parse(maximized);
                    if(isMaximized)
                        SDL.MaximizeWindow(_Handle);
                }
            }
            catch(Exception)
            {
                // It's just a preferences file, continue on
            }
        }

        private bool MyEventHandler(SDL3Window window, SDL.Event e)
        {
            SDL.GetMouseState(out float mouseX, out float mouseY);

            if (!ImGuiSDL3CSHelper.HandleEvent(e, ref g_MouseWheel, g_MousePressed))
                return false;

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
                return false;
            }

            if (e.Type == (uint)SDL.EventType.MouseButtonDown && e.Button.Button == 1 & !ImGui.GetIO().WantCaptureMouse)
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

            if (e.Type == (uint)SDL.EventType.MouseButtonDown && e.Button.Button == 3 & !ImGui.GetIO().WantCaptureMouse)
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

            if (e.Type == (uint)SDL.EventType.MouseWheel &! ImGui.GetIO().WantCaptureMouse)
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
            return true;
        }

        public override void ImGuiRender()
        {
            foreach (var (_, systemState) in _state.StarSystemStates)
            {
                systemState.PreFrameSetup();
            }

            Renderer.Clear(backColor.X, backColor.Y, backColor.Z, 1f);
            Renderer.BeginFrame();

            _state.GalacticMap.Draw();

            // Render ImGui on top of the rest. this eventualy calls overide void ImGuiLayout();
            base.ImGuiRender();

            foreach (var (_, systemState) in _state.StarSystemStates)
            {
                systemState.PostFrameCleanup();
            }
        }

        private IntPtr _colorTesttexture = IntPtr.Zero;
        private IntPtr _pixels;

        public unsafe override void ImGuiLayout()
        {
            //because the nameIcons are IMGUI not SDL we draw them here.
            _state.GalacticMap.DrawNameIcons();

            // if (_state.ShowImgDbg)
            // {
            //     ImGui.NewLine();
            //     SDL.SDL_GetRendererInfo(_state.SDLRendererPtr, out var renderInfo);
            //     ImGui.Text("SDL RenderInfo:");
            //     ImGui.Text("Name : " + renderInfo.name.ToString());
            //     ImGui.Text("Flags: " +renderInfo.flags.ToString());
            //     ImGui.Text("MaxTexH: " +renderInfo.max_texture_height.ToString());
            //     ImGui.Text("MaxTexW: " +renderInfo.max_texture_width.ToString());
            //     ImGui.Text("NumTxtFormats: " +renderInfo.num_texture_formats.ToString());

            //     SDL.SDL_GetRenderDriverInfo(0, out renderInfo);
            //     ImGui.Text("SDL RenderDriverInfo:");
            //     ImGui.Text("Name : " + renderInfo.name.ToString());
            //     ImGui.Text("Flags: " +renderInfo.flags.ToString());
            //     ImGui.Text("MaxTexH: " +renderInfo.max_texture_height.ToString());
            //     ImGui.Text("MaxTexW: " +renderInfo.max_texture_width.ToString());
            //     ImGui.Text("NumTxtFormats: " +renderInfo.num_texture_formats.ToString());
            //     ImGui.NewLine();

            //     if(_colorTesttexture == IntPtr.Zero)
            //         SDL2Helper.CreateTestTexture(_state.ViewPort.Renderer, ref _colorTesttexture);
            //     ImGui.Image(_colorTesttexture, new System.Numerics.Vector2(200, 200));
            //     if(ImGui.Button("refresh"))
            //         SDL2Helper.CreateTestTexture(_state.ViewPort.Renderer, ref _colorTesttexture);

            //     foreach (var kvp in _state.SDLImageDictionary)
            //     {
            //         (int txWidth, int txHeight) = _state.ViewPort.Renderer.GetTextureDimensions(kvp.Value);
            //         ImGui.Image(kvp.Value, new System.Numerics.Vector2(txWidth, txHeight));
            //         ImGui.Text(kvp.Key);
            //     }
            // }

            if (_state.ShowMetrixWindow)
                ImGui.ShowMetricsWindow(ref _state.ShowMetrixWindow);

            if (_state.ShowDemoWindow)
            {
                ImGui.ShowDemoWindow();
                ImGui.ShowUserGuide();
            }

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

#if DEBUG
            var dispsize = ImGui.GetIO().DisplaySize;
            var pos = new System.Numerics.Vector2(0, dispsize.Y - ImGui.GetFrameHeightWithSpacing());
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            if (Window.Begin("GitHash", _gitHashFlags))
            {
                ImGui.Text("Version: " + AssemblyInfo.GetGitHash());
                Window.End();
            }
#endif
        }

        public static string? GetAppDataPath()
        {
            return SDL.GetPrefPath(OrgName, AppName);
        }

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