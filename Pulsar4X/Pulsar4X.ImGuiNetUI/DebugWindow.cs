using System;
using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;

using Pulsar4X.ECSLib;
using Vector2 = System.Numerics.Vector2;

namespace Pulsar4X.SDL2UI
{
    public class DebugWindow :PulsarGuiWindow
    {
        EntityState _selectedEntityState;

        private Entity _selectedEntity;
        private Entity SelectedEntity
        {
            get { return _selectedEntity;}
            set
            {
                _selectedEntity = value;
                _selectedEntityName = SelectedEntity.GetDataBlob<NameDB>().GetName(_state.Faction);
                _selectedEntityState = _systemState.EntityStatesWithNames[_selectedEntity.Guid];
            }
        } 
        private string _selectedEntityName;
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
        bool _dateChangeSinceLastFrame = true;
        bool _isRunningFrame = false;
        bool _drawSOI = false;
        bool _drawParentSOI = false;
        //List<ECSLib.Vector4> positions = new List<ECSLib.Vector4>();

        List<(string name, Entity entity)> _factionOwnedEntites = new List<(string name, Entity entity)>();
        
        
        private DebugWindow() 
        {
              
        }
        internal static DebugWindow GetInstance()
        {
            DebugWindow instance;
            if (!_state.LoadedWindows.ContainsKey(typeof(DebugWindow)))
                instance = new DebugWindow();
            else
            {
                instance = (DebugWindow)_state.LoadedWindows[typeof(DebugWindow)];
                instance.RefreshFactionEntites();
                if(_state.LastClickedEntity?.Entity != null)
                    instance.SelectedEntity = _state.LastClickedEntity.Entity;
            }
            if(_state.LastClickedEntity?.Entity != null && instance.SelectedEntity != _state.LastClickedEntity.Entity)
                instance.SelectedEntity = _state.LastClickedEntity.Entity;
            instance._systemState = _state.StarSystemStates[_state.SelectedStarSysGuid];
            return instance;
        }



        internal void SetGameEvents()
        {
            if (_state.Game != null)
            {
                _state.Game.GameLoop.GameGlobalDateChangedEvent += GameLoop_GameGlobalDateChangedEvent;
                _state.SelectedSystem.ManagerSubpulses.SystemDateChangedEvent += SystemSubpulse_SystemDateChangedEvent;
                _state.EntityClickedEvent += _state_EntityClicked;
                
            }
        }

