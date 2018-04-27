using System;
using SDL2;
namespace Pulsar4X.SDL2UI
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var window = SDL.SDL_CreateWindow("Pulsar4X Window", 100, 100, 640, 480, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

            var renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
        }
    }
}
