using System;
using System.Numerics;
using SDL2;
using ImGuiNET;
using ImGuiSDL2CS;
using System.Drawing;
using Pulsar4X.ECSLib;
using System.Linq;
using Pulsar4X.SDL2UI.Combat;
using Vector3 = System.Numerics.Vector3;

namespace Pulsar4X.SDL2UI
{
    public class Program
    {
        static SDL2Window Instance;
        [STAThread]
        public static void Main()
        {
            Instance = new PulsarMainWindow();
            Instance.Run();
            Instance.Dispose();
        }
    }

    public class PulsarMainWindow : ImGuiSDL2CSWindow
    {
        private GlobalUIState _state; 


        //private MemoryEditor _MemoryEditor = new MemoryEditor();
        //private byte[] _MemoryEditorData;

        //private FileDialog _Dialog = new FileDialog(false, false, true, false, false, false);

        Vector3 backColor = new Vector3(0 / 255f, 0 / 255f, 28 / 255f);

        int mouseDownX;
        int mouseDownY;
        int mouseDownAltX;
        int mouseDownAltY;

        public PulsarMainWindow()
            : base("Pulsar4X")
        {

            _state = new GlobalUIState(this);
            //_state.MainWinSize = this.Size;
            //_state.ShowMetrixWindow = true;
            // Create any managed resources and set up the main game window here.
            /*
            _MemoryEditorData = new byte[1024];
            Random rnd = new Random();
            for (int i = 0; i < _MemoryEditorData.Length; i++)
            {
                _MemoryEditorData[i] = (byte)rnd.Next(255);
            }
            */
            backColor = new Vector3(0 / 255f, 0 / 255f, 28 / 255f);

            _state.GalacticMap = new GalacticMapRender(this, _state);
            //_state.MapRendering = new SystemMapRendering(this, _state);


            OnEvent = MyEventHandler;
        }

        private bool MyEventHandler(SDL2Window window, SDL.SDL_Event e)
        {

            int mouseX;
            int mouseY;
            SDL.SDL_GetMouseState(out mouseX, out mouseY);
            
            if (!ImGuiSDL2CSHelper.HandleEvent(e, ref g_MouseWheel, g_MousePressed))
                return false;

            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN && e.button.button == 1 & !ImGui.GetIO().WantCaptureMouse)
            {
                _state.onFocusMoved();
                _state.Camera.IsGrabbingMap = true;
                _state.Camera.MouseFrameIncrementX = e.motion.x;
                _state.Camera.MouseFrameIncrementY = e.motion.y;
                mouseDownX = mouseX;
                mouseDownY = mouseY;
            }
            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP && e.button.button == 1)
            {
                //_state.onFocusMoved();
                _state.Camera.IsGrabbingMap = false;

                if (mouseDownX == mouseX && mouseDownY == mouseY) //click on map.  
                {
                    _state.MapClicked(_state.Camera.WorldCoordinate_m(mouseX, mouseY), MouseButtons.Primary);//sdl and imgu use different numbers for buttons.
                }
            }

            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN && e.button.button == 3 & !ImGui.GetIO().WantCaptureMouse)
            {
                _state.onFocusMoved();
                mouseDownAltX = mouseX;
                mouseDownAltY = mouseY;
            }
            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP && e.button.button == 3)
            {
                _state.onFocusMoved();
                _state.Camera.IsGrabbingMap = false;

                if (mouseDownAltX == mouseX && mouseDownAltY == mouseY) //click on map.  
                {
                    _state.MapClicked(_state.Camera.WorldCoordinate_m(mouseX, mouseY), MouseButtons.Alt);//sdl and imgu use different numbers for buttons.
                }
            }

            if (_state.Camera.IsGrabbingMap)
            {
                int deltaX = _state.Camera.MouseFrameIncrementX - e.motion.x;
                int deltaY = _state.Camera.MouseFrameIncrementY - e.motion.y;
                _state.Camera.WorldOffset_m(deltaX, deltaY);

                _state.Camera.MouseFrameIncrementX = e.motion.x;
                _state.Camera.MouseFrameIncrementY = e.motion.y;

            }


            if (e.type == SDL.SDL_EventType.SDL_KEYUP)
            {
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
                {
                    MainMenuItems mainMenu = MainMenuItems.GetInstance();
                    mainMenu.IsActive = true;
                }
            }

            if (e.type == SDL.SDL_EventType.SDL_MOUSEWHEEL &! ImGui.GetIO().WantCaptureMouse)
            {
                _state.onFocusMoved();
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
            foreach (var systemState in _state.StarSystemStates.Values)
            {
                systemState.PreFrameSetup();
            }

            GL.ClearColor(backColor.X, backColor.Y, backColor.Z, 1f);
            GL.Clear(GL.Enum.GL_COLOR_BUFFER_BIT);

            _state.GalacticMap.Draw();
            //_state.MapRendering.Draw();

            // Render ImGui on top of the rest. this eventualy calls overide void ImGuiLayout();
            base.ImGuiRender();

            foreach (var systemState in _state.StarSystemStates.Values)
            {
                systemState.PostFrameCleanup();
            }
        }

        public unsafe override void ImGuiLayout()
        {
            if (_state.ShowImgDbg)
            {
                
                ImGui.NewLine();
                SDL.SDL_RendererInfo renderInfo;
                SDL.SDL_GetRendererInfo(_state.rendererPtr, out renderInfo);
                ImGui.Text("SDL RenderInfo:");
                ImGui.Text("Name : " + renderInfo.name.ToString());
                ImGui.Text("Flags: " +renderInfo.flags.ToString());
                ImGui.Text("MaxTexH: " +renderInfo.max_texture_height.ToString());
                ImGui.Text("MaxTexW: " +renderInfo.max_texture_width.ToString());
                ImGui.Text("NumTxtFormats: " +renderInfo.num_texture_formats.ToString());
                //ImGui.Text("Flags: " +renderInfo.texture_formats.ToString());
                        
                        
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
                    int h, w, a;
                    uint f;
                    int q = SDL.SDL_QueryTexture(kvp.Value, out f, out a, out w, out h);
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

            
            foreach (var item in _state.LoadedWindows.Values.ToArray())
            {
                item.Display();
            }

            //because the nameIcons are IMGUI not SDL we draw them here. 
            _state.GalacticMap.DrawNameIcons();
            var dispsize = ImGui.GetIO().DisplaySize;
            var pos = new Vector2(0, dispsize.Y - ImGui.GetFrameHeightWithSpacing());
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            var flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
            if (ImGui.Begin("GitHash", flags))
            {
                ImGui.Text(AssemblyInfo.GetGitHash());
            }
        }

    }
    public enum MouseButtons
    {
        Primary,
        Alt,
        Middle
    }
    public class MouseState
    {
        
    }


}
