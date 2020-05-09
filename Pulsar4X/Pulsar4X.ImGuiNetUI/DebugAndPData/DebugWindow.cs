using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;
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
                if (_selectedEntity != value)
                {
                    _selectedEntity = value;
                    _selectedEntityName = SelectedEntity.GetDataBlob<NameDB>().GetName(_uiState.Faction);
                    _selectedEntityState = _systemState.EntityStatesWithNames[_selectedEntity.Guid];
                    OnSelectedEntityChanged();
                }
            }
        } 
        
        private string _selectedEntityName;
        
        SensorReceverAtbDB _selectedReceverAtb;
        
        SystemState _systemState;
        public SystemState systemState{get{return _systemState;} set{_systemState = value;}}

        bool _dateChangeSinceLastFrame = true;
        bool _isRunningFrame = false;
        bool _drawSOI = false;
        bool _drawParentSOI = false;
        //List<ECSLib.Vector4> positions = new List<ECSLib.Vector4>();

        private IntPtr _dmgTxtr;
        
        List<(string name, Entity entity)> _factionOwnedEntites = new List<(string name, Entity entity)>();
        
        private List<(string name, int count)> _listfoo = new List<(string, int)>()
        {
            ("Item1", 5),
            ("Item2", 8),
            ("Item3", 9),
            ("Item4", 3),
            ("Item5", 1)
            
        };

        private bool _showSizesDemo = false;
        
        private DebugWindow() 
        {
              
        }
        internal static DebugWindow GetInstance()
        {
            DebugWindow instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(DebugWindow)))
                instance = new DebugWindow();
            else
            {
                instance = (DebugWindow)_uiState.LoadedWindows[typeof(DebugWindow)];
                instance.RefreshFactionEntites();
                if(_uiState.LastClickedEntity?.Entity != null)
                    instance.SelectedEntity = _uiState.LastClickedEntity.Entity;
            }
            if(_uiState.LastClickedEntity?.Entity != null && instance.SelectedEntity != _uiState.LastClickedEntity.Entity)
                instance.SelectedEntity = _uiState.LastClickedEntity.Entity;
            instance._systemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
            return instance;
        }



        internal void SetGameEvents()
        {
            if (_uiState.Game != null)
            {
                _uiState.EntityClickedEvent += UIStateEntityClicked;
            }
        }

        private void OnSelectedEntityChanged()
        {
            
            if (SelectedEntity.HasDataBlob<EntityDamageProfileDB>())
            {
                var dmgdb = SelectedEntity.GetDataBlob<EntityDamageProfileDB>();
                _dmgTxtr = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, dmgdb.DamageProfile);
            }
            else if(_selectedEntity.HasDataBlob<SensorInfoDB>())
            {
                
                var actualEntity = SelectedEntity.GetDataBlob<SensorInfoDB>().DetectedEntity;
                if (actualEntity.IsValid && actualEntity.HasDataBlob<EntityDamageProfileDB>())
                {
                    var dmgdb = SelectedEntity.GetDataBlob<EntityDamageProfileDB>();
                    _dmgTxtr = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, dmgdb.DamageProfile);
                }
            }
            else
                _dmgTxtr = IntPtr.Zero;
            
        }

        private void UIStateEntityClicked(EntityState entityState, MouseButtons btn)
        {
            if(btn == MouseButtons.Primary)
            {
                SelectedEntity = entityState.Entity;
            }
        }


        


        ECSLib.Vector3 pos = new ECSLib.Vector3();
        double truAnomoly = 0;

        internal override void Display()
        {
            _isRunningFrame = true;
            if (IsActive)
            {
                if (ImGui.Begin("debug", ref IsActive))
                {


                    BorderGroup.Begin("Info", ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));
                    
                    ImGui.Text(_uiState.PrimarySystemDateTime.ToString());
                    ImGui.Text("GitHash: " + AssemblyInfo.GetGitHash());
                    
                    BorderGroup.End();
                    ImGui.NewLine();
                    
                    if (ImGui.CollapsingHeader("Camera Functions", ImGuiTreeNodeFlags.CollapsingHeader))
                    {
                        if (_uiState.Camera.IsPinnedToEntity)
                        {
                            var entyName = _systemState.EntityStatesWithNames[_uiState.Camera.PinnedEntityGuid].Name;
                            ImGui.Text("Camera is pinned to:");
                            ImGui.SameLine();
                            ImGui.Text(entyName);
                        }
                        else
                        {
                            ImGui.Text("Camera is not pinned to an entity.");
                        }
                        
                        ImGui.Text("Zoom: " + _uiState.Camera.ZoomLevel);

                        ImGui.Text("Raw Cursor Coordinate");
                        Vector2 mouseCoord = ImGui.GetMousePos();
                        ImGui.Text("x: " + mouseCoord.X);
                        ImGui.SameLine();
                        ImGui.Text("y: " + mouseCoord.Y);
                        
                        ImGui.Text("Cursor World Coordinate:");
                        var mouseWorldCoord = _uiState.Camera.MouseWorldCoordinate_m();
                        ImGui.Text("x" + Stringify.Distance(mouseWorldCoord.X));
                        ImGui.SameLine();
                        ImGui.Text("y" + Stringify.Distance(mouseWorldCoord.Y));
                        var mouseWorldCoord_AU = _uiState.Camera.MouseWorldCoordinate_AU();
                        ImGui.Text("x" + mouseWorldCoord_AU.X + " AU");
                        ImGui.SameLine();
                        ImGui.Text("y" + mouseWorldCoord_AU.Y + " AU");

                        ImGui.Text("Cursor View Coordinate:");
                        var mouseViewCoord = _uiState.Camera.ViewCoordinate_m(mouseWorldCoord);
                        ImGui.Text("x" + mouseViewCoord.x + " p");
                        ImGui.SameLine();
                        ImGui.Text("y" + mouseViewCoord.y + " p");
                        var mouseviewCoord_AU = _uiState.Camera.ViewCoordinate_AU(mouseWorldCoord_AU);
                        ImGui.Text("x" + mouseviewCoord_AU.x + " p");
                        ImGui.SameLine();
                        ImGui.Text("y" + mouseviewCoord_AU.y + " p");
                    
                        ImGui.Text("Camrera WorldPosition");
                        var camWorldCoord_m = _uiState.Camera.CameraWorldPosition_m;
                        ImGui.Text("x" + camWorldCoord_m.X + " m");
                        ImGui.SameLine();
                        ImGui.Text("y" + camWorldCoord_m.Y + " m");
                        var camWorldCoord_AU = _uiState.Camera.CameraWorldPosition_AU;
                        ImGui.Text("x" + camWorldCoord_AU.X + " AU");
                        ImGui.SameLine();
                        ImGui.Text("y" + camWorldCoord_AU.Y + " AU");
                        
                    }


                    
                    
                    ImGui.Text("Special Chars");
                    ImGui.Text("Proggy clean is crsp but these chars are blury, Ω, ω, ν");
                    //ImGui.Text("this text is fine, Ω, ω, ν "+"this text is not blury");


                    if (ImGui.CollapsingHeader("GraphicTests", ImGuiTreeNodeFlags.CollapsingHeader))
                    {
                        var window = GraphicDebugWindow.GetWindow(_uiState);
                        window.Display();
                        window.Enable(true, _uiState);
                    }


                    ImGui.Checkbox("Sizes Demo", ref _showSizesDemo);
                    if(_showSizesDemo)
                        SizesDemo.Display();

                    if (ImGui.CollapsingHeader("UI Examples"))
                    {
                        ImGui.Text("ReOrderable List Exampeles");

                        ImGui.Text("HoverButtons");
                        HoverButtons();

                        ImGui.Text("Static Buttons");
                        StaticButtons();
                
                        ImGui.Text("Buttons Group");
                        ButtonBox();
                        
                        ImGui.NewLine();
                        //ImGui.Text("BorderListOptionsWidget");
                        BorderListOptionsWiget();
                        
                        ImGui.NewLine();
                    }
                    
                    ImGui.Text("Selected Star System: " + _uiState.SelectedStarSysGuid);
                    ImGui.Text("Number Of Entites: " + _uiState.SelectedSystem.NumberOfEntites);
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
                            
                            if (ImGui.CollapsingHeader("DataBlob List"))
                            {
                                EntityInspector.Display(_selectedEntity);
                                ImGui.NewLine();
                            }


                            
                            

                            if (SelectedEntity.HasDataBlob<MassVolumeDB>())
                            {
                                if (ImGui.CollapsingHeader("MassVolumeDB: ###MassVolDBHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    MassVolumeDB mvdb = SelectedEntity.GetDataBlob<MassVolumeDB>();
                                    ImGui.Text("Mass " + Stringify.Mass(mvdb.Mass));
                                    ImGui.Text("Volume " + Stringify.Velocity(mvdb.Volume_m3));
                                    ImGui.Text("Density " + mvdb.Density + "g/cm^3");
                                    ImGui.Text("Radius " + Stringify.Distance(mvdb.RadiusInM));
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

                                        _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(cir), cir);
                                    }
                                    else
                                        _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(cir));
                                }

                                if (ImGui.CollapsingHeader("OrbitDB: ###OrbitDBHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {

                                    OrbitDB orbitDB = SelectedEntity.GetDataBlob<OrbitDB>();

                                    //if (_uiState.CurrentSystemDateTime != lastDate)
                                    //{
                                    pos = OrbitProcessor.GetAbsolutePosition_AU(orbitDB, _uiState.PrimarySystemDateTime);
                                        truAnomoly = OrbitProcessor.GetTrueAnomaly(orbitDB, _uiState.PrimarySystemDateTime);
                                        //lastDate = _uiState.PrimarySystemDateTime;
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
                                    ImGui.Text("SOI Radius: " + Distance.AuToKm(OrbitProcessor.GetSOI_AU(_uiState.LastClickedEntity.Entity)));
                                    ImGui.Text("Orbital Period:" + orbitDB.OrbitalPeriod);
                                    ImGui.Text("SemiMajAxis: " + orbitDB.SemiMajorAxis_AU);
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

                                        _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(psoi), psoi);
                                        _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(psoilin), psoilin);
                                    }
                                    else
                                    {
                                        _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(psoi));
                                        _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(psoilin));
                                    }
                                }

                            }
                            
                            if (_selectedEntityState.OrbitIcon != null)
                            {

                                if (ImGui.CollapsingHeader("OrbitIcon: ###OrbitIconHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    OrbitDB orbitDB = SelectedEntity.GetDataBlob<OrbitDB>();
                                    if (orbitDB != null)
                                    {
                                        //string startRadian = _uiState.LastClickedEntity.OrbitIcon._ellipseStartArcAngleRadians.ToString();
                                        //string startDegrees = Angle.ToDegrees(_uiState.LastClickedEntity.OrbitIcon._ellipseStartArcAngleRadians).ToString();
                                        //ImGui.Text("StartAngleRadians: " + startRadian);

                                        //ImGui.Text("StartAngleDegrees: " + startDegrees);
                                        if (ImGui.CollapsingHeader("OrbitIconLines", ImGuiTreeNodeFlags.CollapsingHeader))
                                        {
                                            var window = OrbitalDebugWindow.GetWindow(_uiState.LastClickedEntity);
                                            window.Display();
                                            window.Enable(true, _uiState);
                                        }
                                    }
                                }

                            }

                            if (SelectedEntity.HasDataBlob<EnergyGenAbilityDB>())
                            {
                                if (ImGui.CollapsingHeader("Power ###PowerHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    var powerDB = _selectedEntity.GetDataBlob<EnergyGenAbilityDB>();
                                    ImGui.Text("Generates " +powerDB.EnergyType.Name); 
                                    ImGui.Text("Max of: " + powerDB.TotalOutputMax + "/s");
                                    string fueltype = StaticRefLib.StaticData.CargoGoods.GetMaterial(powerDB.TotalFuelUseAtMax.type).Name;
                                    ImGui.Text("Burning " + powerDB.TotalFuelUseAtMax.maxUse + " of "  +  fueltype); 
                                    ImGui.Text("With " + powerDB.LocalFuel + " remaining reactor fuel");
                                    
                                    foreach (var etype in powerDB.EnergyStored)
                                    {
                                        string etypename = StaticRefLib.StaticData.CargoGoods.GetMaterial(etype.Key).Name;
                                        ImGui.Text(etypename);
                                        
                                        ImGui.Text(etype.Value.ToString() + "/" + powerDB.EnergyStoreMax[etype.Key].ToString());
                                    }
                                    
                                }
                            }
                                

                            if (SelectedEntity.HasDataBlob<WarpAbilityDB>())
                            {
                                if (ImGui.CollapsingHeader("Warp: ###WarpHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    WarpAbilityDB warpDB = SelectedEntity.GetDataBlob<WarpAbilityDB>();
  
                                    ImGui.Text("Max Speed: " + warpDB.MaxSpeed);
                                    ImGui.Text("CurrentVector: " + warpDB.CurrentVectorMS);
                                    ImGui.Text("Current Speed: " + ECSLib.Vector3.Magnitude( warpDB.CurrentVectorMS));
                                    
                                    
                                    //ImGui.Text("Energy type: " + warpDB.EnergyType);
                                    ImGui.Text( StaticRefLib.StaticData.CargoGoods.GetMaterial(warpDB.EnergyType).Name);
                                    
                                    ImGui.Text("Creation Cost: " +warpDB.BubbleCreationCost.ToString());
                                    ImGui.Text("Sustain Cost: " +warpDB.BubbleSustainCost.ToString());
                                    ImGui.Text("Collapse Cost: " +warpDB.BubbleCollapseCost.ToString());
                                    
                                }


                            }
                            if (SelectedEntity.HasDataBlob<NewtonMoveDB>())
                            {
                                var nmdb = _uiState.LastClickedEntity.Entity.GetDataBlob<NewtonMoveDB>();
                                var ntdb = _uiState.LastClickedEntity.Entity.GetDataBlob<NewtonThrustAbilityDB>();
                                if (ImGui.CollapsingHeader("NewtonMove: ###NewtHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    ImGui.Text("Manuver DV:" + Stringify.Distance(nmdb.DeltaVForManuver_m.Length())+"/s");
                                    ImGui.Text("Parent Body: " + nmdb.SOIParent.GetDataBlob<NameDB>().DefaultName);
                                    ImGui.Text("Current Vector:");
                                    ImGui.Text("X:" + Stringify.Distance(nmdb.CurrentVector_ms.X)+"/s");
                                    ImGui.Text("Y:" + Stringify.Distance(nmdb.CurrentVector_ms.Y)+"/s");
                                    ImGui.Text("Z:" + Stringify.Distance(nmdb.CurrentVector_ms.Z)+"/s");
                                    
                                    ImGui.Text("Remaining Dv:" + Stringify.Distance( ntdb.DeltaV) + "/s");
                                    ImGui.Text("Exhaust Velocity: " + ntdb.ExhaustVelocity);
                                    ImGui.Text("BurnRate: " + ntdb.FuelBurnRate);
                                    ImGui.Text("Thrust: " + ntdb.ThrustInNewtons);
                                    

                                }

                            }
                            
                            if (SelectedEntity.HasDataBlob<WarpMovingDB>())
                            {
                                var db = _uiState.LastClickedEntity.Entity.GetDataBlob<WarpMovingDB>();
                                if (ImGui.CollapsingHeader("Transit: ###TransitHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                                {
                                    ImGui.Text("EntryPoint: ");
                                    ImGui.Text("X:" + Stringify.Distance(db.EntryPointAbsolute.X));
                                    ImGui.Text("Y:" + Stringify.Distance(db.EntryPointAbsolute.Y));
                                    ImGui.Text("Z:" + Stringify.Distance(db.EntryPointAbsolute.Z));
                                    
                                    
                                    ImGui.Text("ExitPoint: ");
                                    ImGui.Text("X:" + Stringify.Distance(db.ExitPointAbsolute.X));
                                    ImGui.Text("Y:" + Stringify.Distance(db.ExitPointAbsolute.Y));
                                    ImGui.Text("Z:" + Stringify.Distance(db.ExitPointAbsolute.Z));
                                    
                                    ImGui.Text("Relitive ExitPoint: ");
                                    ImGui.Text("X:" + Stringify.Distance(db.ExitPointRalitive.X));
                                    ImGui.Text("Y:" + Stringify.Distance(db.ExitPointRalitive.Y));
                                    ImGui.Text("Z:" + Stringify.Distance(db.ExitPointRalitive.Z));
                                    
                                    
                                    ImGui.Text("EDA " + db.PredictedExitTime.ToString());
                                    double distance = Distance.DistanceBetween(db.EntryPointAbsolute, db.ExitPointAbsolute);
                                    ImGui.Text("Distance " + Stringify.Distance(distance));
                                    ImGui.SameLine();
                                    var timeToTarget = db.PredictedExitTime - _uiState.PrimarySystemDateTime;
                                    ImGui.Text("Remaining TTT " + timeToTarget);
                                    var totalTime = db.PredictedExitTime - db.EntryDateTime;
                                    ImGui.Text("Total TTT  " + totalTime);
                                    double speed = ((distance) / totalTime.TotalSeconds);
                                    ImGui.Text("speed2 " + speed);
                                    ImGui.Text("LastDateTime: ");
                                    ImGui.Text(db.LastProcessDateTime.ToString());
                                    ImGui.Text("Time Since Last: ");
                                    var timelen = _uiState.PrimarySystemDateTime - db.LastProcessDateTime;
                                    ImGui.Text(timelen.ToString());

                                }
                            }

                            if (_selectedEntity.HasDataBlob<SensorProfileDB>() && ImGui.CollapsingHeader("SensorProfile"))
                            {
                                var profile = _selectedEntity.GetDataBlob<SensorProfileDB>();
                                ImGui.Text("Target CrossSection: " + profile.TargetCrossSection_msq + " m^2");
                                ImGui.Text("Emitted Count: " + profile.EmittedEMSpectra.Count);
                                ImGui.Text("Reflected Count: " + profile.ReflectedEMSpectra.Count);

                                double highestMagnatude = 0;
                                double atWavelength = 0;
                                foreach (var kvp in profile.EmittedEMSpectra)
                                {
                                    if (kvp.Value > highestMagnatude)
                                    {
                                        highestMagnatude = kvp.Value;
                                        atWavelength = kvp.Key.WavelengthAverage_nm;
                                    }
                                }
                                
                                ImGui.Text("Highest Emitted Signal: " + highestMagnatude + " kw");
                                ImGui.Text("at wavelength : " + atWavelength + " nm");
                                
                                highestMagnatude = 0;
                                atWavelength = 0;
                                foreach (var kvp in profile.ReflectedEMSpectra)
                                {
                                    if (kvp.Value > highestMagnatude)
                                    {
                                        highestMagnatude = kvp.Value;
                                        atWavelength = kvp.Key.WavelengthAverage_nm;
                                    }
                                }
                                
                                ImGui.Text("Highest Reflected Signal: " + highestMagnatude + " kw");
                                ImGui.Text("at wavelength : " + atWavelength + " nm");
                                
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
                            if (_dmgTxtr != IntPtr.Zero)
                            {
                                if (ImGui.CollapsingHeader("DamageProfile"))
                                {
                                    if (ImGui.ImageButton(_dmgTxtr, new Vector2(64, 64)))
                                    {
                                        //show a full sized scrollable image. 
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

        void RefreshFactionEntites()
        {
            _factionOwnedEntites = new List<(string name, Entity entity)>();
            foreach (var entity in _uiState.SelectedSystem.GetEntitiesByFaction(_uiState.Faction.Guid))
            {
                string name = entity.GetDataBlob<NameDB>().GetName(_uiState.Faction);
                _factionOwnedEntites.Add((name, entity));

            }

            
        }
        
                private int _hvSelectedIndex = -1;
        void HoverButtons()
        {
            
            ImGui.BeginChild("Hover Buttons");

            int loopto = _listfoo.Count;
            if (_hvSelectedIndex >= _listfoo.Count)
                _hvSelectedIndex = -1;
            if (_hvSelectedIndex > -1)
                loopto = _hvSelectedIndex;


            float heightt = ImGui.GetTextLineHeightWithSpacing() * loopto;

            var spacingH = ImGui.GetTextLineHeightWithSpacing() - ImGui.GetTextLineHeight();

            float hoverHeigt = ImGui.GetTextLineHeightWithSpacing() + spacingH * 3;

            float heightb = ImGui.GetTextLineHeightWithSpacing() * (_listfoo.Count - loopto - 1);
            float colomnWidth0 = 300;

            for (int i = 0; i < loopto; i++)
            {
                ImGui.BeginChild("TopItems", new Vector2(400, heightt));
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 300);

                ImGui.BeginGroup();
                var cpos = ImGui.GetCursorPos();
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ChildBg));
                ImGui.Button("##ht"+i, new Vector2(colomnWidth0 - spacingH, ImGui.GetTextLineHeightWithSpacing()));
                ImGui.PopStyleColor();
                ImGui.SetCursorPos(cpos);
                ImGui.Text(_listfoo[i].name);
                ImGui.EndGroup();

                if (ImGui.IsItemHovered())
                {
                    _hvSelectedIndex = i;
                }

                ImGui.NextColumn();
                ImGui.NextColumn();


                ImGui.EndChild();
            }


            if (_hvSelectedIndex > -1)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 0.5f);
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 2f);
                ImGui.BeginChild("Buttons", new Vector2(400, hoverHeigt), true);
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 300);

                var queueItem = _listfoo[_hvSelectedIndex];

                ImGui.BeginGroup();
                ImGui.Text(_listfoo[_hvSelectedIndex].name);
                ImGui.EndGroup();

                ImGui.NextColumn();

                ImGui.BeginGroup();

                if (ImGui.SmallButton("^" + "##hv" + _hvSelectedIndex) && _hvSelectedIndex > 0)
                {
                    _listfoo.RemoveAt(_hvSelectedIndex);
                    _listfoo.Insert(_hvSelectedIndex - 1, queueItem);
                }

                ImGui.SameLine();
                if (ImGui.SmallButton("v" + "##hv" + _hvSelectedIndex) && _hvSelectedIndex < _listfoo.Count - 1)
                {

                    _listfoo.RemoveAt(_hvSelectedIndex);
                    _listfoo.Insert(_hvSelectedIndex + 1, queueItem);
                }

                ImGui.SameLine();
                if (ImGui.SmallButton("x" + "##hv" + _hvSelectedIndex))
                {
                    _listfoo.RemoveAt(_hvSelectedIndex);
                }

                ImGui.EndGroup();
                if (ImGui.IsItemHovered())
                {
                    _hvSelectedIndex = _hvSelectedIndex;
                }

                ImGui.NextColumn();

                ImGui.EndChild();
                ImGui.PopStyleVar(2);


                for (int i = _hvSelectedIndex + 1; i < _listfoo.Count; i++)
                {
                    ImGui.BeginChild("Bottom", new Vector2(400, heightb));
                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, 300);

                    ImGui.BeginGroup();
                    var cpos = ImGui.GetCursorPos();
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ChildBg));
                    ImGui.Button("##hb" + i, new Vector2(colomnWidth0 - spacingH, ImGui.GetTextLineHeightWithSpacing()));
                    ImGui.PopStyleColor();
                    ImGui.SetCursorPos(cpos);
                    ImGui.Text(_listfoo[i].name);
                    ImGui.EndGroup();

                    if (ImGui.IsItemHovered())
                    {
                        _hvSelectedIndex = i;
                    }

                    ImGui.NextColumn();
                    ImGui.NextColumn();

                    ImGui.EndChild();
                }
                
            }
        }

        void StaticButtons()
        {
                int selectedItem = -1;
                for (int i = 0; i < _listfoo.Count; i++)
                {
                    string name = _listfoo[i].name;
                    int number = _listfoo[i].count;
                    
                    /*
                    if (ImGui.Selectable(name, selectedItem == i, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        selectedItem = i;
                    }
                    */
                    ImGui.Text(name);
                    
                    bool hovered = ImGui.IsItemHovered();
                    if (hovered)
                        selectedItem = i;
                    
                    ImGui.NextColumn();
                    ImGui.Text(number.ToString());
                    

                    ImGui.SameLine();
                    if (ImGui.SmallButton("+##sb" + i)) //todo: imagebutton
                    {
                        _listfoo[i] = (name, _listfoo[i].count + 1);
                        
                    }
                    ImGui.SameLine();
                    if (ImGui.SmallButton("-##sb" + i) && number > 0) //todo: imagebutton
                    {
                        _listfoo[i] = (name, _listfoo[i].count - 1);
                        
                    }
                    ImGui.SameLine();
                    if (ImGui.SmallButton("x##sb" + i)) //todo: imagebutton
                    {
                        _listfoo.RemoveAt(i);
                        
                    }

                    if (i > 0)
                    {
                        ImGui.SameLine();
                        if (ImGui.SmallButton("^##sb" + i)) //todo: imagebutton
                        {

                            (string name, int count) item = _listfoo[i];
                            _listfoo.RemoveAt(i);
                            _listfoo.Insert(i - 1, item);


                        }
                    }


                    if (_listfoo.Count <= i)
                    {
                        ImGui.SameLine();
                        if (ImGui.SmallButton("v##sb" + i)) //todo: imagebutton
                        {
                            (string name, int count) item = _listfoo[i];
                            _listfoo.RemoveAt(i);
                            _listfoo.Insert(i + 1, item);
                        }
                    }
                        
                    ImGui.NextColumn();
                    
                }
        }
        
        private (string name, int count) _bbselectedItem;
        private int _bbSelectedIndex = -1;
        void ButtonBox()
        {
            
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
            ImGui.BeginChild("ButtonBoxList", new Vector2(280, 100), true, ImGuiWindowFlags.ChildWindow);
            ImGui.Columns(2);
            for (int i = 0; i < _listfoo.Count; i++)
            {
                
                bool selected = _bbSelectedIndex == i;

                if (ImGui.Selectable(_listfoo[i].name, ref selected))
                {
                    _bbselectedItem = _listfoo[i];
                    _bbSelectedIndex = i;
                }
                
            }
            

            ImGui.EndChild();
            ImGui.SameLine();

            ImGui.BeginChild("Buttons##bb", new Vector2(116, 100), true, ImGuiWindowFlags.ChildWindow);
            ImGui.BeginGroup();
            //if (ImGui.ImageButton(_uiState.SDLImageDictionary["UpImg"], new Vector2(16, 8)))
            if (ImGui.Button("^" + "##bb" + _bbSelectedIndex))
            {
                (string name, int count) item = _listfoo[_bbSelectedIndex];
                _listfoo.RemoveAt(_bbSelectedIndex);
                _listfoo.Insert(_bbSelectedIndex - 1, item);
                _bbSelectedIndex--;
            }
            //if (ImGui.ImageButton(_uiState.SDLImageDictionary["DnImg"], new Vector2(16, 8)))
            if (ImGui.Button("v" + "##bb" + _bbSelectedIndex))
            {
                (string name, int count) item = _listfoo[_bbSelectedIndex];
                _listfoo.RemoveAt(_bbSelectedIndex);
                _listfoo.Insert(_bbSelectedIndex + 1, item);
                _bbSelectedIndex++;
            }
            ImGui.EndGroup();
            ImGui.SameLine();
            //if (ImGui.ImageButton(_uiState.SDLImageDictionary["RepeatImg"], new Vector2(16, 16)))
            if (ImGui.Button("+" + "##bb" + _bbSelectedIndex))
            {
                //_refineryVM.CurrentJobSelectedItem.ChangeRepeat(!_refineryVM.CurrentJobSelectedItem.Repeat);
                _listfoo[_bbSelectedIndex] = (_bbselectedItem.name, _bbselectedItem.count + 1);
            }
        
            ImGui.SameLine();
            //if (ImGui.ImageButton(_uiState.SDLImageDictionary["CancelImg"], new Vector2(16, 16)))
            if (ImGui.Button("-" + "##bb" + _bbSelectedIndex))
            {
                //_refineryVM.CurrentJobSelectedItem.CancelJob();
                _listfoo[_bbSelectedIndex] = (_bbselectedItem.name, _bbselectedItem.count - 1);
            }



            ImGui.EndGroup();

            ImGui.EndChild();


            ImGui.PopStyleVar();

        }

        private int _bloSelectedIndex = -1;
        void BorderListOptionsWiget()
        {
            
            
            string[] items = new string[_listfoo.Count];
            for (int i = 0; i < _listfoo.Count; i++)
            {
                items[i] = _listfoo[i].name;
            }
            ImGui.Indent(5);
            BorderGroup.Begin("List Options: ");
            BorderListOptions.Begin("blo", items, ref _bloSelectedIndex, 64);
            var s1 = ImGui.GetCursorPos();
            if(_bloSelectedIndex >=0)
            {
                if (ImGui.Button("^"))
                {
                    (string name, int count) item = _listfoo[_bloSelectedIndex];
                    _listfoo.RemoveAt(_bloSelectedIndex);
                    _listfoo.Insert(_bloSelectedIndex - 1, item);
                    _bloSelectedIndex--;
                }

                ImGui.SameLine();
                if (ImGui.Button("v"))
                {
                    (string name, int count) item = _listfoo[_bloSelectedIndex];
                    _listfoo.RemoveAt(_bloSelectedIndex);
                    _listfoo.Insert(_bloSelectedIndex + 1, item);
                    _bloSelectedIndex++;
                }

                ImGui.Text(_listfoo[_bloSelectedIndex].count.ToString());
                ImGui.SameLine();
                if (ImGui.Button("+"))
                {

                    _listfoo[_bloSelectedIndex] = (_listfoo[_bloSelectedIndex].name, _listfoo[_bloSelectedIndex].count + 1);
                }

                ImGui.SameLine();
                if (ImGui.Button("-"))
                {
                    _listfoo[_bloSelectedIndex] = (_listfoo[_bloSelectedIndex].name, _listfoo[_bloSelectedIndex].count - 1);
                }
            }

            var s2 = ImGui.GetCursorPos();
            BorderListOptions.End(s2-s1);
            
            BorderGroup.End(137);
            ImGui.Unindent(5);
            
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            _dateChangeSinceLastFrame = true;

            if (_systemState.EntitiesAdded.Count > 0 || _systemState.EntitysToBin.Count > 0)
            {
                RefreshFactionEntites();
            }

        }

        public override void OnGameTickChange(DateTime newDate)
        {
        }
        
        public override void OnSelectedSystemChange(StarSystem newStarSys)
        {
            throw new NotImplementedException();
        }
    }

    public static class EntityInspector
    {
        private static int _selectedDB = -1;

        public static void Display(Entity entity)
        {
            var dblist = entity.DataBlobs;
            string[] stArray = new string[dblist.Count];
            for (int i = 0; i < dblist.Count; i++)
            {
                var db = dblist[i];
                stArray[i] = db.GetType().ToString();

            }
            BorderListOptions.Begin("DataBlobs:", stArray, ref _selectedDB, 300f);

            var p0 = ImGui.GetCursorPos();
            
            if(_selectedDB >= 0)
                DBDisplay(dblist[_selectedDB]);

            var p1 = ImGui.GetCursorPos();
            var size = new Vector2(ImGui.GetContentRegionAvail().X, p1.Y - p0.Y );
            
            BorderListOptions.End(size);

        }



        static void DBDisplay(BaseDataBlob dataBlob)
        {
            Type dbType = dataBlob.GetType();
            PropertyInfo[] propertyInfos;
            
            propertyInfos = dbType.GetProperties();
            FieldInfo[] fieldInfos;
            fieldInfos = dbType.GetFields();
            
 
            int numlines = propertyInfos.Length + fieldInfos.Length;
            float totalVerticalSpace = ImGui.GetTextLineHeightWithSpacing() * numlines;
            
            var size = new Vector2(ImGui.GetContentRegionAvail().X, totalVerticalSpace);
            
            ImGui.BeginChild("InnerColomns", size);
            
            ImGui.Columns(2);
            
            foreach (var property in propertyInfos)
            {
                ImGui.Text(property.Name);
                ImGui.NextColumn();
                var value = property.GetValue(dataBlob);
                if(value != null)
                    ImGui.Text(value.ToString());
                else ImGui.Text("null");
                ImGui.NextColumn();
            }

            foreach (var field in fieldInfos)
            {
                ImGui.Text(field.Name);
                ImGui.NextColumn();
                var value = field.GetValue(dataBlob);
                if(value != null)
                    ImGui.Text(value.ToString());
                else ImGui.Text("null");
                ImGui.NextColumn();
                
            }

            ImGui.Columns(0);
            
            ImGui.EndChild();

        }


    }

}