        private void _state_EntityClicked(EntityState entityState, MouseButtons btn)
        {
            if(btn == MouseButtons.Primary)
            {
                SelectedEntity = entityState.Entity;
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

            _gameRates[_gameRateIndex] = _currentGFPS;
            if (_gameRateIndex >= _gameRates.Length - 1)
                _gameRateIndex = 0;
            else
                _gameRateIndex++;

            //positions.Add(_state.LastClickedEntity.Entity.GetDataBlob<PositionDB>().AbsolutePosition_AU);
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

        void SystemSubpulse_SystemDateChangedEvent(DateTime newDate)
        {
            _dateChangeSinceLastFrame = true;
            _currentSFPS = (float)_state.Game.GameLoop.LastSubtickTime.TotalSeconds;
            _systemRates[_systemRateIndex] = _currentSFPS;
            if (_systemRateIndex >= _systemRates.Length - 1)
                _systemRateIndex = 0;
            else
                _systemRateIndex++;

            if (_systemState.EntitiesAdded.Count > 0 || _systemState.EntitysToBin.Count > 0)
            {
                RefreshFactionEntites();
            }

        }

        DateTime lastDate = new DateTime();
        ECSLib.Vector3 pos = new ECSLib.Vector3();
        double truAnomoly = 0;

        internal override void Display()
        {
            _isRunningFrame = true;
            if (IsActive)
            {
                SetFrameRateArray();
                if (ImGui.Begin("debug", ref IsActive))
                {
                    ImGui.Text(_state.PrimarySystemDateTime.ToString());
                    ImGui.Text("Cursor World Coordinate:");
                    var mouseWorldCoord = _state.Camera.MouseWorldCoordinate();
                    ImGui.Text("x" + mouseWorldCoord.X);
                    ImGui.SameLine();
                    ImGui.Text("y" + mouseWorldCoord.Y);

                    ImGui.Text("Special Chars");
                    //ImGui.Text("\u03A9, Ω, ω, ν");
                    if (ImGui.CollapsingHeader("FrameRates", ImGuiTreeNodeFlags.CollapsingHeader))
                    {

                        //plot vars: (label, values, valueOffset, overlayText, scaleMin, scaleMax, graphSize, Stride)
                        //core game processing rate.
                        //ImGui.PlotHistogram("##GRHistogram", _gameRatesDisplay, 10, _timeSpan.TotalSeconds.ToString(), 0, 1f, new ImVec2(0, 80), sizeof(float));
                        //ImGui.PlotHistogram("##GRHistogram1", _gameRatesDisplay, 0 , _timeSpan.TotalSeconds.ToString(), 0, 1f, new ImVec2(0, 80), sizeof(float));
                        //string label, ref float values... 
                        //ImGui.PlotHistogram(
                        ImGui.PlotHistogram("Game Tick ##GTHistogram", ref _gameRates[0], _gameRates.Length , _gameRateIndex, _currentGFPS.ToString(), 0f, largestGFPS, new Vector2(248, 60), sizeof(float));
                        ImGui.PlotLines("Game Tick ##GTPlotlines", ref _gameRates[0], _gameRates.Length, _gameRateIndex, _currentGFPS.ToString(), 0, largestGFPS, new Vector2(248, 60), sizeof(float));
                        //current star system processing rate. 
                        ImGui.PlotHistogram("System Tick ##STHistogram", ref _systemRates[0], _systemRates.Length, _systemRateIndex, _currentSFPS.ToString(), 0f, 1f, new Vector2(248, 60), sizeof(float));
                        ImGui.PlotLines("System Tick ##STPlotlines", ref _systemRates[0], _systemRates.Length, _systemRateIndex, _currentSFPS.ToString(), 0, 1, new Vector2(248, 60), sizeof(float));
                        //ui framerate
                        ImGui.PlotHistogram("Frame Rate ##FPSHistogram", ref _frameRates[0], _frameRates.Length, _frameRateIndex, _currentFPS.ToString(), 0f, 10000, new Vector2(248, 60), sizeof(float));

                        foreach (var item in _systemState.StarSystem.ManagerSubpulses.ProcessTime)
                        {
                            ImGui.Text(item.Key.Name);
                            ImGui.SameLine();
                            ImGui.Text(item.Value.ToString());
                        }

                    }

                    if (ImGui.CollapsingHeader("GraphicTests", ImGuiTreeNodeFlags.CollapsingHeader))
                    {
                        var window = GraphicDebugWindow.GetWindow(_state);
                        window.Display();
                        window.Enable(true, _state);
                    }

                    ImGui.Text("Selected Star System: " + _state.SelectedStarSysGuid);
                    ImGui.Text("Number Of Entites: " + _state.SelectedSystem.NumberOfEntites);
                    if(ImGui.CollapsingHeader("Log"))
                    {
                        ImGui.BeginChild("LogChild", new Vector2(800, 300), true);
                        ImGui.Columns(4, "Events", true);
                        ImGui.Text("DateTime");
                        ImGui.NextColumn();
                        ImGui.Text("Faction");
                        ImGui.NextColumn();
                        ImGui.Text("Entity");
                        ImGui.NextColumn();
                        ImGui.Text("Event Message");
                        ImGui.NextColumn();

                        foreach (var gameEvent in StaticRefLib.EventLog.GetAllEvents())
                        {

                            string entityStr = "";
                            if (gameEvent.Entity != null)
                                if (gameEvent.Entity.HasDataBlob<NameDB>())
                                    entityStr = gameEvent.Entity.GetDataBlob<NameDB>().DefaultName;
                                else
                                    entityStr = gameEvent.Entity.Guid.ToString();
                            string factionStr = "";
                            if (gameEvent.Faction != null)
                                if (gameEvent.Faction.HasDataBlob<NameDB>())
                                    factionStr = gameEvent.Faction.GetDataBlob<NameDB>().DefaultName;
                                else
                                    factionStr = gameEvent.Faction.Guid.ToString();

                            ImGui.Separator();
                            ImGui.Text(gameEvent.Time.ToString()); 
                                ImGui.NextColumn();
                            ImGui.Text(factionStr);
                                ImGui.NextColumn();
                            ImGui.Text(entityStr);
                                ImGui.NextColumn();
                            ImGui.TextWrapped(gameEvent.Message);

                                ImGui.NextColumn();
                               

                        }
                        //ImGui.Separator();
                        //ImGui.Columns();
                        ImGui.EndChild();
                    }
                    if (ImGui.CollapsingHeader("Entity List"))
                    {
                        for (int i = 0; i < _factionOwnedEntites.Count; i++)
                        {
                            if (ImGui.Selectable(_factionOwnedEntites[i].name + "##" + i))
                            {
                                SelectedEntity = _factionOwnedEntites[i].entity;
                            }
                        }
                    }
                    if (SelectedEntity != null && SelectedEntity.IsValid)
                    {
                        if (ImGui.CollapsingHeader("Selected Entity: " + _selectedEntityName + "###NameHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                        {
                            ImGui.Text(SelectedEntity.Guid.ToString());
                            if (SelectedEntity.HasDataBlob<PositionDB>())
                            {
                                var positiondb = SelectedEntity.GetDataBlob<PositionDB>();
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
                            if (SelectedEntity.HasDataBlob<MassVolumeDB>())
                            {
                                if (ImGui.CollapsingHeader("MassVolumeDB: ###MassVolDBHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    MassVolumeDB mvdb = SelectedEntity.GetDataBlob<MassVolumeDB>();
                                    ImGui.Text("Mass " + mvdb.Mass + "Kg");
                                    ImGui.Text("Volume " + mvdb.Volume + "Km^3");
                                    ImGui.Text("Density " + mvdb.Density + "g/cm^3");
                                    ImGui.Text("Radius " + mvdb.Radius + "Km");
                                }

                            }
                            if (SelectedEntity.HasDataBlob<OrbitDB>())
                            {

                                if (ImGui.Checkbox("Draw SOI", ref _drawSOI))
                                {
                                    SimpleCircle cir; 
                                    if (_drawSOI)
                                    {
                                        var soiradius = OrbitProcessor.GetSOI_AU(SelectedEntity);
                                        var colour = new SDL2.SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 100 };
                                        cir = new SimpleCircle(SelectedEntity.GetDataBlob<PositionDB>(), soiradius, colour);

                                        _state.SelectedSysMapRender.UIWidgets.Add(nameof(cir), cir);
                                    }
                                    else
                                        _state.SelectedSysMapRender.UIWidgets.Remove(nameof(cir));
                                }

                                if (ImGui.CollapsingHeader("OrbitDB: ###OrbitDBHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {

                                    OrbitDB orbitDB = SelectedEntity.GetDataBlob<OrbitDB>();

                                    //if (_state.CurrentSystemDateTime != lastDate)
                                    //{
                                    pos = OrbitProcessor.GetAbsolutePosition_AU(orbitDB, _state.PrimarySystemDateTime);
                                        truAnomoly = OrbitProcessor.GetTrueAnomaly(orbitDB, _state.PrimarySystemDateTime);
                                        lastDate = _state.PrimarySystemDateTime;
                                    //}

                                    ImGui.Text("x: " + pos.X);
                                    ImGui.Text("y: " + pos.Y);
                                    ImGui.Text("z: " + pos.Z);
                                    ImGui.Text("Eccentricity: " + orbitDB.Eccentricity);
                                    ImGui.Text("AoP:" + orbitDB.ArgumentOfPeriapsis_Degrees);
                                    ImGui.Text("TrueAnomaly: " + truAnomoly);
                                    ImGui.Text("MeanMotion: " + orbitDB.MeanMotion_DegreesSec + " in Deg/s");
                                    ImGui.Text("MeanVelocity: " + OrbitMath.MeanOrbitalVelocityInAU(orbitDB) + "Au/s");
                                    ImGui.Text("MeanVelocity: " + Distance.AuToKm( OrbitMath.MeanOrbitalVelocityInAU(orbitDB)) + "Km/s");
                                    ImGui.Text("SOI Radius: " + Distance.AuToKm(OrbitProcessor.GetSOI_AU(_state.LastClickedEntity.Entity)));
                                    ImGui.Text("Orbital Period:" + orbitDB.OrbitalPeriod);
                                    ImGui.Text("SemiMajAxis: " + orbitDB.SemiMajorAxisAU);
                                    ImGui.Text("Periapsis: " + Distance.AuToKm(orbitDB.Periapsis_AU).ToString("g3") + " Km");
                                    ImGui.Text("Appoapsis: " + Distance.AuToKm(orbitDB.Apoapsis_AU).ToString("g3") + " Km");
                                    if (orbitDB.Parent != null)
                                        ImGui.Text("Parent: " + orbitDB.Parent.GetDataBlob<NameDB>().DefaultName);
                                    if (orbitDB.Children.Count > 0)
                                    {
                                        foreach (var item in orbitDB.Children)
                                        {
                                            ImGui.Text(item.GetDataBlob<NameDB>().DefaultName);
                                        }

                                    }
                                                
                                }
                            }

                            if (SelectedEntity.HasDataBlob< NewtonMoveDB>())
                            {
                                if (ImGui.Checkbox("Draw Parent SOI", ref _drawParentSOI))
                                {
                                    SimpleCircle psoi;
                                    SimpleLine psoilin;
                                    if (_drawParentSOI)
                                    {
                                        var myPos = SelectedEntity.GetDataBlob<PositionDB>();
                                        var parent = myPos.Parent;
                                        var pObt = parent.GetDataBlob<OrbitDB>();
                                        var cnmve = SelectedEntity.GetDataBlob<NewtonMoveDB>();

                                        var soiradius = OrbitProcessor.GetSOI_AU(parent);
                                        var colour = new SDL2.SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 100 };
                                        psoi = new SimpleCircle(parent.GetDataBlob<PositionDB>(), soiradius, colour);
                                        var pmass = parent.GetDataBlob<MassVolumeDB>().Mass;
                                        var mymass = SelectedEntity.GetDataBlob<MassVolumeDB>().Mass;

                                        var sgp = GameConstants.Science.GravitationalConstant * (pmass + mymass) / 3.347928976e33;
                                        var vel = Distance.KmToAU(cnmve.CurrentVector_ms);
                                        var cpos = myPos.RelativePosition_AU;
                                        var eccentVector = OrbitMath.EccentricityVector(sgp, cpos, vel);
                                        double ce = eccentVector.Length();
                                        var r = cpos.Length();
                                        var v = vel.Length();

                                        var ca = 1 / (2 / r - Math.Pow(v, 2) / sgp);
                                        var cp = EllipseMath.SemiLatusRectum(ca, ce);

                                        var cAoP = Math.Atan2(eccentVector.Y, eccentVector.X);

                                        /*
                                        var pa = pObt.SemiMajorAxis;
                                        var pe = pObt.Eccentricity;
                                        var pp = EllipseMath.SemiLatusRectum(pa, pe);
                                        */
                                        double θ = OrbitMath.AngleAtRadus(soiradius, cp, ce);
                                        θ += cAoP;

                                        var x = soiradius * Math.Cos(θ);
                                        var y = soiradius * Math.Sin(θ);
                                        psoilin = new SimpleLine(parent.GetDataBlob<PositionDB>(), new PointD() { X = x, Y = y }, colour);

                                        _state.SelectedSysMapRender.UIWidgets.Add(nameof(psoi), psoi);
                                        _state.SelectedSysMapRender.UIWidgets.Add(nameof(psoilin), psoilin);
                                    }
                                    else
                                    {
                                        _state.SelectedSysMapRender.UIWidgets.Remove(nameof(psoi));
                                        _state.SelectedSysMapRender.UIWidgets.Remove(nameof(psoilin));
                                    }
                                }

                            }

                            if (_selectedEntityState.OrbitIcon != null)
                            {

                                if (ImGui.CollapsingHeader("OrbitIcon: ###OrbitIconHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    OrbitDB orbitDB = SelectedEntity.GetDataBlob<OrbitDB>();

                                    //string startRadian = _state.LastClickedEntity.OrbitIcon._ellipseStartArcAngleRadians.ToString();
                                    //string startDegrees = Angle.ToDegrees(_state.LastClickedEntity.OrbitIcon._ellipseStartArcAngleRadians).ToString();
                                    //ImGui.Text("StartAngleRadians: " + startRadian);
                                    //ImGui.Text("StartAngleDegrees: " + startDegrees);
                                    if (ImGui.CollapsingHeader("OrbitIconLines", ImGuiTreeNodeFlags.CollapsingHeader))
                                    {
                                        var window = OrbitalDebugWindow.GetWindow(_state.LastClickedEntity);
                                        window.Display();
                                        window.Enable(true, _state);
                                    }
                                }

                            }

                            if (SelectedEntity.HasDataBlob<PropulsionAbilityDB>())
                            {
                                if (ImGui.CollapsingHeader("Propulsion: ###PropulsionHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    PropulsionAbilityDB propulsionDB = SelectedEntity.GetDataBlob<PropulsionAbilityDB>();
                                    ImGui.Text("NonNewt Engine Power: " + propulsionDB.TotalEnginePower);
                                    ImGui.Text("Max Speed: " + propulsionDB.MaximumSpeed_MS);
                                    ImGui.Text("CurrentVector: " + propulsionDB.CurrentVectorMS);
                                    ImGui.Text("Current Speed: " + ECSLib.Vector3.Magnitude( propulsionDB.CurrentVectorMS));
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
                                if (SelectedEntity.HasDataBlob<TranslateMoveDB>())
                                {
                                    var db = _state.LastClickedEntity.Entity.GetDataBlob<TranslateMoveDB>();
                                    if (ImGui.CollapsingHeader("Transit: ###TransitHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                    {
                                        ImGui.Text("EntryPoint " + db.TranslateEntryPoint_AU);
                                        ImGui.Text("ExitPoint  " + db.TranslateExitPoint_AU);
                                        ImGui.Text("EDA " + db.PredictedExitTime.ToString());
                                        double distance = Distance.DistanceBetween(db.TranslateEntryPoint_AU, db.TranslateExitPoint_AU);
                                        ImGui.Text("Distance " + distance + " AU");
                                        ImGui.SameLine();
                                        double distancekm = Distance.AuToKm(distance);
                                        ImGui.Text(distancekm.ToString() + " KM");
                                        var timeToTarget = db.PredictedExitTime - _state.PrimarySystemDateTime;
                                        ImGui.Text("Remaining TTT " + timeToTarget);
                                        var totalTime = db.PredictedExitTime - db.EntryDateTime;
                                        ImGui.Text("Total TTT  " + totalTime);
                                        double speed = ((distancekm * 1000) / totalTime.TotalSeconds);
                                        ImGui.Text("speed2 " + speed);
                                        ImGui.Text("LastDateTime: ");
                                        ImGui.Text(db.LastProcessDateTime.ToString());
                                        ImGui.Text("Time Since Last: ");
                                        var timelen = _state.PrimarySystemDateTime - db.LastProcessDateTime;
                                        ImGui.Text(timelen.ToString());

                                    }

                                }

                            }
                            if (SelectedEntity.HasDataBlob<SensorInfoDB>())
                            {
                                var actualEntity = SelectedEntity.GetDataBlob<SensorInfoDB>().DetectedEntity;
                                if (actualEntity.IsValid && actualEntity.HasDataBlob<AsteroidDamageDB>())
                                {
                                    var dmgDB = actualEntity.GetDataBlob<AsteroidDamageDB>();
                                    ImGui.Text("Remaining HP: " + dmgDB.Health.ToString());
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

        void RefreshFactionEntites()
        {
            _factionOwnedEntites = new List<(string name, Entity entity)>();
            foreach (var entity in _state.SelectedSystem.GetEntitiesByFaction(_state.Faction.Guid))
            {
                string name = entity.GetDataBlob<NameDB>().GetName(_state.Faction);
                _factionOwnedEntites.Add((name, entity));

            }

            
        }



    }
}
