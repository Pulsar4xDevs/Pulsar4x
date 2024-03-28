using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Engine;
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
            if (!_uiState.LoadedWindows.ContainsKey(typeof(SmallBodyEntityInfoPanel)))
            {
                thisItem = new SmallBodyEntityInfoPanel();
            }
            else
            {
                thisItem = (SmallBodyEntityInfoPanel)_uiState.LoadedWindows[typeof(SmallBodyEntityInfoPanel)];
            }


            return thisItem;


        }

        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Small bodies:", ref IsActive, _flags))
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
            }

        }
    }
}

