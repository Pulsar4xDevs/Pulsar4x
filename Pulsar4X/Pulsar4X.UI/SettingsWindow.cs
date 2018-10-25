using System;
using ImGuiNET;

namespace Pulsar4X.VeldridUI
{
    public class SettingsWindow : PulsarGuiWindow
    {
        ImGuiTreeNodeFlags _xpanderFlags = ImGuiTreeNodeFlags.CollapsingHeader;
        UserOrbitSettings _userOrbitSettings;
        int _arcSegments;
        ImVec3 _colour;
        int _maxAlpha;
        int _minAlpha;
        bool IsThreaded;
        private SettingsWindow()
        {
            _userOrbitSettings = _state.UserOrbitSettings;
            _arcSegments = _userOrbitSettings.NumberOfArcSegments;
            _maxAlpha = _userOrbitSettings.MaxAlpha;
            _minAlpha = _userOrbitSettings.MinAlpha;
            _colour = Helpers.Color(_userOrbitSettings.Red, _userOrbitSettings.Grn, _userOrbitSettings.Blu);
            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            IsThreaded = _state.Game.Settings.EnableMultiThreading;
        }
        internal static SettingsWindow GetInstance()
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(SettingsWindow)))
            {
                return new SettingsWindow();
            }
            return (SettingsWindow)_state.LoadedWindows[typeof(SettingsWindow)];
        }

        internal override void Display()
        {
            if (IsActive)
            {
                ImVec2 size = new ImVec2(200, 100);
                ImVec2 pos = new ImVec2(0, 0);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

                if (ImGui.Begin("Settings", ref IsActive, _flags))
                {

                    if (ImGui.Button("Show Debug Window"))
                    {
                        DebugWindow.GetInstance().IsActive = true;
                        //_state.LoadedWindows.Add(_state.Debug);

                    }
                    if (ImGui.CollapsingHeader("Process settings", _xpanderFlags))
                    {
                        if (ImGui.Checkbox("MultiThreaded", ref IsThreaded))
                        {
                            _state.Game.Settings.EnableMultiThreading = IsThreaded;
                        }

                    }


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

                }

                ImGui.End();
            }
        }
    }
}
