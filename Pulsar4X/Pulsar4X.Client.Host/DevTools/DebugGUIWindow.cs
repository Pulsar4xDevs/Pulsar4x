using System;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;

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
                using (var w = Window.Scope("Debug GUI Window", ref IsActive))
                {
                    if (w.Visible)
                    {
                        ImGui.Text("GitHash: " + AssemblyInfo.GetGitHash());
                        ImGui.Text("Window Height: " + ImGui.GetContentRegionAvail().Y);
                        ImGui.Text("Window Width: " + ImGui.GetContentRegionAvail().X);

                        string datetimenow = DateTime.Now.ToString();
                    }
                }
            }
        }
    }
}
