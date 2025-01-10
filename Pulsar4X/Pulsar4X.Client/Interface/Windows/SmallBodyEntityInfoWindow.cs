using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.SDL2UI
{
    //displays all asteroids and comets in current system
    class SmallBodyEntityInfoWindow : PulsarGuiWindow
    {


        private SmallBodyEntityInfoWindow()
        {
            //_flags = ImGuiWindowFlags.NoCollapse;
        }



        internal static SmallBodyEntityInfoWindow GetInstance()
        {

            SmallBodyEntityInfoWindow thisItem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(SmallBodyEntityInfoWindow)))
            {
                thisItem = new SmallBodyEntityInfoWindow();
            }
            else
            {
                thisItem = (SmallBodyEntityInfoWindow)_uiState.LoadedWindows[typeof(SmallBodyEntityInfoWindow)];
            }


            return thisItem;


        }

        internal override void Display()
        {
            if (IsActive && Window.Begin("Small bodies:", ref IsActive, _flags))
            {
                if (_uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSysGuid))
                {
                    foreach (var smallBody in _uiState.StarSystemStates[_uiState.SelectedStarSysGuid].EntityStatesWithNames)
                    {
                        if (smallBody.Value.IsSmallBody())
                        {
                            if (ImGui.SmallButton(smallBody.Value.Name))
                            {
                                _uiState.EntityClicked(smallBody.Value.Entity.Id, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                            }
                        }
                    }
                }
                Window.End();
            }

        }
    }
}

