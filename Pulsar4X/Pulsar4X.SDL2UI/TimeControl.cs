using System;
using System.Collections.Generic;
using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public class TimeControl : PulsarGuiWindow
    {
        GlobalUIState _state;
        int _buttonTextureID;
        bool _paused = true;
        int timeSpanValue = 1;
        int _timeSpanType = 2;

        string[] _timespanTypeSelection = new string[6]
        {
            "Seconds",
            "Minutes",
            "Hours",
            "Days",
            "Months",
            "Years"
        };

        internal TimeControl(GlobalUIState state)
        {
            IsActive = true;
            _state = state;
            _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;
        }

        internal override void Display()
        {
            ImVec2 size = new ImVec2(200, 100);
            ImVec2 pos = new ImVec2(0,0);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);

            ImGui.Begin("TimeControl", ref IsActive, _flags);

            ImGui.SliderInt("", ref timeSpanValue, 1, 60, timeSpanValue.ToString());

            ImGui.SameLine();
            ImGui.Combo("", ref _timeSpanType, _timespanTypeSelection);
            //ImGui.sel

            //ImGuiInputTextFlags.
            //if (ImGui.ImageButton(_buttonTextureID, new ImVec2(16,16), ...
            if (ImGui.Button(">"))
                PausePlayPressed();
            ImGui.SameLine();
            if (ImGui.Button("||>"))
                OneStepPressed();
            ImGui.End();

        }

        void PausePlayPressed()
        {
            
        }
        void OneStepPressed()
        { }
    }
}
