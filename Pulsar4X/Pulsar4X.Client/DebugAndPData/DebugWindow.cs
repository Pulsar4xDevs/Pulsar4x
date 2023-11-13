using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Engine;
using Pulsar4X.Atb;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;
using Pulsar4X.Extensions;
using Pulsar4X.SDL2UI.Combat;
using Vector3 = Pulsar4X.Orbital.Vector3;
using System.Collections.Immutable;
using System.Linq;

namespace Pulsar4X.SDL2UI
{
    public class DebugWindow : PulsarGuiWindow
    {
        EntityState? _selectedEntityState;

        private Entity? _selectedEntity;
        private Entity? SelectedEntity
        {
            get { return _selectedEntity;}
            set
            {
                if (_selectedEntity != value)
                {

                    _selectedEntity = value;

                    if(_selectedEntity != null)
                    {
                        _selectedEntityName = _selectedEntity.HasDataBlob<NameDB>() ? _selectedEntity.GetDataBlob<NameDB>().GetName(_uiState.Faction) : "Unknown";
                        if(SystemState != null && SystemState.EntityStatesWithNames.ContainsKey(_selectedEntity.Id))
                        {
                            _selectedEntityState = SystemState.EntityStatesWithNames[_selectedEntity.Id];
                            _uiState.EntityClicked(_selectedEntityState, MouseButtons.Primary);
                        }
                    }
                    else
                    {
                        _selectedEntityState = null;
                        _selectedEntityName = null;
                    }
                    OnSelectedEntityChanged();
                }
            }
        }

        private string? _selectedEntityName;

        public SystemState? SystemState { get; set; }

        bool _dateChangeSinceLastFrame = true;
        bool _isRunningFrame = false;
        bool _drawSOI = false;
        bool _drawParentSOI = false;
        //List<ECSLib.Vector4> positions = new List<ECSLib.Vector4>();
        private bool _showDamageWindow = false;
        private IntPtr _dmgTxtr;

        List<(string name, Entity entity)> _factionOwnedEntites = new List<(string name, Entity entity)>();
        List<(string name, Entity entity, string faction)> _allEntites = new List<(string name, Entity entity, string faction)>();


