using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;

namespace Pulsar4X.SDL2UI
{

    public class PerformanceWindow : PulsarGuiWindow
    {
        Stopwatch _sw = new Stopwatch();

        SystemState? _systemState;

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

        private List<(string txt, double time, double count)> _callData = new List<(string txt, double time, double count)>();


        private PerformanceWindow()
        {
            // TODO: fix this
            // _dataBlobTypes = new Type[EntityManager.DataBlobTypes.Count];
            // _dataBlobTypeStrings = new String[EntityManager.DataBlobTypes.Count];

            // int i = 0;
            // foreach (var kvp in EntityManager.DataBlobTypes)
            // {
            //     _dataBlobTypes[i]=kvp.Key;
            //     _dataBlobTypeStrings[i] = kvp.Key.Name;
            //     i++;
            // }
        }
        internal static PerformanceWindow GetInstance()
        {
            PerformanceWindow instance;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(PerformanceWindow)))
                instance = new PerformanceWindow();
            else
            {
                instance = (PerformanceWindow)_uiState.LoadedWindows[typeof(PerformanceWindow)];
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
            if(IsActive && _systemState != null && ImGui.Begin("Perf Display"))
            {
                SetFrameRateArray();
                ImGui.Text("Global Tick: "); ImGui.SameLine();
                var t_lpt = _uiState.Game.TimePulse.LastProcessingTime.TotalMilliseconds;
                var t_tf = _uiState.Game.TimePulse.TickFrequency.TotalMilliseconds;
                var txt_lpt = t_lpt.ToString();
                var col = new Vector4(0, 1, 0, 1);
                if (t_lpt > _uiState.Game.TimePulse.TickFrequency.TotalMilliseconds)
                    col = new Vector4(1, 0, 0, 1);
                ImGui.Text(txt_lpt); ImGui.SameLine();
                var overtime = t_lpt - t_tf;
                ImGui.TextColored(col, overtime.ToString());

                System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess();
                ImGui.Text("Physical mem usage:"); ImGui.SameLine();
                ImGui.Text((p.WorkingSet64 / 1048576).ToString() + "MiB");
                ImGui.Text("Paged system mem size:"); ImGui.SameLine();
                ImGui.Text((p.PagedSystemMemorySize64 / 1048576).ToString() + "MiB");
                ImGui.Text("Paged mem size:"); ImGui.SameLine();
                ImGui.Text((p.PagedMemorySize64 / 1048576).ToString() + "MiB");

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

                var data = _systemState.StarSystem.ManagerSubpulses.Performance.GetLatestEntry();
                ImGui.Text($"StarSystemID: {_systemState.StarSystem.ID}");
                ImGui.Columns(4);
                ImGui.SetColumnWidth(0, 160);
                ImGui.SetColumnWidth(1, 64);
                ImGui.SetColumnWidth(2, 128);
                ImGui.SetColumnWidth(3, 128);


                ImGui.Text("Name"); ImGui.NextColumn();
                ImGui.Text("Count"); ImGui.NextColumn();
                ImGui.Text("Run Time"); ImGui.NextColumn();
                ImGui.Text("Average"); ImGui.NextColumn();
                string str = "";
                foreach (var (key, values) in data.TimesById)
                {

                    ImGui.Text(key);
                    ImGui.NextColumn();
                    ImGui.Text( values.times.Count.ToString());

                    ImGui.NextColumn();
                    str = $"{(values.sum):0.00}ms";
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(str).X - ImGui.GetScrollX() - 2 * ImGui.GetStyle().ItemSpacing.X);
                    ImGui.Text(str);
                    ImGui.NextColumn();
                    str = $"{(values.sum / values.times.Count):0.00}ms";
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(str).X - ImGui.GetScrollX() - 2 * ImGui.GetStyle().ItemSpacing.X);
                    ImGui.Text( str );
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
                ImGui.Text($"    IsProcecssing: {_systemState.StarSystem.ManagerSubpulses.IsProcessing}");
                ImGui.Text($"    CurrentProcess: {_systemState.StarSystem.ManagerSubpulses.CurrentProcess}");
                ImGui.Text($"    Last Total ProcessTime: {data.FullIntervalTime}");

                var numDB = _systemState.StarSystem.GetAllDataBlobsOfType<OrbitDB>().Count;
                ImGui.Text($"ObitDB Count: {numDB}");
                numDB = _systemState.StarSystem.GetAllDataBlobsOfType<NewtonMoveDB>().Count;
                ImGui.Text($"NewtonMoveDB Count: {numDB}");
                numDB = _systemState.StarSystem.GetAllDataBlobsOfType<SensorAbilityDB>().Count;
                ImGui.Text($"SensorAbilityDB Count: {numDB}");


                if(ImGui.CollapsingHeader("All Systems"))
                {
                    foreach (var starsys in _uiState.Game.Systems)
                    {
                        ImGui.Text(((StarSystem)starsys).ID.ToString());
                        ImGui.Text($"    IsProcecssing: {starsys.ManagerSubpulses.IsProcessing}");
                        ImGui.Text($"    CurrentProcess: {starsys.ManagerSubpulses.CurrentProcess}");
                        ImGui.Text($"    Last Total ProcessTime: {starsys.ManagerSubpulses.Performance.GetLatestEntry().FullIntervalTime}");

                    }
                }


                if (ImGui.CollapsingHeader("Call Times"))
                {



                    if (ImGui.Button("Time"))
                    {
                        _callData = new List<(string txt, double time, double count)>();
                        _sw.Restart();
                        List<Entity> entites = _systemState.StarSystem.GetAllEntitiesWithDataBlob<OrbitDB>();
                        _sw.Stop();
                        _callData.Add((
                              "Using GetEntitysWithDatablob\n {0,0} ticks to retreave {1,24} Entites",
                              _sw.Elapsed.Ticks,
                              entites.Count
                              ));


                        _sw.Restart();
                        var datablobs = _systemState.StarSystem.GetAllDataBlobsOfType<OrbitDB>();
                        _sw.Stop();
                        _callData.Add((
                                          "Using GetAllDataBlobsOfType<T>()\n {0,0} ticks to retreave {1,24} Entites",
                                          _sw.Elapsed.Ticks,
                                          datablobs.Count
                                      ));

                        _sw.Restart();
                        datablobs = _systemState.StarSystem.GetAllDataBlobsOfType<OrbitDB>();
                        _sw.Stop();
                        _callData.Add((
                                          "Using GetAllDataBlobsOfType<T>(int typeIndex)\n {0,0} ticks to retreave {1,24} Entites",
                                          _sw.Elapsed.Ticks,
                                          datablobs.Count
                                      ));


                        var db = datablobs[0];
                        if(db.OwningEntity != null)
                        {
                            _sw.Restart();
                            var ent = db.OwningEntity;
                            _sw.Stop();
                            _callData.Add((
                                            "Using datablob.OwningEntity\n {0,0} ticks to retreave the entity",
                                            _sw.Elapsed.Ticks,
                                            1
                                        ));

                            _sw.Restart();
                            ent.GetDataBlob<OrbitDB>();
                            _sw.Stop();
                            _callData.Add((
                                            "Using entity.GetDataBlob<T>()\n {0,0} ticks to get db",
                                            _sw.Elapsed.Ticks,
                                            1
                                            ));

                            _sw.Restart();
                            ent.GetDataBlob<OrbitDB>();
                            _sw.Stop();
                            _callData.Add((
                                            "Using entity.GetDataBlob<T>(typeIndex)\n {0,0} ticks to get db",
                                            _sw.Elapsed.Ticks,
                                            1
                                        ));


                            _sw.Restart();
                            ent.RemoveDataBlob<OrbitDB>();
                            _sw.Stop();
                            _callData.Add((
                                            "Using entity.RemoveDataBlob<T>()\n {0,0} ticks to remove from entity",
                                            _sw.Elapsed.Ticks,
                                            1
                                        ));

                            _sw.Restart();
                            ent.SetDataBlob(db);
                            _sw.Stop();
                            _callData.Add((
                                            "Using entity.SetDataBlob(db)\n {0,0} ticks to add to entity",
                                                _sw.Elapsed.Ticks,
                                                1
                                            ));

                            _sw.Restart();
                            // FIXME: ?
                            //ent.RemoveDataBlob(typeIndex);
                            _sw.Stop();
                            _callData.Add((
                                                "Using entity.RemoveDataBlob(typeIndex)\n {0,0} ticks to remove from entity",
                                                _sw.Elapsed.Ticks,
                                                1
                                            ));

                            _sw.Restart();
                            // FIXME: ?
                            //ent.SetDataBlob(db, typeIndex);
                            _sw.Stop();
                            _callData.Add((
                                                "Using entity.SetDataBlob(db, typeIndex)\n {0,0} ticks to add to entity",
                                                _sw.Elapsed.Ticks,
                                                1
                                            ));
                        }
                    }

                    foreach (var dat in _callData)
                    {
                        string foo = string.Format(dat.txt, dat.time, dat.count);
                        ImGui.Text(foo);
                    }
                }

                if (ImGui.Button("Record To File"))
                {
                    RecordToFile();
                }
            }
        }

        void RecordToFile()
        {
            if(_systemState == null) return;

            var t_lpt = _uiState.Game.TimePulse.LastProcessingTime.TotalMilliseconds;
            var t_tf = _uiState.Game.TimePulse.TickFrequency.TotalMilliseconds;
            var overtime = t_lpt - t_tf;
            var starsysdata = _systemState.StarSystem.ManagerSubpulses.Performance.GetLatestEntry();
            var dirst = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if(dirst == null) throw new InvalidOperationException($"{dirst} cannot be null");
            var dirinf = new System.IO.DirectoryInfo(dirst);
            var dir = dirinf.Parent?.Parent?.Parent?.Parent;
            if(dir == null) throw new InvalidOperationException("dir cannot be null");
            string machine = Environment.MachineName;
            string gitver = AssemblyInfo.GetGitHash();
            string datetime = DateTime.Now.ToString();
            string threaded = string.Format("{0,-28}{1,16}","Threaded:", _uiState.Game.Settings.EnableMultiThreading.ToString());
            string timespan = string.Format("{0,-28}{1,16}","Time Span:" , _uiState.Game.TimePulse.Ticklength.ToString());
            string txt_lpt =  string.Format("{0,-28}{1,16}","Full Process Time:", t_lpt.ToString());

            string sysname = _systemState.StarSystem.NameDB.OwnersName;
            string sysptime = string.Format("{0,0} {1,-24}:{2,15}",sysname, "Time:", starsysdata.FullIntervalTime.ToString("0.0000"));
            var fpath = System.IO.Path.Combine(dir.FullName, "Perflog_" + machine);

            //var sb = StringBuilder(gitver);
            string dataString = "\n" + gitver + "\n"
                                         + datetime + "\n"
                                         + threaded + "\n"
                                         + timespan + "\n"
                                         + txt_lpt + "\n"
                                         + sysptime + "\n";
            foreach (var (key, values) in starsysdata.TimesById)
            {
                dataString += string.Format("{0,-28}:{1,16}", key, values.sum.ToString("0.0000") + "\n");
            }

            dataString += "_________________________________________________";
            //if (!System.IO.File.Exists(fpath))
                //System.IO.File.Create(fpath);
            System.IO.File.AppendAllText(fpath, dataString);
        }

        public override void OnGameTickChange(DateTime newDate)
        {
            _currentGFPS = (float)_uiState.Game.TimePulse.LastSubtickTime.TotalSeconds;

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
    }
}