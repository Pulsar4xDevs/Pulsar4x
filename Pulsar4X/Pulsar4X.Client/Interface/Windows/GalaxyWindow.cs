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
