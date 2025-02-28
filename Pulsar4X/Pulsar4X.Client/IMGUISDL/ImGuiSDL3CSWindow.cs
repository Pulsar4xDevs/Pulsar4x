using SDL3;
using System;
using ImGuiNET;
using System.IO;
using System.Numerics;
using Pulsar4X.Client.Interface.Themes;

namespace ImGuiSDL2CS
{
    public class ImGuiSDL3CSWindow : SDL3Window {

        protected readonly bool _IsSuperClass;

        protected double g_Time = 0.0f;
        protected readonly bool[] g_MousePressed = { false, false, false };
        protected float g_MouseWheel = 0.0f;
        protected IntPtr _fontTexture = IntPtr.Zero;

        protected ITheme Theme { get; set; }

        public Vector2 Position
        {
            get
            {
                SDL.GetWindowPosition(Handle, out int x, out int y);
                return new Vector2(x, y);
            }
            set
            {
                SDL.SetWindowPosition(Handle, (int) Math.Round(value.X), (int) Math.Round(value.Y));
            }
        }

        public Vector2 Size
        {
            get
            {
                SDL.GetWindowSize(Handle, out int x, out int y);
                return new Vector2(x, y);
            }
            set
            {
                SDL.SetWindowSize(Handle, (int) Math.Round(value.X), (int) Math.Round(value.Y));
            }
        }

        public ImGuiSDL3CSWindow(
            string title = "ImGui.NET-SDL2-CS Window",
            int x = 0, int y = 0,
            int width = 800, int height = 600,
            SDL.WindowFlags flags = SDL.WindowFlags.OpenGL | SDL.WindowFlags.Resizable | SDL.WindowFlags.Hidden
        ) : base(title, x, y, width, height, flags)
        {
            _IsSuperClass = GetType() == typeof(ImGuiSDL3CSWindow);
            var io = ImGui.GetIO();
            ImGuiSDL3CSHelper.Init();
            OnEvent = ImGuiOnEvent;
            OnLoop = ImGuiOnLoop;
            SDL.SetHint("SDL_RENDER_LINE_METHOD", "2"); //https://github.com/libsdl-org/SDL/blob/1fc7f681187f80ccd6b9625214b47db665cd9aaf/include/SDL_hints.h#L1304-L1315

            // Apply ImGui theme
            // TODO: allow player to select/change this
            ApplyTheme(new FuturisticTheme());
        }

        public override void Run()
        {
            if (!File.Exists("imgui.ini"))
                File.WriteAllText("imgui.ini", "");

            Create();

            base.Run();
        }

        public void ApplyTheme(ITheme theme)
        {
            Theme = theme;
            Theme.Apply();
        }

        public bool ImGuiOnEvent(SDL3Window window, SDL.Event e)
            => ImGuiSDL3CSHelper.HandleEvent(e, ref g_MouseWheel, g_MousePressed);

        public void ImGuiOnLoop(SDL3Window window)
        {
            ImGuiRender();
            Swap();
        }

        public virtual void ImGuiRender()
        {
            SDL.MouseButtonFlags mouseMask = SDL.GetMouseState(out float mouseX, out float mouseY);

            if ((SDL.GetWindowFlags(Handle) & SDL.WindowFlags.MouseFocus) == 0)
                mouseX = mouseY = -1;

            ImGuiSDL3CSHelper.NewFrame(Size, Vector2.One, new Vector2(mouseX, mouseY), mouseMask, ref g_MouseWheel, g_MousePressed, ref g_Time);

            ImGuiLayout();

            ImGui.Render();
            Renderer.RenderImGui(ImGui.GetDrawData(), (int) Size.X, (int) Size.Y);
        }

        public virtual void ImGuiLayout()
        {
            if (_IsSuperClass)
                ImGui.Text($"Create a new class inheriting {GetType().FullName}, overriding {nameof(ImGuiLayout)}!");
            else
                ImGui.Text($"Override {nameof(ImGuiLayout)} in {GetType().FullName}!");
        }

        protected unsafe virtual void Create()
        {
            var io = ImGui.GetIO();

            // Build texture atlas
            io.Fonts.GetTexDataAsAlpha8(out byte* pixels, out int width, out int height);

            _fontTexture = new IntPtr(Renderer.CreateDefaultFontTexture(width, height, (IntPtr)pixels));

            // Store the texture identifier in the ImFontAtlas substructure.
            io.Fonts.SetTexID(_fontTexture);
            io.Fonts.ClearTexData(); // Clears CPU side texture data.
        }

        protected override void Dispose(bool disposing)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            if (disposing) {
                // Dispose managed state (managed objects).
            }

            // Free unmanaged resources (unmanaged objects) and override a finalizer below.
            // Set large fields to null.
            if (_fontTexture != IntPtr.Zero) {
                // Texture gets deleted with the context.
                // GL.DeleteTexture(g_FontTexture);
                if ( io.Fonts.TexID == _fontTexture)
                    io.Fonts.TexID = IntPtr.Zero;
                _fontTexture = IntPtr.Zero;
            }

            base.Dispose(disposing);
        }

        ~ImGuiSDL3CSWindow()
        {
            Dispose(false);
        }
    }
}
