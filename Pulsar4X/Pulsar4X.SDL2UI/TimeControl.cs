using System;
using System.Collections.Generic;
using ImGuiNET;
using ImGuiSDL2CS;

namespace Pulsar4X.SDL2UI
{
    public class TimeControl : PulsarGuiWindow
    {
        GlobalUIState _state;
        ECSLib.TimeLoop _timeloop {get { return _state.Game.GameLoop; } }

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

        bool _expanded;

        ImGuiTreeNodeFlags _xpanderFlags = ImGuiTreeNodeFlags.AllowOverlapMode;
        float _freqTimeSpanValue = 0.5f;
        int _freqSpanType = 0;

        internal TimeControl(GlobalUIState state)
        {
            IsActive = true;
            _state = state;

            _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;



        }

        protected override void DisplayActual()
        {
            ImVec2 size = new ImVec2(200, 100);
            ImVec2 pos = new ImVec2(0,0);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

            ImGui.Begin("TimeControl", ref IsActive, _flags);
            ImGui.PushItemWidth(100);
            ImGui.PushStyleColor(ImGuiCol.Header, new ImVec4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new ImVec4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new ImVec4(0, 0, 0, 0));
            if (ImGui.CollapsingHeader("", _xpanderFlags))
                _expanded = true;
            else
                _expanded = false;
            ImGui.PopStyleColor(3);
            ImGui.SameLine();
            if (ImGui.SliderInt("##spnSldr", ref _timeSpanValue, 1, 60, _timeSpanValue.ToString()))
                AdjustTimeSpan();
            ImGui.SameLine();
            if (ImGui.Combo("##spnCmbo", ref _timeSpanType, _timespanTypeSelection))
                AdjustTimeSpan();
            ImGui.SameLine();

            if (ImGui.ImageButton(_state.GLImageDictionary["PlayImg"], new ImVec2(16, 16), new ImVec2(0, 0), new ImVec2(16, 16), 0, new ImVec4(0, 0, 0, 0), new ImVec4(0, 0, 0, 0)))
                PausePlayPressed();
            ImGui.SameLine();
            if (ImGui.Button("||>"))
                OneStepPressed();
            if (_expanded)
            {
                ImGui.PushItemWidth(100);
                if (ImGui.SliderFloat("##freqSldr", ref _freqTimeSpanValue, 0.1f, 1, _freqTimeSpanValue.ToString(), 1))
                    AdjustFreqency();
                ImGui.SameLine();
                if (ImGui.Combo("##freqCmbo", ref _freqSpanType, _timespanTypeSelection))
                    AdjustFreqency();
            }



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
        void AdjustFreqency()
        {
            switch (_timeSpanType)
            {
                case 0:
                    _timeloop.TickFrequency = TimeSpan.FromSeconds(_freqTimeSpanValue);
                    break;
                case 1:
                    _timeloop.TickFrequency = TimeSpan.FromMinutes(_freqTimeSpanValue);
                    break;
                case 2:
                    _timeloop.TickFrequency = TimeSpan.FromHours(_freqTimeSpanValue);
                    break;
                case 3:
                    _timeloop.TickFrequency = TimeSpan.FromDays(_freqTimeSpanValue);
                    break;
                case 4:
                    _timeloop.TickFrequency = TimeSpan.FromDays(_freqTimeSpanValue * 7);
                    break;
                case 5:
                    _timeloop.TickFrequency = TimeSpan.FromDays(_freqTimeSpanValue * 30);
                    break;
                case 6:
                    _timeloop.TickFrequency = TimeSpan.FromDays(_freqTimeSpanValue * 365);
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
