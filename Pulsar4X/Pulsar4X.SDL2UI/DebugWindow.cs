using System;
using ImGuiNET;
using ImGuiSDL2CS;

namespace Pulsar4X.SDL2UI
{
    public class DebugWindow :PulsarGuiWindow
    {
        GlobalUIState _state;
        TimeSpan _timeSpan = new TimeSpan();

        int _gameRateIndex = 0;
        float[] _gameRates = new float[120];


        int _frameRateIndex = 0;
        float _currentFPS;
        float[] _frameRates = new float[120];

        public DebugWindow(GlobalUIState state)
        {
            _state = state;
        }

        internal void SetGameEvents()
        {
            if (_state.Game != null)
                _state.Game.GameLoop.GameGlobalDateChangedEvent += GameLoop_GameGlobalDateChangedEvent;
        }

        void GameLoop_GameGlobalDateChangedEvent(DateTime newDate)
        {
            _timeSpan = _state.Game.GameLoop.LastProcessingTime;
            _gameRates[_gameRateIndex] = (float)_timeSpan.TotalMilliseconds;
            if (_gameRateIndex < _gameRates.Length - 1)
                _gameRateIndex++;
            else
                _gameRateIndex = 0;
        }


        void SetFrameRateArray()
        {
            _currentFPS = ImGui.IO.Framerate;
            _frameRates[_frameRateIndex] = _currentFPS;
            if (_frameRateIndex < _frameRates.Length - 1)
                _frameRateIndex++;
            else
                _frameRateIndex = 0;

        }

        internal override void Display()
        {
            SetFrameRateArray();
            ImGui.Begin("debug");
            //core game processing rate.
            ImGui.PlotHistogram("##GRHistogram", _gameRates, _gameRateIndex, "", 0f, 2000f, new ImVec2(240, 100), 1);

            //current star system processing rate. 

            //ui framerate
            ImGui.PlotHistogram("##FPSHistogram", _frameRates, _frameRateIndex, _currentFPS.ToString(), 0f, 2000f, new ImVec2(240, 100), 1);
            ImGui.End();

        }

    }
}
