using ImGuiNET;
using SDL3;
using System;
using System.Diagnostics;
using System.Numerics;

namespace Pulsar4X.Client
{
    public partial class SDL3Window : IDisposable
    {
        private const string _defaultTitle = "SDL3 Window";
        public const string OrgName = "Pulsar4X";
        public const string AppName = "Pulsar4X";
        public readonly nint Window;
        public readonly nint Renderer;
        public readonly nint ImGuiContext;
        public readonly ImGuiSDL3 PlatformBackend;
        public readonly ImGuiSDL3Renderer ImGuiRenderer;

        private SDL.Rect _screenClipRect;

        /// <summary>
        /// Window title
        /// </summary>
        public string Title {
            get {
                return SDL.GetWindowTitle(Window);
            }
            set {
                SDL.SetWindowTitle(Window, value);
            }
        }

        /// <summary>
        /// X coordinate of the window screen position
        /// </summary>
        public int X {
            get {
                SDL.GetWindowPosition(Window, out int x, out _);
                return x;
            }
            set {
                SDL.GetWindowPosition(Window, out _, out int y);
                SDL.SetWindowPosition(Window, value, y);
            }
        }

        /// <summary>
        /// Y coordinate of the window screen position
        /// </summary>
        public int Y {
            get {
                SDL.GetWindowPosition(Window, out _, out int y);
                return y;
            }
            set {
                SDL.GetWindowPosition(Window, out int x, out _);
                SDL.SetWindowPosition(Window, x, value);
            }
        }

        /// <summary>
        /// Width of the window
        /// </summary>
        public int Width {
            get {
                SDL.GetWindowSize(Window, out int x, out _);
                return x;
            }
            set {
                SDL.GetWindowSize(Window, out _, out int y);
                SDL.SetWindowSize(Window, value, y);
            }
        }

        /// <summary>
        /// Height of the window
        /// </summary>
        public int Height {
            get {
                SDL.GetWindowSize(Window, out _, out int y);
                return y;
            }
            set {
                SDL.GetWindowSize(Window, out int x, out _);
                SDL.SetWindowSize(Window, x, value);
            }
        }

        public System.Drawing.Size Size
        {
            get
            {
                SDL.GetWindowSize(Window, out int w, out int h);
                return new(w, h);
            }
            set
            {
                SDL.SetWindowSize(Window, value.Width, value.Height);
            }
        }

        public SDL.WindowFlags Flags => (SDL.WindowFlags) SDL.GetWindowFlags(Window);
        public bool IsAlive { get; set; } = false;
        
        protected bool _ctrlPressed = false;
        public bool IsCtrlPressed => _ctrlPressed;

        public SDL3Window(
            string title = _defaultTitle,
            int width = 1280, int height = 720,
            SDL.WindowFlags flags = SDL.WindowFlags.Resizable | SDL.WindowFlags.Hidden
        )
        {
            // Initialize SDL
            if (!SDL.Init(SDL.InitFlags.Video))
                throw new Exception($"SDL_Init failed: {SDL.GetError()}");
            if (!SDL3.TTF.Init())
                throw new Exception("SDL TTF init failed");

            // Create window & renderer
            if(!SDL.CreateWindowAndRenderer(title, width, height, flags, out Window, out Renderer))
                throw new Exception($"SDL_CreateWindowAndRenderer failed: {SDL.GetError()}");

            // Create ImGui context
            ImGuiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(ImGuiContext);

            // Init platform and imgui renderer
            PlatformBackend = new (Window, Renderer);
            ImGuiRenderer = new (Renderer);

            // Setup screen clip rect
            SetupScreenClipRect();

            // Set the background color
            SetRenderDrawColor(0, 0, 28, 255);
        }

        public bool IsVisible => (Flags & SDL.WindowFlags.Hidden) == 0;
        public void Show() => SDL.ShowWindow(Window);
        public void Hide() => SDL.HideWindow(Window);
        public void Maximize() => SDL.MaximizeWindow(Window);
        public (float, float, SDL.MouseButtonFlags) GetMouseState()
        {
            var flags = SDL.GetMouseState(out float x, out float y);
            return (x, y, flags);
        }

        private readonly Stopwatch timer = Stopwatch.StartNew();
        private TimeSpan time = TimeSpan.Zero;

