using System;
using System.Collections.Generic;
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
        bool _dateChangeSinceLastFrame = true;
        bool _isRunningFrame = false;

        List<Vector4> positions = new List<Vector4>();

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

        private void _state_EntityClicked(Guid arg1, MouseButtons arg2)
        {
            throw new NotImplementedException();
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

            positions.Add(_state.LastClickedEntity.Entity.GetDataBlob<PositionDB>().AbsolutePosition_AU);
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
            _dateChangeSinceLastFrame = true;
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
            _isRunningFrame = true;
            if (IsActive)
            {
                SetFrameRateArray();
                if (ImGui.Begin("debug", ref IsActive))
                {
                    ImGui.Text(_state.CurrentSystemDateTime.ToString());
                    ImGui.Text("Cursor World Coordinate:");
                    var mouseWorldCoord = _state.Camera.MouseWorldCoordinate();
                    ImGui.Text("x" + mouseWorldCoord.X);
                    ImGui.SameLine();
                    ImGui.Text("y" + mouseWorldCoord.Y);

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

                    if (_state.LastClickedEntity.Name != null)
                    {
                        if (ImGui.CollapsingHeader("Selected Entity: " + _state.LastClickedEntity.Name + "###NameHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                        {
                            ImGui.Text(_state.LastClickedEntity.Entity.Guid.ToString());
                            if (_state.LastClickedEntity.Entity.HasDataBlob<PositionDB>())
                            {
                                var positiondb = _state.LastClickedEntity.Entity.GetDataBlob<PositionDB>();
                                var posv4 = positiondb.AbsolutePosition_AU;
                                ImGui.Text("x: " + posv4.X);
                                ImGui.Text("y: " + posv4.Y);
                                ImGui.Text("z: " + posv4.Z);
                                if (positiondb.Parent != null)
                                {
                                    ImGui.Text("Parent: " + positiondb.Parent.GetDataBlob<NameDB>().DefaultName);
                                    ImGui.Text("Dist: " + Distance.AuToKm( positiondb.RelativePosition_AU.Length()));
                                }
                            }
                            if (_state.LastClickedEntity.Entity.HasDataBlob<OrbitDB>())
                            {

                                if (ImGui.CollapsingHeader("OrbitDB: ###OrbitDBHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    OrbitDB orbitDB = _state.LastClickedEntity.Entity.GetDataBlob<OrbitDB>();


                                    //if (_state.CurrentSystemDateTime != lastDate)
                                    //{
                                        pos = OrbitProcessor.GetAbsolutePosition_AU(orbitDB, _state.CurrentSystemDateTime);
                                        truAnomoly = OrbitProcessor.GetTrueAnomaly(orbitDB, _state.CurrentSystemDateTime);
                                        lastDate = _state.CurrentSystemDateTime;
                                    //}

                                    ImGui.Text("x: " + pos.X);
                                    ImGui.Text("y: " + pos.Y);
                                    ImGui.Text("z: " + pos.Z);
                                    ImGui.Text("Eccentricity: " + orbitDB.Eccentricity);
                                    ImGui.Text("AoP:" + orbitDB.ArgumentOfPeriapsis);
                                    ImGui.Text("TrueAnomaly: " + truAnomoly);
                                    ImGui.Text("MeanMotion: " + orbitDB.MeanMotion);
                                    ImGui.Text("SOI Radius: " + Distance.AuToKm( GMath.GetSOI(_state.LastClickedEntity.Entity)));
                                    ImGui.Text("Orbital Period:" + orbitDB.OrbitalPeriod);
                                    ImGui.Text("SemiMajAxis: " + orbitDB.SemiMajorAxis);   
                                                
                                }
                            }
                            if (_state.LastClickedEntity.OrbitIcon != null)
                            {

                                if (ImGui.CollapsingHeader("OrbitIcon: ###OrbitIconHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    OrbitDB orbitDB = _state.LastClickedEntity.Entity.GetDataBlob<OrbitDB>();

                                    string startRadian = _state.LastClickedEntity.OrbitIcon._ellipseStartArcAngleRadians.ToString();
                                    string startDegrees = Angle.ToDegrees(_state.LastClickedEntity.OrbitIcon._ellipseStartArcAngleRadians).ToString();
                                    ImGui.Text("StartAngleRadians: " + startRadian);
                                    ImGui.Text("StartAngleDegrees: " + startDegrees);

                                }
                            }

                            if (_state.LastClickedEntity.Entity.HasDataBlob<PropulsionDB>())
                            {
                                if (ImGui.CollapsingHeader("Propulsion: ###PropulsionHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    PropulsionDB propulsionDB = _state.LastClickedEntity.Entity.GetDataBlob<PropulsionDB>();
                                    ImGui.Text("NonNewt Engine Power: " + propulsionDB.TotalEnginePower);
                                    ImGui.Text("Max Speed: " + propulsionDB.MaximumSpeed_MS);
                                    ImGui.Text("CurrentVector: " + propulsionDB.CurrentVectorMS);
                                    ImGui.Text("Current Speed: " + Vector4.Magnitude( propulsionDB.CurrentVectorMS));
                                    if (_state.LastClickedEntity.Entity.HasDataBlob<CargoStorageDB>())
                                    {
                                        var fuelsGuid = propulsionDB.FuelUsePerKM;
                                        var storage = _state.LastClickedEntity.Entity.GetDataBlob<CargoStorageDB>();
                                        foreach (var fuelItemGuid in fuelsGuid.Keys)
                                        {
                                            var fuel = _state.Game.StaticData.GetICargoable(fuelItemGuid);
                                            ImGui.Text(fuel.Name);
                                            ImGui.SameLine();
                                            ImGui.Text(StorageSpaceProcessor.GetAmount(storage, fuel).ToString());
                                                 
                                        }

                                    }
                                }
                                if (_state.LastClickedEntity.Entity.HasDataBlob<TranslateMoveDB>())
                                {
                                    var db = _state.LastClickedEntity.Entity.GetDataBlob<TranslateMoveDB>();
                                    if (ImGui.CollapsingHeader("Transit: ###TransitHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                    {
                                        ImGui.Text("EntryPoint " + db.TranslateEntryPoint_AU);
                                        ImGui.Text("ExitPoint  " + db.TranslationExitPoint_AU);
                                        ImGui.Text("EDA " + db.PredictedExitTime.ToString());
                                        double distance = Distance.DistanceBetween(db.TranslateEntryPoint_AU, db.TranslationExitPoint_AU);
                                        ImGui.Text("Distance " + distance + " AU");
                                        ImGui.SameLine();
                                        double distancekm = Distance.AuToKm(distance);
                                        ImGui.Text(distancekm.ToString() + " KM");
                                        var timeToTarget = db.PredictedExitTime - _state.CurrentSystemDateTime;
                                        ImGui.Text("Remaining TTT " + timeToTarget);
                                        var totalTime = db.PredictedExitTime - db.EntryDateTime;
                                        ImGui.Text("Total TTT  " + totalTime);
                                        double speed = ((distancekm * 1000) / totalTime.TotalSeconds);
                                        ImGui.Text("speed2 " + speed);
                                        ImGui.Text("LastDateTime: ");
                                        ImGui.Text(db.LastProcessDateTime.ToString());
                                        ImGui.Text("Time Since Last: ");
                                        var timelen = _state.CurrentSystemDateTime - db.LastProcessDateTime;
                                        ImGui.Text(timelen.ToString());

                                    }

                                }

                            }


                        }
                    }
                }
                //else IsActive = false;
                ImGui.End();
            }
            _isRunningFrame = false;
            _dateChangeSinceLastFrame = false;
        }



    }
}
