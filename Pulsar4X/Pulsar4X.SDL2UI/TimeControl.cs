using System;
using System.Collections.Generic;
using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public class TimeControl : PulsarGuiWindow
    {
        GlobalUIState _state;
        ECSLib.TimeLoop _timeloop {get { return _state.Game.GameLoop; } }
        int _buttonTextureID;
        bool _isPaused = true;
        int _timeSpanValue = 1;
        int _timeSpanType = 2;

        string[] _timespanTypeSelection = new string[7]
        {
            "Seconds",
            "Minutes",
            "Hours",
            "Days",
            "Weeks",
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

            if (ImGui.SliderInt("", ref _timeSpanValue, 1, 60, _timeSpanValue.ToString()))
                AdjustTimeSpan();
            ImGui.SameLine();
            if (ImGui.Combo("", ref _timeSpanType, _timespanTypeSelection))
                AdjustTimeSpan();
            ImGui.SameLine();
            //if (ImGui.ImageButton(_buttonTextureID, new ImVec2(16,16), 
            if (ImGui.Button(">"))
                PausePlayPressed();
            ImGui.SameLine();
            if (ImGui.Button("||>"))
                OneStepPressed();
            ImGui.End();

        }

        void AdjustTimeSpan()
        { 
            switch (_timeSpanType)
            {
                case 0:
                    _timeloop.Ticklength = TimeSpan.FromSeconds(_timeSpanValue);
                    break;
                case 1:
                    _timeloop.Ticklength = TimeSpan.FromMinutes(_timeSpanValue);
                    break;
                case 2:
                    _timeloop.Ticklength = TimeSpan.FromHours(_timeSpanValue);
                    break;
                case 3:
                    _timeloop.Ticklength = TimeSpan.FromDays(_timeSpanValue);
                    break;
                case 4:
                    _timeloop.Ticklength = TimeSpan.FromDays(_timeSpanValue * 7);
                    break;
                case 5:
                    _timeloop.Ticklength = TimeSpan.FromDays(_timeSpanValue * 30);
                    break;
                case 6:
                    _timeloop.Ticklength = TimeSpan.FromDays(_timeSpanValue * 365);
                    break;
            }
        }

        void PausePlayPressed()
        {
            if (_timeloop == null)
                return;
            if (_isPaused)
            {
                _timeloop.StartTime();
                _isPaused = false;
            }
            else
            {
                _timeloop.PauseTime();
                _isPaused = true;
            }   
        }
        void OneStepPressed()
        {
            if (_timeloop == null)
                return;
            _timeloop.TimeStep();
        }
    }
}