        private List<(string name, int count)> _listfoo = new List<(string, int)>()
        {
            ("Item1", 5),
            ("Item2", 8),
            ("Item3", 9),
            ("Item4", 3),
            ("Item5", 1)

        };



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
                //if(_uiState.LastClickedEntity?.Entity != null)
                //    instance.SelectedEntity = _uiState.LastClickedEntity.Entity;
            }
            //if(_uiState.LastClickedEntity?.Entity != null && instance.SelectedEntity != _uiState.LastClickedEntity.Entity)
            //    instance.SelectedEntity = _uiState.LastClickedEntity.Entity;
            instance.SystemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
            return instance;
        }



        internal void SetGameEvents()
        {
            if (_uiState.Game != null)
            {
                //_uiState.EntityClickedEvent += UIStateEntityClicked;
            }
        }

        private void OnSelectedEntityChanged()
        {
            if(SelectedEntity == null) return;

            if (SelectedEntity.HasDataBlob<EntityDamageProfileDB>())
            {
                var dmgdb = SelectedEntity.GetDataBlob<EntityDamageProfileDB>();
                _dmgTxtr = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, dmgdb.DamageProfile);
            }
            else if(SelectedEntity.HasDataBlob<SensorInfoDB>())
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





        Orbital.Vector3 pos = new Orbital.Vector3();
        double truAnomoly = 0;

        internal override void Display()
        {
            _isRunningFrame = true;
            if(IsActive && ImGui.Begin("debug", ref IsActive))
            {
                if(ImGui.BeginTabBar("DebugTabs"))
                {
                    DisplayInfoTab();
                    DisplayEntitiesTab();
                    DisplayInstanceProcessorsTab();
                    DisplayHotLoopProcessorsTab();

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }

            _isRunningFrame = false;
            _dateChangeSinceLastFrame = false;
        }

        private void DisplayEntitiesTab()
        {
            if (ImGui.BeginTabItem("Entities"))
            {
                var size = ImGui.GetContentRegionAvail();
                var firstChildSize = new System.Numerics.Vector2(size.X * 0.27f, 0);
                var secondChildSize = new System.Numerics.Vector2(size.X * 0.72f, 0);

                if (ImGui.BeginChild("Enttiy Selector", firstChildSize))
                {
                    ImGui.Columns(3);
                    for (int i = 0; i < _allEntites.Count; i++)
                    {
                        if (ImGui.Selectable(_allEntites[i].name + "##" + i))
                        {
                            SelectedEntity = _allEntites[i].entity;
                        }
                        ImGui.NextColumn();
                        ImGui.Text(_allEntites[i].entity.Id.ToString());
                        ImGui.NextColumn();
                        ImGui.Text(_allEntites[i].faction);
                        ImGui.NextColumn();

                    }
                    ImGui.Columns(1);
                    ImGui.EndChild();
                }

                ImGui.SameLine();

                if (SelectedEntity != null && SelectedEntity.IsValid && ImGui.BeginChild("Entity Inspector", secondChildSize))
                {
                    if (ImGui.CollapsingHeader("Selected Entity: " + _selectedEntityName + "###NameHeader", ImGuiTreeNodeFlags.DefaultOpen))
                    {

                        ImGui.Text(SelectedEntity.Id.ToString());
                        if (SelectedEntity.HasDataBlob<PositionDB>())
                        {
                            var positiondb = SelectedEntity.GetDataBlob<PositionDB>();
                            var posAbs = positiondb.AbsolutePosition;
                            ImGui.Text("x: " + Stringify.Distance(posAbs.X));
                            ImGui.Text("y: " + Stringify.Distance(posAbs.Y));
                            ImGui.Text("z: " + Stringify.Distance(posAbs.Z));
                            if (positiondb.Parent != null)
                            {
                                ImGui.Text("Parent: " + positiondb.Parent.GetDataBlob<NameDB>().DefaultName);

                                ImGui.Text("Dist: " + Stringify.Distance(positiondb.RelativePosition.Length()));
                            }

                            (Vector3 position, Vector3 Velocity) relativeState;
                            try
                            {
                                relativeState = SelectedEntity.GetRelativeState();
                            }
                            catch (Exception)
                            {
                                relativeState.Velocity = Vector3.Zero;
                            }

                            ImGui.Text("relative Velocity: " + relativeState.Velocity);
                            ImGui.Text("relative Speed: " + Stringify.Velocity(relativeState.Velocity.Length()));
                        }

                        if (ImGui.CollapsingHeader("DataBlob List"))
                        {
                            EntityInspector.DisplayDatablobs(SelectedEntity);
                            ImGui.NewLine();
                        }





                        if (SelectedEntity.HasDataBlob<MassVolumeDB>())
                        {
                            if (ImGui.CollapsingHeader("MassVolumeDB: ###MassVolDBHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                            {
                                MassVolumeDB mvdb = SelectedEntity.GetDataBlob<MassVolumeDB>();
                                ImGui.Text("Mass " + Stringify.Mass(mvdb.MassDry));
                                ImGui.Text("Volume " + Stringify.Velocity(mvdb.Volume_m3));
                                ImGui.Text("Density " + mvdb.DensityDry_gcm + "g/cm^3");
                                ImGui.Text("Radius " + Stringify.Distance(mvdb.RadiusInM));
                            }

                        }
                        if (SelectedEntity.HasDataBlob<OrbitDB>() || SelectedEntity.HasDataBlob<OrbitUpdateOftenDB>())
                        {

                            if (ImGui.Checkbox("Draw SOI", ref _drawSOI))
                            {
                                SimpleCircle cir;
                                if (_drawSOI)
                                {
                                    var soiradius = SelectedEntity.GetSOI_AU();
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

                                if (orbitDB == null)
                                    orbitDB = SelectedEntity.GetDataBlob<OrbitUpdateOftenDB>();
                                //if (_uiState.CurrentSystemDateTime != lastDate)
                                //{
                                pos = orbitDB.GetAbsolutePosition_AU(_uiState.PrimarySystemDateTime);
                                truAnomoly = orbitDB.GetTrueAnomaly(_uiState.PrimarySystemDateTime);
                                //lastDate = _uiState.PrimarySystemDateTime;
                                //}


                                ImGui.Text("x: " + pos.X);
                                ImGui.Text("y: " + pos.Y);
                                ImGui.Text("z: " + pos.Z);

                                ImGui.Text("MeanMotion: " + Angle.ToDegrees(orbitDB.MeanMotion) + " in Deg/s");
                                ImGui.Text("MeanVelocity: " + Stringify.Velocity(orbitDB.MeanOrbitalVelocityInm()));

                                ImGui.Text("SOI Radius: " + Stringify.Distance(SelectedEntity.GetSOI_m()));
                                ImGui.Text("Orbital Period:" + orbitDB.OrbitalPeriod);
                                ImGui.Text("Orbital Period:" + orbitDB.OrbitalPeriod.TotalSeconds);

                                var ke = OrbitMath.KeplerFromOrbitDB(orbitDB);
                                ImGui.Columns(3);
                                ImGui.SetColumnWidth(0, 128);
                                ImGui.SetColumnWidth(1, 128);
                                ImGui.SetColumnWidth(2, 128);

                                ImGui.Text("Eccentricity:");
                                ImGui.NextColumn();
                                ImGui.Text(orbitDB.Eccentricity.ToString());
                                ImGui.NextColumn();
                                ImGui.Text(ke.Eccentricity.ToString());
                                ImGui.NextColumn();


                                ImGui.Text("AoP:");
                                ImGui.NextColumn();
                                ImGui.Text(Angle.ToDegrees(orbitDB.ArgumentOfPeriapsis).ToString());
                                ImGui.NextColumn();
                                ImGui.Text(ke.AoP.ToString());
                                ImGui.NextColumn();


                                ImGui.Text("TrueAnomaly:");
                                ImGui.NextColumn();
                                ImGui.Text(truAnomoly.ToString());
                                ImGui.NextColumn();
                                ImGui.Text(ke.TrueAnomalyAtEpoch.ToString());
                                ImGui.NextColumn();


                                ImGui.Text("SemiMajAxis:");
                                ImGui.NextColumn();
                                ImGui.Text(Stringify.Distance(orbitDB.SemiMajorAxis));
                                ImGui.NextColumn();
                                ImGui.Text(Stringify.Distance(ke.SemiMajorAxis));
                                ImGui.NextColumn();


                                ImGui.Text("Periapsis: ");
                                ImGui.NextColumn();
                                ImGui.Text(Stringify.Distance(orbitDB.Periapsis));
                                ImGui.NextColumn();
                                ImGui.Text(Stringify.Distance(ke.Periapsis));
                                ImGui.NextColumn();


                                ImGui.Text("Appoapsis: ");
                                ImGui.NextColumn();
                                ImGui.Text(Stringify.Distance(orbitDB.Apoapsis));
                                ImGui.NextColumn();
                                ImGui.Text(Stringify.Distance(ke.Apoapsis));
                                ImGui.NextColumn();


                                ImGui.Columns(1);





                                if (orbitDB.Parent != null)
                                    ImGui.Text("Parent: " + orbitDB.Parent.GetDataBlob<NameDB>().DefaultName);
                                if (orbitDB.Children.Count > 0)
                                {
                                    foreach (var item in orbitDB.Children)
                                    {
                                        ImGui.Text(item.GetDataBlob<NameDB>().DefaultName);
                                    }

                                }



                                var relativeState = SelectedEntity.GetRelativeState();
                                //var globalvec = OrbitMath.OrbitToGlobalVector(relativeState.Velocity, orbitDB.LongitudeOfAscendingNode, orbitDB.Inclination);
                                var progradeVec = new Vector3(0, 100, 0);
                                //var thrustvec = OrbitMath.OrbitToGlobalVector(progradeVec, pos, relativeState.Velocity);
                                //var thrustvec2 = OrbitMath.OrbitToGlobalVector(progradeVec, orbitDB.LongitudeOfAscendingNode, orbitDB.Inclination);
                                var thrustvec3 = OrbitMath.ProgradeToStateVector(progradeVec, truAnomoly, orbitDB.ArgumentOfPeriapsis, orbitDB.LongitudeOfAscendingNode, orbitDB.Inclination);
                                var thrustvec4 = OrbitMath.ProgradeToStateVector(orbitDB.GravitationalParameter_m3S2, progradeVec, pos, relativeState.Velocity);
                                ImGui.Text("stateVec: " + relativeState.Velocity);
                                //ImGui.Text("globalVec: " + globalvec);
                                //ImGui.Text("thrustvec: " + thrustvec);
                                //ImGui.Text("thrustvec2: " + thrustvec2);
                                ImGui.Text("thrustvec3: " + thrustvec3);
                                ImGui.Text("trhustvec4: " + thrustvec4);

                            }
                        }

                        if (SelectedEntity.HasDataBlob<NewtonMoveDB>())
                        {
                            if (ImGui.Checkbox("Draw Parent SOI", ref _drawParentSOI))
                            {
                                SimpleCircle psoi;
                                SimpleLine psoilin;
                                if (_drawParentSOI)
                                {
                                    _drawParentSOI = EnableDrawSOI();
                                }
                                else
                                {
                                    _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(psoi));
                                    _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(psoilin));
                                }
                            }

                        }


                        if (SelectedEntity.HasDataBlob<EnergyGenAbilityDB>())
                        {
                            if (ImGui.CollapsingHeader("Power ###PowerHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                            {
                                var powerDB = SelectedEntity.GetDataBlob<EnergyGenAbilityDB>();
                                ImGui.Text("Generates " + powerDB.EnergyType.Name);
                                ImGui.Text("Max of: " + powerDB.TotalOutputMax + "/s");
                                string fueltype = SelectedEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMaterial(powerDB.TotalFuelUseAtMax.type).Name;
                                ImGui.Text("Burning " + powerDB.TotalFuelUseAtMax.maxUse + " of " + fueltype);
                                ImGui.Text("With " + powerDB.LocalFuel + " remaining reactor fuel");

                                foreach (var etype in powerDB.EnergyStored)
                                {
                                    string etypename = SelectedEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMaterial(etype.Key).Name;
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
                                ImGui.Text("Current Speed: " + warpDB.CurrentVectorMS.Length());


                                //ImGui.Text("Energy type: " + warpDB.EnergyType);
                                ImGui.Text(SelectedEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMaterial(warpDB.EnergyType).Name);

                                ImGui.Text("Creation Cost: " + warpDB.BubbleCreationCost.ToString());
                                ImGui.Text("Sustain Cost: " + warpDB.BubbleSustainCost.ToString());
                                ImGui.Text("Collapse Cost: " + warpDB.BubbleCollapseCost.ToString());

                            }


                        }
                        if (SelectedEntity.HasDataBlob<NewtonMoveDB>())
                        {
                            var nmdb = _uiState.LastClickedEntity.Entity.GetDataBlob<NewtonMoveDB>();
                            var ntdb = _uiState.LastClickedEntity.Entity.GetDataBlob<NewtonThrustAbilityDB>();
                            if (ImGui.CollapsingHeader("NewtonMove: ###NewtHeader", ImGuiTreeNodeFlags.CollapsingHeader))
                            {
                                ImGui.Text("Manuver Vector: " + nmdb.ManuverDeltaV);
                                ImGui.Text("Total Manuver DV: " + Stringify.Distance(nmdb.ManuverDeltaVLen) + "/s");
                                ImGui.Text("Parent Body: " + nmdb.SOIParent.GetDataBlob<NameDB>().DefaultName);
                                ImGui.Text("Current Vector:");
                                ImGui.Text("X:" + Stringify.Velocity(nmdb.CurrentVector_ms.X));
                                ImGui.Text("Y:" + Stringify.Velocity(nmdb.CurrentVector_ms.Y));
                                ImGui.Text("Z:" + Stringify.Velocity(nmdb.CurrentVector_ms.Z));
                                ImGui.Text("Speed: " + Stringify.Velocity(nmdb.CurrentVector_ms.Length()));
                                ImGui.Text("Remaining Dv:" + Stringify.Distance(ntdb.DeltaV) + "/s");
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
                                ImGui.Text("X:" + Stringify.Distance(db.ExitPointrelative.X));
                                ImGui.Text("Y:" + Stringify.Distance(db.ExitPointrelative.Y));
                                ImGui.Text("Z:" + Stringify.Distance(db.ExitPointrelative.Z));


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

                        if (SelectedEntity.HasDataBlob<SensorProfileDB>() && ImGui.CollapsingHeader("SensorProfile"))
                        {
                            var profile = SelectedEntity.GetDataBlob<SensorProfileDB>();
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

                        if (SelectedEntity.HasDataBlob<GenericFiringWeaponsDB>())
                        {
                            var db = SelectedEntity.GetDataBlob<GenericFiringWeaponsDB>();
                            if (ImGui.CollapsingHeader("Firing Weapons"))
                            {
                                for (int i = 0; i < db.WpnIDs.Length; i++)
                                {
                                    float reloadAmount = db.InternalMagQty[i];
                                    float reloadMax = db.InternalMagSizes[i];
                                    float reloadPerShot = db.AmountPerShot[i];
                                    float minShots = db.MinShotsPerfire[i];

                                    float percFull = reloadAmount / reloadMax;
                                    float percToFire = reloadAmount / (minShots * reloadPerShot);
                                    float percPerSec = db.ReloadAmountsPerSec[i] / reloadMax;
                                    System.Numerics.Vector2 pbsize = new System.Numerics.Vector2(200, 5);

                                    float maxShots = reloadMax / reloadPerShot;
                                    ImGui.Text("Max shots per burst: " + maxShots);
                                    ImGui.Text("Min shots per burst: " + minShots);

                                    ImGui.Text("Reload Amount Per Sec:" + db.ReloadAmountsPerSec[i]);
                                    ImGui.Text("Reload Percent Per Sec:" + percPerSec);

                                    ImGui.Text("Reload Progress " + reloadAmount + "/" + reloadMax);
                                    ImGui.ProgressBar(percFull, pbsize);
                                    ImGui.Text("Reload Progress to min fire");
                                    ImGui.ProgressBar(percToFire, pbsize);


                                    var firingShots = db.ShotsFiredThisTick[i];
                                    ImGui.Text("Firing " + firingShots + " this tick");

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
                        if (_dmgTxtr != IntPtr.Zero)
                        {
                            if (ImGui.CollapsingHeader("DamageProfile"))
                            {
                                if (ImGui.ImageButton(_dmgTxtr, new System.Numerics.Vector2(64, 64)))
                                {
                                    //show a full sized scrollable image.
                                }
                            }
                        }

                        if (SelectedEntity.HasDataBlob<EntityDamageProfileDB>())
                        {
                            var dv = DamageViewer.GetInstance();
                            if (ImGui.Checkbox("Show Damage", ref _showDamageWindow))
                            {
                                if (dv.CanActive)
                                    DamageViewer.GetInstance().SetActive(_showDamageWindow);
                                else
                                    _showDamageWindow = false;
                            }

                        }
                    }

                    ImGui.EndChild();
                }

                ImGui.EndTabItem();
            }
        }

        private void DisplayInfoTab()
        {
            if (ImGui.BeginTabItem("Info"))
            {
                BorderGroup.Begin("Info", ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));

                ImGui.Text(_uiState.PrimarySystemDateTime.ToString());
                ImGui.Text("GitHash: " + AssemblyInfo.GetGitHash());

                BorderGroup.End();
                ImGui.NewLine();

                if (ImGui.CollapsingHeader("Camera Functions", ImGuiTreeNodeFlags.CollapsingHeader))
                {
                    var cam = _uiState.Camera;
                    if (cam.IsPinnedToEntity)
                    {
                        var entyName = (SystemState == null) ? "<null>" : SystemState.EntityStatesWithNames[_uiState.Camera.PinnedEntityGuid].Name;
                        ImGui.Text("Camera is pinned to:");
                        ImGui.SameLine();
                        ImGui.Text(entyName);
                    }
                    else
                    {
                        ImGui.Text("Camera is not pinned to an entity.");
                    }

                    ImGui.Text("Zoom: " + cam.ZoomLevel);

                    ImGui.Text("Raw Cursor Coordinate");
                    System.Numerics.Vector2 mouseCoord = ImGui.GetMousePos();
                    ImGui.Text("x: " + mouseCoord.X);
                    ImGui.SameLine();
                    ImGui.Text("y: " + mouseCoord.Y);

                    ImGui.Text("Cursor World Coordinate:");
                    var mouseWorldCoord = cam.MouseWorldCoordinate_m();
                    ImGui.Text("x" + Stringify.Distance(mouseWorldCoord.X));
                    ImGui.SameLine();
                    ImGui.Text("y" + Stringify.Distance(mouseWorldCoord.Y));
                    var mouseWorldCoord_AU = Distance.MToAU(cam.MouseWorldCoordinate_AU());
                    ImGui.Text("x" + mouseWorldCoord_AU.X + " AU");
                    ImGui.SameLine();
                    ImGui.Text("y" + mouseWorldCoord_AU.Y + " AU");

                    ImGui.Text("Cursor View Coordinate:");
                    ImGui.Text("(WorldCoord - CameraWorldPos) * zoomLevel + viewportCenter");
                    ImGui.Text("(" + mouseWorldCoord.X + "-" + cam.CameraWorldPosition.X + ") *" + cam.ZoomLevel + "+" + cam.ViewPortCenter.X);
                    var mouseViewCoord = cam.ViewCoordinate_m(mouseWorldCoord);
                    ImGui.Text("x" + mouseViewCoord.x + " p");
                    ImGui.SameLine();
                    ImGui.Text("y" + mouseViewCoord.y + " p");
                    var mouseviewCoord_AU = cam.ViewCoordinate_AU(mouseWorldCoord);
                    ImGui.Text("x" + mouseviewCoord_AU.x + " p");
                    ImGui.SameLine();
                    ImGui.Text("y" + mouseviewCoord_AU.y + " p");

                    ImGui.Text("Camrera WorldPosition");
                    var camWorldCoord_m = cam.CameraWorldPosition;
                    ImGui.Text("x" + camWorldCoord_m.X + " m");
                    ImGui.SameLine();
                    ImGui.Text("y" + camWorldCoord_m.Y + " m");
                    var camWorldCoord_AU = Distance.MToAU(cam.CameraWorldPosition);
                    ImGui.Text("x" + camWorldCoord_AU.X + " AU");
                    ImGui.SameLine();
                    ImGui.Text("y" + camWorldCoord_AU.Y + " AU");

                }




                ImGui.Text("Suported Special Chars");
                ImGui.Text("Ω, ω, ν, Δ, θ");



                if (ImGui.CollapsingHeader("GraphicTests", ImGuiTreeNodeFlags.CollapsingHeader))
                {
                    var window = GraphicDebugWindow.GetWindow(_uiState);
                    window.Display();
                    window.Enable(true, _uiState);
                }




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
                ImGui.Text("Number Of Entites: " + _uiState.SelectedSystem.EntityCount);
                if (ImGui.CollapsingHeader("Log"))
                {
                    ImGui.BeginChild("LogChild", new System.Numerics.Vector2(800, 300), true);
                    ImGui.Columns(4, "Events", true);
                    ImGui.Text("DateTime");
                    ImGui.NextColumn();
                    ImGui.Text("Faction");
                    ImGui.NextColumn();
                    ImGui.Text("Entity");
                    ImGui.NextColumn();
                    ImGui.Text("Event Message");
                    ImGui.NextColumn();

                    // foreach (var gameEvent in StaticRefLib.EventLog.GetAllEvents())
                    // {

                    //     string entityStr = "";
                    //     if (gameEvent.Entity != null)
                    //     {
                    //         if (gameEvent.EntityName.IsNullOrWhitespace() && gameEvent.Entity.IsValid)
                    //         {
                    //             if (gameEvent.Entity.HasDataBlob<NameDB>())
                    //                 entityStr = gameEvent.Entity.GetDataBlob<NameDB>().DefaultName;
                    //             else
                    //                 entityStr = gameEvent.Entity.Guid.ToString();
                    //         }
                    //         else
                    //         {
                    //             entityStr = gameEvent.EntityName;
                    //         }
                    //     }


                    //     string factionStr = "";
                    //     if (gameEvent.Faction != null)
                    //         if (gameEvent.Faction.HasDataBlob<NameDB>())
                    //             factionStr = gameEvent.Faction.GetDataBlob<NameDB>().DefaultName;
                    //         else
                    //             factionStr = gameEvent.Faction.Guid.ToString();

                    //     ImGui.Separator();
                    //     ImGui.Text(gameEvent.Time.ToString());
                    //         ImGui.NextColumn();
                    //     ImGui.Text(factionStr);
                    //         ImGui.NextColumn();
                    //     ImGui.Text(entityStr);
                    //         ImGui.NextColumn();
                    //     ImGui.TextWrapped(gameEvent.Message);

                    //         ImGui.NextColumn();


                    // }
                    //ImGui.Separator();
                    //ImGui.Columns();
                    ImGui.EndChild();
                }

                ImGui.EndTabItem();
            }
        }

        private void DisplayInstanceProcessorsTab()
        {
            if(SystemState == null) return;

            if(ImGui.BeginTabItem("Instance Processors"))
            {
                ImGui.Columns(3);
                foreach(var (dateTime, processSet) in SystemState.StarSystem.ManagerSubpulses.InstanceProcessorsQueue.ToArray())
                {
                    if(processSet.Count > 0)
                        ImGui.Separator();

                    foreach(var thing in processSet.ToArray())
                    {
                        ImGui.Text(dateTime.ToString());
                        ImGui.NextColumn();
                        ImGui.Text("Instance (" + thing.Value.Count + ")");
                        ImGui.NextColumn();
                        ImGui.Text(thing.Key);
                        ImGui.NextColumn();
                    }
                }

                ImGui.EndTabItem();
            }
        }

        private void DisplayHotLoopProcessorsTab()
        {
            if(SystemState == null) return;

            if(ImGui.BeginTabItem("HotLoop Processors"))
            {
                ImGui.Columns(3);
                foreach(var (type, dateTime) in SystemState.StarSystem.ManagerSubpulses.HotLoopProcessorsNextRun)
                {
                    ImGui.Text(dateTime.ToString());
                    ImGui.NextColumn();
                    ImGui.Text("System");
                    ImGui.NextColumn();
                    ImGui.Text(type.processorType.Name);
                    ImGui.NextColumn();
                }
                ImGui.EndTabItem();
            }
        }

        private bool EnableDrawSOI()
        {
            if(SelectedEntity == null) return false;
            var myPos = SelectedEntity.GetDataBlob<PositionDB>();

            if(myPos.Parent == null) return false;
            var parent = myPos.Parent;
            var cnmve = SelectedEntity.GetDataBlob<NewtonMoveDB>();

            var soiradius = parent.GetSOI_AU();
            var colour = new SDL2.SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 100 };
            var psoi = new SimpleCircle(parent.GetDataBlob<PositionDB>(), soiradius, colour);
            var pmass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var mymass = SelectedEntity.GetDataBlob<MassVolumeDB>().MassDry;

            var sgp = GeneralMath.StandardGravitationalParameter(pmass + mymass);
            var vel = Distance.KmToM(cnmve.CurrentVector_ms);
            var cpos = myPos.RelativePosition;
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
            double θ = EllipseMath.TrueAnomalyAtRadus(soiradius, cp, ce);
            θ += cAoP;

            var x = soiradius * Math.Cos(θ);
            var y = soiradius * Math.Sin(θ);
            var psoilin = new SimpleLine(parent.GetDataBlob<PositionDB>(), new Orbital.Vector2() { X = x, Y = y }, colour);

            _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(psoi), psoi);
            _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(psoilin), psoilin);
            return true;
        }

        void RefreshFactionEntites()
        {
            _factionOwnedEntites = new List<(string name, Entity entity)>();
            var factionEntites = _uiState.SelectedSystem.GetEntitiesByFaction(_uiState.Faction.Id);
            foreach (var entity in factionEntites.ToArray())
            {
                string name = entity.Id.ToString();
                if(entity.HasDataBlob<NameDB>())
                {
                    name = entity.GetDataBlob<NameDB>().GetName(_uiState.Faction);
                }
                _factionOwnedEntites.Add((name, entity));
            }

            _allEntites = new List<(string name, Entity entity, string faction)>();

            foreach (var entity in _uiState.Game.Factions)
            {
                addEntity(entity.Value);
            }

            foreach (var entity in _uiState.SelectedSystem.GetAllEntites())
            {
                addEntity(entity);
            }

            void addEntity(Entity entity)
            {
                if(entity == null)
                    return;
                string name = entity.Id.ToString();
                if(entity.HasDataBlob<NameDB>())
                    name = entity.GetDataBlob<NameDB>().OwnersName;

                string factionOwner = Guid.Empty.ToString();
                if(entity.FactionOwnerID == Game.NeutralFactionId)
                {
                    factionOwner = "Neutral";
                }
                else if(_uiState.Game.Factions.ContainsKey(entity.FactionOwnerID))
                {
                    factionOwner = _uiState.Game.Factions[entity.FactionOwnerID].GetDataBlob<NameDB>().OwnersName;
                }
                _allEntites.Add((name, entity, factionOwner));
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
                ImGui.BeginChild("TopItems", new System.Numerics.Vector2(400, heightt));
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 300);

                ImGui.BeginGroup();
                var cpos = ImGui.GetCursorPos();
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ChildBg));
                ImGui.Button("##ht"+i, new System.Numerics.Vector2(colomnWidth0 - spacingH, ImGui.GetTextLineHeightWithSpacing()));
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
                ImGui.BeginChild("Buttons", new System.Numerics.Vector2(400, hoverHeigt), true);
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
                    //_hvSelectedIndex = _hvSelectedIndex;
                }

                ImGui.NextColumn();

                ImGui.EndChild();
                ImGui.PopStyleVar(2);


                for (int i = _hvSelectedIndex + 1; i < _listfoo.Count; i++)
                {
                    ImGui.BeginChild("Bottom", new System.Numerics.Vector2(400, heightb));
                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, 300);

                    ImGui.BeginGroup();
                    var cpos = ImGui.GetCursorPos();
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ChildBg));
                    ImGui.Button("##hb" + i, new System.Numerics.Vector2(colomnWidth0 - spacingH, ImGui.GetTextLineHeightWithSpacing()));
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
            ImGui.BeginChild("ButtonBoxList", new System.Numerics.Vector2(280, 100), true, ImGuiWindowFlags.ChildWindow);
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

            ImGui.BeginChild("Buttons##bb", new System.Numerics.Vector2(116, 100), true, ImGuiWindowFlags.ChildWindow);
            ImGui.BeginGroup();
            //if (ImGui.ImageButton(_uiState.UpImg(), new Vector2(16, 8)))
            if (ImGui.Button("^" + "##bb" + _bbSelectedIndex))
            {
                (string name, int count) item = _listfoo[_bbSelectedIndex];
                _listfoo.RemoveAt(_bbSelectedIndex);
                _listfoo.Insert(_bbSelectedIndex - 1, item);
                _bbSelectedIndex--;
            }
            //if (ImGui.ImageButton(_uiState.DnImg(), new Vector2(16, 8)))
            if (ImGui.Button("v" + "##bb" + _bbSelectedIndex))
            {
                (string name, int count) item = _listfoo[_bbSelectedIndex];
                _listfoo.RemoveAt(_bbSelectedIndex);
                _listfoo.Insert(_bbSelectedIndex + 1, item);
                _bbSelectedIndex++;
            }
            ImGui.EndGroup();
            ImGui.SameLine();
            //if (ImGui.ImageButton(_uiState.RepeatImg(), new Vector2(16, 16)))
            if (ImGui.Button("+" + "##bb" + _bbSelectedIndex))
            {
                //_refineryVM.CurrentJobSelectedItem.ChangeRepeat(!_refineryVM.CurrentJobSelectedItem.Repeat);
                _listfoo[_bbSelectedIndex] = (_bbselectedItem.name, _bbselectedItem.count + 1);
            }

            ImGui.SameLine();
            //if (ImGui.ImageButton(_uiState.CancelImg(), new Vector2(16, 16)))
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

            if(SystemState == null) return;

            if (SystemState.EntitiesAdded.Count > 0 || SystemState.EntitysToBin.Count > 0)
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
}
