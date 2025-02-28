using Pulsar4X.Client.Rendering;
using SDL3;
using System;

namespace ImGuiSDL2CS
{
    public class SDL3Window : IDisposable
    {
        private const String defaultTitle = "SDL3 Window";

        protected IntPtr _Handle;
        public IntPtr Handle => _Handle;

        public IRenderer Renderer { get; set; }

        /// <summary>
        /// Window title
        /// </summary>
        public string Title {
            get {
                return SDL.GetWindowTitle(_Handle);
            }
            set {
                SDL.SetWindowTitle(_Handle, value);
            }
        }

        /// <summary>
        /// X coordinate of the window screen position
        /// </summary>
        public int X {
            get {
                SDL.GetWindowPosition(_Handle, out int x, out _);
                return x;
            }
            set {
                SDL.GetWindowPosition(_Handle, out _, out int y);
                SDL.SetWindowPosition(_Handle, value, y);
            }
        }

        /// <summary>
        /// Y coordinate of the window screen position
        /// </summary>
        public int Y {
            get {
                SDL.GetWindowPosition(_Handle, out _, out int y);
                return y;
            }
            set {
                SDL.GetWindowPosition(_Handle, out int x, out _);
                SDL.SetWindowPosition(_Handle, x, value);
            }
        }

        /// <summary>
        /// Width of the window
        /// </summary>
        public int Width {
            get {
                SDL.GetWindowSize(_Handle, out int x, out _);
                return x;
            }
            set {
                SDL.GetWindowSize(_Handle, out _, out int y);
                SDL.SetWindowSize(_Handle, value, y);
            }
        }

        /// <summary>
        /// Height of the window
        /// </summary>
        public int Height {
            get {
                SDL.GetWindowSize(_Handle, out _, out int y);
                return y;
            }
            set {
                SDL.GetWindowSize(_Handle, out int x, out _);
                SDL.SetWindowSize(_Handle, x, value);
            }
        }

        public SDL.WindowFlags Flags => (SDL.WindowFlags) SDL.GetWindowFlags(_Handle);

        public Action<SDL3Window>? OnLoop;
        public Func<SDL3Window, SDL.Event, bool>? OnEvent;
        public bool IsAlive = false;

        public SDL3Window(
            string title = defaultTitle,
            int x = 0, int y = 0,
            int width = 800, int height = 600,
            SDL.WindowFlags flags = SDL.WindowFlags.OpenGL | SDL.WindowFlags.Resizable | SDL.WindowFlags.Hidden
        )
        {
            Init(title, x, y, width, height, flags);
        }

        public void Init(
            string title = defaultTitle,
            int x = 0, int y = 0,
            int width = 800, int height = 600,
            SDL.WindowFlags flags = SDL.WindowFlags.OpenGL | SDL.WindowFlags.Resizable | SDL.WindowFlags.Hidden
        )
        {
            // init SDL
            SDL.Init(SDL.InitFlags.Video);

            // SDL3 no longer needs to init SDL_image
            // https://github.com/libsdl-org/SDL_image/blob/main/docs/README-migration.md
            //
            // // init SDL_image
            // var sdlImageFlags = SDL_image.IMG_InitFlags.IMG_INIT_PNG | SDL_image.IMG_InitFlags.IMG_INIT_JPG;
            // var result = SDL_image.IMG_Init(sdlImageFlags);

            // if ((result & (int)sdlImageFlags) != (int)sdlImageFlags)
            // {
            //     // Some format failed to initialize
            //     throw new Exception($"SDL2_image failed to initialize: {SDL.SDL_GetError()}");
            // }

            if (_Handle != IntPtr.Zero)
                throw new InvalidOperationException("SDL2Window already initialized, Dispose() first before reusing!");

            _Handle = SDL.CreateWindow(title, width, height, flags);

            Renderer = RendererFactory.CreateRenderer(RendererType.OpenGL);
            Renderer.SetAttributes();
            Renderer.Initialize(_Handle);
        }

        public bool IsVisible => (Flags & SDL.WindowFlags.Hidden) == 0;
        public void Show() => SDL.ShowWindow(_Handle);
        public void Hide() => SDL.HideWindow(_Handle);
        public virtual void Swap() => Renderer.EndFrame();

        public virtual void Run()
        {
            Show();

            IsAlive = true;
            while(IsAlive)
            {
                PollEvents();
                OnLoop?.Invoke(this);
            }
        }

        public virtual void PollEvents()
        {
            while (SDL.PollEvent(out var e))
                if (OnEvent == null || OnEvent.Invoke(this, e))
                    HandleEvent(e);
        }

        public virtual void HandleEvent(SDL.Event e)
        {
            if ((SDL.EventType)e.Type == SDL.EventType.Quit)
                IsAlive = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                // Dispose managed state (managed objects).
            }

            // Free unmanaged resources (unmanaged objects) and override a finalizer below.
            // Set large fields to null.
            Renderer.Dispose();

            // No longer needed in SDL3
            //SDL_image.IMG_Quit();

            if (_Handle != IntPtr.Zero) {
                SDL.DestroyWindow(_Handle);
                _Handle = IntPtr.Zero;
            }
        }

        ~SDL3Window()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Renderer.Dispose();
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
