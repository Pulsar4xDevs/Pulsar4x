using System;
using System.Collections.Generic;
using ImGuiSDL2CS;

namespace Pulsar4X.SDL2UI
{
    public class GalacticMapRender
    {
        GlobalUIState _state;
        List<SystemState> SystemStates = new List<SystemState>();
        internal Dictionary<Guid,SystemMapRendering> RenderedMaps = new Dictionary<Guid, SystemMapRendering>();
        ImGuiSDL2CSWindow _window;
        internal SystemMapRendering PrimarySysMap { get; set; }

        public GalacticMapRender(ImGuiSDL2CSWindow window, GlobalUIState state)
        {
            _state = state;
            _window = window;

        }

        internal void SetFaction()
        {
            int i = 0;
            double startangle = Math.PI * 0.5;
            float angleIncrease = 0.01f;
            int startR = 32;
            int radInc = 5;
            foreach (var item in _state.StarSystemStates)
            {

                SystemMapRendering map = new SystemMapRendering(_window, _state);
                map.SetSystem(item.Value.StarSystem);
                RenderedMaps[item.Key] = map;
                var x = (startR + radInc * i) * Math.Sin(startangle - angleIncrease * i);
                var y = (startR + radInc * i) * Math.Cos(startangle - angleIncrease * i);
                map.GalacticMapPosition.X = x;
                map.GalacticMapPosition.Y = y;
                i++;
            }
        }



        internal void DrawNameIcons()
        {
            foreach (var kvp in RenderedMaps)
            {
                var sysid = kvp.Key;
                var sysmap = kvp.Value;
                sysmap.DrawNameIcons();
            }
        }

        internal void Draw()
        {
            foreach (var kvp in RenderedMaps)
            {
                var sysid = kvp.Key;
                var sysmap = kvp.Value;
                sysmap.Draw();
            }
        }
    }
}
