using System;
using System.Collections.Generic;
using ImGuiNET;
using ImGuiSDL2CS;
using System.Numerics;
using Vector2 = System.Numerics.Vector2;

namespace Pulsar4X.SDL2UI
{
    public class TimeControl : PulsarGuiWindow
    {
        ECSLib.MasterTimePulse _timeloop => _uiState.Game.GamePulse;

        bool _isPaused = true;
        int _timeSpanValue = 1;
        int _timeSpanType = 2;
        new ImGuiWindowFlags _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;

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

        ImGuiTreeNodeFlags _xpanderFlags = ImGuiTreeNodeFlags.AllowItemOverlap;
        float _freqTimeSpanValue = 0.5f;
        int _freqSpanType = 0;

        private TimeControl()
        {
            IsActive = true;
        }

        internal static TimeControl GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(TimeControl)))
            {
                return new TimeControl();
            }
            return (TimeControl)_uiState.LoadedWindows[typeof(TimeControl)];
        }

        internal override void Display()
        {
            var iconSize = new Vector2(16, 16);
            var size = new Vector2(200, 100);
            var pos = new Vector2(0,0);
            var col = new Vector4(0, 0, 0, 0);


            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

            ImGui.Begin("TimeControl", ref IsActive, _flags);
            ImGui.PushItemWidth(100);

            ImGui.PushStyleColor(ImGuiCol.Header, col);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, col);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, col);

            DateTime currenttime = _uiState.SelectedSystemTime;
            if (ImGui.CollapsingHeader("", _xpanderFlags))//Let the user open up the the time frequency menu
                _expanded = true;
            else
                _expanded = false;
            ImGui.PopStyleColor(3);
            ImGui.SameLine();
            ImGui.Text(currenttime.ToShortDateString());
            ImGui.SameLine();
            if (ImGui.SliderInt("##spnSldr", ref _timeSpanValue, 1, 60, _timeSpanValue.ToString()))
                AdjustTimeSpan();
            ImGui.SameLine();
            if (ImGui.Combo("##spnCmbo", ref _timeSpanType, _timespanTypeSelection, _timespanTypeSelection.Length))
                AdjustTimeSpan();
            ImGui.SameLine();
            if (_isPaused == true)//When time is paused
            {
                if (ImGui.ImageButton(_uiState.Img_Play(), iconSize))//Provide a button to unpause
                    PausePlayPressed();
                ImGui.SameLine();
                if (ImGui.ImageButton(_uiState.Img_OneStep(), iconSize))//Provide a button to increment time
                    OneStepPressed();
            }
            else//When time is running
            {
                if (ImGui.ImageButton(_uiState.Img_Pause(), iconSize))//Provide a button to unpause time
                    PausePlayPressed();
            }
            

            
            if (_expanded)//When the submenu is expanded allow the user to adjust time frequency
            {
                ImGui.PushItemWidth(100);
                ImGui.Text("   " + currenttime.ToShortTimeString());
                ImGui.SameLine();
                if (ImGui.SliderFloat("##freqSldr", ref _freqTimeSpanValue, 0.1f, 1, _freqTimeSpanValue.ToString(), ImGuiSliderFlags.None))
                    AdjustFreqency();
                ImGui.SameLine();
                if (ImGui.Combo("##freqCmbo", ref _freqSpanType, _timespanTypeSelection, _timespanTypeSelection.Length))
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
            switch (_freqSpanType)
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
