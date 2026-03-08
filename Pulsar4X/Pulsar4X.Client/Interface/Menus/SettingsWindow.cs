using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System;
using Pulsar4X.Engine;
using Vector3 = System.Numerics.Vector3;
using Pulsar4X.Orbits;
using Pulsar4X.Movement;
using Pulsar4X.Client.Combat;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class SettingsWindow : PulsarGuiWindow
    {
        ImGuiTreeNodeFlags _xpanderFlags = ImGuiTreeNodeFlags.CollapsingHeader;
        List<List<UserOrbitSettings>> _userOrbitSettingsMtx;
        //UserOrbitSettings _userOrbitSettings;
        private Pulsar4X.Engine.GameSettings _gameSettings;
        private bool _isThreaded;
        private bool _enforceSingleThread;
        private bool _relativeOrbitVelocity;
        private bool _strictNewtonion;
        private bool _showSizesDemo = false;
        private bool _showSelectorWindow = true;
        private OrbitalDebugWindow _orbitalDebugWindow;
        private GameLogWindow _logWindow;
        private int _currentResolutionIndex = 0;

        private SettingsWindow()
        {
            _userOrbitSettingsMtx = _uiState.UserOrbitSettingsMtx;
            
            // Only initialize game-specific settings if a game is loaded
            if (_uiState.Game != null)
            {
                _gameSettings = _uiState.Game.Settings;
                _isThreaded = _gameSettings.EnableMultiThreading;
                _enforceSingleThread = _gameSettings.EnforceSingleThread;
                _relativeOrbitVelocity = _gameSettings.UseRelativeVelocity;
                _strictNewtonion = _gameSettings.StrictNewtonion;
            }

            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            _orbitalDebugWindow = OrbitalDebugWindow.GetInstance();
            _logWindow = GameLogWindow.GetInstance();

            var settings = _uiState.GameSettings;

            if (settings != null)
            {
                var curRes = Array.FindIndex(
                        GameSettings.DisplayModes,
                        r => r.W == settings.WindowWidth && r.H == settings.WindowHeight);
                if (curRes >= 0)
                    _currentResolutionIndex = curRes + 1;
            }
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
                System.Numerics.Vector2 size = new System.Numerics.Vector2(600, 700);
                System.Numerics.Vector2 pos = new System.Numerics.Vector2(
                        _uiState.ViewPort.Size.Width / 2 - size.X / 2,
                        _uiState.ViewPort.Size.Height / 2 - size.Y / 2);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

                if (Window.Begin("Game Settings", ref IsActive, _flags))
                {
                    if (ImGui.BeginTabBar("SettingsTabs"))
                    {
                        if (ImGui.BeginTabItem("Display"))
                        {
                            DisplayVideoSettings();
                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("Audio"))
                        {
                            DisplayAudioSettings();
                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("Interface"))
                        {
                            DisplayInterfaceSettings();
                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("Debug"))
                        {
                            DisplayDebugSettings();
                            ImGui.EndTabItem();
                        }

                        if (_uiState.IsGameLoaded && ImGui.BeginTabItem("Game"))
                        {
                            DisplayGameSettings();
                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("Map"))
                        {
                            DisplayMapSettings();
                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                    }

                    ImGui.Separator();
                    
                    if (ImGui.Button("Apply Settings"))
                    {
                        ApplySettings();
                    }
                    
                    ImGui.SameLine();
                    
                    if (ImGui.Button("Save Settings"))
                    {
                        SaveSettings();
                    }
                    
                    ImGui.SameLine();
                    
                    if (ImGui.Button("Reset to Defaults"))
                    {
                        ResetToDefaults();
                    }
                }

                Window.End();
            }
        }

        private void DisplayVideoSettings()
        {
            ImGui.Text("Display Settings");
            ImGui.Separator();

            var settings = _uiState.GameSettings;
            
            // Debug info
            var currentWindowSize = _uiState.ViewPort.Size;
            ImGui.Text($"Current Window Size: {(int)currentWindowSize.Width}x{(int)currentWindowSize.Height}");
            ImGui.Text("GameSettings Size: " + ((settings.WindowWidth > 0 && settings.WindowHeight > 0) ?
                        $"{settings.WindowWidth}x{settings.WindowHeight}" :
                        "Automatic"));
            ImGui.Separator();
            
            // Display Mode
            ImGui.Text("Display Mode:");
            int displayModeIndex = (int)settings.DisplayMode;
            string[] displayModes = { "Windowed", "Fullscreen", "Borderless Fullscreen" };
            if (ImGui.Combo("##DisplayMode", ref displayModeIndex, displayModes, displayModes.Length))
            {
                settings.DisplayMode = (Pulsar4X.Client.GameSettings.DisplayModeType)displayModeIndex;
            }

            // Resolution
            string[] autoStr = { "Automatic" };
            var dispModesStr = GameSettings.DisplayModes.Select(r => $"{r.W}x{r.H}");
            var resolutionStr = autoStr.Union(dispModesStr).ToArray();

            var isWindowed = displayModeIndex == (int)GameSettings.DisplayModeType.Windowed;
            if (!isWindowed)
                ImGui.BeginDisabled();

            ImGui.Text("Resolution:");
            if (ImGui.Combo("##Resolution", ref _currentResolutionIndex, resolutionStr, resolutionStr.Length))
            {
                if (_currentResolutionIndex > 0)
                {
                    var selectedRes = GameSettings.DisplayModes[_currentResolutionIndex - 1];
                    settings.WindowWidth = selectedRes.W;
                    settings.WindowHeight = selectedRes.H;
                }
                else
                {
                    settings.WindowWidth = settings.WindowHeight = -1;
                }
            }

            if (!isWindowed)
                ImGui.EndDisabled();

            // VSync
            bool vsync = settings.VSync;
            if (ImGui.Checkbox("VSync", ref vsync))
            {
                settings.VSync = vsync;
            }
        }

        private void DisplayAudioSettings()
        {
            ImGui.Text("Audio Settings");
            ImGui.Separator();

            var settings = _uiState.GameSettings;

            // Audio Enabled
            bool audioEnabled = settings.AudioEnabled;
            if (ImGui.Checkbox("Enable Audio", ref audioEnabled))
            {
                settings.AudioEnabled = audioEnabled;
            }

            if (settings.AudioEnabled)
            {
                // Master Volume
                float masterVolume = settings.MasterVolume;
                if (ImGui.SliderFloat("Master Volume", ref masterVolume, 0.0f, 1.0f, "%.2f"))
                {
                    settings.MasterVolume = masterVolume;
                }

                // Music Volume
                float musicVolume = settings.MusicVolume;
                if (ImGui.SliderFloat("Music Volume", ref musicVolume, 0.0f, 1.0f, "%.2f"))
                {
                    settings.MusicVolume = musicVolume;
                }

                // Sound Effects Volume
                float sfxVolume = settings.SoundEffectsVolume;
                if (ImGui.SliderFloat("Sound Effects Volume", ref sfxVolume, 0.0f, 1.0f, "%.2f"))
                {
                    settings.SoundEffectsVolume = sfxVolume;
                }
            }
        }

        private void DisplayInterfaceSettings()
        {
            ImGui.Text("Interface Settings");
            ImGui.Separator();

            var settings = _uiState.GameSettings;

            // UI Scale
            float uiScale = settings.UIScale;
            if (ImGui.SliderFloat("UI Scale", ref uiScale, 0.5f, 3.0f, "%.2f"))
            {
                settings.UIScale = uiScale;
                // Apply UI scale immediately
                ImGui.GetStyle().FontScaleMain = uiScale;
                //ImGui.GetStyle().ScaleAllSizes(uiScale);
            }

            // Show Tooltips
            bool showTooltips = settings.ShowTooltips;
            if (ImGui.Checkbox("Show Tooltips", ref showTooltips))
            {
                settings.ShowTooltips = showTooltips;
            }

            // Show FPS
            bool showFPS = settings.ShowFPS;
            if (ImGui.Checkbox("Show FPS Counter", ref showFPS))
            {
                settings.ShowFPS = showFPS;
            }

            // 24-hour clock
            bool europeClock = settings.EuropeClock;
            if (ImGui.Checkbox("24-hour clock", ref europeClock))
            {
                settings.EuropeClock = europeClock;
            }
        }

        private void DisplayDebugSettings()
        {
            ImGui.Text("Debug Settings");
            ImGui.Separator();

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
                   || lastClickedEntity.HasDataBlob<NewtonMoveDB>())
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
            

            bool debugGUIActive = DebugGUIWindow.GetInstance().GetActive();
            if (ImGui.Checkbox("Show Pulsar GUI Debug Window", ref debugGUIActive))
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
                    DamageViewerWindow.GetInstance().SetActive();
                else
                    DamageViewerWindow.GetInstance().SetActive(false);
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
        }

        private void DisplayGameSettings()
        {
            if (_uiState.Game == null || _gameSettings == null) return;

            ImGui.Text("Game Process Settings");
            ImGui.Separator();

            if (ImGui.Checkbox("MultiThreaded", ref _isThreaded))
            {
                _gameSettings.EnableMultiThreading = _isThreaded;
            }

            if (ImGui.Checkbox("EnforceSingleThread", ref _enforceSingleThread))
            {
                _gameSettings.EnforceSingleThread = _enforceSingleThread;
                if (_enforceSingleThread)
                {
                    _isThreaded = false;
                    _gameSettings.EnableMultiThreading = false;
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

        private void DisplayMapSettings()
        {
            ImGui.Text("Map Settings");
            ImGui.Separator();

            for (int i = 0; i < Utils.EnumEntries<UserOrbitSettings.OrbitBodyType>(); i++)
            {
                UserOrbitSettings.OrbitBodyType otype = (UserOrbitSettings.OrbitBodyType)i;
                string typeStr = otype.ToString();
                if (ImGui.TreeNode(typeStr))
                {
                    float _nameZoomLevel = _uiState.DrawNameZoomLvl[otype];
                    ImGui.SliderFloat("Draw Names at Zoom: ", ref _nameZoomLevel, 0.01f, 10000f);
                    _uiState.DrawNameZoomLvl[otype] = _nameZoomLevel;
                    
                    for (int j = 0; j < Utils.EnumEntries<UserOrbitSettings.OrbitTrajectoryType>(); j++)
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
                            {
                                _uiState.SelectedSysMapRender?.UpdateUserOrbitSettings();
                            }

                            if (ImGui.SliderInt("Number Of Segments ##" + i + j, ref _arcSegments, 1, 255, _userOrbitSettings.NumberOfArcSegments.ToString()))
                            {
                                _userOrbitSettings.NumberOfArcSegments = (byte)_arcSegments;
                                _uiState.SelectedSysMapRender?.UpdateUserOrbitSettings();
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
                                _uiState.SelectedSysMapRender?.UpdateUserOrbitSettings();
                            }

                            if (ImGui.SliderInt("Min Alpha  ##" + i + j, ref _minAlpha, 0, _maxAlpha, ""))
                            {
                                _userOrbitSettings.MinAlpha = (byte)_minAlpha;
                                _uiState.SelectedSysMapRender?.UpdateUserOrbitSettings();
                            }
                            ImGui.TreePop();
                        }
                    }
                    ImGui.TreePop();
                }
            }

            if(ImGui.Button("Save Map Changes"))
            {
                ((PulsarMainWindow)_uiState.ViewPort).SaveOrbitSettings();
            }
        }

        private void ApplySettings()
        {
            Helpers.SetClock((_uiState.GameSettings.EuropeClock) ? "en-150" : "en");
            _uiState.GameSettings.ApplyDisplaySettings(_uiState.ViewPort);
        }

        private void SaveSettings()
        {
            _uiState.GameSettings.Save();
            ((PulsarMainWindow)_uiState.ViewPort).SaveOrbitSettings();
        }

        private void ResetToDefaults()
        {
            _uiState.GameSettings = new GameSettings();
            ImGui.GetStyle().FontScaleMain = 1.0f;
            //ImGui.GetStyle().ScaleAllSizes(1.0f);
        }
    }
}
