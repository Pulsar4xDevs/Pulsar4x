using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImGuiNET;
using System.IO;
using System.Runtime.InteropServices;
using System.Numerics;


namespace ImGuiSDL2CS {
    public class ImGuiSDL2CSWindow : SDL2Window {

        protected readonly bool _IsSuperClass;

        protected double g_Time = 0.0f;
        protected readonly bool[] g_MousePressed = { false, false, false };
        protected float g_MouseWheel = 0.0f;
        protected IntPtr g_FontTexture = IntPtr.Zero;

        public Vector2 Position {
            get {
                int x, y;
                SDL.SDL_GetWindowPosition(Handle, out x, out y);
                return new Vector2(x, y);
            }
            set {
                SDL.SDL_SetWindowPosition(Handle, (int) Math.Round(value.X), (int) Math.Round(value.Y));
            }
        }

        public System.Numerics.Vector2 Size {
            get {
                int x, y;
                SDL.SDL_GetWindowSize(Handle, out x, out y);
                return new System.Numerics.Vector2(x, y);
            }
            set {
                SDL.SDL_SetWindowSize(Handle, (int) Math.Round(value.X), (int) Math.Round(value.Y));
            }
        }

        public ImGuiSDL2CSWindow(
            string title = "ImGui.NET-SDL2-CS Window",
            int x = SDL.SDL_WINDOWPOS_CENTERED, int y = SDL.SDL_WINDOWPOS_CENTERED,
            int width = 800, int height = 600,
            SDL.SDL_WindowFlags flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN
        ) : base(title, x, y, width, height, flags) {
            _IsSuperClass = GetType() == typeof(ImGuiSDL2CSWindow);
            var io = ImGui.GetIO();
            ImGuiSDL2CSHelper.Init();
            OnEvent = ImGuiOnEvent;
            OnLoop = ImGuiOnLoop;
            SDL.SDL_SetHint("SDL_RENDER_LINE_METHOD", "2"); //https://github.com/libsdl-org/SDL/blob/1fc7f681187f80ccd6b9625214b47db665cd9aaf/include/SDL_hints.h#L1304-L1315
        }

        public override void Run() {
            if (!File.Exists("imgui.ini"))
                File.WriteAllText("imgui.ini", "");

            Create();

            base.Run();
        }

        public bool ImGuiOnEvent(SDL2Window window, SDL.SDL_Event e)
            => ImGuiSDL2CSHelper.HandleEvent(e, ref g_MouseWheel, g_MousePressed);

        public void ImGuiOnLoop(SDL2Window window) {
            GL.ClearColor(0.1f, 0.125f, 0.15f, 1f);
            GL.Clear(GL.Enum.GL_COLOR_BUFFER_BIT);

            ImGuiRender();

            Swap();
        }

        public virtual void ImGuiRender() {
            int mouseX, mouseY;
            uint mouseMask = SDL.SDL_GetMouseState(out mouseX, out mouseY);
            if ((SDL.SDL_GetWindowFlags(Handle) & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS) == 0)
                mouseX = mouseY = -1;
            ImGuiSDL2CSHelper.NewFrame(Size, System.Numerics.Vector2.One, new System.Numerics.Vector2(mouseX, mouseY), mouseMask, ref g_MouseWheel, g_MousePressed, ref g_Time);

            ImGuiLayout();

            ImGuiSDL2CSHelper.Render(Size);
        }

        public virtual void ImGuiLayout() {
            if (_IsSuperClass)
                ImGui.Text($"Create a new class inheriting {GetType().FullName}, overriding {nameof(ImGuiLayout)}!");
            else
                ImGui.Text($"Override {nameof(ImGuiLayout)} in {GetType().FullName}!");
        }

        protected unsafe virtual void Create() {
            var io = ImGui.GetIO();

            // Build texture atlas
            byte* pixels;
            int width, height, bytesPerPixel;

            io.Fonts.GetTexDataAsAlpha8(out pixels, out width, out height);
            
            int lastTexture;
            GL.GetIntegerv(GL.Enum.GL_TEXTURE_BINDING_2D, out lastTexture);

            // Create OpenGL texture
            int fonttxtureID;
            GL.GenTextures(1, out fonttxtureID);
            GL.BindTexture(GL.Enum.GL_TEXTURE_2D, fonttxtureID);
            GL.TexParameteri(GL.Enum.GL_TEXTURE_2D, GL.Enum.GL_TEXTURE_MIN_FILTER, (int) GL.Enum.GL_LINEAR);
            GL.TexParameteri(GL.Enum.GL_TEXTURE_2D, GL.Enum.GL_TEXTURE_MAG_FILTER, (int) GL.Enum.GL_LINEAR);
            GL.PixelStorei(GL.Enum.GL_UNPACK_ROW_LENGTH, 0);
            GL.TexImage2D(
                GL.Enum.GL_TEXTURE_2D,
                0,
                (int) GL.Enum.GL_ALPHA,
                width,
                height,
                0,
                GL.Enum.GL_ALPHA,
                GL.Enum.GL_UNSIGNED_BYTE,
                new IntPtr(pixels)
            );
            g_FontTexture = new IntPtr(fonttxtureID);
            // Store the texture identifier in the ImFontAtlas substructure.
            io.Fonts.SetTexID(g_FontTexture);
            ImGuiSDL2CSHelper.FontTextureID = g_FontTexture;
            io.Fonts.ClearTexData(); // Clears CPU side texture data.
            GL.BindTexture(GL.Enum.GL_TEXTURE_2D, lastTexture);
        }



        protected override void Dispose(bool disposing) {
            ImGuiIOPtr io = ImGui.GetIO();

            if (disposing) {
                // Dispose managed state (managed objects).
            }

            // Free unmanaged resources (unmanaged objects) and override a finalizer below.
            // Set large fields to null.
            if (g_FontTexture != IntPtr.Zero) {
                // Texture gets deleted with the context.
                // GL.DeleteTexture(g_FontTexture);
                if ( io.Fonts.TexID == g_FontTexture)
                    io.Fonts.TexID = IntPtr.Zero;
                g_FontTexture = IntPtr.Zero;
            }

            base.Dispose(disposing);
        }

        ~ImGuiSDL2CSWindow() {
            Dispose(false);
        }

    }
}
