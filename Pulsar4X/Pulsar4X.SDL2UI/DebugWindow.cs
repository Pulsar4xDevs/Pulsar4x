using System;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class DebugWindow :PulsarGuiWindow
    {
        TimeSpan _timeSpan = new TimeSpan();

        int _gameRateIndex = 0;
        float[] _gameRates = new float[10];
        float _lastProcessTime;
        int _frameRateIndex = 0;
        float _currentFPS;
        float[] _frameRates = new float[120];

        private DebugWindow() 
        {
        }
        internal static DebugWindow GetInstance()
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(DebugWindow)))
            {
                return new DebugWindow();
            }
            return (DebugWindow)_state.LoadedWindows[typeof(DebugWindow)];
        }



        internal void SetGameEvents()
        {
            if (_state.Game != null)
                _state.Game.GameLoop.GameGlobalDateChangedEvent += GameLoop_GameGlobalDateChangedEvent;
        }

        void GameLoop_GameGlobalDateChangedEvent(DateTime newDate)
        {
            _timeSpan = _state.Game.GameLoop.LastProcessingTime;
            _gameRates[_gameRateIndex] = (float)_timeSpan.TotalSeconds;
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
            if (IsActive)
            {
                SetFrameRateArray();
                if (ImGui.Begin("debug", ref IsActive))
                {
                    if (ImGui.CollapsingHeader("FrameRates", ImGuiTreeNodeFlags.CollapsingHeader))
                    {
                        //core game processing rate.
                        ImGui.PlotHistogram("##GRHistogram", _gameRates, _gameRateIndex, _timeSpan.TotalSeconds.ToString(), 0, 1, new ImVec2(0, 80), 1);
                        //ImGui.PlotHistogram("##GRHistogram1", _gameRates, _gameRateIndex , _timeSpan.TotalSeconds.ToString(), 1f, 10, new ImVec2(240, 100), 1);
                        //ImGui.PlotHistogram("##GRHistogram2", _gameRates, _gameRateIndex, _timeSpan.TotalSeconds.ToString(), 0.1f, 0.10f, new ImVec2(240, 100), 1);

                        //current star system processing rate. 

                        //ui framerate
                        ImGui.PlotHistogram("##FPSHistogram", _frameRates, _frameRateIndex, _currentFPS.ToString(), 0f, 1, new ImVec2(0, 80), 1);
                    }
                    if (_state.LastClickedEntity.OrbitIcon != null)
                    {
                        if (ImGui.CollapsingHeader("Selected Entity: " + _state.LastClickedEntity.Name, ImGuiTreeNodeFlags.CollapsingHeader))
                        {
                            string startRadian = _state.LastClickedEntity.OrbitIcon._ellipseStartArcAngleRadians.ToString();
                            string startDegrees = Angle.ToDegrees(_state.LastClickedEntity.OrbitIcon._ellipseStartArcAngleRadians).ToString();
                            ImGui.Text("StartAngleRadians: " + startRadian);
                            ImGui.Text("StartAngleDegrees: " + startDegrees);
                        }
                    }
                }
                //else IsActive = false;
                ImGui.End();
            }
        
        }

    }
}
