using System;
using ImGuiNET;
using System.Numerics;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Pulsar4X.SDL2UI.Combat;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Pulsar4X.SDL2UI
{
    public class SettingsWindow : PulsarGuiWindow
    {
        ImGuiTreeNodeFlags _xpanderFlags = ImGuiTreeNodeFlags.CollapsingHeader;
        List<List<UserOrbitSettings>> _userOrbitSettingsMtx;
        //UserOrbitSettings _userOrbitSettings;

        bool IsThreaded;
        private bool EnforceSingleThread;
        bool relativeOrbitVelocity;

        private OrbitalDebugWindow _orbitalDebugWindow;
        
        private SettingsWindow()
        {
            _userOrbitSettingsMtx = _uiState.UserOrbitSettingsMtx;



            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            IsThreaded = _uiState.Game.Settings.EnableMultiThreading;
            EnforceSingleThread = _uiState.Game.Settings.EnforceSingleThread;
            
            relativeOrbitVelocity = ECSLib.OrbitProcessor.UseRelativeVelocity;

            _orbitalDebugWindow = OrbitalDebugWindow.GetInstance();

        }
        internal static SettingsWindow GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(SettingsWindow)))
            {
                return new SettingsWindow();
            }
            return (SettingsWindow)_uiState.LoadedWindows[typeof(SettingsWindow)];
        }

        internal override void Display()
        {
            if (IsActive)
            {
                System.Numerics.Vector2 size = new System.Numerics.Vector2(200, 100);
                System.Numerics.Vector2 pos = new System.Numerics.Vector2(0, 0);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

                if (ImGui.Begin("Settings", ref IsActive, _flags))
                {
                    bool debugActive = DebugWindow.GetInstance().GetActive();

                    if (ImGui.Checkbox("Show Pulsar Debug Window", ref debugActive))
                    {
                        DebugWindow.GetInstance().ToggleActive();
                    }

                    if(_uiState.LastClickedEntity != null && _uiState.LastClickedEntity.Entity.HasDataBlob<OrbitDB>())
                    {
                        bool orbitDebugActive = _orbitalDebugWindow.GetActive();
                        if (ImGui.Checkbox("Show Orbit Debug Lines", ref orbitDebugActive))
                        {
                            OrbitalDebugWindow.GetInstance().ToggleActive();
                        }
                    }

                    bool sensorActive = SensorDraw.GetInstance().GetActive();
                    if (ImGui.Checkbox("Show Sensor Draw", ref sensorActive))
                    {
                        SensorDraw.GetInstance().ToggleActive();
                    }
                    if (ImGui.Checkbox("Show Pulsar GUI Debug Window", ref debugActive))
                    {
                        DebugGUIWindow.GetInstance().ToggleActive();
                    }

                    bool perfActive = PerformanceDisplay.GetInstance().GetActive();
                    if (ImGui.Checkbox("Show Pulsar Performance Window", ref perfActive))
                    {
                        PerformanceDisplay.GetInstance().ToggleActive();
                    }

                    ImGui.Checkbox("Show ImguiMetrix", ref _uiState.ShowMetrixWindow);
                    ImGui.Checkbox("Show ImgDebug", ref _uiState.ShowImgDbg);
                    ImGui.Checkbox("DemoWindow", ref _uiState.ShowDemoWindow);
                    if (ImGui.Checkbox("DamageWindow", ref _uiState.ShowDamageWindow))
                    {
                        if (_uiState.ShowDamageWindow)
                            DamageViewer.GetInstance().SetActive();
                        else
                            DamageViewer.GetInstance().SetActive(false);
                        
                    }


                    if (ImGui.CollapsingHeader("Process settings", _xpanderFlags))
                    {
                        if (ImGui.Checkbox("MultiThreaded", ref IsThreaded))
                        {
                            _uiState.Game.Settings.EnableMultiThreading = IsThreaded;
                        }

                        if (ImGui.Checkbox("EnforceSingleThread", ref EnforceSingleThread))
                        {
                            _uiState.Game.Settings.EnforceSingleThread = EnforceSingleThread;
                            if (EnforceSingleThread)
                            {
                                IsThreaded = false;
                                _uiState.Game.Settings.EnableMultiThreading = false;
                            }
                        }

                        if (ImGui.Checkbox("Translate Uses relative Velocity", ref relativeOrbitVelocity))
                        {
                            ECSLib.OrbitProcessor.UseRelativeVelocity = relativeOrbitVelocity;
                        }
                        if (ImGui.IsItemHovered())
                        { 
                            if (relativeOrbitVelocity)
                                ImGui.SetTooltip("Ships exiting from a non newtonion translation will enter an orbit: \n Using a vector relative to it's origin parent");
                            else
                                ImGui.SetTooltip("Ships exiting from a non newtonion translation will enter an orbit: \n Using the absolute Vector (ie raltive to the root'sun'");
                        }
                        
                    }


                    if (ImGui.CollapsingHeader("Map Settings", _xpanderFlags))
                    {


                        for (int i = 0; i < (int)UserOrbitSettings.OrbitBodyType.NumberOf; i++)
                        {
                            UserOrbitSettings.OrbitBodyType otype = (UserOrbitSettings.OrbitBodyType)i;
                            string typeStr = otype.ToString();
                            if (ImGui.TreeNode(typeStr ))
                            {
                                float _nameZoomLevel = _uiState.DrawNameZoomLvl[(int)otype];
                                ImGui.SliderFloat("Draw Names at Zoom: ", ref _nameZoomLevel, 0.01f, 10000f);
                                _uiState.DrawNameZoomLvl[(int)otype] = _nameZoomLevel;
                                for (int j = 0; j < (int)UserOrbitSettings.OrbitTrajectoryType.NumberOf; j++)
                                {

                                    UserOrbitSettings.OrbitTrajectoryType trtype = (UserOrbitSettings.OrbitTrajectoryType)j;
                                    string trtypeStr = trtype.ToString();
                                    if (ImGui.TreeNode(trtypeStr))
                                    {

                                        UserOrbitSettings _userOrbitSettings = _userOrbitSettingsMtx[i][j];
                                        int _arcSegments = _userOrbitSettings.NumberOfArcSegments;
                                        Vector3 _colour = Helpers.Color(_userOrbitSettings.Red, _userOrbitSettings.Grn, _userOrbitSettings.Blu);
                                        int _maxAlpha = _userOrbitSettings.MaxAlpha;
                                        int _minAlpha = _userOrbitSettings.MinAlpha;


                                        //TODO: make this a knob/dial? need to create a custom control: https://github.com/ocornut/imgui/issues/942
                                        if (ImGui.SliderAngle("Sweep Angle ##" + i + j, ref _userOrbitSettings.EllipseSweepRadians, 1f, 360f))
                                            _uiState.SelectedSysMapRender.UpdateUserOrbitSettings();

                                        if (ImGui.SliderInt("Number Of Segments ##" + i + j, ref _arcSegments, 1, 255, _userOrbitSettings.NumberOfArcSegments.ToString()))
                                        {
                                            _userOrbitSettings.NumberOfArcSegments = (byte)_arcSegments;
                                            _uiState.SelectedSysMapRender.UpdateUserOrbitSettings();
                                        }

                                        if (ImGui.ColorEdit3("Orbit Ring Colour ##" + i + j, ref _colour))
                                        {
                                            _userOrbitSettings.Red = Helpers.Color(_colour.X);
                                            _userOrbitSettings.Grn = Helpers.Color(_colour.Y);
                                            _userOrbitSettings.Blu = Helpers.Color(_colour.Z);
                                        }
                                        if (ImGui.SliderInt("Max Alpha ##" + i + j, ref _maxAlpha, _minAlpha, 255, ""))
                                        {
                                            _userOrbitSettings.MaxAlpha = (byte)_maxAlpha;
                                            _uiState.SelectedSysMapRender.UpdateUserOrbitSettings();
                                        }

                                        if (ImGui.SliderInt("Min Alpha  ##" + i + j, ref _minAlpha, 0, _maxAlpha, ""))
                                        {
                                            _userOrbitSettings.MinAlpha = (byte)_minAlpha;
                                            _uiState.SelectedSysMapRender.UpdateUserOrbitSettings();
                                        }
                                    } 
                                }
                                ImGui.TreePop();
                            }
                        }
                    }

                }

                ImGui.End();
            }
        }
    }
}
