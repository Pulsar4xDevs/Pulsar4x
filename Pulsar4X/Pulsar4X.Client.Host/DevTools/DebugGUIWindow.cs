using System;
using ImGuiNET;

namespace Pulsar4X.Client
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
