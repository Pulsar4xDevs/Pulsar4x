using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class SensorDraw : PulsarGuiWindow
    {
        private EntityState _selectedEntitySate;
        private Entity _selectedEntity => _selectedEntitySate.Entity;
        private Entity[] _potentialTargetEntities;
        private string[] _potentialTargetNames;
        private int _targetIndex = -1;
        private Entity _targetEntity;
        private SensorProfileDB _targetSensorProfile;
        
        private SystemState _selectedStarSysState;
        private StarSystem _selectedStarSys => _selectedStarSysState.StarSystem;

        private SensorReceverAtbDB[] _selectedReceverAtb;

        private ImDrawListPtr _draw_list;
        
        private (Vector2 p0, Vector2 p1, Vector2 p2)[] _recevers = new (Vector2, Vector2, Vector2)[0];
        private bool[] _drawRecvers = new bool[0];
        private uint[] _receverColours;
        
        private (Vector2 p0, Vector2 p1, Vector2 p2)[] _reflecters = new (Vector2, Vector2, Vector2)[0];
        private bool[] _drawReflectors = new bool[0];
        private uint[] _reflectorColours;
        
        private (Vector2 p0, Vector2 p1, Vector2 p2)[] _emmitters = new (Vector2, Vector2, Vector2)[0];
        private bool[] _drawEmmiters = new bool[0];
        private uint[] _emmiterColours;
        
        private double lowestWave = 0;
        private double highestWave = 0;
        private double lowestMag = 0;
        private double highestMag = 0;
        private Vector2 _scalingFactor = new Vector2(0.1f, 0.1f);
        private Vector2 _translation = new Vector2(0,0);
        private SensorDraw() 
        {
            _draw_list = ImGui.GetWindowDrawList();
        }
        internal static SensorDraw GetInstance()
        {
            SensorDraw instance;
            if (!_state.LoadedWindows.ContainsKey(typeof(SensorDraw)))
                instance = new SensorDraw();
            else
            {
                instance = (SensorDraw)_state.LoadedWindows[typeof(SensorDraw)];
                if(_state.LastClickedEntity?.Entity != null)
                    instance._selectedEntitySate = _state.LastClickedEntity;
            }
            if(instance._selectedEntitySate != null)
            {
                if (_state.LastClickedEntity?.Entity != null && instance._selectedEntity != _state.LastClickedEntity.Entity)
                    instance._selectedEntitySate = _state.LastClickedEntity;
            }
            else
            {
                if(_state.LastClickedEntity?.Entity != null)
                    instance._selectedEntitySate = _state.LastClickedEntity;
            }
            
            instance._selectedStarSysState = _state.StarSystemStates[_state.SelectedStarSysGuid];
            return instance;
        }
        
        
        
        
        
        
        internal override void Display()
        {
            //
            if(!IsActive || _selectedEntitySate == null)
                return;
            ImGui.SetNextWindowSize(new Vector2(1500, 800));
            if (ImGui.Begin("Sensor Display", ref IsActive))
            {
                
                if (_selectedEntity.HasDataBlob<SensorAbilityDB>())
                {
                    if(_selectedReceverAtb == null || ImGui.Button("refresh") )
                    {
                        SetSensorData();     
                    }
                    
                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, 300);
                    
                    if(ImGui.Combo("Targets", ref _targetIndex, _potentialTargetNames, _potentialTargetNames.Length))
                    {
                        _targetEntity = _potentialTargetEntities[_targetIndex];
                        SetTargetData();
                    }
            
                    
                    ImGui.Text("lowest_x: " + lowestWave);
                    ImGui.Text("highest_x: " + highestWave);
                    ImGui.Text("lowest_y: " + lowestMag);
                    ImGui.Text("highest_y: " + highestMag);
                    if(_targetSensorProfile != null)
                        ImGui.Text("target cross section: " + _targetSensorProfile.TargetCrossSection_msq);
                    
                    
                    
                    uint borderColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                    
                    uint receverColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 1.0f, 0.5f, 1.0f));
                    uint receverFill = ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 1.0f, 0.5f, 0.75f));
                    
                    uint reflectedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.5f, 1.0f));
                    uint reflectedFill = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.5f, 0.75f));
                    
                    uint emittedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.25f, 1.0f));
                    uint emittedFill = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.25f, 0.5f));


                    ImGui.Text("Recevers: ");
                    ImGui.PushID("recevers");
                    WavInfo(_drawRecvers, _recevers);
                    ImGui.PopID();
                    if(_reflecters != null)
                    {
                        ImGui.PushID("reflectors");
                        ImGui.Text("Reflectors:");
                        WavInfo(_drawReflectors, _reflecters);
                        ImGui.PopID();
                    }
                    if(_emmitters != null)
                    {
                        ImGui.PushID("emmiters");
                        ImGui.Text("Emmiters:");
                        WavInfo(_drawEmmiters, _emmitters);
                        ImGui.PopID();
                    }
                
                    
                    ImGui.NextColumn();
                    
                    // ImDrawList API uses screen coordinates!
                    Vector2 canvas_pos = ImGui.GetCursorScreenPos();
                    Vector2 canvas_size = ImGui.GetContentRegionAvail();
                    Vector2 canvas_endPos = canvas_pos + canvas_size;
                    Vector2 waveBounds = new Vector2((float)(highestWave - lowestWave),(float)( highestMag - lowestMag));

                    _scalingFactor.X = 1 / (waveBounds.X / canvas_size.X);
                    _scalingFactor.Y = 1 / (waveBounds.Y / canvas_size.Y);

                    _translation.X = (float)(canvas_pos.X - lowestWave * _scalingFactor.X);
                    _translation.Y = (float)(canvas_pos.Y - lowestMag * _scalingFactor.Y);  
     
                    _draw_list.AddRect(canvas_pos, canvas_endPos, borderColour );
                    
                    ImGui.Text("Scale:");
                    ImGui.Text("X: " + _scalingFactor.X + " Y: " + _scalingFactor.Y);
                    
                    Vector2 p0 = _translation + new Vector2((float)lowestWave, (float)lowestMag) * _scalingFactor;
                    Vector2 p1 = _translation + new Vector2((float)highestWave, (float)highestMag) * _scalingFactor;
                    ImGui.Text("Box From: " + p0);
                    ImGui.Text("Box To:   " + p1);

                    DrawWav(_drawRecvers, _recevers, receverColour);

                    DrawWav(_drawReflectors, _reflecters, reflectedColour);

                    DrawWav(_drawEmmiters, _emmitters, emittedColour);

                }

                void DrawWav(bool[] enabledArray, (Vector2 p0, Vector2 p1, Vector2 p2)[] wavesArry, uint colour)
                {
                    for (int i = 0; i < enabledArray.Length; i++)
                    {
                        if (enabledArray[i])
                        {
                            Vector2 p0 = _translation + wavesArry[i].p0 * _scalingFactor;
                            Vector2 p1 = _translation + wavesArry[i].p1 * _scalingFactor;
                            Vector2 p2 = _translation + wavesArry[i].p2 * _scalingFactor;
                            //_draw_list.AddLine(p0, p1, colour);
                            //_draw_list.AddLine(p1, p2, colour);
                            _draw_list.AddTriangleFilled(p0, p1, p2, colour);
                        }
                    }
                }

                void WavInfo(bool[] enabledArray, (Vector2 p0, Vector2 p1, Vector2 p2)[] wavesArry)
                {
                    for (int i = 0; i < enabledArray.Length; i++)
                    {
                        if (ImGui.Checkbox("##drawbool" + i, ref enabledArray[i]))
                        {
                            ResetBounds();
                        }

                        ImGui.Text("MinWav: " + wavesArry[i].p0 .X);
                        ImGui.SameLine();
                        ImGui.Text("Magnitude: " + wavesArry[i].p0.Y);

                        ImGui.Text("AvgWav: " + wavesArry[i].p1.X);
                        ImGui.SameLine();
                        ImGui.Text("Magnitude: " + wavesArry[i].p1.Y);

                        ImGui.Text("MaxWav: " + wavesArry[i].p2.X);
                        ImGui.SameLine();
                        ImGui.Text("Magnitude: " + wavesArry[i].p2.Y);
                    }
                }

                void ResetBounds()
                {
                    lowestWave = float.MaxValue;
                    lowestMag = float.MaxValue;
                    highestMag = 0;
                    highestWave = 0;
                    

                    for (int i = 0; i < _drawRecvers.Length; i++)
                    {
                        if(_drawRecvers[i])
                        {
                            float low = _recevers[i].p0.X;
                            float high = _recevers[i].p2.X;
                            float mag1 = _recevers[i].p2.Y;
                            float mag2 = _recevers[i].p1.Y;
                            if (low < lowestWave)
                                lowestWave = low;
                            if (high > highestWave)
                                highestWave = high;
                            if (mag1 > highestMag)
                                highestMag = mag1;
                            if (mag2 < lowestMag)
                                lowestMag = mag2;
                        }
                    }

                    for (int i = 0; i < _drawReflectors.Length; i++)
                    {
                        if(_drawReflectors[i])
                        {
                            float low = _reflecters[i].p0.X;
                            float high = _reflecters[i].p2.X;
                            float mag1 = _reflecters[i].p0.Y;
                            float mag2 = _reflecters[i].p1.Y;

                            if (low < lowestWave)
                                lowestWave = low;
                            if (high > highestWave)
                                highestWave = high;
                            
                            if (mag1 < lowestMag)
                                lowestMag = mag1;
                            if (mag2 > highestMag)
                                highestMag = mag2;
                        }
                    }
                    
                    for (int i = 0; i < _drawEmmiters.Length; i++)
                    {
                        if(_drawEmmiters[i])
                        {
                            float low = _emmitters[i].p0.X;
                            float high = _emmitters[i].p2.X;
                            float mag1 = _emmitters[i].p0.Y;
                            float mag2 = _emmitters[i].p1.Y;
                            
                            if (low < lowestWave)
                                lowestWave = low;
                            if (high > highestWave)
                                highestWave = high;
                            
                            if (mag1 < lowestMag)
                                lowestMag = mag1;
                            if (mag2 > highestMag)
                                highestMag = mag2;
                        }
                    }
                }

                void SetSensorData()
                {                            
                    if (_selectedEntity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<SensorReceverAtbDB>(out var recevers))
                    {
                        _selectedReceverAtb = new SensorReceverAtbDB[recevers.Count];
                        _recevers = new (Vector2, Vector2, Vector2)[recevers.Count];
                        _drawRecvers = new bool[recevers.Count];
                        
                        int i = 0;
                        foreach (var recever in recevers)
                        {
                            _selectedReceverAtb[i] = recever.Design.GetAttribute<SensorReceverAtbDB>();
                            
                            
                            float low = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthMin_nm;
                            float mid = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthAverage_nm;
                            float high = (float)_selectedReceverAtb[i].RecevingWaveformCapabilty.WavelengthMax_nm;

                            float mag1 = (float)_selectedReceverAtb[i].WorstSensitivity_kW;
                            float mag2 = (float)_selectedReceverAtb[i].BestSensitivity_kW;
                            
                            _recevers[i].p0 = new Vector2(low, mag1);
                            _recevers[i].p1 = new Vector2(mid, mag2);
                            _recevers[i].p2 =  new Vector2(high, mag1);
                            i++;
                        }
                        
                        var tgts = _selectedStarSys.GetAllEntitiesWithDataBlob<SensorProfileDB>();
                        _potentialTargetNames = new string[tgts.Count];
                        _potentialTargetEntities = tgts.ToArray();
                        i = 0;
                        foreach (var target in tgts)
                        {
                            string name = target.GetDataBlob<NameDB>().GetName(_state.Faction);
                            _potentialTargetNames[i] = name;
                            i++;
                        }
                    }
                }

                void SetTargetData()
                {
                    _targetSensorProfile = _targetEntity.GetDataBlob<SensorProfileDB>();
                    SetReflectedEMProfile.SetEntityProfile(_targetEntity, _state.PrimarySystemDateTime);
                    var emitted = _targetSensorProfile.EmittedEMSpectra;
                    var reflected = _targetSensorProfile.ReflectedEMSpectra;

                    _reflecters = new (Vector2, Vector2, Vector2)[reflected.Count];
                    _drawReflectors = new bool[reflected.Count];
                    _emmitters = new (Vector2, Vector2, Vector2)[emitted.Count];
                    _drawEmmiters = new bool[emitted.Count];
                    int i = 0;
                    foreach (var waveformkvp in reflected)
                    {
                        float low = (float)waveformkvp.Key.WavelengthMin_nm;
                        float mid = (float)waveformkvp.Key.WavelengthAverage_nm;
                        float high = (float)waveformkvp.Key.WavelengthMax_nm;
                        float magnatude = (float)waveformkvp.Value;
                        if (float.IsInfinity(magnatude))
                            magnatude = float.MaxValue;
                        
                        _reflecters[i].p0 = new Vector2(low, 0);
                        _reflecters[i].p1 = new Vector2(mid, magnatude);
                        _reflecters[i].p2 = new Vector2(high, 0);
                        i++;
                    }
                    
                    i = 0;
                    foreach (var waveformkvp in emitted)
                    {
                        float low = (float)waveformkvp.Key.WavelengthMin_nm;
                        float mid = (float)waveformkvp.Key.WavelengthAverage_nm;
                        float high = (float)waveformkvp.Key.WavelengthMax_nm;
                        float magnatude = (float)waveformkvp.Value;
                        if (float.IsInfinity(magnatude))
                            magnatude = float.MaxValue;
                        _emmitters[i].p0 = new Vector2(low, 0);
                        _emmitters[i].p1 = new Vector2(mid, magnatude);
                        _emmitters[i].p2 =  new Vector2(high, 0);
                        i++;
                    }
                }
            }
        }
    }
}