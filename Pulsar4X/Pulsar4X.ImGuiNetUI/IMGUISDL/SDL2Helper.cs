using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            int stencilSize = 8,
            int majorVersion = 2,
            int minorVersion = 2
        ) {
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, doubleBuffer);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, depthSize);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, stencilSize);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, majorVersion);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minorVersion);
        }

    }
}
