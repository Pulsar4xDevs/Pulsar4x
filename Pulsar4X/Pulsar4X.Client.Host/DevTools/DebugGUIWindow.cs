using System;
using ImGuiNET;

namespace Pulsar4X.Client
{
    public class DebugGUIWindow : UniquePulsarGuiWindow<DebugGUIWindow>
    {

        private DebugGUIWindow()
        {

        }
        internal static DebugGUIWindow GetInstance()
        {
            if(_uiState.TryGetUniqueWindow<DebugGUIWindow>(out var window))
            {
                return window;
            }

            return _uiState.AddUniqueWindow(new DebugGUIWindow());
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

                    string datetimenow = DateTime.Now.ToString();


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
    }
}
