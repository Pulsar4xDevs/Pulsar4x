using System;
using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public class SettingsWindow : PulsarGuiWindow
    {
        GlobalUIState _state;

        ImGuiTreeNodeFlags _xpanderFlags = ImGuiTreeNodeFlags.CollapsingHeader;
        UserOrbitSettings _userOrbitSettings;
        int _arcSegments;
        ImVec3 _colour;
        int _maxAlpha;
        int _minAlpha;
        public SettingsWindow(GlobalUIState state)
        {
            _state = state;
            _userOrbitSettings = state.UserOrbitSettings;
            _arcSegments = _userOrbitSettings.NumberOfArcSegments;
            _maxAlpha = _userOrbitSettings.MaxAlpha;
            _minAlpha = _userOrbitSettings.MinAlpha;
            _colour = Helpers.Color(_userOrbitSettings.Red, _userOrbitSettings.Grn, _userOrbitSettings.Blu);
        }

        internal override void Display()
        {
            ImVec2 size = new ImVec2(200, 100);
            ImVec2 pos = new ImVec2(0, 0);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

            ImGui.Begin("Settings", ref IsActive, _flags);


            if (ImGui.CollapsingHeader("Map Settings", _xpanderFlags))
            {
                //TODO: make this a knob/dial? need to create a custom control: https://github.com/ocornut/imgui/issues/942
                if (ImGui.SliderAngle("Sweep Angle", ref _userOrbitSettings.EllipseSweepRadians, 1f, 360f))
                    _state.MapRendering.UpdateUserOrbitSettings();

                if (ImGui.SliderInt("Number Of Segments", ref _arcSegments, 1, 255, _userOrbitSettings.NumberOfArcSegments.ToString()))
                {
                    _userOrbitSettings.NumberOfArcSegments = (byte)_arcSegments;
                    _state.MapRendering.UpdateUserOrbitSettings();
                }

                if (ImGui.ColorEdit3("Orbit Ring Colour", ref _colour, false))
                {
                    _userOrbitSettings.Red = Helpers.Color(_colour.x);
                    _userOrbitSettings.Grn = Helpers.Color(_colour.y);
                    _userOrbitSettings.Blu = Helpers.Color(_colour.z);
                }
                if (ImGui.SliderInt("Max Alpha", ref _maxAlpha, _minAlpha, 255, ""))
                {
                    _userOrbitSettings.MaxAlpha = (byte)_maxAlpha;
                    _state.MapRendering.UpdateUserOrbitSettings();
                }

                if (ImGui.SliderInt("Min Alpha", ref _minAlpha, 0, _maxAlpha, ""))
                {
                    _userOrbitSettings.MinAlpha = (byte)_minAlpha;
                    _state.MapRendering.UpdateUserOrbitSettings();
                }
            }


            ImGui.End();
        }
    }
}
