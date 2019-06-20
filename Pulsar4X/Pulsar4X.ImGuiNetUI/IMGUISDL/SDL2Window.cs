using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImGuiSDL2CS {
    public class SDL2Window : IDisposable {

        protected IntPtr _Handle;
        public IntPtr Handle => _Handle;

        protected IntPtr _GLContext;
        public IntPtr GLContext => _GLContext;

        public string Title {
            get {
                return SDL.SDL_GetWindowTitle(_Handle);
            }
            set {
                SDL.SDL_SetWindowTitle(_Handle, value);
            }
        }

        public int X {
            get {
                int x, y;
                SDL.SDL_GetWindowPosition(_Handle, out x, out y);
                return x;
            }
            set {
                int x, y;
                SDL.SDL_GetWindowPosition(_Handle, out x, out y);
                SDL.SDL_SetWindowPosition(_Handle, value, y);
            }
        }

        public int Y {
            get {
                int x, y;
                SDL.SDL_GetWindowPosition(_Handle, out x, out y);
                return y;
            }
            set {
                int x, y;
                SDL.SDL_GetWindowPosition(_Handle, out x, out y);
                SDL.SDL_SetWindowPosition(_Handle, x, value);
            }
        }

        public int Width {
            get {
                int x, y;
                SDL.SDL_GetWindowSize(_Handle, out x, out y);
                return x;
            }
            set {
                int x, y;
                SDL.SDL_GetWindowSize(_Handle, out x, out y);
                SDL.SDL_SetWindowSize(_Handle, value, y);
            }
        }

        public int Height {
            get {
                int x, y;
                SDL.SDL_GetWindowSize(_Handle, out x, out y);
                return y;
            }
            set {
                int x, y;
                SDL.SDL_GetWindowSize(_Handle, out x, out y);
                SDL.SDL_SetWindowSize(_Handle, x, value);
            }
        }

        public SDL.SDL_WindowFlags Flags => (SDL.SDL_WindowFlags) SDL.SDL_GetWindowFlags(_Handle);

        public Action<SDL2Window> OnLoop;
        public Func<SDL2Window, SDL.SDL_Event, bool> OnEvent;
        public bool IsAlive = false;

        public SDL2Window(
            string title = "SDL2 Window",
            int x = SDL.SDL_WINDOWPOS_CENTERED, int y = SDL.SDL_WINDOWPOS_CENTERED,
            int width = 800, int height = 600,
            SDL.SDL_WindowFlags flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN
        ) 
        {
            Init(title, x, y, width, height, flags);
        }

        public void Init(
            string title = "SDL2 Window",
            int x = SDL.SDL_WINDOWPOS_CENTERED, int y = SDL.SDL_WINDOWPOS_CENTERED,
            int width = 800, int height = 600,
            SDL.SDL_WindowFlags flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN
        ) {
            SDL2Helper.Init();

            if (_Handle != IntPtr.Zero)
                throw new InvalidOperationException("SDL2Window already initialized, Dispose() first before reusing!");

            _Handle = SDL.SDL_CreateWindow(title, x, y, width, height, flags);

            if ((flags & SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL) == SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL) {
                _GLContext = SDL.SDL_GL_CreateContext(_Handle);
                SDL.SDL_GL_MakeCurrent(_Handle, _GLContext);
            }
        }

        public bool IsVisible => (Flags & SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN) == 0;
        public void Show() => SDL.SDL_ShowWindow(_Handle);
        public void Hide() => SDL.SDL_HideWindow(_Handle);

        public virtual void Run() {
            Show();
            IsAlive = true;
            do {
                while (IsAlive && OnLoop == null) {
                    PollEvents();
                }
                while (IsAlive && OnLoop != null) {
                    PollEvents();
                    OnLoop(this);
                }
            } while (IsAlive);
            IsAlive = false;
        }

        public virtual void PollEvents() {
            SDL.SDL_Event e;
            while (SDL.SDL_PollEvent(out e) != 0)
                if (OnEvent == null || OnEvent.Invoke(this, e))
                    HandleEvent(e);
        }

        public virtual void HandleEvent(SDL.SDL_Event e) {
            if (e.type == SDL.SDL_EventType.SDL_QUIT)
                IsAlive = false;
        }

        public virtual void Swap() => SDL.SDL_GL_SwapWindow(_Handle);

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                // Dispose managed state (managed objects).
            }

            // Free unmanaged resources (unmanaged objects) and override a finalizer below.
            // Set large fields to null.

            if (_Handle != IntPtr.Zero) {
                SDL.SDL_DestroyWindow(_Handle);
                _Handle = IntPtr.Zero;
            }
        }

        ~SDL2Window() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
