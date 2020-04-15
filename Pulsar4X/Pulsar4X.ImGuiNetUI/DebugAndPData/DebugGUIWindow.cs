using System;
using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Vector2 = System.Numerics.Vector2;

namespace Pulsar4X.SDL2UI
{
    public class DebugGUIWindow : PulsarGuiWindow
    {

        private DebugGUIWindow() 
        {
              
        }
        internal static DebugGUIWindow GetInstance()
        {
            DebugGUIWindow instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(DebugGUIWindow)))
                instance = new DebugGUIWindow();
            else
            {
                instance = (DebugGUIWindow)_uiState.LoadedWindows[typeof(DebugGUIWindow)];

            }

            return instance;
        }




        internal override void Display()
        {

            if (IsActive)
            {
                if (ImGui.Begin("Debug GUI Window", ref IsActive))
                {
                    ImGui.Text("GitHash: " + AssemblyInfo.GetGitHash());
                    ImGui.Text("Window Height: " + ImGui.GetContentRegionAvail().Y);
                    ImGui.Text("Window Width: " + ImGui.GetContentRegionAvail().X);

                }

                ImGui.End();
            }
        }


        public override void OnGameTickChange(DateTime newDate)
        {
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
        }

        public override void OnSelectedSystemChange(StarSystem newStarSys)
        {
            throw new NotImplementedException();
        }
    }
}
