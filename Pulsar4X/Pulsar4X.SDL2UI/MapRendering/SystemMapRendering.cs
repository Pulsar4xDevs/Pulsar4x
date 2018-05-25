using System;
using Pulsar4X.ECSLib;
using ImGuiSDL2CS;
using SDL2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    internal class SystemMapRendering : PulsarGuiWindow
    {
        GlobalUIState _state;
        Camera _camera;
        internal IntPtr windowPtr;
        internal IntPtr surfacePtr; 
        internal IntPtr rendererPtr;
        ImGuiSDL2CSWindow _window;
        List<IDrawData> _drawableIcons = new List<IDrawData>();
        List<Vector4> _positions = new List<Vector4>();
        List<OrbitDB> _orbits = new List<OrbitDB>();
        SystemMap_DrawableVM _sysMap;
        Entity _faction;
        internal SystemMapRendering(ImGuiSDL2CSWindow window, GlobalUIState state)
        {
            _state = state;
            _camera = _state.Camera;
            _window = window;
            windowPtr = window.Handle;
            surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            rendererPtr = SDL.SDL_GetRenderer(windowPtr);
            _drawableIcons.Add(new TestDrawIconData(_camera));
        }

        internal void SetSystem(StarSystem starSys)
        {
            _sysMap = new SystemMap_DrawableVM();
            _sysMap.Initialise(null, starSys, _state.Faction);
            _faction = _state.Faction;


            //orbits.AddRange(sysMap.IconableEntitys

        }


        internal override void Display()
        {
            Matrix matrix = _camera.GetViewProjectionMatrix(); //new Matrix();
            //matrix.Translate(camera.x, camera.y);
            //matrix.Scale(camera.ZoomLevel);

            foreach (var icon in _drawableIcons)
            {
                icon.Draw(rendererPtr, matrix);
            }
        }
    }
}
