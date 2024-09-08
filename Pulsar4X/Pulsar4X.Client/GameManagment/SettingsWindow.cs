using System;
using ImGuiNET;
using System.Numerics;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.SDL2UI.Combat;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Pulsar4X.Datablobs;

namespace Pulsar4X.SDL2UI
{
    public class SettingsWindow : PulsarGuiWindow
    {
        ImGuiTreeNodeFlags _xpanderFlags = ImGuiTreeNodeFlags.CollapsingHeader;
        List<List<UserOrbitSettings>> _userOrbitSettingsMtx;
        //UserOrbitSettings _userOrbitSettings;
        private GameSettings _gameSettings;
        private bool _isThreaded;
        private bool _enforceSingleThread;
        private bool _relativeOrbitVelocity;
        private bool _strictNewtonion;
        private bool _showSizesDemo = false;
        private bool _showSelectorWindow = true;
        private OrbitalDebugWindow _orbitalDebugWindow;
        private GameLogWindow _logWindow;
        private SettingsWindow()
        {
            _userOrbitSettingsMtx = _uiState.UserOrbitSettingsMtx;
            _gameSettings = _uiState.Game.Settings;


            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            _isThreaded = _gameSettings.EnableMultiThreading;
            _enforceSingleThread = _gameSettings.EnforceSingleThread;

            _relativeOrbitVelocity = _gameSettings.UseRelativeVelocity;
            _strictNewtonion = _gameSettings.StrictNewtonion;

            _orbitalDebugWindow = OrbitalDebugWindow.GetInstance();
            _logWindow = GameLogWindow.GetInstance();

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

                    bool dataViewerActive = DataViewerWindow.GetInstance().GetActive();
                    if (ImGui.Checkbox("Show DataViewer Window", ref dataViewerActive))
                    {
                        DataViewerWindow.GetInstance().ToggleActive();
                    }

                    if (_uiState.LastClickedEntity != null)
                    {
                        var lastClickedEntity = _uiState.LastClickedEntity.Entity;

                         if(lastClickedEntity.HasDataBlob<OrbitDB>()
                            || lastClickedEntity.HasDataBlob<OrbitUpdateOftenDB>()
                            || lastClickedEntity.HasDataBlob<NewtonSimDB>())
                        {
                            bool orbitDebugActive = _orbitalDebugWindow.GetActive();
                            if (ImGui.Checkbox("Show Orbit Debug Lines", ref orbitDebugActive))
                            {
                                OrbitalDebugWindow.GetInstance().ToggleActive();
                            }
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

                    bool logActive = _logWindow.GetActive();
                    if (ImGui.Checkbox("Show Log", ref logActive))
                    {
                        _logWindow.ToggleActive();
                    }

                    bool perfActive = PerformanceWindow.GetInstance().GetActive();
                    if (ImGui.Checkbox("Show Pulsar Performance Window", ref perfActive))
                    {
                        PerformanceWindow.GetInstance().ToggleActive();
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

                    ImGui.Checkbox("Show Sizes Demo", ref _showSizesDemo);
                    if(_showSizesDemo)
                    {
                        SizesDemo.Display();
                    }

                    if(ImGui.Checkbox("Show Selector", ref _showSelectorWindow))
                    {
                        Selector.GetInstance().SetActive(_showSelectorWindow);
                    }

                    if (ImGui.CollapsingHeader("Process settings", _xpanderFlags))
                    {
                        if (ImGui.Checkbox("MultiThreaded", ref _isThreaded))
                        {
                            _uiState.Game.Settings.EnableMultiThreading = _isThreaded;
                        }

                        if (ImGui.Checkbox("EnforceSingleThread", ref _enforceSingleThread))
                        {
                            _uiState.Game.Settings.EnforceSingleThread = _enforceSingleThread;
                            if (_enforceSingleThread)
                            {
                                _isThreaded = false;
                                _uiState.Game.Settings.EnableMultiThreading = false;
                            }
                        }

                        if (ImGui.Checkbox("Translate Uses relative Velocity", ref _relativeOrbitVelocity))
                        {
                            _gameSettings.UseRelativeVelocity = _relativeOrbitVelocity;
                        }
                        if (ImGui.IsItemHovered())
                        {
                            if (_relativeOrbitVelocity)
                                ImGui.SetTooltip("Ships exiting from a non newtonion translation will enter an orbit: \n Using a vector relative to it's origin parent");
                            else
                                ImGui.SetTooltip("Ships exiting from a non newtonion translation will enter an orbit: \n Using the absolute Vector (ie raltive to the root'sun'");
                        }

                        if (ImGui.Checkbox("Translate Uses Strict Newtonion", ref _strictNewtonion))
                        {
                            _gameSettings.StrictNewtonion = _strictNewtonion;
                        }
                        if (ImGui.IsItemHovered())
                        {
                            if (_strictNewtonion)
                                ImGui.SetTooltip("Ships exiting from a non newtonion translation will enter: \n An orbit using a vector relative to it's origin vector");
                            else
                                ImGui.SetTooltip("Ships exiting from a non newtonion translation will enter: \n a Simple circular orbit ignoring its origin newton vector");
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
                                float _nameZoomLevel = _uiState.DrawNameZoomLvl[otype];
                                ImGui.SliderFloat("Draw Names at Zoom: ", ref _nameZoomLevel, 0.01f, 10000f);
                                _uiState.DrawNameZoomLvl[otype] = _nameZoomLevel;
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
