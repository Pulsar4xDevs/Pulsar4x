using System;
using System.Runtime.InteropServices;
using SDL2;
using ImGuiNET;
using ImGuiSDL2CS;
using System.Drawing;

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
        private GlobalUIState _state; // = new GlobalUIState(new Camera(this));

        IntPtr _logoTexture;

        private MemoryEditor _MemoryEditor = new MemoryEditor();
        private byte[] _MemoryEditorData;

        private FileDialog _Dialog = new FileDialog(false, false, true, false, false, false);

        ImVec3 backColor = new ImVec3(0 / 255f, 0 / 255f, 28 / 255f);
         

        public PulsarMainWindow()
            : base("Pulsar4X")
        {

            _state = new GlobalUIState(this);
            //_state.MainWinSize = this.Size;

            // Create any managed resources and set up the main game window here.
            _MemoryEditorData = new byte[1024];
            Random rnd = new Random();
            for (int i = 0; i < _MemoryEditorData.Length; i++)
            {
                _MemoryEditorData[i] = (byte)rnd.Next(255);
            }
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

            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN && e.button.button == 1 &! ImGui.IO.WantCaptureMouse)
            { 
                _state.Camera.IsGrabbingMap = true;
                _state.Camera.MouseFrameIncrementX = e.motion.x;
                _state.Camera.MouseFrameIncrementY = e.motion.y;
            }
            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP && e.button.button == 1)
            {
                _state.Camera.IsGrabbingMap = false;

            }
                 

            if (_state.Camera.IsGrabbingMap)
            {
                int deltaX = _state.Camera.MouseFrameIncrementX - e.motion.x;
                int deltaY = _state.Camera.MouseFrameIncrementY - e.motion.y;
                _state.Camera.WorldOffset(deltaX, deltaY);

                _state.Camera.MouseFrameIncrementX = e.motion.x;
                _state.Camera.MouseFrameIncrementY = e.motion.y;

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

            foreach (var item in _state.OpenWindows.ToArray())
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

            var imgSize = new SDL.SDL_Rect() { x = 0, y = 0, h = 98, w = 273 };
            SDL.SDL_RenderCopy(_state.MapRendering.rendererPtr, _logoTexture, ref imgSize, ref imgSize);
            _state.MapRendering.Draw();

            // Render ImGui on top of the rest.
            base.ImGuiRender();
        }

    }

    public abstract class PulsarGuiWindow
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.Default;
        internal bool IsActive = false;
        internal abstract void Display();
    }
}
