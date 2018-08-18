using System;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class DebugWindow :PulsarGuiWindow
    {
        
        float largestGFPS = 0;
        int largestIndex = 0;
/*
float nextLargeGFPS = 0;
        int nextLargeIndex = 0;
*/
        float _currentGFPS;
        int _gameRateIndex = 0;
        float[] _gameRates = new float[80];

        float _currentFPS;
        int _frameRateIndex = 0;
        float[] _frameRates = new float[80];

        float _currentSFPS;
        int _systemRateIndex = 0;
        float[] _systemRates = new float[80];

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
            {
                _state.Game.GameLoop.GameGlobalDateChangedEvent += GameLoop_GameGlobalDateChangedEvent;
                _state.MapRendering.SysMap.SystemSubpulse.SystemDateChangedEvent += SystemSubpulse_SystemDateChangedEvent;
            }
        }

        void GameLoop_GameGlobalDateChangedEvent(DateTime newDate)
        {
            _currentGFPS = (float)_state.Game.GameLoop.LastSubtickTime.TotalSeconds;

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
            /*
            else if (_currentGFPS > nextLargeGFPS)
            {
                nextLargeGFPS = _currentGFPS;
                nextLargeIndex = 0;
            }
            if (largestIndex > _gameRates.Length * 2)
            {
                largestGFPS = nextLargeGFPS;
                largestIndex = nextLargeIndex;
                nextLargeGFPS = _currentGFPS;
                nextLargeIndex = 0;
            }
            if(nextLargeIndex > _gameRates.Length)
            {
                nextLargeGFPS = _currentGFPS;
                nextLargeIndex = 0;
            }*/
            _gameRates[_gameRateIndex] = _currentGFPS;
            if (_gameRateIndex >= _gameRates.Length - 1)
                _gameRateIndex = 0;
            else
                _gameRateIndex++;
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

        void SystemSubpulse_SystemDateChangedEvent(DateTime newDate)
        {
            _currentSFPS = (float)_state.Game.GameLoop.LastSubtickTime.TotalSeconds;
            _systemRates[_systemRateIndex] = _currentSFPS;
            if (_systemRateIndex >= _systemRates.Length - 1)
                _systemRateIndex = 0;
            else
                _systemRateIndex++;
        }

        DateTime lastDate = new DateTime();
        Vector4 pos = new Vector4();
        double truAnomoly = 0;

        internal override void Display()
        {
            if (IsActive)
            {
                SetFrameRateArray();
                if (ImGui.Begin("debug", ref IsActive))
                {
                    ImGui.Text(_state.CurrentSystemDateTime.ToString());
                    if (ImGui.CollapsingHeader("FrameRates", ImGuiTreeNodeFlags.CollapsingHeader))
                    {

                        //plot vars: (label, values, valueOffset, overlayText, scaleMin, scaleMax, graphSize, Stride)
                        //core game processing rate.
                        //ImGui.PlotHistogram("##GRHistogram", _gameRatesDisplay, 10, _timeSpan.TotalSeconds.ToString(), 0, 1f, new ImVec2(0, 80), sizeof(float));
                        //ImGui.PlotHistogram("##GRHistogram1", _gameRatesDisplay, 0 , _timeSpan.TotalSeconds.ToString(), 0, 1f, new ImVec2(0, 80), sizeof(float));
                        ImGui.PlotHistogram("Game Tick ##GTHistogram", _gameRates, _gameRateIndex, _currentGFPS.ToString(), 0f, largestGFPS, new ImVec2(248, 60), sizeof(float));
                        ImGui.PlotLines("Game Tick ##GTPlotlines", _gameRates, _gameRateIndex, _currentGFPS.ToString(), 0, largestGFPS, new ImVec2(248, 60), sizeof(float));
                        //current star system processing rate. 
                        ImGui.PlotHistogram("System Tick ##STHistogram", _systemRates, _systemRateIndex, _currentSFPS.ToString(), 0f, 1f, new ImVec2(248, 60), sizeof(float));
                        ImGui.PlotLines("System Tick ##STPlotlines", _systemRates, _systemRateIndex, _currentSFPS.ToString(), 0, 1, new ImVec2(248, 60), sizeof(float));
                        //ui framerate
                        ImGui.PlotHistogram("Frame Rate ##FPSHistogram", _frameRates, _frameRateIndex, _currentFPS.ToString(), 0f, 10000, new ImVec2(248, 60), sizeof(float));

                    }
                    if (_state.LastClickedEntity.OrbitIcon != null)
                    {
                        if (ImGui.CollapsingHeader("Selected Entity: " + _state.LastClickedEntity.Name + "###OrbitHeader" , ImGuiTreeNodeFlags.CollapsingHeader ))
                        {
                            OrbitDB orbitDB = _state.LastClickedEntity.Entity.GetDataBlob<OrbitDB>();
                            
                            string startRadian = _state.LastClickedEntity.OrbitIcon._ellipseStartArcAngleRadians.ToString();
                            string startDegrees = Angle.ToDegrees(_state.LastClickedEntity.OrbitIcon._ellipseStartArcAngleRadians).ToString();
                            ImGui.Text("StartAngleRadians: " + startRadian);
                            ImGui.Text("StartAngleDegrees: " + startDegrees);

                            if (_state.CurrentSystemDateTime != lastDate)
                            {
                                pos = OrbitProcessor.GetAbsolutePosition(orbitDB, _state.CurrentSystemDateTime);
                                truAnomoly = OrbitProcessor.GetTrueAnomaly(orbitDB, _state.CurrentSystemDateTime);
                                lastDate = _state.CurrentSystemDateTime;
                            }

                            ImGui.Text("x: " + pos.X);
                            ImGui.Text("y: " + pos.Y);
                            ImGui.Text("z: " + pos.Z);
                            ImGui.Text("TrueAnomaly: " + truAnomoly);
                            ImGui.Text("MeanMotion: " + orbitDB.MeanMotion);

                        }
                    }
                }
                //else IsActive = false;
                ImGui.End();
            }
        
        }



    }
}
