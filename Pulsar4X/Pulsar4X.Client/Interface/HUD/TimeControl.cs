using System;
using ImGuiNET;
using System.Numerics;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;

namespace Pulsar4X.Client
{
    public class TimeControl : PulsarGuiWindow
    {
        MasterTimePulse? _timeloop => _uiState.Game?.TimePulse;

        int _timeSpanValue = 1;
        int _timeSpanType = 3;
        new ImGuiWindowFlags _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;

        string[] _timespanTypeSelection = new string[8]
        {
            "Milliseconds",
            "Seconds",
            "Minutes",
            "Hours",
            "Days",
            "Weeks",
            "Months",
            "Years"
        };

        bool _expanded;

        float _freqTimeSpanValue = 1f;
        int _freqSpanType = 1;

        Vector2 _iconSize = new Vector2(16, 16);
        Vector2 _windowSize = new Vector2(200, 100);
        Vector2 _windowPosition = new Vector2(0, 0);

        private TimeControl()
        {
            IsActive = true;
            ReadTimeSpan();
            ReadFreqency();
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
            bool isPaused = !(_timeloop?.IsRunning ?? false);
            bool isStopping = (_timeloop?.IsStopping ?? false);
            var buttonTexture = isPaused ? _uiState.Img_Play() : _uiState.Img_Pause();

            ImGui.SetNextWindowSize(_windowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(_windowPosition, ImGuiCond.Appearing);

            Window.Begin("TimeControl", ref IsActive, _flags);
            ImGui.PushItemWidth(100);

            DateTime currenttime = _uiState.SelectedSystemTime;

            // Small arrow button for expanding time frequency menu
            if (ImGui.ArrowButton("##expand", _expanded ? ImGuiDir.Down : ImGuiDir.Right))
                _expanded = !_expanded;

            // Date display
            ImGui.SameLine();
            ImGui.Text(currenttime.ToShortDateString());

            // Time span slider
            ImGui.SameLine();
            ImGui.BeginDisabled(!isPaused);
            if (ImGui.SliderInt("##spnSldr", ref _timeSpanValue, 1, 60, _timeSpanValue.ToString()))
                AdjustTimeSpan();

            // Time duration combo
            ImGui.SameLine();
            if (ImGui.Combo("##spnCmbo", ref _timeSpanType, _timespanTypeSelection, _timespanTypeSelection.Length))
                AdjustTimeSpan();

            ImGui.EndDisabled();

            ImGui.SameLine();

            if (isStopping) ImGui.BeginDisabled();
            
            if (ImGui.ImageButton("playpause", buttonTexture.ToTextureRef(), _iconSize))
            {
                PausePlayPressed();
            }

            if (isStopping) ImGui.EndDisabled();

            // Step button only shown when paused
            if (isPaused)
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("onestep", _uiState.Img_OneStep().ToTextureRef(), _iconSize))
                {
                    OneStepPressed();
                }
            }
            else
            {
                ImGui.SameLine();
                ImGui.InvisibleButton("##onestep_invisbtn", _iconSize);
            }

            //When the submenu is expanded allow the user to adjust time frequency
            if (_expanded)
            {
                ImGui.PushItemWidth(100);
                ImGui.Indent();
                ImGui.Text(currenttime.ToString(_uiState.GameSettings.GetTimeFormat()));

                ImGui.BeginDisabled(!isPaused);
                ImGui.SameLine();
                float freqSliderMin = _freqSpanType == 0 ? 1 : 0.001f;
                float freqSliderMax = _freqSpanType == 0 ? 1000 : 60;
                if (_freqTimeSpanValue > freqSliderMax)
                    freqSliderMax = _freqTimeSpanValue;
                if (_freqTimeSpanValue > 0 && _freqTimeSpanValue < freqSliderMin)
                    freqSliderMin = _freqTimeSpanValue;

                string freqFormat = _freqSpanType == 0 ? "%.0f" : "%.3g";
                if (ImGui.SliderFloat("##freqSldr", ref _freqTimeSpanValue, freqSliderMin, freqSliderMax, freqFormat, ImGuiSliderFlags.None))
                {
                    _freqTimeSpanValue = _freqSpanType == 0
                        ? (float)Math.Round(_freqTimeSpanValue)
                        : (float)Math.Round(_freqTimeSpanValue, 3);
                    AdjustFreqency();
                }

                ImGui.SameLine();
                if (ImGui.Combo("##freqCmbo", ref _freqSpanType, _timespanTypeSelection, _timespanTypeSelection.Length))
                    ReadFreqency();
                ImGui.EndDisabled();
            }
            Window.End();
        }

