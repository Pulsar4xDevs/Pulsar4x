using SDL2;
using System;
using ImGuiNET;
using System.IO;
using Pulsar4X.DataStructures;
using Vector2 = System.Numerics.Vector2;
using System.Runtime.InteropServices;

namespace ImGuiSDL2CS
{
    public static class ImGuiSDL2CSHelper
    {
        private static bool _Initialized = false;
        public static bool Initialized => _Initialized;
        public static void Init()
        {
            if (_Initialized)
                return;
            _Initialized = true;

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            ImGuiIOPtr io = ImGui.GetIO();

            io.AddKeyEvent(ImGuiKey.Tab, true);// =  SDL.SDL_Keycode.SDLK_TAB;
            
            io.KeyMap[(int)ImGuiKey.Tab] = (int) SDL.SDL_Keycode.SDLK_TAB;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int) SDL.SDL_Scancode.SDL_SCANCODE_LEFT;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int) SDL.SDL_Scancode.SDL_SCANCODE_RIGHT;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int) SDL.SDL_Scancode.SDL_SCANCODE_UP;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int) SDL.SDL_Scancode.SDL_SCANCODE_DOWN;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int) SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int) SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN;
            io.KeyMap[(int)ImGuiKey.Home] = (int) SDL.SDL_Scancode.SDL_SCANCODE_HOME;
            io.KeyMap[(int)ImGuiKey.End] = (int) SDL.SDL_Scancode.SDL_SCANCODE_END;
            io.KeyMap[(int)ImGuiKey.Delete] = (int) SDL.SDL_Keycode.SDLK_DELETE;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int) SDL.SDL_Keycode.SDLK_BACKSPACE;
            io.KeyMap[(int)ImGuiKey.Enter] = (int) SDL.SDL_Keycode.SDLK_RETURN;
            io.KeyMap[(int)ImGuiKey.Escape] = (int) SDL.SDL_Keycode.SDLK_ESCAPE;
            io.KeyMap[(int)ImGuiKey.A] = (int) SDL.SDL_Keycode.SDLK_a;
            io.KeyMap[(int)ImGuiKey.C] = (int) SDL.SDL_Keycode.SDLK_c;
            io.KeyMap[(int)ImGuiKey.V] = (int) SDL.SDL_Keycode.SDLK_v;
            io.KeyMap[(int)ImGuiKey.X] = (int) SDL.SDL_Keycode.SDLK_x;
            io.KeyMap[(int)ImGuiKey.Y] = (int) SDL.SDL_Keycode.SDLK_y;
            io.KeyMap[(int)ImGuiKey.Z] = (int) SDL.SDL_Keycode.SDLK_z;
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            //io.GetClipboardTextFn((userData) => SDL.SDL_GetClipboardText());

            //io.SetSetClipboardTextFn((userData, text) => SDL.SDL_SetClipboardText(text));

            unsafe
            {
                string rf = "Resources";

                ImFontAtlasPtr fontAtlas = ImGui.GetIO().Fonts;
                ImFontConfigPtr config = new (ImGuiNative.ImFontConfig_ImFontConfig());
                ImFontGlyphRangesBuilderPtr builder = new (ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());

                builder.AddText("ΩωΝνΔδθΘϖ"); //Omega, Nu, Delta, Theta (UPPER and lower cases)
                //builder.AddRanges(fontAtlas.GetGlyphRangesDefault());
                builder.BuildRanges(out ImVector ranges);

                config.PixelSnapH = true;
                fontAtlas.AddFontFromFileTTF(Path.Combine(rf, "ProggyClean.ttf"), 13, config);
                config.MergeMode = true;
                fontAtlas.AddFontFromFileTTF(Path.Combine(rf, "DejaVuSans.ttf"), 13, config, ranges.Data);
            }

            if (io.Fonts.Fonts.Size == 0)
                io.Fonts.AddFontDefault();
        }

        public static void NewFrame(Vector2 size, Vector2 scale, Vector2 mousePosition, uint mouseMask, ref float mouseWheel, bool[] mousePressed, ref double g_Time)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = size;
            io.DisplayFramebufferScale = scale;

            double currentTime = SDL.SDL_GetTicks() / 1000D;
            io.DeltaTime = g_Time > 0D ? (float) (currentTime - g_Time) : (1f/60f);
            g_Time = currentTime;

            io.MousePos = mousePosition;

            io.MouseDown[0] = mousePressed[0] || (mouseMask & SDL.SDL_BUTTON(SDL.SDL_BUTTON_LEFT)) != 0;
            io.MouseDown[1] = mousePressed[1] || (mouseMask & SDL.SDL_BUTTON(SDL.SDL_BUTTON_RIGHT)) != 0;
            io.MouseDown[2] = mousePressed[2] || (mouseMask & SDL.SDL_BUTTON(SDL.SDL_BUTTON_MIDDLE)) != 0;
            mousePressed[0] = mousePressed[1] = mousePressed[2] = false;

            io.MouseWheel = mouseWheel;
            mouseWheel = 0f;

            SDL.SDL_ShowCursor(io.MouseDrawCursor ? 0 : 1);

            ImGui.NewFrame();
        }

        public static bool HandleEvent(SDL.SDL_Event e, ref float mouseWheel, bool[] mousePressed)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    if (e.wheel.y > 0)
                        mouseWheel = 1;
                    if (e.wheel.y < 0)
                        mouseWheel = -1;
                    return true;
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    if (mousePressed == null)
                        return true;
                    if (e.button.button == SDL.SDL_BUTTON_LEFT && mousePressed.Length > 0)
                        mousePressed[0] = true;
                    if (e.button.button == SDL.SDL_BUTTON_RIGHT && mousePressed.Length > 1)
                        mousePressed[1] = true;
                    if (e.button.button == SDL.SDL_BUTTON_MIDDLE && mousePressed.Length > 2)
                        mousePressed[2] = true;
                    return true;
                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    unsafe
                    {
                        // THIS IS THE ONLY UNSAFE THING LEFT!

                        //ImGui.GetIO().AddInputCharactersUTF8(e.text.ToString());
                        int i = 0;
                        while (e.text.text[i] != 0)
                        {
                            ImGui.GetIO().AddInputCharacter(e.text.text[i]);
                            i += 1;
                        }
                    }
                    return true;
                case SDL.SDL_EventType.SDL_KEYDOWN:
                case SDL.SDL_EventType.SDL_KEYUP:
                    int key = (int) e.key.keysym.sym & ~SDL.SDLK_SCANCODE_MASK;
                    io.KeysDown[key] = e.type == SDL.SDL_EventType.SDL_KEYDOWN;
                    SDL.SDL_Keymod keyModState = SDL.SDL_GetModState();

                    io.KeyShift = (keyModState & SDL.SDL_Keymod.KMOD_SHIFT) != 0;
                    io.KeyCtrl = (keyModState & SDL.SDL_Keymod.KMOD_CTRL) != 0;
                    io.KeyAlt = (keyModState & SDL.SDL_Keymod.KMOD_ALT) != 0;
                    io.KeySuper = (keyModState & SDL.SDL_Keymod.KMOD_GUI) != 0;
                    return true;
            }

            return true;
        }

        public static byte[] BytesFromString(string str, int sizeMax = 128)
        {
            byte[] dstArray = new byte[sizeMax];
            byte[] srsArray = System.Text.Encoding.UTF8.GetBytes(str);
            int srsSize = Math.Min(srsArray.Length, sizeMax);
            System.Buffer.BlockCopy(srsArray, 0, dstArray, 0, srsSize);
            return dstArray;
        }

        public static string StringFromBytes(byte[] byteArray)
        {
            // Get the string and trim off any trailing null characters
            string result = System.Text.Encoding.UTF8.GetString(byteArray);
            int nullIndex = result.IndexOf('\0');
            if(nullIndex >= 0)
            {
                result = result.Substring(0, nullIndex);
            }
            return result;
        }

        public static (IntPtr, uint) GuidToIntPtr(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            return (ptr, (uint)bytes.Length);
        }


        public static IntPtr CreateSDLTexture(IntPtr rendererPtr, RawBmp rawImg)
        {
            IntPtr texture;
            int h = rawImg.Height;
            int w = rawImg.Width;
            int d = rawImg.Depth * 8;
            int s = rawImg.Stride;
            IntPtr pxls;
            unsafe
            {
                fixed (byte* ptr = rawImg.ByteArray)
                {
                    pxls = new IntPtr(ptr);
                }
            }

            uint rmask = 0xff000000;
            uint gmask = 0x00ff0000;
            uint bmask = 0x0000ff00;
            uint amask = 0x000000ff;

            IntPtr sdlSurface = SDL.SDL_CreateRGBSurfaceFrom(pxls, w, h, d, s, rmask, gmask, bmask, amask);
            texture = SDL.SDL_CreateTextureFromSurface(rendererPtr, sdlSurface);

            int q = SDL.SDL_QueryTexture(texture, out uint f, out int a, out int qw, out int qh);
            if (q != 0)
            {
                ImGui.Text("QueryResult: " + q);
                ImGui.Text(SDL.SDL_GetError());
            }
            ImGui.Text("a: " + a +" f: " + f +" w: "+ qw +" h: "+ qh);
            return texture;
        }
    }
}
