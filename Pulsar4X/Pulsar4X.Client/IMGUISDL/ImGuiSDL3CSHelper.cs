using SDL3;
using System;
using ImGuiNET;
using System.IO;
using Pulsar4X.DataStructures;
using Vector2 = System.Numerics.Vector2;
using System.Runtime.InteropServices;

namespace ImGuiSDL2CS
{
    public static class ImGuiSDL3CSHelper
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

            io.KeyMap[(int)ImGuiKey.Tab] = (int) SDL.Keycode.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int) SDL.Scancode.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int) SDL.Scancode.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int) SDL.Scancode.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int) SDL.Scancode.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int) SDL.Scancode.Pageup;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int) SDL.Scancode.Pagedown;
            io.KeyMap[(int)ImGuiKey.Home] = (int) SDL.Scancode.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int) SDL.Scancode.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int) SDL.Keycode.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int) SDL.Keycode.Backspace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int) SDL.Keycode.Return;
            io.KeyMap[(int)ImGuiKey.Escape] = (int) SDL.Keycode.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int) SDL.Keycode.A;
            io.KeyMap[(int)ImGuiKey.C] = (int) SDL.Keycode.C;
            io.KeyMap[(int)ImGuiKey.V] = (int) SDL.Keycode.V;
            io.KeyMap[(int)ImGuiKey.X] = (int) SDL.Keycode.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int) SDL.Keycode.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int) SDL.Keycode.Z;
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

        public static void NewFrame(Vector2 size, Vector2 scale, Vector2 mousePosition, SDL.MouseButtonFlags mouseMask, ref float mouseWheel, bool[] mousePressed, ref double g_Time)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = size;
            io.DisplayFramebufferScale = scale;

            double currentTime = SDL.GetTicks() / 1000D;
            io.DeltaTime = g_Time > 0D ? (float) (currentTime - g_Time) : (1f/60f);
            g_Time = currentTime;

            io.MousePos = mousePosition;

            io.MouseDown[0] = mousePressed[0] || (mouseMask & SDL.MouseButtonFlags.Left) != 0;
            io.MouseDown[1] = mousePressed[1] || (mouseMask & SDL.MouseButtonFlags.Right) != 0;
            io.MouseDown[2] = mousePressed[2] || (mouseMask & SDL.MouseButtonFlags.Middle) != 0;
            mousePressed[0] = mousePressed[1] = mousePressed[2] = false;

            io.MouseWheel = mouseWheel;
            mouseWheel = 0f;

            if(io.MouseDrawCursor)
                SDL.ShowCursor();
            else
                SDL.HideCursor();

            ImGui.NewFrame();
        }

        public static bool HandleEvent(SDL.Event e, ref float mouseWheel, bool[] mousePressed)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            switch (e.Type)
            {
                case (uint)SDL.EventType.MouseWheel:
                    if (e.Wheel.Y > 0)
                        mouseWheel = 1;
                    if (e.Wheel.X < 0)
                        mouseWheel = -1;
                    return true;
                case (uint)SDL.EventType.MouseButtonDown:
                    if (mousePressed == null)
                        return true;
                    if (e.Button.Button == SDL.ButtonLeft && mousePressed.Length > 0)
                        mousePressed[0] = true;
                    if (e.Button.Button == SDL.ButtonRight && mousePressed.Length > 1)
                        mousePressed[1] = true;
                    if (e.Button.Button == SDL.ButtonMiddle && mousePressed.Length > 2)
                        mousePressed[2] = true;
                    return true;
                case (uint)SDL.EventType.TextInput:
                    ImGui.GetIO().AddInputCharacter((uint)e.Text.Text);
                    // unsafe
                    // {
                    //     // THIS IS THE ONLY UNSAFE THING LEFT!

                    //     //ImGui.GetIO().AddInputCharactersUTF8(e.text.ToString());

                    //     int i = 0;
                    //     while (e.text.text[i] != 0)
                    //     {
                    //         ImGui.GetIO().AddInputCharacter(e.text.text[i]);
                    //         i += 1;
                    //     }
                    // }
                    return true;
                case (uint)SDL.EventType.KeyDown:
                case (uint)SDL.EventType.KeyUp:
                    SDL.Keycode key = e.Key.Key & ~SDL.Keycode.ScanCodeMask;
                    io.KeysDown[(int)key] = e.Type == (uint)SDL.EventType.KeyDown;
                    SDL.Keymod keyModState = SDL.GetModState();

                    io.KeyShift = (keyModState & SDL.Keymod.Shift) != 0;
                    io.KeyCtrl = (keyModState & SDL.Keymod.Ctrl) != 0;
                    io.KeyAlt = (keyModState & SDL.Keymod.Alt) != 0;
                    io.KeySuper = (keyModState & SDL.Keymod.GUI) != 0;
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


        // public static IntPtr CreateSDLTexture(IntPtr rendererPtr, RawBmp rawImg)
        // {
        //     IntPtr texture;
        //     int h = rawImg.Height;
        //     int w = rawImg.Width;
        //     int d = rawImg.Depth * 8;
        //     int s = rawImg.Stride;
        //     IntPtr pxls;
        //     unsafe
        //     {
        //         fixed (byte* ptr = rawImg.ByteArray)
        //         {
        //             pxls = new IntPtr(ptr);
        //         }
        //     }

        //     uint rmask = 0xff000000;
        //     uint gmask = 0x00ff0000;
        //     uint bmask = 0x0000ff00;
        //     uint amask = 0x000000ff;

        //     IntPtr sdlSurface = SDL.SDL_CreateRGBSurfaceFrom(pxls, w, h, d, s, rmask, gmask, bmask, amask);
        //     texture = SDL.SDL_CreateTextureFromSurface(rendererPtr, sdlSurface);

        //     int q = SDL.SDL_QueryTexture(texture, out uint f, out int a, out int qw, out int qh);
        //     if (q != 0)
        //     {
        //         ImGui.Text("QueryResult: " + q);
        //         ImGui.Text(SDL.SDL_GetError());
        //     }
        //     ImGui.Text("a: " + a +" f: " + f +" w: "+ qw +" h: "+ qh);
        //     return texture;
        // }
    }
}
