using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public class FleetWindow : PulsarGuiWindow
    {
        private FleetWindow() {}
        internal static FleetWindow GetInstance()
        {
            FleetWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(FleetWindow)))
            {
                thisitem = new FleetWindow();
            }
            thisitem = (FleetWindow)_uiState.LoadedWindows[typeof(FleetWindow)];

            return thisitem;
        }
        internal override void Display()
        {
            if(IsActive && ImGui.Begin("Fleet Management", ref IsActive, _flags))
            {

            }
        }
    }
}