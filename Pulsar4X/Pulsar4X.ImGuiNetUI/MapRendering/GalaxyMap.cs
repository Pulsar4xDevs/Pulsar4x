using System;
using System.Collections.Generic;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class SystemIcon 
    {
        public PointD _mapPosition;
        public float Scale = 1;
        public PointD ViewScreenPos;
        StarIcon _starIcon; 
        //public Shape[] DrawShapes;

        public SystemIcon(SystemState starSystemState)
        {
            _starIcon = new StarIcon(starSystemState.StarSystem.GetFirstEntityWithDataBlob<StarInfoDB>());
        }

        public virtual void OnFrameUpdate(Matrix matrix, Camera camera)
        {


            _starIcon.OnFrameUpdate(matrix, camera);
        }


        public virtual void Draw(IntPtr rendererPtr, Camera camera)
        {
            _starIcon.Draw(rendererPtr, camera);

        }
    }
    public class GalaxyMap : PulsarGuiWindow
    {
        Dictionary<Guid, SystemState> KnownSystems = new Dictionary<Guid, SystemState>();
        ImGuiSDL2CSWindow _window;
        Dictionary<Guid, SystemIcon> SystemIcons = new Dictionary<Guid, SystemIcon>();
        private GalaxyMap(Entity faction)
        {
            int i = 0;
            double startangle = Math.PI * 0.5;
            float angleIncrease = 0.01f;
            int startR = 32;
            int radInc = 5;
            foreach (var item in _state.StarSystemStates)
            {
                KnownSystems[item.Key] = item.Value;
                var icon = new SystemIcon(item.Value);
                var x = (startR + radInc * i) * Math.Sin(startangle - angleIncrease * i);
                var y = (startR + radInc * i) * Math.Cos(startangle - angleIncrease * i);
                icon._mapPosition.X = x;
                icon._mapPosition.Y = y;
                SystemIcons[item.Key] = icon;
                i++;
            }
        }

        internal static GalaxyMap GetInstance(ImGuiSDL2CSWindow window, Entity faction)
        {
            GalaxyMap instance;
            if (!_state.LoadedWindows.ContainsKey(typeof(GalaxyMap)))
            {
                instance = new GalaxyMap(faction);
            }
            else 
            instance = (GalaxyMap)_state.LoadedWindows[typeof(GalaxyMap)];
            instance._window = window;

            return instance;
        }

        internal override void Display()
        {
            if(IsActive)            
            { 






            }
        }

        internal void Draw(IntPtr renderPtr, Camera camera)
        {
            foreach (var icon in SystemIcons)
            {
                icon.Value.Draw(renderPtr, camera);
            }


        }
    }
}
