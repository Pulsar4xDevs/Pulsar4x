using System;
using ImGuiNET;
using System.Numerics;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    public class SettingsWindow : PulsarGuiWindow
    {
        ImGuiTreeNodeFlags _xpanderFlags = ImGuiTreeNodeFlags.CollapsingHeader;
        List<List<UserOrbitSettings>> _userOrbitSettingsMtx;
        //UserOrbitSettings _userOrbitSettings;

        bool IsThreaded;

        bool RalitiveOrbitVelocity;
        private SettingsWindow()
        {
            _userOrbitSettingsMtx = _state.UserOrbitSettingsMtx;



            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            IsThreaded = _state.Game.Settings.EnableMultiThreading;

            RalitiveOrbitVelocity = ECSLib.OrbitProcessor.UseRalitiveVelocity;
        }
        internal static SettingsWindow GetInstance()
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(SettingsWindow)))
            {
                return new SettingsWindow();
            }
            return (SettingsWindow)_state.LoadedWindows[typeof(SettingsWindow)];
        }

        internal override void Display()
        {
            if (IsActive)
            {
                Vector2 size = new Vector2(200, 100);
                Vector2 pos = new Vector2(0, 0);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

                if (ImGui.Begin("Settings", ref IsActive, _flags))
                {
                
                    ImGui.Checkbox("Show Pulsar Debug Window", ref DebugWindow.GetInstance().IsActive);
                    ImGui.Checkbox("Show ImguiMetrix", ref _state.ShowMetrixWindow);
                    ImGui.Checkbox("Show ImgDebug", ref _state.ShowImgDbg);
                    ImGui.Checkbox("DemoWindow", ref _state.ShowDemoWindow);
                        
                    if (ImGui.CollapsingHeader("Process settings", _xpanderFlags))
                    {
                        if (ImGui.Checkbox("MultiThreaded", ref IsThreaded))
                        {
                            _state.Game.Settings.EnableMultiThreading = IsThreaded;
                        }
                        if (ImGui.Checkbox("Translate Uses Ralitive Velocity", ref RalitiveOrbitVelocity))
                        {
                            ECSLib.OrbitProcessor.UseRalitiveVelocity = RalitiveOrbitVelocity;
                        }
                        if (ImGui.IsItemHovered())
                        { 
                            if (RalitiveOrbitVelocity)
                                ImGui.SetTooltip("Ships exiting from a non newtonion translation will enter an orbit: \n Using a vector ralitive to it's origin parent");
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
                                float _nameZoomLevel = _state.DrawNameZoomLvl[(int)otype];
                                ImGui.SliderFloat("Draw Names at Zoom: ", ref _nameZoomLevel, 0.01f, 10000f);
                                _state.DrawNameZoomLvl[(int)otype] = _nameZoomLevel;
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
                                            _state.SelectedSysMapRender.UpdateUserOrbitSettings();

                                        if (ImGui.SliderInt("Number Of Segments ##" + i + j, ref _arcSegments, 1, 255, _userOrbitSettings.NumberOfArcSegments.ToString()))
                                        {
                                            _userOrbitSettings.NumberOfArcSegments = (byte)_arcSegments;
                                            _state.SelectedSysMapRender.UpdateUserOrbitSettings();
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
                                            _state.SelectedSysMapRender.UpdateUserOrbitSettings();
                                        }

                                        if (ImGui.SliderInt("Min Alpha  ##" + i + j, ref _minAlpha, 0, _maxAlpha, ""))
                                        {
                                            _userOrbitSettings.MinAlpha = (byte)_minAlpha;
                                            _state.SelectedSysMapRender.UpdateUserOrbitSettings();
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
