using System;
using System.Runtime.InteropServices;
using SDL2;
using ImGuiNET;
using ImGuiSDL2CS;
using System.Drawing;
using Pulsar4X.ECSLib;
using System.Linq;

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

        IntPtr _logoTexture;

        //private MemoryEditor _MemoryEditor = new MemoryEditor();
        //private byte[] _MemoryEditorData;

        //private FileDialog _Dialog = new FileDialog(false, false, true, false, false, false);

        ImVec3 backColor = new ImVec3(0 / 255f, 0 / 255f, 28 / 255f);

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
            backColor = new ImVec3(0 / 255f, 0 / 255f, 28 / 255f);

            _state.MapRendering = new SystemMapRendering(this, _state);


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
                    _state.MapClicked(_state.Camera.WorldCoordinate(mouseX, mouseY), MouseButtons.Primary);//sdl and imgu use different numbers for buttons.
                }
            }

            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN && e.button.button == 3 & !ImGui.GetIO().WantCaptureMouse)
            {
                mouseDownAltX = mouseX;
                mouseDownAltY = mouseY;
            }
            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP && e.button.button == 3)
            {
                _state.Camera.IsGrabbingMap = false;

                if (mouseDownAltX == mouseX && mouseDownAltY == mouseY) //click on map.  
                {
                    _state.MapClicked(_state.Camera.WorldCoordinate(mouseX, mouseY), MouseButtons.Alt);//sdl and imgu use different numbers for buttons.
                }
            }

            if (_state.Camera.IsGrabbingMap)
            {
                int deltaX = _state.Camera.MouseFrameIncrementX - e.motion.x;
                int deltaY = _state.Camera.MouseFrameIncrementY - e.motion.y;
                _state.Camera.WorldOffset(deltaX, deltaY);

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

            if (e.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
            {
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

        public unsafe override void ImGuiLayout()
        {
            /* uncomment this to attempt image display 
            ImGui.Image(_state.GLImageDictionary["PlayImg"], new ImVec2(16, 16), new ImVec2(0, 0), new ImVec2(1, 1), new ImVec4(1,1,1,1), new ImVec4(1, 0, 0, 1));
            ImGui.Image(_state.GLImageDictionary["PlayImg"], new ImVec2(16, 16), new ImVec2(0, 0), new ImVec2(16, 16), new ImVec4(1,1,1,1), new ImVec4(255, 0, 0, 255));
            ImGui.Image(_state.GLImageDictionary["PlayImg"], new ImVec2(16, 16), new ImVec2(0, 0), new ImVec2(1, 1), new ImVec4(0,0,0,1), new ImVec4(255, 0, 0, 255));
            ImGui.Image(_state.GLImageDictionary["PlayImg"], new ImVec2(16, 16), new ImVec2(0, 0), new ImVec2(16, 16), new ImVec4(0,0,0,1), new ImVec4(255, 0, 0, 255));
*/

            //ImGui.ShowMetricsWindow(ref _state.ShowMetrixWindow);

            foreach (var item in _state.LoadedWindows.Values.ToArray())
            {
                item.Display();
            }
            foreach (var item in _state.MapRendering._nameIcons.Values)
            {
                item.Draw(_state.rendererPtr, _state.Camera);
            }
        }


        public override void ImGuiRender()
        {
            GL.ClearColor(backColor.X, backColor.Y, backColor.Z, 1f);
            GL.Clear(GL.Enum.GL_COLOR_BUFFER_BIT);

            /*
            var imgSize = new SDL.SDL_Rect() { x = 0, y = 0, h = 98, w = 273 };
            var txtr = (IntPtr)_state.SDLImageDictionary["Logo"];
            SDL.SDL_RenderCopy(_state.MapRendering.rendererPtr, txtr, ref imgSize, ref imgSize);
            */

            _state.MapRendering.Draw();

            // Render ImGui on top of the rest.
            base.ImGuiRender();
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
