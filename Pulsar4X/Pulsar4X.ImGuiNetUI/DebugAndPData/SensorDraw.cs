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
        private Vector2[] _recevers;
        private bool[] _drawRecvers;
        private Vector2[] _reflecters;
        private bool[] _drawReflectors;
        private Vector2[] _emmitters;
        private bool[] _drawEmmiters;
        private double lowestWave = 0;
        private double highestWave = 0;
        private double lowestMag = 0;
        private double highestMag = 0;
        private Vector2 scalingFactor = new Vector2(0.1f, 0.1f);
        private SensorDraw() 
        {
              
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
                            if (_selectedEntity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<SensorReceverAtbDB>(out var recevers))
                            {
                                _selectedReceverAtb = new SensorReceverAtbDB[recevers.Count];
                                _recevers = new Vector2[recevers.Count * 3];
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
                                    
                                    Vector2 p0 = new Vector2(low, mag1);
                                    Vector2 p1 = new Vector2(mid, mag2);
                                    Vector2 p2 =  new Vector2(high, mag1);

                                    _recevers[i] = p0;
                                    _recevers[i + 1] = p1;
                                    _recevers[i + 2] = p2;

                                    if (i == 0)
                                    {                                    
                                        lowestWave = low;
                                        lowestMag = mag2;
                                    }

                                    if (low < lowestWave)
                                        lowestWave = low;
                                    if (high > highestWave)
                                        highestWave = high;
                                    
                                    if (mag1 > highestMag)
                                        highestMag = mag1;
                                    if (mag2 < lowestMag)
                                        lowestMag = mag2;
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
                        
                        
                        ImDrawListPtr draw_list = ImGui.GetWindowDrawList();
                        uint borderColour = ImGui.ColorConvertFloat4ToU32(new Vector4(50, 50, 50, 255));
                        
                        uint receverColour = ImGui.ColorConvertFloat4ToU32(new Vector4(50, 50, 50, 255));
                        uint receverFill = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 200, 0, 50));
                        uint reflectedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(200, 200, 50, 255));
                        uint reflectedFill = ImGui.ColorConvertFloat4ToU32(new Vector4(200, 200, 50, 100));
                        uint emittedColour = ImGui.ColorConvertFloat4ToU32(new Vector4(200, 40, 50, 255));
                        uint emittedFill = ImGui.ColorConvertFloat4ToU32(new Vector4(200, 40, 50, 100));


                        WavInfo(_drawRecvers, _recevers);
                        if(_reflecters != null)
                            WavInfo(_drawReflectors, _reflecters);
                        if(_emmitters != null)
                            WavInfo(_drawEmmiters, _emmitters);
                    
                        
                        ImGui.NextColumn();
                        Vector2 canvas_pos = ImGui.GetCursorScreenPos();
                        //canvas_pos.X += 300;// ImDrawList API uses screen coordinates!
                        Vector2 canvas_size = ImGui.GetContentRegionAvail();
                        //canvas_size.X -= 300;
                        Vector2 canvas_endPos = canvas_pos + canvas_size;
                        Vector2 waveBounds = new Vector2((float)(highestWave - lowestWave),(float)( highestMag - lowestMag));

                        scalingFactor.X = 1 / (waveBounds.X / canvas_size.X);
                        scalingFactor.Y = 1 / (waveBounds.Y / canvas_size.Y);
                        
                        Vector2 translation = new Vector2
                        (
                            (float)(canvas_pos.X - lowestWave * scalingFactor.X), 
                            (float)(canvas_pos.Y - lowestMag * scalingFactor.Y)  
                        );
                        
                        
                        draw_list.AddRect(canvas_pos, canvas_endPos, borderColour );
                        
                        for (int i = 0; i < _recevers.Length / 3; i++)
                        {
                            

                            Vector2 p0 = translation + _recevers[i] * scalingFactor;
                            Vector2 p1 = translation + _recevers[i+1] * scalingFactor;
                            Vector2 p2 = translation + _recevers[i+2] * scalingFactor;
                            draw_list.AddTriangleFilled(p0, p1, p2, receverFill);
                            draw_list.AddLine(p0, p1, receverColour);
                            draw_list.AddLine(p1, p2, receverColour);
                            
                            
                        }
 
                        if(_reflecters != null)
                        {
                            for (int i = 0; i < _reflecters.Length / 3; i++)
                            {

                                Vector2 p0 = translation +  _reflecters[i] * scalingFactor;
                                Vector2 p1 = translation + _reflecters[i+1] * scalingFactor;
                                Vector2 p2 = translation + _reflecters[i+2] * scalingFactor;
                            
                                draw_list.AddLine(p0, p1, reflectedColour);
                                draw_list.AddLine(p1, p2, reflectedColour);
                                draw_list.AddTriangleFilled(p0, p1, p2, reflectedFill);
                            }
                        }
                        
                        if(_emmitters != null)
                        {
                            for (int i = 0; i < _emmitters.Length / 3; i++)
                            {


                                Vector2 p0 = translation + _emmitters[i] * scalingFactor;
                                Vector2 p1 = translation + _emmitters[i+1] * scalingFactor;
                                Vector2 p2 = translation + _emmitters[i+2] * scalingFactor;
                            
                                draw_list.AddLine(p0, p1, emittedColour);
                                draw_list.AddLine(p1, p2, emittedColour);
                                draw_list.AddTriangleFilled(p0, p1, p2, emittedFill);
                            }
                        }

                }

                void WavInfo(bool[] enabledArray, Vector2[] wavesArry)
                {
                    for (int i = 0; i < enabledArray.Length; i++)
                    {
                        ImGui.Checkbox("##recebool" + i, ref enabledArray[i]);
                        ImGui.Text("minWav: " + wavesArry[i].X);
                        ImGui.SameLine();
                        ImGui.Text("minSen: " + wavesArry[i].Y);

                        ImGui.Text("AvgWav: " + wavesArry[i + 1].X);
                        ImGui.SameLine();
                        ImGui.Text("MaxSen: " + wavesArry[i + 1].Y);

                        ImGui.Text("maxWav: " + wavesArry[i + 2].X);
                        ImGui.SameLine();
                        ImGui.Text("minSen: " + wavesArry[i + 2].Y);
                    }
                }

                void SetTargetData()
                {
                    _targetSensorProfile = _targetEntity.GetDataBlob<SensorProfileDB>();
                   // if (_targetSensorProfile.ReflectedEMSpectra.Count == 0)
                        SetReflectedEMProfile.SetEntityProfile(_targetEntity, _state.PrimarySystemDateTime);
                    var emitted = _targetSensorProfile.EmittedEMSpectra;
                    var reflected = _targetSensorProfile.ReflectedEMSpectra;

                    _reflecters = new Vector2[reflected.Count * 3];
                    _drawReflectors = new bool[reflected.Count];
                    _emmitters = new Vector2[emitted.Count * 3];
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
                        
                        Vector2 p0 = new Vector2(low, 0);
                        Vector2 p1 = new Vector2(mid, magnatude);
                        Vector2 p2 = new Vector2(high, 0);

                        _reflecters[i] = p0;
                        _reflecters[i + 1] = p1;
                        _reflecters[i + 2] = p2;



                        if (low < lowestWave)
                            lowestWave = low;
                        if (high > highestWave)
                            highestWave = high;
                        if (magnatude > highestMag)
                            highestMag = magnatude;
                        
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
                        
                        Vector2 p0 = new Vector2(low, 0);
                        Vector2 p1 = new Vector2(mid, magnatude);
                        Vector2 p2 =  new Vector2(high, 0);

                        _emmitters[i] = p0;
                        _emmitters[i + 1] = p1;
                        _emmitters[i + 2] = p2;
                        
                        if (low < lowestWave)
                            lowestWave = low;
                        if (high > highestWave)
                            highestWave = high;
                        if (magnatude > highestMag)
                            highestMag = magnatude;
                    }
                    
                }


            }
        }
    }
}