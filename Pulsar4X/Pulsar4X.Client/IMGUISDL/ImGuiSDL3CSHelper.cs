using System;
using ImGuiNET;
using System.IO;
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

            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
            io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;

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
