using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    class  GalaxyWindow: PulsarGuiWindow
    {

        private GalaxyWindow()
        {
            //_flags = ImGuiWindowFlags.NoCollapse;
        }



        internal static GalaxyWindow GetInstance()
        {

            GalaxyWindow thisItem;
            if (!_state.LoadedWindows.ContainsKey(typeof(GalaxyWindow)))
            {
                thisItem = new GalaxyWindow();
            }
            else
            {
                thisItem = (GalaxyWindow)_state.LoadedWindows[typeof(GalaxyWindow)];
            }


            return thisItem;


        }

        internal override void Display()
        {

            //ImGui.SetNextWindowSize();
            if (IsActive && ImGui.Begin("Galaxy Browser", ref IsActive, _flags))
            {
                //if (ImGui.Begin("LOL",_flags))
                //{
           
                uint iterations = 0;
                foreach (var starSystem in _state.StarSystemStates)
                {
                    ImGui.PushID(iterations.ToString());
                    if (ImGui.SmallButton(starSystem.Value.StarSystem.NameDB.DefaultName))
                    {
                        _state.SetActiveSystem(starSystem.Key);
                    }
                    ImGui.PopID();
                    iterations++;
                }
                ImGui.End();
            //}
            }
        }
    }
}