        public virtual void Run()
        {
            IsAlive = true;
            Show();
            ImGui.GetIO().ConfigErrorRecovery = true;
            ImGui.GetIO().ConfigErrorRecoveryEnableAssert = false;
            ImGui.GetIO().ConfigErrorRecoveryEnableTooltip = true;
            ImGui.GetIO().ConfigErrorRecoveryEnableDebugLog = true;
            
            while(IsAlive)
            {
                ImGui.GetIO().DeltaTime = (float)(timer.Elapsed - time).TotalSeconds;
                time = timer.Elapsed;

                PollEvents();

                // Is alive is set to false on poll events if the window closes or the user exits
                // so we should force exit here
                if(!IsAlive)
                    return;

                Update();
                BeginFrame();
                Render();
                EndFrame();
                PostFrameUpdate();
            }

            Exit();
        }

        public virtual void PollEvents()
        {
            if(ImGui.GetIO().WantTextInput && !SDL.TextInputActive(Window))
                SDL.StartTextInput(Window);
            else if(!ImGui.GetIO().WantTextInput && SDL.TextInputActive(Window))
                SDL.StopTextInput(Window);

            // Update Ctrl key state
            var keyMods = SDL.GetModState();
            _ctrlPressed = keyMods.HasFlag(SDL.Keymod.LCtrl) || keyMods.HasFlag(SDL.Keymod.RCtrl);

            while(SDL.PollEvent(out var ev))
            {
                PlatformBackend.ProcessEvent(ev);

                switch((SDL.EventType)ev.Type)
                {
                    case SDL.EventType.WindowCloseRequested:
                    case SDL.EventType.Quit:
                        IsAlive = false;
                        break;
                    case SDL.EventType.WindowResized:
                        SetupScreenClipRect();
                        break;
                }

                HandleEvent(ev);
            }
        }

        public virtual void HandleEvent(SDL.Event ev) {}

        public virtual void Update() {}

        public virtual void BeginFrame()
        {
            // Setup the new frame
            PlatformBackend.NewFrame();
            ImGuiRenderer.NewFrame();
            ImGui.NewFrame();

            // Clear the buffer
            SDL.RenderClear(Renderer);

            // Reset the clip rect to the screen size
            SDL.SetRenderClipRect(Renderer, _screenClipRect);
        }
        public virtual void Render() {}
        public virtual void EndFrame()
        {
            // Finish ImGui frame
            ImGui.EndFrame();

            // Render ImGui
            ImGui.Render();
            ImGuiRenderer.RenderDrawData(ImGui.GetDrawData());

            // Swap the buffer to screen
            SDL.RenderPresent(Renderer);
        }

        public virtual void PostFrameUpdate() {}

        public virtual void Exit() {}

        public void SetRenderDrawColor(byte r, byte g, byte b, byte a)
        {
            SDL.SetRenderDrawColor(Renderer, r, g, b, a);
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
            IsAlive = false;
            ImGuiRenderer.Dispose();
            ImGui.DestroyContext();
            SDL.DestroyWindow(Window);
            SDL.DestroyRenderer(Renderer);
            SDL3.TTF.Quit();
            SDL.Quit();
        }

        ~SDL3Window()
        {
            Dispose();
        }

        private void SetupScreenClipRect()
        {
            SDL.GetWindowSize(Window, out int w, out int h);
            _screenClipRect = new()
            {
                X = 0,
                Y = 0,
                W = w,
                H = h
            };
        }

        public static string? GetAppDataPath()
        {
            return SDL.GetPrefPath(OrgName, AppName);
        }

        public RenderState GetRenderState()
        {
            SDL.GetRenderDrawColor(Renderer, out byte r, out byte g, out byte b, out byte a);
            SDL.GetRenderDrawBlendMode(Renderer, out SDL.BlendMode blendMode);

            return new RenderState()
            {
                BlendMode = blendMode,
                Red = r,
                Green = g,
                Blue = b,
                Alpha = a,
            };
        }

        public void SetRenderState(RenderState renderState)
        {
            SDL.SetRenderDrawBlendMode(Renderer, renderState.BlendMode);
            SDL.SetRenderDrawColor(Renderer, renderState.Red, renderState.Green, renderState.Blue,  renderState.Alpha);
        }

        public void SetBlendMode(SDL.BlendMode mode) => SDL.SetRenderDrawBlendMode(Renderer, mode);
    }
}
