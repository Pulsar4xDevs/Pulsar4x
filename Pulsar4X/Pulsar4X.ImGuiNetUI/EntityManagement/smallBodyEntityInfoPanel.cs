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
    //displays all asteroids and comets in current system
    class SmallBodyEntityInfoPanel : PulsarGuiWindow
    {


        private SmallBodyEntityInfoPanel()
        {
            //_flags = ImGuiWindowFlags.NoCollapse;
        }



        internal static SmallBodyEntityInfoPanel GetInstance()
        {

            SmallBodyEntityInfoPanel thisItem;
            if (!_state.LoadedWindows.ContainsKey(typeof(SmallBodyEntityInfoPanel)))
            {
                thisItem = new SmallBodyEntityInfoPanel();
            }
            else
            {
                thisItem = (SmallBodyEntityInfoPanel)_state.LoadedWindows[typeof(SmallBodyEntityInfoPanel)];
            }


            return thisItem;


        }

        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Small bodies:", ref IsActive, _flags))
            {
                if (_state.StarSystemStates.ContainsKey(_state.SelectedStarSysGuid))
                {
                    foreach (var smallBody in _state.StarSystemStates[_state.SelectedStarSysGuid].EntityStatesWithNames)
                    {
                        if (smallBody.Value.BodyType == UserOrbitSettings.OrbitBodyType.Asteroid || smallBody.Value.BodyType == UserOrbitSettings.OrbitBodyType.Comet)
                        {
                            if (ImGui.SmallButton(smallBody.Value.Name))
                            {
                                _state.EntityClicked(smallBody.Value.Entity.Guid, _state.SelectedStarSysGuid, MouseButtons.Primary);
                            }
                        }
                    }
                }
            }

        }
    }
}

