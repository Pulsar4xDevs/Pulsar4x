using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    
    public class PerformanceDisplay : PulsarGuiWindow
    {

        private string[] _dataBlobTypeStrings;
        private Type[] _dataBlobTypes;
        private int _dataBlobTypeIndex;

        
        Stopwatch _sw = new Stopwatch();
        
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
        

        private double ticks1;
        private double ms1;
        private double ticks2;
        private double ticks3;
        private double count1;
        private double count2;
        private double count3;
        
        
        
        private PerformanceDisplay() 
        {
            _dataBlobTypes = new Type[EntityManager.DataBlobTypes.Count];
            _dataBlobTypeStrings = new String[EntityManager.DataBlobTypes.Count];

            int i = 0;
            foreach (var kvp in EntityManager.DataBlobTypes)
            {
                _dataBlobTypes[i]=kvp.Key;
                _dataBlobTypeStrings[i] = kvp.Key.Name;
                i++;
            }
        }
        internal static PerformanceDisplay GetInstance()
        {
            PerformanceDisplay instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(PerformanceDisplay)))
                instance = new PerformanceDisplay();
            else
            {
                instance = (PerformanceDisplay)_uiState.LoadedWindows[typeof(PerformanceDisplay)];
            }
            instance._systemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
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
                ImGui.Text("Global Tick: "); ImGui.SameLine();
                var t_lpt = _uiState.Game.GamePulse.LastProcessingTime.TotalMilliseconds;
                var t_tf = _uiState.Game.GamePulse.TickFrequency.TotalMilliseconds;
                var txt_lpt = t_lpt.ToString();
                var col = new Vector4(0, 1, 0, 1);
                if (t_lpt > _uiState.Game.GamePulse.TickFrequency.TotalMilliseconds)
                    col = new Vector4(1, 0, 0, 1);
                ImGui.Text(txt_lpt); ImGui.SameLine();
                var overtime = t_lpt - t_tf;
                ImGui.TextColored(col, overtime.ToString());
                
                
                //plot vars: (label, values, valueOffset, overlayText, scaleMin, scaleMax, graphSize, Stride)
                //core game processing rate.
                //ImGui.PlotHistogram("##GRHistogram", _gameRatesDisplay, 10, _timeSpan.TotalSeconds.ToString(), 0, 1f, new ImVec2(0, 80), sizeof(float));
                //ImGui.PlotHistogram("##GRHistogram1", _gameRatesDisplay, 0 , _timeSpan.TotalSeconds.ToString(), 0, 1f, new ImVec2(0, 80), sizeof(float));
                //string label, ref float values... 
                //ImGui.PlotHistogram(
                ImGui.PlotHistogram("Game Tick ##GTHistogram", ref _gameRates[0], _gameRates.Length, _gameRateIndex, _currentGFPS.ToString(), 0f, largestGFPS, new System.Numerics.Vector2(248, 60), sizeof(float));
                ImGui.PlotLines("Game Tick ##GTPlotlines", ref _gameRates[0], _gameRates.Length, _gameRateIndex, _currentGFPS.ToString(), 0, largestGFPS, new System.Numerics.Vector2(248, 60), sizeof(float));
                //current star system processing rate. 
                ImGui.PlotHistogram("System Tick ##STHistogram", ref _systemRates[0], _systemRates.Length, _systemRateIndex, _currentSFPS.ToString(), 0f, 1f, new System.Numerics.Vector2(248, 60), sizeof(float));
                ImGui.PlotLines("System Tick ##STPlotlines", ref _systemRates[0], _systemRates.Length, _systemRateIndex, _currentSFPS.ToString(), 0, 1, new System.Numerics.Vector2(248, 60), sizeof(float));
                //ui framerate
                ImGui.PlotHistogram("Frame Rate ##FPSHistogram", ref _frameRates[0], _frameRates.Length, _frameRateIndex, _currentFPS.ToString(), 0f, 10000, new System.Numerics.Vector2(248, 60), sizeof(float));

                var data = _systemState.StarSystem.ManagerSubpulses.GetLastPerfData();
                ImGui.Text("StarSystemID: " + _systemState.StarSystem.Guid);
                foreach (var item in data.ProcessTimes)
                {
                    ImGui.Text(item.pname);
                    ImGui.SameLine();
                    ImGui.Text((item.psum * 1000000).ToString() + "ns ");
                    ImGui.SameLine();
                    ImGui.Text( "ran: " + item.ptimes.Length.ToString() + " times");
                    ImGui.SameLine();
                    ImGui.Text( "averaging: " + (item.psum * 1000000 / item.ptimes.Length).ToString() + "ns");
                }
                ImGui.Text("    IsProcecssing: " + _systemState.StarSystem.ManagerSubpulses.IsProcessing);
                ImGui.Text("    CurrentProcess: " + _systemState.StarSystem.ManagerSubpulses.CurrentProcess);
                ImGui.Text("    Last Total ProcessTime: " + data.FullPulseTimeMS);

                var numDB = _systemState.StarSystem.GetAllDataBlobsOfType<OrbitDB>().Count;
                ImGui.Text("ObitDB Count: " + numDB);
                numDB = _systemState.StarSystem.GetAllDataBlobsOfType<NewtonMoveDB>().Count;
                ImGui.Text("NewtonMoveDB Count: " + numDB);
                numDB = _systemState.StarSystem.GetAllDataBlobsOfType<SensorAbilityDB>().Count;
                ImGui.Text("SensorAbilityDB Count: " + numDB);
                
                
                if(ImGui.CollapsingHeader("All Systems"))
                {
                    foreach (var starsys in StaticRefLib.Game.Systems.Values)
                    {
                        ImGui.Text(starsys.Guid.ToString());
                        ImGui.Text("    IsProcecssing: " + starsys.ManagerSubpulses.IsProcessing);
                        ImGui.Text("    CurrentProcess: " + starsys.ManagerSubpulses.CurrentProcess);
                        ImGui.Text("    Last Total ProcessTime: " + starsys.ManagerSubpulses.GetLastPerfData().FullPulseTimeMS);

                    }
                }


                if (ImGui.CollapsingHeader("Call Times"))
                {

                    
                    
                    if (ImGui.Button("Time"))
                    {
                        _sw.Restart();
                        List<Entity> entites = _systemState.StarSystem.GetAllEntitiesWithDataBlob<NewtonMoveDB>();
                        _sw.Stop();
                        count1 = entites.Count;
                        ticks1 = _sw.ElapsedTicks;
                        ms1 = _sw.Elapsed.TotalMilliseconds;
                        
                        _sw.Restart();
                        var datablobs = _systemState.StarSystem.GetAllDataBlobsOfType<NewtonMoveDB>();
                        _sw.Stop();
                        count2 = datablobs.Count;
                        ticks2 = _sw.ElapsedTicks;

                        int typeIndex = EntityManager.GetTypeIndex<NewtonMoveDB>();
                        _sw.Restart();
                        datablobs = _systemState.StarSystem.GetAllDataBlobsOfType<NewtonMoveDB>(typeIndex);
                        _sw.Stop();
                        count3 = datablobs.Count;
                        ticks3 = _sw.ElapsedTicks;
                        
                        

                        
                    }
                    
                    
                    
                    
                    
                    
                    
                    ImGui.Text("Using GetEntitysWithDatablob");
                    ImGui.Text(ticks1 + " ticks to retreave " + count1 + " Entites");
                    ImGui.Text(ms1 + " in ms");
                    ImGui.Text("Using GetAllDataBlobsOfType<T>()");
                    ImGui.Text(ticks2 + " ticks to retreave " + count2 + " Datablobs by Type");
                    ImGui.Text("Using GetAllDataBlobsOfType<T>(int typeIndex)");
                    ImGui.Text(ticks3 + " ticks to retreave " + count3 + " Datablobs by typeIndex");
                
                    
                    
                    
                }
            }
        }

        public override void OnGameTickChange(DateTime newDate)
        {
            _currentGFPS = (float)_uiState.Game.GamePulse.LastSubtickTime.TotalSeconds;

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

        public override void OnSystemTickChange(DateTime newDate)
        {
            
        }

        public override void OnSelectedSystemChange(StarSystem newStarSys)
        {
            throw new NotImplementedException();
        }
    }
}