using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.SDL2UI
{
    public class  GalaxyWindow : PulsarGuiWindow
    {

        private GalaxyWindow()
        {

            //_flags = ImGuiWindowFlags.NoCollapse;
        }



        internal static GalaxyWindow GetInstance()
        {

            GalaxyWindow thisItem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(GalaxyWindow)))
            {
                thisItem = new GalaxyWindow();
            }
            thisItem = (GalaxyWindow)_uiState.LoadedWindows[typeof(GalaxyWindow)];

            return thisItem;

        }

        internal override void Display()
        {

            //ImGui.SetNextWindowSize();
            if (IsActive && Window.Begin("Galaxy Browser", ref IsActive, _flags))
            {

                uint iterations = 0;
                foreach (var starSystem in _uiState.StarSystemStates)
                {
                    ImGui.PushID(iterations.ToString());
                    if (ImGui.SmallButton(starSystem.Value.StarSystem.NameDB.DefaultName))
                    {
                        _uiState.SetActiveSystem(starSystem.Key);
                    }
                    ImGui.PopID();
                    iterations++;
                }
                Window.End();
            }
        }
    }
}
