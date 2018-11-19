using System;
using ImGuiNET;
using System.Numerics;
namespace Pulsar4X.SDL2UI
{
    public class SettingsWindow : PulsarGuiWindow
    {
        ImGuiTreeNodeFlags _xpanderFlags = ImGuiTreeNodeFlags.CollapsingHeader;
        UserOrbitSettings _userOrbitSettings;
        int _arcSegments;
        Vector3 _colour;
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
                Vector2 size = new Vector2(200, 100);
                Vector2 pos = new Vector2(0, 0);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

                if (ImGui.Begin("Settings", ref IsActive, _flags))
                {

                    if (ImGui.Button("Show Debug Window"))
                    {
                        var instance = DebugWindow.GetInstance();
                        instance.IsActive = !instance.IsActive;
                        //_state.LoadedWindows.Add(_state.Debug);

                    }
                    if (ImGui.Button("Show ImguiMetrix"))
                        _state.ShowMetrixWindow = !_state.ShowMetrixWindow;
                    if (ImGui.Button("Show ImgDebug"))
                        _state.ShowImgDbg = !_state.ShowImgDbg;

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

                        if (ImGui.ColorEdit3("Orbit Ring Colour", ref _colour))
                        {
                            _userOrbitSettings.Red = Helpers.Color(_colour.X);
                            _userOrbitSettings.Grn = Helpers.Color(_colour.Y);
                            _userOrbitSettings.Blu = Helpers.Color(_colour.Z);
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
