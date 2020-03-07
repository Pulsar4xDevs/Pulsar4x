using System;
using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class PerformanceDisplay : PulsarGuiWindow
    {
        
        SystemState _systemState;
        
        float largestGFPS = 0;
        int largestIndex = 0;

        float _currentGFPS;
        int _gameRateIndex = 0;
        float[] _gameRates = new float[80];

        float _currentFPS;
        int _frameRateIndex = 0;
        float[] _frameRates = new float[80];

        float _currentSFPS;
        int _systemRateIndex = 0;
        float[] _systemRates = new float[80];

        public void OnGlobalDateChange()
        {
            _currentGFPS = (float)_state.Game.GamePulse.LastSubtickTime.TotalSeconds;

            if (_currentGFPS > largestGFPS)
            {
                largestGFPS = _currentGFPS;
                largestIndex = 0;
            }
            else if (largestIndex == _gameRates.Length)
            {
                largestGFPS = _currentGFPS;
                foreach (var item in _gameRates)
                {
                    if (item > largestGFPS)
                        largestGFPS = item;
                }
            }
            else
            {
                largestIndex++;
            }

            _gameRates[_gameRateIndex] = _currentGFPS;
            if (_gameRateIndex >= _gameRates.Length - 1)
                _gameRateIndex = 0;
            else
                _gameRateIndex++;

        }
        
        
        
        private PerformanceDisplay() 
        {
              
        }
        internal static PerformanceDisplay GetInstance()
        {
            PerformanceDisplay instance;
            if (!_state.LoadedWindows.ContainsKey(typeof(PerformanceDisplay)))
                instance = new PerformanceDisplay();
            else
            {
                instance = (PerformanceDisplay)_state.LoadedWindows[typeof(PerformanceDisplay)];
            }
            instance._systemState = _state.StarSystemStates[_state.SelectedStarSysGuid];
            return instance;
        }
        
        
        void SetFrameRateArray()
        {
            _currentFPS = ImGui.GetIO().Framerate;
            _frameRates[_frameRateIndex] = _currentFPS;
            if (_frameRateIndex < _frameRates.Length - 1)
                _frameRateIndex++;
            else
                _frameRateIndex = 0;

        }

        internal override void Display()
        {
            if(IsActive && ImGui.Begin("Perf Display"))
            {
                SetFrameRateArray();
                //plot vars: (label, values, valueOffset, overlayText, scaleMin, scaleMax, graphSize, Stride)
                //core game processing rate.
                //ImGui.PlotHistogram("##GRHistogram", _gameRatesDisplay, 10, _timeSpan.TotalSeconds.ToString(), 0, 1f, new ImVec2(0, 80), sizeof(float));
                //ImGui.PlotHistogram("##GRHistogram1", _gameRatesDisplay, 0 , _timeSpan.TotalSeconds.ToString(), 0, 1f, new ImVec2(0, 80), sizeof(float));
                //string label, ref float values... 
                //ImGui.PlotHistogram(
                ImGui.PlotHistogram("Game Tick ##GTHistogram", ref _gameRates[0], _gameRates.Length, _gameRateIndex, _currentGFPS.ToString(), 0f, largestGFPS, new Vector2(248, 60), sizeof(float));
                ImGui.PlotLines("Game Tick ##GTPlotlines", ref _gameRates[0], _gameRates.Length, _gameRateIndex, _currentGFPS.ToString(), 0, largestGFPS, new Vector2(248, 60), sizeof(float));
                //current star system processing rate. 
                ImGui.PlotHistogram("System Tick ##STHistogram", ref _systemRates[0], _systemRates.Length, _systemRateIndex, _currentSFPS.ToString(), 0f, 1f, new Vector2(248, 60), sizeof(float));
                ImGui.PlotLines("System Tick ##STPlotlines", ref _systemRates[0], _systemRates.Length, _systemRateIndex, _currentSFPS.ToString(), 0, 1, new Vector2(248, 60), sizeof(float));
                //ui framerate
                ImGui.PlotHistogram("Frame Rate ##FPSHistogram", ref _frameRates[0], _frameRates.Length, _frameRateIndex, _currentFPS.ToString(), 0f, 10000, new Vector2(248, 60), sizeof(float));

                var data = _systemState.StarSystem.ManagerSubpulses.GetLastPerfData();
                foreach (var item in data.ProcessTimes)
                {
                    ImGui.Text(item.pname);
                    ImGui.SameLine();
                    ImGui.Text(item.psum.ToString() + "ms ");
                    ImGui.SameLine();
                    ImGui.Text( "ran: " + item.ptimes.Length.ToString() + " times");
                    ImGui.SameLine();
                    ImGui.Text( "averaging: " + (item.psum / item.ptimes.Length).ToString() + "ms");
                }

                foreach (var starsys in StaticRefLib.Game.Systems.Values)
                {
                    ImGui.Text(starsys.Guid.ToString());
                    ImGui.Text("    IsProcecssing: " + starsys.ManagerSubpulses.IsProcessing);
                    ImGui.Text("    CurrentProcess: " + starsys.ManagerSubpulses.CurrentProcess);
                }
            }
        }
    }
}