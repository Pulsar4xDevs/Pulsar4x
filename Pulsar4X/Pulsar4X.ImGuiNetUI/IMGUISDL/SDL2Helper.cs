using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Pulsar4X.DataStructures;

namespace ImGuiSDL2CS {
    public static class SDL2Helper {

        private static bool _Initialized = false;
        public static bool Initialized => _Initialized;

        public static void Init() {
            if (_Initialized)
                return;
            _Initialized = true;

            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            SetGLAttributes();
        }

        public static void SetGLAttributes(
            int doubleBuffer = 1,
            int depthSize = 24,
            int stencilSize = 8
            //int majorVersion = 2,
            //int minorVersion = 2
        ) {
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, doubleBuffer);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, depthSize);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, stencilSize);
            //SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, majorVersion);
            //SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minorVersion);
        }
        
        
        public static IntPtr CreateSDLTexture(IntPtr rendererPtr, RawBmp rawImg, bool clean = false)
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


            SDL.SDL_DestroyTexture(rendererPtr);
            IntPtr sdlSurface = SDL.SDL_CreateRGBSurfaceFrom(pxls, w, h, d, s, rmask, gmask, bmask, amask);
            texture = SDL.SDL_CreateTextureFromSurface(rendererPtr, sdlSurface);
            SDL.SDL_FreeSurface(sdlSurface);


            int a;
            uint f;
            int qw;
            int qh;
            int q = SDL.SDL_QueryTexture(texture, out f, out a, out qw, out qh);
            if (q != 0)
            {
                ImGui.Text("QueryResult: " + q);
                ImGui.Text(SDL.SDL_GetError());
            }
            ImGui.Text("a: " + a +" f: " + f +" w: "+ qw +" h: "+ qh);
            
            return texture;
        }

    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Int4
    {
        public readonly int X, Y, Z, W;
    }
}
