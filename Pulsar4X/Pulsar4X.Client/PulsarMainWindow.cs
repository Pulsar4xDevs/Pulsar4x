using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using SDL2;
using Microsoft.Extensions.Configuration;

namespace Pulsar4X.SDL2UI
{
    public enum MouseButtons
    {
        Primary,
        Alt,
        Middle
    }

    public class PulsarMainWindow : ImGuiSDL2CSWindow
    {
        public const string OrgName = "Pulsar4X";
        public const string AppName = "Pulsar4X";
        public const string PreferencesFile = "preferences.ini";
        private readonly GlobalUIState _state;

        Vector3 backColor;
        int mouseDownX;
        int mouseDownY;
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
                // Read and apply any window preferences
                string preferencesDirectory = SDL.SDL_GetPrefPath(OrgName, AppName);
                string preferencesPath = Path.Combine(preferencesDirectory, PreferencesFile);
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
                        SDL.SDL_MaximizeWindow(_Handle);
                }
            }
            catch(Exception e)
            {
                // It's just a preferences file, continue on
            }
        }

        private bool MyEventHandler(SDL2Window window, SDL.SDL_Event e)
        {
            SDL.SDL_GetMouseState(out int mouseX, out int mouseY);

            if (!ImGuiSDL2CSHelper.HandleEvent(e, ref g_MouseWheel, g_MousePressed))
                return false;

            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN && e.button.button == 1 & !ImGui.GetIO().WantCaptureMouse)
            {
                _state.OnFocusMoved();
                _state.Camera.IsGrabbingMap = true;
                _state.Camera.MouseFrameIncrementX = e.motion.x;
                _state.Camera.MouseFrameIncrementY = e.motion.y;
                mouseDownX = mouseX;
                mouseDownY = mouseY;
            }

            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP && e.button.button == 1)
            {
                _state.Camera.IsGrabbingMap = false;

                if (mouseDownX == mouseX && mouseDownY == mouseY) //click on map.
                {
                    _state.MapClicked(_state.Camera.WorldCoordinate_m(mouseX, mouseY), MouseButtons.Primary); //sdl and imgu use different numbers for buttons.
                }
            }

            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN && e.button.button == 3 & !ImGui.GetIO().WantCaptureMouse)
            {
                _state.OnFocusMoved();
                mouseDownAltX = mouseX;
                mouseDownAltY = mouseY;
            }

            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP && e.button.button == 3)
            {
                _state.OnFocusMoved();
                _state.Camera.IsGrabbingMap = false;

                if (mouseDownAltX == mouseX && mouseDownAltY == mouseY) //click on map.
                {
                    _state.MapClicked(_state.Camera.WorldCoordinate_m(mouseX, mouseY), MouseButtons.Alt);//sdl and imgu use different numbers for buttons.
                }
            }

            if (_state.Camera.IsGrabbingMap && e.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
            {
                int deltaX = _state.Camera.MouseFrameIncrementX - e.motion.x;
                int deltaY = _state.Camera.MouseFrameIncrementY - e.motion.y;
                _state.Camera.WorldOffset_m(deltaX, deltaY);

                _state.Camera.MouseFrameIncrementX = e.motion.x;
                _state.Camera.MouseFrameIncrementY = e.motion.y;

            }

            // The top of the hotkey stack should list for hotkeys
            _state.HotKeys.Peek().HandleEvent(e);

            if (e.type == SDL.SDL_EventType.SDL_MOUSEWHEEL &! ImGui.GetIO().WantCaptureMouse)
            {
                _state.OnFocusMoved();
                if (e.wheel.y > 0)
                {
                    _state.Camera.ZoomIn(mouseX, mouseY);
                }
                else if (e.wheel.y < 0)
                {
                    _state.Camera.ZoomOut(mouseX, mouseY);
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

            GL.ClearColor(backColor.X, backColor.Y, backColor.Z, 1f);
            GL.Clear(GL.Enum.GL_COLOR_BUFFER_BIT);

            _state.GalacticMap.Draw();

            // Render ImGui on top of the rest. this eventualy calls overide void ImGuiLayout();
            base.ImGuiRender();

            foreach (var (_, systemState) in _state.StarSystemStates)
            {
                systemState.PostFrameCleanup();
            }
        }

        public unsafe override void ImGuiLayout()
        {
            //because the nameIcons are IMGUI not SDL we draw them here.
            _state.GalacticMap.DrawNameIcons();

            if (_state.ShowImgDbg)
            {
                ImGui.NewLine();
                SDL.SDL_GetRendererInfo(_state.rendererPtr, out var renderInfo);
                ImGui.Text("SDL RenderInfo:");
                ImGui.Text("Name : " + renderInfo.name.ToString());
                ImGui.Text("Flags: " +renderInfo.flags.ToString());
                ImGui.Text("MaxTexH: " +renderInfo.max_texture_height.ToString());
                ImGui.Text("MaxTexW: " +renderInfo.max_texture_width.ToString());
                ImGui.Text("NumTxtFormats: " +renderInfo.num_texture_formats.ToString());

                SDL.SDL_GetRenderDriverInfo(0, out renderInfo);
                ImGui.Text("SDL RenderDriverInfo:");
                ImGui.Text("Name : " + renderInfo.name.ToString());
                ImGui.Text("Flags: " +renderInfo.flags.ToString());
                ImGui.Text("MaxTexH: " +renderInfo.max_texture_height.ToString());
                ImGui.Text("MaxTexW: " +renderInfo.max_texture_width.ToString());
                ImGui.Text("NumTxtFormats: " +renderInfo.num_texture_formats.ToString());
                ImGui.NewLine();

                foreach (var kvp in _state.SDLImageDictionary)
                {
                    int q = SDL.SDL_QueryTexture(kvp.Value, out uint f, out int a, out int w, out int h);
                    if (q != 0)
                    {
                        ImGui.Text("QueryResult: " + q);
                        ImGui.Text(SDL.SDL_GetError());
                    }
                    ImGui.Image(kvp.Value, new System.Numerics.Vector2(w, h));
                }
            }

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

            var dispsize = ImGui.GetIO().DisplaySize;
            var pos = new System.Numerics.Vector2(0, dispsize.Y - ImGui.GetFrameHeightWithSpacing());
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            var flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
            if (ImGui.Begin("GitHash", flags))
            {
                ImGui.Text("Version: " + AssemblyInfo.GetGitHash());
            }
        }
    }
}