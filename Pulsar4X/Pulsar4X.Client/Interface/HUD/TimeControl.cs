using System;
using ImGuiNET;
using System.Numerics;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    public class TimeControl : UniquePulsarGuiWindow<TimeControl>
    {
        // The client reads the clock from the galaxy model and submits changes as commands; it no
        // longer touches the engine's MasterTimePulse directly.
        private TimeState? Time => _uiState.GameClient?.Galaxy.Time;

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
            if(_uiState.TryGetUniqueWindow<TimeControl>(out var window))
            {
                return window;
            }

            return _uiState.AddUniqueWindow(new TimeControl());
        }

        private void Submit(TimeControlRequest request) => _uiState.GameClient?.SetTimeControlAsync(request);

        internal override void Display()
        {
            var time = Time;
            bool isPaused = !(time?.IsRunning ?? false);
            bool isStopping = time?.IsStopping ?? false;
            var buttonTexture = isPaused ? _uiState.Img_Play() : _uiState.Img_Pause();

            ImGui.SetNextWindowSize(_windowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(_windowPosition, ImGuiCond.Appearing);

            Window.Begin("TimeControl", ref IsActive, _flags);
            ImGui.PushItemWidth(100);

            DateTime currenttime = time?.GameDateTime ?? default;

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

        // Converts a (value, unit-index) pair from the combo boxes into a TimeSpan.
        private static TimeSpan ToTimeSpan(double value, int unitType) => unitType switch
        {
            0 => TimeSpan.FromMilliseconds(value),
            1 => TimeSpan.FromSeconds(value),
            2 => TimeSpan.FromMinutes(value),
            3 => TimeSpan.FromHours(value),
            4 => TimeSpan.FromDays(value),
            5 => TimeSpan.FromDays(value * 7),
            6 => TimeSpan.FromDays(value * 30),
            7 => TimeSpan.FromDays(value * 365),
            _ => TimeSpan.FromHours(value),
        };

        void AdjustTimeSpan()
        {
            Submit(new TimeControlRequest(TimeControlAction.SetTickLength, TickLength: ToTimeSpan(_timeSpanValue, _timeSpanType)));
        }

        void ReadTimeSpan()
        {
            if (Time is not { } time) return;

            _timeSpanValue = _timeSpanType switch
            {
                0 => (int)time.TickLength.TotalMilliseconds,
                1 => (int)time.TickLength.TotalSeconds,
                2 => (int)time.TickLength.TotalMinutes,
                3 => (int)time.TickLength.TotalHours,
                4 => (int)time.TickLength.TotalDays,
                5 => (int)time.TickLength.TotalDays / 7,
                6 => (int)time.TickLength.TotalDays / 30,
                7 => (int)time.TickLength.TotalDays / 365,
                _ => _timeSpanValue,
            };
        }

        void AdjustFreqency()
        {
            Submit(new TimeControlRequest(TimeControlAction.SetTickFrequency, TickFrequency: ToTimeSpan(_freqTimeSpanValue, _freqSpanType)));
        }

        void ReadFreqency()
        {
            if (Time is not { } time) return;

            _freqTimeSpanValue = _freqSpanType switch
            {
                0 => (float)time.TickFrequency.TotalMilliseconds,
                1 => (float)time.TickFrequency.TotalSeconds,
                2 => (float)time.TickFrequency.TotalMinutes,
                3 => (float)time.TickFrequency.TotalHours,
                4 => (float)time.TickFrequency.TotalDays,
                5 => (float)time.TickFrequency.TotalDays / 7,
                6 => (float)time.TickFrequency.TotalDays / 30,
                7 => (float)time.TickFrequency.TotalDays / 365,
                _ => _freqTimeSpanValue,
            };
        }

        internal void PausePlayPressed()
        {
            bool isRunning = Time?.IsRunning ?? false;
            Submit(new TimeControlRequest(isRunning ? TimeControlAction.Pause : TimeControlAction.Start));
        }

        internal void OneStepPressed()
        {
            // Advances by the current tick length (set via the time-span controls).
            Submit(new TimeControlRequest(TimeControlAction.StepOnce));
        }
    }
}