        void AdjustTimeSpan()
        {
            if(_timeloop == null) return;

            switch (_timeSpanType)
            {
                case 0:
                    _timeloop.Ticklength = TimeSpan.FromMilliseconds(_timeSpanValue);
                    break;
                case 1:
                    _timeloop.Ticklength = TimeSpan.FromSeconds(_timeSpanValue);
                    break;
                case 2:
                    _timeloop.Ticklength = TimeSpan.FromMinutes(_timeSpanValue);
                    break;
                case 3:
                    _timeloop.Ticklength = TimeSpan.FromHours(_timeSpanValue);
                    break;
                case 4:
                    _timeloop.Ticklength = TimeSpan.FromDays(_timeSpanValue);
                    break;
                case 5:
                    _timeloop.Ticklength = TimeSpan.FromDays(_timeSpanValue * 7);
                    break;
                case 6:
                    _timeloop.Ticklength = TimeSpan.FromDays(_timeSpanValue * 30);
                    break;
                case 7:
                    _timeloop.Ticklength = TimeSpan.FromDays(_timeSpanValue * 365);
                    break;
            }
        }
        void ReadTimeSpan()
        {
            if(_timeloop == null) return;

            switch (_timeSpanType)
            {
                case 0:
                    _timeSpanValue = (int)_timeloop.Ticklength.TotalSeconds;
                    break;
                case 1:
                    _timeSpanValue = (int)_timeloop.Ticklength.TotalSeconds;
                    break;
                case 2:
                    _timeSpanValue = (int)_timeloop.Ticklength.TotalMinutes;
                    break;
                case 3:
                    _timeSpanValue = (int)_timeloop.Ticklength.TotalHours;
                    break;
                case 4:
                    _timeSpanValue = (int)_timeloop.Ticklength.TotalDays;
                    break;
                case 5:
                    _timeSpanValue = (int)_timeloop.Ticklength.TotalDays / 7;
                    break;
                case 6:
                    _timeSpanValue = (int)_timeloop.Ticklength.TotalDays / 30;
                    break;
                case 7:
                    _timeSpanValue = (int)_timeloop.Ticklength.TotalDays / 365;
                    break;
            }
        }
        void AdjustFreqency()
        {
            if(_timeloop == null) return;

            switch (_freqSpanType)
            {
                case 0:
                    _timeloop.TickFrequency = TimeSpan.FromMilliseconds(_freqTimeSpanValue);
                    break;
                case 1:
                    _timeloop.TickFrequency = TimeSpan.FromSeconds(_freqTimeSpanValue);
                    break;
                case 2:
                    _timeloop.TickFrequency = TimeSpan.FromMinutes(_freqTimeSpanValue);
                    break;
                case 3:
                    _timeloop.TickFrequency = TimeSpan.FromHours(_freqTimeSpanValue);
                    break;
                case 4:
                    _timeloop.TickFrequency = TimeSpan.FromDays(_freqTimeSpanValue);
                    break;
                case 5:
                    _timeloop.TickFrequency = TimeSpan.FromDays(_freqTimeSpanValue * 7);
                    break;
                case 6:
                    _timeloop.TickFrequency = TimeSpan.FromDays(_freqTimeSpanValue * 30);
                    break;
                case 7:
                    _timeloop.TickFrequency = TimeSpan.FromDays(_freqTimeSpanValue * 365);
                    break;
            }
        }
        void ReadFreqency()
        {
            if(_timeloop == null) return;

            switch (_freqSpanType)
            {
                case 0:
                    _freqTimeSpanValue = (float)_timeloop.TickFrequency.TotalMilliseconds;
                    break;
                case 1:
                    _freqTimeSpanValue = (float)_timeloop.TickFrequency.TotalSeconds;
                    break;
                case 2:
                    _freqTimeSpanValue = (float)_timeloop.TickFrequency.TotalMinutes;
                    break;
                case 3:
                    _freqTimeSpanValue = (float)_timeloop.TickFrequency.TotalHours;
                    break;
                case 4:
                    _freqTimeSpanValue = (float)_timeloop.TickFrequency.TotalDays;
                    break;
                case 5:
                    _freqTimeSpanValue = (float)_timeloop.TickFrequency.TotalDays / 7;
                    break;
                case 6:
                    _freqTimeSpanValue = (float)_timeloop.TickFrequency.TotalDays / 30;
                    break;
                case 7:
                    _freqTimeSpanValue = (float)_timeloop.TickFrequency.TotalDays / 365;
                    break;
            }
        }

        internal void PausePlayPressed()
        {
            if (_timeloop == null)
                return;

            if (_timeloop.IsRunning)
            {
                _timeloop.PauseTime();
            }
            else
            {
                _timeloop.StartTime();
            }
        }

        internal void OneStepPressed()
        {
            if (_timeloop == null)
                return;

            _timeloop.TimeStep();
        }
    }
}
