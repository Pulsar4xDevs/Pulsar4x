using System.Linq;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class  GalaxyWindow : UniquePulsarGuiWindow<GalaxyWindow>
    {

        private GalaxyWindow()
        {

            //_flags = ImGuiWindowFlags.NoCollapse;
        }



        internal static GalaxyWindow GetInstance()
        {
            if(_uiState.TryGetUniqueWindow<GalaxyWindow>(out var window))
            {
                return window;
            }

            return _uiState.AddUniqueWindow(new GalaxyWindow());
        }

        internal override void Display()
        {
            if (!IsActive) return;
            //ImGui.SetNextWindowSize();
            if (Window.Begin("Galaxy Browser", ref IsActive, _flags))
            {
                // The faction's known systems, kept current by the adapter's event stream.
                var galaxy = _uiState.GameClient?.Galaxy;
                if (galaxy != null)
                {
                    foreach (var system in galaxy.KnownSystems.OrderBy(s => s.Name))
                    {
                        ImGui.PushID(system.SystemId);
                        if (ImGui.SmallButton(system.Name))
                        {
                            _uiState.SetActiveSystem(system.SystemId);
                        }
                        ImGui.PopID();
                    }
                }
            }
            Window.End();
        }
    }
}
